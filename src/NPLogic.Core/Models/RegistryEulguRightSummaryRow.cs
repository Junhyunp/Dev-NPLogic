using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 "3. (근)저당권 및 전세권 등(을구)" 표의 한 행
    /// (OCR 표 컬럼 그대로 저장/표시용)
    /// </summary>
    public class RegistryEulguRightSummaryRow
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }

        /// <summary>순위번호</summary>
        public string? RankNo { get; set; }

        /// <summary>등기목적</summary>
        public string? Purpose { get; set; }

        /// <summary>접수정보</summary>
        public string? Receipt { get; set; }

        /// <summary>주요등기사항</summary>
        public string? Details { get; set; }

        /// <summary>대상소유자</summary>
        public string? TargetOwner { get; set; }

        /// <summary>표 원본 순서</summary>
        public int? SortIndex { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

