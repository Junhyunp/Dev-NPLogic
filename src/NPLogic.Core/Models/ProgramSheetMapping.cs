using System;
using System.Collections.Generic;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 시트 유형 (데이터디스크)
    /// </summary>
    public enum DataDiskSheetType
    {
        Unknown,
        BorrowerGeneral,       // Sheet A: 차주일반정보
        BorrowerRestructuring, // Sheet A-1: 회생차주정보
        Loan,                  // Sheet B: 채권일반정보
        Property,              // Sheet C-1: 담보물건정보
        RegistryDetail,        // Sheet C-2: 등기부등본정보
        CollateralSetting,     // Sheet C-3: 담보설정정보
        Guarantee,             // Sheet D: 보증정보
        InterimAdvance,        // Interim: 가지급금
        InterimCollection      // Interim: 회수정보
    }

    /// <summary>
    /// 프로그램 시트 매핑 정보 (DB 엔티티)
    /// 각 시트별 업로드 일자, 업로드한 사람, 컬럼 매핑 등을 기록
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
        /// 실제 Excel 시트 이름
        /// </summary>
        public string? ExcelSheetName { get; set; }

        /// <summary>
        /// 업로드된 파일명
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 컬럼 매핑 정보 (JSON)
        /// Key: Excel 컬럼명, Value: DB 컬럼명
        /// </summary>
        public Dictionary<string, string>? ColumnMappings { get; set; }

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
        /// 업로드한 사람 이름 (조인 결과)
        /// </summary>
        public string? UploadedByName { get; set; }

        /// <summary>
        /// 업로드 일자 표시용
        /// </summary>
        public string UploadedAtDisplay => UploadedAt == DateTime.MinValue 
            ? "" 
            : UploadedAt.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// 업로드 여부
        /// </summary>
        public bool IsUploaded => UploadedAt != DateTime.MinValue && UploadedBy != null;

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

        /// <summary>
        /// DataDiskSheetType을 시트 타입 문자열로 변환
        /// </summary>
        public static string ToSheetTypeString(DataDiskSheetType type)
        {
            return type switch
            {
                DataDiskSheetType.BorrowerGeneral => "SheetA",
                DataDiskSheetType.BorrowerRestructuring => "SheetA1",
                DataDiskSheetType.Loan => "SheetB",
                DataDiskSheetType.Property => "SheetC1",
                DataDiskSheetType.RegistryDetail => "SheetC2",
                DataDiskSheetType.CollateralSetting => "SheetC3",
                DataDiskSheetType.Guarantee => "SheetD",
                DataDiskSheetType.InterimAdvance => "Interim_Advance",
                DataDiskSheetType.InterimCollection => "Interim_Collection",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 시트 타입 문자열을 DataDiskSheetType으로 변환
        /// </summary>
        public static DataDiskSheetType FromSheetTypeString(string sheetType)
        {
            return sheetType switch
            {
                "SheetA" => DataDiskSheetType.BorrowerGeneral,
                "SheetA1" => DataDiskSheetType.BorrowerRestructuring,
                "SheetB" => DataDiskSheetType.Loan,
                "SheetC1" => DataDiskSheetType.Property,
                "SheetC2" => DataDiskSheetType.RegistryDetail,
                "SheetC3" => DataDiskSheetType.CollateralSetting,
                "SheetD" => DataDiskSheetType.Guarantee,
                "Interim_Advance" => DataDiskSheetType.InterimAdvance,
                "Interim_Collection" => DataDiskSheetType.InterimCollection,
                _ => DataDiskSheetType.Unknown
            };
        }
    }

    /// <summary>
    /// 시트 매핑 정보 (UI 바인딩용)
    /// </summary>
    public class SheetMappingInfo
    {
        /// <summary>
        /// Excel 시트명
        /// </summary>
        public string ExcelSheetName { get; set; } = string.Empty;

        /// <summary>
        /// 시트 인덱스
        /// </summary>
        public int SheetIndex { get; set; }

        /// <summary>
        /// 자동 감지된 시트 타입
        /// </summary>
        public DataDiskSheetType DetectedType { get; set; }

        /// <summary>
        /// 사용자가 선택한 시트 타입
        /// </summary>
        public DataDiskSheetType SelectedType { get; set; }

        /// <summary>
        /// Excel 헤더 목록
        /// </summary>
        public List<string> Headers { get; set; } = new();

        /// <summary>
        /// 컬럼 매핑 목록
        /// </summary>
        public List<ColumnMappingInfo> ColumnMappings { get; set; } = new();

        /// <summary>
        /// 데이터 행 수 (헤더 제외)
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 선택 여부
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 시트 타입 표시명
        /// </summary>
        public string SelectedTypeDisplay => SelectedType switch
        {
            DataDiskSheetType.BorrowerGeneral => "차주일반정보",
            DataDiskSheetType.BorrowerRestructuring => "회생차주정보",
            DataDiskSheetType.Loan => "채권정보",
            DataDiskSheetType.Property => "담보물건정보",
            DataDiskSheetType.RegistryDetail => "등기부등본정보",
            DataDiskSheetType.CollateralSetting => "담보설정정보",
            DataDiskSheetType.Guarantee => "보증정보",
            DataDiskSheetType.InterimAdvance => "가지급금",
            DataDiskSheetType.InterimCollection => "회수정보",
            _ => "알 수 없음"
        };

        /// <summary>
        /// 표시용 문자열
        /// </summary>
        public string DisplayText => $"{ExcelSheetName} ({SelectedTypeDisplay}) - {RowCount}행";

        /// <summary>
        /// 컬럼 매핑을 Dictionary로 변환
        /// </summary>
        public Dictionary<string, string> ToColumnMappingsDictionary()
        {
            var dict = new Dictionary<string, string>();
            foreach (var mapping in ColumnMappings)
            {
                if (!string.IsNullOrEmpty(mapping.DbColumn) && mapping.DbColumn != "(선택안함)")
                {
                    dict[mapping.ExcelColumn] = mapping.DbColumn;
                }
            }
            return dict;
        }
    }

    /// <summary>
    /// 컬럼 매핑 정보
    /// </summary>
    public class ColumnMappingInfo
    {
        /// <summary>
        /// Excel 컬럼명
        /// </summary>
        public string ExcelColumn { get; set; } = string.Empty;

        /// <summary>
        /// DB 컬럼명 (null 또는 빈 값이면 매핑 안 함)
        /// </summary>
        public string? DbColumn { get; set; }

        /// <summary>
        /// DB 컬럼 표시명
        /// </summary>
        public string? DbColumnDisplay { get; set; }

        /// <summary>
        /// 자동 매칭 여부 (기본 규칙으로 매칭됨)
        /// </summary>
        public bool IsAutoMatched { get; set; }

        /// <summary>
        /// 필수 컬럼 여부
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 매핑됨 여부
        /// </summary>
        public bool IsMapped => !string.IsNullOrEmpty(DbColumn) && DbColumn != "(선택안함)";
    }

    /// <summary>
    /// 시트 처리 결과
    /// </summary>
    public class SheetProcessResult
    {
        public string SheetName { get; set; } = string.Empty;
        public DataDiskSheetType SheetType { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
