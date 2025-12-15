using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 프로그램-사용자 매핑 모델
    /// </summary>
    public class ProgramUser
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 프로그램 ID
        /// </summary>
        public Guid ProgramId { get; set; }

        /// <summary>
        /// 사용자 ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 역할 (pm: PM, member: 팀원)
        /// </summary>
        public string Role { get; set; } = "member";

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// PM 여부
        /// </summary>
        public bool IsPM => Role?.ToLower() == "pm";

        /// <summary>
        /// 팀원 여부
        /// </summary>
        public bool IsMember => Role?.ToLower() == "member";

        // Navigation properties (for joins)
        public Program? Program { get; set; }
        public User? User { get; set; }
    }

    /// <summary>
    /// 프로그램 내 역할
    /// </summary>
    public enum ProgramUserRole
    {
        PM,      // 프로젝트 매니저
        Member   // 팀원
    }
}

