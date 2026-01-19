using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 임금채권 항목
    /// </summary>
    public class WageClaimItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 순번
        /// </summary>
        public int SequenceNumber { get; set; }
        
        /// <summary>
        /// 성명
        /// </summary>
        public string EmployeeName { get; set; } = "";
        
        /// <summary>
        /// 생년월일
        /// </summary>
        public DateTime? BirthDate { get; set; }
        
        /// <summary>
        /// 가압류일자
        /// </summary>
        public DateTime? SeizureDate { get; set; }
        
        /// <summary>
        /// 가압류금액
        /// </summary>
        public decimal SeizureAmount { get; set; }
        
        /// <summary>
        /// 배당요구일자
        /// </summary>
        public DateTime? ClaimDate { get; set; }
        
        /// <summary>
        /// 임금
        /// </summary>
        public decimal WageAmount { get; set; }
        
        /// <summary>
        /// 퇴직금
        /// </summary>
        public decimal SeveranceAmount { get; set; }
        
        /// <summary>
        /// 기타
        /// </summary>
        public decimal OtherAmount { get; set; }
        
        /// <summary>
        /// 배당요구액 합계 (임금 + 퇴직금 + 기타)
        /// </summary>
        public decimal TotalAmount => WageAmount + SeveranceAmount + OtherAmount;
        
        /// <summary>
        /// 3개월 임금 (최종 3개월분 임금)
        /// </summary>
        public decimal ThreeMonthWage { get; set; }
        
        /// <summary>
        /// 3년분 퇴직금
        /// </summary>
        public decimal ThreeYearSeverance { get; set; }
        
        /// <summary>
        /// 체당금지급액
        /// </summary>
        public decimal SubstitutePayment { get; set; }
        
        /// <summary>
        /// 반영금액
        /// </summary>
        public decimal ReflectedAmount { get; set; }
        
        /// <summary>
        /// 비고
        /// </summary>
        public string Notes { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
