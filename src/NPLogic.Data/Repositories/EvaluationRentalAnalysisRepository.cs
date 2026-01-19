using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Supabase;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 임대 분석 리포지토리 (evaluation_rental_analysis 테이블)
    /// </summary>
    public class EvaluationRentalAnalysisRepository
    {
        private readonly Client _supabase;
        
        public EvaluationRentalAnalysisRepository(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        }
        
        /// <summary>
        /// 평가 ID로 임대 분석 목록 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetByEvaluationIdAsync(Guid evaluationId)
        {
            var response = await _supabase
                .From<EvaluationRentalAnalysis>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 평가 ID와 분석 유형으로 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetByTypeAsync(Guid evaluationId, string analysisType)
        {
            var response = await _supabase
                .From<EvaluationRentalAnalysis>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Filter("analysis_type", Postgrest.Constants.Operator.Equals, analysisType)
                .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 임대호가분석 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetRentalQuotesAsync(Guid evaluationId)
        {
            return await GetByTypeAsync(evaluationId, "rental_quote");
        }
        
        /// <summary>
        /// 무상임대분석 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetFreeRentAnalysisAsync(Guid evaluationId)
        {
            return await GetByTypeAsync(evaluationId, "free_rent");
        }
        
        /// <summary>
        /// 수익가치 (임대차 정보) 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetIncomeValueAnalysisAsync(Guid evaluationId)
        {
            return await GetByTypeAsync(evaluationId, "income_value");
        }
        
        /// <summary>
        /// 탐문결과 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetInquiryAnalysisAsync(Guid evaluationId)
        {
            return await GetByTypeAsync(evaluationId, "inquiry");
        }
        
        /// <summary>
        /// 임대 분석 저장
        /// </summary>
        public async Task<EvaluationRentalAnalysis> SaveAsync(EvaluationRentalAnalysis analysis)
        {
            if (analysis.Id == Guid.Empty)
            {
                analysis.Id = Guid.NewGuid();
                analysis.CreatedAt = DateTime.UtcNow;
            }
            analysis.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<EvaluationRentalAnalysis>()
                .Upsert(analysis);
            
            return response.Models.Count > 0 ? response.Models[0] : analysis;
        }
        
        /// <summary>
        /// 여러 임대 분석 저장
        /// </summary>
        public async Task SaveBatchAsync(List<EvaluationRentalAnalysis> analysisList)
        {
            foreach (var analysis in analysisList)
            {
                if (analysis.Id == Guid.Empty)
                {
                    analysis.Id = Guid.NewGuid();
                    analysis.CreatedAt = DateTime.UtcNow;
                }
                analysis.UpdatedAt = DateTime.UtcNow;
            }
            
            await _supabase
                .From<EvaluationRentalAnalysis>()
                .Upsert(analysisList);
        }
        
        /// <summary>
        /// 임대 분석 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _supabase
                .From<EvaluationRentalAnalysis>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 평가 ID의 모든 임대 분석 삭제
        /// </summary>
        public async Task DeleteByEvaluationIdAsync(Guid evaluationId)
        {
            await _supabase
                .From<EvaluationRentalAnalysis>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 특정 유형의 임대 분석 삭제
        /// </summary>
        public async Task DeleteByTypeAsync(Guid evaluationId, string analysisType)
        {
            await _supabase
                .From<EvaluationRentalAnalysis>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Filter("analysis_type", Postgrest.Constants.Operator.Equals, analysisType)
                .Delete();
        }
    }
}
