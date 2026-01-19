using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 평가 사례 정보 (사례1~4)
    /// </summary>
    [Table("evaluation_cases")]
    public class EvaluationCase : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Column("evaluation_id")]
        public Guid EvaluationId { get; set; }
        
        /// <summary>
        /// 사례 번호 (1~4)
        /// </summary>
        [Column("case_number")]
        public int CaseNumber { get; set; }
        
        /// <summary>
        /// 사례 구분 (낙찰, 본건낙찰, 매매, 직접입력)
        /// </summary>
        [Column("case_type")]
        public string? CaseType { get; set; }
        
        /// <summary>
        /// 경매사건번호
        /// </summary>
        [Column("auction_case_no")]
        public string? AuctionCaseNo { get; set; }
        
        /// <summary>
        /// 낙찰일자
        /// </summary>
        [Column("winning_date")]
        public DateTime? WinningDate { get; set; }
        
        /// <summary>
        /// 용도
        /// </summary>
        [Column("usage")]
        public string? Usage { get; set; }
        
        /// <summary>
        /// 소재지
        /// </summary>
        [Column("address")]
        public string? Address { get; set; }
        
        /// <summary>
        /// 토지면적(평)
        /// </summary>
        [Column("land_area_pyeong")]
        public decimal? LandAreaPyeong { get; set; }
        
        /// <summary>
        /// 건물연면적(평)
        /// </summary>
        [Column("building_area_pyeong")]
        public decimal? BuildingAreaPyeong { get; set; }
        
        /// <summary>
        /// 보존등기일
        /// </summary>
        [Column("preservation_date")]
        public DateTime? PreservationDate { get; set; }
        
        /// <summary>
        /// 사용승인일
        /// </summary>
        [Column("use_approval_date")]
        public DateTime? UseApprovalDate { get; set; }
        
        /// <summary>
        /// 법사가
        /// </summary>
        [Column("legal_price")]
        public decimal? LegalPrice { get; set; }
        
        /// <summary>
        /// 토지
        /// </summary>
        [Column("land_price")]
        public decimal? LandPrice { get; set; }
        
        /// <summary>
        /// 건물
        /// </summary>
        [Column("building_price")]
        public decimal? BuildingPrice { get; set; }
        
        /// <summary>
        /// 감평기준일자
        /// </summary>
        [Column("appraisal_date")]
        public DateTime? AppraisalDate { get; set; }
        
        /// <summary>
        /// 평당감정가(토지)
        /// </summary>
        [Column("land_unit_appraisal")]
        public decimal? LandUnitAppraisal { get; set; }
        
        /// <summary>
        /// 평당감정가(건물)
        /// </summary>
        [Column("building_unit_appraisal")]
        public decimal? BuildingUnitAppraisal { get; set; }
        
        /// <summary>
        /// 토지평당법사가
        /// </summary>
        [Column("land_unit_legal")]
        public decimal? LandUnitLegal { get; set; }
        
        /// <summary>
        /// 건물평당법사가
        /// </summary>
        [Column("building_unit_legal")]
        public decimal? BuildingUnitLegal { get; set; }
        
        /// <summary>
        /// 낙찰가액
        /// </summary>
        [Column("winning_price")]
        public decimal? WinningPrice { get; set; }
        
        /// <summary>
        /// 낙찰가율
        /// </summary>
        [Column("winning_rate")]
        public decimal? WinningRate { get; set; }
        
        /// <summary>
        /// 낙찰회차
        /// </summary>
        [Column("winning_round")]
        public int? WinningRound { get; set; }
        
        /// <summary>
        /// 평당낙찰가(토지)
        /// </summary>
        [Column("land_unit_winning")]
        public decimal? LandUnitWinning { get; set; }
        
        /// <summary>
        /// 평당낙찰가(건물)
        /// </summary>
        [Column("building_unit_winning")]
        public decimal? BuildingUnitWinning { get; set; }
        
        /// <summary>
        /// 토지평당낙찰가**
        /// </summary>
        [Column("land_unit_winning_adj")]
        public decimal? LandUnitWinningAdj { get; set; }
        
        /// <summary>
        /// 건물평당낙찰가**
        /// </summary>
        [Column("building_unit_winning_adj")]
        public decimal? BuildingUnitWinningAdj { get; set; }
        
        /// <summary>
        /// 용적율
        /// </summary>
        [Column("floor_area_ratio")]
        public decimal? FloorAreaRatio { get; set; }
        
        /// <summary>
        /// 2등 입찰가
        /// </summary>
        [Column("second_bid_price")]
        public decimal? SecondBidPrice { get; set; }
        
        /// <summary>
        /// 사례 비고 사항
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }
        
        /// <summary>
        /// 위도
        /// </summary>
        [Column("latitude")]
        public decimal? Latitude { get; set; }
        
        /// <summary>
        /// 경도
        /// </summary>
        [Column("longitude")]
        public decimal? Longitude { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
