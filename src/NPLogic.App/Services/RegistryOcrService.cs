using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// 등기부등본 OCR 서비스
    /// Python FastAPI 서버를 관리하고 OCR API를 호출합니다.
    /// </summary>
    public class RegistryOcrService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private Process? _serverProcess;
        private bool _disposed;

        private const string DefaultServerUrl = "http://localhost:8000";
        private const string ServerExeName = "registry_ocr.exe";
        private const string HealthEndpoint = "/api/health";
        private const string OcrEndpoint = "/api/ocr/registry";
        private const int MaxStartupWaitSeconds = 30;
        private const int HealthCheckIntervalMs = 500;

        public string ServerUrl { get; private set; }
        public bool IsServerRunning { get; private set; }

        public RegistryOcrService(string? serverUrl = null)
        {
            ServerUrl = serverUrl ?? DefaultServerUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ServerUrl),
                Timeout = TimeSpan.FromMinutes(5) // OCR은 시간이 오래 걸릴 수 있음
            };
        }

        /// <summary>
        /// Python OCR 서버를 시작합니다.
        /// </summary>
        /// <param name="exePath">서버 실행 파일 경로 (null이면 자동 탐색)</param>
        /// <returns>서버 시작 성공 여부</returns>
        public async Task<bool> StartServerAsync(string? exePath = null)
        {
            // 이미 실행 중인지 확인
            if (await CheckHealthAsync())
            {
                IsServerRunning = true;
                return true;
            }

            // 실행 파일 경로 찾기
            var serverExePath = exePath ?? FindServerExePath();
            if (string.IsNullOrEmpty(serverExePath) || !File.Exists(serverExePath))
            {
                throw new FileNotFoundException(
                    $"OCR 서버 실행 파일을 찾을 수 없습니다: {serverExePath ?? ServerExeName}");
            }

            try
            {
                // 프로세스 시작
                var startInfo = new ProcessStartInfo
                {
                    FileName = serverExePath,
                    WorkingDirectory = Path.GetDirectoryName(serverExePath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _serverProcess = Process.Start(startInfo);

                if (_serverProcess == null)
                {
                    throw new Exception("서버 프로세스를 시작할 수 없습니다.");
                }

                // 서버가 준비될 때까지 대기
                var waitResult = await WaitForServerReadyAsync(MaxStartupWaitSeconds);
                
                if (!waitResult)
                {
                    StopServer();
                    throw new TimeoutException(
                        $"OCR 서버가 {MaxStartupWaitSeconds}초 내에 시작되지 않았습니다.");
                }

                IsServerRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                StopServer();
                throw new Exception($"OCR 서버 시작 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 서버 실행 파일 경로를 자동으로 찾습니다.
        /// </summary>
        private string? FindServerExePath()
        {
            // 검색할 경로 목록 (우선순위순)
            var searchPaths = new List<string>
            {
                // 1. 앱 실행 디렉토리의 backend 폴더
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "registry_ocr", ServerExeName),
                
                // 2. 앱 실행 디렉토리
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "registry_ocr", ServerExeName),
                
                // 3. 개발 환경: 프로젝트 상대 경로
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", 
                    "manager", "client", "Auction-Certificate", "dist", "registry_ocr", ServerExeName),
            };

            foreach (var path in searchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        /// <summary>
        /// 서버가 준비될 때까지 대기합니다.
        /// </summary>
        private async Task<bool> WaitForServerReadyAsync(int maxWaitSeconds)
        {
            var startTime = DateTime.Now;
            var maxWaitTime = TimeSpan.FromSeconds(maxWaitSeconds);

            while (DateTime.Now - startTime < maxWaitTime)
            {
                if (await CheckHealthAsync())
                {
                    return true;
                }

                await Task.Delay(HealthCheckIntervalMs);
            }

            return false;
        }

        /// <summary>
        /// 서버 헬스체크를 수행합니다.
        /// </summary>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync(HealthEndpoint, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var health = JsonSerializer.Deserialize<HealthResponse>(content);
                    return health?.Status == "ok";
                }
            }
            catch
            {
                // 연결 실패 등의 예외는 무시
            }

            return false;
        }

        /// <summary>
        /// Python OCR 서버를 중지합니다.
        /// </summary>
        public void StopServer()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(entireProcessTree: true);
                    _serverProcess.WaitForExit(5000);
                }
            }
            catch
            {
                // 종료 실패 무시
            }
            finally
            {
                _serverProcess?.Dispose();
                _serverProcess = null;
                IsServerRunning = false;
            }
        }

        /// <summary>
        /// 등기부등본 PDF를 OCR 처리합니다.
        /// </summary>
        /// <param name="pdfFilePath">PDF 파일 경로</param>
        /// <param name="progress">진행률 콜백</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>OCR 결과</returns>
        public async Task<OcrResult> ProcessPdfAsync(
            string pdfFilePath,
            IProgress<OcrProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(pdfFilePath))
            {
                throw new FileNotFoundException("PDF 파일을 찾을 수 없습니다.", pdfFilePath);
            }

            var fileName = Path.GetFileName(pdfFilePath);
            progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Uploading, 0));

            try
            {
                // 서버 상태 확인
                if (!await CheckHealthAsync())
                {
                    throw new Exception("OCR 서버가 응답하지 않습니다. 서버를 시작해주세요.");
                }

                progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Processing, 30));

                // Multipart form data 생성
                using var fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read);
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                content.Add(streamContent, "file", fileName);

                // API 호출
                var response = await _httpClient.PostAsync(OcrEndpoint, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Parsing, 90));

                // 응답 파싱
                var ocrResponse = JsonSerializer.Deserialize<OcrApiResponse>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (ocrResponse == null)
                {
                    throw new Exception("OCR 응답을 파싱할 수 없습니다.");
                }

                progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Completed, 100));

                if (!ocrResponse.Success)
                {
                    return new OcrResult
                    {
                        Success = false,
                        FileName = fileName,
                        Error = ocrResponse.Error ?? "알 수 없는 오류가 발생했습니다."
                    };
                }

                return new OcrResult
                {
                    Success = true,
                    FileName = fileName,
                    Data = ocrResponse.Data
                };
            }
            catch (TaskCanceledException)
            {
                progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Cancelled, 0));
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Failed, 0));
                return new OcrResult
                {
                    Success = false,
                    FileName = fileName,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 여러 PDF 파일을 OCR 처리합니다.
        /// </summary>
        public async Task<List<OcrResult>> ProcessMultiplePdfsAsync(
            IEnumerable<string> pdfFilePaths,
            IProgress<OcrBatchProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<OcrResult>();
            var fileList = new List<string>(pdfFilePaths);
            var total = fileList.Count;
            var current = 0;

            foreach (var pdfPath in fileList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                current++;
                var fileName = Path.GetFileName(pdfPath);

                progress?.Report(new OcrBatchProgress(
                    currentFile: fileName,
                    currentIndex: current,
                    totalFiles: total,
                    status: OcrProgressStatus.Processing
                ));

                var result = await ProcessPdfAsync(pdfPath, cancellationToken: cancellationToken);
                results.Add(result);

                progress?.Report(new OcrBatchProgress(
                    currentFile: fileName,
                    currentIndex: current,
                    totalFiles: total,
                    status: result.Success ? OcrProgressStatus.Completed : OcrProgressStatus.Failed
                ));
            }

            return results;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopServer();
                _httpClient.Dispose();
                _disposed = true;
            }
        }
    }

    #region DTOs

    /// <summary>
    /// 헬스체크 응답
    /// </summary>
    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// OCR API 응답
    /// </summary>
    public class OcrApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public OcrResultData? Data { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// OCR 결과 데이터
    /// </summary>
    public class OcrResultData
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("owners")]
        public List<Dictionary<string, object?>>? Owners { get; set; }

        [JsonPropertyName("gapgu")]
        public List<Dictionary<string, object?>>? Gapgu { get; set; }

        [JsonPropertyName("eulgu")]
        public List<Dictionary<string, object?>>? Eulgu { get; set; }
    }

    /// <summary>
    /// OCR 결과
    /// </summary>
    public class OcrResult
    {
        public bool Success { get; set; }
        public string FileName { get; set; } = string.Empty;
        public OcrResultData? Data { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// OCR 진행 상태
    /// </summary>
    public enum OcrProgressStatus
    {
        Uploading,
        Processing,
        Parsing,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// OCR 진행률 정보
    /// </summary>
    public class OcrProgress
    {
        public string FileName { get; }
        public OcrProgressStatus Status { get; }
        public int ProgressPercent { get; }

        public OcrProgress(string fileName, OcrProgressStatus status, int progressPercent)
        {
            FileName = fileName;
            Status = status;
            ProgressPercent = progressPercent;
        }
    }

    /// <summary>
    /// 배치 OCR 진행률 정보
    /// </summary>
    public class OcrBatchProgress
    {
        public string CurrentFile { get; }
        public int CurrentIndex { get; }
        public int TotalFiles { get; }
        public OcrProgressStatus Status { get; }
        public int OverallProgressPercent => TotalFiles > 0 ? (CurrentIndex * 100) / TotalFiles : 0;

        public OcrBatchProgress(string currentFile, int currentIndex, int totalFiles, OcrProgressStatus status)
        {
            CurrentFile = currentFile;
            CurrentIndex = currentIndex;
            TotalFiles = totalFiles;
            Status = status;
        }
    }

    #endregion
}




