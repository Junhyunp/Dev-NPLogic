using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 신용보증서 정보
    /// </summary>
    public class CreditGuarantee
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 대출 ID (loans 테이블 FK)
        /// </summary>
        public Guid? LoanId { get; set; }

        /// <summary>
        /// 차주 ID (borrowers 테이블 FK)
        /// </summary>
        public Guid? BorrowerId { get; set; }

        /// <summary>
        /// 자산유형
        /// </summary>
        public string? AssetType { get; set; }

        /// <summary>
        /// 차주일련번호
        /// </summary>
        public string? BorrowerNumber { get; set; }

        /// <summary>
        /// 차주명
        /// </summary>
        public string? BorrowerName { get; set; }

        /// <summary>
        /// 계좌일련번호
        /// </summary>
        public string? AccountSerial { get; set; }

        /// <summary>
        /// 관련 대출채권 계좌번호
        /// </summary>
        public string? RelatedLoanAccountNumber { get; set; }

        /// <summary>
        /// 보증서번호
        /// </summary>
        public string? GuaranteeNumber { get; set; }

        /// <summary>
        /// 보증종류
        /// </summary>
        public string? GuaranteeType { get; set; }

        /// <summary>
        /// 보증기관
        /// </summary>
        public string? GuaranteeInstitution { get; set; }

        /// <summary>
        /// 보증비율
        /// </summary>
        public decimal? GuaranteeRatio { get; set; }

        /// <summary>
        /// 환산후 보증잔액
        /// </summary>
        public decimal? ConvertedGuaranteeBalance { get; set; }

        /// <summary>
        /// 보증금액
        /// </summary>
        public decimal? GuaranteeAmount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
