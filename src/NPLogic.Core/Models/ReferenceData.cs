using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 법원 정보
    /// </summary>
    public class Court
    {
        public Guid Id { get; set; }
        public string CourtCode { get; set; } = "";
        public string CourtName { get; set; } = "";
        public string? Region { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public decimal? DiscountRate1 { get; set; }
        public decimal? DiscountRate2 { get; set; }
        public decimal? DiscountRate3 { get; set; }
        public decimal? DiscountRate4 { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 금융기관 정보
    /// </summary>
    public class FinancialInstitution
    {
        public Guid Id { get; set; }
        public string InstitutionCode { get; set; } = "";
        public string InstitutionName { get; set; } = "";
        public string? InstitutionType { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? SwiftCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 감정평가기관 정보
    /// </summary>
    public class AppraisalFirm
    {
        public Guid Id { get; set; }
        public string FirmCode { get; set; } = "";
        public string FirmName { get; set; } = "";
        public string? Representative { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LicenseNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 공통 코드
    /// </summary>
    public class CommonCode
    {
        public Guid Id { get; set; }
        public string CodeGroup { get; set; } = "";
        public string CodeValue { get; set; } = "";
        public string CodeName { get; set; } = "";
        public int SortOrder { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 시스템 설정
    /// </summary>
    public class SystemSetting
    {
        public Guid Id { get; set; }
        public string SettingKey { get; set; } = "";
        public string? SettingValue { get; set; }
        public string SettingType { get; set; } = "STRING";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public T? GetTypedValue<T>()
        {
            if (string.IsNullOrEmpty(SettingValue))
                return default;

            try
            {
                return (T)Convert.ChangeType(SettingValue, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
}

