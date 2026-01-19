using System;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Supabase;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 상권정보/임대동향 리포지토리 (evaluation_commercial_data 테이블)
    /// </summary>
    public class EvaluationCommercialDataRepository
    {
        private readonly Client _supabase;
        
        public EvaluationCommercialDataRepository(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        }
        
        /// <summary>
        /// 평가 ID로 상권정보 조회
        /// </summary>
        public async Task<EvaluationCommercialData?> GetByEvaluationIdAsync(Guid evaluationId)
        {
            var response = await _supabase
                .From<EvaluationCommercialData>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Single();
            
            return response;
        }
        
        /// <summary>
        /// 상권정보 저장
        /// </summary>
        public async Task<EvaluationCommercialData> SaveAsync(EvaluationCommercialData data)
        {
            if (data.Id == Guid.Empty)
            {
                data.Id = Guid.NewGuid();
                data.CreatedAt = DateTime.UtcNow;
            }
            data.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<EvaluationCommercialData>()
                .Upsert(data);
            
            return response.Models.Count > 0 ? response.Models[0] : data;
        }
        
        /// <summary>
        /// 상권정보 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _supabase
                .From<EvaluationCommercialData>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 평가 ID로 상권정보 삭제
        /// </summary>
        public async Task DeleteByEvaluationIdAsync(Guid evaluationId)
        {
            await _supabase
                .From<EvaluationCommercialData>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Delete();
        }
    }
}
