using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.Services
{
    /// <summary>
    /// 데이터디스크 업로드 통합 서비스
    /// ProgramManagementView와 DataUploadView에서 공통으로 사용
    /// </summary>
    public class DataDiskUploadService
    {
        private readonly ExcelService _excelService;
        private readonly PropertyRepository _propertyRepository;
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;
        private readonly BorrowerRestructuringRepository _restructuringRepository;
        private readonly RightAnalysisRepository _rightAnalysisRepository;
        private readonly InterimRepository _interimRepository;
        private readonly ProgramSheetMappingRepository _sheetMappingRepository;
        private readonly RegistrySheetDataRepository _registrySheetDataRepository;
        private readonly CreditGuaranteeRepository _creditGuaranteeRepository;

        /// <summary>
        /// 진행 상황 업데이트 콜백
        /// </summary>
        public Action<int, int, string>? OnProgressUpdate { get; set; }

        public DataDiskUploadService(
            ExcelService excelService,
            PropertyRepository propertyRepository,
            BorrowerRepository borrowerRepository,
            LoanRepository loanRepository,
            BorrowerRestructuringRepository restructuringRepository,
            RightAnalysisRepository rightAnalysisRepository,
            InterimRepository interimRepository,
            ProgramSheetMappingRepository sheetMappingRepository,
            RegistrySheetDataRepository registrySheetDataRepository,
            CreditGuaranteeRepository creditGuaranteeRepository)
        {
            _excelService = excelService;
            _propertyRepository = propertyRepository;
            _borrowerRepository = borrowerRepository;
            _loanRepository = loanRepository;
            _restructuringRepository = restructuringRepository;
            _rightAnalysisRepository = rightAnalysisRepository;
            _interimRepository = interimRepository;
            _sheetMappingRepository = sheetMappingRepository;
            _registrySheetDataRepository = registrySheetDataRepository;
            _creditGuaranteeRepository = creditGuaranteeRepository;
        }

        #region 시트 로드 및 매핑

        /// <summary>
        /// 현재 감지된 은행 타입
        /// </summary>
        public BankType DetectedBankType { get; private set; } = BankType.Unknown;

        /// <summary>
        /// Excel 파일에서 시트 목록 로드 (자동 타입 감지 포함)
        /// </summary>
        public List<SheetMappingInfo> LoadExcelSheets(string filePath)
        {
            var sheets = _excelService.GetSheetNames(filePath);
            var result = new List<SheetMappingInfo>();

            // 은행 자동 감지
            var sheetNames = sheets.Select(s => s.Name).ToList();
            var (detectedBank, confidence) = BankMappingConfig.DetectBank(sheetNames);
            DetectedBankType = detectedBank;
            
            System.Diagnostics.Debug.WriteLine($"[LoadExcelSheets] 은행 감지: {detectedBank} (신뢰도: {confidence:P0})");

            foreach (var sheet in sheets)
            {
                DataDiskSheetType detectedType;
                
                // 은행이 감지된 경우 은행별 매핑 템플릿 사용
                if (detectedBank != BankType.Unknown)
                {
                    var standardSheetType = BankMappingConfig.DetectStandardSheetType(detectedBank, sheet.Name);
                    detectedType = ConvertStandardSheetTypeToDataDiskType(standardSheetType);
                }
                else
                {
                    detectedType = ConvertSheetType(sheet.SheetType);
                }
                
                result.Add(new SheetMappingInfo
                {
                    ExcelSheetName = sheet.Name,
                    SheetIndex = sheet.Index,
                    DetectedType = detectedType,
                    SelectedType = detectedType,
                    Headers = sheet.Headers,
                    RowCount = Math.Max(0, sheet.RowCount - 1), // 헤더 제외
                    IsSelected = detectedType != DataDiskSheetType.Unknown
                });
            }

            return result;
        }

        /// <summary>
        /// Excel 파일에서 시트 목록 로드 (은행 타입 지정)
        /// </summary>
        public List<SheetMappingInfo> LoadExcelSheets(string filePath, BankType bankType)
        {
            DetectedBankType = bankType;
            var sheets = _excelService.GetSheetNames(filePath);
            var result = new List<SheetMappingInfo>();

            foreach (var sheet in sheets)
            {
                DataDiskSheetType detectedType;
                
                if (bankType != BankType.Unknown)
                {
                    var standardSheetType = BankMappingConfig.DetectStandardSheetType(bankType, sheet.Name);
                    detectedType = ConvertStandardSheetTypeToDataDiskType(standardSheetType);
                }
                else
                {
                    detectedType = ConvertSheetType(sheet.SheetType);
                }
                
                result.Add(new SheetMappingInfo
                {
                    ExcelSheetName = sheet.Name,
                    SheetIndex = sheet.Index,
                    DetectedType = detectedType,
                    SelectedType = detectedType,
                    Headers = sheet.Headers,
                    RowCount = Math.Max(0, sheet.RowCount - 1),
                    IsSelected = detectedType != DataDiskSheetType.Unknown
                });
            }

            return result;
        }

        /// <summary>
        /// 시트의 기본 컬럼 매핑 가져오기 (은행별 매핑 템플릿 사용)
        /// </summary>
        public List<ColumnMappingInfo> GetDefaultColumnMappings(DataDiskSheetType sheetType, List<string> headers)
        {
            return GetDefaultColumnMappings(sheetType, headers, DetectedBankType);
        }

        /// <summary>
        /// 시트의 기본 컬럼 매핑 가져오기 (은행 타입 지정)
        /// </summary>
        public List<ColumnMappingInfo> GetDefaultColumnMappings(DataDiskSheetType sheetType, List<string> headers, BankType bankType)
        {
            var result = new List<ColumnMappingInfo>();

            // 이미 매핑된 DB 컬럼 추적 (중복 방지)
            var mappedDbColumns = new HashSet<string>();

            // 은행이 감지된 경우 매핑 템플릿 사용
            if (bankType != BankType.Unknown)
            {
                var standardSheetName = ConvertDataDiskTypeToStandardSheetName(sheetType);
                var reverseMapping = BankMappingConfig.BuildReverseColumnMapping(bankType, standardSheetName);

                System.Diagnostics.Debug.WriteLine($"[GetDefaultColumnMappings] 은행: {bankType}, 시트: {standardSheetName}, 역매핑 규칙: {reverseMapping.Count}개");

                foreach (var header in headers)
                {
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    // 컬럼명 정규화 (공백 제거)
                    var normalizedHeader = BankMappingConfig.NormalizeColumnName(header);

                    // 은행별 매핑 템플릿에서 대표컬럼명 찾기
                    string? standardColumnName = null;
                    if (reverseMapping.TryGetValue(normalizedHeader, out var standardCol))
                    {
                        standardColumnName = standardCol;
                    }

                    // 기존 SheetMappingConfig에서 DB 컬럼 찾기
                    var mappingRules = SheetMappingConfig.GetMappingRules(ConvertToExcelSheetType(sheetType));
                    ColumnMappingRule? rule = null;

                    if (standardColumnName != null)
                    {
                        // 대표컬럼명으로 DB 컬럼 찾기
                        rule = SheetMappingConfig.FindMappingRule(mappingRules, standardColumnName);
                    }

                    if (rule == null)
                    {
                        // 원본 컬럼명으로 직접 찾기
                        rule = SheetMappingConfig.FindMappingRule(mappingRules, header);
                    }

                    // 중복 DB 컬럼 매핑 방지: 이미 매핑된 DB 컬럼은 건너뛰기
                    string? dbColumn = rule?.DbColumnName;
                    bool isAutoMatched = rule != null;

                    if (!string.IsNullOrEmpty(dbColumn) && mappedDbColumns.Contains(dbColumn))
                    {
                        // 이미 다른 Excel 컬럼이 이 DB 컬럼에 매핑됨 → 매핑 안함
                        dbColumn = null;
                        isAutoMatched = false;
                    }
                    else if (!string.IsNullOrEmpty(dbColumn))
                    {
                        mappedDbColumns.Add(dbColumn);
                    }

                    result.Add(new ColumnMappingInfo
                    {
                        ExcelColumn = header,
                        DbColumn = dbColumn,
                        DbColumnDisplay = dbColumn != null ? GetDbColumnDisplayName(dbColumn) : null,
                        IsAutoMatched = isAutoMatched,
                        IsRequired = rule?.IsRequired ?? false,
                        StandardColumnName = standardColumnName // 대표컬럼명 저장
                    });
                }
            }
            else
            {
                // 기존 로직 (은행 미감지 시)
                var mappingRules = SheetMappingConfig.GetMappingRules(ConvertToExcelSheetType(sheetType));

                foreach (var header in headers)
                {
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    var normalizedHeader = header.Replace("\n", " ").Replace("\r", "").Trim();
                    var rule = SheetMappingConfig.FindMappingRule(mappingRules, normalizedHeader);

                    // 중복 DB 컬럼 매핑 방지: 이미 매핑된 DB 컬럼은 건너뛰기
                    string? dbColumn = rule?.DbColumnName;
                    bool isAutoMatched = rule != null;

                    if (!string.IsNullOrEmpty(dbColumn) && mappedDbColumns.Contains(dbColumn))
                    {
                        // 이미 다른 Excel 컬럼이 이 DB 컬럼에 매핑됨 → 매핑 안함
                        dbColumn = null;
                        isAutoMatched = false;
                    }
                    else if (!string.IsNullOrEmpty(dbColumn))
                    {
                        mappedDbColumns.Add(dbColumn);
                    }

                    result.Add(new ColumnMappingInfo
                    {
                        ExcelColumn = header,
                        DbColumn = dbColumn,
                        DbColumnDisplay = dbColumn != null ? GetDbColumnDisplayName(dbColumn) : null,
                        IsAutoMatched = isAutoMatched,
                        IsRequired = rule?.IsRequired ?? false
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 대표시트타입 → DataDiskSheetType 변환
        /// </summary>
        private DataDiskSheetType ConvertStandardSheetTypeToDataDiskType(string? standardSheetType)
        {
            return standardSheetType switch
            {
                "차주일반정보" => DataDiskSheetType.BorrowerGeneral,
                "회생차주정보" => DataDiskSheetType.BorrowerRestructuring,
                "채권일반정보" => DataDiskSheetType.Loan,
                "물건정보" => DataDiskSheetType.Property,
                "등기부등본정보" => DataDiskSheetType.RegistryDetail,
                "신용보증서" => DataDiskSheetType.Guarantee,
                _ => DataDiskSheetType.Unknown
            };
        }

        /// <summary>
        /// DataDiskSheetType → 대표시트명 변환
        /// </summary>
        private string ConvertDataDiskTypeToStandardSheetName(DataDiskSheetType sheetType)
        {
            return sheetType switch
            {
                DataDiskSheetType.BorrowerGeneral => "차주일반정보",
                DataDiskSheetType.BorrowerRestructuring => "회생차주정보",
                DataDiskSheetType.Loan => "채권일반정보",
                DataDiskSheetType.Property => "물건정보",
                DataDiskSheetType.RegistryDetail => "등기부등본정보",
                DataDiskSheetType.Guarantee => "신용보증서",
                _ => ""
            };
        }

        /// <summary>
        /// 시트 타입별 사용 가능한 DB 컬럼 목록 가져오기 (대표컬럼만 반환)
        /// </summary>
        public List<(string DbColumn, string DisplayName)> GetAvailableDbColumns(DataDiskSheetType sheetType)
        {
            // 시트 타입별 대표컬럼만 반환
            var representativeColumns = GetRepresentativeColumns(sheetType);

            return representativeColumns
                .Select(col => (col, GetDbColumnDisplayName(col)))
                .ToList();
        }

        /// <summary>
        /// 시트 타입별 대표컬럼 목록
        /// </summary>
        private List<string> GetRepresentativeColumns(DataDiskSheetType sheetType)
        {
            return sheetType switch
            {
                DataDiskSheetType.BorrowerGeneral => new List<string>
                {
                    "asset_type",           // 자산유형
                    "borrower_number",      // 차주일련번호
                    "borrower_name",        // 차주명
                    "related_borrower",     // 관련차주
                    "borrower_type",        // 차주형태
                    "unpaid_principal",     // 미상환원금잔액
                    "accrued_interest",     // 미수이자
                    "mortgage_amount",      // 근저당권설정액
                    "notes"                 // 비고
                },
                DataDiskSheetType.Property => new List<string>
                {
                    // 기본 정보
                    "asset_type",                   // 자산유형
                    "borrower_number",              // 차주일련번호
                    "borrower_name",                // 차주명
                    "collateral_number",            // 물건 일련번호
                    "address_province",             // 담보소재지 1
                    "address_city",                 // 담보소재지 2
                    "address_district",             // 담보소재지 3
                    "address_detail",               // 담보소재지 4
                    "property_type",                // 물건 종류
                    "land_area",                    // 물건 대지면적
                    "building_area",                // 물건 건물면적
                    "machinery_value",              // 물건 기타 (기계기구 등)
                    "joint_collateral_amount",      // 공담 물건 금액

                    // 선순위 정보
                    "senior_mortgage_amount",       // 물건별 선순위 설정액
                    "senior_housing_small_deposit", // 선순위 주택 소액보증금
                    "senior_commercial_small_deposit", // 선순위 상가 소액보증금
                    "senior_small_deposit",         // 선순위 소액보증금
                    "senior_housing_lease_deposit", // 선순위 주택 임차보증금
                    "senior_commercial_lease_deposit", // 선순위 상가 임차보증금
                    "senior_lease_deposit",         // 선순위 임차보증금
                    "senior_wage_claim",            // 선순위 임금채권
                    "senior_current_tax",           // 선순위 당해세
                    "senior_tax_claim",             // 선순위 조세채권
                    "senior_etc",                   // 선순위 기타
                    "senior_total",                 // 선순위 합계

                    // 감정평가 정보
                    "appraisal_type",               // 감정평가구분
                    "appraisal_date",               // 감정평가일자
                    "appraisal_agency",             // 감정평가기관
                    "land_appraisal_value",         // 토지감정평가액
                    "building_appraisal_value",     // 건물감정평가액
                    "machinery_appraisal_value",    // 기계평가액
                    "excluded_appraisal",           // 제시외
                    "appraisal_value",              // 감정평가액합계
                    "kb_price",                     // KB아파트시세

                    // 경매 기본 정보
                    "auction_started",              // 경매개시여부
                    "auction_court",                // 경매 관할법원

                    // 경매 선행 정보
                    "precedent_auction_applicant",  // 경매신청기관(선행)
                    "precedent_auction_start_date", // 경매개시일자(선행)
                    "precedent_case_number",        // 경매사건번호(선행)
                    "precedent_claim_deadline",     // 배당요구종기일(선행)
                    "precedent_claim_amount",       // 청구금액(선행)

                    // 경매 후행 정보
                    "subsequent_auction_applicant", // 경매신청기관(후행)
                    "subsequent_auction_start_date",// 경매개시일자(후행)
                    "subsequent_case_number",       // 경매사건번호(후행)
                    "subsequent_claim_deadline",    // 배당요구종기일(후행)
                    "subsequent_claim_amount",      // 청구금액(후행)

                    // 경매 기일/결과 정보
                    "initial_court_value",          // 최초법사가
                    "first_auction_date",           // 최초경매기일
                    "final_auction_round",          // 최종경매회차
                    "final_auction_result",         // 최종경매결과
                    "final_auction_date",           // 최종경매기일
                    "next_auction_date",            // 차기경매기일
                    "winning_bid_amount",           // 낙찰금액
                    "final_minimum_bid",            // 최종경매일의 최저입찰금액
                    "next_minimum_bid",             // 차후최종경매일의 최저입찰금액
                    "notes"                         // 비고
                },
                DataDiskSheetType.BorrowerRestructuring => new List<string>
                {
                    "asset_type",               // 자산유형 (borrowers 참조)
                    "borrower_number",          // 차주일련번호 (borrowers 참조)
                    "borrower_name",            // 차주명 (borrowers 참조)
                    "progress_stage",           // 세부진행단계
                    "court_name",               // 관할법원
                    "case_number",              // 회생사건번호
                    "preservation_date",        // 보전처분일
                    "commencement_date",        // 개시결정일
                    "claim_filing_date",        // 채권신고일
                    "approval_dismissal_date",  // 인가/폐지결정일
                    "industry",                 // 업종
                    "listing_status",           // 상장/비상장
                    "employee_count",           // 종업원수
                    "establishment_date"        // 설립일
                },
                DataDiskSheetType.Loan => new List<string>
                {
                    "borrower_number",          // 차주일련번호
                    "borrower_name",            // 차주명
                    "account_serial",           // 대출일련번호
                    "loan_type",                // 대출과목
                    "account_number",           // 계좌번호
                    "normal_interest_rate",     // 정상이자율
                    "overdue_interest_rate",    // 연체이자율
                    "initial_loan_date",        // 최초대출일
                    "initial_loan_amount",      // 최초대출원금
                    "converted_loan_balance",   // 환산된 대출잔액
                    "advance_payment",          // 가지급금
                    "unpaid_principal",         // 미상환원금잔액
                    "accrued_interest",         // 미수이자
                    "total_claim_amount"        // 채권액 합계
                },
                DataDiskSheetType.RegistryDetail => new List<string>
                {
                    "borrower_number",          // 차주일련번호
                    "borrower_name",            // 차주명
                    "property_number",          // 물건번호
                    "jibun_number",             // 지번번호
                    "address_province",         // 담보소재지1
                    "address_city",             // 담보소재지2
                    "address_district",         // 담보소재지3
                    "address_detail"            // 담보소재지4
                },
                DataDiskSheetType.Guarantee => new List<string>
                {
                    // 신용보증서 대표 컬럼 (모두 직접 저장)
                    "asset_type",                       // 자산유형
                    "borrower_number",                  // 차주일련번호
                    "borrower_name",                    // 차주명
                    "loan_account_serial",              // 계좌일련번호
                    "guarantee_institution",            // 보증기관
                    "guarantee_type",                   // 보증종류
                    "guarantee_number",                 // 보증서번호
                    "guarantee_ratio",                  // 보증비율
                    "converted_guarantee_balance",      // 환산후 보증잔액
                    "related_loan_account_number"       // 관련 대출채권 계좌번호
                },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// 프로그램의 기존 시트 매핑 정보 로드
        /// </summary>
        public async Task<List<ProgramSheetMapping>> GetExistingMappingsAsync(Guid programId)
        {
            return await _sheetMappingRepository.GetByProgramIdAsync(programId);
        }

        /// <summary>
        /// 시트 매핑 정보 저장
        /// </summary>
        public async Task SaveSheetMappingsAsync(Guid programId, List<SheetMappingInfo> mappings, Guid userId, string fileName)
        {
            await _sheetMappingRepository.SaveMappingsAsync(programId, mappings, userId, fileName);
        }

        #endregion

        #region 시트 처리

        /// <summary>
        /// 선택된 시트들 일괄 처리
        /// </summary>
        public async Task<(int TotalCreated, int TotalUpdated, int TotalFailed)> ProcessSheetsAsync(
            string filePath,
            List<SheetMappingInfo> sheets,
            string programId)
        {
            int totalCreated = 0, totalUpdated = 0, totalFailed = 0;

            var selectedSheets = sheets.Where(s => s.IsSelected).ToList();

            foreach (var sheet in selectedSheets)
            {
                try
                {
                    var result = await ProcessSheetAsync(filePath, sheet, programId);
                    totalCreated += result.CreatedCount;
                    totalUpdated += result.UpdatedCount;
                    totalFailed += result.FailedCount;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DataDiskUploadService] 시트 처리 실패 ({sheet.ExcelSheetName}): {ex.Message}");
                    totalFailed++;
                }
            }

            return (totalCreated, totalUpdated, totalFailed);
        }

        /// <summary>
        /// 단일 시트 처리
        /// </summary>
        public async Task<SheetProcessResult> ProcessSheetAsync(
            string filePath,
            SheetMappingInfo sheet,
            string programId)
        {
            var result = new SheetProcessResult
            {
                SheetName = sheet.ExcelSheetName,
                SheetType = sheet.SelectedType
            };

            try
            {
                // 헤더 행 감지
                var headerRow = _excelService.DetectHeaderRow(filePath, sheet.ExcelSheetName);
                var (columns, data) = await _excelService.ReadExcelSheetAsync(filePath, sheet.ExcelSheetName, headerRow);

                System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 시트 '{sheet.ExcelSheetName}' ({sheet.SelectedType}): {data.Count}행, 헤더행={headerRow}");

                if (data.Count == 0)
                {
                    result.Success = true;
                    return result;
                }

                int totalRows = data.Count;
                int processed = 0;

                // 컬럼 매핑 정보 (사용자 지정 or 자동 감지)
                var columnMappings = sheet.ColumnMappings.Count > 0
                    ? sheet.ToColumnMappingsDictionary()
                    : GetDefaultColumnMappingsDictionary(sheet.SelectedType, columns);

                // 데이터 변환 규칙 적용
                var sheetTypeName = ConvertDataDiskTypeToStandardSheetName(sheet.SelectedType);
                foreach (var row in data)
                {
                    DataTransformService.ApplyAllTransformations(row, DetectedBankType, sheetTypeName);
                }

                // 차주번호 -> 차주ID 캐시 (Loan, Restructuring 저장 시 사용)
                var borrowerCache = new Dictionary<string, Guid>();

                switch (sheet.SelectedType)
                {
                    case DataDiskSheetType.BorrowerGeneral:
                        {
                            var (created, failed, newProcessed) = await ProcessBorrowerGeneralAsync(data, programId, processed, totalRows, borrowerCache, columnMappings);
                            result.CreatedCount = created;
                            result.FailedCount = failed;
                            processed = newProcessed;
                        }
                        break;

                    case DataDiskSheetType.BorrowerRestructuring:
                        {
                            var (created, failed, newProcessed) = await ProcessBorrowerRestructuringAsync(data, programId, processed, totalRows, borrowerCache, columnMappings);
                            result.CreatedCount = created;
                            result.FailedCount = failed;
                            processed = newProcessed;
                        }
                        break;

                    case DataDiskSheetType.Loan:
                        {
                            var (created, failed, newProcessed) = await ProcessLoanAsync(data, processed, totalRows, borrowerCache, columnMappings);
                            result.CreatedCount = created;
                            result.FailedCount = failed;
                            processed = newProcessed;
                        }
                        break;

                    case DataDiskSheetType.Property:
                        {
                            var (created, failed, newProcessed) = await ProcessPropertyAsync(data, columns, programId, processed, totalRows, columnMappings);
                            result.CreatedCount = created;
                            result.FailedCount = failed;
                            processed = newProcessed;
                        }
                        break;

                    case DataDiskSheetType.RegistryDetail:
                        {
                            var (created, failed, newProcessed) = await ProcessRegistryDetailAsync(data, programId, processed, totalRows, columnMappings);
                            result.CreatedCount = created;
                            result.FailedCount = failed;
                            processed = newProcessed;
                        }
                        break;

                    case DataDiskSheetType.Guarantee:
                        {
                            var (created, failed, newProcessed) = await ProcessGuaranteeAsync(data, programId, processed, totalRows, columnMappings);
                            result.CreatedCount = created;
                            result.FailedCount = failed;
                            processed = newProcessed;
                        }
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 지원하지 않는 시트 타입: {sheet.SelectedType}");
                        break;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 시트 처리 에러: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 시트별 처리 메서드

        private async Task<(int Created, int Failed, int Processed)> ProcessBorrowerGeneralAsync(
            List<Dictionary<string, object>> data,
            string programId,
            int startProcessed,
            int totalRows,
            Dictionary<string, Guid> borrowerCache,
            Dictionary<string, string> columnMappings)
        {
            int created = 0, failed = 0;
            int processed = startProcessed;

            foreach (var row in data)
            {
                processed++;
                OnProgressUpdate?.Invoke(processed, totalRows, "차주일반정보");

                try
                {
                    var borrower = MapRowToBorrower(row, programId, columnMappings);
                    if (string.IsNullOrEmpty(borrower.BorrowerNumber))
                    {
                        failed++;
                        continue;
                    }

                    var saved = await _borrowerRepository.CreateAsync(borrower);
                    borrowerCache[borrower.BorrowerNumber] = saved.Id;
                    created++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessBorrowerGeneral] 차주 생성 실패: {ex.Message}");
                    failed++;
                }
            }

            return (created, failed, processed);
        }

        private async Task<(int Created, int Failed, int Processed)> ProcessBorrowerRestructuringAsync(
            List<Dictionary<string, object>> data,
            string programId,
            int startProcessed,
            int totalRows,
            Dictionary<string, Guid> borrowerCache,
            Dictionary<string, string> columnMappings)
        {
            int created = 0, failed = 0;
            int processed = startProcessed;

            foreach (var row in data)
            {
                processed++;
                OnProgressUpdate?.Invoke(processed, totalRows, "회생차주정보");

                try
                {
                    var borrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings);
                    if (string.IsNullOrEmpty(borrowerNumber))
                    {
                        failed++;
                        continue;
                    }

                    // 캐시에서 차주 ID 찾기, 없으면 DB 조회
                    Guid? borrowerId = null;
                    if (borrowerCache.TryGetValue(borrowerNumber, out var cachedId))
                    {
                        borrowerId = cachedId;
                    }
                    else
                    {
                        var existingBorrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                        if (existingBorrower != null)
                        {
                            borrowerId = existingBorrower.Id;
                            borrowerCache[borrowerNumber] = existingBorrower.Id;
                        }
                    }

                    var restructuring = MapRowToBorrowerRestructuring(row, borrowerId, columnMappings);
                    if (restructuring.BorrowerId == Guid.Empty)
                    {
                        failed++;
                        continue;
                    }

                    await _restructuringRepository.CreateAsync(restructuring);

                    // 차주 회생 상태는 이미 BorrowerRestructuring 레코드로 연결됨

                    created++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessRestructuring] 회생정보 생성 실패: {ex.Message}");
                    failed++;
                }
            }

            return (created, failed, processed);
        }

        private async Task<(int Created, int Failed, int Processed)> ProcessLoanAsync(
            List<Dictionary<string, object>> data,
            int startProcessed,
            int totalRows,
            Dictionary<string, Guid> borrowerCache,
            Dictionary<string, string> columnMappings)
        {
            int created = 0, failed = 0;
            int processed = startProcessed;

            foreach (var row in data)
            {
                processed++;
                OnProgressUpdate?.Invoke(processed, totalRows, "채권정보");

                try
                {
                    var borrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings);
                    Guid? borrowerId = null;

                    if (!string.IsNullOrEmpty(borrowerNumber) && borrowerCache.TryGetValue(borrowerNumber, out var cachedId))
                    {
                        borrowerId = cachedId;
                    }

                    var loan = MapRowToLoan(row, borrowerId, columnMappings);
                    await _loanRepository.CreateAsync(loan);
                    created++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessLoan] 채권 생성 실패: {ex.Message}");
                    failed++;
                }
            }

            return (created, failed, processed);
        }

        private async Task<(int Created, int Failed, int Processed)> ProcessPropertyAsync(
            List<Dictionary<string, object>> data,
            List<string> columns,
            string programId,
            int startProcessed,
            int totalRows,
            Dictionary<string, string> columnMappings)
        {
            int created = 0, failed = 0;
            int processed = startProcessed;
            var mappingRules = SheetMappingConfig.GetMappingRules(SheetType.Property);
            int rowIndex = 0;

            foreach (var row in data)
            {
                rowIndex++;
                processed++;
                OnProgressUpdate?.Invoke(processed, totalRows, "담보물건정보");

                try
                {
                    var (property, rightData) = MapRowToPropertyWithRules(row, columns, mappingRules, programId, columnMappings);
                    if (property == null)
                    {
                        failed++;
                        continue;
                    }

                    // 물건번호 설정
                    string finalPropertyNumber = DeterminePropertyNumber(property, rowIndex);
                    property.PropertyNumber = finalPropertyNumber;

                    await _propertyRepository.CreateAsync(property);

                    // 권리분석 데이터 저장
                    if (rightData.Count > 0)
                    {
                        await SaveRightAnalysisDataAsync(property, rightData);
                    }

                    created++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessProperty] 물건 생성 실패: {ex.Message}");
                    failed++;
                }
            }

            return (created, failed, processed);
        }

        private async Task<(int Created, int Failed, int Processed)> ProcessRegistryDetailAsync(
            List<Dictionary<string, object>> data,
            string programId,
            int startProcessed,
            int totalRows,
            Dictionary<string, string> columnMappings)
        {
            int created = 0, failed = 0;
            int processed = startProcessed;

            // property_number -> property_id 캐시 (차주번호와 물건번호 조합으로 물건 조회)
            var propertyCache = new Dictionary<string, Guid>();

            foreach (var row in data)
            {
                processed++;
                OnProgressUpdate?.Invoke(processed, totalRows, "등기부등본정보");

                try
                {
                    // 대표 컬럼을 모두 직접 저장 (MapRowToRegistrySheetData에서 처리)
                    var registryData = MapRowToRegistrySheetData(row, columnMappings);

                    // 최소 필수값 체크 (차주일련번호 또는 물건번호 중 하나는 있어야 함)
                    if (string.IsNullOrEmpty(registryData.BorrowerNumber) && string.IsNullOrEmpty(registryData.PropertyNumber))
                    {
                        failed++;
                        continue;
                    }

                    // FK 연결 시도 (선택사항 - 실패해도 계속 진행)
                    var borrowerNumber = registryData.BorrowerNumber;
                    var propertyNumber = registryData.PropertyNumber;

                    if (!string.IsNullOrEmpty(borrowerNumber) || !string.IsNullOrEmpty(propertyNumber))
                    {
                        var cacheKey = $"{borrowerNumber}_{propertyNumber}";
                        if (propertyCache.TryGetValue(cacheKey, out var cachedPropertyId))
                        {
                            registryData.PropertyId = cachedPropertyId;
                        }
                        else if (!string.IsNullOrEmpty(borrowerNumber) && !string.IsNullOrEmpty(propertyNumber))
                        {
                            // 차주번호와 물건번호로 물건 조회
                            var property = await _propertyRepository.GetByBorrowerAndPropertyNumberAsync(
                                borrowerNumber, propertyNumber);

                            if (property != null)
                            {
                                registryData.PropertyId = property.Id;
                                propertyCache[cacheKey] = property.Id;
                            }
                        }
                    }

                    await _registrySheetDataRepository.UpsertAsync(registryData);
                    created++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessRegistryDetail] 등기부등본정보 생성 실패: {ex.Message}");
                    failed++;
                }
            }

            return (created, failed, processed);
        }

        private async Task<(int Created, int Failed, int Processed)> ProcessGuaranteeAsync(
            List<Dictionary<string, object>> data,
            string programId,
            int startProcessed,
            int totalRows,
            Dictionary<string, string> columnMappings)
        {
            int created = 0, failed = 0;
            int processed = startProcessed;

            // 캐시: 차주번호 -> borrower_id (FK 연결용, 선택사항)
            var borrowerCache = new Dictionary<string, Guid>();

            foreach (var row in data)
            {
                processed++;
                OnProgressUpdate?.Invoke(processed, totalRows, "신용보증서");

                try
                {
                    // 대표 컬럼 값 읽기
                    var assetType = GetMappedValue<string>(row, "asset_type", columnMappings);
                    var borrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings);
                    var borrowerName = GetMappedValue<string>(row, "borrower_name", columnMappings);
                    var guaranteeNumber = GetMappedValue<string>(row, "guarantee_number", columnMappings);

                    // 최소 필수값 체크 (차주일련번호 또는 보증서번호 중 하나는 있어야 함)
                    if (string.IsNullOrEmpty(borrowerNumber) && string.IsNullOrEmpty(guaranteeNumber))
                    {
                        failed++;
                        continue;
                    }

                    // FK 연결 시도 (선택사항 - 실패해도 계속 진행)
                    Guid? borrowerId = null;
                    if (!string.IsNullOrEmpty(borrowerNumber))
                    {
                        if (borrowerCache.TryGetValue(borrowerNumber, out var cachedBorrowerId))
                        {
                            borrowerId = cachedBorrowerId;
                        }
                        else
                        {
                            var borrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                            if (borrower != null)
                            {
                                borrowerId = borrower.Id;
                                borrowerCache[borrowerNumber] = borrower.Id;
                            }
                        }
                    }

                    var guarantee = new CreditGuarantee
                    {
                        Id = Guid.NewGuid(),
                        BorrowerId = borrowerId,
                        // 대표 컬럼들 (모두 직접 저장)
                        AssetType = assetType,
                        BorrowerNumber = borrowerNumber,
                        BorrowerName = borrowerName,
                        AccountSerial = GetMappedValue<string>(row, "loan_account_serial", columnMappings),
                        GuaranteeInstitution = GetMappedValue<string>(row, "guarantee_institution", columnMappings),
                        GuaranteeType = GetMappedValue<string>(row, "guarantee_type", columnMappings),
                        GuaranteeNumber = guaranteeNumber,
                        GuaranteeRatio = GetMappedValue<decimal?>(row, "guarantee_ratio", columnMappings),
                        ConvertedGuaranteeBalance = GetMappedValue<decimal?>(row, "converted_guarantee_balance", columnMappings),
                        RelatedLoanAccountNumber = GetMappedValue<string>(row, "related_loan_account_number", columnMappings),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _creditGuaranteeRepository.UpsertAsync(guarantee);
                    created++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProcessGuarantee] 신용보증서 생성 실패: {ex.Message}");
                    failed++;
                }
            }

            return (created, failed, processed);
        }

        #endregion

        #region 매핑 메서드

        private Borrower MapRowToBorrower(Dictionary<string, object> row, string programId, Dictionary<string, string> columnMappings)
        {
            var borrower = new Borrower
            {
                Id = Guid.NewGuid(),
                ProgramId = programId
            };

            // 차주일반정보 대표컬럼
            borrower.AssetType = GetMappedValue<string>(row, "asset_type", columnMappings);
            borrower.BorrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings) ?? "";
            borrower.BorrowerName = GetMappedValue<string>(row, "borrower_name", columnMappings) ?? "";
            borrower.RelatedBorrower = GetMappedValue<string>(row, "related_borrower", columnMappings);
            borrower.BorrowerType = GetMappedValue<string>(row, "borrower_type", columnMappings) ?? "개인";
            borrower.UnpaidPrincipal = GetMappedValue<decimal?>(row, "unpaid_principal", columnMappings);
            borrower.AccruedInterest = GetMappedValue<decimal?>(row, "accrued_interest", columnMappings);
            borrower.MortgageAmount = GetMappedValue<decimal?>(row, "mortgage_amount", columnMappings) ?? 0;
            borrower.Notes = GetMappedValue<string>(row, "notes", columnMappings);
            // 기타
            borrower.Opb = GetMappedValue<decimal?>(row, "opb", columnMappings) ?? 0;

            return borrower;
        }

        private BorrowerRestructuring MapRowToBorrowerRestructuring(Dictionary<string, object> row, Guid? borrowerId, Dictionary<string, string> columnMappings)
        {
            return new BorrowerRestructuring
            {
                Id = Guid.NewGuid(),
                BorrowerId = borrowerId ?? Guid.Empty,
                // 회생차주정보 대표컬럼 (직접 저장)
                AssetType = GetMappedValue<string>(row, "asset_type", columnMappings),
                BorrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings),
                BorrowerName = GetMappedValue<string>(row, "borrower_name", columnMappings),
                // 회생 정보
                ApprovalStatus = GetMappedValue<string>(row, "approval_status", columnMappings),
                ProgressStage = GetMappedValue<string>(row, "progress_stage", columnMappings),
                CourtName = GetMappedValue<string>(row, "court_name", columnMappings),
                CaseNumber = GetMappedValue<string>(row, "case_number", columnMappings),
                FilingDate = GetMappedValue<DateTime?>(row, "filing_date", columnMappings),
                PreservationDate = GetMappedValue<DateTime?>(row, "preservation_date", columnMappings),
                CommencementDate = GetMappedValue<DateTime?>(row, "commencement_date", columnMappings),
                ClaimFilingDate = GetMappedValue<DateTime?>(row, "claim_filing_date", columnMappings),
                ApprovalDismissalDate = GetMappedValue<DateTime?>(row, "approval_dismissal_date", columnMappings),
                // 회사 정보
                Industry = GetMappedValue<string>(row, "industry", columnMappings),
                ListingStatus = GetMappedValue<string>(row, "listing_status", columnMappings),
                EmployeeCount = GetMappedValue<int?>(row, "employee_count", columnMappings),
                EstablishmentDate = GetMappedValue<DateTime?>(row, "establishment_date", columnMappings)
            };
        }

        private Loan MapRowToLoan(Dictionary<string, object> row, Guid? borrowerId, Dictionary<string, string> columnMappings)
        {
            // 기본 값 추출
            var borrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings);
            var borrowerName = GetMappedValue<string>(row, "borrower_name", columnMappings);
            var accountSerial = GetMappedValue<string>(row, "account_serial", columnMappings);
            var normalRate = GetMappedValue<decimal?>(row, "normal_interest_rate", columnMappings);
            var overdueRate = GetMappedValue<decimal?>(row, "overdue_interest_rate", columnMappings);
            var convertedLoanBalance = GetMappedValue<decimal?>(row, "converted_loan_balance", columnMappings);
            var unpaidPrincipal = GetMappedValue<decimal?>(row, "unpaid_principal", columnMappings);
            var advancePayment = GetMappedValue<decimal?>(row, "advance_payment", columnMappings) ?? 0;
            var accruedInterest = GetMappedValue<decimal?>(row, "accrued_interest", columnMappings) ?? 0;
            var totalClaimAmount = GetMappedValue<decimal?>(row, "total_claim_amount", columnMappings);

            // [규칙1] 대출일련번호가 숫자인 경우 -> "차주일련번호-대출일련번호" 형식으로 변환
            if (!string.IsNullOrEmpty(accountSerial) && !string.IsNullOrEmpty(borrowerNumber))
            {
                // 숫자만 있는지 확인
                if (accountSerial.All(char.IsDigit))
                {
                    accountSerial = $"{borrowerNumber}-{accountSerial}";
                }
            }

            // [규칙2] 정상이자율만 있고 연체이자율 없으면 -> 연체이자율 = 정상이자율 + 3%
            // [규칙3] 연체이자율만 있고 정상이자율 없으면 -> 정상이자율 = 연체이자율 - 3%
            if (normalRate.HasValue && !overdueRate.HasValue)
            {
                overdueRate = normalRate.Value + 0.03m;
            }
            else if (overdueRate.HasValue && !normalRate.HasValue)
            {
                normalRate = overdueRate.Value - 0.03m;
            }

            // [규칙4] 채권액 합계 없으면 -> 환산된 대출잔액(또는 미상환원금잔액) + 가지급금 + 미수이자
            if (!totalClaimAmount.HasValue)
            {
                var balanceForCalc = convertedLoanBalance ?? unpaidPrincipal ?? 0;
                totalClaimAmount = balanceForCalc + advancePayment + accruedInterest;
            }

            return new Loan
            {
                Id = Guid.NewGuid(),
                BorrowerId = borrowerId,
                // 채권일반정보 대표컬럼 (직접 저장)
                BorrowerNumber = borrowerNumber,
                BorrowerName = borrowerName,
                AccountSerial = accountSerial,
                LoanType = GetMappedValue<string>(row, "loan_type", columnMappings),
                AccountNumber = GetMappedValue<string>(row, "account_number", columnMappings),
                NormalInterestRate = normalRate,
                OverdueInterestRate = overdueRate,
                InitialLoanDate = GetMappedValue<DateTime?>(row, "initial_loan_date", columnMappings),
                LastInterestDate = GetMappedValue<DateTime?>(row, "last_interest_date", columnMappings),
                InitialLoanAmount = GetMappedValue<decimal?>(row, "initial_loan_amount", columnMappings),
                ConvertedLoanBalance = convertedLoanBalance,
                UnpaidPrincipal = unpaidPrincipal,
                AdvancePayment = advancePayment,
                AccruedInterest = accruedInterest,
                TotalClaimAmount = totalClaimAmount
            };
        }

        private RegistrySheetData MapRowToRegistrySheetData(Dictionary<string, object> row, Dictionary<string, string> columnMappings)
        {
            // 등기부등본정보 대표 컬럼 (모두 직접 저장)
            return new RegistrySheetData
            {
                Id = Guid.NewGuid(),
                BorrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings),
                BorrowerName = GetMappedValue<string>(row, "borrower_name", columnMappings),
                PropertyNumber = GetMappedValue<string>(row, "property_number", columnMappings),
                JibunNumber = GetMappedValue<string>(row, "jibun_number", columnMappings),
                AddressProvince = GetMappedValue<string>(row, "address_province", columnMappings),
                AddressCity = GetMappedValue<string>(row, "address_city", columnMappings),
                AddressDistrict = GetMappedValue<string>(row, "address_district", columnMappings),
                AddressDetail = GetMappedValue<string>(row, "address_detail", columnMappings),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private (Property? Property, Dictionary<string, object?> RightData) MapRowToPropertyWithRules(
            Dictionary<string, object> row,
            List<string> columns,
            List<ColumnMappingRule> rules,
            string programId,
            Dictionary<string, string> columnMappings)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                ProjectId = programId,
                ProgramId = Guid.TryParse(programId, out var guid) ? guid : null,
                Status = "pending"
            };

            var addressParts = new Dictionary<string, string>();
            var rightData = new Dictionary<string, object?>();

            foreach (var col in columns)
            {
                // 사용자 지정 매핑 확인
                string? dbColumn = null;
                if (columnMappings.TryGetValue(col, out var mappedCol))
                {
                    dbColumn = mappedCol;
                }
                else
                {
                    // 기본 매핑 규칙 사용
                    var normalizedCol = col.Replace("\n", " ").Replace("\r", "").Trim();
                    var rule = SheetMappingConfig.FindMappingRule(rules, normalizedCol);
                    dbColumn = rule?.DbColumnName;
                }

                if (string.IsNullOrEmpty(dbColumn))
                    continue;

                var rawValue = row.ContainsKey(col) ? row[col] : null;
                var value = ConvertValue(rawValue, dbColumn);

                // Property 필드 매핑
                MapPropertyField(property, dbColumn, value, addressParts, rightData);
            }

            // 주소 조합
            CombineAddress(property, addressParts);

            return (property, rightData);
        }

        private void MapPropertyField(Property property, string dbColumn, object? value,
            Dictionary<string, string> addressParts, Dictionary<string, object?> rightData)
        {
            switch (dbColumn)
            {
                // ========== 기본 정보 (물건정보 대표컬럼) ==========
                case "asset_type":
                    property.AssetType = value?.ToString();
                    break;
                case "borrower_number":
                    property.BorrowerNumber = value?.ToString();
                    break;
                case "borrower_name":
                    property.BorrowerName = value?.ToString();
                    property.DebtorName = value?.ToString(); // 기존 DebtorName도 호환성 유지
                    break;
                case "collateral_number":
                    property.CollateralNumber = value?.ToString();
                    break;
                case "property_type":
                    property.PropertyType = NormalizePropertyType(value?.ToString());
                    break;
                case "property_number":
                    if (!string.IsNullOrEmpty(value?.ToString()))
                        property.PropertyNumber = value?.ToString();
                    break;

                // ========== 주소 ==========
                case "address_province":
                    property.AddressProvince = value?.ToString();
                    if (value != null) addressParts["province"] = value.ToString()!;
                    break;
                case "address_city":
                    property.AddressCity = value?.ToString();
                    if (value != null) addressParts["city"] = value.ToString()!;
                    break;
                case "address_district":
                    property.AddressDistrict = value?.ToString();
                    if (value != null) addressParts["district"] = value.ToString()!;
                    break;
                case "address_detail":
                    if (value != null) addressParts["detail"] = value.ToString()!;
                    break;

                // ========== 면적/금액 ==========
                case "land_area":
                    property.LandArea = value as decimal?;
                    break;
                case "building_area":
                    property.BuildingArea = value as decimal?;
                    break;
                case "machinery_value":
                    property.MachineryValue = value as decimal?;
                    break;
                case "joint_collateral_amount":
                    property.JointCollateralAmount = value as decimal?;
                    break;

                // ========== 선순위 정보 ==========
                case "senior_mortgage_amount":
                    property.SeniorMortgageAmount = value as decimal?;
                    break;
                case "senior_housing_small_deposit":
                    property.SeniorHousingSmallDeposit = value as decimal?;
                    break;
                case "senior_commercial_small_deposit":
                    property.SeniorCommercialSmallDeposit = value as decimal?;
                    break;
                case "senior_small_deposit":
                    property.SeniorSmallDeposit = value as decimal?;
                    rightData["small_deposit_dd"] = value;
                    break;
                case "senior_housing_lease_deposit":
                    property.SeniorHousingLeaseDeposit = value as decimal?;
                    break;
                case "senior_commercial_lease_deposit":
                    property.SeniorCommercialLeaseDeposit = value as decimal?;
                    break;
                case "senior_lease_deposit":
                    property.SeniorLeaseDeposit = value as decimal?;
                    rightData["lease_deposit_dd"] = value;
                    break;
                case "senior_wage_claim":
                    property.SeniorWageClaim = value as decimal?;
                    rightData["wage_claim_dd"] = value;
                    break;
                case "senior_current_tax":
                    property.SeniorCurrentTax = value as decimal?;
                    rightData["current_tax_dd"] = value;
                    break;
                case "senior_tax_claim":
                    property.SeniorTaxClaim = value as decimal?;
                    rightData["senior_tax_dd"] = value;
                    break;
                case "senior_etc":
                    property.SeniorEtc = value as decimal?;
                    rightData["etc_dd"] = value;
                    break;
                case "senior_total":
                    property.SeniorTotal = value as decimal?;
                    break;

                // ========== 감정평가 정보 ==========
                case "appraisal_type":
                    property.AppraisalType = value?.ToString();
                    break;
                case "appraisal_date":
                    property.AppraisalDate = value as DateTime?;
                    break;
                case "appraisal_agency":
                    property.AppraisalAgency = value?.ToString();
                    break;
                case "land_appraisal_value":
                    property.LandAppraisalValue = value as decimal?;
                    break;
                case "building_appraisal_value":
                    property.BuildingAppraisalValue = value as decimal?;
                    break;
                case "machinery_appraisal_value":
                    property.MachineryAppraisalValue = value as decimal?;
                    break;
                case "excluded_appraisal":
                    property.ExcludedAppraisal = value as decimal?;
                    break;
                case "appraisal_value":
                    property.AppraisalValue = value as decimal?;
                    rightData["appraisal_value"] = value;
                    break;
                case "kb_price":
                    property.KbPrice = value as decimal?;
                    break;

                // ========== 경매 기본 정보 ==========
                case "auction_started":
                    if (value is bool boolVal)
                        property.AuctionStarted = boolVal;
                    else if (value != null)
                    {
                        var strVal = value.ToString()?.Trim().ToLower();
                        property.AuctionStarted = strVal == "y" || strVal == "yes" || strVal == "true" || strVal == "1" || strVal == "예" || strVal == "개시";
                    }
                    break;
                case "auction_court":
                    property.AuctionCourt = value?.ToString();
                    rightData["court_name"] = value;
                    break;

                // ========== 경매 선행 정보 ==========
                case "precedent_auction_applicant":
                    property.PrecedentAuctionApplicant = value?.ToString();
                    break;
                case "precedent_auction_start_date":
                    property.PrecedentAuctionStartDate = value as DateTime?;
                    break;
                case "precedent_case_number":
                    property.PrecedentCaseNumber = value?.ToString();
                    break;
                case "precedent_claim_deadline":
                    property.PrecedentClaimDeadline = value as DateTime?;
                    break;
                case "precedent_claim_amount":
                    property.PrecedentClaimAmount = value as decimal?;
                    break;

                // ========== 경매 후행 정보 ==========
                case "subsequent_auction_applicant":
                    property.SubsequentAuctionApplicant = value?.ToString();
                    break;
                case "subsequent_auction_start_date":
                    property.SubsequentAuctionStartDate = value as DateTime?;
                    break;
                case "subsequent_case_number":
                    property.SubsequentCaseNumber = value?.ToString();
                    break;
                case "subsequent_claim_deadline":
                    property.SubsequentClaimDeadline = value as DateTime?;
                    break;
                case "subsequent_claim_amount":
                    property.SubsequentClaimAmount = value as decimal?;
                    break;

                // ========== 경매 기일/결과 정보 ==========
                case "initial_court_value":
                    property.InitialCourtValue = value as decimal?;
                    break;
                case "first_auction_date":
                    property.FirstAuctionDate = value as DateTime?;
                    break;
                case "final_auction_round":
                    if (value is int intVal)
                        property.FinalAuctionRound = intVal;
                    else if (int.TryParse(value?.ToString(), out var parsed))
                        property.FinalAuctionRound = parsed;
                    break;
                case "final_auction_result":
                    property.FinalAuctionResult = value?.ToString();
                    break;
                case "final_auction_date":
                    property.FinalAuctionDate = value as DateTime?;
                    break;
                case "next_auction_date":
                    property.NextAuctionDate = value as DateTime?;
                    break;
                case "winning_bid_amount":
                    property.WinningBidAmount = value as decimal?;
                    break;
                case "final_minimum_bid":
                    property.FinalMinimumBid = value as decimal?;
                    property.MinimumBid = value as decimal?;
                    rightData["final_min_bid"] = value;
                    break;
                case "next_minimum_bid":
                    property.NextMinimumBid = value as decimal?;
                    break;
                case "notes":
                    property.Notes = value?.ToString();
                    break;
            }
        }

        private void CombineAddress(Property property, Dictionary<string, string> addressParts)
        {
            var components = new List<string>();
            if (addressParts.TryGetValue("province", out var prov) && !string.IsNullOrEmpty(prov))
                components.Add(prov);
            if (addressParts.TryGetValue("city", out var city) && !string.IsNullOrEmpty(city))
                components.Add(city);
            if (addressParts.TryGetValue("district", out var dist) && !string.IsNullOrEmpty(dist))
                components.Add(dist);
            if (addressParts.TryGetValue("detail", out var detail) && !string.IsNullOrEmpty(detail))
                components.Add(detail);

            if (components.Count > 0)
            {
                property.AddressFull = string.Join(" ", components);
            }
        }

        private string DeterminePropertyNumber(Property property, int rowIndex)
        {
            if (!string.IsNullOrEmpty(property.PropertyNumber) && !property.PropertyNumber.All(char.IsDigit))
            {
                return property.PropertyNumber;
            }
            if (!string.IsNullOrEmpty(property.CollateralNumber))
            {
                return property.CollateralNumber;
            }
            return $"P-{rowIndex:D4}";
        }

        private async Task SaveRightAnalysisDataAsync(Property property, Dictionary<string, object?> rightData)
        {
            try
            {
                var existing = await _rightAnalysisRepository.GetByPropertyIdAsync(property.Id);
                var rightAnalysis = existing ?? new RightAnalysis
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // DD 선순위 금액 설정
                if (rightData.TryGetValue("small_deposit_dd", out var smallDeposit) && smallDeposit is decimal sd)
                    rightAnalysis.SmallDepositDd = sd;
                if (rightData.TryGetValue("lease_deposit_dd", out var leaseDeposit) && leaseDeposit is decimal ld)
                    rightAnalysis.LeaseDepositDd = ld;
                if (rightData.TryGetValue("wage_claim_dd", out var wageClaim) && wageClaim is decimal wc)
                    rightAnalysis.WageClaimDd = wc;
                if (rightData.TryGetValue("current_tax_dd", out var currentTax) && currentTax is decimal ct)
                    rightAnalysis.CurrentTaxDd = ct;
                if (rightData.TryGetValue("senior_tax_dd", out var seniorTax) && seniorTax is decimal st)
                    rightAnalysis.SeniorTaxDd = st;
                if (rightData.TryGetValue("etc_dd", out var etcDd) && etcDd is decimal etc)
                    rightAnalysis.EtcDd = etc;

                // 감정평가 정보
                if (rightData.TryGetValue("appraisal_value", out var appraisalValue) && appraisalValue is decimal av)
                    rightAnalysis.AppraisalValue = av;

                // 경매 정보
                if (rightData.TryGetValue("court_name", out var courtName) && courtName != null)
                    rightAnalysis.CourtName = courtName.ToString();
                if (rightData.TryGetValue("final_min_bid", out var minBid) && minBid is decimal mb)
                    rightAnalysis.MinimumBid = mb;

                // 선순위 합계
                rightAnalysis.SeniorTotalDd =
                    rightAnalysis.SmallDepositDd +
                    rightAnalysis.LeaseDepositDd +
                    rightAnalysis.WageClaimDd +
                    rightAnalysis.CurrentTaxDd +
                    rightAnalysis.SeniorTaxDd +
                    rightAnalysis.EtcDd;

                rightAnalysis.UpdatedAt = DateTime.UtcNow;

                await _rightAnalysisRepository.UpsertAsync(rightAnalysis);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveRightAnalysisDataAsync] 에러: {ex.Message}");
            }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// SheetType (ExcelService) -> DataDiskSheetType 변환
        /// </summary>
        private DataDiskSheetType ConvertSheetType(SheetType excelSheetType)
        {
            return excelSheetType switch
            {
                SheetType.BorrowerGeneral => DataDiskSheetType.BorrowerGeneral,
                SheetType.BorrowerRestructuring => DataDiskSheetType.BorrowerRestructuring,
                SheetType.Loan => DataDiskSheetType.Loan,
                SheetType.Property => DataDiskSheetType.Property,
                SheetType.RegistryDetail => DataDiskSheetType.RegistryDetail,
                SheetType.Guarantee => DataDiskSheetType.Guarantee,
                _ => DataDiskSheetType.Unknown
            };
        }

        /// <summary>
        /// DataDiskSheetType -> SheetType (ExcelService) 변환
        /// </summary>
        private SheetType ConvertToExcelSheetType(DataDiskSheetType sheetType)
        {
            return sheetType switch
            {
                DataDiskSheetType.BorrowerGeneral => SheetType.BorrowerGeneral,
                DataDiskSheetType.BorrowerRestructuring => SheetType.BorrowerRestructuring,
                DataDiskSheetType.Loan => SheetType.Loan,
                DataDiskSheetType.Property => SheetType.Property,
                DataDiskSheetType.RegistryDetail => SheetType.RegistryDetail,
                DataDiskSheetType.Guarantee => SheetType.Guarantee,
                _ => SheetType.Unknown
            };
        }

        /// <summary>
        /// 기본 컬럼 매핑을 Dictionary로 반환
        /// </summary>
        private Dictionary<string, string> GetDefaultColumnMappingsDictionary(DataDiskSheetType sheetType, List<string> headers)
        {
            var mappings = GetDefaultColumnMappings(sheetType, headers);
            var dict = new Dictionary<string, string>();

            foreach (var mapping in mappings)
            {
                if (!string.IsNullOrEmpty(mapping.DbColumn))
                {
                    dict[mapping.ExcelColumn] = mapping.DbColumn;
                }
            }

            return dict;
        }

        /// <summary>
        /// DB 컬럼 표시명 가져오기
        /// </summary>
        private string GetDbColumnDisplayName(string dbColumn)
        {
            return dbColumn switch
            {
                // 차주일반정보 대표컬럼
                "asset_type" => "자산유형",
                "borrower_number" => "차주일련번호",
                "borrower_name" => "차주명",
                "related_borrower" => "관련차주",
                "borrower_type" => "차주형태",
                "unpaid_principal" => "미상환원금잔액",
                "accrued_interest" => "미수이자",
                "mortgage_amount" => "근저당권설정액",
                "notes" => "비고",

                // 물건정보 기본 대표컬럼
                "address_province" => "담보소재지1",
                "address_city" => "담보소재지2",
                "address_district" => "담보소재지3",
                "address_detail" => "담보소재지4",
                "property_type" => "물건종류",
                "land_area" => "물건 대지면적",
                "building_area" => "물건 건물면적",
                "machinery_value" => "물건 기타(기계기구 등)",
                "collateral_number" => "물건 일련번호",
                "joint_collateral_amount" => "공담 물건 금액",

                // 물건정보 선순위 대표컬럼
                "senior_mortgage_amount" => "물건별 선순위 설정액",
                "senior_housing_small_deposit" => "선순위 주택 소액보증금",
                "senior_commercial_small_deposit" => "선순위 상가 소액보증금",
                "senior_small_deposit" => "선순위 소액보증금",
                "senior_housing_lease_deposit" => "선순위 주택 임차보증금",
                "senior_commercial_lease_deposit" => "선순위 상가 임차보증금",
                "senior_lease_deposit" => "선순위 임차보증금",
                "senior_wage_claim" => "선순위 임금채권",
                "senior_current_tax" => "선순위 당해세",
                "senior_tax_claim" => "선순위 조세채권",
                "senior_etc" => "선순위 기타",
                "senior_total" => "선순위 합계",

                // 물건정보 감정평가 대표컬럼
                "appraisal_type" => "감정평가구분",
                "appraisal_date" => "감정평가일자",
                "appraisal_agency" => "감정평가기관",
                "land_appraisal_value" => "토지감정평가액",
                "building_appraisal_value" => "건물감정평가액",
                "machinery_appraisal_value" => "기계평가액",
                "excluded_appraisal" => "제시외",
                "appraisal_value" => "감정평가액합계",
                "kb_price" => "KB아파트시세",

                // 물건정보 경매 기본 대표컬럼
                "auction_started" => "경매개시여부",
                "auction_court" => "경매 관할법원",

                // 물건정보 경매 선행 대표컬럼
                "precedent_auction_applicant" => "경매신청기관(선행)",
                "precedent_auction_start_date" => "경매개시일자(선행)",
                "precedent_case_number" => "경매사건번호(선행)",
                "precedent_claim_deadline" => "배당요구종기일(선행)",
                "precedent_claim_amount" => "청구금액(선행)",

                // 물건정보 경매 후행 대표컬럼
                "subsequent_auction_applicant" => "경매신청기관(후행)",
                "subsequent_auction_start_date" => "경매개시일자(후행)",
                "subsequent_case_number" => "경매사건번호(후행)",
                "subsequent_claim_deadline" => "배당요구종기일(후행)",
                "subsequent_claim_amount" => "청구금액(후행)",

                // 물건정보 경매 기일/결과 대표컬럼
                "initial_court_value" => "최초법사가",
                "first_auction_date" => "최초경매기일",
                "final_auction_round" => "최종경매회차",
                "final_auction_result" => "최종경매결과",
                "final_auction_date" => "최종경매기일",
                "next_auction_date" => "차기경매기일",
                "winning_bid_amount" => "낙찰금액",
                "final_minimum_bid" => "최종경매일의 최저입찰금액",
                "next_minimum_bid" => "차후최종경매일의 최저입찰금액",

                // 채권일반정보 대표컬럼
                "account_serial" => "대출일련번호",
                "loan_type" => "대출과목",
                "account_number" => "계좌번호",
                "normal_interest_rate" => "정상이자율",
                "overdue_interest_rate" => "연체이자율",
                "initial_loan_date" => "최초대출일",
                "initial_loan_amount" => "최초대출원금",
                "converted_loan_balance" => "환산된 대출잔액",
                "loan_principal_balance" => "대출금잔액",
                "advance_payment" => "가지급금",
                "total_claim_amount" => "채권액 합계",

                // 회생차주정보 대표컬럼
                "progress_stage" => "세부진행단계",
                "court_name" => "관할법원",
                "case_number" => "회생사건번호",
                "preservation_date" => "보전처분일",
                "commencement_date" => "개시결정일",
                "claim_filing_date" => "채권신고일",
                "approval_dismissal_date" => "인가/폐지결정일",
                "industry" => "업종",
                "listing_status" => "상장/비상장",
                "employee_count" => "종업원수",
                "establishment_date" => "설립일",

                // 신용보증서정보 대표컬럼
                "loan_account_serial" => "계좌일련번호",
                "guarantee_institution" => "보증기관",
                "guarantee_type" => "보증종류",
                "guarantee_number" => "보증서번호",
                "guarantee_ratio" => "보증비율",
                "converted_guarantee_balance" => "환산후 보증잔액",
                "guarantee_amount" => "보증금액",
                "related_loan_account_number" => "관련 대출채권 계좌번호",

                // 등기부등본정보 대표컬럼
                "jibun_number" => "지번번호",

                // 기타 (자동매칭용)
                "opb" => "대출원금잔액",
                "property_number" => "물건번호",
                "senior_other" => "기타 선순위",
                _ => dbColumn
            };
        }

        /// <summary>
        /// 매핑된 컬럼에서 값 추출
        /// </summary>
        private T? GetMappedValue<T>(Dictionary<string, object> row, string dbColumn, Dictionary<string, string> columnMappings)
        {
            // columnMappings에서 dbColumn에 해당하는 Excel 컬럼 찾기
            var excelColumn = columnMappings.FirstOrDefault(x => x.Value == dbColumn).Key;
            
            if (string.IsNullOrEmpty(excelColumn))
            {
                // 기본 컬럼명으로 직접 검색
                foreach (var kvp in row)
                {
                    var normalizedKey = kvp.Key?.ToLower().Replace("\n", " ").Replace("\r", "").Trim() ?? "";
                    if (MatchesDbColumn(normalizedKey, dbColumn))
                    {
                        return ConvertTo<T>(kvp.Value);
                    }
                }
                return default;
            }

            if (!row.TryGetValue(excelColumn, out var value))
                return default;

            return ConvertTo<T>(value);
        }

        private bool MatchesDbColumn(string normalizedKey, string dbColumn)
        {
            return dbColumn switch
            {
                "borrower_number" => normalizedKey.Contains("차주일련번호") || normalizedKey.Contains("차주번호"),
                "borrower_name" => normalizedKey.Contains("차주명"),
                "borrower_type" => normalizedKey.Contains("차주형태") || normalizedKey.Contains("차주유형"),
                _ => false
            };
        }

        private T? ConvertTo<T>(object? value)
        {
            if (value == null)
                return default;

            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)value.ToString()!;

                if (targetType == typeof(decimal))
                {
                    var str = value.ToString()?.Replace(",", "").Replace(" ", "").Trim();
                    if (string.IsNullOrEmpty(str) || str == "-")
                        return default;
                    if (decimal.TryParse(str, out var dec))
                        return (T)(object)dec;
                }

                if (targetType == typeof(int))
                {
                    if (int.TryParse(value.ToString(), out var i))
                        return (T)(object)i;
                }

                if (targetType == typeof(DateTime))
                {
                    if (value is DateTime dt)
                        return (T)(object)dt;
                    if (DateTime.TryParse(value.ToString(), out var parsed))
                        return (T)(object)parsed;
                }

                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return default;
            }
        }

        private object? ConvertValue(object? rawValue, string dbColumn)
        {
            if (rawValue == null)
                return null;

            // dbColumn에 따라 적절한 타입으로 변환
            var isNumeric = dbColumn.Contains("amount") || dbColumn.Contains("area") || 
                           dbColumn.Contains("value") || dbColumn.Contains("rate") ||
                           dbColumn.Contains("deposit") || dbColumn.Contains("claim") ||
                           dbColumn.Contains("tax") || dbColumn.Contains("bid");

            if (isNumeric)
            {
                var str = rawValue.ToString()?.Replace(",", "").Replace(" ", "").Trim();
                if (string.IsNullOrEmpty(str) || str == "-")
                    return null;
                if (decimal.TryParse(str, out var dec))
                    return dec;
            }

            var isDate = dbColumn.Contains("date");
            if (isDate)
            {
                if (rawValue is DateTime dt)
                    return dt;
                if (DateTime.TryParse(rawValue.ToString(), out var parsed))
                    return parsed;
            }

            return rawValue.ToString()?.Trim();
        }

        /// <summary>
        /// 물건종류(담보종류) 값 정규화
        /// 피드백 9번: 물건종류 인식 오류 수정
        /// </summary>
        private static string NormalizePropertyType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "기타";

            // 공백 제거 및 소문자 변환
            var normalized = value.Trim().ToLower();

            // 알려진 유형으로 매핑
            return normalized switch
            {
                "아파트" or "apartment" or "apt" => "아파트",
                "상가" or "store" or "commercial" or "상업" or "근생" or "근린상가" or "근린생활시설" => "상가",
                "토지" or "land" or "대지" or "임야" or "전" or "답" or "잡종지" => "토지",
                "빌라" or "villa" or "연립" or "연립주택" or "다세대" or "다세대주택" => "빌라",
                "오피스텔" or "officetel" or "오피" => "오피스텔",
                "단독주택" or "house" or "단독" or "주택" => "단독주택",
                "다가구주택" or "multi-family" or "다가구" => "다가구주택",
                "공장" or "factory" or "창고" or "warehouse" or "공장창고" => "공장",
                _ => string.IsNullOrWhiteSpace(value) ? "기타" : value.Trim() // 알 수 없는 값은 원본 유지
            };
        }

        #endregion
    }
}
