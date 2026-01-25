using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부등본정보 시트 데이터 모델 (데이터디스크 업로드용)
    /// </summary>
    public class RegistrySheetData
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 물건 ID (FK) - properties 테이블과 JOIN
        /// </summary>
        public Guid? PropertyId { get; set; }

        /// <summary>
        /// 차주일련번호
        /// </summary>
        public string? BorrowerNumber { get; set; }

        /// <summary>
        /// 차주명
        /// </summary>
        public string? BorrowerName { get; set; }

        /// <summary>
        /// 물건번호
        /// </summary>
        public string? PropertyNumber { get; set; }

        /// <summary>
        /// 지번번호
        /// </summary>
        public string? JibunNumber { get; set; }

        /// <summary>
        /// 담보소재지1 (시/도)
        /// </summary>
        public string? AddressProvince { get; set; }

        /// <summary>
        /// 담보소재지2 (시/군/구)
        /// </summary>
        public string? AddressCity { get; set; }

        /// <summary>
        /// 담보소재지3 (읍/면/동)
        /// </summary>
        public string? AddressDistrict { get; set; }

        /// <summary>
        /// 담보소재지4 (상세주소)
        /// </summary>
        public string? AddressDetail { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
