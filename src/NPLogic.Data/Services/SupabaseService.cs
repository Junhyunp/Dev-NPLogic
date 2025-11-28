using System;
using System.Threading.Tasks;

namespace NPLogic.Services
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

        public SupabaseService(string supabaseUrl, string supabaseKey)
        {
            _supabaseUrl = supabaseUrl ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _supabaseKey = supabaseKey ?? throw new ArgumentNullException(nameof(supabaseKey));
            _sessionStorage = new SessionStorageService();
        }

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
        /// 토큰 수동 갱신 시도
        /// </summary>
        public async Task<bool> TryRefreshTokenAsync()
        {
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
        }

        /// <summary>
        /// API 호출 전 세션 유효성 확인 및 필요 시 갱신
        /// </summary>
        public async Task EnsureValidSessionAsync()
        {
            if (_client?.Auth.CurrentSession == null)
                return;

            // 세션 만료 시간 확인 (5분 여유)
            var session = _client.Auth.CurrentSession;
            var expiresAt = session.ExpiresAt();
            var timeUntilExpiry = expiresAt - DateTime.UtcNow;

            if (timeUntilExpiry.TotalMinutes < 5)
            {
                System.Diagnostics.Debug.WriteLine($"Session expiring soon ({timeUntilExpiry.TotalMinutes:F1} min), refreshing...");
                await TryRefreshTokenAsync();
            }
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
        /// </summary>
        public async Task<Supabase.Client> GetClientAsync()
        {
            if (_client == null)
                throw new InvalidOperationException("Supabase client is not initialized. Call InitializeAsync() first.");

            await EnsureValidSessionAsync();
            return _client;
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

