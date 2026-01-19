using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 지번별 평가 (공장/창고용)
    /// </summary>
    [Table("evaluation_land_parcels")]
    public class EvaluationLandParcel : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Column("evaluation_id")]
        public Guid EvaluationId { get; set; }
        
        /// <summary>
        /// 지번일련번호 (예: R-035-1-1)
        /// </summary>
        [Column("parcel_number")]
        public string? ParcelNumber { get; set; }
        
        /// <summary>
        /// 구분 (토지/건물/기계기구)
        /// </summary>
        [Column("parcel_type")]
        public string ParcelType { get; set; } = "토지";
        
        /// <summary>
        /// 지번주소지
        /// </summary>
        [Column("address")]
        public string? Address { get; set; }
        
        /// <summary>
        /// 면적(평)
        /// </summary>
        [Column("area_pyeong")]
        public decimal? AreaPyeong { get; set; }
        
        /// <summary>
        /// 평당감정가
        /// </summary>
        [Column("unit_appraisal")]
        public decimal? UnitAppraisal { get; set; }
        
        /// <summary>
        /// 감정가
        /// </summary>
        [Column("appraisal_value")]
        public decimal? AppraisalValue { get; set; }
        
        /// <summary>
        /// 1안 평당 평가액
        /// </summary>
        [Column("scenario1_unit_price")]
        public decimal? Scenario1UnitPrice { get; set; }
        
        /// <summary>
        /// 1안 평가액
        /// </summary>
        [Column("scenario1_value")]
        public decimal? Scenario1Value { get; set; }
        
        /// <summary>
        /// 2안 평당 평가액
        /// </summary>
        [Column("scenario2_unit_price")]
        public decimal? Scenario2UnitPrice { get; set; }
        
        /// <summary>
        /// 2안 평가액
        /// </summary>
        [Column("scenario2_value")]
        public decimal? Scenario2Value { get; set; }
        
        /// <summary>
        /// 정렬 순서
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
