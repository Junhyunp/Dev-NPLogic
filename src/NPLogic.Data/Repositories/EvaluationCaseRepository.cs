using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Supabase;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 평가 사례 리포지토리 (evaluation_cases 테이블)
    /// </summary>
    public class EvaluationCaseRepository
    {
        private readonly Client _supabase;
        
        public EvaluationCaseRepository(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        }
        
        /// <summary>
        /// 평가 ID로 사례 목록 조회
        /// </summary>
        public async Task<List<EvaluationCase>> GetByEvaluationIdAsync(Guid evaluationId)
        {
            var response = await _supabase
                .From<EvaluationCase>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Order("case_number", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 사례 저장 (생성 또는 업데이트)
        /// </summary>
        public async Task<EvaluationCase> SaveAsync(EvaluationCase evaluationCase)
        {
            if (evaluationCase.Id == Guid.Empty)
            {
                evaluationCase.Id = Guid.NewGuid();
                evaluationCase.CreatedAt = DateTime.UtcNow;
            }
            evaluationCase.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<EvaluationCase>()
                .Upsert(evaluationCase);
            
            return response.Models.Count > 0 ? response.Models[0] : evaluationCase;
        }
        
        /// <summary>
        /// 사례 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _supabase
                .From<EvaluationCase>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 평가 ID의 모든 사례 삭제
        /// </summary>
        public async Task DeleteByEvaluationIdAsync(Guid evaluationId)
        {
            await _supabase
                .From<EvaluationCase>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 적용할 사례 설정 (해당 평가의 다른 사례들은 비적용으로 변경)
        /// </summary>
        public async Task SetAppliedCaseAsync(Guid evaluationId, Guid caseId)
        {
            // 해당 평가의 모든 사례 조회
            var cases = await GetByEvaluationIdAsync(evaluationId);
            
            // 각 사례의 적용 상태 업데이트
            foreach (var evalCase in cases)
            {
                // 선택된 사례만 적용 상태로 설정 (여기서는 Notes에 표시)
                if (evalCase.Id == caseId)
                {
                    evalCase.Notes = evalCase.Notes?.Replace("[미적용]", "") ?? "";
                    if (!evalCase.Notes.Contains("[적용]"))
                    {
                        evalCase.Notes = "[적용] " + evalCase.Notes;
                    }
                }
                else
                {
                    evalCase.Notes = evalCase.Notes?.Replace("[적용]", "[미적용]") ?? "[미적용]";
                }
                await SaveAsync(evalCase);
            }
        }
    }
}
