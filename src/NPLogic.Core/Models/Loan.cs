using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 대출 모델
    /// </summary>
    public class Loan
    {
        public Guid Id { get; set; }
        public Guid? BorrowerId { get; set; }

        // ========== 채권정보 (기본) ==========

        /// <summary>채권관리번호 (A)</summary>
        public string? BondManagementNumber { get; set; }

        /// <summary>계좌일련번호</summary>
        public string? AccountSerial { get; set; }

        /// <summary>대출과목 (B)</summary>
        public string? LoanType { get; set; }

        /// <summary>대출종류 (C)</summary>
        public string? LoanCategory { get; set; }

        /// <summary>계좌번호 (D)</summary>
        public string? AccountNumber { get; set; }

        // ========== 대출 금액 ==========

        /// <summary>최초대출원금 (F)</summary>
        public decimal? InitialLoanAmount { get; set; }

        /// <summary>최초대출금전액</summary>
        public decimal? InitialLoanFullAmount { get; set; }

        /// <summary>대출원금잔액 (G/S)</summary>
        public decimal? LoanPrincipalBalance { get; set; }

        /// <summary>가지급금 (H)</summary>
        public decimal AdvancePayment { get; set; }

        /// <summary>미수이자 (I)</summary>
        public decimal AccruedInterest { get; set; }

        /// <summary>채권액 합계 (J)</summary>
        public decimal? TotalClaimAmount { get; set; }

        /// <summary>20년 채권잔액 (N1)</summary>
        public decimal? BondBalance20YearN1 { get; set; }

        /// <summary>20년 채권잔액 (N2)</summary>
        public decimal? BondBalance20YearN2 { get; set; }

        // ========== 날짜 ==========

        /// <summary>최초대출일 (E)</summary>
        public DateTime? InitialLoanDate { get; set; }

        /// <summary>최종이수일 (K)</summary>
        public DateTime? LastInterestDate { get; set; }

        // ========== 이자율 ==========

        /// <summary>정상이자율 (L)</summary>
        public decimal? NormalInterestRate { get; set; }

        /// <summary>연체이자율 (M)</summary>
        public decimal? OverdueInterestRate { get; set; }

        /// <summary>담보이자율</summary>
        public decimal? CollateralInterestRate { get; set; }

        /// <summary>이자율적용 (적용할 이자율)</summary>
        public decimal? AppliedInterestRate { get; set; }

        // ========== 담보 연결 ==========

        /// <summary>1순위 피담보 (N)</summary>
        public Guid? Collateral1Id { get; set; }

        /// <summary>2순위 피담보 (N2)</summary>
        public Guid? Collateral2Id { get; set; }

        /// <summary>3순위 피담보 (N3)</summary>
        public Guid? Collateral3Id { get; set; }

        // ========== 추가 정보 ==========

        /// <summary>MO대상 여부</summary>
        public bool IsMoTarget { get; set; }

        /// <summary>추심상태 (H)</summary>
        public string? CollectionStatus { get; set; }

        /// <summary>부서/성명 (담당자)</summary>
        public string? DepartmentPerson { get; set; }

        /// <summary>의율/담보/담보이자 (O)</summary>
        public string? RateCollateralInfo { get; set; }

        /// <summary>기 대출비율/담보이자 (Q)</summary>
        public decimal? PriorLoanRatio { get; set; }

        /// <summary>채권담보/담보이자 (R)</summary>
        public string? BondCollateralInfo { get; set; }

        // ========== UI 상태 (DB 저장 안함) ==========

        /// <summary>빈 행 여부 (UI 표시용, DB에 저장되지 않음)</summary>
        public bool IsEmptyRow { get; set; }

        // ========== 체크박스 상태 ==========

        /// <summary>약정서 확인</summary>
        public bool HasAgreementDoc { get; set; }

        /// <summary>경매신청서</summary>
        public bool HasAuctionApplication { get; set; }

        /// <summary>경매비용</summary>
        public bool HasAuctionCost { get; set; }

        /// <summary>대위등기비용</summary>
        public bool HasSubrogationRegistrationCost { get; set; }

        /// <summary>1순위 피담보(N)</summary>
        public bool HasCollateralPriority1 { get; set; }

        /// <summary>2순위 피담보(N2)</summary>
        public bool HasCollateralPriority2 { get; set; }

        /// <summary>3순위 피담보(N3)</summary>
        public bool HasCollateralPriority3 { get; set; }

        /// <summary>유효보증서여부 (O)</summary>
        public bool HasValidGuarantee { get; set; }

        /// <summary>기 대위변제 (P)</summary>
        public bool HasPriorSubrogation { get; set; }

        /// <summary>해지부보증여부 (Q)</summary>
        public bool IsTerminatedGuarantee { get; set; }

        /// <summary>MCI보증</summary>
        public bool HasMciGuarantee { get; set; }

        // ========== Interim 정보 ==========

        /// <summary>원금 상계 (R)</summary>
        public decimal PrincipalOffset { get; set; }

        /// <summary>원금회수 (S)</summary>
        public decimal PrincipalRecovery { get; set; }

        /// <summary>이자 상계 (T)</summary>
        public decimal InterestOffset { get; set; }

        /// <summary>이자회수 (U)</summary>
        public decimal InterestRecovery { get; set; }

        // ========== 보증서요약 ==========

        /// <summary>보증기관 (B)</summary>
        public string? GuaranteeOrganization { get; set; }

        /// <summary>보증번호 (N)</summary>
        public string? GuaranteeNumber { get; set; }

        /// <summary>보증가기관 코드</summary>
        public string? GuaranteeOrgCode { get; set; }

        /// <summary>해지유 배당자(현) (O)</summary>
        public string? TerminationDividendStatus { get; set; }

        /// <summary>대위변제금액 수수액 산정 (AC)</summary>
        public decimal? SubrogationAmountCalculation { get; set; }

        /// <summary>대위변제액 (AB)</summary>
        public decimal? SubrogationAmount { get; set; }

        /// <summary>선임료 (L)</summary>
        public decimal? AppointmentFee { get; set; }

        /// <summary>대위변제/채권 (AD)</summary>
        public decimal? SubrogationBondRatio { get; set; }

        /// <summary>약정이자 (AD)</summary>
        public decimal? AgreedInterest { get; set; }

        /// <summary>대위변제/채권도이자 (AD)</summary>
        public decimal? SubrogationBondInterest { get; set; }

        /// <summary>보증가능 여부</summary>
        public bool IsGuaranteeAvailable { get; set; }

        /// <summary>Cash In 반영 여부</summary>
        public bool IsCashInReflected { get; set; }

        // ========== Loan Cap 계산 (시나리오 1) ==========

        /// <summary>1안 예상배당일 (DD1)</summary>
        public DateTime? ExpectedDividendDate1 { get; set; }

        /// <summary>1안 예상배당가 (AB)</summary>
        public decimal? ExpectedDividendValue1 { get; set; }

        /// <summary>1안 연체이자 (AN)</summary>
        public decimal? OverdueInterest1 { get; set; }

        /// <summary>1안 채권이자</summary>
        public decimal? BondInterest1 { get; set; }

        /// <summary>1안 Loan Cap (LC1)</summary>
        public decimal? LoanCap1 { get; set; }

        /// <summary>1안 보증기준 Loan Cap (oLC1)</summary>
        public decimal? GuaranteeLoanCap1 { get; set; }

        /// <summary>1안 MO 필수 반영 여부</summary>
        public bool IsMoRequired1 { get; set; }

        // ========== Loan Cap 계산 (시나리오 2) ==========

        /// <summary>2안 예상배당일 (DD2)</summary>
        public DateTime? ExpectedDividendDate2 { get; set; }

        /// <summary>2안 예상배당가 수수료</summary>
        public decimal? ExpectedDividendFee2 { get; set; }

        /// <summary>2안 연체이자 (AN)</summary>
        public decimal? OverdueInterest2 { get; set; }

        /// <summary>2안 채권이자 (DD)</summary>
        public decimal? BondInterest2 { get; set; }

        /// <summary>2안 Loan Cap (LC2)</summary>
        public decimal? LoanCap2 { get; set; }

        /// <summary>2안 보증기준 Loan Cap (oLC2)</summary>
        public decimal? GuaranteeLoanCap2 { get; set; }

        /// <summary>2안 MO 필수 반영 여부</summary>
        public bool IsMoRequired2 { get; set; }

        // ========== MCI 보증 정보 ==========

        /// <summary>MCI최초가입금액</summary>
        public decimal? MciInitialAmount { get; set; }

        /// <summary>MCI가입잔액</summary>
        public decimal? MciBalance { get; set; }

        /// <summary>보증서번호</summary>
        public string? MciGuaranteeNumber { get; set; }

        /// <summary>채권번호</summary>
        public string? MciBondNumber { get; set; }

        /// <summary>MCI대출거래업체</summary>
        public string? MciLoanTrader { get; set; }

        /// <summary>MCI대출번호</summary>
        public string? MciLoanNumber { get; set; }

        /// <summary>MCI주요상품분류</summary>
        public string? MciProductCategory { get; set; }

        /// <summary>MCI최초시작일수</summary>
        public int? MciInitialDays { get; set; }

        /// <summary>MCI정상여부</summary>
        public bool IsMciNormal { get; set; }

        /// <summary>MCI청구지급율 (%)</summary>
        public decimal? MciClaimPaymentRate { get; set; }

        /// <summary>MCI채권팀전액</summary>
        public decimal? MciBondTeamAmount { get; set; }

        /// <summary>MCI대상여부</summary>
        public bool IsMciTarget { get; set; }

        // ========== 배당/대상금액 단물 (해지부보증용) ==========

        /// <summary>공제산정금액</summary>
        public decimal? DeductionCalculationAmount { get; set; }

        /// <summary>사례사건 회수</summary>
        public int? CaseCount { get; set; }

        /// <summary>1개월호 자보보증</summary>
        public decimal? SelfGuaranteeMonth1 { get; set; }

        /// <summary>2개월호 자보보증</summary>
        public decimal? SelfGuaranteeMonth2 { get; set; }

        /// <summary>3개월호 신규보증</summary>
        public decimal? NewGuaranteeMonth3 { get; set; }

        /// <summary>7개월호 신규보증</summary>
        public decimal? NewGuaranteeMonth7 { get; set; }

        // ========== 기타항목 및 변경 ==========

        /// <summary>기타항목 구분</summary>
        public string? OtherItemCategory { get; set; }

        /// <summary>기타항목 지급</summary>
        public decimal? OtherItemPayment { get; set; }

        /// <summary>기타항목 회수</summary>
        public decimal? OtherItemRecovery { get; set; }

        // ========== 기타 ==========

        /// <summary>비고</summary>
        public string? Notes { get; set; }

        // ========== 메타데이터 ==========

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ========== 계산 메서드 ==========

        /// <summary>
        /// 채권액 합계 계산 (G + H + I)
        /// </summary>
        public decimal CalculateTotalClaim()
        {
            return (LoanPrincipalBalance ?? 0) + AdvancePayment + AccruedInterest;
        }

        /// <summary>
        /// 연체이자 계산
        /// </summary>
        /// <param name="dividendDate">예상배당일</param>
        /// <returns>연체이자</returns>
        public decimal CalculateOverdueInterest(DateTime dividendDate)
        {
            if (!LastInterestDate.HasValue || !OverdueInterestRate.HasValue || !LoanPrincipalBalance.HasValue)
                return 0;

            var days = (dividendDate - LastInterestDate.Value).Days;
            if (days <= 0) return 0;

            return LoanPrincipalBalance.Value * OverdueInterestRate.Value * days / 365;
        }

        /// <summary>
        /// Loan Cap 계산 (유효보증서 N and 기대위변제 N인 경우)
        /// Loan Cap = 대출원금잔액 + 미수이자 + 연체이자 - 원금상계 - 원금회수 - 이자상계 - 이자회수
        /// </summary>
        /// <param name="overdueInterest">연체이자</param>
        /// <returns>Loan Cap</returns>
        public decimal CalculateLoanCap(decimal overdueInterest)
        {
            var loanCap = (LoanPrincipalBalance ?? 0) 
                          + AccruedInterest 
                          + overdueInterest
                          - PrincipalOffset 
                          - PrincipalRecovery 
                          - InterestOffset 
                          - InterestRecovery;

            return Math.Max(0, loanCap);
        }

        /// <summary>
        /// 시나리오 1 Loan Cap 계산 및 설정
        /// </summary>
        public void CalculateScenario1()
        {
            if (ExpectedDividendDate1.HasValue)
            {
                OverdueInterest1 = CalculateOverdueInterest(ExpectedDividendDate1.Value);
                LoanCap1 = CalculateLoanCap(OverdueInterest1 ?? 0);
            }
        }

        /// <summary>
        /// 시나리오 2 Loan Cap 계산 및 설정
        /// </summary>
        public void CalculateScenario2()
        {
            if (ExpectedDividendDate2.HasValue)
            {
                OverdueInterest2 = CalculateOverdueInterest(ExpectedDividendDate2.Value);
                LoanCap2 = CalculateLoanCap(OverdueInterest2 ?? 0);
            }
        }

        /// <summary>
        /// 금액 포맷팅 (천원 단위)
        /// </summary>
        public string GetLoanCapDisplay(int scenario)
        {
            var cap = scenario == 1 ? LoanCap1 : LoanCap2;
            return cap.HasValue ? $"{cap.Value:N0}원" : "-";
        }

        /// <summary>
        /// 이자율 표시 (%)
        /// </summary>
        public string GetNormalInterestRateDisplay()
        {
            return NormalInterestRate.HasValue ? $"{NormalInterestRate.Value:P2}" : "-";
        }

        public string GetOverdueInterestRateDisplay()
        {
            return OverdueInterestRate.HasValue ? $"{OverdueInterestRate.Value:P2}" : "-";
        }
    }
}

