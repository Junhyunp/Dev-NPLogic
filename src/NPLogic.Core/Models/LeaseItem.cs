using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 주택임대차 또는 상가임대차 항목
    /// </summary>
    public class LeaseItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 임대차 유형: residential(주택), commercial(상가)
        /// </summary>
        public string LeaseType { get; set; } = "residential";
        
        /// <summary>
        /// 구분 (예: 1층, 2층, 지하 등)
        /// </summary>
        public string Category { get; set; } = "";
        
        /// <summary>
        /// 면적 (평)
        /// </summary>
        public decimal Area { get; set; }
        
        /// <summary>
        /// 임차인
        /// </summary>
        public string TenantName { get; set; } = "";
        
        /// <summary>
        /// 임차개시일
        /// </summary>
        public DateTime? LeaseStartDate { get; set; }
        
        /// <summary>
        /// 임차종료일
        /// </summary>
        public DateTime? LeaseEndDate { get; set; }
        
        /// <summary>
        /// 전입일 (주택임대차)
        /// </summary>
        public DateTime? MoveInDate { get; set; }
        
        /// <summary>
        /// 확정신고일 (상가임대차)
        /// </summary>
        public DateTime? ConfirmationDate { get; set; }
        
        /// <summary>
        /// 보증금
        /// </summary>
        public decimal Deposit { get; set; }
        
        /// <summary>
        /// 월세
        /// </summary>
        public decimal MonthlyRent { get; set; }
        
        /// <summary>
        /// 소액보증금
        /// </summary>
        public decimal SmallDeposit { get; set; }
        
        /// <summary>
        /// 선순위 보증금
        /// </summary>
        public decimal SeniorDeposit { get; set; }
        
        /// <summary>
        /// 비고
        /// </summary>
        public string Notes { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
