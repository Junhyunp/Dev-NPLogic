using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 소유자 정보 모델
    /// </summary>
    public class RegistryOwner
    {
        public Guid Id { get; set; }
        public Guid? RegistryDocumentId { get; set; }
        public Guid? PropertyId { get; set; }
        public string? OwnerName { get; set; } // 소유자명
        public string? OwnerRegNo { get; set; } // 등록번호
        public string? ShareRatio { get; set; } // 지분 (예: "100%", "1/2")
        public DateTime? RegistrationDate { get; set; } // 등기일자
        public string? RegistrationCause { get; set; } // 등기원인 (예: "매매", "상속")
        public DateTime CreatedAt { get; set; }
    }
}

