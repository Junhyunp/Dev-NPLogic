using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 임대 분석 모델 (evaluation_rental_analysis 테이블)
    /// 임대호가분석, 무상임대분석, 수익가치, 탐문결과 등을 저장
    /// </summary>
    [Table("evaluation_rental_analysis")]
    public class EvaluationRentalAnalysis : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }
        
        [Column("evaluation_id")]
        public Guid EvaluationId { get; set; }
        
        /// <summary>
        /// 분석 유형: rental_quote (임대호가분석), free_rent (무상임대), income_value (수익가치), inquiry (탐문결과)
        /// </summary>
        [Column("analysis_type")]
        public string AnalysisType { get; set; } = "";
        
        /// <summary>
        /// 정렬 순서
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }
        
        /// <summary>
        /// 항목명 (예: 보증금, 월임대료, 관리비 등)
        /// </summary>
        [Column("item_name")]
        public string? ItemName { get; set; }
        
        /// <summary>
        /// 문자열 값
        /// </summary>
        [Column("string_value")]
        public string? StringValue { get; set; }
        
        /// <summary>
        /// 숫자 값 1
        /// </summary>
        [Column("numeric_value_1")]
        public decimal? NumericValue1 { get; set; }
        
        /// <summary>
        /// 숫자 값 2
        /// </summary>
        [Column("numeric_value_2")]
        public decimal? NumericValue2 { get; set; }
        
        /// <summary>
        /// 숫자 값 3
        /// </summary>
        [Column("numeric_value_3")]
        public decimal? NumericValue3 { get; set; }
        
        /// <summary>
        /// 면적 (㎡)
        /// </summary>
        [Column("area_sqm")]
        public decimal? AreaSqm { get; set; }
        
        /// <summary>
        /// 단가 (원/㎡)
        /// </summary>
        [Column("unit_price")]
        public decimal? UnitPrice { get; set; }
        
        /// <summary>
        /// 비율/율
        /// </summary>
        [Column("rate")]
        public decimal? Rate { get; set; }
        
        /// <summary>
        /// 비고/메모
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }
        
        /// <summary>
        /// 출처
        /// </summary>
        [Column("source")]
        public string? Source { get; set; }
        
        /// <summary>
        /// 기준일
        /// </summary>
        [Column("reference_date")]
        public DateTime? ReferenceDate { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
