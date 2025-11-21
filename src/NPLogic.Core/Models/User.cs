using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 사용자 모델
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        public Guid? AuthUserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 역할 열거형
        /// </summary>
        public UserRole GetRole()
        {
            return Role.ToLower() switch
            {
                "pm" => UserRole.PM,
                "evaluator" => UserRole.Evaluator,
                "admin" => UserRole.Admin,
                _ => UserRole.Evaluator
            };
        }

        /// <summary>
        /// 상태 열거형
        /// </summary>
        public UserStatus GetStatus()
        {
            return Status.ToLower() switch
            {
                "active" => UserStatus.Active,
                "inactive" => UserStatus.Inactive,
                _ => UserStatus.Active
            };
        }

        /// <summary>
        /// 활성 사용자 여부
        /// </summary>
        public bool IsActive => Status.ToLower() == "active";

        /// <summary>
        /// 관리자 여부
        /// </summary>
        public bool IsAdmin => Role.ToLower() == "admin";

        /// <summary>
        /// PM 여부
        /// </summary>
        public bool IsPM => Role.ToLower() == "pm";

        /// <summary>
        /// 평가자 여부
        /// </summary>
        public bool IsEvaluator => Role.ToLower() == "evaluator";
    }

    /// <summary>
    /// 사용자 역할
    /// </summary>
    public enum UserRole
    {
        PM,         // 프로젝트 매니저
        Evaluator,  // 평가자 (회계사)
        Admin       // 관리자
    }

    /// <summary>
    /// 사용자 상태
    /// </summary>
    public enum UserStatus
    {
        Active,     // 활성
        Inactive    // 비활성
    }
}

