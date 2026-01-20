using System;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.Services
{
    /// <summary>
    /// 권한 관리 서비스
    /// - 삭제 권한: Admin 또는 해당 프로그램의 PM만 가능
    /// - 수정 권한: Admin 또는 해당 프로그램의 PM만 가능
    /// - 조회 권한: 본인 소속 회계법인 데이터만 (Admin은 전체)
    /// </summary>
    public class PermissionService
    {
        private readonly ProgramUserRepository _programUserRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        public PermissionService(
            ProgramUserRepository programUserRepository,
            UserRepository userRepository,
            AuthService authService)
        {
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// 현재 로그인한 사용자 가져오기
        /// </summary>
        public async Task<User?> GetCurrentUserAsync()
        {
            var authUser = _authService.GetSession()?.User;
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
            {
                return await _userRepository.GetByAuthUserIdAsync(Guid.Parse(authUser.Id));
            }
            return null;
        }

        /// <summary>
        /// 해당 프로그램에서 인차지(PM)인지 확인
        /// </summary>
        public async Task<bool> IsInChargeAsync(User user, Guid programId)
        {
            if (user == null) return false;
            
            // Admin은 모든 프로그램의 인차지로 간주
            if (user.IsAdmin) return true;

            try
            {
                var programPM = await _programUserRepository.GetProgramPMAsync(programId);
                return programPM != null && programPM.UserId == user.Id;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 삭제 권한 확인
        /// - Admin은 모든 삭제 가능
        /// - PM은 본인이 담당하는 프로그램의 데이터만 삭제 가능
        /// </summary>
        public async Task<bool> CanDeleteAsync(Guid programId, User? currentUser)
        {
            if (currentUser == null) return false;
            
            // Admin은 모든 삭제 가능
            if (currentUser.IsAdmin) return true;

            // PM이 아닌 경우 삭제 불가
            if (!currentUser.IsPM) return false;

            // 해당 프로그램의 PM인 경우만 삭제 가능
            return await IsInChargeAsync(currentUser, programId);
        }

        /// <summary>
        /// 수정 권한 확인
        /// - Admin은 모든 수정 가능
        /// - PM은 본인이 담당하는 프로그램의 데이터만 수정 가능
        /// </summary>
        public async Task<bool> CanEditAsync(Guid programId, User? currentUser)
        {
            if (currentUser == null) return false;

            // Admin은 모든 수정 가능
            if (currentUser.IsAdmin) return true;

            // PM이 아닌 경우 수정 불가
            if (!currentUser.IsPM) return false;

            // 해당 프로그램의 PM인 경우만 수정 가능
            return await IsInChargeAsync(currentUser, programId);
        }

        /// <summary>
        /// 조회 권한 확인 (회계법인 기반)
        /// - Admin은 모든 데이터 조회 가능
        /// - PM/Evaluator는 본인 소속 회계법인 데이터만 조회
        /// </summary>
        public bool CanView(string? programAccountingFirm, User? currentUser)
        {
            if (currentUser == null) return false;

            // Admin은 모든 데이터 조회 가능
            if (currentUser.IsAdmin) return true;

            // 회계법인이 설정되지 않은 경우 조회 가능
            if (string.IsNullOrEmpty(currentUser.AccountingFirm)) return true;
            if (string.IsNullOrEmpty(programAccountingFirm)) return true;

            // 동일한 회계법인만 조회 가능
            return string.Equals(currentUser.AccountingFirm, programAccountingFirm, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 사용자가 해당 프로그램에 접근 가능한지 확인
        /// </summary>
        public async Task<bool> CanAccessProgramAsync(Guid programId, User? currentUser)
        {
            if (currentUser == null) return false;

            // Admin은 모든 프로그램 접근 가능
            if (currentUser.IsAdmin) return true;

            try
            {
                // 프로그램-사용자 매핑 확인
                var userPrograms = await _programUserRepository.GetByUserIdAsync(currentUser.Id);
                return userPrograms.Exists(p => p.ProgramId == programId);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 권한 없음 에러 메시지 생성
        /// </summary>
        public static string GetNoPermissionMessage(string action)
        {
            return action switch
            {
                "delete" => "삭제 권한이 없습니다. 해당 프로그램의 PM만 삭제할 수 있습니다.",
                "edit" => "수정 권한이 없습니다. 해당 프로그램의 PM만 수정할 수 있습니다.",
                "view" => "조회 권한이 없습니다. 본인 소속 회계법인 데이터만 조회할 수 있습니다.",
                _ => "권한이 없습니다."
            };
        }
    }
}
