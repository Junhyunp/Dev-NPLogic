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

        // 위치 정보
        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        // 상태
        public string Status { get; set; } = "pending";

        // 담당자
        public Guid? AssignedTo { get; set; }

        // 메타데이터
        public Guid? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

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
}

