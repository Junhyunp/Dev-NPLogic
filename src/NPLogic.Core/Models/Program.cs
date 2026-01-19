using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 프로그램(데이터디스크) 모델
    /// </summary>
    public class Program
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 프로그램명 (예: SHB-2025-03)
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// 담당팀
        /// </summary>
        public string? Team { get; set; }

        /// <summary>
        /// 담당 회계법인
        /// </summary>
        public string? AccountingFirm { get; set; }

        /// <summary>
        /// 차주수
        /// </summary>
        public int BorrowerCount { get; set; }

        /// <summary>
        /// 물건수
        /// </summary>
        public int PropertyCount { get; set; }

        /// <summary>
        /// Cut-off 날짜
        /// </summary>
        public DateTime? CutOffDate { get; set; }

        /// <summary>
        /// 입찰일
        /// </summary>
        public DateTime? BidDate { get; set; }

        /// <summary>
        /// 상태 (active, inactive, completed)
        /// </summary>
        public string Status { get; set; } = "active";

        /// <summary>
        /// 적용환율 (1 USD)
        /// </summary>
        public decimal? ExchangeRateUsd { get; set; }

        /// <summary>
        /// 적용환율 (1 JPY)
        /// </summary>
        public decimal? ExchangeRateJpy { get; set; }

        /// <summary>
        /// 은행명
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// Pool (예: A+B+C+D+E)
        /// </summary>
        public string? Pool { get; set; }

        /// <summary>
        /// Action Date (adate)
        /// </summary>
        public DateTime? ActionDate { get; set; }

        // ========== 기초정보관리 ==========

        /// <summary>
        /// IRR (할인율)
        /// </summary>
        public decimal Irr { get; set; } = 0.15m;

        /// <summary>
        /// 신용보증서 회수율
        /// </summary>
        public decimal CreditGuaranteeRecoveryRate { get; set; } = 0.80m;

        /// <summary>
        /// Interim 회수율
        /// </summary>
        public decimal InterimRecoveryRate { get; set; } = 0.80m;

        /// <summary>
        /// 경매개시후 1회차 Lead time (개월)
        /// </summary>
        public int AuctionFirstLeadTimeMonths { get; set; } = 6;

        /// <summary>
        /// 회차별 Lead time (개월)
        /// </summary>
        public int RoundLeadTime { get; set; } = 2;

        /// <summary>
        /// 배당시 Lead time (개월)
        /// </summary>
        public int DistributionLeadTime { get; set; } = 1;

        /// <summary>
        /// 개회 경매개시일까지 Lead time (개월)
        /// </summary>
        public int OpeningToAuctionLeadTime { get; set; } = 6;

        /// <summary>
        /// 대위등기 필요시 경매개시일까지 Lead time (개월)
        /// </summary>
        public int SubrogationToAuctionLeadTime { get; set; } = 8;

        /// <summary>
        /// 개회, 대위등기 중복시 Lead time (개월)
        /// </summary>
        public int CombinedLeadTime { get; set; } = 10;

        // ========== 사용인 설정 ==========

        /// <summary>
        /// 소속
        /// </summary>
        public string? AgentAffiliation { get; set; }

        /// <summary>
        /// 사용인명
        /// </summary>
        public string? AgentName { get; set; }

        /// <summary>
        /// 등급 (A/B/C)
        /// </summary>
        public string AgentGrade { get; set; } = "A";

        /// <summary>
        /// 사용인 연락처
        /// </summary>
        public string? AgentContact { get; set; }

        /// <summary>
        /// 카카오톡 ID
        /// </summary>
        public string? AgentKakaoId { get; set; }

        /// <summary>
        /// 굿옥션 ID
        /// </summary>
        public string? GoodAuctionId { get; set; }

        /// <summary>
        /// 굿옥션 비밀번호
        /// </summary>
        public string? GoodAuctionPw { get; set; }

        /// <summary>
        /// 생성자 ID
        /// </summary>
        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 활성 프로그램 여부
        /// </summary>
        public bool IsActive => Status?.ToLower() == "active";

        /// <summary>
        /// 완료된 프로그램 여부
        /// </summary>
        public bool IsCompleted => Status?.ToLower() == "completed";
    }

    /// <summary>
    /// 프로그램 상태
    /// </summary>
    public enum ProgramStatus
    {
        Active,     // 활성
        Inactive,   // 비활성
        Completed   // 완료
    }
}

