using System;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Supabase;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 선순위 관리비 리포지토리 (evaluation_management_fees 테이블)
    /// </summary>
    public class EvaluationManagementFeeRepository
    {
        private readonly Client _supabase;
        
        public EvaluationManagementFeeRepository(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        }
        
        /// <summary>
        /// 평가 ID로 선순위 관리비 조회
        /// </summary>
        public async Task<EvaluationManagementFee?> GetByEvaluationIdAsync(Guid evaluationId)
        {
            var response = await _supabase
                .From<EvaluationManagementFee>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Get();
            
            return response.Models.FirstOrDefault();
        }
        
        /// <summary>
        /// 선순위 관리비 저장
        /// </summary>
        public async Task<EvaluationManagementFee> SaveAsync(EvaluationManagementFee fee)
        {
            if (fee.Id == Guid.Empty)
            {
                fee.Id = Guid.NewGuid();
                fee.CreatedAt = DateTime.UtcNow;
            }
            fee.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<EvaluationManagementFee>()
                .Upsert(fee);
            
            return response.Models.Count > 0 ? response.Models[0] : fee;
        }
        
        /// <summary>
        /// 선순위 관리비 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _supabase
                .From<EvaluationManagementFee>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 평가 ID로 선순위 관리비 삭제
        /// </summary>
        public async Task DeleteByEvaluationIdAsync(Guid evaluationId)
        {
            await _supabase
                .From<EvaluationManagementFee>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 추정관리비 계산 및 저장
        /// </summary>
        public async Task<EvaluationManagementFee> CalculateAndSaveAsync(
            Guid evaluationId,
            string? managementOfficePhone,
            decimal arrearsFee,
            decimal monthlyFee,
            int scenario1Months,
            int scenario2Months)
        {
            var fee = await GetByEvaluationIdAsync(evaluationId) ?? new EvaluationManagementFee
            {
                EvaluationId = evaluationId
            };
            
            fee.ManagementOfficePhone = managementOfficePhone;
            fee.ArrearsFee = arrearsFee;
            fee.MonthlyFee = monthlyFee;
            fee.Scenario1EstimatedFee = arrearsFee + (monthlyFee * scenario1Months);
            fee.Scenario2EstimatedFee = arrearsFee + (monthlyFee * scenario2Months);
            
            return await SaveAsync(fee);
        }
    }
}
