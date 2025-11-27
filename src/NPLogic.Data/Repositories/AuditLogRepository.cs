using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 작업 이력 Repository
    /// </summary>
    public class AuditLogRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public AuditLogRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 전체 이력 조회
        /// </summary>
        public async Task<List<AuditLog>> GetAllAsync(int limit = 100)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<AuditLogTable>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Limit(limit)
                    .Get();

                return response.Models.Select(MapToAuditLog).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"작업 이력 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 필터링된 이력 조회
        /// </summary>
        public async Task<List<AuditLog>> GetFilteredAsync(
            string? tableName = null,
            string? action = null,
            Guid? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int limit = 100)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var query = client.From<AuditLogTable>();

                if (!string.IsNullOrEmpty(tableName) && tableName != "전체")
                {
                    query = query.Where(x => x.TableName == tableName);
                }

                if (!string.IsNullOrEmpty(action) && action != "전체")
                {
                    query = query.Where(x => x.Action == action);
                }

                if (userId.HasValue)
                {
                    query = query.Where(x => x.UserId == userId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(x => x.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(x => x.CreatedAt <= endDate.Value.AddDays(1));
                }

                var response = await query
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Limit(limit)
                    .Get();

                return response.Models.Select(MapToAuditLog).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"작업 이력 필터링 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 특정 레코드의 이력 조회
        /// </summary>
        public async Task<List<AuditLog>> GetByRecordIdAsync(Guid recordId)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<AuditLogTable>()
                    .Where(x => x.RecordId == recordId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAuditLog).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"레코드 이력 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 특정 사용자의 이력 조회
        /// </summary>
        public async Task<List<AuditLog>> GetByUserIdAsync(Guid userId, int limit = 100)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<AuditLogTable>()
                    .Where(x => x.UserId == userId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Limit(limit)
                    .Get();

                return response.Models.Select(MapToAuditLog).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자 이력 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 이력 생성 (로깅용)
        /// </summary>
        public async Task<AuditLog> CreateAsync(AuditLog log)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToAuditLogTable(log);
                table.CreatedAt = DateTime.UtcNow;

                var response = await client.From<AuditLogTable>().Insert(table);
                return MapToAuditLog(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"작업 이력 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 테이블 목록 조회 (필터용)
        /// </summary>
        public async Task<List<string>> GetTableNamesAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<AuditLogTable>()
                    .Select("table_name")
                    .Get();

                return response.Models
                    .Select(x => x.TableName ?? "")
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"테이블 목록 조회 실패: {ex.Message}", ex);
            }
        }

        // ========== Mapper ==========

        private AuditLog MapToAuditLog(AuditLogTable t) => new AuditLog
        {
            Id = t.Id,
            TableName = t.TableName,
            RecordId = t.RecordId,
            Action = t.Action,
            OldData = t.OldData,
            NewData = t.NewData,
            UserId = t.UserId,
            UserEmail = t.UserEmail,
            IpAddress = t.IpAddress,
            UserAgent = t.UserAgent,
            CreatedAt = t.CreatedAt
        };

        private AuditLogTable MapToAuditLogTable(AuditLog a) => new AuditLogTable
        {
            Id = a.Id,
            TableName = a.TableName,
            RecordId = a.RecordId,
            Action = a.Action,
            OldData = a.OldData,
            NewData = a.NewData,
            UserId = a.UserId,
            UserEmail = a.UserEmail,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent,
            CreatedAt = a.CreatedAt
        };
    }

    // ========== Table Class ==========

    [Postgrest.Attributes.Table("audit_logs")]
    internal class AuditLogTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("table_name")] public string? TableName { get; set; }
        [Postgrest.Attributes.Column("record_id")] public Guid? RecordId { get; set; }
        [Postgrest.Attributes.Column("action")] public string? Action { get; set; }
        [Postgrest.Attributes.Column("old_data")] public string? OldData { get; set; }
        [Postgrest.Attributes.Column("new_data")] public string? NewData { get; set; }
        [Postgrest.Attributes.Column("user_id")] public Guid? UserId { get; set; }
        [Postgrest.Attributes.Column("user_email")] public string? UserEmail { get; set; }
        [Postgrest.Attributes.Column("ip_address")] public string? IpAddress { get; set; }
        [Postgrest.Attributes.Column("user_agent")] public string? UserAgent { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}

