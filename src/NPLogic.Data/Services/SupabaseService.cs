using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NPLogic.Data.Exceptions;

namespace NPLogic.Data.Services
{
    /// <summary>
    /// Supabase 클라이언트 서비스
    /// </summary>
    public class SupabaseService
    {
        private Supabase.Client? _client;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private System.Timers.Timer? _refreshTimer;
        private readonly SessionStorageService _sessionStorage;

        // 동시 갱신 방지용 세마포어
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        // 마지막 세션 체크 시각 (절전 감지용)
        private DateTime _lastSessionCheckTime = DateTime.UtcNow;

        public SupabaseService(string supabaseUrl, string supabaseKey)
        {
            _supabaseUrl = supabaseUrl ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _supabaseKey = supabaseKey ?? throw new ArgumentNullException(nameof(supabaseKey));
            _sessionStorage = new SessionStorageService();
        }

        /// <summary>
        /// Supabase 프로젝트 URL
        /// </summary>
        public string Url => _supabaseUrl;

        /// <summary>
        /// Supabase API 키
        /// </summary>
        public string Key => _supabaseKey;

        /// <summary>
        /// Supabase 클라이언트 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_client != null)
                return;

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            _client = new Supabase.Client(_supabaseUrl, _supabaseKey, options);
            await _client.InitializeAsync();

            // 토큰 자동 갱신 타이머 시작 (50분마다 - JWT는 보통 1시간 만료)
            StartRefreshTimer();

            // 절전/잠금 복귀 이벤트 등록
            RegisterSystemEvents();
        }

