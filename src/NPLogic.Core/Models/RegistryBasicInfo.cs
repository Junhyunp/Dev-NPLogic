using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// reference/Auction-Certificate의 basic_info.csv에 대응하는 1행 요약 정보
    /// (세트(RegistryRun)당 1행)
    /// </summary>
    public class RegistryBasicInfo
    {
        public Guid Id { get; set; }
        public Guid RegistryRunId { get; set; }
        public Guid PropertyId { get; set; }

        public string? JibeonId { get; set; } // 지번일련번호
        public string? RegistryAddress { get; set; } // 물건지 (등기부등본)
        public string? DdAddress { get; set; } // 물건지 (DD)
        public bool? IsAddressMatched { get; set; } // 일치여부
        public string? CollateralType { get; set; } // 담보물형태
        public decimal? LandAreaPyeong { get; set; } // 대지면적 (평)
        public decimal? BuildingAreaPyeong { get; set; } // 건물면적 (평)

        public string? OwnerName { get; set; } // 소유자
        public string? OwnerRegNo { get; set; } // 등록번호
        public string? ShareRatio { get; set; } // 최종지분
        public string? OwnerAddress { get; set; } // 소유자 주소

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

