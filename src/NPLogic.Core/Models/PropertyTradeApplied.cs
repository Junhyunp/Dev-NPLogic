using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 실거래가 적용 항목 (체크된 행만 저장)
    /// 테이블에 존재 = 적용(is_applied=true)
    /// </summary>
    public class PropertyTradeApplied
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PropertyId { get; set; }

        /// <summary>거래일자 (YYYYMMDD)</summary>
        public int DealDate { get; set; }

        /// <summary>거래금액 (만원)</summary>
        public int DealAmount { get; set; }

        /// <summary>거래면적 (㎡)</summary>
        public double Area { get; set; }

        /// <summary>층수</summary>
        public string? Floor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
