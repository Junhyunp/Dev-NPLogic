using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 "1. 소유지분현황(갑구)" 표의 한 행
    /// (OCR 표 컬럼 그대로 저장/표시용)
    /// </summary>
    public class RegistryGapguOwnershipShareRow
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }

        /// <summary>순위번호</summary>
        public string? RankNo { get; set; }

        /// <summary>등기명의인</summary>
        public string? OwnerName { get; set; }

        /// <summary>(주민)등록번호</summary>
        public string? OwnerRegNo { get; set; }

        /// <summary>최종지분</summary>
        public string? ShareRatio { get; set; }

        /// <summary>주소</summary>
        public string? Address { get; set; }

        /// <summary>표 원본 순서</summary>
        public int? SortIndex { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

