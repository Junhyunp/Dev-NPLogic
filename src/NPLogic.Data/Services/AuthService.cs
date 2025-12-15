using System;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;

namespace NPLogic.Services
{
    /// <summary>
    /// 인증 서비스
    /// </summary>
    public class AuthService
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionStorageService _sessionStorage;

        public AuthService(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
            _sessionStorage = new SessionStorageService();
        }

        /// <summary>
        /// 이메일/비밀번호로 회원가입
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage, Supabase.Gotrue.User? User)> SignUpWithEmailAsync(
            string email, 
            string password, 
            string? name = null, 
            string? role = null)
        {
            try
            {
                var client = _supabaseService.GetClient();
                
                // 메타데이터 설정 (트리거에서 사용)
                var options = new Supabase.Gotrue.SignUpOptions
                {
                    Data = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "name", name ?? email.Split('@')[0] },
                        { "role", role ?? "evaluator" }
                    }
                };
                
                var session = await client.Auth.SignUp(email, password, options);

                if (session?.User == null)
                    return (false, "회원가입에 실패했습니다.", null);

                return (true, null, session.User);
            }
            catch (GotrueException ex)
            {
                // 이미 가입된 이메일
                if (ex.Message.Contains("already registered") || ex.Message.Contains("User already registered"))
                {
                    return (false, "이미 가입된 이메일입니다.\n로그인을 시도해주세요.", null);
                }
                
                // 비밀번호 강도 부족
                if (ex.Message.Contains("Password") && ex.Message.Contains("weak"))
                {
                    return (false, "비밀번호는 최소 6자 이상이어야 합니다.\n영문, 숫자, 특수문자를 조합하는 것을 권장합니다.", null);
                }
                
                return (false, $"회원가입 실패: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"회원가입 오류: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 이메일/비밀번호로 로그인
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage, Supabase.Gotrue.User? User)> SignInWithEmailAsync(string email, string password, bool rememberMe = false)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var session = await client.Auth.SignIn(email, password);

                if (session?.User == null)
                    return (false, "로그인에 실패했습니다.", null);

                // 자동 로그인 선택 시 세션 정보 저장
                if (rememberMe && session.AccessToken != null && session.RefreshToken != null)
                {
                    // 만료 시간을 Unix timestamp로 변환 (기본 7일 후)
                    var expiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
                    
                    _sessionStorage.SaveSession(
                        session.AccessToken,
                        session.RefreshToken,
                        expiresAt,
                        email
                    );
                }

                return (true, null, session.User);
            }
            catch (GotrueException ex)
            {
                // 이메일 미인증 에러
                if (ex.Message.Contains("email_not_confirmed") || ex.Message.Contains("Email not confirmed"))
                {
                    return (false, "이메일 인증이 필요합니다.\n가입하신 이메일의 받은편지함을 확인하여\n인증 링크를 클릭해주세요.", null);
                }
                
                // 잘못된 로그인 정보
                if (ex.Message.Contains("Invalid login credentials") || ex.Message.Contains("Invalid"))
                {
                    return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);
                }
                
                return (false, $"로그인 실패: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"로그인 오류: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        public async Task<bool> SignOutAsync()
        {
            try
            {
                await _supabaseService.SignOutAsync();
                
                // 저장된 세션 정보 삭제
                _sessionStorage.ClearSession();
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 인증 상태 확인
        /// </summary>
        public bool IsAuthenticated()
        {
            return _supabaseService.IsAuthenticated();
        }

        /// <summary>
        /// 현재 사용자 세션 가져오기
        /// </summary>
        public Supabase.Gotrue.Session? GetSession()
        {
            return _supabaseService.GetSession();
        }

        /// <summary>
        /// 비밀번호 재설정 이메일 전송
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client.Auth.ResetPasswordForEmail(email);
                return true;
            }
            catch (GotrueException ex)
            {
                System.Diagnostics.Debug.WriteLine($"비밀번호 재설정 이메일 전송 실패: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"비밀번호 재설정 이메일 전송 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 새 사용자 생성 (관리자용)
        /// </summary>
        public async Task<Supabase.Gotrue.User?> CreateUserAsync(string email, string password)
        {
            var result = await SignUpWithEmailAsync(email, password);
            if (result.Success && result.User != null)
            {
                return result.User;
            }
            throw new Exception(result.ErrorMessage ?? "사용자 생성에 실패했습니다.");
        }

        /// <summary>
        /// 저장된 세션으로 자동 로그인 시도
        /// </summary>
        public async Task<bool> TryAutoSignInAsync()
        {
            try
            {
                // 저장된 세션 로드
                var sessionData = _sessionStorage.LoadSession();
                
                if (sessionData == null)
                    return false;

                // 세션 유효성 확인
                if (!_sessionStorage.IsSessionValid(sessionData))
                {
                    // 만료된 세션 삭제
                    _sessionStorage.ClearSession();
                    return false;
                }

                // 저장된 토큰으로 세션 복원
                var client = _supabaseService.GetClient();
                
                // Supabase 클라이언트에 세션 설정
                if (sessionData.AccessToken != null && sessionData.RefreshToken != null)
                {
                    var session = await client.Auth.SetSession(sessionData.AccessToken, sessionData.RefreshToken);
                    
                    if (session?.User != null)
                    {
                        // 세션 복원 성공 - 새로운 토큰으로 업데이트 (7일 후 만료)
                        if (session.AccessToken != null && session.RefreshToken != null && sessionData.Email != null)
                        {
                            var expiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
                            
                            _sessionStorage.SaveSession(
                                session.AccessToken,
                                session.RefreshToken,
                                expiresAt,
                                sessionData.Email
                            );
                        }
                        return true;
                    }
                }

                // 세션 복원 실패 - 저장된 정보 삭제
                _sessionStorage.ClearSession();
                return false;
            }
            catch (Exception ex)
            {
                // 자동 로그인 실패 - 저장된 정보 삭제
                System.Diagnostics.Debug.WriteLine($"자동 로그인 실패: {ex.Message}");
                _sessionStorage.ClearSession();
                return false;
            }
        }
    }
}

