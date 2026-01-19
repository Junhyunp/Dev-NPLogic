using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 상권정보 및 임대동향 모델 (evaluation_commercial_data 테이블)
    /// </summary>
    [Table("evaluation_commercial_data")]
    public class EvaluationCommercialData : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }
        
        [Column("evaluation_id")]
        public Guid EvaluationId { get; set; }
        
        // 상권분석 - 유동인구
        
        /// <summary>
        /// 일평균 유동인구
        /// </summary>
        [Column("daily_foot_traffic")]
        public int? DailyFootTraffic { get; set; }
        
        /// <summary>
        /// 주중 유동인구
        /// </summary>
        [Column("weekday_traffic")]
        public int? WeekdayTraffic { get; set; }
        
        /// <summary>
        /// 주말 유동인구
        /// </summary>
        [Column("weekend_traffic")]
        public int? WeekendTraffic { get; set; }
        
        /// <summary>
        /// 주요 연령대
        /// </summary>
        [Column("main_age_group")]
        public string? MainAgeGroup { get; set; }
        
        // 상권분석 - 매출
        
        /// <summary>
        /// 일평균 매출 (천원)
        /// </summary>
        [Column("daily_sales_1k")]
        public decimal? DailySales1k { get; set; }
        
        /// <summary>
        /// 월평균 매출 (천원)
        /// </summary>
        [Column("monthly_sales_1k")]
        public decimal? MonthlySales1k { get; set; }
        
        /// <summary>
        /// 객단가 (원)
        /// </summary>
        [Column("avg_transaction")]
        public decimal? AvgTransaction { get; set; }
        
        // 상권분석 - 점포
        
        /// <summary>
        /// 동일업종 점포수
        /// </summary>
        [Column("same_industry_stores")]
        public int? SameIndustryStores { get; set; }
        
        /// <summary>
        /// 폐업률 (%)
        /// </summary>
        [Column("closure_rate")]
        public decimal? ClosureRate { get; set; }
        
        /// <summary>
        /// 생존기간 평균 (개월)
        /// </summary>
        [Column("avg_survival_months")]
        public int? AvgSurvivalMonths { get; set; }
        
        // 임대동향
        
        /// <summary>
        /// 권역 보증금 하한 (천원)
        /// </summary>
        [Column("area_deposit_low")]
        public decimal? AreaDepositLow { get; set; }
        
        /// <summary>
        /// 권역 보증금 상한 (천원)
        /// </summary>
        [Column("area_deposit_high")]
        public decimal? AreaDepositHigh { get; set; }
        
        /// <summary>
        /// 권역 월임대료 하한 (천원)
        /// </summary>
        [Column("area_monthly_rent_low")]
        public decimal? AreaMonthlyRentLow { get; set; }
        
        /// <summary>
        /// 권역 월임대료 상한 (천원)
        /// </summary>
        [Column("area_monthly_rent_high")]
        public decimal? AreaMonthlyRentHigh { get; set; }
        
        /// <summary>
        /// 층별효용비율: 지하층 (%)
        /// </summary>
        [Column("floor_utility_basement")]
        public decimal? FloorUtilityBasement { get; set; }
        
        /// <summary>
        /// 층별효용비율: 1층 (%)
        /// </summary>
        [Column("floor_utility_1f")]
        public decimal? FloorUtility1F { get; set; }
        
        /// <summary>
        /// 층별효용비율: 2층 (%)
        /// </summary>
        [Column("floor_utility_2f")]
        public decimal? FloorUtility2F { get; set; }
        
        /// <summary>
        /// 층별효용비율: 3층 이상 (%)
        /// </summary>
        [Column("floor_utility_3f_up")]
        public decimal? FloorUtility3FUp { get; set; }
        
        /// <summary>
        /// 적용 층수
        /// </summary>
        [Column("applied_floor")]
        public string? AppliedFloor { get; set; }
        
        /// <summary>
        /// 적용 층별효용비율 (%)
        /// </summary>
        [Column("applied_floor_utility")]
        public decimal? AppliedFloorUtility { get; set; }
        
        /// <summary>
        /// 상권지도 캡처 URL
        /// </summary>
        [Column("commercial_map_url")]
        public string? CommercialMapUrl { get; set; }
        
        /// <summary>
        /// 데이터 출처
        /// </summary>
        [Column("data_source")]
        public string? DataSource { get; set; }
        
        /// <summary>
        /// 기준일
        /// </summary>
        [Column("reference_date")]
        public DateTime? ReferenceDate { get; set; }
        
        /// <summary>
        /// 메모
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
