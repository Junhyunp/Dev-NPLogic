using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 권리 정보 모델 (갑구/을구)
    /// </summary>
    public class RegistryRight
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }

        /// <summary>
        /// 구분: 갑구, 을구
        /// </summary>
        public string Section { get; set; } = "갑구";

        /// <summary>
        /// 권리 유형: 근저당권설정, 가압류, 전세권 등
        /// </summary>
        public string? RightType { get; set; }
        
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

        /// <summary>
        /// 대상소유자
        /// </summary>
        public string? TargetOwner { get; set; }

        /// <summary>
        /// 지번번호
        /// </summary>
        public string? JibeonNumber { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

