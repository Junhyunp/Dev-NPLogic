using System;
using System.Collections.Generic;
using System.Linq;

namespace NPLogic.Services
{
    /// <summary>
    /// IBK Excel 시트별 컬럼 매핑 설정
    /// </summary>
    public static class SheetMappingConfig
    {
        /// <summary>
        /// 시트 유형별 컬럼 매핑 가져오기
        /// </summary>
        public static List<ColumnMappingRule> GetMappingRules(SheetType sheetType)
        {
            return sheetType switch
            {
                SheetType.BorrowerGeneral => GetBorrowerGeneralMappings(),
                SheetType.BorrowerRestructuring => GetBorrowerRestructuringMappings(),
                SheetType.Loan => GetLoanMappings(),
                SheetType.Property => GetPropertyMappings(),
                SheetType.RegistryDetail => GetRegistryDetailMappings(),
                SheetType.CollateralSetting => GetCollateralSettingMappings(),
                SheetType.Guarantee => GetGuaranteeMappings(),
                _ => new List<ColumnMappingRule>()
            };
        }

        /// <summary>
        /// Sheet A: 차주일반정보 → borrowers
        /// </summary>
        private static List<ColumnMappingRule> GetBorrowerGeneralMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", true, ColumnDataType.String),
                new("관련차주", "related_borrower", false, ColumnDataType.String),
                new("차주형태", "borrower_type", false, ColumnDataType.String),
                
                // 금액 정보
                new("대출원금잔액", "opb", false, ColumnDataType.Decimal),
                new("가지급금", "advance_payment", false, ColumnDataType.Decimal),
                new("미상환원금잔액", "unpaid_principal", false, ColumnDataType.Decimal),
                new("미수이자", "accrued_interest", false, ColumnDataType.Decimal),
                new("차주별 근저당권설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("차주별 선순위 근저당설정액", "senior_mortgage_amount", false, ColumnDataType.Decimal),
                
                // 비고
                new("비고", "notes", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet A-1: 회생차주정보 → borrower_restructuring
        /// </summary>
        private static List<ColumnMappingRule> GetBorrowerRestructuringMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보 (차주 조회용)
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),
                new("관련차주", "related_borrower", false, ColumnDataType.String),
                
                // 회생 정보
                new("인가/미인가", "approval_status", false, ColumnDataType.String),
                new("세부진행단계", "progress_stage", false, ColumnDataType.String),
                new("관할법원", "court_name", false, ColumnDataType.String),
                new("회생사건번호", "case_number", false, ColumnDataType.String),
                
                // 날짜 정보
                new("회생신청일", "filing_date", false, ColumnDataType.Date),
                new("보전처분일", "preservation_date", false, ColumnDataType.Date),
                new("개시결정일", "commencement_date", false, ColumnDataType.Date),
                new("채권신고일", "claim_filing_date", false, ColumnDataType.Date),
                new("인가/폐지결정일", "approval_dismissal_date", false, ColumnDataType.Date),
                
                // 기타
                new("회생탈락권", "excluded_claim", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet B: 채권일반정보 → loans
        /// </summary>
        private static List<ColumnMappingRule> GetLoanMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),
                new("대출일련번호", "account_serial", true, ColumnDataType.String),
                
                // 대출 정보
                new("대출과목", "loan_type", false, ColumnDataType.String),
                new("계좌번호", "account_number", false, ColumnDataType.String),
                new("이자율", "normal_interest_rate", false, ColumnDataType.Decimal),
                
                // 날짜
                new("최초대출일", "initial_loan_date", false, ColumnDataType.Date),
                new("대출만기일", "maturity_date", false, ColumnDataType.Date),
                new("최종이수일", "last_interest_date", false, ColumnDataType.Date),
                
                // 금액
                new("통화표시", "currency", false, ColumnDataType.String),
                new("최초대출금액", "initial_loan_amount", false, ColumnDataType.Decimal),
                new("대출금잔액", "loan_principal_balance", false, ColumnDataType.Decimal),
            };
        }

