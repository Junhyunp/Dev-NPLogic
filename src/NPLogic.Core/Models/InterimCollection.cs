using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// Interim 회수정보 모델
    /// Cut-off 이후 회수된 금액 (자진변제, 대위변제, 상계 등)
    /// </summary>
    public class InterimCollection
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

        /// <summary>회수일자</summary>
        public DateTime? CollectionDate { get; set; }

        /// <summary>회수원금</summary>
        public decimal? PrincipalAmount { get; set; }

        /// <summary>회수가지급금</summary>
        public decimal? AdvanceAmount { get; set; }

        /// <summary>회수이자</summary>
        public decimal? InterestAmount { get; set; }

        /// <summary>총회수액</summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>비고</summary>
        public string? Notes { get; set; }

        /// <summary>업로드한 사용자 ID</summary>
        public Guid? UploadedBy { get; set; }

        /// <summary>생성일시</summary>
        public DateTime CreatedAt { get; set; }

        // ========== 헬퍼 메서드 ==========

        /// <summary>
        /// 총회수액 포맷팅
        /// </summary>
        public string GetTotalAmountDisplay()
        {
            return TotalAmount.HasValue ? $"{TotalAmount.Value:N0}원" : "-";
        }

        /// <summary>
        /// 회수원금 포맷팅
        /// </summary>
        public string GetPrincipalAmountDisplay()
        {
            return PrincipalAmount.HasValue ? $"{PrincipalAmount.Value:N0}원" : "-";
        }

        /// <summary>
        /// 회수이자 포맷팅
        /// </summary>
        public string GetInterestAmountDisplay()
        {
            return InterestAmount.HasValue ? $"{InterestAmount.Value:N0}원" : "-";
        }

        /// <summary>
        /// 회수일자 포맷팅
        /// </summary>
        public string GetCollectionDateDisplay()
        {
            return CollectionDate.HasValue ? CollectionDate.Value.ToString("yyyy-MM-dd") : "-";
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
