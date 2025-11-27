using System;
using System.Collections.Generic;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 현금흐름 모델
    /// </summary>
    public class CashFlow
    {
        public Guid Id { get; set; }
        public Guid? BorrowerId { get; set; }
        public Guid? PropertyId { get; set; }

        /// <summary>현금흐름 시점</summary>
        public DateTime FlowDate { get; set; }

        /// <summary>현금유입 (회수금)</summary>
        public decimal CashInflow { get; set; }

        /// <summary>현금유출 (비용)</summary>
        public decimal CashOutflow { get; set; }

        /// <summary>순현금흐름</summary>
        public decimal NetCashFlow => CashInflow - CashOutflow;

        /// <summary>현금흐름 유형 (RECOVERY, COST, INTERIM 등)</summary>
        public string FlowType { get; set; } = "RECOVERY";

        /// <summary>설명</summary>
        public string? Description { get; set; }

        /// <summary>시나리오 (1안, 2안 등)</summary>
        public int Scenario { get; set; } = 1;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 현금흐름 집계 항목
    /// </summary>
    public class CashFlowSummaryItem
    {
        public int Period { get; set; }
        public DateTime FlowDate { get; set; }
        public decimal CashInflow { get; set; }
        public decimal CashOutflow { get; set; }
        public decimal NetCashFlow => CashInflow - CashOutflow;
        public decimal CumulativeCashFlow { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// XNPV 계산 결과
    /// </summary>
    public class XnpvResult
    {
        public decimal Xnpv { get; set; }
        public decimal Irr { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal TotalInflow { get; set; }
        public decimal TotalOutflow { get; set; }
        public decimal TotalNetCashFlow { get; set; }
        public int Scenario { get; set; }
        public string InvestmentOpinion { get; set; } = "";
    }
}

