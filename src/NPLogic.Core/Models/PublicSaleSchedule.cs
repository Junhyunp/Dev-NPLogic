using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 공매 일정 모델
    /// </summary>
    public class PublicSaleSchedule
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 공매번호
        /// </summary>
        public string? SaleNumber { get; set; }
        
        /// <summary>
        /// 공매일
        /// </summary>
        public DateTime? SaleDate { get; set; }
        
        /// <summary>
        /// 최저매각가
        /// </summary>
        public decimal? MinimumBid { get; set; }
        
        /// <summary>
        /// 낙찰가
        /// </summary>
        public decimal? SalePrice { get; set; }
        
        /// <summary>
        /// 상태: scheduled, completed, cancelled
        /// </summary>
        public string Status { get; set; } = "scheduled";
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ========== 연관 데이터 ==========
        
        /// <summary>
        /// 물건 정보 (조인용)
        /// </summary>
        public Property? Property { get; set; }

        /// <summary>
        /// 상태 표시
        /// </summary>
        public string StatusDisplay => Status switch
        {
            "scheduled" => "예정",
            "completed" => "완료",
            "cancelled" => "취소",
            _ => Status
        };

        /// <summary>
        /// 낙찰률 (낙찰가/최저매각가)
        /// </summary>
        public decimal? BidRate => MinimumBid > 0 && SalePrice > 0 
            ? (SalePrice / MinimumBid) * 100 
            : null;
    }
}

