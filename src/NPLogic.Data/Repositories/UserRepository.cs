using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 사용자 Repository
    /// </summary>
    public class UserRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public UserRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 모든 사용자 조회
        /// </summary>
        public async Task<List<User>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<UserTable>()
                    .Get();

                return response.Models.Select(MapToUser).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 사용자 조회
        /// </summary>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<UserTable>()
                    .Where(x => x.Id == id)
                    .Get();

                var model = response.Models.FirstOrDefault();
                return model == null ? null : MapToUser(model);
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Auth User ID로 사용자 조회
        /// </summary>
        public async Task<User?> GetByAuthUserIdAsync(Guid authUserId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<UserTable>()
                    .Where(x => x.AuthUserId == authUserId)
                    .Get();

                var model = response.Models.FirstOrDefault();
                return model == null ? null : MapToUser(model);
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 이메일로 사용자 조회
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<UserTable>()
                    .Where(x => x.Email == email)
                    .Get();

                var model = response.Models.FirstOrDefault();
                return model == null ? null : MapToUser(model);
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 역할별 사용자 조회
        /// </summary>
        public async Task<List<User>> GetByRoleAsync(string role)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<UserTable>()
                    .Where(x => x.Role == role)
                    .Get();

                return response.Models.Select(MapToUser).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"역할별 사용자 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자 생성
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var userTable = MapToUserTable(user);
                userTable.CreatedAt = DateTime.UtcNow;
                userTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<UserTable>()
                    .Insert(userTable);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("사용자 생성 후 데이터 조회 실패");

                return MapToUser(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자 수정
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var userTable = MapToUserTable(user);
                userTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<UserTable>()
                    .Where(x => x.Id == user.Id)
                    .Update(userTable);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("사용자 수정 후 데이터 조회 실패");

                return MapToUser(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<UserTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자 상태 변경
        /// </summary>
        public async Task<bool> UpdateStatusAsync(Guid id, string status)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var update = new UserTable { Status = status, UpdatedAt = DateTime.UtcNow };
                
                await client
                    .From<UserTable>()
                    .Where(x => x.Id == id)
                    .Update(update);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 상태 변경 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권한 확인 - 관리자 또는 PM
        /// </summary>
        public bool CanManageUsers(User user)
        {
            return user.IsAdmin || user.IsPM;
        }

        /// <summary>
        /// 권한 확인 - 물건 접근 가능
        /// </summary>
        public bool CanAccessProperty(User user, Guid? assignedUserId)
        {
            // 관리자와 PM은 모든 물건 접근 가능
            if (user.IsAdmin || user.IsPM)
                return true;

            // 평가자는 자신에게 할당된 물건만 접근 가능
            if (user.IsEvaluator)
                return assignedUserId == user.Id;

            return false;
        }

        /// <summary>
        /// 활성 사용자 목록 조회
        /// </summary>
        public async Task<List<User>> GetActiveUsersAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<UserTable>()
                    .Where(x => x.Status == "active")
                    .Get();

                return response.Models.Select(MapToUser).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"활성 사용자 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// UserTable -> User 매핑
        /// </summary>
        private User MapToUser(UserTable table)
        {
            return new User
            {
                Id = table.Id,
                AuthUserId = table.AuthUserId,
                Email = table.Email ?? string.Empty,
                Name = table.Name ?? string.Empty,
                Role = table.Role ?? string.Empty,
                Status = table.Status ?? "active",
                Team = table.Team,
                Position = table.Position,
                AccountingFirm = table.AccountingFirm,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        /// <summary>
        /// User -> UserTable 매핑
        /// </summary>
        private UserTable MapToUserTable(User user)
        {
            return new UserTable
            {
                Id = user.Id,
                AuthUserId = user.AuthUserId,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                Status = user.Status,
                Team = user.Team,
                Position = user.Position,
                AccountingFirm = user.AccountingFirm,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }

    /// <summary>
    /// Supabase users 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("users")]
    internal class UserTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("auth_user_id")]
        public Guid? AuthUserId { get; set; }

        [Postgrest.Attributes.Column("email")]
        public string? Email { get; set; }

        [Postgrest.Attributes.Column("name")]
        public string? Name { get; set; }

        [Postgrest.Attributes.Column("role")]
        public string? Role { get; set; }

        [Postgrest.Attributes.Column("status")]
        public string? Status { get; set; }

        [Postgrest.Attributes.Column("team")]
        public string? Team { get; set; }

        [Postgrest.Attributes.Column("position")]
        public string? Position { get; set; }

        [Postgrest.Attributes.Column("accounting_firm")]
        public string? AccountingFirm { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

