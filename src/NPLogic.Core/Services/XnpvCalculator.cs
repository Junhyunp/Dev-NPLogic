using System;
using System.Collections.Generic;
using System.Linq;
using NPLogic.Core.Models;

namespace NPLogic.Core.Services
{
    /// <summary>
    /// XNPV 계산기
    /// </summary>
    public class XnpvCalculator
    {
        /// <summary>
        /// XNPV 계산 (Excel XNPV 함수와 동일)
        /// </summary>
        /// <param name="discountRate">연간 할인율</param>
        /// <param name="cashFlows">현금흐름 목록 (날짜, 금액)</param>
        /// <returns>XNPV 값</returns>
        public static decimal CalculateXnpv(decimal discountRate, List<(DateTime Date, decimal Amount)> cashFlows)
        {
            if (cashFlows == null || cashFlows.Count == 0)
                return 0;

            var baseDate = cashFlows.Min(cf => cf.Date);
            decimal xnpv = 0;

            foreach (var cf in cashFlows)
            {
                var daysDiff = (cf.Date - baseDate).TotalDays;
                var yearFraction = daysDiff / 365.0;
                var discountFactor = Math.Pow(1.0 + (double)discountRate, -yearFraction);
                xnpv += cf.Amount * (decimal)discountFactor;
            }

            return Math.Round(xnpv, 2);
        }

        /// <summary>
        /// XNPV 계산 (CashFlow 리스트 사용)
        /// </summary>
        public static decimal CalculateXnpv(decimal discountRate, List<CashFlow> cashFlows)
        {
            var flows = cashFlows.Select(cf => (cf.FlowDate, cf.NetCashFlow)).ToList();
            return CalculateXnpv(discountRate, flows);
        }

        /// <summary>
        /// IRR 계산 (Newton-Raphson 방법)
        /// </summary>
        /// <param name="cashFlows">현금흐름 목록 (날짜, 금액)</param>
        /// <param name="guess">초기 추측값 (기본 0.1 = 10%)</param>
        /// <param name="tolerance">허용 오차</param>
        /// <param name="maxIterations">최대 반복 횟수</param>
        /// <returns>IRR 값</returns>
        public static decimal CalculateXirr(List<(DateTime Date, decimal Amount)> cashFlows, 
            decimal guess = 0.1m, decimal tolerance = 0.0001m, int maxIterations = 100)
        {
            if (cashFlows == null || cashFlows.Count < 2)
                return 0;

            decimal rate = guess;
            
            for (int i = 0; i < maxIterations; i++)
            {
                var xnpv = CalculateXnpv(rate, cashFlows);
                var derivative = CalculateXnpvDerivative(rate, cashFlows);

                if (Math.Abs(derivative) < 0.000001m)
                    break;

                var newRate = rate - xnpv / derivative;
                
                if (Math.Abs(newRate - rate) < tolerance)
                    return Math.Round(newRate, 6);

                rate = newRate;
            }

            return Math.Round(rate, 6);
        }

        /// <summary>
        /// XNPV 미분값 계산 (IRR 계산용)
        /// </summary>
        private static decimal CalculateXnpvDerivative(decimal discountRate, List<(DateTime Date, decimal Amount)> cashFlows)
        {
            if (cashFlows == null || cashFlows.Count == 0)
                return 0;

            var baseDate = cashFlows.Min(cf => cf.Date);
            decimal derivative = 0;

            foreach (var cf in cashFlows)
            {
                var daysDiff = (cf.Date - baseDate).TotalDays;
                var yearFraction = daysDiff / 365.0;
                var factor = -yearFraction * Math.Pow(1.0 + (double)discountRate, -yearFraction - 1);
                derivative += cf.Amount * (decimal)factor;
            }

            return derivative;
        }

        /// <summary>
        /// 현금흐름 집계 결과 계산
        /// </summary>
        public static XnpvResult CalculateResult(List<CashFlow> cashFlows, decimal discountRate, int scenario = 1)
        {
            var flows = cashFlows.Where(cf => cf.Scenario == scenario).ToList();
            
            var totalInflow = flows.Sum(cf => cf.CashInflow);
            var totalOutflow = flows.Sum(cf => cf.CashOutflow);
            var totalNet = totalInflow - totalOutflow;

            var flowPairs = flows.Select(cf => (cf.FlowDate, cf.NetCashFlow)).ToList();
            
            var xnpv = CalculateXnpv(discountRate, flowPairs);
            var irr = flowPairs.Count >= 2 ? CalculateXirr(flowPairs) : 0;

            var opinion = GetInvestmentOpinion(xnpv, totalNet, discountRate);

            return new XnpvResult
            {
                Xnpv = xnpv,
                Irr = irr,
                DiscountRate = discountRate,
                TotalInflow = totalInflow,
                TotalOutflow = totalOutflow,
                TotalNetCashFlow = totalNet,
                Scenario = scenario,
                InvestmentOpinion = opinion
            };
        }

        /// <summary>
        /// 투자 의견 도출
        /// </summary>
        private static string GetInvestmentOpinion(decimal xnpv, decimal totalNet, decimal discountRate)
        {
            if (xnpv > 0 && totalNet > 0)
            {
                if (xnpv / Math.Abs(totalNet) > 0.1m)
                    return "투자 권장 (XNPV 양호)";
                return "투자 고려 가능";
            }
            else if (xnpv <= 0 && totalNet > 0)
            {
                return "할인율 대비 수익성 부족";
            }
            else
            {
                return "투자 비권장 (손실 예상)";
            }
        }

        /// <summary>
        /// 시나리오 비교 분석
        /// </summary>
        public static List<XnpvResult> CompareScenarios(List<CashFlow> cashFlows, decimal discountRate, int[] scenarios)
        {
            var results = new List<XnpvResult>();
            
            foreach (var scenario in scenarios)
            {
                var result = CalculateResult(cashFlows, discountRate, scenario);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 할인율 민감도 분석
        /// </summary>
        public static List<(decimal Rate, decimal Xnpv)> SensitivityAnalysis(
            List<CashFlow> cashFlows, 
            decimal minRate, 
            decimal maxRate, 
            decimal step,
            int scenario = 1)
        {
            var results = new List<(decimal Rate, decimal Xnpv)>();
            var flows = cashFlows.Where(cf => cf.Scenario == scenario).ToList();
            var flowPairs = flows.Select(cf => (cf.FlowDate, cf.NetCashFlow)).ToList();

            for (var rate = minRate; rate <= maxRate; rate += step)
            {
                var xnpv = CalculateXnpv(rate, flowPairs);
                results.Add((rate, xnpv));
            }

            return results;
        }
    }
}

