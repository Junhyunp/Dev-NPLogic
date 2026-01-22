using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 차주 모델
    /// </summary>
    public class Borrower
    {
        public Guid Id { get; set; }

        /// <summary>차주번호</summary>
        public string BorrowerNumber { get; set; } = "";

        /// <summary>차주명</summary>
        public string BorrowerName { get; set; } = "";

        /// <summary>차주유형 (개인, 개인사업자, 법인)</summary>
        public string BorrowerType { get; set; } = "개인";

        /// <summary>사업자번호</summary>
        public string? BusinessNumber { get; set; }

        /// <summary>자산유형</summary>
        public string? AssetType { get; set; }

        /// <summary>관련차주</summary>
        public string? RelatedBorrower { get; set; }

        // ========== 요약 통계 ==========

        /// <summary>물건갯수</summary>
        public int PropertyCount { get; set; }

        /// <summary>OPB (Outstanding Principal Balance)</summary>
        public decimal Opb { get; set; }

        /// <summary>근저당설정액</summary>
        public decimal MortgageAmount { get; set; }

        /// <summary>미상환원금잔액</summary>
        public decimal? UnpaidPrincipal { get; set; }

        /// <summary>미수이자</summary>
        public decimal? AccruedInterest { get; set; }

        /// <summary>비고</summary>
        public string? Notes { get; set; }

        // ========== 상태 체크 ==========

        /// <summary>회생 (S 차주)</summary>
        public bool IsRestructuring { get; set; }

        /// <summary>개회</summary>
        public bool IsOpened { get; set; }

        /// <summary>차주사망</summary>
        public bool IsDeceased { get; set; }

        // ========== XNPV 결과 ==========

        /// <summary>XNPV 1안</summary>
        public decimal? XnpvScenario1 { get; set; }

        /// <summary>1안 Ratio</summary>
        public decimal? XnpvRatio1 { get; set; }

        /// <summary>XNPV 2안</summary>
        public decimal? XnpvScenario2 { get; set; }

        /// <summary>2안 Ratio</summary>
        public decimal? XnpvRatio2 { get; set; }

        // ========== 연락처 정보 ==========

        /// <summary>대표자</summary>
        public string? Representative { get; set; }

        /// <summary>전화번호</summary>
        public string? Phone { get; set; }

        /// <summary>이메일</summary>
        public string? Email { get; set; }

        /// <summary>주소</summary>
        public string? Address { get; set; }

        // ========== 프로그램 연결 ==========

        /// <summary>프로그램 ID</summary>
        public string? ProgramId { get; set; }

        // ========== 메타데이터 ==========

        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ========== 헬퍼 메서드 ==========

        /// <summary>
        /// 차주유형 열거형
        /// </summary>
        public BorrowerTypeEnum GetBorrowerType()
        {
            return BorrowerType switch
            {
                "개인" => BorrowerTypeEnum.Individual,
                "개인사업자" => BorrowerTypeEnum.SoleProprietor,
                "법인" => BorrowerTypeEnum.Corporation,
                _ => BorrowerTypeEnum.Individual
            };
        }

        /// <summary>
        /// 차주유형 표시 문자열
        /// </summary>
        public string GetBorrowerTypeDisplay()
        {
            return BorrowerType switch
            {
                "개인" => "개인",
                "개인사업자" => "개인사업자",
                "법인" => "법인",
                _ => BorrowerType ?? "개인"
            };
        }

        /// <summary>
        /// 사업자번호 포맷팅 (###-##-#####)
        /// </summary>
        public string GetFormattedBusinessNumber()
        {
            if (string.IsNullOrWhiteSpace(BusinessNumber))
                return "N/A";

            var digits = new string(BusinessNumber.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"{digits.Substring(0, 3)}-{digits.Substring(3, 2)}-{digits.Substring(5, 5)}";

            return BusinessNumber;
        }

        /// <summary>
        /// 상태 요약 문자열
        /// </summary>
        public string GetStatusSummary()
        {
            var statuses = new List<string>();
            if (IsRestructuring) statuses.Add("회생");
            if (IsOpened) statuses.Add("개회");
            if (IsDeceased) statuses.Add("사망");

            return statuses.Count > 0 ? string.Join(", ", statuses) : "정상";
        }

        /// <summary>
        /// XNPV Ratio 포맷팅 (%)
        /// </summary>
        public string GetXnpvRatio1Display()
        {
            return XnpvRatio1.HasValue ? $"{XnpvRatio1.Value:P2}" : "-";
        }

        public string GetXnpvRatio2Display()
        {
            return XnpvRatio2.HasValue ? $"{XnpvRatio2.Value:P2}" : "-";
        }

        /// <summary>
        /// 금액 포맷팅 (천원 단위)
        /// </summary>
        public string GetOpbDisplay()
        {
            return $"{Opb / 1000:N0}천원";
        }

        public string GetMortgageAmountDisplay()
        {
            return $"{MortgageAmount / 1000:N0}천원";
        }
    }

    /// <summary>
    /// 차주유형 열거형
    /// </summary>
    public enum BorrowerTypeEnum
    {
        Individual,      // 개인
        SoleProprietor,  // 개인사업자
        Corporation      // 법인
    }
}

