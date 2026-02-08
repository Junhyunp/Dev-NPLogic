using System;
using System.Collections.Generic;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 등기부 처리 결과 세트 (PDF 1개 처리 결과 단위)
    /// - 같은 물건(Property)에 여러 개가 쌓일 수 있음
    /// </summary>
    public class RegistryRun
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }

        /// <summary>물건번호 (예: R-001_01)</summary>
        public string? PropertyNumber { get; set; }

        /// <summary>등기부등본 일련번호(물건 내 증가)</summary>
        public int DeedSeq { get; set; }

        /// <summary>지번일련번호 (예: R-001_01_01)</summary>
        public string? JibeonId { get; set; }

        /// <summary>원본 PDF 파일명</summary>
        public string? SourcePdfName { get; set; }

        /// <summary>주요 등기사항 요약 이미지 (Base64 문자열 배열, DB jsonb)</summary>
        public List<string>? SummaryImagesBase64 { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

