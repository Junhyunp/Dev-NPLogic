using System;
using System.Text.Json;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 평가 정보 모델
    /// </summary>
    public class Evaluation
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        
        /// <summary>평가 유형 (아파트, 연립다세대, 공장창고, 상가, 주택토지)</summary>
        public string? EvaluationType { get; set; }
        
        /// <summary>시장가치 (시세)</summary>
        public decimal? MarketValue { get; set; }
        
        /// <summary>평가가치</summary>
        public decimal? EvaluatedValue { get; set; }
        
        /// <summary>회수율</summary>
        public decimal? RecoveryRate { get; set; }
        
        /// <summary>평가 상세 정보 (JSON)</summary>
        public string? EvaluationDetailsJson { get; set; }
        
        /// <summary>평가자 ID</summary>
        public Guid? EvaluatedBy { get; set; }
        
        /// <summary>평가 일시</summary>
        public DateTime? EvaluatedAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 계산된 속성
        public EvaluationDetails? EvaluationDetails
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EvaluationDetailsJson))
                    return null;
                try
                {
                    return JsonSerializer.Deserialize<EvaluationDetails>(EvaluationDetailsJson);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                EvaluationDetailsJson = value != null 
                    ? JsonSerializer.Serialize(value) 
                    : null;
            }
        }
    }

    /// <summary>
    /// 평가 상세 정보
    /// </summary>
    public class EvaluationDetails
    {
        // === 본건 감정가 정보 ===
        public decimal? AppraisalValue { get; set; }
        public decimal? LandAppraisalValue { get; set; }
        public decimal? BuildingAppraisalValue { get; set; }
        public decimal? MachineAppraisalValue { get; set; }
        public DateTime? AppraisalDate { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        
        // === 사례 정보 (최대 4개) ===
        public CaseInfo? Case1 { get; set; }
        public CaseInfo? Case2 { get; set; }
        public CaseInfo? Case3 { get; set; }
        public CaseInfo? Case4 { get; set; }
        
        // === 낙찰통계 ===
        public AuctionStatistics? RegionStats1Year { get; set; }
        public AuctionStatistics? RegionStats6Month { get; set; }
        public AuctionStatistics? RegionStats3Month { get; set; }
        public AuctionStatistics? DistrictStats1Year { get; set; }
        public AuctionStatistics? DistrictStats6Month { get; set; }
        public AuctionStatistics? DistrictStats3Month { get; set; }
        public AuctionStatistics? DongStats1Year { get; set; }
        public AuctionStatistics? DongStats6Month { get; set; }
        public AuctionStatistics? DongStats3Month { get; set; }
        
        /// <summary>적용 낙찰가율</summary>
        public decimal? AppliedBidRate { get; set; }
        
        // === 평가 결과 (시나리오별) ===
        public ScenarioResult? Scenario1 { get; set; }
        public ScenarioResult? Scenario2 { get; set; }
        
        // === 탐문 내역 (공장/상가용) ===
        public string? RealEstateName { get; set; }
        public string? RealEstateContact { get; set; }
        public string? InquiryContent { get; set; }
    }

    /// <summary>
    /// 사례 정보
    /// </summary>
    public class CaseInfo
    {
        /// <summary>사례 구분 (낙찰, 본건낙찰, 매매, 직접입력)</summary>
        public string? CaseType { get; set; }
        
        /// <summary>경매 사건번호</summary>
        public string? AuctionCaseNumber { get; set; }
        
        /// <summary>낙찰/매매 일자</summary>
        public DateTime? TransactionDate { get; set; }
        
        /// <summary>용도</summary>
        public string? Usage { get; set; }
        
        /// <summary>소재지</summary>
        public string? Address { get; set; }
        
        /// <summary>토지 면적 (평)</summary>
        public decimal? LandAreaPyeong { get; set; }
        
        /// <summary>건물 연면적 (평)</summary>
        public decimal? BuildingAreaPyeong { get; set; }
        
        /// <summary>기계기구 (공장용)</summary>
        public decimal? MachineValue { get; set; }
        
        /// <summary>보존등기일</summary>
        public DateTime? RegistrationDate { get; set; }
        
        /// <summary>사용승인일</summary>
        public DateTime? ApprovalDate { get; set; }
        
        /// <summary>법사가 (감정가)</summary>
        public decimal? AppraisalValue { get; set; }
        
        /// <summary>토지 감정가</summary>
        public decimal? LandAppraisalValue { get; set; }
        
        /// <summary>건물 감정가</summary>
        public decimal? BuildingAppraisalValue { get; set; }
        
        /// <summary>감평 기준일자</summary>
        public DateTime? AppraisalBaseDate { get; set; }
        
        /// <summary>낙찰가액</summary>
        public decimal? WinningBidAmount { get; set; }
        
        /// <summary>낙찰가율 (%)</summary>
        public decimal? WinningBidRate { get; set; }
        
        /// <summary>낙찰회차</summary>
        public int? AuctionRound { get; set; }
        
        /// <summary>평당 감정가 (토지)</summary>
        public decimal? LandPricePerPyeong { get; set; }
        
        /// <summary>평당 감정가 (건물)</summary>
        public decimal? BuildingPricePerPyeong { get; set; }
        
        /// <summary>평당 낙찰가 (토지)</summary>
        public decimal? LandBidPricePerPyeong { get; set; }
        
        /// <summary>평당 낙찰가 (건물)</summary>
        public decimal? BuildingBidPricePerPyeong { get; set; }
        
        /// <summary>용적률</summary>
        public decimal? FloorAreaRatio { get; set; }
        
        /// <summary>2등 입찰가</summary>
        public decimal? SecondBidAmount { get; set; }
        
        /// <summary>비고</summary>
        public string? Notes { get; set; }
        
        /// <summary>기계인정율 (공장용, 기본 20%)</summary>
        public decimal? MachineRecognitionRate { get; set; }
    }

    /// <summary>
    /// 낙찰통계
    /// </summary>
    public class AuctionStatistics
    {
        /// <summary>지역명</summary>
        public string? RegionName { get; set; }
        
        /// <summary>평균 낙찰가율 (%)</summary>
        public decimal? AverageBidRate { get; set; }
        
        /// <summary>낙찰 건수</summary>
        public int? BidCount { get; set; }
    }

    /// <summary>
    /// 시나리오별 평가 결과
    /// </summary>
    public class ScenarioResult
    {
        /// <summary>평가액</summary>
        public decimal? EvaluatedValue { get; set; }
        
        /// <summary>낙찰가율</summary>
        public decimal? BidRate { get; set; }
        
        /// <summary>토지 평당 낙찰가</summary>
        public decimal? LandPricePerPyeong { get; set; }
        
        /// <summary>건물 평당 낙찰가</summary>
        public decimal? BuildingPricePerPyeong { get; set; }
        
        /// <summary>평가 사유</summary>
        public string? EvaluationReason { get; set; }
        
        /// <summary>저감반영액 (공장용)</summary>
        public decimal? ReductionAmount { get; set; }
        
        // === 세부 항목 (공장/주택용) ===
        /// <summary>토지 평가액</summary>
        public decimal? LandValue { get; set; }
        
        /// <summary>토지(법면,도로 등) 평가액</summary>
        public decimal? LandOtherValue { get; set; }
        
        /// <summary>건물 평가액</summary>
        public decimal? BuildingValue { get; set; }
        
        /// <summary>기계 평가액</summary>
        public decimal? MachineValue { get; set; }
        
        /// <summary>합계</summary>
        public decimal? TotalValue { get; set; }
    }

    /// <summary>
    /// 평가 유형 열거형
    /// </summary>
    public enum EvaluationTypeEnum
    {
        /// <summary>아파트</summary>
        Apartment,
        
        /// <summary>연립다세대</summary>
        MultiFamily,
        
        /// <summary>공장/창고</summary>
        FactoryWarehouse,
        
        /// <summary>상가/아파트형공장</summary>
        CommercialFactory,
        
        /// <summary>주택/근린시설/토지</summary>
        HouseLand
    }
}

