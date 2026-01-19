using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 기계기구 목록
    /// </summary>
    [Table("evaluation_machinery")]
    public class EvaluationMachinery : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Column("evaluation_id")]
        public Guid EvaluationId { get; set; }
        
        /// <summary>
        /// 번호
        /// </summary>
        [Column("item_number")]
        public int? ItemNumber { get; set; }
        
        /// <summary>
        /// 기계기구명
        /// </summary>
        [Column("machinery_name")]
        public string? MachineryName { get; set; }
        
        /// <summary>
        /// 제조사
        /// </summary>
        [Column("manufacturer")]
        public string? Manufacturer { get; set; }
        
        /// <summary>
        /// 제작일자
        /// </summary>
        [Column("manufacture_date")]
        public DateTime? ManufactureDate { get; set; }
        
        /// <summary>
        /// 수량
        /// </summary>
        [Column("quantity")]
        public int Quantity { get; set; } = 1;
        
        /// <summary>
        /// 단가
        /// </summary>
        [Column("unit_price")]
        public decimal? UnitPrice { get; set; }
        
        /// <summary>
        /// 감정가
        /// </summary>
        [Column("appraisal_value")]
        public decimal? AppraisalValue { get; set; }
        
        /// <summary>
        /// 공장저당1호 포함 여부
        /// </summary>
        [Column("factory_mortgage_1")]
        public bool FactoryMortgage1 { get; set; }
        
        /// <summary>
        /// 공장저당2호 포함 여부
        /// </summary>
        [Column("factory_mortgage_2")]
        public bool FactoryMortgage2 { get; set; }
        
        /// <summary>
        /// 공장저당3호 포함 여부
        /// </summary>
        [Column("factory_mortgage_3")]
        public bool FactoryMortgage3 { get; set; }
        
        /// <summary>
        /// 공장저당4호 포함 여부
        /// </summary>
        [Column("factory_mortgage_4")]
        public bool FactoryMortgage4 { get; set; }
        
        /// <summary>
        /// 기계인정율 (기본 20%)
        /// </summary>
        [Column("recognition_rate")]
        public decimal RecognitionRate { get; set; } = 0.2m;
        
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
