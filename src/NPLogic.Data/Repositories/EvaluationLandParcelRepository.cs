using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Supabase;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 지번별 평가 리포지토리 (evaluation_land_parcels 테이블)
    /// </summary>
    public class EvaluationLandParcelRepository
    {
        private readonly Client _supabase;
        
        public EvaluationLandParcelRepository(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        }
        
        /// <summary>
        /// 평가 ID로 지번별 평가 목록 조회
        /// </summary>
        public async Task<List<EvaluationLandParcel>> GetByEvaluationIdAsync(Guid evaluationId)
        {
            var response = await _supabase
                .From<EvaluationLandParcel>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 평가 ID와 구분(토지/건물/기계기구)으로 조회
        /// </summary>
        public async Task<List<EvaluationLandParcel>> GetByTypeAsync(Guid evaluationId, string parcelType)
        {
            var response = await _supabase
                .From<EvaluationLandParcel>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Filter("parcel_type", Postgrest.Constants.Operator.Equals, parcelType)
                .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            return response.Models;
        }
        
        /// <summary>
        /// 지번별 평가 저장
        /// </summary>
        public async Task<EvaluationLandParcel> SaveAsync(EvaluationLandParcel parcel)
        {
            if (parcel.Id == Guid.Empty)
            {
                parcel.Id = Guid.NewGuid();
                parcel.CreatedAt = DateTime.UtcNow;
            }
            parcel.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<EvaluationLandParcel>()
                .Upsert(parcel);
            
            return response.Models.Count > 0 ? response.Models[0] : parcel;
        }
        
        /// <summary>
        /// 여러 지번별 평가 저장
        /// </summary>
        public async Task SaveBatchAsync(List<EvaluationLandParcel> parcels)
        {
            foreach (var parcel in parcels)
            {
                if (parcel.Id == Guid.Empty)
                {
                    parcel.Id = Guid.NewGuid();
                    parcel.CreatedAt = DateTime.UtcNow;
                }
                parcel.UpdatedAt = DateTime.UtcNow;
            }
            
            await _supabase
                .From<EvaluationLandParcel>()
                .Upsert(parcels);
        }
        
        /// <summary>
        /// 지번별 평가 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            await _supabase
                .From<EvaluationLandParcel>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();
        }
        
        /// <summary>
        /// 평가 ID의 모든 지번별 평가 삭제
        /// </summary>
        public async Task DeleteByEvaluationIdAsync(Guid evaluationId)
        {
            await _supabase
                .From<EvaluationLandParcel>()
                .Filter("evaluation_id", Postgrest.Constants.Operator.Equals, evaluationId.ToString())
                .Delete();
        }
    }
}
