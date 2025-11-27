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

        // ========== 채권정보 ==========

        /// <summary>계좌일련번호 (A)</summary>
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

        /// <summary>대출원금잔액 (G)</summary>
        public decimal? LoanPrincipalBalance { get; set; }

        /// <summary>가지급금 (H)</summary>
        public decimal AdvancePayment { get; set; }

        /// <summary>미수이자 (I)</summary>
        public decimal AccruedInterest { get; set; }

        /// <summary>채권액 합계 (J)</summary>
        public decimal? TotalClaimAmount { get; set; }

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

        // ========== 담보 연결 ==========

        /// <summary>1순위 피담보 (N)</summary>
        public Guid? Collateral1Id { get; set; }

        /// <summary>2순위 피담보 (N2)</summary>
        public Guid? Collateral2Id { get; set; }

        /// <summary>3순위 피담보 (N3)</summary>
        public Guid? Collateral3Id { get; set; }

        // ========== 체크박스 상태 ==========

        /// <summary>약정서 확인</summary>
        public bool HasAgreementDoc { get; set; }

        /// <summary>경매비용, 대위등기비용</summary>
        public bool HasAuctionCost { get; set; }

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

        // ========== Loan Cap 계산 (시나리오 1) ==========

        /// <summary>1안 예상배당일 (DD1)</summary>
        public DateTime? ExpectedDividendDate1 { get; set; }

        /// <summary>1안 연체이자 (V)</summary>
        public decimal? OverdueInterest1 { get; set; }

        /// <summary>1안 Loan Cap (LC1)</summary>
        public decimal? LoanCap1 { get; set; }

        // ========== Loan Cap 계산 (시나리오 2) ==========

        /// <summary>2안 예상배당일 (DD2)</summary>
        public DateTime? ExpectedDividendDate2 { get; set; }

        /// <summary>2안 연체이자 (W)</summary>
        public decimal? OverdueInterest2 { get; set; }

        /// <summary>2안 Loan Cap (LC2)</summary>
        public decimal? LoanCap2 { get; set; }

        // ========== MCI 보증 정보 ==========

        /// <summary>MCI최초가입금액</summary>
        public decimal? MciInitialAmount { get; set; }

        /// <summary>MCI가입잔액</summary>
        public decimal? MciBalance { get; set; }

        /// <summary>보증서번호</summary>
        public string? MciGuaranteeNumber { get; set; }

        /// <summary>채권번호</summary>
        public string? MciBondNumber { get; set; }

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

