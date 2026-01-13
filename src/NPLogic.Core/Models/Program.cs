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

