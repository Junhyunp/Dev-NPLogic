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

        private PropertyQa MapToModel(PropertyQaTable table)
        {
            return new PropertyQa
            {
                Id = table.Id,
                PropertyId = table.PropertyId,
                Question = table.Question,
                Answer = table.Answer,
                CreatedBy = table.CreatedBy,
                AnsweredBy = table.AnsweredBy,
                CreatedAt = table.CreatedAt,
                AnsweredAt = table.AnsweredAt,
                UpdatedAt = table.UpdatedAt
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
                CreatedBy = model.CreatedBy,
                AnsweredBy = model.AnsweredBy,
                CreatedAt = model.CreatedAt,
                AnsweredAt = model.AnsweredAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }

    /// <summary>
    /// QA 모델
    /// </summary>
    public class PropertyQa
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string Question { get; set; } = "";
        public string? Answer { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? AnsweredBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("property_qa")]
    internal class PropertyQaTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid PropertyId { get; set; }

        [Postgrest.Attributes.Column("question")]
        public string Question { get; set; } = "";

        [Postgrest.Attributes.Column("answer")]
        public string? Answer { get; set; }

        [Postgrest.Attributes.Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Postgrest.Attributes.Column("answered_by")]
        public Guid? AnsweredBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("answered_at")]
        public DateTime? AnsweredAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

