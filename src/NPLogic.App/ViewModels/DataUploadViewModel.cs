using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 데이터 업로드 ViewModel
    /// </summary>
    public partial class DataUploadViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;
        private readonly BorrowerRestructuringRepository _restructuringRepository;
        private readonly ExcelService _excelService;
        private readonly StorageService _storageService;

        // ========== 프로그램 연결 ==========
        /// <summary>
        /// 업로드할 물건들이 연결될 프로그램 ID
        /// </summary>
        [ObservableProperty]
        private string? _targetProgramId;

        /// <summary>
        /// 대상 프로그램명 (표시용)
        /// </summary>
        [ObservableProperty]
        private string? _targetProgramName;

        /// <summary>
        /// 프로그램이 설정되어 있는지 여부
        /// </summary>
        public bool HasTargetProgram => !string.IsNullOrEmpty(TargetProgramId);

        // Excel 관련
        [ObservableProperty]
        private string? _selectedExcelFile;

        [ObservableProperty]
        private bool _showMappingSection;

        [ObservableProperty]
        private ObservableCollection<string> _excelColumns = new();

        [ObservableProperty]
        private ObservableCollection<ColumnMapping> _columnMappings = new();

        [ObservableProperty]
        private ObservableCollection<Dictionary<string, object>> _previewData = new();

        [ObservableProperty]
        private bool _isExcelUploading;

        [ObservableProperty]
        private double _excelUploadProgress;

        [ObservableProperty]
        private int _processedRows;

        [ObservableProperty]
        private int _totalRows;

        [ObservableProperty]
        private string? _excelErrorMessage;

        // 가이드 관련
        [ObservableProperty]
        private bool _isGuideExpanded = false;

        [ObservableProperty]
        private string _guideExpandIcon = "▶";

        // PDF 관련
        [ObservableProperty]
        private ObservableCollection<PdfFileItem> _pdfFiles = new();

        [ObservableProperty]
        private bool _hasPdfFiles;

        [ObservableProperty]
        private bool _isPdfUploading;

        [ObservableProperty]
        private double _pdfUploadProgress;

        [ObservableProperty]
        private int _completedPdfCount;

        [ObservableProperty]
        private int _totalPdfCount;

        private List<Dictionary<string, object>>? _allExcelData;
        private string? _currentFilePath;

        // ========== IBK Multi-Sheet 관련 ==========
        [ObservableProperty]
        private ObservableCollection<SelectableSheetInfo> _availableSheets = new();

        [ObservableProperty]
        private bool _showSheetSelection;

        [ObservableProperty]
        private string? _uploadStatusMessage;

        [ObservableProperty]
        private int _totalSheetsToProcess;

        [ObservableProperty]
        private int _processedSheets;

        /// <summary>
        /// 선택된 시트가 있는지
        /// </summary>
        public bool HasSelectedSheets => AvailableSheets.Any(s => s.IsSelected);

        public DataUploadViewModel(
            PropertyRepository propertyRepository,
            ExcelService excelService,
            StorageService storageService,
            BorrowerRepository? borrowerRepository = null,
            LoanRepository? loanRepository = null,
            BorrowerRestructuringRepository? restructuringRepository = null)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _borrowerRepository = borrowerRepository!;
            _loanRepository = loanRepository!;
            _restructuringRepository = restructuringRepository!;

            InitializeColumnMappings();
        }

        /// <summary>
        /// 대상 프로그램 설정
        /// </summary>
        /// <param name="programId">프로그램 ID</param>
        /// <param name="programName">프로그램명 (표시용)</param>
        public void SetTargetProgram(string programId, string? programName = null)
        {
            TargetProgramId = programId;
            TargetProgramName = programName;
            OnPropertyChanged(nameof(HasTargetProgram));
        }

        /// <summary>
        /// 대상 프로그램 초기화
        /// </summary>
        public void ClearTargetProgram()
        {
            TargetProgramId = null;
            TargetProgramName = null;
            OnPropertyChanged(nameof(HasTargetProgram));
        }

        /// <summary>
        /// 컬럼 매핑 초기화
        /// </summary>
        private void InitializeColumnMappings()
        {
            ColumnMappings.Clear();
            
            // 필수 컬럼들
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "ProjectId (프로젝트 ID)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "PropertyNumber (물건번호)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "PropertyType (물건유형)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "AddressFull (전체주소)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "AddressRoad (도로명주소)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "AddressJibun (지번주소)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "LandArea (토지면적)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "BuildingArea (건물면적)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "AppraisalValue (감정가)" });
            ColumnMappings.Add(new ColumnMapping { TargetColumn = "MinimumBid (최저입찰가)" });
        }

        /// <summary>
        /// Excel 파일 선택
        /// </summary>
        [RelayCommand]
        private void SelectExcelFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Excel 파일 선택"
            };

            if (dialog.ShowDialog() == true)
            {
                HandleExcelFileDrop(dialog.FileName);
            }
        }

        /// <summary>
        /// Excel 파일 선택 취소
        /// </summary>
        [RelayCommand]
        private void CancelExcelFile()
        {
            SelectedExcelFile = null;
            ShowMappingSection = false;
            ExcelColumns.Clear();
            PreviewData.Clear();
            _allExcelData = null;
            ProcessedRows = 0;
            TotalRows = 0;
            ExcelUploadProgress = 0;
            ExcelErrorMessage = null;
            
            // 컬럼 매핑 초기화
            InitializeColumnMappings();
        }

        /// <summary>
        /// 가이드 패널 접기/펼치기
        /// </summary>
        [RelayCommand]
        private void ToggleGuide()
        {
            IsGuideExpanded = !IsGuideExpanded;
            GuideExpandIcon = IsGuideExpanded ? "▼" : "▶";
        }

        /// <summary>
        /// Excel 템플릿 다운로드
        /// </summary>
        [RelayCommand]
        private async Task DownloadExcelTemplate()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "물건목록_템플릿.xlsx",
                    Filter = "Excel Files|*.xlsx",
                    Title = "템플릿 저장 위치 선택"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 템플릿 생성
                    var templateData = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "ProjectId", "예: PROJ-2024-001" },
                            { "PropertyNumber", "예: 2024-001" },
                            { "PropertyType", "예: 아파트" },
                            { "AddressFull", "예: 서울시 강남구 논현동 123-45" },
                            { "AddressRoad", "예: 서울시 강남구 학동로 123" },
                            { "AddressJibun", "예: 서울시 강남구 논현동 123-45" },
                            { "LandArea", "85.5" },
                            { "BuildingArea", "102.3" },
                            { "AppraisalValue", "150000000" },
                            { "MinimumBid", "120000000" }
                        }
                    };

                    var columns = new List<string>
                    {
                        "ProjectId", "PropertyNumber", "PropertyType", "AddressFull",
                        "AddressRoad", "AddressJibun", "LandArea", "BuildingArea",
                        "AppraisalValue", "MinimumBid"
                    };

                    await _excelService.WriteExcelFileAsync(dialog.FileName, columns, templateData);

                    // 성공 메시지
                    UI.Services.ToastService.Instance.ShowSuccess("템플릿 다운로드 완료", 
                        $"파일 저장: {System.IO.Path.GetFileName(dialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ExcelErrorMessage = $"템플릿 다운로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// Excel 파일 드롭 처리
        /// </summary>
        public async void HandleExcelFileDrop(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ExcelErrorMessage = "파일을 찾을 수 없습니다.";
                    return;
                }

                var ext = Path.GetExtension(filePath).ToLower();
                if (ext != ".xlsx" && ext != ".xls")
                {
                    ExcelErrorMessage = "Excel 파일만 업로드 가능합니다 (.xlsx, .xls)";
                    return;
                }

                ExcelErrorMessage = null;
                SelectedExcelFile = Path.GetFileName(filePath);
                _currentFilePath = filePath;

                // 시트 목록 조회 (IBK Multi-Sheet 지원)
                var sheets = _excelService.GetSheetNames(filePath);
                
                // 여러 시트가 있거나 IBK 형식이면 시트 선택 UI 표시
                if (sheets.Count > 1 || sheets.Any(s => s.SheetType != SheetType.Unknown))
                {
                    AvailableSheets.Clear();
                    foreach (var sheet in sheets)
                    {
                        var selectableSheet = new SelectableSheetInfo
                        {
                            Name = sheet.Name,
                            Index = sheet.Index,
                            RowCount = sheet.RowCount,
                            Headers = sheet.Headers,
                            SheetType = sheet.SheetType,
                            IsSelected = sheet.SheetType != SheetType.Unknown // 감지된 시트는 기본 선택
                        };
                        AvailableSheets.Add(selectableSheet);
                    }
                    
                    ShowSheetSelection = true;
                    ShowMappingSection = false;
                    OnPropertyChanged(nameof(HasSelectedSheets));
                }
                else
                {
                    // 단일 시트: 기존 방식
                    ShowSheetSelection = false;
                    await LoadSingleSheetDataAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                ExcelErrorMessage = $"Excel 파일 읽기 실패: {ex.Message}";
                ShowMappingSection = false;
                ShowSheetSelection = false;
            }
        }

        /// <summary>
        /// 단일 시트 데이터 로드 (기존 방식)
        /// </summary>
        private async Task LoadSingleSheetDataAsync(string filePath)
        {
            var (columns, data) = await _excelService.ReadExcelFileAsync(filePath);

            ExcelColumns.Clear();
            ExcelColumns.Add("(선택 안 함)");
            foreach (var col in columns)
            {
                ExcelColumns.Add(col);
            }

            _allExcelData = data;

            // 미리보기 데이터 (최대 10행)
            PreviewData.Clear();
            foreach (var row in data.Take(10))
            {
                PreviewData.Add(row);
            }

            ShowMappingSection = true;
            TotalRows = data.Count;
        }

        /// <summary>
        /// 자동 매핑
        /// </summary>
        [RelayCommand]
        private void AutoMapColumns()
        {
            foreach (var mapping in ColumnMappings)
            {
                var targetLower = mapping.TargetColumn.ToLower();
                
                // 매칭 로직
                var matchedColumn = ExcelColumns.FirstOrDefault(col =>
                {
                    var colLower = col.ToLower();
                    
                    // ProjectId
                    if (targetLower.Contains("projectid") && 
                        (colLower.Contains("프로젝트") || colLower.Contains("project")))
                        return true;
                    
                    // PropertyNumber
                    if (targetLower.Contains("propertynumber") && 
                        (colLower.Contains("물건번호") || colLower.Contains("번호")))
                        return true;
                    
                    // PropertyType
                    if (targetLower.Contains("propertytype") && 
                        (colLower.Contains("유형") || colLower.Contains("종류")))
                        return true;
                    
                    // AddressFull
                    if (targetLower.Contains("addressfull") && 
                        (colLower.Contains("전체주소") || colLower.Contains("주소")))
                        return true;
                    
                    // AddressRoad
                    if (targetLower.Contains("addressroad") && 
                        colLower.Contains("도로명"))
                        return true;
                    
                    // AddressJibun
                    if (targetLower.Contains("addressjibun") && 
                        colLower.Contains("지번"))
                        return true;
                    
                    // LandArea
                    if (targetLower.Contains("landarea") && 
                        (colLower.Contains("토지") || colLower.Contains("대지")))
                        return true;
                    
                    // BuildingArea
                    if (targetLower.Contains("buildingarea") && 
                        colLower.Contains("건물"))
                        return true;
                    
                    // AppraisalValue
                    if (targetLower.Contains("appraisalvalue") && 
                        (colLower.Contains("감정") || colLower.Contains("평가")))
                        return true;
                    
                    // MinimumBid
                    if (targetLower.Contains("minimumbid") && 
                        (colLower.Contains("최저") || colLower.Contains("입찰")))
                        return true;
                    
                    return false;
                });

                if (matchedColumn != null)
                {
                    mapping.SourceColumn = matchedColumn;
                }
            }
        }

        /// <summary>
        /// Excel 데이터 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadExcelAsync()
        {
            if (_allExcelData == null || _allExcelData.Count == 0)
            {
                ExcelErrorMessage = "업로드할 데이터가 없습니다.";
                return;
            }

            try
            {
                IsExcelUploading = true;
                ExcelErrorMessage = null;
                ProcessedRows = 0;

                var properties = new List<Property>();

                // 데이터 매핑
                foreach (var row in _allExcelData)
                {
                    try
                    {
                        var property = MapRowToProperty(row);
                        properties.Add(property);
                    }
                    catch (Exception ex)
                    {
                        // 개별 행 에러는 로그만 남기고 계속 진행
                        System.Diagnostics.Debug.WriteLine($"행 매핑 실패: {ex.Message}");
                    }
                }

                // 일괄 저장
                for (int i = 0; i < properties.Count; i++)
                {
                    await _propertyRepository.CreateAsync(properties[i]);
                    
                    ProcessedRows = i + 1;
                    ExcelUploadProgress = (double)(i + 1) / properties.Count * 100;
                }

                // 완료
                ExcelErrorMessage = null;
                System.Windows.MessageBox.Show(
                    $"총 {properties.Count}개의 물건이 성공적으로 업로드되었습니다.",
                    "업로드 완료",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                // 초기화
                ResetExcelUpload();
            }
            catch (Exception ex)
            {
                ExcelErrorMessage = $"업로드 실패: {ex.Message}";
            }
            finally
            {
                IsExcelUploading = false;
            }
        }

        /// <summary>
        /// 행 데이터를 Property 객체로 매핑
        /// </summary>
        private Property MapRowToProperty(Dictionary<string, object> row)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                Status = "pending"
            };

            // 대상 프로그램이 설정되어 있으면 우선 사용
            if (!string.IsNullOrEmpty(TargetProgramId))
            {
                property.ProjectId = TargetProgramId;
            }

            foreach (var mapping in ColumnMappings)
            {
                if (string.IsNullOrEmpty(mapping.SourceColumn) || 
                    mapping.SourceColumn == "(선택 안 함)")
                    continue;

                if (!row.ContainsKey(mapping.SourceColumn))
                    continue;

                var value = row[mapping.SourceColumn];
                var targetField = mapping.TargetColumn.Split(' ')[0]; // "ProjectId (프로젝트 ID)" -> "ProjectId"

                switch (targetField)
                {
                    case "ProjectId":
                        // 대상 프로그램이 설정되어 있지 않은 경우에만 Excel 값 사용
                        if (string.IsNullOrEmpty(TargetProgramId))
                        {
                            property.ProjectId = value?.ToString();
                        }
                        break;
                    case "PropertyNumber":
                        property.PropertyNumber = value?.ToString();
                        break;
                    case "PropertyType":
                        property.PropertyType = value?.ToString();
                        break;
                    case "AddressFull":
                        property.AddressFull = value?.ToString();
                        break;
                    case "AddressRoad":
                        property.AddressRoad = value?.ToString();
                        break;
                    case "AddressJibun":
                        property.AddressJibun = value?.ToString();
                        break;
                    case "LandArea":
                        if (decimal.TryParse(value?.ToString(), out var landArea))
                            property.LandArea = landArea;
                        break;
                    case "BuildingArea":
                        if (decimal.TryParse(value?.ToString(), out var buildingArea))
                            property.BuildingArea = buildingArea;
                        break;
                    case "AppraisalValue":
                        if (decimal.TryParse(value?.ToString()?.Replace(",", ""), out var appraisal))
                            property.AppraisalValue = appraisal;
                        break;
                    case "MinimumBid":
                        if (decimal.TryParse(value?.ToString()?.Replace(",", ""), out var minimumBid))
                            property.MinimumBid = minimumBid;
                        break;
                }
            }

            return property;
        }

        // ==================== IBK Multi-Sheet Upload ====================

        /// <summary>
        /// 시트 선택 토글
        /// </summary>
        public void ToggleSheetSelection(SelectableSheetInfo sheet)
        {
            sheet.IsSelected = !sheet.IsSelected;
            OnPropertyChanged(nameof(HasSelectedSheets));
        }

        /// <summary>
        /// 선택된 시트들 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadSelectedSheetsAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !HasSelectedSheets)
            {
                ExcelErrorMessage = "업로드할 시트를 선택해주세요.";
                return;
            }

            var selectedSheets = AvailableSheets.Where(s => s.IsSelected).ToList();

            try
            {
                IsExcelUploading = true;
                ExcelErrorMessage = null;
                TotalSheetsToProcess = selectedSheets.Count;
                ProcessedSheets = 0;
                
                var results = new List<SheetUploadResult>();

                foreach (var sheet in selectedSheets)
                {
                    UploadStatusMessage = $"처리 중: {sheet.Name} ({sheet.SheetTypeDisplay})";
                    
                    try
                    {
                        var result = await ProcessSheetAsync(_currentFilePath, sheet);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new SheetUploadResult
                        {
                            SheetName = sheet.Name,
                            SheetType = sheet.SheetType,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                    }

                    ProcessedSheets++;
                    ExcelUploadProgress = (double)ProcessedSheets / TotalSheetsToProcess * 100;
                }

                // 결과 요약
                var successCount = results.Count(r => r.Success);
                var totalCreated = results.Sum(r => r.CreatedCount);
                var totalUpdated = results.Sum(r => r.UpdatedCount);
                var totalFailed = results.Sum(r => r.FailedCount);

                var message = $"업로드 완료!\n\n" +
                    $"처리 시트: {successCount}/{results.Count}\n" +
                    $"생성: {totalCreated}건, 수정: {totalUpdated}건, 실패: {totalFailed}건";

                if (results.Any(r => !r.Success))
                {
                    message += "\n\n실패한 시트:\n";
                    foreach (var failed in results.Where(r => !r.Success))
                    {
                        message += $"- {failed.SheetName}: {failed.ErrorMessage}\n";
                    }
                }

                System.Windows.MessageBox.Show(message, "업로드 완료",
                    System.Windows.MessageBoxButton.OK,
                    results.All(r => r.Success) ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);

                ResetExcelUpload();
            }
            catch (Exception ex)
            {
                ExcelErrorMessage = $"업로드 실패: {ex.Message}";
            }
            finally
            {
                IsExcelUploading = false;
                UploadStatusMessage = null;
            }
        }

        /// <summary>
        /// 시트별 데이터 처리
        /// </summary>
        private async Task<SheetUploadResult> ProcessSheetAsync(string filePath, SelectableSheetInfo sheet)
        {
            var result = new SheetUploadResult
            {
                SheetName = sheet.Name,
                SheetType = sheet.SheetType
            };

            // 헤더 행 감지 (IBK 형식의 경우 헤더가 중간에 있을 수 있음)
            var headerRow = _excelService.DetectHeaderRow(filePath, sheet.Name);
            
            // 시트 데이터 읽기
            var (columns, data) = await _excelService.ReadExcelSheetAsync(filePath, sheet.Name, headerRow);
            
            if (data.Count == 0)
            {
                result.Success = true;
                return result;
            }

            // 매핑 규칙 가져오기
            var mappingRules = SheetMappingConfig.GetMappingRules(sheet.SheetType);

            switch (sheet.SheetType)
            {
                case SheetType.BorrowerGeneral:
                    var borrowerResult = await ProcessBorrowerGeneralAsync(data, columns, mappingRules);
                    result.CreatedCount = borrowerResult.Created;
                    result.UpdatedCount = borrowerResult.Updated;
                    result.FailedCount = borrowerResult.Failed;
                    break;

                case SheetType.BorrowerRestructuring:
                    var restructuringResult = await ProcessBorrowerRestructuringAsync(data, columns, mappingRules);
                    result.CreatedCount = restructuringResult.Created;
                    result.UpdatedCount = restructuringResult.Updated;
                    result.FailedCount = restructuringResult.Failed;
                    break;

                case SheetType.Loan:
                    var loanResult = await ProcessLoanAsync(data, columns, mappingRules);
                    result.CreatedCount = loanResult.Created;
                    result.UpdatedCount = loanResult.Updated;
                    result.FailedCount = loanResult.Failed;
                    break;

                case SheetType.Property:
                    var propertyResult = await ProcessPropertyAsync(data, columns, mappingRules);
                    result.CreatedCount = propertyResult.Created;
                    result.UpdatedCount = propertyResult.Updated;
                    result.FailedCount = propertyResult.Failed;
                    break;

                case SheetType.RegistryDetail:
                    // Sheet C-2: 등기부등본정보 - OCR로 처리하므로 여기서는 스킵
                    System.Diagnostics.Debug.WriteLine($"[ProcessSheetAsync] Sheet C-2 (등기부등본정보) 스킵 - OCR 탭에서 처리");
                    result.Success = true;
                    return result;

                case SheetType.CollateralSetting:
                    // Sheet C-3: 담보설정정보 - 향후 구현 예정
                    System.Diagnostics.Debug.WriteLine($"[ProcessSheetAsync] Sheet C-3 (담보설정정보) 스킵 - 향후 구현 예정");
                    result.Success = true;
                    return result;

                case SheetType.Guarantee:
                    // Sheet D: 보증정보 - 향후 구현 예정
                    System.Diagnostics.Debug.WriteLine($"[ProcessSheetAsync] Sheet D (보증정보) 스킵 - 향후 구현 예정");
                    result.Success = true;
                    return result;

                default:
                    result.ErrorMessage = "알 수 없는 시트 유형";
                    result.Success = false;
                    return result;
            }

            result.Success = true;
            return result;
        }

        /// <summary>
        /// 차주일반정보 처리
        /// </summary>
        private async Task<(int Created, int Updated, int Failed)> ProcessBorrowerGeneralAsync(
            List<Dictionary<string, object>> data,
            List<string> columns,
            List<ColumnMappingRule> rules)
        {
            if (_borrowerRepository == null)
                return (0, 0, data.Count);

            var borrowers = new List<Borrower>();

            foreach (var row in data)
            {
                try
                {
                    var borrower = new Borrower
                    {
                        Id = Guid.NewGuid(),
                        ProgramId = TargetProgramId
                    };

                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule == null) continue;

                        var value = row.ContainsKey(col) ? rule.ConvertValue(row[col]) : null;

                        switch (rule.DbColumnName)
                        {
                            case "borrower_number":
                                borrower.BorrowerNumber = value?.ToString() ?? "";
                                break;
                            case "borrower_name":
                                borrower.BorrowerName = value?.ToString() ?? "";
                                break;
                            case "borrower_type":
                                borrower.BorrowerType = value?.ToString() ?? "개인";
                                break;
                            case "opb":
                                if (value is decimal opb) borrower.Opb = opb;
                                break;
                            case "mortgage_amount":
                                if (value is decimal mortgage) borrower.MortgageAmount = mortgage;
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(borrower.BorrowerNumber))
                    {
                        borrowers.Add(borrower);
                    }
                }
                catch { /* 개별 행 에러 무시 */ }
            }

            return await _borrowerRepository.BulkUpsertAsync(borrowers);
        }

        /// <summary>
        /// 회생차주정보 처리
        /// </summary>
        private async Task<(int Created, int Updated, int Failed)> ProcessBorrowerRestructuringAsync(
            List<Dictionary<string, object>> data,
            List<string> columns,
            List<ColumnMappingRule> rules)
        {
            if (_borrowerRepository == null || _restructuringRepository == null)
                return (0, 0, data.Count);

            int created = 0, updated = 0, failed = 0;

            foreach (var row in data)
            {
                try
                {
                    // 차주번호로 차주 조회
                    string? borrowerNumber = null;
                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule?.DbColumnName == "borrower_number")
                        {
                            borrowerNumber = row.ContainsKey(col) ? row[col]?.ToString() : null;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(borrowerNumber))
                    {
                        failed++;
                        continue;
                    }

                    var borrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                    if (borrower == null)
                    {
                        failed++;
                        continue;
                    }

                    // 차주의 회생 플래그 업데이트
                    borrower.IsRestructuring = true;
                    await _borrowerRepository.UpdateAsync(borrower);

                    // 회생 상세 정보 저장
                    var restructuring = new BorrowerRestructuring
                    {
                        BorrowerId = borrower.Id
                    };

                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule == null) continue;

                        var value = row.ContainsKey(col) ? rule.ConvertValue(row[col]) : null;

                        switch (rule.DbColumnName)
                        {
                            case "approval_status":
                                restructuring.ApprovalStatus = value?.ToString();
                                break;
                            case "progress_stage":
                                restructuring.ProgressStage = value?.ToString();
                                break;
                            case "court_name":
                                restructuring.CourtName = value?.ToString();
                                break;
                            case "case_number":
                                restructuring.CaseNumber = value?.ToString();
                                break;
                            case "filing_date":
                                if (value is DateTime fd) restructuring.FilingDate = fd;
                                break;
                            case "preservation_date":
                                if (value is DateTime pd) restructuring.PreservationDate = pd;
                                break;
                            case "commencement_date":
                                if (value is DateTime cd) restructuring.CommencementDate = cd;
                                break;
                            case "claim_filing_date":
                                if (value is DateTime cfd) restructuring.ClaimFilingDate = cfd;
                                break;
                            case "approval_dismissal_date":
                                if (value is DateTime add) restructuring.ApprovalDismissalDate = add;
                                break;
                            case "excluded_claim":
                                restructuring.ExcludedClaim = value?.ToString();
                                break;
                        }
                    }

                    var existing = await _restructuringRepository.GetByBorrowerIdAsync(borrower.Id);
                    if (existing != null)
                    {
                        restructuring.Id = existing.Id;
                        await _restructuringRepository.UpdateAsync(restructuring);
                        updated++;
                    }
                    else
                    {
                        await _restructuringRepository.CreateAsync(restructuring);
                        created++;
                    }
                }
                catch
                {
                    failed++;
                }
            }

            return (created, updated, failed);
        }

        /// <summary>
        /// 채권정보 처리
        /// </summary>
        private async Task<(int Created, int Updated, int Failed)> ProcessLoanAsync(
            List<Dictionary<string, object>> data,
            List<string> columns,
            List<ColumnMappingRule> rules)
        {
            if (_borrowerRepository == null || _loanRepository == null)
                return (0, 0, data.Count);

            var loans = new List<Loan>();

            foreach (var row in data)
            {
                try
                {
                    // 차주번호로 차주 조회
                    string? borrowerNumber = null;
                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule?.DbColumnName == "borrower_number")
                        {
                            borrowerNumber = row.ContainsKey(col) ? row[col]?.ToString() : null;
                            break;
                        }
                    }

                    Guid? borrowerId = null;
                    if (!string.IsNullOrEmpty(borrowerNumber))
                    {
                        var borrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                        borrowerId = borrower?.Id;
                    }

                    var loan = new Loan
                    {
                        Id = Guid.NewGuid(),
                        BorrowerId = borrowerId
                    };

                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule == null) continue;

                        var value = row.ContainsKey(col) ? rule.ConvertValue(row[col]) : null;

                        switch (rule.DbColumnName)
                        {
                            case "account_serial":
                                loan.AccountSerial = value?.ToString();
                                break;
                            case "loan_type":
                                loan.LoanType = value?.ToString();
                                break;
                            case "account_number":
                                loan.AccountNumber = value?.ToString();
                                break;
                            case "normal_interest_rate":
                                if (value is decimal rate) loan.NormalInterestRate = rate;
                                break;
                            case "initial_loan_date":
                                if (value is DateTime ild) loan.InitialLoanDate = ild;
                                break;
                            case "last_interest_date":
                                if (value is DateTime lid) loan.LastInterestDate = lid;
                                break;
                            case "initial_loan_amount":
                                if (value is decimal ila) loan.InitialLoanAmount = ila;
                                break;
                            case "loan_principal_balance":
                                if (value is decimal lpb) loan.LoanPrincipalBalance = lpb;
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(loan.AccountSerial))
                    {
                        loans.Add(loan);
                    }
                }
                catch { /* 개별 행 에러 무시 */ }
            }

            return await _loanRepository.BulkUpsertAsync(loans);
        }

        /// <summary>
        /// 담보물건정보 처리
        /// </summary>
        private async Task<(int Created, int Updated, int Failed)> ProcessPropertyAsync(
            List<Dictionary<string, object>> data,
            List<string> columns,
            List<ColumnMappingRule> rules)
        {
            var properties = new List<Property>();

            // 주소 조합을 위한 임시 저장소
            var addressParts = new Dictionary<int, Dictionary<string, string>>();

            // 디버그: 사용 가능한 컬럼 출력
            System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] 데이터 행 수: {data.Count}");
            System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] 컬럼 수: {columns.Count}");
            System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] 컬럼 목록:");
            foreach (var col in columns.Take(20))
            {
                var rule = SheetMappingConfig.FindMappingRule(rules, col);
                System.Diagnostics.Debug.WriteLine($"  - '{col}' → {(rule != null ? rule.DbColumnName : "(매핑 없음)")}");
            }

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                addressParts[i] = new Dictionary<string, string>();

                try
                {
                    // 차주번호로 차주 조회
                    string? borrowerNumber = null;
                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule?.DbColumnName == "borrower_number")
                        {
                            borrowerNumber = row.ContainsKey(col) ? row[col]?.ToString() : null;
                            break;
                        }
                    }

                    Guid? borrowerId = null;
                    if (!string.IsNullOrEmpty(borrowerNumber) && _borrowerRepository != null)
                    {
                        var borrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                        borrowerId = borrower?.Id;
                    }

                    var property = new Property
                    {
                        Id = Guid.NewGuid(),
                        BorrowerId = borrowerId,
                        Status = "pending"
                    };

                    if (!string.IsNullOrEmpty(TargetProgramId) && Guid.TryParse(TargetProgramId, out var programGuid))
                    {
                        property.ProgramId = programGuid;
                    }

                    foreach (var col in columns)
                    {
                        var rule = SheetMappingConfig.FindMappingRule(rules, col);
                        if (rule == null) continue;

                        var value = row.ContainsKey(col) ? rule.ConvertValue(row[col]) : null;
                        
                        // 첫 번째 행에서 매핑된 값 디버그 출력
                        if (i == 0 && value != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] Row 0: '{col}' → {rule.DbColumnName} = '{value}'");
                        }

                        switch (rule.DbColumnName)
                        {
                            case "borrower_number":
                                // 차주번호도 물건에 저장 (대시보드 표시용)
                                property.BorrowerNumber = value?.ToString();
                                break;
                            case "borrower_name":
                                // 차주명을 DebtorName에 저장
                                property.DebtorName = value?.ToString();
                                break;
                            case "property_number":
                                property.PropertyNumber = value?.ToString();
                                break;
                            case "collateral_number":
                                property.CollateralNumber = value?.ToString();
                                break;
                            case "property_type":
                                property.PropertyType = value?.ToString();
                                break;
                            case "land_area":
                                if (value is decimal la) property.LandArea = la;
                                break;
                            case "building_area":
                                if (value is decimal ba) property.BuildingArea = ba;
                                break;
                            case "address_province":
                                addressParts[i]["province"] = value?.ToString() ?? "";
                                break;
                            case "address_city":
                                addressParts[i]["city"] = value?.ToString() ?? "";
                                break;
                            case "address_district":
                                addressParts[i]["district"] = value?.ToString() ?? "";
                                break;
                            case "address_detail":
                                addressParts[i]["detail"] = value?.ToString() ?? "";
                                break;
                        }
                    }

                    // 주소 조합
                    var parts = addressParts[i];
                    var addressComponents = new List<string>();
                    if (parts.TryGetValue("province", out var prov) && !string.IsNullOrEmpty(prov))
                        addressComponents.Add(prov);
                    if (parts.TryGetValue("city", out var city) && !string.IsNullOrEmpty(city))
                        addressComponents.Add(city);
                    if (parts.TryGetValue("district", out var dist) && !string.IsNullOrEmpty(dist))
                        addressComponents.Add(dist);
                    if (parts.TryGetValue("detail", out var detail) && !string.IsNullOrEmpty(detail))
                        addressComponents.Add(detail);

                    if (addressComponents.Count > 0)
                    {
                        property.AddressFull = string.Join(" ", addressComponents);
                    }

                    // 디버그: 첫 5개 행의 속성 값 출력
                    if (i < 5)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] Row {i}: PropertyNumber='{property.PropertyNumber}', DebtorName='{property.DebtorName}', CollateralNumber='{property.CollateralNumber}', PropertyType='{property.PropertyType}'");
                    }

                    if (!string.IsNullOrEmpty(property.PropertyNumber))
                    {
                        properties.Add(property);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] Row {i}: PropertyNumber가 비어있어 스킵");
                    }
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] Row {i}: 에러 - {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ProcessPropertyAsync] 저장할 Property 수: {properties.Count}");
            return await _propertyRepository.BulkUpsertAsync(properties);
        }

        /// <summary>
        /// Excel 업로드 초기화
        /// </summary>
        private void ResetExcelUpload()
        {
            SelectedExcelFile = null;
            ShowMappingSection = false;
            ShowSheetSelection = false;
            ExcelColumns.Clear();
            PreviewData.Clear();
            AvailableSheets.Clear();
            _allExcelData = null;
            _currentFilePath = null;
            ProcessedRows = 0;
            TotalRows = 0;
            ExcelUploadProgress = 0;
            ProcessedSheets = 0;
            TotalSheetsToProcess = 0;
            UploadStatusMessage = null;
            InitializeColumnMappings();
        }

        /// <summary>
        /// PDF 파일 선택
        /// </summary>
        [RelayCommand]
        private void SelectPdfFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "PDF Files|*.pdf",
                Title = "PDF 파일 선택",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                HandlePdfFilesDrop(dialog.FileNames);
            }
        }

        /// <summary>
        /// PDF 파일 전체 취소
        /// </summary>
        [RelayCommand]
        private void CancelAllPdfFiles()
        {
            PdfFiles.Clear();
            HasPdfFiles = false;
            TotalPdfCount = 0;
            CompletedPdfCount = 0;
            PdfUploadProgress = 0;
            IsPdfUploading = false;
        }

        /// <summary>
        /// PDF 파일 드롭 처리
        /// </summary>
        public void HandlePdfFilesDrop(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                var ext = Path.GetExtension(filePath).ToLower();
                if (ext != ".pdf")
                    continue;

                var fileInfo = new FileInfo(filePath);
                var pdfItem = new PdfFileItem
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    Status = "대기중",
                    Progress = 0
                };

                PdfFiles.Add(pdfItem);
            }

            HasPdfFiles = PdfFiles.Count > 0;
            TotalPdfCount = PdfFiles.Count;
        }

        /// <summary>
        /// PDF 파일 제거
        /// </summary>
        [RelayCommand]
        private void RemovePdfFile(PdfFileItem item)
        {
            PdfFiles.Remove(item);
            HasPdfFiles = PdfFiles.Count > 0;
            TotalPdfCount = PdfFiles.Count;
        }

        /// <summary>
        /// PDF 파일 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadPdfFilesAsync()
        {
            if (PdfFiles.Count == 0)
                return;

            try
            {
                IsPdfUploading = true;
                CompletedPdfCount = 0;

                foreach (var pdfFile in PdfFiles)
                {
                    if (pdfFile.Status == "완료")
                        continue;

                    try
                    {
                        pdfFile.Status = "업로드중";
                        
                        // Supabase Storage에 업로드
                        await _storageService.UploadFileAsync(
                            pdfFile.FilePath,
                            "documents", // 버킷 이름
                            pdfFile.FileName,
                            progress =>
                            {
                                pdfFile.Progress = progress;
                            });

                        pdfFile.Status = "완료";
                        pdfFile.Progress = 100;
                        CompletedPdfCount++;
                        
                        PdfUploadProgress = (double)CompletedPdfCount / TotalPdfCount * 100;
                    }
                    catch (Exception ex)
                    {
                        pdfFile.Status = $"실패: {ex.Message}";
                    }
                }

                // 완료
                System.Windows.MessageBox.Show(
                    $"총 {CompletedPdfCount}개의 PDF가 성공적으로 업로드되었습니다.",
                    "업로드 완료",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"업로드 실패: {ex.Message}",
                    "오류",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsPdfUploading = false;
            }
        }
    }

    /// <summary>
    /// 컬럼 매핑
    /// </summary>
    public partial class ColumnMapping : ObservableObject
    {
        [ObservableProperty]
        private string _targetColumn = "";

        [ObservableProperty]
        private string? _sourceColumn;
    }

    /// <summary>
    /// PDF 파일 항목
    /// </summary>
    public partial class PdfFileItem : ObservableObject
    {
        [ObservableProperty]
        private string _filePath = "";

        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        private string _status = "대기중";

        [ObservableProperty]
        private double _progress;

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024:N1} KB";
                else
                    return $"{FileSize / (1024 * 1024):N1} MB";
            }
        }
    }

    /// <summary>
    /// 선택 가능한 시트 정보
    /// </summary>
    public partial class SelectableSheetInfo : ObservableObject
    {
        public string Name { get; set; } = "";
        public int Index { get; set; }
        public int RowCount { get; set; }
        public List<string> Headers { get; set; } = new();
        public SheetType SheetType { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// 시트 유형 표시명
        /// </summary>
        public string SheetTypeDisplay => SheetType switch
        {
            SheetType.BorrowerGeneral => "차주일반정보",
            SheetType.BorrowerRestructuring => "회생차주정보",
            SheetType.Loan => "채권정보",
            SheetType.Property => "담보물건정보",
            SheetType.RegistryDetail => "등기부등본정보",
            SheetType.CollateralSetting => "담보설정정보",
            SheetType.Guarantee => "보증정보",
            _ => "알 수 없음"
        };

        /// <summary>
        /// 데이터 행 수 (헤더 제외)
        /// </summary>
        public int DataRowCount => Math.Max(0, RowCount - 1);

        /// <summary>
        /// 표시용 문자열
        /// </summary>
        public string DisplayText => $"{Name} ({SheetTypeDisplay}) - {DataRowCount}행";
    }

    /// <summary>
    /// 시트 업로드 결과
    /// </summary>
    public class SheetUploadResult
    {
        public string SheetName { get; set; } = "";
        public SheetType SheetType { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
    }
}

