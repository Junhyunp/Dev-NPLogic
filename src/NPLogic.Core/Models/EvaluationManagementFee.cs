using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 선순위 관리비 모델 (evaluation_management_fees 테이블)
    /// </summary>
    [Table("evaluation_management_fees")]
    public class EvaluationManagementFee : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }
        
        [Column("evaluation_id")]
        public Guid EvaluationId { get; set; }
        
        /// <summary>
        /// 관리사무소 연락처
        /// </summary>
        [Column("management_office_phone")]
        public string? ManagementOfficePhone { get; set; }
        
        /// <summary>
        /// 체납관리비 (원)
        /// </summary>
        [Column("arrears_fee")]
        public decimal ArrearsFee { get; set; }
        
        /// <summary>
        /// 월관리비 (원)
        /// </summary>
        [Column("monthly_fee")]
        public decimal MonthlyFee { get; set; }
        
        /// <summary>
        /// 시나리오1 추정관리비 (원)
        /// </summary>
        [Column("scenario1_estimated_fee")]
        public decimal Scenario1EstimatedFee { get; set; }
        
        /// <summary>
        /// 시나리오2 추정관리비 (원)
        /// </summary>
        [Column("scenario2_estimated_fee")]
        public decimal Scenario2EstimatedFee { get; set; }
        
        /// <summary>
        /// 조회일
        /// </summary>
        [Column("inquiry_date")]
        public DateTime? InquiryDate { get; set; }
        
        /// <summary>
        /// 비고
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
