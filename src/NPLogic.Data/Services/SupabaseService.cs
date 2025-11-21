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

        public SupabaseService(string supabaseUrl, string supabaseKey)
        {
            _supabaseUrl = supabaseUrl ?? throw new ArgumentNullException(nameof(supabaseUrl));
            _supabaseKey = supabaseKey ?? throw new ArgumentNullException(nameof(supabaseKey));
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

