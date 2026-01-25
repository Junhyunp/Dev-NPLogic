using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NPLogic.Services
{
    /// <summary>
    /// 은행 유형
    /// </summary>
    public enum BankType
    {
        Unknown,
        KB,
        IBK,
        NH,
        SHB
    }

    /// <summary>
    /// 은행별 매핑 템플릿 설정
    /// mapping_template.json 기반
    /// </summary>
    public class BankMappingConfig
    {
        private static BankMappingTemplate? _template;
        private static readonly object _lock = new();

        /// <summary>
        /// 대표 시트명 목록 (내부 표준)
        /// </summary>
        public static readonly Dictionary<string, string> StandardSheetNames = new()
        {
            { "BorrowerGeneral", "차주일반정보" },
            { "BorrowerRestructuring", "회생차주정보" },
            { "Loan", "채권일반정보" },
            { "Property", "물건정보" },
            { "RegistryDetail", "등기부등본정보" },
            { "Guarantee", "신용보증서" }
        };

        /// <summary>
        /// 대표 컬럼명 목록 (시트별)
        /// </summary>
        public static readonly Dictionary<string, List<string>> StandardColumns = new()
        {
            { "차주일반정보", new List<string> { "자산유형", "차주일련번호", "차주명", "관련차주", "차주형태", "미상환원금잔액", "미수이자", "근저당권설정액", "비고" } },
            { "회생차주정보", new List<string> { "자산유형", "차주일련번호", "차주명", "세부 진행단계", "관할법원", "회생사건번호", "보전처분일", "개시결정일", "채권신고일", "인가/폐지결정일", "업종", "상장/비상장", "종업원수", "설립일" } },
            { "채권일반정보", new List<string> { "차주일련번호", "차주명", "대출일련번호", "대출과목", "계좌번호", "정상이자율", "연체이자율", "최초대출일", "최초대출원금", "환산된 대출잔액", "가지급금", "미상환원금잔액", "미수이자", "채권액 합계" } },
            { "물건정보", new List<string> { "자산유형", "차주일련번호", "차주명", "물건 일련번호", "담보소재지 1", "담보소재지 2", "담보소재지 3", "담보소재지 4", "물건 종류", "물건 대지면적", "물건 건물면적", "물건 기타 (기계기구 등)", "공담 물건 금액", "물건별 선순위 설정액", "선순위 주택 소액보증금", "선순위 상가 소액보증금", "선순위 소액보증금", "선순위 주택 임차보증금", "선순위 상가 임차보증금", "선순위 임차보증금", "선순위 임금채권", "선순위 당해세", "선순위 조세채권", "선순위 기타", "선순위 합계", "감정평가구분", "감정평가일자", "감정평가기관", "토지감정평가액", "건물감정평가액", "기계평가액", "제시외", "감정평가액합계", "KB아파트시세", "경매개시여부", "경매 관할법원", "경매신청기관(선행)", "경매개시일자(선행)", "경매사건번호(선행)", "배당요구종기일(선행)", "청구금액(선행)", "경매신청기관(후행)", "경매개시일자(후행)", "경매사건번호(후행)", "배당요구종기일(후행)", "청구금액(후행)", "최초법사가", "최초경매기일", "최종경매회차", "최종경매결과", "최종경매기일", "차기경매기일", "낙찰금액", "최종경매일의 최저입찰금액", "차후최종경매일의 최저입찰금액", "비고" } },
            { "등기부등본정보", new List<string> { "차주일련번호", "차주명", "물건번호", "지번번호", "담보소재지1", "담보소재지2", "담보소재지3", "담보소재지4" } },
            { "신용보증서", new List<string> { "자산유형", "차주일련번호", "차주명", "계좌일련번호", "보증기관", "보증종류", "보증서번호", "보증비율", "환산후 보증잔액", "관련 대출채권 계좌번호" } }
        };

        /// <summary>
        /// 매핑 템플릿 로드 (싱글톤)
        /// </summary>
        public static BankMappingTemplate GetTemplate()
        {
            if (_template == null)
            {
                lock (_lock)
                {
                    if (_template == null)
                    {
                        _template = LoadTemplate();
                    }
                }
            }
            return _template;
        }

        /// <summary>
        /// 매핑 템플릿 강제 리로드
        /// </summary>
        public static void ReloadTemplate()
        {
            lock (_lock)
            {
                _template = LoadTemplate();
            }
        }

        /// <summary>
        /// 템플릿 파일 로드
        /// </summary>
        private static BankMappingTemplate LoadTemplate()
        {
            try
            {
                // 1. 실행 파일 디렉토리에서 찾기
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var templatePath = Path.Combine(exeDir, "Resources", "mapping_template.json");

                if (!File.Exists(templatePath))
                {
                    // 2. 개발 환경에서 manager/client 폴더에서 찾기
                    var devPath = Path.Combine(exeDir, "..", "..", "..", "..", "..", "manager", "client", "매핑 로직", "03. mapping_template.json");
                    if (File.Exists(devPath))
                    {
                        templatePath = devPath;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[BankMappingConfig] 매핑 템플릿 파일을 찾을 수 없음: {templatePath}");
                        return CreateDefaultTemplate();
                    }
                }

                var json = File.ReadAllText(templatePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var template = JsonSerializer.Deserialize<BankMappingTemplate>(json, options);
                if (template == null)
                {
                    System.Diagnostics.Debug.WriteLine("[BankMappingConfig] 템플릿 파싱 실패, 기본 템플릿 사용");
                    return CreateDefaultTemplate();
                }

                System.Diagnostics.Debug.WriteLine($"[BankMappingConfig] 템플릿 로드 완료: {template.Banks?.Count ?? 0}개 은행");
                return template;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BankMappingConfig] 템플릿 로드 오류: {ex.Message}");
                return CreateDefaultTemplate();
            }
        }

        /// <summary>
        /// 기본 템플릿 생성 (파일 로드 실패 시)
        /// </summary>
        private static BankMappingTemplate CreateDefaultTemplate()
        {
            return new BankMappingTemplate
            {
                Version = "1.0",
                Description = "기본 매핑 템플릿",
                Banks = new Dictionary<string, BankConfig>()
            };
        }

        /// <summary>
        /// 은행별 시트 매핑 정보 가져오기
        /// </summary>
        public static BankConfig? GetBankConfig(BankType bank)
        {
            var template = GetTemplate();
            var bankName = bank.ToString();

            if (template.Banks?.TryGetValue(bankName, out var config) == true)
            {
                return config;
            }

            return null;
        }

        /// <summary>
        /// 은행별 특정 시트의 컬럼 매핑 가져오기
        /// 대표컬럼명 → 은행별 원본컬럼명 딕셔너리 반환
        /// </summary>
        public static Dictionary<string, string?> GetColumnMappings(BankType bank, string standardSheetName)
        {
            var config = GetBankConfig(bank);
            if (config?.Sheets == null)
                return new Dictionary<string, string?>();

            if (config.Sheets.TryGetValue(standardSheetName, out var sheetConfig))
            {
                return sheetConfig.Columns ?? new Dictionary<string, string?>();
            }

            return new Dictionary<string, string?>();
        }

        /// <summary>
        /// 은행별 원본 시트명 가져오기
        /// </summary>
        public static string? GetOriginalSheetName(BankType bank, string standardSheetName)
        {
            var config = GetBankConfig(bank);
            if (config?.Sheets == null)
                return null;

            if (config.Sheets.TryGetValue(standardSheetName, out var sheetConfig))
            {
                return sheetConfig.SheetName;
            }

            return null;
        }

        /// <summary>
        /// 원본 컬럼명을 대표 컬럼명으로 변환
        /// </summary>
        public static string? GetStandardColumnName(BankType bank, string standardSheetName, string originalColumnName)
        {
            var mappings = GetColumnMappings(bank, standardSheetName);
            
            // 공백 제거 후 비교
            var normalizedOriginal = NormalizeColumnName(originalColumnName);

            foreach (var kvp in mappings)
            {
                if (kvp.Value != null && NormalizeColumnName(kvp.Value) == normalizedOriginal)
                {
                    return kvp.Key; // 대표 컬럼명 반환
                }
            }

            return null;
        }

        /// <summary>
        /// 대표 컬럼명에 해당하는 원본 컬럼명 가져오기
        /// </summary>
        public static string? GetOriginalColumnName(BankType bank, string standardSheetName, string standardColumnName)
        {
            var mappings = GetColumnMappings(bank, standardSheetName);
            
            if (mappings.TryGetValue(standardColumnName, out var originalName))
            {
                return originalName;
            }

            return null;
        }

        /// <summary>
        /// 컬럼명 정규화 (공백 제거)
        /// </summary>
        public static string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            return columnName
                .Replace(" ", "")
                .Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Trim();
        }

        /// <summary>
        /// 역방향 매핑 생성 (원본컬럼명 → 대표컬럼명)
        /// </summary>
        public static Dictionary<string, string> BuildReverseColumnMapping(BankType bank, string standardSheetName)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var mappings = GetColumnMappings(bank, standardSheetName);

            foreach (var kvp in mappings)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    var normalizedOriginal = NormalizeColumnName(kvp.Value);
                    if (!result.ContainsKey(normalizedOriginal))
                    {
                        result[normalizedOriginal] = kvp.Key;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 모든 은행의 시트명 패턴 가져오기 (은행 감지용)
        /// </summary>
        public static Dictionary<BankType, List<string>> GetAllSheetNamePatterns()
        {
            var result = new Dictionary<BankType, List<string>>();
            var template = GetTemplate();

            if (template.Banks == null)
                return result;

            foreach (var bankKvp in template.Banks)
            {
                if (Enum.TryParse<BankType>(bankKvp.Key, out var bankType))
                {
                    var sheetNames = new List<string>();
                    if (bankKvp.Value.Sheets != null)
                    {
                        foreach (var sheetKvp in bankKvp.Value.Sheets)
                        {
                            if (!string.IsNullOrEmpty(sheetKvp.Value.SheetName))
                            {
                                sheetNames.Add(sheetKvp.Value.SheetName);
                            }
                        }
                    }
                    result[bankType] = sheetNames;
                }
            }

            return result;
        }

        /// <summary>
        /// 시트명 목록을 기반으로 은행 자동 감지
        /// </summary>
        /// <param name="sheetNames">Excel 파일의 시트명 목록</param>
        /// <returns>감지된 은행 유형과 신뢰도</returns>
        public static (BankType Bank, double Confidence) DetectBank(List<string> sheetNames)
        {
            if (sheetNames == null || sheetNames.Count == 0)
                return (BankType.Unknown, 0);

            var scores = new Dictionary<BankType, int>
            {
                { BankType.KB, 0 },
                { BankType.IBK, 0 },
                { BankType.NH, 0 },
                { BankType.SHB, 0 }
            };

            // 각 은행별 특징적인 시트명 패턴
            var bankPatterns = new Dictionary<BankType, List<(string Pattern, int Weight)>>
            {
                { BankType.KB, new List<(string, int)>
                    {
                        ("Sheet A(차주일반정보)", 10),
                        ("Sheet B(채권일반정보)", 10),
                        ("Sheet C-1(물건정보)", 10),
                        ("Sheet C-2(등기부등본정보)", 10),
                        ("Sheet D(신용보증서정보)", 10),
                        ("(차주일반정보)", 5),
                        ("(채권일반정보)", 5),
                        ("(물건정보)", 5)
                    }
                },
                { BankType.IBK, new List<(string, int)>
                    {
                        ("Sheet A-1", 8), // IBK has A-1 for 회생차주정보
                        ("Sheet A", 3),
                        ("Sheet B", 3),
                        ("Sheet C-1", 3),
                        ("Sheet C-2", 3),
                        ("Sheet D", 3)
                    }
                },
                { BankType.NH, new List<(string, int)>
                    {
                        ("Sheet F", 10), // NH has Sheet F for 회생차주정보
                        ("Sheet B-1", 10), // NH has Sheet B-1 for 채권일반정보
                        ("Sheet A", 3),
                        ("Sheet C-1", 3),
                        ("Sheet C-2", 3),
                        ("Sheet D", 3)
                    }
                },
                { BankType.SHB, new List<(string, int)>
                    {
                        ("1.차주일반", 10),
                        ("2.매각대상채권", 10),
                        ("3.담보물건", 10),
                        ("3-1.담보지번", 10),
                        ("4.회생차주 추가정보", 10),
                        ("5.신용보증서", 10)
                    }
                }
            };

            // 각 시트명에 대해 패턴 매칭
            foreach (var sheetName in sheetNames)
            {
                var normalizedSheet = NormalizeColumnName(sheetName);
                
                foreach (var bankKvp in bankPatterns)
                {
                    foreach (var (pattern, weight) in bankKvp.Value)
                    {
                        var normalizedPattern = NormalizeColumnName(pattern);
                        
                        // 정확한 매칭
                        if (normalizedSheet == normalizedPattern)
                        {
                            scores[bankKvp.Key] += weight * 2;
                        }
                        // 포함 매칭
                        else if (sheetName.Contains(pattern) || normalizedSheet.Contains(normalizedPattern))
                        {
                            scores[bankKvp.Key] += weight;
                        }
                    }
                }
            }

            // 가장 높은 점수의 은행 찾기
            var maxScore = scores.Max(s => s.Value);
            if (maxScore == 0)
                return (BankType.Unknown, 0);

            var detectedBank = scores.First(s => s.Value == maxScore).Key;
            
            // 신뢰도 계산 (최대 점수 대비 비율)
            var totalPossibleScore = bankPatterns[detectedBank].Sum(p => p.Weight * 2);
            var confidence = Math.Min(1.0, (double)maxScore / totalPossibleScore);

            System.Diagnostics.Debug.WriteLine($"[DetectBank] 감지 결과: {detectedBank} (신뢰도: {confidence:P0})");
            System.Diagnostics.Debug.WriteLine($"[DetectBank] 점수: KB={scores[BankType.KB]}, IBK={scores[BankType.IBK]}, NH={scores[BankType.NH]}, SHB={scores[BankType.SHB]}");

            return (detectedBank, confidence);
        }

        /// <summary>
        /// 시트명으로 해당 시트의 대표시트타입 감지
        /// </summary>
        public static string? DetectStandardSheetType(BankType bank, string sheetName)
        {
            var config = GetBankConfig(bank);
            if (config?.Sheets == null)
                return null;

            var normalizedSheet = NormalizeColumnName(sheetName);

            // 1. 정확히 일치하는 경우 먼저 확인
            foreach (var kvp in config.Sheets)
            {
                if (!string.IsNullOrEmpty(kvp.Value.SheetName))
                {
                    var normalizedBankSheet = NormalizeColumnName(kvp.Value.SheetName);
                    if (normalizedSheet == normalizedBankSheet ||
                        sheetName.Equals(kvp.Value.SheetName, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Key;
                    }
                }
            }

            // 2. 더 구체적인 시트명(길이가 긴 것)부터 Contains 확인
            //    예: "Sheet A-1"이 "Sheet A"보다 먼저 확인되어야 함
            var orderedSheets = config.Sheets
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value.SheetName))
                .OrderByDescending(kvp => kvp.Value.SheetName!.Length);

            foreach (var kvp in orderedSheets)
            {
                var normalizedBankSheet = NormalizeColumnName(kvp.Value.SheetName!);
                if (sheetName.Contains(kvp.Value.SheetName!) ||
                    normalizedSheet.Contains(normalizedBankSheet))
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// 은행과 시트명으로 SheetType enum 반환
        /// </summary>
        public static SheetType GetSheetTypeEnum(BankType bank, string sheetName)
        {
            var standardType = DetectStandardSheetType(bank, sheetName);
            if (standardType == null)
                return SheetType.Unknown;

            return standardType switch
            {
                "차주일반정보" => SheetType.BorrowerGeneral,
                "회생차주정보" => SheetType.BorrowerRestructuring,
                "채권일반정보" => SheetType.Loan,
                "물건정보" => SheetType.Property,
                "등기부등본정보" => SheetType.RegistryDetail,
                "신용보증서" => SheetType.Guarantee,
                _ => SheetType.Unknown
            };
        }
    }

    // SheetType enum은 ExcelService.cs에 정의되어 있음

    #region JSON 모델 클래스

    /// <summary>
    /// 매핑 템플릿 루트
    /// </summary>
    public class BankMappingTemplate
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("banks")]
        public Dictionary<string, BankConfig>? Banks { get; set; }
    }

    /// <summary>
    /// 은행별 설정
    /// </summary>
    public class BankConfig
    {
        [JsonPropertyName("sheets")]
        public Dictionary<string, SheetConfig>? Sheets { get; set; }
    }

    /// <summary>
    /// 시트별 설정
    /// </summary>
    public class SheetConfig
    {
        [JsonPropertyName("sheet_name")]
        public string? SheetName { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("columns")]
        public Dictionary<string, string?>? Columns { get; set; }
    }

    #endregion
}
