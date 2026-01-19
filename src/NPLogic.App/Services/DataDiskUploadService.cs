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
            ProgramSheetMappingRepository sheetMappingRepository)
        {
            _excelService = excelService;
            _propertyRepository = propertyRepository;
            _borrowerRepository = borrowerRepository;
            _loanRepository = loanRepository;
            _restructuringRepository = restructuringRepository;
            _rightAnalysisRepository = rightAnalysisRepository;
            _interimRepository = interimRepository;
            _sheetMappingRepository = sheetMappingRepository;
        }

        #region 시트 로드 및 매핑

        /// <summary>
        /// Excel 파일에서 시트 목록 로드 (자동 타입 감지 포함)
        /// </summary>
        public List<SheetMappingInfo> LoadExcelSheets(string filePath)
        {
            var sheets = _excelService.GetSheetNames(filePath);
            var result = new List<SheetMappingInfo>();

            foreach (var sheet in sheets)
            {
                var detectedType = ConvertSheetType(sheet.SheetType);
                
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
        /// 시트의 기본 컬럼 매핑 가져오기
        /// </summary>
        public List<ColumnMappingInfo> GetDefaultColumnMappings(DataDiskSheetType sheetType, List<string> headers)
        {
            var mappingRules = SheetMappingConfig.GetMappingRules(ConvertToExcelSheetType(sheetType));
            var result = new List<ColumnMappingInfo>();

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var normalizedHeader = header.Replace("\n", " ").Replace("\r", "").Trim();
                var rule = SheetMappingConfig.FindMappingRule(mappingRules, normalizedHeader);

                result.Add(new ColumnMappingInfo
                {
                    ExcelColumn = header,
                    DbColumn = rule?.DbColumnName,
                    DbColumnDisplay = rule != null ? GetDbColumnDisplayName(rule.DbColumnName) : null,
                    IsAutoMatched = rule != null,
                    IsRequired = rule?.IsRequired ?? false
                });
            }

            return result;
        }

        /// <summary>
        /// 시트 타입별 사용 가능한 DB 컬럼 목록 가져오기
        /// </summary>
        public List<(string DbColumn, string DisplayName)> GetAvailableDbColumns(DataDiskSheetType sheetType)
        {
            var mappingRules = SheetMappingConfig.GetMappingRules(ConvertToExcelSheetType(sheetType));
            
            // 중복 제거하여 DB 컬럼 목록 반환
            return mappingRules
                .Select(r => r.DbColumnName)
                .Distinct()
                .Select(col => (col, GetDbColumnDisplayName(col)))
                .OrderBy(x => x.Item2)
                .ToList();
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

        #endregion

        #region 매핑 메서드

        private Borrower MapRowToBorrower(Dictionary<string, object> row, string programId, Dictionary<string, string> columnMappings)
        {
            var borrower = new Borrower
            {
                Id = Guid.NewGuid(),
                ProgramId = programId
            };

            borrower.BorrowerNumber = GetMappedValue<string>(row, "borrower_number", columnMappings) ?? "";
            borrower.BorrowerName = GetMappedValue<string>(row, "borrower_name", columnMappings) ?? "";
            borrower.BorrowerType = GetMappedValue<string>(row, "borrower_type", columnMappings) ?? "개인";
            borrower.Opb = GetMappedValue<decimal?>(row, "opb", columnMappings) ?? 0;
            borrower.MortgageAmount = GetMappedValue<decimal?>(row, "mortgage_amount", columnMappings) ?? 0;

            return borrower;
        }

        private BorrowerRestructuring MapRowToBorrowerRestructuring(Dictionary<string, object> row, Guid? borrowerId, Dictionary<string, string> columnMappings)
        {
            return new BorrowerRestructuring
            {
                Id = Guid.NewGuid(),
                BorrowerId = borrowerId ?? Guid.Empty,
                ApprovalStatus = GetMappedValue<string>(row, "approval_status", columnMappings),
                ProgressStage = GetMappedValue<string>(row, "progress_stage", columnMappings),
                CourtName = GetMappedValue<string>(row, "court_name", columnMappings),
                CaseNumber = GetMappedValue<string>(row, "case_number", columnMappings),
                FilingDate = GetMappedValue<DateTime?>(row, "filing_date", columnMappings),
                PreservationDate = GetMappedValue<DateTime?>(row, "preservation_date", columnMappings),
                CommencementDate = GetMappedValue<DateTime?>(row, "commencement_date", columnMappings),
                ClaimFilingDate = GetMappedValue<DateTime?>(row, "claim_filing_date", columnMappings)
            };
        }

        private Loan MapRowToLoan(Dictionary<string, object> row, Guid? borrowerId, Dictionary<string, string> columnMappings)
        {
            return new Loan
            {
                Id = Guid.NewGuid(),
                BorrowerId = borrowerId,
                AccountSerial = GetMappedValue<string>(row, "account_serial", columnMappings),
                LoanType = GetMappedValue<string>(row, "loan_type", columnMappings),
                AccountNumber = GetMappedValue<string>(row, "account_number", columnMappings),
                NormalInterestRate = GetMappedValue<decimal?>(row, "normal_interest_rate", columnMappings),
                InitialLoanDate = GetMappedValue<DateTime?>(row, "initial_loan_date", columnMappings),
                LastInterestDate = GetMappedValue<DateTime?>(row, "last_interest_date", columnMappings),
                InitialLoanAmount = GetMappedValue<decimal?>(row, "initial_loan_amount", columnMappings),
                LoanPrincipalBalance = GetMappedValue<decimal?>(row, "loan_principal_balance", columnMappings),
                AccruedInterest = GetMappedValue<decimal?>(row, "accrued_interest", columnMappings) ?? 0
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
                // 기본 정보
                case "borrower_number":
                    property.BorrowerNumber = value?.ToString();
                    break;
                case "borrower_name":
                    property.DebtorName = value?.ToString();
                    break;
                case "collateral_number":
                    property.CollateralNumber = value?.ToString();
                    break;
                case "property_type":
                    property.PropertyType = value?.ToString();
                    break;

                // 주소
                case "address_province":
                    if (value != null) addressParts["province"] = value.ToString()!;
                    break;
                case "address_city":
                    if (value != null) addressParts["city"] = value.ToString()!;
                    break;
                case "address_district":
                    if (value != null) addressParts["district"] = value.ToString()!;
                    break;
                case "address_detail":
                    if (value != null) addressParts["detail"] = value.ToString()!;
                    break;

                // 면적
                case "land_area":
                    property.LandArea = value as decimal?;
                    break;
                case "building_area":
                    property.BuildingArea = value as decimal?;
                    break;
                case "machinery_value":
                    property.MachineryValue = value as decimal?;
                    break;

                // 감정평가
                case "appraisal_value":
                    property.AppraisalValue = value as decimal?;
                    rightData["appraisal_value"] = value;
                    break;

                // 경매 정보
                case "court_name":
                    rightData["court_name"] = value;
                    break;
                case "property_number":
                    if (!string.IsNullOrEmpty(value?.ToString()))
                        property.PropertyNumber = value?.ToString();
                    break;
                case "final_minimum_bid":
                    property.MinimumBid = value as decimal?;
                    rightData["final_min_bid"] = value;
                    break;

                // 선순위 정보 (DD)
                case "senior_small_deposit":
                    rightData["small_deposit_dd"] = value;
                    break;
                case "senior_lease_deposit":
                    rightData["lease_deposit_dd"] = value;
                    break;
                case "senior_wage_claim":
                    rightData["wage_claim_dd"] = value;
                    break;
                case "senior_current_tax":
                    rightData["current_tax_dd"] = value;
                    break;
                case "senior_tax_claim":
                    rightData["senior_tax_dd"] = value;
                    break;
                case "senior_other":
                    rightData["etc_dd"] = value;
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
                SheetType.CollateralSetting => DataDiskSheetType.CollateralSetting,
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
                DataDiskSheetType.CollateralSetting => SheetType.CollateralSetting,
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
                "borrower_number" => "차주번호",
                "borrower_name" => "차주명",
                "borrower_type" => "차주형태",
                "opb" => "대출원금잔액",
                "mortgage_amount" => "근저당설정액",
                "address_province" => "담보소재지1 (시/도)",
                "address_city" => "담보소재지2 (시/군/구)",
                "address_district" => "담보소재지3 (동/읍/면)",
                "address_detail" => "담보소재지4 (상세)",
                "property_type" => "물건유형",
                "land_area" => "대지면적",
                "building_area" => "건물면적",
                "machinery_value" => "기계기구",
                "appraisal_value" => "감정평가액",
                "court_name" => "관할법원",
                "case_number" => "사건번호",
                "property_number" => "물건번호",
                "collateral_number" => "담보번호",
                "account_serial" => "대출일련번호",
                "loan_type" => "대출과목",
                "account_number" => "계좌번호",
                "normal_interest_rate" => "이자율",
                "initial_loan_date" => "최초대출일",
                "initial_loan_amount" => "최초대출금액",
                "loan_principal_balance" => "대출금잔액",
                "senior_small_deposit" => "선순위 소액보증금",
                "senior_lease_deposit" => "선순위 임차보증금",
                "senior_wage_claim" => "선순위 임금채권",
                "senior_current_tax" => "선순위 당해세",
                "senior_tax_claim" => "선순위 조세채권",
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

        #endregion
    }
}
