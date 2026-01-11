using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 경공매 일정 모델
    /// </summary>
    public class AuctionSchedule
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 일정 유형: auction(경매), public_sale(공매)
        /// </summary>
        public string ScheduleType { get; set; } = "auction";
        
        /// <summary>
        /// 사건번호
        /// </summary>
        public string? AuctionNumber { get; set; }
        
        /// <summary>
        /// 경매일 (기일)
        /// </summary>
        public DateTime? AuctionDate { get; set; }
        
        /// <summary>
        /// 입찰일
        /// </summary>
        public DateTime? BidDate { get; set; }
        
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

        // ========== 인터링/상계회수 ==========
        
        /// <summary>
        /// 인터링 원금상계
        /// </summary>
        public decimal InterimPrincipalOffset { get; set; }
        
        /// <summary>
        /// 인터링 원금회수
        /// </summary>
        public decimal InterimPrincipalRecovery { get; set; }
        
        /// <summary>
        /// 인터링 이자상계
        /// </summary>
        public decimal InterimInterestOffset { get; set; }
        
        /// <summary>
        /// 인터링 이자회수
        /// </summary>
        public decimal InterimInterestRecovery { get; set; }

        // ========== 대법원 경매 캡처 (A-002) ==========
        
        /// <summary>
        /// 사건내역 캡처 이미지 URL
        /// </summary>
        public string? CaseCaptureUrl { get; set; }
        
        /// <summary>
        /// 기일내역 캡처 이미지 URL
        /// </summary>
        public string? ScheduleCaptureUrl { get; set; }
        
        /// <summary>
        /// 문건송달내역 캡처 이미지 URL
        /// </summary>
        public string? DocumentCaptureUrl { get; set; }
        
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
        /// 일정 유형 표시 (경매/공매)
        /// </summary>
        public string ScheduleTypeDisplay => ScheduleType switch
        {
            "auction" => "경매",
            "public_sale" => "공매",
            _ => ScheduleType
        };

        /// <summary>
        /// 낙찰률 (낙찰가/최저매각가)
        /// </summary>
        public decimal? BidRate => MinimumBid > 0 && SalePrice > 0 
            ? (SalePrice / MinimumBid) * 100 
            : null;
    }
}

