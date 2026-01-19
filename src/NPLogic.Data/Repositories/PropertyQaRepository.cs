using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 물건 QA Repository
    /// </summary>
    public class PropertyQaRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public PropertyQaRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 물건별 QA 목록 조회
        /// </summary>
        public async Task<List<PropertyQa>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyQaTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"QA 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// QA 생성
        /// </summary>
        public async Task<PropertyQa> CreateAsync(PropertyQa qa)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(qa);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<PropertyQaTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("QA 생성 후 데이터 조회 실패");

                return MapToModel(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"QA 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// QA 수정 (답변 등)
        /// </summary>
        public async Task<PropertyQa> UpdateAsync(PropertyQa qa)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(qa);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<PropertyQaTable>()
                    .Where(x => x.Id == qa.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("QA 수정 후 데이터 조회 실패");

                return MapToModel(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"QA 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// QA 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<PropertyQaTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"QA 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주별 QA 목록 조회 (Q-003)
        /// 차주와 연결된 모든 물건의 QA를 가져옴
        /// </summary>
        public async Task<List<PropertyQa>> GetByBorrowerIdAsync(Guid borrowerId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // 1. 차주의 모든 물건 ID 조회
                var propertiesResponse = await client
                    .From<PropertyForQaTable>()
                    .Where(x => x.BorrowerId == borrowerId)
                    .Select("id, property_number, address_full")
                    .Get();
                
                var propertyIds = propertiesResponse.Models.Select(p => p.Id).ToList();
                if (!propertyIds.Any()) return new List<PropertyQa>();

                // 2. 해당 물건들의 QA 조회
                var response = await client
                    .From<PropertyQaTable>()
                    .Filter("property_id", Postgrest.Constants.Operator.In, propertyIds)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                var qas = response.Models.Select(MapToModel).ToList();
                
                // 물건 정보 매핑
                foreach (var qa in qas)
                {
                    var prop = propertiesResponse.Models.FirstOrDefault(p => p.Id == qa.PropertyId);
                    if (prop != null)
                    {
                        qa.PropertyNumber = prop.PropertyNumber;
                        qa.AddressFull = prop.AddressFull;
                    }
                }

                return qas;
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 QA 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 전체 QA 목록 조회
        /// </summary>
        public async Task<List<PropertyQa>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyQaTable>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"QA 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주번호로 QA 목록 조회
        /// </summary>
        public async Task<List<PropertyQa>> GetByBorrowerNumberAsync(string borrowerNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyQaTable>()
                    .Where(x => x.BorrowerNumber == borrowerNumber)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"차주별 QA 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주별 QA 요약 정보 조회
        /// </summary>
        public async Task<List<BorrowerQaSummary>> GetBorrowerQaSummariesAsync()
        {
            try
            {
                var allQas = await GetAllAsync();

                var summaries = allQas
                    .Where(q => !string.IsNullOrEmpty(q.BorrowerNumber))
                    .GroupBy(q => new { q.BorrowerNumber, q.BorrowerName })
                    .Select(g => new BorrowerQaSummary
                    {
                        BorrowerNumber = g.Key.BorrowerNumber ?? "",
                        BorrowerName = g.Key.BorrowerName ?? "",
                        TotalCount = g.Count(),
                        AnsweredCount = g.Count(q => q.IsAnswered),
                        UnansweredCount = g.Count(q => !q.IsAnswered),
                        LastQuestionAt = g.Max(q => q.CreatedAt),
                        LastAnswerAt = g.Where(q => q.AnsweredAt.HasValue).Max(q => q.AnsweredAt)
                    })
                    .OrderBy(s => s.BorrowerNumber)
                    .ToList();

                return summaries;
            }
            catch (Exception ex)
            {
                throw new Exception($"차주별 QA 요약 조회 실패: {ex.Message}", ex);
            }
        }

        private PropertyQa MapToModel(PropertyQaTable table)
        {
            return new PropertyQa
            {
                Id = table.Id,
                PropertyId = table.PropertyId,
                Question = table.Question ?? "",
                Answer = table.Answer,
                Part = table.Part,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                CreatedBy = table.CreatedBy,
                AnsweredBy = table.AnsweredBy,
                CreatedAt = table.CreatedAt ?? DateTime.UtcNow,
                AnsweredAt = table.AnsweredAt,
                UpdatedAt = table.UpdatedAt ?? DateTime.UtcNow
            };
        }

        private PropertyQaTable MapToTable(PropertyQa model)
        {
            return new PropertyQaTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                Question = model.Question,
                Answer = model.Answer,
                Part = model.Part,
                BorrowerNumber = model.BorrowerNumber,
                BorrowerName = model.BorrowerName,
                CreatedBy = model.CreatedBy,
                AnsweredBy = model.AnsweredBy,
                CreatedAt = model.CreatedAt,
                AnsweredAt = model.AnsweredAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }

    [Postgrest.Attributes.Table("property_qa")]
    internal class PropertyQaTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid PropertyId { get; set; }

        [Postgrest.Attributes.Column("question")]
        public string? Question { get; set; }

        [Postgrest.Attributes.Column("answer")]
        public string? Answer { get; set; }

        [Postgrest.Attributes.Column("part")]
        public string? Part { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Postgrest.Attributes.Column("answered_by")]
        public Guid? AnsweredBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Postgrest.Attributes.Column("answered_at")]
        public DateTime? AnsweredAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // QA 조인용 간소화된 Property 테이블
    [Postgrest.Attributes.Table("properties")]
    internal class PropertyForQaTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("borrower_id")] public Guid? BorrowerId { get; set; }
        [Postgrest.Attributes.Column("property_number")] public string? PropertyNumber { get; set; }
        [Postgrest.Attributes.Column("address_full")] public string? AddressFull { get; set; }
    }

    /// <summary>
    /// QA 알림 Repository
    /// 피드백 섹션 18: 알림 기능 - 답변 도착 시 팝업 알람
    /// </summary>
    public class QaNotificationRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public QaNotificationRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 사용자의 미읽은 알림 조회
        /// </summary>
        public async Task<List<QaNotification>> GetUnreadByUserIdAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<QaNotificationTable>()
                    .Where(x => x.UserId == userId)
                    .Where(x => x.IsRead == false)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToNotification).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"알림 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 알림 생성
        /// </summary>
        public async Task<QaNotification> CreateAsync(QaNotification notification)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(notification);
                table.Id = Guid.NewGuid();
                table.CreatedAt = DateTime.UtcNow;

                var response = await client
                    .From<QaNotificationTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("알림 생성 후 데이터 조회 실패");

                return MapToNotification(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"알림 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 알림 읽음 처리
        /// </summary>
        public async Task MarkAsReadAsync(Guid notificationId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<QaNotificationTable>()
                    .Where(x => x.Id == notificationId)
                    .Set(x => x.IsRead, true)
                    .Set(x => x.ReadAt!, DateTime.UtcNow)
                    .Update();
            }
            catch (Exception ex)
            {
                throw new Exception($"알림 읽음 처리 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자의 모든 알림 읽음 처리
        /// </summary>
        public async Task MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<QaNotificationTable>()
                    .Where(x => x.UserId == userId)
                    .Where(x => x.IsRead == false)
                    .Set(x => x.IsRead, true)
                    .Set(x => x.ReadAt!, DateTime.UtcNow)
                    .Update();
            }
            catch (Exception ex)
            {
                throw new Exception($"전체 알림 읽음 처리 실패: {ex.Message}", ex);
            }
        }

        private static QaNotification MapToNotification(QaNotificationTable table)
        {
            return new QaNotification
            {
                Id = table.Id,
                UserId = table.UserId,
                QaId = table.QaId,
                PropertyId = table.PropertyId,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                NotificationType = table.NotificationType ?? "answer_received",
                Message = table.Message,
                IsRead = table.IsRead,
                ReadAt = table.ReadAt,
                CreatedAt = table.CreatedAt ?? DateTime.MinValue
            };
        }

        private static QaNotificationTable MapToTable(QaNotification notification)
        {
            return new QaNotificationTable
            {
                Id = notification.Id,
                UserId = notification.UserId,
                QaId = notification.QaId,
                PropertyId = notification.PropertyId,
                BorrowerNumber = notification.BorrowerNumber,
                BorrowerName = notification.BorrowerName,
                NotificationType = notification.NotificationType,
                Message = notification.Message,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                CreatedAt = notification.CreatedAt
            };
        }
    }

    [Postgrest.Attributes.Table("qa_notifications")]
    internal class QaNotificationTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("user_id")]
        public Guid UserId { get; set; }

        [Postgrest.Attributes.Column("qa_id")]
        public Guid QaId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("notification_type")]
        public string? NotificationType { get; set; }

        [Postgrest.Attributes.Column("message")]
        public string? Message { get; set; }

        [Postgrest.Attributes.Column("is_read")]
        public bool IsRead { get; set; }

        [Postgrest.Attributes.Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}