        /// <summary>
        /// 절전/잠금 복귀 시 토큰 자동 갱신을 위한 시스템 이벤트 등록
        /// </summary>
        private void RegisterSystemEvents()
        {
            try
            {
                SystemEvents.PowerModeChanged += OnPowerModeChanged;
                SystemEvents.SessionSwitch += OnSessionSwitch;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService] Failed to register system events: {ex.Message}");
            }
        }

        /// <summary>
        /// 절전 모드 복귀 시 토큰 갱신
        /// </summary>
        private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                System.Diagnostics.Debug.WriteLine("[SupabaseService] System resumed from sleep, refreshing token...");
                await ForceRefreshAsync();
            }
        }

        /// <summary>
        /// 화면 잠금 해제 시 토큰 갱신
        /// </summary>
        private async void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                System.Diagnostics.Debug.WriteLine("[SupabaseService] Session unlocked, refreshing token...");
                await ForceRefreshAsync();
            }
        }

        /// <summary>
        /// 강제 토큰 갱신 (절전/잠금 복귀 시 호출)
        /// </summary>
        private async Task ForceRefreshAsync()
        {
            try
            {
                if (_client?.Auth.CurrentSession == null)
                    return;

                var refreshed = await TryRefreshTokenAsync();
                if (refreshed)
                {
                    _lastSessionCheckTime = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine("[SupabaseService] Token refreshed after system resume/unlock");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SupabaseService] Token refresh failed after system resume/unlock");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService] ForceRefresh error: {ex.Message}");
            }
        }

        /// <summary>
        /// 토큰 자동 갱신 타이머 시작
        /// </summary>
        private void StartRefreshTimer()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();

            // 50분마다 토큰 갱신 시도 (JWT는 보통 1시간 만료)
            _refreshTimer = new System.Timers.Timer(50 * 60 * 1000); // 50분
            _refreshTimer.Elapsed += async (s, e) => await TryRefreshTokenAsync();
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }

        /// <summary>
        /// 토큰 수동 갱신 시도 (동시 호출 방지)
        /// </summary>
        public async Task<bool> TryRefreshTokenAsync()
        {
            // 동시 갱신 방지: 이미 갱신 중이면 대기 후 성공 반환
            if (!await _refreshLock.WaitAsync(TimeSpan.FromSeconds(10)))
            {
                System.Diagnostics.Debug.WriteLine("[SupabaseService] Refresh already in progress, skipping");
                return true; // 다른 스레드가 갱신 중이므로 성공으로 간주
            }

            try
            {
                if (_client?.Auth.CurrentSession == null)
                    return false;

                var session = await _client.Auth.RefreshSession();
                if (session != null)
                {
                    // 갱신된 토큰을 세션 저장소에 저장
                    if (session.AccessToken != null && session.RefreshToken != null)
                    {
                        var expiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
                        var email = _client.Auth.CurrentUser?.Email;

                        if (email != null)
                        {
                            _sessionStorage.SaveSession(
                                session.AccessToken,
                                session.RefreshToken,
                                expiresAt,
                                email
                            );
                        }
                    }

                    _lastSessionCheckTime = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine("Token manually refreshed successfully");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Token refresh failed: {ex.Message}");
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// API 호출 전 세션 유효성 확인 및 필요 시 갱신
        /// 토큰 갱신에 실패하면 SessionExpiredException을 발생시킵니다.
        /// </summary>
        /// <param name="throwOnFailure">갱신 실패 시 예외 발생 여부 (기본값: true)</param>
        public async Task EnsureValidSessionAsync(bool throwOnFailure = true)
        {
            if (_client?.Auth.CurrentSession == null)
            {
                if (throwOnFailure)
                    throw new SessionExpiredException("로그인 세션이 없습니다. 다시 로그인해주세요.");
                return;
            }

            // 절전/잠금 감지: 마지막 체크로부터 5분 이상 경과 시 강제 갱신
            // (타이머가 절전 중 밀린 경우를 보완)
            var timeSinceLastCheck = DateTime.UtcNow - _lastSessionCheckTime;
            if (timeSinceLastCheck.TotalMinutes > 5)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService] Gap detected ({timeSinceLastCheck.TotalMinutes:F1} min since last check), force refreshing...");

                var refreshed = await TryRefreshTokenAsync();
                _lastSessionCheckTime = DateTime.UtcNow;

                if (!refreshed && throwOnFailure)
                {
                    _sessionStorage.ClearSession();
                    throw new SessionExpiredException("세션이 만료되었습니다. 다시 로그인해주세요.");
                }
                return;
            }

            // 세션 만료 시간 확인 (여유를 두고 선제 갱신)
            // NOTE: 일부 라이브러리는 ExpiresAt()의 DateTime Kind가 Local/Unspecified일 수 있어
            //       UtcNow로만 비교하면 만료를 감지하지 못하고(PGRST303: JWT expired) 작업 중간에 터질 수 있음.
            var session = _client.Auth.CurrentSession;
            var expiresAt = session.ExpiresAt();

            TimeSpan timeUntilExpiry;
            if (expiresAt.Kind == DateTimeKind.Utc)
            {
                timeUntilExpiry = expiresAt - DateTime.UtcNow;
            }
            else if (expiresAt.Kind == DateTimeKind.Local)
            {
                timeUntilExpiry = expiresAt - DateTime.Now;
            }
            else
            {
                // Kind == Unspecified: Utc 기준/Local 기준 둘 다 계산 후 더 그럴듯한 값을 사용
                var utcDiff = expiresAt - DateTime.UtcNow;
                var localDiff = expiresAt - DateTime.Now;
                timeUntilExpiry = Math.Abs(utcDiff.TotalMinutes) <= Math.Abs(localDiff.TotalMinutes) ? utcDiff : localDiff;
            }

            // 이미 만료되었거나 곧 만료 예정이면 갱신 (DD 업로드 같은 장시간 작업 대비)
            const double refreshThresholdMinutes = 10;
            if (timeUntilExpiry.TotalMinutes < refreshThresholdMinutes)
            {
                System.Diagnostics.Debug.WriteLine($"Session expiring soon ({timeUntilExpiry.TotalMinutes:F1} min), refreshing...");

                var refreshed = await TryRefreshTokenAsync();

                if (!refreshed && throwOnFailure)
                {
                    // 갱신 실패 시 세션 정보 삭제
                    _sessionStorage.ClearSession();
                    throw new SessionExpiredException("세션이 만료되었습니다. 다시 로그인해주세요.");
                }
            }

            _lastSessionCheckTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Supabase 클라이언트 가져오기
        /// </summary>
        public Supabase.Client GetClient()
        {
            if (_client == null)
                throw new InvalidOperationException("Supabase client is not initialized. Call InitializeAsync() first.");

            return _client;
        }

        /// <summary>
        /// Supabase 클라이언트 가져오기 (세션 유효성 확인 후)
        /// 세션이 만료되었고 갱신에 실패하면 SessionExpiredException을 발생시킵니다.
        /// </summary>
        public async Task<Supabase.Client> GetClientAsync()
        {
            if (_client == null)
                throw new InvalidOperationException("Supabase client is not initialized. Call InitializeAsync() first.");

            // 세션 유효성 확인 및 필요 시 갱신 (실패 시 예외 발생)
            await EnsureValidSessionAsync(throwOnFailure: true);
            return _client;
        }

        /// <summary>
        /// 세션 유효성 확인 (예외 없이 결과만 반환)
        /// 앱 포커스 복귀 시 선제적 확인에 사용
        /// </summary>
        /// <returns>세션이 유효하면 true, 만료되었으면 false</returns>
        public async Task<bool> CheckAndRefreshSessionAsync()
        {
            try
            {
                await EnsureValidSessionAsync(throwOnFailure: false);
                return _client?.Auth.CurrentSession != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 현재 사용자 세션 가져오기
        /// </summary>
        public Supabase.Gotrue.Session? GetSession()
        {
            return _client?.Auth.CurrentSession;
        }

        /// <summary>
        /// 현재 사용자 정보 가져오기
        /// </summary>
        public Supabase.Gotrue.User? GetCurrentUser()
        {
            return _client?.Auth.CurrentUser;
        }

        /// <summary>
        /// 인증 상태 확인
        /// </summary>
        public bool IsAuthenticated()
        {
            return _client?.Auth.CurrentUser != null;
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        public async Task SignOutAsync()
        {
            if (_client != null)
            {
                await _client.Auth.SignOut();
            }
        }
    }
}
