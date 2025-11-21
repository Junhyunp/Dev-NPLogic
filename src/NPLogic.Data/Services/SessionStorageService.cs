using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NPLogic.Services
{
    /// <summary>
    /// 세션 정보 로컬 저장 서비스
    /// Windows DPAPI를 사용하여 세션 정보를 암호화하여 저장
    /// </summary>
    public class SessionStorageService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NPLogic"
        );
        private static readonly string SessionFilePath = Path.Combine(AppDataFolder, "session.dat");

        /// <summary>
        /// 세션 정보 클래스
        /// </summary>
        public class SessionData
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public long ExpiresAt { get; set; }
            public string? Email { get; set; }
        }

        /// <summary>
        /// 세션 정보를 암호화하여 저장
        /// </summary>
        public bool SaveSession(string accessToken, string refreshToken, long expiresAt, string email)
        {
            try
            {
                // 폴더가 없으면 생성
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var sessionData = new SessionData
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    Email = email
                };

                // JSON으로 직렬화
                var json = JsonSerializer.Serialize(sessionData);
                var bytes = Encoding.UTF8.GetBytes(json);

                // Windows DPAPI로 암호화 (현재 사용자만 복호화 가능)
                var encryptedBytes = ProtectedData.Protect(
                    bytes,
                    null,
                    DataProtectionScope.CurrentUser
                );

                // 파일로 저장
                File.WriteAllBytes(SessionFilePath, encryptedBytes);
                return true;
            }
            catch (Exception ex)
            {
                // 로깅 (나중에 Serilog로 교체)
                System.Diagnostics.Debug.WriteLine($"세션 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 저장된 세션 정보를 복호화하여 로드
        /// </summary>
        public SessionData? LoadSession()
        {
            try
            {
                // 파일이 없으면 null 반환
                if (!File.Exists(SessionFilePath))
                    return null;

                // 파일 읽기
                var encryptedBytes = File.ReadAllBytes(SessionFilePath);

                // 복호화
                var bytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser
                );

                // JSON 역직렬화
                var json = Encoding.UTF8.GetString(bytes);
                var sessionData = JsonSerializer.Deserialize<SessionData>(json);

                return sessionData;
            }
            catch (Exception ex)
            {
                // 파일이 손상되었거나 복호화 실패
                System.Diagnostics.Debug.WriteLine($"세션 로드 실패: {ex.Message}");
                
                // 손상된 파일 삭제
                try
                {
                    if (File.Exists(SessionFilePath))
                        File.Delete(SessionFilePath);
                }
                catch { }

                return null;
            }
        }

        /// <summary>
        /// 저장된 세션 정보 삭제
        /// </summary>
        public bool ClearSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"세션 삭제 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션이 유효한지 확인 (만료 시간 체크)
        /// </summary>
        public bool IsSessionValid(SessionData session)
        {
            if (session == null)
                return false;

            // 현재 시간 (Unix timestamp, 초 단위)
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // 만료 시간보다 현재 시간이 작으면 유효
            return currentTime < session.ExpiresAt;
        }
    }
}

