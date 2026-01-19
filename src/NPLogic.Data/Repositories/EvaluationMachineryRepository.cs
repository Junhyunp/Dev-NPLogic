using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Supabase;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 기계기구 리포지토리 (evaluation_machinery 테이블)
    /// </summary>
    public class EvaluationMachineryRepository
    {
        private readonly Client _supabase;
        
        public EvaluationMachineryRepository(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        }
        
        /// <summary>
        /// 평가 ID로 기계기구 목록 조회
        /// </summary>
        public async Task<List<EvaluationMachinery>> GetByEvaluationIdAsync(Guid evaluationId)
        {
            var response = await _supabase
                .From<EvaluationMachinery>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Order("item_number", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 특정 공장저당에 해당하는 기계기구 조회
        /// </summary>
        public async Task<List<EvaluationMachinery>> GetByFactoryMortgageAsync(Guid evaluationId, int mortgageNumber)
        {
            var filterColumn = mortgageNumber switch
            {
                1 => "factory_mortgage_1",
                2 => "factory_mortgage_2",
                3 => "factory_mortgage_3",
                4 => "factory_mortgage_4",
                _ => throw new ArgumentException($"Invalid mortgage number: {mortgageNumber}")
            };
            
            var response = await _supabase
                .From<EvaluationMachinery>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Filter(filterColumn, Postgrest.Constants.Operator.Equals, "true")
                .Order("item_number", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 기계기구 저장
        /// </summary>
        public async Task<EvaluationMachinery> SaveAsync(EvaluationMachinery machinery)
        {
            if (machinery.Id == Guid.Empty)
            {
                machinery.Id = Guid.NewGuid();
                machinery.CreatedAt = DateTime.UtcNow;
            }
            machinery.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<EvaluationMachinery>()
                .Upsert(machinery);
            
            return response.Models.Count > 0 ? response.Models[0] : machinery;
        }
        
        /// <summary>
        /// 여러 기계기구 저장
        /// </summary>
        public async Task SaveBatchAsync(List<EvaluationMachinery> machineryList)
        {
            foreach (var machinery in machineryList)
            {
                if (machinery.Id == Guid.Empty)
                {
                    machinery.Id = Guid.NewGuid();
                    machinery.CreatedAt = DateTime.UtcNow;
                }
                machinery.UpdatedAt = DateTime.UtcNow;
            }
            
            await _supabase
                .From<EvaluationMachinery>()
                .Upsert(machineryList);
        }
        
        /// <summary>
        /// 기계기구 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _supabase
                .From<EvaluationMachinery>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 평가 ID의 모든 기계기구 삭제
        /// </summary>
        public async Task DeleteByEvaluationIdAsync(Guid evaluationId)
        {
            await _supabase
                .From<EvaluationMachinery>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Delete();
        }
    }
}
