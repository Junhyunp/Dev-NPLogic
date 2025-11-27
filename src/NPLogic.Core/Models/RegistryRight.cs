using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 권리 정보 모델 (갑구/을구)
    /// </summary>
    public class RegistryRight
    {
        public Guid Id { get; set; }
        public Guid? RegistryDocumentId { get; set; }
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 권리 유형: 갑구(gap), 을구(eul)
        /// </summary>
        public string RightType { get; set; } = "gap";
        
        /// <summary>
        /// 순위번호
        /// </summary>
        public int? RightOrder { get; set; }
        
        /// <summary>
        /// 권리자/채권자 (갑구) 또는 근저당권자 (을구)
        /// </summary>
        public string? RightHolder { get; set; }
        
        /// <summary>
        /// 청구금액 (갑구) 또는 채권최고액 (을구)
        /// </summary>
        public decimal? ClaimAmount { get; set; }
        
        /// <summary>
        /// 등기일자 (접수정보)
        /// </summary>
        public DateTime? RegistrationDate { get; set; }
        
        /// <summary>
        /// 접수번호
        /// </summary>
        public string? RegistrationNumber { get; set; }
        
        /// <summary>
        /// 등기원인/목적 (가압류, 압류, 근저당권 등)
        /// </summary>
        public string? RegistrationCause { get; set; }
        
        /// <summary>
        /// 상태: active, cancelled
        /// </summary>
        public string Status { get; set; } = "active";
        
        /// <summary>
        /// 비고/메모
        /// </summary>
        public string? Notes { get; set; }
        
        // ========== 을구 전용 필드 ==========
        
        /// <summary>
        /// 채무자 (을구 전용)
        /// </summary>
        public string? Debtor { get; set; }
        
        /// <summary>
        /// 담보종류 (을구 전용)
        /// </summary>
        public string? CollateralType { get; set; }
        
        /// <summary>
        /// 공장저당 여부 (을구 전용)
        /// </summary>
        public bool IsFactoryMortgage { get; set; }
        
        // ========== 갑구 전용 필드 ==========
        
        /// <summary>
        /// 임금채권 추정 여부 (갑구 전용)
        /// </summary>
        public bool IsWageClaimEstimate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

