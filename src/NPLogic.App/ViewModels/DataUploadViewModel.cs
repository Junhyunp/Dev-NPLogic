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

        public DataUploadViewModel(
            PropertyRepository propertyRepository,
            ExcelService excelService,
            StorageService storageService)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

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

                // Excel 파일 읽기
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
            catch (Exception ex)
            {
                ExcelErrorMessage = $"Excel 파일 읽기 실패: {ex.Message}";
                ShowMappingSection = false;
            }
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

        /// <summary>
        /// Excel 업로드 초기화
        /// </summary>
        private void ResetExcelUpload()
        {
            SelectedExcelFile = null;
            ShowMappingSection = false;
            ExcelColumns.Clear();
            PreviewData.Clear();
            _allExcelData = null;
            ProcessedRows = 0;
            TotalRows = 0;
            ExcelUploadProgress = 0;
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
}

