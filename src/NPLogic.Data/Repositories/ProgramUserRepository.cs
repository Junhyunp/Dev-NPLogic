using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 프로그램-사용자 매핑 Repository
    /// </summary>
    public class ProgramUserRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public ProgramUserRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 프로그램의 모든 사용자 매핑 조회
        /// </summary>
        public async Task<List<ProgramUser>> GetByProgramIdAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramUserTable>()
                    .Where(x => x.ProgramId == programId)
                    .Get();

                return response.Models.Select(MapToProgramUser).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 사용자 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자의 모든 프로그램 매핑 조회
        /// </summary>
        public async Task<List<ProgramUser>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramUserTable>()
                    .Where(x => x.UserId == userId)
                    .Get();

                return response.Models.Select(MapToProgramUser).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 프로그램 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램의 PM 조회
        /// </summary>
        public async Task<ProgramUser?> GetProgramPMAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramUserTable>()
                    .Where(x => x.ProgramId == programId && x.Role == "pm")
                    .Get();

                var model = response.Models.FirstOrDefault();
                return model == null ? null : MapToProgramUser(model);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 PM 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자가 PM인 프로그램 목록 조회
        /// </summary>
        public async Task<List<ProgramUser>> GetUserPMProgramsAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramUserTable>()
                    .Where(x => x.UserId == userId && x.Role == "pm")
                    .Get();

                return response.Models.Select(MapToProgramUser).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 PM 프로그램 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램-사용자 매핑 생성
        /// </summary>
        public async Task<ProgramUser> CreateAsync(ProgramUser programUser)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToProgramUserTable(programUser);
                table.Id = Guid.NewGuid();
                table.CreatedAt = DateTime.UtcNow;

                var response = await client
                    .From<ProgramUserTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("프로그램-사용자 매핑 생성 후 데이터 조회 실패");

                return MapToProgramUser(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램-사용자 매핑 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램에 PM 할당
        /// </summary>
        public async Task<ProgramUser> AssignPMAsync(Guid programId, Guid userId)
        {
            try
            {
                // 기존 PM이 있으면 삭제
                var existingPM = await GetProgramPMAsync(programId);
                if (existingPM != null)
                {
                    await DeleteAsync(existingPM.Id);
                }

                // 새 PM 할당
                return await CreateAsync(new ProgramUser
                {
                    ProgramId = programId,
                    UserId = userId,
                    Role = "pm"
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"PM 할당 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램에 팀원 추가
        /// </summary>
        public async Task<ProgramUser> AddMemberAsync(Guid programId, Guid userId)
        {
            return await CreateAsync(new ProgramUser
            {
                ProgramId = programId,
                UserId = userId,
                Role = "member"
            });
        }

        /// <summary>
        /// 프로그램-사용자 매핑 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<ProgramUserTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램-사용자 매핑 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램에서 사용자 제거
        /// </summary>
        public async Task<bool> RemoveUserFromProgramAsync(Guid programId, Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<ProgramUserTable>()
                    .Where(x => x.ProgramId == programId && x.UserId == userId)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램에서 사용자 제거 실패: {ex.Message}", ex);
            }
        }

        private ProgramUser MapToProgramUser(ProgramUserTable table)
        {
            return new ProgramUser
            {
                Id = table.Id,
                ProgramId = table.ProgramId,
                UserId = table.UserId,
                Role = table.Role ?? "member",
                CreatedAt = table.CreatedAt
            };
        }

        private ProgramUserTable MapToProgramUserTable(ProgramUser programUser)
        {
            return new ProgramUserTable
            {
                Id = programUser.Id,
                ProgramId = programUser.ProgramId,
                UserId = programUser.UserId,
                Role = programUser.Role,
                CreatedAt = programUser.CreatedAt
            };
        }
    }

    /// <summary>
    /// Supabase program_users 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("program_users")]
    internal class ProgramUserTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("program_id")]
        public Guid ProgramId { get; set; }

        [Postgrest.Attributes.Column("user_id")]
        public Guid UserId { get; set; }

        [Postgrest.Attributes.Column("role")]
        public string? Role { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}

