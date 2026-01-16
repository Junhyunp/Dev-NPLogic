using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 차주 정보 요약 (전체 탭용)
    /// </summary>
    public class BorrowerSummaryModel
    {
        /// <summary>차주번호</summary>
        public string BorrowerNumber { get; set; } = "";
        
        /// <summary>차주명</summary>
        public string BorrowerName { get; set; } = "";
        
        /// <summary>차주유형 (개인/개인사업자/법인)</summary>
        public string BorrowerType { get; set; } = "";
        
        /// <summary>사업자번호</summary>
        public string? BusinessNumber { get; set; }
        
        /// <summary>물건갯수</summary>
        public int PropertyCount { get; set; }
        
        /// <summary>OPB</summary>
        public decimal Opb { get; set; }
        
        /// <summary>근저당설정액</summary>
        public decimal MortgageAmount { get; set; }
        
        /// <summary>회생 여부</summary>
        public bool IsRestructuring { get; set; }
        
        /// <summary>개회 여부</summary>
        public bool IsOpened { get; set; }
        
        /// <summary>차주사망 여부</summary>
        public bool IsDeceased { get; set; }
        
        /// <summary>XNPV 1안</summary>
        public decimal? XnpvScenario1 { get; set; }
        
        /// <summary>XNPV 1안 Ratio</summary>
        public decimal? XnpvRatio1 { get; set; }
        
        /// <summary>XNPV 2안</summary>
        public decimal? XnpvScenario2 { get; set; }
        
        /// <summary>XNPV 2안 Ratio</summary>
        public decimal? XnpvRatio2 { get; set; }
        
        // ========== 표시용 속성 ==========
        
        /// <summary>상태 요약 문자열</summary>
        public string StatusSummary
        {
            get
            {
                var statuses = new System.Collections.Generic.List<string>();
                if (IsRestructuring) statuses.Add("회생");
                if (IsOpened) statuses.Add("개회");
                if (IsDeceased) statuses.Add("사망");
                return statuses.Count > 0 ? string.Join(", ", statuses) : "정상";
            }
        }
        
        /// <summary>OPB 표시용 (천원)</summary>
        public string OpbDisplay => $"{Opb / 1000:N0}천원";
        
        /// <summary>근저당설정액 표시용 (천원)</summary>
        public string MortgageAmountDisplay => $"{MortgageAmount / 1000:N0}천원";
        
        /// <summary>XNPV 1안 Ratio 표시용</summary>
        public string XnpvRatio1Display => XnpvRatio1.HasValue ? $"{XnpvRatio1.Value:P1}" : "-";
        
        /// <summary>XNPV 2안 Ratio 표시용</summary>
        public string XnpvRatio2Display => XnpvRatio2.HasValue ? $"{XnpvRatio2.Value:P1}" : "-";
    }

    /// <summary>
    /// Loan 요약 (전체 탭용)
    /// </summary>
    public class LoanSummaryModel
    {
        /// <summary>대출 건수</summary>
        public int LoanCount { get; set; }
        
        /// <summary>대출원금잔액 합계</summary>
        public decimal TotalPrincipalBalance { get; set; }
        
        /// <summary>채권액 합계</summary>
        public decimal TotalClaimAmount { get; set; }
        
        /// <summary>Loan Cap 1안 합계</summary>
        public decimal TotalLoanCap1 { get; set; }
        
        /// <summary>Loan Cap 2안 합계</summary>
        public decimal TotalLoanCap2 { get; set; }
        
        /// <summary>MCI 보증 대출 건수</summary>
        public int MciLoanCount { get; set; }
        
        /// <summary>유효보증서 대출 건수</summary>
        public int ValidGuaranteeCount { get; set; }
        
        // ========== 표시용 속성 ==========
        
        /// <summary>대출원금잔액 표시용</summary>
        public string TotalPrincipalBalanceDisplay => $"{TotalPrincipalBalance:N0}원";
        
        /// <summary>채권액 합계 표시용</summary>
        public string TotalClaimAmountDisplay => $"{TotalClaimAmount:N0}원";
        
        /// <summary>Loan Cap 1안 표시용</summary>
        public string TotalLoanCap1Display => $"{TotalLoanCap1:N0}원";
        
        /// <summary>Loan Cap 2안 표시용</summary>
        public string TotalLoanCap2Display => $"{TotalLoanCap2:N0}원";
        
        /// <summary>보증서 현황 표시용</summary>
        public string GuaranteeStatusDisplay => MciLoanCount > 0 ? $"MCI {MciLoanCount}건" : (ValidGuaranteeCount > 0 ? $"유효 {ValidGuaranteeCount}건" : "없음");
    }

    /// <summary>
    /// 담보물건 요약 (전체 탭용)
    /// </summary>
    public class CollateralSummaryModel
    {
        /// <summary>담보종류 (물건유형)</summary>
        public string? PropertyType { get; set; }
        
        /// <summary>토지 면적 (㎡)</summary>
        public decimal? LandArea { get; set; }
        
        /// <summary>건물 면적 (㎡)</summary>
        public decimal? BuildingArea { get; set; }
        
        /// <summary>기계기구 면적/가치</summary>
        public decimal? MachineValue { get; set; }
        
        /// <summary>감정평가액</summary>
        public decimal? AppraisalValue { get; set; }
        
        /// <summary>공장저당 여부</summary>
        public bool IsFactoryMortgage { get; set; }
        
        /// <summary>공단입주 여부</summary>
        public bool IsInIndustrialComplex { get; set; }
        
        // ========== 표시용 속성 ==========
        
        /// <summary>토지 면적 표시용 (평)</summary>
        public string LandAreaDisplay => LandArea.HasValue ? $"{LandArea.Value / 3.3058m:N1}평" : "-";
        
        /// <summary>건물 면적 표시용 (평)</summary>
        public string BuildingAreaDisplay => BuildingArea.HasValue ? $"{BuildingArea.Value / 3.3058m:N1}평" : "-";
        
        /// <summary>감정평가액 표시용</summary>
        public string AppraisalValueDisplay => AppraisalValue.HasValue ? $"{AppraisalValue.Value:N0}원" : "-";
        
        /// <summary>공장저당 표시용</summary>
        public string FactoryMortgageDisplay => IsFactoryMortgage ? "Y" : "N";
        
        /// <summary>공단입주 표시용</summary>
        public string IndustrialComplexDisplay => IsInIndustrialComplex ? "Y" : "N";
    }

    /// <summary>
    /// 선순위 요약 (전체 탭용)
    /// </summary>
    public class SeniorRightsSummaryModel
    {
        /// <summary>선순위 근저당 DD</summary>
        public decimal SeniorMortgageDd { get; set; }
        
        /// <summary>선순위 근저당 반영</summary>
        public decimal SeniorMortgageReflected { get; set; }
        
        /// <summary>유치권 DD</summary>
        public decimal LienDd { get; set; }
        
        /// <summary>유치권 반영</summary>
        public decimal LienReflected { get; set; }
        
        /// <summary>소액보증금 DD</summary>
        public decimal SmallDepositDd { get; set; }
        
        /// <summary>소액보증금 반영</summary>
        public decimal SmallDepositReflected { get; set; }
        
        /// <summary>임차보증금 DD</summary>
        public decimal LeaseDepositDd { get; set; }
        
        /// <summary>임차보증금 반영</summary>
        public decimal LeaseDepositReflected { get; set; }
        
        /// <summary>임금채권 DD</summary>
        public decimal WageClaimDd { get; set; }
        
        /// <summary>임금채권 반영</summary>
        public decimal WageClaimReflected { get; set; }
        
        /// <summary>당해세 DD</summary>
        public decimal CurrentTaxDd { get; set; }
        
        /// <summary>당해세 반영</summary>
        public decimal CurrentTaxReflected { get; set; }
        
        /// <summary>조세채권 DD</summary>
        public decimal SeniorTaxDd { get; set; }
        
        /// <summary>조세채권 반영</summary>
        public decimal SeniorTaxReflected { get; set; }
        
        /// <summary>선순위 합계 DD</summary>
        public decimal TotalDd => SeniorMortgageDd + LienDd + SmallDepositDd + LeaseDepositDd + WageClaimDd + CurrentTaxDd + SeniorTaxDd;
        
        /// <summary>선순위 합계 반영</summary>
        public decimal TotalReflected => SeniorMortgageReflected + LienReflected + SmallDepositReflected + LeaseDepositReflected + WageClaimReflected + CurrentTaxReflected + SeniorTaxReflected;
        
        // ========== 표시용 속성 ==========
        
        /// <summary>선순위 합계 표시용</summary>
        public string TotalReflectedDisplay => $"{TotalReflected:N0}원";
        
        /// <summary>선순위 근저당 표시용 (DD/반영)</summary>
        public string SeniorMortgageDisplay => $"{SeniorMortgageDd:N0} / {SeniorMortgageReflected:N0}";
        
        /// <summary>유치권 표시용 (DD/반영)</summary>
        public string LienDisplay => $"{LienDd:N0} / {LienReflected:N0}";
        
        /// <summary>소액보증금 표시용 (DD/반영)</summary>
        public string SmallDepositDisplay => $"{SmallDepositDd:N0} / {SmallDepositReflected:N0}";
        
        /// <summary>임차보증금 표시용 (DD/반영)</summary>
        public string LeaseDepositDisplay => $"{LeaseDepositDd:N0} / {LeaseDepositReflected:N0}";
        
        /// <summary>임금채권 표시용 (DD/반영)</summary>
        public string WageClaimDisplay => $"{WageClaimDd:N0} / {WageClaimReflected:N0}";
        
        /// <summary>당해세 표시용 (DD/반영)</summary>
        public string CurrentTaxDisplay => $"{CurrentTaxDd:N0} / {CurrentTaxReflected:N0}";
        
        /// <summary>조세채권 표시용 (DD/반영)</summary>
        public string SeniorTaxDisplay => $"{SeniorTaxDd:N0} / {SeniorTaxReflected:N0}";
    }

    /// <summary>
    /// 평가 요약 (전체 탭용)
    /// </summary>
    public class EvaluationSummaryModel
    {
        /// <summary>평가유형</summary>
        public string? EvaluationType { get; set; }
        
        /// <summary>시나리오1 낙찰가</summary>
        public decimal? Scenario1Value { get; set; }
        
        /// <summary>시나리오1 낙찰가율</summary>
        public decimal? Scenario1BidRate { get; set; }
        
        /// <summary>시나리오2 낙찰가</summary>
        public decimal? Scenario2Value { get; set; }
        
        /// <summary>시나리오2 낙찰가율</summary>
        public decimal? Scenario2BidRate { get; set; }
        
        /// <summary>적용 낙찰가율</summary>
        public decimal? AppliedBidRate { get; set; }
        
        /// <summary>평가 사유</summary>
        public string? EvaluationReason { get; set; }
        
        /// <summary>평가일시</summary>
        public DateTime? EvaluatedAt { get; set; }
        
        // ========== 표시용 속성 ==========
        
        /// <summary>시나리오1 표시용</summary>
        public string Scenario1Display => Scenario1Value.HasValue ? $"{Scenario1Value.Value:N0}원" : "-";
        
        /// <summary>시나리오1 낙찰가율 표시용</summary>
        public string Scenario1BidRateDisplay => Scenario1BidRate.HasValue ? $"{Scenario1BidRate.Value:P1}" : "-";
        
        /// <summary>시나리오2 표시용</summary>
        public string Scenario2Display => Scenario2Value.HasValue ? $"{Scenario2Value.Value:N0}원" : "-";
        
        /// <summary>시나리오2 낙찰가율 표시용</summary>
        public string Scenario2BidRateDisplay => Scenario2BidRate.HasValue ? $"{Scenario2BidRate.Value:P1}" : "-";
        
        /// <summary>적용 낙찰가율 표시용</summary>
        public string AppliedBidRateDisplay => AppliedBidRate.HasValue ? $"{AppliedBidRate.Value:P1}" : "-";
    }

    /// <summary>
    /// 경(공)매 일정 요약 (전체 탭용)
    /// </summary>
    public class AuctionSummaryModel
    {
        /// <summary>경매개시여부</summary>
        public bool IsAuctionStarted { get; set; }
        
        /// <summary>관할법원</summary>
        public string? CourtName { get; set; }
        
        /// <summary>경매 사건번호</summary>
        public string? CaseNumber { get; set; }
        
        /// <summary>경매개시일</summary>
        public DateTime? AuctionStartDate { get; set; }
        
        /// <summary>최종경매회차</summary>
        public int? FinalAuctionRound { get; set; }
        
        /// <summary>최종경매결과</summary>
        public string? FinalAuctionResult { get; set; }
        
        /// <summary>차후예정 경매일</summary>
        public DateTime? NextAuctionDate { get; set; }
        
        /// <summary>차후예정 최저입찰가</summary>
        public decimal? NextMinimumBid { get; set; }
        
        /// <summary>배당요구종기일</summary>
        public DateTime? ClaimDeadlineDate { get; set; }
        
        /// <summary>낙찰금액</summary>
        public decimal? WinningBidAmount { get; set; }
        
        // ========== 표시용 속성 ==========
        
        /// <summary>경매개시여부 표시용</summary>
        public string AuctionStatusDisplay => IsAuctionStarted ? "개시" : "미개시";
        
        /// <summary>관할법원/사건번호 표시용</summary>
        public string CourtCaseDisplay => !string.IsNullOrEmpty(CourtName) && !string.IsNullOrEmpty(CaseNumber) 
            ? $"{CourtName} {CaseNumber}" 
            : "-";
        
        /// <summary>최종경매 표시용</summary>
        public string FinalAuctionDisplay => FinalAuctionRound.HasValue 
            ? $"{FinalAuctionRound}회차 ({FinalAuctionResult ?? "-"})" 
            : "-";
        
        /// <summary>차후예정일 표시용</summary>
        public string NextAuctionDisplay => NextAuctionDate.HasValue 
            ? NextAuctionDate.Value.ToString("yyyy-MM-dd") 
            : "-";
        
        /// <summary>차후예정 최저입찰가 표시용</summary>
        public string NextMinimumBidDisplay => NextMinimumBid.HasValue 
            ? $"{NextMinimumBid.Value:N0}원" 
            : "-";
        
        /// <summary>낙찰금액 표시용</summary>
        public string WinningBidAmountDisplay => WinningBidAmount.HasValue 
            ? $"{WinningBidAmount.Value:N0}원" 
            : "-";
    }
}
