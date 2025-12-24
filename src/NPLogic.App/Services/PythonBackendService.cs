using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// Python 백엔드 서버 관리 서비스
    /// OCR, 추천 등 Python 기능을 제공하는 FastAPI 서버를 관리합니다.
    /// Singleton으로 앱 전체에서 공유되며, on-demand로 서버를 시작합니다.
    /// </summary>
    public class PythonBackendService : IDisposable
    {
        private static PythonBackendService? _instance;
        private static readonly object _lock = new();

        private readonly HttpClient _httpClient;
        private Process? _serverProcess;
        private bool _disposed;
        private bool _isStarting;

        private const string DefaultServerUrl = "http://localhost:8000";
        private const string HealthEndpoint = "/api/health";
        private const string ServerExeName = "nplogic_backend.exe";
        private const string PythonServerScript = "server.py";
        private const int MaxStartupWaitSeconds = 30;
        private const int HealthCheckIntervalMs = 500;

        public string ServerUrl { get; }
        public bool IsServerRunning { get; private set; }

        /// <summary>
        /// Singleton 인스턴스 가져오기
        /// </summary>
        public static PythonBackendService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PythonBackendService();
                    }
                }
                return _instance;
            }
        }

        private PythonBackendService(string? serverUrl = null)
        {
            ServerUrl = serverUrl ?? DefaultServerUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ServerUrl),
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        /// <summary>
        /// 서버가 실행 중인지 확인하고, 아니면 시작합니다.
        /// </summary>
        /// <returns>서버 준비 완료 여부</returns>
        public async Task<bool> EnsureServerRunningAsync()
        {
            // 이미 실행 중이면 바로 반환
            if (await CheckHealthAsync())
            {
                IsServerRunning = true;
                return true;
            }

            // 다른 스레드가 이미 시작 중이면 대기
            if (_isStarting)
            {
                return await WaitForServerReadyAsync(MaxStartupWaitSeconds);
            }

            try
            {
                _isStarting = true;
                return await StartServerInternalAsync();
            }
            finally
            {
                _isStarting = false;
            }
        }

        /// <summary>
        /// 서버 시작 내부 로직
        /// </summary>
        private async Task<bool> StartServerInternalAsync()
        {
            // 1. exe 파일 먼저 찾기 (패키징 환경)
            var exePath = FindServerExePath();

            // 2. exe 없으면 Python 스크립트 찾기 (개발 환경)
            var pythonScriptPath = FindPythonScriptPath();

            if (string.IsNullOrEmpty(exePath) && string.IsNullOrEmpty(pythonScriptPath))
            {
                throw new FileNotFoundException(
                    "Python 백엔드 서버를 찾을 수 없습니다. exe 파일 또는 Python 스크립트가 필요합니다.");
            }

            try
            {
                ProcessStartInfo startInfo;

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    // exe 파일로 실행 (패키징 환경)
                    startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    Debug.WriteLine($"[PythonBackendService] Starting exe: {exePath}");
                }
                else if (!string.IsNullOrEmpty(pythonScriptPath))
                {
                    // Python 스크립트로 실행 (개발 환경)
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"\"{pythonScriptPath}\"",
                        WorkingDirectory = Path.GetDirectoryName(pythonScriptPath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    Debug.WriteLine($"[PythonBackendService] Starting python script: {pythonScriptPath}");
                }
                else
                {
                    throw new FileNotFoundException("Python 백엔드 서버를 시작할 수 없습니다.");
                }

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
                        $"Python 백엔드 서버가 {MaxStartupWaitSeconds}초 내에 시작되지 않았습니다.");
                }

                IsServerRunning = true;
                Debug.WriteLine("[PythonBackendService] Server started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PythonBackendService] Failed to start server: {ex.Message}");
                StopServer();
                throw new Exception($"Python 백엔드 서버 시작 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 서버 실행 파일 경로를 찾습니다. (패키징 환경용)
        /// </summary>
        private string? FindServerExePath()
        {
            var searchPaths = new[]
            {
                // 1. 앱 실행 디렉토리의 backend 폴더
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "nplogic_backend", ServerExeName),
                
                // 2. 앱 실행 디렉토리의 backend 폴더 (registry_ocr 호환)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "registry_ocr", "registry_ocr.exe"),
                
                // 3. 앱 실행 디렉토리
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ServerExeName),
            };

            foreach (var path in searchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    Debug.WriteLine($"[PythonBackendService] Found exe at: {fullPath}");
                    return fullPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Python 서버 스크립트 경로를 찾습니다. (개발 환경용)
        /// </summary>
        private string? FindPythonScriptPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            var searchPaths = new[]
            {
                // 개발 환경: bin/Debug/net8.0-windows/ 에서 프로젝트 루트로
                // src/NPLogic.App/bin/Debug/net8.0-windows/ -> 프로젝트 루트
                Path.Combine(baseDir, "..", "..", "..", "..", "..", 
                    "manager", "client", "Auction-Certificate", PythonServerScript),
            };

            foreach (var path in searchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                Debug.WriteLine($"[PythonBackendService] Checking python script at: {fullPath}");
                if (File.Exists(fullPath))
                {
                    Debug.WriteLine($"[PythonBackendService] Found python script at: {fullPath}");
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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.GetAsync(HealthEndpoint, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var health = JsonSerializer.Deserialize<BackendHealthResponse>(content);
                    return health?.Status == "ok";
                }
            }
            catch
            {
                // 연결 실패는 무시
            }

            return false;
        }

        /// <summary>
        /// 서버를 중지합니다.
        /// </summary>
        public void StopServer()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    Debug.WriteLine("[PythonBackendService] Stopping server...");
                    _serverProcess.Kill(entireProcessTree: true);
                    _serverProcess.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PythonBackendService] Error stopping server: {ex.Message}");
            }
            finally
            {
                _serverProcess?.Dispose();
                _serverProcess = null;
                IsServerRunning = false;
            }
        }

        /// <summary>
        /// HttpClient 인스턴스를 반환합니다. (OCR, Recommend 서비스에서 사용)
        /// </summary>
        public HttpClient GetHttpClient() => _httpClient;

        public void Dispose()
        {
            if (!_disposed)
            {
                StopServer();
                _httpClient.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// 앱 종료 시 정리
        /// </summary>
        public static void Shutdown()
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    /// <summary>
    /// 백엔드 헬스체크 응답
    /// </summary>
    internal class BackendHealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}



