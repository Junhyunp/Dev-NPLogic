using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 작업 이력 로그
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string? TableName { get; set; }
        public Guid? RecordId { get; set; }
        public string? Action { get; set; } // INSERT, UPDATE, DELETE
        public string? OldData { get; set; } // JSON
        public string? NewData { get; set; } // JSON
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 액션 한글 표시
        /// </summary>
        public string ActionDisplay => Action switch
        {
            "INSERT" => "생성",
            "UPDATE" => "수정",
            "DELETE" => "삭제",
            _ => Action ?? ""
        };

        /// <summary>
        /// 테이블 한글 표시
        /// </summary>
        public string TableNameDisplay => TableName switch
        {
            "properties" => "물건",
            "borrowers" => "차주",
            "loans" => "대출",
            "users" => "사용자",
            "evaluations" => "평가",
            "right_analysis" => "권리분석",
            "registry_documents" => "등기문서",
            "registry_rights" => "등기권리",
            "auction_schedules" => "경매일정",
            "public_sale_schedules" => "공매일정",
            "settings" => "설정",
            "calculation_formulas" => "계산수식",
            "courts" => "법원",
            "common_codes" => "공통코드",
            _ => TableName ?? ""
        };

        /// <summary>
        /// 변경 요약
        /// </summary>
        public string ChangeSummary
        {
            get
            {
                if (Action == "INSERT")
                    return "새 레코드 생성";
                if (Action == "DELETE")
                    return "레코드 삭제";
                return "레코드 수정";
            }
        }
    }
}

