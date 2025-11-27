using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 문서 모델
    /// </summary>
    public class RegistryDocument
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string OcrStatus { get; set; } = "pending"; // pending, processing, completed, failed
        public DateTime? OcrProcessedAt { get; set; }
        public string? OcrError { get; set; }
        public string? RegistryType { get; set; } // 토지, 건물, 집합건물
        public string? RegistryNumber { get; set; } // 등기번호
        public string? ExtractedData { get; set; } // JSON 형태의 추출 데이터
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