        /// <summary>
        /// Sheet C-1: 담보재산정보_물건정보 → properties + 권리분석(right_analysis)
        /// </summary>
        private static List<ColumnMappingRule> GetPropertyMappings()
        {
            return new List<ColumnMappingRule>
            {
                // ========== 기본 정보 ==========
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("Pool", "pool_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("채권구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),
                new("물건번호", "collateral_number", false, ColumnDataType.String),
                new("Assign", "assigned_to_name", false, ColumnDataType.String),
                
                // Property 일련번호 (내부용, property_number와 별도)
                new("Property 일련번호", "property_serial", false, ColumnDataType.Integer),
                new("Property일련번호", "property_serial", false, ColumnDataType.Integer),
                
                // ========== 주소 정보 ==========
                new("담보소재지1", "address_province", false, ColumnDataType.String),
                new("담보소재지 1", "address_province", false, ColumnDataType.String),
                new("(특별광역시/도)", "address_province", false, ColumnDataType.String),
                new("(특별,광역시/도)", "address_province", false, ColumnDataType.String),
                new("담보소재지2", "address_city", false, ColumnDataType.String),
                new("담보소재지 2", "address_city", false, ColumnDataType.String),
                new("(시/군/구)", "address_city", false, ColumnDataType.String),
                new("담보소재지3", "address_district", false, ColumnDataType.String),
                new("담보소재지 3", "address_district", false, ColumnDataType.String),
                new("(동/리/읍/면)", "address_district", false, ColumnDataType.String),
                new("(동/읍/면/리)", "address_district", false, ColumnDataType.String),
                new("담보소재지4", "address_detail", false, ColumnDataType.String),
                new("담보소재지 4", "address_detail", false, ColumnDataType.String),
                new("(나머지/상세지번/기타재산내역)", "address_detail", false, ColumnDataType.String),
                new("담보소재지4 (나머지상세지번/기타재산내역)", "address_detail", false, ColumnDataType.String),
                
                // ========== 물건 타입 및 면적 ==========
                new("Property Type", "property_type", false, ColumnDataType.String),
                new("Property-대지면적", "land_area", false, ColumnDataType.Decimal),
                new("Property- 대지면적", "land_area", false, ColumnDataType.Decimal),
                new("Property-건물면적", "building_area", false, ColumnDataType.Decimal),
                new("Property- 건물면적", "building_area", false, ColumnDataType.Decimal),
                new("Property-기타(기계기구등)", "machinery_value", false, ColumnDataType.Decimal),
                new("Property- 기타(기계기구등)", "machinery_value", false, ColumnDataType.Decimal),
                new("소유자명", "owner_name", false, ColumnDataType.String),
                new("권리유형", "right_type", false, ColumnDataType.String),
                
                // ========== 근저당 설정 정보 ==========
                new("Property 설정 순위", "mortgage_rank", false, ColumnDataType.String),
                new("Property설정 순위", "mortgage_rank", false, ColumnDataType.String),
                new("Property별 근저당권설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("Property별근저당권설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("환산후 Property별 근저당권설정액", "mortgage_amount_converted", false, ColumnDataType.Decimal),
                
                // ========== 선순위 정보 (권리분석용) ==========
                new("선순위 소액보증금", "senior_small_deposit", false, ColumnDataType.Decimal),
                new("선순위소액보증금", "senior_small_deposit", false, ColumnDataType.Decimal),
                new("선순위\n소액보증금", "senior_small_deposit", false, ColumnDataType.Decimal),
                new("선순위 임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 \n임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위\n임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위 \n임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위\n임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위 당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위 \n당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위\n당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위 조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("선순위조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("선순위 \n조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("선순위\n조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("기타 선순위", "senior_other", false, ColumnDataType.Decimal),
                new("기타선순위", "senior_other", false, ColumnDataType.Decimal),
                new("합계", "senior_total", false, ColumnDataType.Decimal),
                
                // ========== 감정평가 정보 ==========
                new("감정평가구분", "appraisal_type", false, ColumnDataType.String),
                new("감정평가일", "appraisal_date", false, ColumnDataType.Date),
                new("감정평가기관", "appraisal_agency", false, ColumnDataType.String),
                // 실제 엑셀: "감정평가액\n_대지" 등 (줄바꿈+언더스코어)
                new("감정평가액\n_대지", "appraisal_land", false, ColumnDataType.Decimal),
                new("감정평가액_대지", "appraisal_land", false, ColumnDataType.Decimal),
                new("감정평가액 _대지", "appraisal_land", false, ColumnDataType.Decimal),
                new("감정평가액\n_건물", "appraisal_building", false, ColumnDataType.Decimal),
                new("감정평가액_건물", "appraisal_building", false, ColumnDataType.Decimal),
                new("감정평가액 _건물", "appraisal_building", false, ColumnDataType.Decimal),
                new("감정평가액\n_기계기구", "appraisal_machinery", false, ColumnDataType.Decimal),
                new("감정평가액_기계기구", "appraisal_machinery", false, ColumnDataType.Decimal),
                new("감정평가액 _기계기구", "appraisal_machinery", false, ColumnDataType.Decimal),
                new("감정평가액\n_제시외 및 기타", "appraisal_other", false, ColumnDataType.Decimal),
                new("감정평가액_제시외 및 기타", "appraisal_other", false, ColumnDataType.Decimal),
                new("감정평가액 _제시외 및 기타", "appraisal_other", false, ColumnDataType.Decimal),
                new("감정평가액\n_합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액_합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액 _합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액\n_합계", "appraisal_value", false, ColumnDataType.Decimal),
                
                // ========== 경매 정보 (선행) ==========
                new("경매 개시여부", "auction_status", false, ColumnDataType.String),
                new("경매개시여부", "auction_status", false, ColumnDataType.String),
                new("경매 \n개시여부", "auction_status", false, ColumnDataType.String),
                new("경매\n개시여부", "auction_status", false, ColumnDataType.String),
                new("경매 관할법원", "court_name", false, ColumnDataType.String),
                new("경매관할법원", "court_name", false, ColumnDataType.String),
                new("경매신청기관(선행)", "auction_applicant_precedent", false, ColumnDataType.String),
                new("경매신청기관 (선행)", "auction_applicant_precedent", false, ColumnDataType.String),
                new("경매신청기관\n(선행)", "auction_applicant_precedent", false, ColumnDataType.String),
                new("경매개시일자(선행)", "auction_start_date_precedent", false, ColumnDataType.Date),
                new("경매개시일자 (선행)", "auction_start_date_precedent", false, ColumnDataType.Date),
                new("경매개시일자\n(선행)", "auction_start_date_precedent", false, ColumnDataType.Date),
                new("경매사건번호(선행)", "case_number_precedent", false, ColumnDataType.String),
                new("경매사건번호 (선행)", "case_number_precedent", false, ColumnDataType.String),
                new("경매사건번호\n(선행)", "case_number_precedent", false, ColumnDataType.String),
                new("배당요구종기일(선행)", "claim_deadline_precedent", false, ColumnDataType.Date),
                new("배당요구종기일 (선행)", "claim_deadline_precedent", false, ColumnDataType.Date),
                new("배당요구종기일\n(선행)", "claim_deadline_precedent", false, ColumnDataType.Date),
                new("청구금액(선행)", "claim_amount_precedent", false, ColumnDataType.Decimal),
                new("청구금액 (선행)", "claim_amount_precedent", false, ColumnDataType.Decimal),
                
                // ========== 경매 정보 (후행) ==========
                new("경매신청기관(후행)", "auction_applicant_subsequent", false, ColumnDataType.String),
                new("경매신청기관 (후행)", "auction_applicant_subsequent", false, ColumnDataType.String),
                new("경매개시일자(후행)", "auction_start_date_subsequent", false, ColumnDataType.Date),
                new("경매개시일자 (후행)", "auction_start_date_subsequent", false, ColumnDataType.Date),
                new("경매사건번호(후행)", "case_number_subsequent", false, ColumnDataType.String),
                new("경매사건번호 (후행)", "case_number_subsequent", false, ColumnDataType.String),
                new("배당요구종기일(후행)", "claim_deadline_subsequent", false, ColumnDataType.Date),
                new("배당요구종기일 (후행)", "claim_deadline_subsequent", false, ColumnDataType.Date),
                new("청구금액(후행)", "claim_amount_subsequent", false, ColumnDataType.Decimal),
                new("청구금액 (후행)", "claim_amount_subsequent", false, ColumnDataType.Decimal),
                
                // ========== 경매 진행 정보 ==========
                new("최초법사가", "initial_appraisal_value", false, ColumnDataType.Decimal),
                new("최초경매기일", "initial_auction_date", false, ColumnDataType.Date),
                new("최종경매회차", "final_auction_round", false, ColumnDataType.Integer),
                new("최종경매결과", "final_auction_result", false, ColumnDataType.String),
                new("최종경매기일", "final_auction_date", false, ColumnDataType.Date),
                new("차기경매기일", "next_auction_date", false, ColumnDataType.Date),
                new("매각금액", "winning_bid_amount", false, ColumnDataType.Decimal),
                new("최종경매일의 최저입찰금액", "final_minimum_bid", false, ColumnDataType.Decimal),
                new("최종경매일의최저입찰금액", "final_minimum_bid", false, ColumnDataType.Decimal),
                new("차후예정경매일의 최저입찰금액", "next_minimum_bid", false, ColumnDataType.Decimal),
                new("차후예정경매일의최저입찰금액", "next_minimum_bid", false, ColumnDataType.Decimal),
                
                // ========== 경매사건번호 (대시보드 "사건번호" 표시용) ==========
                new("경매사건번호", "property_number", false, ColumnDataType.String),
                new("경매사건번호(IBK)", "property_number", false, ColumnDataType.String),
                new("경매사건번호 (IBK)", "property_number", false, ColumnDataType.String),
                new("경매사건번호\n(IBK)", "property_number", false, ColumnDataType.String),
                
                // 비고
                new("비고", "notes", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet C-2: 등기부등본정보 (OCR로 처리하므로 기본 매핑만 제공)
        /// </summary>
        private static List<ColumnMappingRule> GetRegistryDetailMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 조회용 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("차주일련번호", "borrower_number", false, ColumnDataType.String),
                new("물건번호", "collateral_number", false, ColumnDataType.String),
                new("Property 일련번호", "property_serial", false, ColumnDataType.Integer),
            };
        }

        /// <summary>
        /// Sheet C-3: 담보설정정보
        /// </summary>
        private static List<ColumnMappingRule> GetCollateralSettingMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 조회용 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("차주일련번호", "borrower_number", false, ColumnDataType.String),
                new("물건번호", "collateral_number", false, ColumnDataType.String),
                new("Property 일련번호", "property_serial", false, ColumnDataType.Integer),
                
                // 담보설정 정보
                new("근저당설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("근저당권자", "mortgagee", false, ColumnDataType.String),
                new("설정일", "setting_date", false, ColumnDataType.Date),
                new("설정순위", "setting_rank", false, ColumnDataType.Integer),
            };
        }

        /// <summary>
        /// Sheet D: 보증정보
        /// </summary>
        private static List<ColumnMappingRule> GetGuaranteeMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 조회용 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("차주일련번호", "borrower_number", false, ColumnDataType.String),
                
                // 보증 정보
                new("보증인", "guarantor_name", false, ColumnDataType.String),
                new("보증인유형", "guarantor_type", false, ColumnDataType.String),
                new("보증금액", "guarantee_amount", false, ColumnDataType.Decimal),
                new("보증종류", "guarantee_type", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Excel 컬럼명으로 매핑 규칙 찾기 (유사 매칭 지원)
        /// </summary>
        public static ColumnMappingRule? FindMappingRule(List<ColumnMappingRule> rules, string excelColumnName)
        {
            if (string.IsNullOrWhiteSpace(excelColumnName))
                return null;

            // 줄바꿈을 공백으로 변환하고 정리
            var normalizedName = NormalizeColumnName(excelColumnName);

            // 1. 정확한 매칭 (양쪽 모두 normalize해서 비교)
            var exactMatch = rules.FirstOrDefault(r => 
                NormalizeColumnName(r.ExcelColumnName).Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return exactMatch;

            // 2. 포함 매칭 (Excel 컬럼명이 규칙의 컬럼명을 포함)
            var containsMatch = rules.FirstOrDefault(r => 
                normalizedName.Contains(NormalizeColumnName(r.ExcelColumnName), StringComparison.OrdinalIgnoreCase));
            if (containsMatch != null)
                return containsMatch;

            // 3. 역방향 포함 매칭 (규칙의 컬럼명이 Excel 컬럼명을 포함)
            var reverseMatch = rules.FirstOrDefault(r => 
                NormalizeColumnName(r.ExcelColumnName).Contains(normalizedName, StringComparison.OrdinalIgnoreCase));
            
            return reverseMatch;
        }

        /// <summary>
        /// 컬럼명 정규화 (줄바꿈, 연속 공백 처리)
        /// </summary>
        private static string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            return columnName
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("  ", " ")
                .Trim();
        }
    }

    /// <summary>
    /// 컬럼 매핑 규칙
    /// </summary>
    public class ColumnMappingRule
    {
        public string ExcelColumnName { get; set; }
        public string DbColumnName { get; set; }
        public bool IsRequired { get; set; }
        public ColumnDataType DataType { get; set; }

        public ColumnMappingRule(string excelColumnName, string dbColumnName, bool isRequired, ColumnDataType dataType)
        {
            ExcelColumnName = excelColumnName;
            DbColumnName = dbColumnName;
            IsRequired = isRequired;
            DataType = dataType;
        }

        /// <summary>
        /// 값 변환
        /// </summary>
        public object? ConvertValue(object? rawValue)
        {
            if (rawValue == null || (rawValue is string s && string.IsNullOrWhiteSpace(s)))
                return null;

            try
            {
                return DataType switch
                {
                    ColumnDataType.String => rawValue.ToString()?.Trim(),
                    ColumnDataType.Integer => Convert.ToInt32(rawValue),
                    ColumnDataType.Decimal => ParseDecimal(rawValue),
                    ColumnDataType.Date => ParseDate(rawValue),
                    ColumnDataType.Boolean => ParseBoolean(rawValue),
                    _ => rawValue
                };
            }
            catch
            {
                return null;
            }
        }

        private static decimal? ParseDecimal(object value)
        {
            var str = value.ToString()?.Replace(",", "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(str) || str == "-")
                return null;
            
            // 백분율 처리
            if (str.EndsWith("%"))
            {
                str = str.TrimEnd('%');
                if (decimal.TryParse(str, out var pctValue))
                    return pctValue / 100;
            }

            if (decimal.TryParse(str, out var decValue))
                return decValue;

            return null;
        }

        private static DateTime? ParseDate(object value)
        {
            if (value is DateTime dt)
                return dt;

            var str = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(str) || str == "-")
                return null;

            // 다양한 날짜 형식 처리
            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyy.MM.dd",
                "yyyyMMdd",
                "MM/dd/yyyy",
                "dd/MM/yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(str, format, null, System.Globalization.DateTimeStyles.None, out var parsed))
                    return parsed;
            }

            if (DateTime.TryParse(str, out var fallback))
                return fallback;

            // Excel 날짜 숫자 (OADate)
            if (double.TryParse(str, out var oaDate) && oaDate > 0 && oaDate < 100000)
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch { }
            }

            return null;
        }

        private static bool? ParseBoolean(object value)
        {
            var str = value.ToString()?.Trim().ToLower();
            if (string.IsNullOrEmpty(str))
                return null;

            return str switch
            {
                "y" or "yes" or "true" or "1" or "예" or "○" => true,
                "n" or "no" or "false" or "0" or "아니오" or "x" => false,
                _ => null
            };
        }
    }

    /// <summary>
    /// 컬럼 데이터 타입
    /// </summary>
    public enum ColumnDataType
    {
        String,
        Integer,
        Decimal,
        Date,
        Boolean
    }
}

