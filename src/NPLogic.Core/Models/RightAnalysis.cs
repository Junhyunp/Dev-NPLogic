using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 권리분석 모델 - 선순위 분석 및 배당 시뮬레이션
    /// </summary>
    public class RightAnalysis
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }

        // ===== 경매사건 정보 =====
        
        /// <summary>경매개시여부: opened(개시)/not_opened(미개시)</summary>
        public string? AuctionStatus { get; set; }
        
        /// <summary>선행경매</summary>
        public bool PrecedentAuction { get; set; }
        
        /// <summary>후행경매</summary>
        public bool SubsequentAuction { get; set; }
        
        /// <summary>관할법원</summary>
        public string? CourtName { get; set; }
        
        /// <summary>경매사건번호</summary>
        public string? CaseNumber { get; set; }
        
        /// <summary>경매신청기관</summary>
        public string? AuctionApplicant { get; set; }
        
        /// <summary>경매개시일자</summary>
        public DateTime? AuctionStartDate { get; set; }
        
        /// <summary>배당요구종기일</summary>
        public DateTime? ClaimDeadlineDate { get; set; }
        
        /// <summary>청구금액</summary>
        public decimal? ClaimAmount { get; set; }
        
        /// <summary>최초법사가 (최초감정가)</summary>
        public decimal? InitialAppraisalValue { get; set; }
        
        /// <summary>최초경매기일</summary>
        public DateTime? InitialAuctionDate { get; set; }
        
        /// <summary>최종경매회차</summary>
        public int? FinalAuctionRound { get; set; }
        
        /// <summary>최종경매기일</summary>
        public DateTime? FinalAuctionDate { get; set; }
        
        /// <summary>최종경매결과</summary>
        public string? FinalAuctionResult { get; set; }
        
        /// <summary>낙찰금액</summary>
        public decimal? WinningBidAmount { get; set; }
        
        /// <summary>차기경매기일</summary>
        public DateTime? NextAuctionDate { get; set; }
        
        /// <summary>차후예정경매 최저입찰금액</summary>
        public decimal? NextMinimumBid { get; set; }
        
        /// <summary>최저입찰가 (최종경매일 기준)</summary>
        public decimal? MinimumBid { get; set; }
        
        /// <summary>경매회차</summary>
        public int? AuctionCount { get; set; }
        
        /// <summary>배당요구종기일 경과여부</summary>
        public bool ClaimDeadlinePassed { get; set; }

        // ===== 경매사건 메모/이미지 =====

        /// <summary>당사자내역 이미지 (Base64)</summary>
        public string? PartyDetailsImageBase64 { get; set; }

        /// <summary>현황조사서 메모</summary>
        public string? SurveyReportNote { get; set; }

        /// <summary>감정평가서 메모</summary>
        public string? AppraisalReportNote { get; set; }

        // ===== 전입/임차 현황 =====
        
        /// <summary>물건지, 소유주 주소지 일치여부</summary>
        public bool? AddressMatch { get; set; }
        
        /// <summary>소유주 전입</summary>
        public bool? OwnerRegistered { get; set; }
        
        /// <summary>전입인(임차인)</summary>
        public string? TenantName { get; set; }
        
        /// <summary>전입일(임차시작일)</summary>
        public DateTime? TenantMoveInDate { get; set; }
        
        /// <summary>주택공시가격</summary>
        public decimal? HousingOfficialPrice { get; set; }
        
        /// <summary>경매열람자료 보유</summary>
        public bool HasAuctionDocs { get; set; }
        
        /// <summary>전입세대열람 보유</summary>
        public bool HasTenantRegistry { get; set; }
        
        /// <summary>상가임대차열람 보유</summary>
        public bool HasCommercialLease { get; set; }
        
        /// <summary>임차인 존재여부</summary>
        public bool? HasTenant { get; set; }
        
        /// <summary>임차인 배당요구신청</summary>
        public bool? TenantClaimSubmitted { get; set; }
        
        /// <summary>임차일이 근저당설정일 이전인지</summary>
        public bool? TenantDateBeforeMortgage { get; set; }
        
        /// <summary>현황조사서 제출여부</summary>
        public bool? SurveyReportSubmitted { get; set; }
        
        /// <summary>현황조사서 제출일자</summary>
        public DateTime? SurveyReportDate { get; set; }
        
        /// <summary>채무자유형: individual(개인)/business(개인사업자)/corporation(법인)</summary>
        public string? DebtorType { get; set; }
        
        /// <summary>임금채권 존재여부</summary>
        public bool HasWageClaim { get; set; }
        
        /// <summary>임금채권 배당요구신청</summary>
        public bool WageClaimSubmitted { get; set; }
        
        /// <summary>임금채권 추정가압류</summary>
        public bool WageClaimEstimatedSeizure { get; set; }
        
        /// <summary>당해세 교부청구</summary>
        public bool HasTaxClaim { get; set; }
        
        /// <summary>선순위조세 교부청구</summary>
        public bool HasSeniorTaxClaim { get; set; }

        // ===== 선순위 분석 그리드 =====
        
        // 선순위 근저당권
        /// <summary>선순위근저당권 - DD금액</summary>
        public decimal SeniorMortgageDd { get; set; }
        
        /// <summary>선순위근저당권 - 평가자 반영금액</summary>
        public decimal SeniorMortgageReflected { get; set; }
        
        /// <summary>선순위근저당권 - 상세추정 근거</summary>
        public string? SeniorMortgageReason { get; set; }

        // 유치권
        /// <summary>유치권 신고금액 - DD금액</summary>
        public decimal LienDd { get; set; }
        
        /// <summary>유치권 - 평가자 반영금액</summary>
        public decimal LienReflected { get; set; }
        
        /// <summary>유치권 - 상세추정 근거</summary>
        public string? LienReason { get; set; }

        // 선순위 소액보증금
        /// <summary>선순위소액보증금 - DD금액</summary>
        public decimal SmallDepositDd { get; set; }
        
        /// <summary>선순위소액보증금 - 평가자 반영금액</summary>
        public decimal SmallDepositReflected { get; set; }
        
        /// <summary>선순위소액보증금 - 상세추정 근거</summary>
        public string? SmallDepositReason { get; set; }
        
        /// <summary>선순위소액보증금 - 판단케이스 코드</summary>
        public string? SmallDepositCase { get; set; }

        // 선순위 임차보증금
        /// <summary>선순위임차보증금 - DD금액</summary>
        public decimal LeaseDepositDd { get; set; }
        
        /// <summary>선순위임차보증금 - 평가자 반영금액</summary>
        public decimal LeaseDepositReflected { get; set; }
        
        /// <summary>선순위임차보증금 - 상세추정 근거</summary>
        public string? LeaseDepositReason { get; set; }

        // 선순위 임금채권
        /// <summary>선순위 임금채권 - DD금액</summary>
        public decimal WageClaimDd { get; set; }
        
        /// <summary>선순위 임금채권 - 평가자 반영금액</summary>
        public decimal WageClaimReflected { get; set; }
        
        /// <summary>선순위 임금채권 - 상세추정 근거</summary>
        public string? WageClaimReason { get; set; }

        // 당해세
        /// <summary>당해세 - DD금액</summary>
        public decimal CurrentTaxDd { get; set; }
        
        /// <summary>당해세 - 평가자 반영금액</summary>
        public decimal CurrentTaxReflected { get; set; }
        
        /// <summary>당해세 - 상세추정 근거</summary>
        public string? CurrentTaxReason { get; set; }

        // 선순위 조세채권
        /// <summary>선순위 조세채권 - DD금액</summary>
        public decimal SeniorTaxDd { get; set; }
        
        /// <summary>선순위 조세채권 - 평가자 반영금액</summary>
        public decimal SeniorTaxReflected { get; set; }
        
        /// <summary>선순위 조세채권 - 상세추정 근거</summary>
        public string? SeniorTaxReason { get; set; }

        // 기타 선순위
        /// <summary>기타 선순위 - DD금액</summary>
        public decimal EtcDd { get; set; }
        
        /// <summary>기타 선순위 - 평가자 반영금액</summary>
        public decimal EtcReflected { get; set; }
        
        /// <summary>기타 선순위 - 상세추정 근거</summary>
        public string? EtcReason { get; set; }

        // DD 합계
        /// <summary>선순위 DD 금액 합계</summary>
        public decimal? SeniorTotalDd { get; set; }

        // ===== 감정평가 정보 =====
        
        /// <summary>감정평가일</summary>
        public DateTime? AppraisalDate { get; set; }
        
        /// <summary>감정평가액 (합계)</summary>
        public decimal? AppraisalValue { get; set; }
        
        /// <summary>감정평가 구분</summary>
        public string? AppraisalType { get; set; }
        
        /// <summary>감정평가기관</summary>
        public string? AppraisalAgency { get; set; }

        // ===== 배당 시뮬레이션 =====
        
        /// <summary>선순위 합계 (자동 계산)</summary>
        public decimal? SeniorRightsTotal { get; set; }
        
        /// <summary>근저당권 개수</summary>
        public int? MortgageCount { get; set; }
        
        /// <summary>가압류 개수</summary>
        public int? SeizureCount { get; set; }
        
        /// <summary>예상낙찰가</summary>
        public decimal? ExpectedWinningBid { get; set; }
        
        /// <summary>경매수수료</summary>
        public decimal? AuctionFees { get; set; }
        
        /// <summary>배당가능재원</summary>
        public decimal? DistributableAmount { get; set; }
        
        /// <summary>선순위 공제 후 금액</summary>
        public decimal? AmountAfterSenior { get; set; }
        
        /// <summary>Loan Cap</summary>
        public decimal? LoanCap { get; set; }
        
        /// <summary>Cap 반영 배당액</summary>
        public decimal? CapAppliedDividend { get; set; }
        
        /// <summary>회수예상금액</summary>
        public decimal? RecoveryAmount { get; set; }
        
        /// <summary>회수율 (%)</summary>
        public decimal? RecoveryRate { get; set; }

        // ===== 위험도 평가 =====
        
        /// <summary>위험도 (high/medium/low)</summary>
        public string? RiskLevel { get; set; }
        
        /// <summary>위험도 판단 근거</summary>
        public string? RiskReason { get; set; }
        
        /// <summary>권장 의견</summary>
        public string? Recommendations { get; set; }
        
        /// <summary>배당분석 상세 (JSON)</summary>
        public string? DistributionAnalysis { get; set; }

        // ===== 메타데이터 =====
        
        /// <summary>분석자</summary>
        public Guid? AnalyzedBy { get; set; }
        
        /// <summary>분석일시</summary>
        public DateTime? AnalyzedAt { get; set; }
        
        /// <summary>분석 완료 여부</summary>
        public bool IsCompleted { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ===== 계산 메서드 =====
        
        /// <summary>
        /// 선순위 합계 계산
        /// </summary>
        public decimal CalculateSeniorRightsTotal()
        {
            return SeniorMortgageReflected 
                 + LienReflected 
                 + SmallDepositReflected 
                 + LeaseDepositReflected 
                 + WageClaimReflected 
                 + CurrentTaxReflected 
                 + SeniorTaxReflected;
        }

        /// <summary>
        /// 배당가능재원 계산
        /// </summary>
        public decimal CalculateDistributableAmount()
        {
            if (ExpectedWinningBid.HasValue)
            {
                return ExpectedWinningBid.Value - (AuctionFees ?? 0);
            }
            return 0;
        }

        /// <summary>
        /// 선순위 공제 후 금액 계산
        /// </summary>
        public decimal CalculateAmountAfterSenior()
        {
            var distributable = CalculateDistributableAmount();
            var seniorTotal = CalculateSeniorRightsTotal();
            return Math.Max(0, distributable - seniorTotal);
        }

        /// <summary>
        /// 회수율 계산
        /// </summary>
        public decimal CalculateRecoveryRate(decimal loanCap)
        {
            if (loanCap <= 0) return 0;
            var amountAfterSenior = CalculateAmountAfterSenior();
            var recovery = Math.Min(amountAfterSenior, loanCap);
            return Math.Round(recovery / loanCap * 100, 2);
        }

        /// <summary>
        /// 위험도 레벨 열거형
        /// </summary>
        public RiskLevelEnum GetRiskLevel()
        {
            return RiskLevel?.ToLower() switch
            {
                "high" => RiskLevelEnum.High,
                "medium" => RiskLevelEnum.Medium,
                "low" => RiskLevelEnum.Low,
                _ => RiskLevelEnum.Unknown
            };
        }

        /// <summary>
        /// 경매 상태 열거형
        /// </summary>
        public AuctionStatusEnum GetAuctionStatus()
        {
            return AuctionStatus?.ToLower() switch
            {
                "opened" => AuctionStatusEnum.Opened,
                "not_opened" => AuctionStatusEnum.NotOpened,
                _ => AuctionStatusEnum.Unknown
            };
        }

        /// <summary>
        /// 채무자 유형 열거형
        /// </summary>
        public DebtorTypeEnum GetDebtorType()
        {
            return DebtorType?.ToLower() switch
            {
                "individual" => DebtorTypeEnum.Individual,
                "business" => DebtorTypeEnum.Business,
                "corporation" => DebtorTypeEnum.Corporation,
                _ => DebtorTypeEnum.Unknown
            };
        }
    }

    /// <summary>
    /// 위험도 레벨
    /// </summary>
    public enum RiskLevelEnum
    {
        Unknown,  // 미평가
        Low,      // 낮음 (🟢)
        Medium,   // 중간 (🟡)
        High      // 높음 (🔴)
    }

    /// <summary>
    /// 경매 상태
    /// </summary>
    public enum AuctionStatusEnum
    {
        Unknown,    // 미확인
        Opened,     // 경매개시
        NotOpened   // 경매미개시
    }

    /// <summary>
    /// 채무자 유형
    /// </summary>
    public enum DebtorTypeEnum
    {
        Unknown,     // 미확인
        Individual,  // 개인
        Business,    // 개인사업자
        Corporation  // 법인
    }
}

