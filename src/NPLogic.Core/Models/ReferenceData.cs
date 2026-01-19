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

    /// <summary>
    /// 계산 수식 설정
    /// </summary>
    public class CalculationFormula
    {
        public Guid Id { get; set; }
        public string FormulaName { get; set; } = "";
        public string FormulaExpression { get; set; } = "";
        public string? FormulaDescription { get; set; }
        public string? AppliesTo { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 데이터 매핑 설정
    /// </summary>
    public class DataMapping
    {
        public Guid Id { get; set; }
        public string SettingKey { get; set; } = "";
        public string? SettingValue { get; set; }
        public string? SettingType { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 법률적용률 설정 (C-002)
    /// </summary>
    public class LegalApplicationRate
    {
        public Guid Id { get; set; }
        public string PropertyType { get; set; } = ""; // 아파트, 연립, 공장 등
        public decimal AppliedRate { get; set; } // 법률적용률 (%)
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 임대차 기준 (C-003, C-004)
    /// </summary>
    public class LeaseStandard
    {
        public Guid Id { get; set; }
        public string LeaseType { get; set; } = "residential"; // residential, commercial
        public string Region { get; set; } = ""; // 서울, 수도권, 광역시, 기타
        public DateTime StartDate { get; set; } // 적용 시작일
        public DateTime? EndDate { get; set; } // 적용 종료일
        public decimal DepositLimit { get; set; } // 적용 보증금 범위 (이하)
        public decimal CompensationAmount { get; set; } // 최우선 변제금액
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 경매비용 산정 기준 (C-005)
    /// </summary>
    public class AuctionCostStandard
    {
        public Guid Id { get; set; }
        public string CostType { get; set; } = ""; // 송달료, 감정료, 매각수수료 등
        public string? CalculationMethod { get; set; } // 고정, 비율, 복합
        public decimal? BaseAmount { get; set; }
        public decimal? Rate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 공매비용 산정 기준 (C-006)
    /// </summary>
    public class PublicSaleCostStandard
    {
        public Guid Id { get; set; }
        public string CostType { get; set; } = ""; // 온비드 수수료, 감정료 등
        public string? CalculationMethod { get; set; } // 고정, 비율, 복합
        public decimal? BaseAmount { get; set; }
        public decimal? Rate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

