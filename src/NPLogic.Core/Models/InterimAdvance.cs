using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// Interim 추가 가지급금 모델
    /// Cut-off 이후 발생한 추가 비용 (경매비용, 보험료 등)
    /// </summary>
    public class InterimAdvance
    {
        public Guid Id { get; set; }

        /// <summary>프로그램 ID</summary>
        public Guid? ProgramId { get; set; }

        /// <summary>차주일련번호 (R-0004)</summary>
        public string? BorrowerNumber { get; set; }

        /// <summary>차주명</summary>
        public string? BorrowerName { get; set; }

        /// <summary>Pool (A, B, C)</summary>
        public string? Pool { get; set; }

        /// <summary>채권구분 (R=Regular, S=Special)</summary>
        public string? LoanType { get; set; }

        /// <summary>계좌일련번호</summary>
        public string? AccountSerial { get; set; }

        /// <summary>계좌번호</summary>
        public string? AccountNumber { get; set; }

        /// <summary>가지급비용종류</summary>
        public string? ExpenseType { get; set; }

        /// <summary>거래일자</summary>
        public DateTime? TransactionDate { get; set; }

        /// <summary>통화 (기본: KRW)</summary>
        public string Currency { get; set; } = "KRW";

        /// <summary>지급거래금액</summary>
        public decimal? Amount { get; set; }

        /// <summary>적요</summary>
        public string? Description { get; set; }

        /// <summary>비고</summary>
        public string? Notes { get; set; }

        /// <summary>업로드한 사용자 ID</summary>
        public Guid? UploadedBy { get; set; }

        /// <summary>생성일시</summary>
        public DateTime CreatedAt { get; set; }

        // ========== 헬퍼 메서드 ==========

        /// <summary>
        /// 금액 포맷팅 (천원 단위)
        /// </summary>
        public string GetAmountDisplay()
        {
            return Amount.HasValue ? $"{Amount.Value:N0}원" : "-";
        }

        /// <summary>
        /// 거래일자 포맷팅
        /// </summary>
        public string GetTransactionDateDisplay()
        {
            return TransactionDate.HasValue ? TransactionDate.Value.ToString("yyyy-MM-dd") : "-";
        }

        /// <summary>
        /// Pool + 채권구분 표시
        /// </summary>
        public string GetPoolLoanTypeDisplay()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Pool)) parts.Add(Pool);
            if (!string.IsNullOrEmpty(LoanType)) parts.Add(LoanType);
            return parts.Count > 0 ? string.Join("-", parts) : "-";
        }
    }
}
