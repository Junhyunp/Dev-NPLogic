using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// reference/Auction-Certificate의 gapgu.csv에 대응하는 행
    /// </summary>
    public class RegistryGapguRow
    {
        public Guid Id { get; set; }
        public Guid RegistryRunId { get; set; }
        public Guid PropertyId { get; set; }

        public string? RankNo { get; set; } // 순위번호
        public string? Purpose { get; set; } // 등기목적
        public string? Receipt { get; set; } // 접수정보
        public DateTime? ReceiptDate { get; set; } // 접수날짜
        public string? RightHolder { get; set; } // 권리자/채권자/가등기권자
        public decimal? ClaimAmount { get; set; } // 청구금액

        // 사용자 입력
        public string? NoteUserInput { get; set; } // 비고
        public string? WageClaimEstimateUserInput { get; set; } // 임금채권추정

        public string? TargetOwner { get; set; } // 대상소유자
        public string? JibunNumber { get; set; } // 지번번호
        public int? SortIndex { get; set; } // 표 원본 순서

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

