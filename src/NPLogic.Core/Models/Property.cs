using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 물건 모델
    /// </summary>
    public class Property
    {
        public Guid Id { get; set; }

        public string? ProjectId { get; set; }

        /// <summary>
        /// 프로그램 ID (programs 테이블 FK)
        /// </summary>
        public Guid? ProgramId { get; set; }

        /// <summary>
        /// 차주 ID (borrowers 테이블 FK)
        /// </summary>
        public Guid? BorrowerId { get; set; }

        public string? PropertyNumber { get; set; }

        public string? PropertyType { get; set; }

        // 주소 정보
        public string? AddressFull { get; set; }

        public string? AddressRoad { get; set; }

        public string? AddressJibun { get; set; }

        public string? AddressDetail { get; set; }

        // 기본 정보
        public decimal? LandArea { get; set; }

        public decimal? BuildingArea { get; set; }

        public string? Floors { get; set; }

        public DateTime? CompletionDate { get; set; }

        // 가격 정보
        public decimal? AppraisalValue { get; set; }

        public decimal? MinimumBid { get; set; }

        public decimal? SalePrice { get; set; }

        /// <summary>
        /// OPB (Outstanding Principal Balance, 대출잔액)
        /// Phase 6.4: 물건에 OPB 컬럼 추가
        /// </summary>
        public decimal? Opb { get; set; }

        // 위치 정보
        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        // 상태
        public string Status { get; set; } = "pending";

        // 담당자
        public Guid? AssignedTo { get; set; }

        // ========== 대시보드 진행 관리 필드 ==========
        
        /// <summary>차주번호 (엑셀 업로드시 저장, 예: R-003)</summary>
        public string? BorrowerNumber { get; set; }

        /// <summary>차주명</summary>
        public string? DebtorName { get; set; }

        /// <summary>담보번호</summary>
        public string? CollateralNumber { get; set; }

        /// <summary>약정서 제출 여부</summary>
        public bool AgreementDoc { get; set; }

        /// <summary>보증서 제출 여부</summary>
        public bool GuaranteeDoc { get; set; }

        /// <summary>경개개시여부 1</summary>
        public string? AuctionStart1 { get; set; }

        /// <summary>경개개시여부 2</summary>
        public string? AuctionStart2 { get; set; }

        /// <summary>경매열람자료 확보 여부</summary>
        public bool AuctionDocs { get; set; }

        /// <summary>전입/상가임대차 열람 완료 여부</summary>
        public bool TenantDocs { get; set; }

        /// <summary>선순위검토 완료 여부</summary>
        public bool SeniorRightsReview { get; set; }

        /// <summary>평가액확정 여부</summary>
        public bool AppraisalConfirmed { get; set; }

        /// <summary>
        /// 상권 데이터 확보 여부 (상가/아파트형공장 전용)
        /// Phase 6.5: 자본소득률 → 상가/아파트형공장 상권 데이터 체크박스
        /// </summary>
        public bool HasCommercialDistrictData { get; set; }

        /// <summary>경(공)매일정</summary>
        public DateTime? AuctionScheduleDate { get; set; }

        /// <summary>QA 미회신 개수</summary>
        public int QaUnansweredCount { get; set; }

        /// <summary>권리분석 상태 (pending/processing/completed)</summary>
        public string RightsAnalysisStatus { get; set; } = "pending";

        /// <summary>Interim 완료 여부</summary>
        public bool InterimCompleted { get; set; }

        /// <summary>전체 진행 상태 (pending/processing/completed)</summary>
        public string OverallStatus => Status;

        // 메타데이터
        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // ========== 진행률 계산 메서드 ==========

        /// <summary>
        /// 전체 진행률 계산 (체크박스 기준)
        /// </summary>
        public int GetProgressPercent()
        {
            int completed = 0;
            int total = 8; // 체크박스 총 개수

            if (AgreementDoc) completed++;
            if (GuaranteeDoc) completed++;
            if (AuctionDocs) completed++;
            if (TenantDocs) completed++;
            if (SeniorRightsReview) completed++;
            if (AppraisalConfirmed) completed++;
            if (!string.IsNullOrEmpty(AuctionStart1)) completed++;
            if (RightsAnalysisStatus == "completed") completed++;

            return (int)Math.Round((double)completed / total * 100);
        }

        /// <summary>
        /// 권리분석 상태 열거형
        /// </summary>
        public RightsAnalysisStatusEnum GetRightsAnalysisStatus()
        {
            return RightsAnalysisStatus?.ToLower() switch
            {
                "pending" => RightsAnalysisStatusEnum.Pending,
                "processing" => RightsAnalysisStatusEnum.Processing,
                "completed" => RightsAnalysisStatusEnum.Completed,
                _ => RightsAnalysisStatusEnum.Pending
            };
        }

        /// <summary>
        /// 물건 상태 열거형
        /// </summary>
        public PropertyStatus GetStatus()
        {
            return Status.ToLower() switch
            {
                "pending" => PropertyStatus.Pending,
                "processing" => PropertyStatus.Processing,
                "completed" => PropertyStatus.Completed,
                _ => PropertyStatus.Pending
            };
        }

        /// <summary>
        /// 물건 유형 열거형
        /// </summary>
        public PropertyType GetPropertyType()
        {
            if (string.IsNullOrWhiteSpace(PropertyType))
                return Models.PropertyType.Other;

            return PropertyType.ToLower() switch
            {
                "아파트" or "apartment" => Models.PropertyType.Apartment,
                "상가" or "store" or "commercial" => Models.PropertyType.Commercial,
                "토지" or "land" => Models.PropertyType.Land,
                "빌라" or "villa" => Models.PropertyType.Villa,
                "오피스텔" or "officetel" => Models.PropertyType.Officetel,
                "단독주택" or "house" => Models.PropertyType.House,
                "다가구주택" or "multi-family" => Models.PropertyType.MultiFamily,
                "공장" or "factory" => Models.PropertyType.Factory,
                _ => Models.PropertyType.Other
            };
        }

        /// <summary>
        /// 주소 표시용 (짧은 버전)
        /// </summary>
        public string GetShortAddress()
        {
            if (!string.IsNullOrWhiteSpace(AddressRoad))
            {
                // 도로명주소에서 시/도, 시/군/구까지만 추출
                var parts = AddressRoad.Split(' ');
                if (parts.Length >= 2)
                    return $"{parts[0]} {parts[1]}";
                return AddressRoad;
            }

            if (!string.IsNullOrWhiteSpace(AddressFull))
            {
                var parts = AddressFull.Split(' ');
                if (parts.Length >= 2)
                    return $"{parts[0]} {parts[1]}";
                return AddressFull;
            }

            return "주소 없음";
        }

        /// <summary>
        /// 총 면적 (토지 + 건물)
        /// </summary>
        public decimal? GetTotalArea()
        {
            return (LandArea ?? 0) + (BuildingArea ?? 0);
        }

        /// <summary>
        /// 감정가 대비 최저입찰가 비율 (%)
        /// </summary>
        public decimal? GetBidRatio()
        {
            if (AppraisalValue.HasValue && AppraisalValue.Value > 0 && MinimumBid.HasValue)
            {
                return Math.Round((MinimumBid.Value / AppraisalValue.Value) * 100, 2);
            }
            return null;
        }
    }

    /// <summary>
    /// 물건 상태
    /// </summary>
    public enum PropertyStatus
    {
        Pending,      // 대기
        Processing,   // 진행중
        Completed     // 완료
    }

    /// <summary>
    /// 물건 유형
    /// </summary>
    public enum PropertyType
    {
        Apartment,    // 아파트
        Commercial,   // 상가
        Land,         // 토지
        Villa,        // 빌라
        Officetel,    // 오피스텔
        House,        // 단독주택
        MultiFamily,  // 다가구주택
        Factory,      // 공장
        Other         // 기타
    }

    /// <summary>
    /// 권리분석 상태
    /// </summary>
    public enum RightsAnalysisStatusEnum
    {
        Pending,      // 대기
        Processing,   // 진행중
        Completed     // 완료
    }
}

