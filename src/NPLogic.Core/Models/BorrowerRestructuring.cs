using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 회생 차주 상세 정보 모델
    /// </summary>
    public class BorrowerRestructuring
    {
        public Guid Id { get; set; }
        
        /// <summary>차주 ID (FK)</summary>
        public Guid BorrowerId { get; set; }

        /// <summary>인가/미인가</summary>
        public string? ApprovalStatus { get; set; }

        /// <summary>세부진행단계</summary>
        public string? ProgressStage { get; set; }

        /// <summary>관할법원</summary>
        public string? CourtName { get; set; }

        /// <summary>회생사건번호</summary>
        public string? CaseNumber { get; set; }

        /// <summary>회생신청일</summary>
        public DateTime? FilingDate { get; set; }

        /// <summary>보전처분일</summary>
        public DateTime? PreservationDate { get; set; }

        /// <summary>개시결정일</summary>
        public DateTime? CommencementDate { get; set; }

        /// <summary>채권신고일</summary>
        public DateTime? ClaimFilingDate { get; set; }

        /// <summary>인가/폐지결정일</summary>
        public DateTime? ApprovalDismissalDate { get; set; }

        /// <summary>회생탈락권</summary>
        public string? ExcludedClaim { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ========== 헬퍼 메서드 ==========

        /// <summary>
        /// 인가 여부
        /// </summary>
        public bool IsApproved => ApprovalStatus?.Contains("인가") == true && 
                                  !ApprovalStatus.Contains("미인가");

        /// <summary>
        /// 진행 상태 요약
        /// </summary>
        public string GetProgressSummary()
        {
            if (string.IsNullOrEmpty(ProgressStage))
                return ApprovalStatus ?? "진행중";

            return $"{ApprovalStatus} - {ProgressStage}";
        }

        /// <summary>
        /// 법원 및 사건번호 표시
        /// </summary>
        public string GetCaseDisplay()
        {
            if (string.IsNullOrEmpty(CourtName) && string.IsNullOrEmpty(CaseNumber))
                return "-";

            return $"{CourtName} {CaseNumber}".Trim();
        }
    }
}

