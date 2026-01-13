using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 프로그램 시트 매핑 정보
    /// 각 시트별 업로드 일자, 업로드한 사람 등을 기록
    /// </summary>
    public class ProgramSheetMapping
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 프로그램 ID
        /// </summary>
        public Guid ProgramId { get; set; }

        /// <summary>
        /// 시트 타입 (SheetA, SheetA1, SheetB, SheetC1, SheetC2, SheetC3, SheetD, SheetE, Interim 등)
        /// </summary>
        public string SheetType { get; set; } = string.Empty;

        /// <summary>
        /// 시트 타입 표시명 (차주일반정보, 회생차주정보 등)
        /// </summary>
        public string SheetTypeDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 실제 시트 이름
        /// </summary>
        public string? SheetName { get; set; }

        /// <summary>
        /// 업로드된 파일명
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 행 수
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 업로드 일시
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// 업로드한 사용자 ID
        /// </summary>
        public Guid? UploadedBy { get; set; }

        /// <summary>
        /// 업로드한 사람 이름
        /// </summary>
        public string? UploadedByName { get; set; }

        /// <summary>
        /// 업로드 일자 표시용
        /// </summary>
        public string UploadedAtDisplay => UploadedAt.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// 시트 타입별 표시 이름 반환
        /// </summary>
        public static string GetDisplayName(string sheetType)
        {
            return sheetType switch
            {
                "SheetA" => "차주일반정보",
                "SheetA1" => "회생차주정보",
                "SheetB" => "채권일반정보",
                "SheetC1" => "물건정보",
                "SheetC2" => "등기부등본정보",
                "SheetC3" => "설정순위",
                "SheetD" => "신용보증서",
                "SheetE" => "가압류",
                "Interim_Advance" => "추가 가지급금",
                "Interim_Collection" => "회수정보",
                "Interim" => "Interim",
                _ => sheetType
            };
        }
    }
}
