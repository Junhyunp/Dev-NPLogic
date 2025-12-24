using System;
using System.Collections.Generic;
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
    /// PythonBackendService를 통해 서버 관리를 위임하고, OCR API 호출만 담당합니다.
    /// </summary>
    public class RegistryOcrService : IDisposable
    {
        private bool _disposed;
        private const string OcrEndpoint = "/api/ocr/registry";

        public RegistryOcrService()
        {
        }

        /// <summary>
        /// 서버가 실행 중인지 확인
        /// </summary>
        public bool IsServerRunning => PythonBackendService.Instance.IsServerRunning;

        /// <summary>
        /// 서버 시작 (PythonBackendService에 위임)
        /// </summary>
        public Task<bool> StartServerAsync(string? exePath = null)
        {
            return PythonBackendService.Instance.EnsureServerRunningAsync();
        }

        /// <summary>
        /// 헬스체크 (PythonBackendService에 위임)
        /// </summary>
        public Task<bool> CheckHealthAsync()
        {
            return PythonBackendService.Instance.CheckHealthAsync();
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
                // 서버가 실행 중인지 확인하고, 아니면 시작
                if (!await PythonBackendService.Instance.EnsureServerRunningAsync())
                {
                    return new OcrResult
                    {
                        Success = false,
                        FileName = fileName,
                        Error = "OCR 서버를 시작할 수 없습니다."
                    };
                }

                progress?.Report(new OcrProgress(fileName, OcrProgressStatus.Processing, 30));

                var httpClient = PythonBackendService.Instance.GetHttpClient();

                // Multipart form data 생성
                using var fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read);
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                content.Add(streamContent, "file", fileName);

                // API 호출
                var response = await httpClient.PostAsync(OcrEndpoint, content, cancellationToken);
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
                // 서버 관리는 PythonBackendService에서 담당
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



