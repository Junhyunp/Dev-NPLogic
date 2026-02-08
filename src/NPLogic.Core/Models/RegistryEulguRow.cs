using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// reference/Auction-Certificate의 eulgu.csv에 대응하는 행 (을구)
    /// - 요구사항: 을구에는 "비고" 컬럼 없음
    /// </summary>
    public class RegistryEulguRow
    {
        public Guid Id { get; set; }
        public Guid RegistryRunId { get; set; }
        public Guid PropertyId { get; set; }

        public string? RankNo { get; set; } // 순위번호
        public string? Purpose { get; set; } // 등기목적
        public string? Receipt { get; set; } // 접수정보
        public DateTime? ReceiptDate { get; set; } // 접수날짜
        public string? MortgageHolder { get; set; } // 근저당권자(표시명)
        public decimal? MaxClaimAmount { get; set; } // 채권최고액(표시명)

        // 사용자 입력
        public string? DebtorUserInput { get; set; } // 채무자
        public string? CollateralTypeUserInput { get; set; } // 담보종류
        public string? IsFactoryMortgageUserInput { get; set; } // 공장저당

        public string? TargetOwner { get; set; } // 대상소유자
        public string? JibunNumber { get; set; } // 지번번호
        public int? SortIndex { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

