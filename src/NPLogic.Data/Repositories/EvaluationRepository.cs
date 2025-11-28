using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 평가 정보 Repository
    /// </summary>
    public class EvaluationRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public EvaluationRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 물건 ID로 평가 정보 조회
        /// </summary>
        public async Task<Evaluation?> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<EvaluationFullTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var dto = response.Models.FirstOrDefault();
                return dto?.ToModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetByPropertyIdAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 평가 정보 생성
        /// </summary>
        public async Task<Evaluation?> CreateAsync(Evaluation evaluation)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var dto = EvaluationFullTable.FromModel(evaluation);
                dto.Id = Guid.NewGuid();
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<EvaluationFullTable>()
                    .Insert(dto);

                return response.Models.FirstOrDefault()?.ToModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateAsync error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 평가 정보 업데이트
        /// </summary>
        public async Task<Evaluation?> UpdateAsync(Evaluation evaluation)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var dto = EvaluationFullTable.FromModel(evaluation);
                dto.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<EvaluationFullTable>()
                    .Where(x => x.Id == evaluation.Id)
                    .Update(dto);

                return response.Models.FirstOrDefault()?.ToModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateAsync error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 평가 정보 저장 (없으면 생성, 있으면 업데이트)
        /// </summary>
        public async Task<Evaluation?> SaveAsync(Evaluation evaluation)
        {
            if (evaluation.Id == Guid.Empty)
            {
                return await CreateAsync(evaluation);
            }
            else
            {
                return await UpdateAsync(evaluation);
            }
        }

        /// <summary>
        /// 평가 정보 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<EvaluationFullTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 물건의 모든 평가 이력 조회
        /// </summary>
        public async Task<List<Evaluation>> GetAllByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<EvaluationFullTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(d => d.ToModel()).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllByPropertyIdAsync error: {ex.Message}");
                return new List<Evaluation>();
            }
        }
    }

    /// <summary>
    /// Supabase Table 모델 (평가 전체 정보용)
    /// </summary>
    [Table("evaluations")]
    internal class EvaluationFullTable : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Column("evaluation_type")]
        public string? EvaluationType { get; set; }

        [Column("market_value")]
        public decimal? MarketValue { get; set; }

        [Column("evaluated_value")]
        public decimal? EvaluatedValue { get; set; }

        [Column("recovery_rate")]
        public decimal? RecoveryRate { get; set; }

        [Column("evaluation_details")]
        public string? EvaluationDetails { get; set; }

        [Column("evaluated_by")]
        public Guid? EvaluatedBy { get; set; }

        [Column("evaluated_at")]
        public DateTime? EvaluatedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Evaluation ToModel()
        {
            return new Evaluation
            {
                Id = Id,
                PropertyId = PropertyId,
                EvaluationType = EvaluationType,
                MarketValue = MarketValue,
                EvaluatedValue = EvaluatedValue,
                RecoveryRate = RecoveryRate,
                EvaluationDetailsJson = EvaluationDetails,
                EvaluatedBy = EvaluatedBy,
                EvaluatedAt = EvaluatedAt,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }

        public static EvaluationFullTable FromModel(Evaluation model)
        {
            return new EvaluationFullTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                EvaluationType = model.EvaluationType,
                MarketValue = model.MarketValue,
                EvaluatedValue = model.EvaluatedValue,
                RecoveryRate = model.RecoveryRate,
                EvaluationDetails = model.EvaluationDetailsJson,
                EvaluatedBy = model.EvaluatedBy,
                EvaluatedAt = model.EvaluatedAt,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
