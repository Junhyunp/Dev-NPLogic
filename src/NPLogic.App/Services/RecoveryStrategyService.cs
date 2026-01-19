using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// 회수전략 시나리오 결과
    /// </summary>
    public class RecoveryScenarioResult
    {
        public int ScenarioNumber { get; set; }
        public string ScenarioName { get; set; } = "";
        public decimal EvaluatedValue { get; set; }
        public decimal BidRate { get; set; }
        public decimal AuctionCost { get; set; }
        public decimal Distribution { get; set; }
        public decimal OffsetRecovery { get; set; }
        public decimal CashFlow { get; set; }
        public decimal? ReductionValue { get; set; }
    }
    
    /// <summary>
    /// 회수전략 요약
    /// </summary>
    public class RecoveryStrategySummary
    {
        public bool IsCapApplied { get; set; }
        public decimal? LoanCap { get; set; }
        public decimal? LoanCap2 { get; set; }
        public decimal? MortgageCap { get; set; }
        
        public DateTime? CfInDate { get; set; }
        public decimal? CashIn { get; set; }
        public decimal? CashOut { get; set; }
        public decimal? Ratio { get; set; }
        public decimal? Xnpv { get; set; }
        public decimal? Opb { get; set; }
        
        // Interim 상계/회수
        public decimal PrincipalRecovery { get; set; }
        public decimal PrincipalOffset { get; set; }
        public decimal InterestRecovery { get; set; }
        public decimal InterestOffset { get; set; }
        public decimal Subrogation { get; set; }
        public decimal OtherRecovery { get; set; }
        public decimal AuctionCost { get; set; }
        
        public List<RecoveryScenarioResult> Scenarios { get; set; } = new();
    }
    
    /// <summary>
    /// 회수전략 계산 서비스
    /// </summary>
    public class RecoveryStrategyService
    {
        // 기본 설정
        private const decimal DefaultAuctionCostRate = 0.03m; // 경매비용 3%
        private const decimal DefaultDiscountRate = 0.06m; // 할인율 6%
        
        /// <summary>
        /// 회수전략 요약 계산
        /// </summary>
        public RecoveryStrategySummary CalculateRecoveryStrategy(
            decimal appraisalValue,
            decimal scenario1Rate,
            decimal scenario2Rate,
            decimal? seniorRights = null,
            decimal? opb = null,
            InterimRecoveryData? interimData = null)
        {
            var summary = new RecoveryStrategySummary();
            
            // 시나리오 1 (하한)
            var scenario1Value = appraisalValue * scenario1Rate;
            var scenario1Cost = scenario1Value * DefaultAuctionCostRate;
            var scenario1Distribution = scenario1Value - scenario1Cost - (seniorRights ?? 0);
            if (scenario1Distribution < 0) scenario1Distribution = 0;
            
            summary.Scenarios.Add(new RecoveryScenarioResult
            {
                ScenarioNumber = 1,
                ScenarioName = "하한",
                EvaluatedValue = scenario1Value,
                BidRate = scenario1Rate,
                AuctionCost = scenario1Cost,
                Distribution = scenario1Distribution,
                OffsetRecovery = interimData?.TotalOffset ?? 0,
                CashFlow = scenario1Distribution + (interimData?.TotalOffset ?? 0)
            });
            
            // 시나리오 2 (상한)
            var scenario2Value = appraisalValue * scenario2Rate;
            var scenario2Cost = scenario2Value * DefaultAuctionCostRate;
            var scenario2Distribution = scenario2Value - scenario2Cost - (seniorRights ?? 0);
            if (scenario2Distribution < 0) scenario2Distribution = 0;
            
            summary.Scenarios.Add(new RecoveryScenarioResult
            {
                ScenarioNumber = 2,
                ScenarioName = "상한",
                EvaluatedValue = scenario2Value,
                BidRate = scenario2Rate,
                AuctionCost = scenario2Cost,
                Distribution = scenario2Distribution,
                OffsetRecovery = interimData?.TotalOffset ?? 0,
                CashFlow = scenario2Distribution + (interimData?.TotalOffset ?? 0)
            });
            
            // Cap 계산
            if (opb.HasValue && opb.Value > 0)
            {
                summary.IsCapApplied = scenario1Value >= opb.Value || scenario2Value >= opb.Value;
                summary.LoanCap = opb.Value;
                summary.MortgageCap = Math.Min(scenario1Value, scenario2Value);
            }
            
            // Interim 데이터 적용
            if (interimData != null)
            {
                summary.PrincipalRecovery = interimData.PrincipalRecovery;
                summary.PrincipalOffset = interimData.PrincipalOffset;
                summary.InterestRecovery = interimData.InterestRecovery;
                summary.InterestOffset = interimData.InterestOffset;
                summary.Subrogation = interimData.Subrogation;
                summary.OtherRecovery = interimData.OtherRecovery;
                summary.AuctionCost = interimData.AuctionCost;
            }
            
            // Cash In/Out 계산
            var avgScenarioValue = (scenario1Value + scenario2Value) / 2;
            summary.CashIn = avgScenarioValue + (interimData?.TotalRecovery ?? 0);
            summary.CashOut = (interimData?.AuctionCost ?? 0);
            
            // Ratio 계산
            if (opb.HasValue && opb.Value > 0)
            {
                summary.Ratio = summary.CashIn / opb.Value;
            }
            
            // XNPV 계산 (간단한 버전)
            summary.Xnpv = CalculateXnpv(summary.CashIn ?? 0, summary.CashOut ?? 0, 12, DefaultDiscountRate);
            summary.Opb = opb;
            
            return summary;
        }
        
        /// <summary>
        /// XNPV 계산 (간단한 버전)
        /// </summary>
        private decimal CalculateXnpv(decimal cashIn, decimal cashOut, int monthsToRecovery, decimal discountRate)
        {
            var netCashFlow = cashIn - cashOut;
            var years = monthsToRecovery / 12.0m;
            var discountFactor = (decimal)Math.Pow((double)(1 + discountRate), -(double)years);
            return netCashFlow * discountFactor;
        }
        
        /// <summary>
        /// 경매비용 계산
        /// </summary>
        public decimal CalculateAuctionCost(decimal winningBid, decimal? additionalCosts = null)
        {
            var baseCost = winningBid * DefaultAuctionCostRate;
            return baseCost + (additionalCosts ?? 0);
        }
        
        /// <summary>
        /// 배당회수 계산
        /// </summary>
        public decimal CalculateDistribution(decimal winningBid, decimal auctionCost, decimal seniorRights)
        {
            var distribution = winningBid - auctionCost - seniorRights;
            return distribution > 0 ? distribution : 0;
        }
    }
    
    /// <summary>
    /// Interim 회수/상계 데이터
    /// </summary>
    public class InterimRecoveryData
    {
        public decimal PrincipalRecovery { get; set; }
        public decimal PrincipalOffset { get; set; }
        public decimal InterestRecovery { get; set; }
        public decimal InterestOffset { get; set; }
        public decimal Subrogation { get; set; }
        public decimal OtherRecovery { get; set; }
        public decimal AuctionCost { get; set; }
        
        public decimal TotalRecovery => PrincipalRecovery + InterestRecovery + OtherRecovery;
        public decimal TotalOffset => PrincipalOffset + InterestOffset + Subrogation;
    }
}
