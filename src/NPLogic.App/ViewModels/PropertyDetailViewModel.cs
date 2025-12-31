using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;
using NPLogic.ViewModels;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 마감 체크리스트 모델
    /// </summary>
    public partial class ClosingChecklistModel : ObservableObject
    {
        [ObservableProperty]
        private bool _registryConfirmed;

        [ObservableProperty]
        private bool _rightsAnalysisConfirmed;

        [ObservableProperty]
        private bool _evaluationConfirmed;

        [ObservableProperty]
        private bool _qaComplete;
    }

    /// <summary>
    /// 권리분석 알림 모델
    /// </summary>
    public class RightsAnalysisAlert
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// 첨부파일 모델
    /// </summary>
    public class AttachmentItem : ObservableObject
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string StoragePath { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024:N0} KB";
                return $"{FileSize / (1024 * 1024):N1} MB";
            }
        }
    }

    /// <summary>
    /// 물건 상세 ViewModel
    /// </summary>
    public partial class PropertyDetailViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly StorageService? _storageService;
        private readonly RegistryRepository? _registryRepository;
        private readonly RightAnalysisRepository? _rightAnalysisRepository;
        private readonly EvaluationRepository? _evaluationRepository;
        private readonly RegistryOcrService? _registryOcrService;
        private readonly PropertyQaRepository? _propertyQaRepository;

        [ObservableProperty]
        private Property _property = new();

        [ObservableProperty]
        private Property _originalProperty = new();

        /// <summary>
        /// 등기부 탭 ViewModel
        /// </summary>
        [ObservableProperty]
        private RegistryTabViewModel? _registryViewModel;

        /// <summary>
        /// 권리분석 탭 ViewModel
        /// </summary>
        [ObservableProperty]
        private RightsAnalysisTabViewModel? _rightsAnalysisViewModel;

        /// <summary>
        /// 평가 탭 ViewModel
        /// </summary>
        [ObservableProperty]
        private EvaluationTabViewModel? _evaluationViewModel;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        // 이전 탭 인덱스 (저장 확인용)
        private int _previousTabIndex = 0;
        private bool _isChangingTab = false;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // 아파트 여부 (KB시세 표시 조건)
        [ObservableProperty]
        private bool _isApartment;

        // KB시세 정보
        [ObservableProperty]
        private decimal _kbPrice;

        [ObservableProperty]
        private decimal _kbJeonsePrice;

        [ObservableProperty]
        private decimal _kbPricePerPyeong;

        [ObservableProperty]
        private DateTime? _kbPriceDate;

        // 감정평가 상세 정보
        [ObservableProperty]
        private string? _appraisalType;

        [ObservableProperty]
        private string? _appraisalOrganization;

        [ObservableProperty]
        private DateTime? _appraisalDate;

        [ObservableProperty]
        private decimal _landAppraisalValue;

        [ObservableProperty]
        private decimal _buildingAppraisalValue;

        [ObservableProperty]
        private decimal _machineAppraisalValue;

        // 첨부파일 목록
        [ObservableProperty]
        private ObservableCollection<AttachmentItem> _attachments = new();

        [ObservableProperty]
        private bool _hasNoAttachments = true;

        // QA 목록
        [ObservableProperty]
        private ObservableCollection<PropertyQa> _qaList = new();

        [ObservableProperty]
        private bool _hasNoQA = true;

        [ObservableProperty]
        private string _newQuestion = "";
        
        // 데이터 업로드 정보 (Phase 5.4)
        [ObservableProperty]
        private DateTime? _lastDataUploadDate;

        #region HomeTab 관련 속성

        /// <summary>
        /// 토지 면적 (평)
        /// </summary>
        public decimal LandAreaPyeong => Property?.LandArea != null ? Property.LandArea.Value / 3.3058m : 0;

        /// <summary>
        /// 건물 면적 (평)
        /// </summary>
        public decimal BuildingAreaPyeong => Property?.BuildingArea != null ? Property.BuildingArea.Value / 3.3058m : 0;

        #endregion

        #region NonCoreTab 관련 속성

        // 채권 정보
        [ObservableProperty]
        private string? _loanSubject;

        [ObservableProperty]
        private string? _loanType;

        [ObservableProperty]
        private string? _accountNumber;

        [ObservableProperty]
        private DateTime? _firstLoanDate;

        [ObservableProperty]
        private decimal _originalPrincipal;

        [ObservableProperty]
        private decimal _remainingPrincipal;

        [ObservableProperty]
        private decimal _accruedInterest;

        [ObservableProperty]
        private decimal _normalInterestRate;

        [ObservableProperty]
        private decimal _overdueInterestRate;

        // Loan Cap
        [ObservableProperty]
        private decimal _loanCap1;

        [ObservableProperty]
        private decimal _loanCap2;

        // 보증서 정보
        [ObservableProperty]
        private string? _guaranteeOrganization;

        [ObservableProperty]
        private decimal _guaranteeBalance;

        [ObservableProperty]
        private decimal _guaranteeRatio;

        [ObservableProperty]
        private bool _isSubrogated;

        [ObservableProperty]
        private DateTime? _subrogationExpectedDate;

        [ObservableProperty]
        private decimal _subrogationPrincipal;

        // 경매 정보
        [ObservableProperty]
        private bool _isAuctionStarted;

        [ObservableProperty]
        private string? _court;

        [ObservableProperty]
        private string? _auctionCaseNumber;

        [ObservableProperty]
        private DateTime? _auctionStartDate;

        [ObservableProperty]
        private DateTime? _dividendDeadline;

        [ObservableProperty]
        private decimal _claimAmount;

        [ObservableProperty]
        private decimal _winningBidAmount;

        // 회생 정보
        [ObservableProperty]
        private string? _restructuringCourt;

        [ObservableProperty]
        private string? _restructuringCaseNumber;

        [ObservableProperty]
        private DateTime? _restructuringStartDate;

        // 현금흐름
        [ObservableProperty]
        private decimal _xnpv1;

        [ObservableProperty]
        private decimal _xnpv2;

        // 차주 개요
        [ObservableProperty]
        private string? _borrowerNumber;

        [ObservableProperty]
        private string? _borrowerName;

        [ObservableProperty]
        private string? _businessNumber;

        [ObservableProperty]
        private decimal _opb;

        // 담보 물건
        [ObservableProperty]
        private bool _isFactoryMortgage;

        // 선순위 평가
        [ObservableProperty]
        private decimal _seniorRightsTotal;

        [ObservableProperty]
        private decimal _tenantDeposit;

        [ObservableProperty]
        private decimal _localTax;

        [ObservableProperty]
        private decimal _otherSeniorRights;

        // 경공매 추가 (인터링/상계회수 - 피드백 30번)
        [ObservableProperty]
        private decimal _interring;

        [ObservableProperty]
        private decimal _offsetRecovery;

        // NPB
        [ObservableProperty]
        private decimal _npbAmount;

        [ObservableProperty]
        private decimal _npbRatio;

        [ObservableProperty]
        private string? _npbNote;

        #endregion

        #region ClosingTab 관련 속성

        [ObservableProperty]
        private bool _isClosingComplete;

        [ObservableProperty]
        private ClosingChecklistModel _closingChecklist = new();

        [ObservableProperty]
        private ObservableCollection<RightsAnalysisAlert> _rightsAnalysisAlerts = new();

        [ObservableProperty]
        private bool _hasNoAlerts = true;

        [ObservableProperty]
        private DateTime? _closingDate;

        [ObservableProperty]
        private string? _closingUser;

        [ObservableProperty]
        private string? _closingNote;

        #endregion

        private Guid? _propertyId;
        private Action? _goBackAction;

        public PropertyDetailViewModel(
            PropertyRepository propertyRepository, 
            StorageService? storageService = null, 
            RegistryRepository? registryRepository = null, 
            RightAnalysisRepository? rightAnalysisRepository = null, 
            EvaluationRepository? evaluationRepository = null, 
            RegistryOcrService? registryOcrService = null,
            PropertyQaRepository? propertyQaRepository = null)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _storageService = storageService;
            _registryRepository = registryRepository;
            _rightAnalysisRepository = rightAnalysisRepository;
            _evaluationRepository = evaluationRepository;
            _registryOcrService = registryOcrService;
            _propertyQaRepository = propertyQaRepository;

            // 등기부 탭 ViewModel 초기화
            if (_registryRepository != null)
            {
                RegistryViewModel = new RegistryTabViewModel(_registryRepository, _registryOcrService);
            }

            // 권리분석 탭 ViewModel 초기화
            if (_rightAnalysisRepository != null && _registryRepository != null)
            {
                RightsAnalysisViewModel = new RightsAnalysisTabViewModel(_rightAnalysisRepository, _registryRepository);
            }

            // 평가 탭 ViewModel 초기화
            if (_evaluationRepository != null)
            {
                EvaluationViewModel = new EvaluationTabViewModel(_evaluationRepository);
            }
        }

        /// <summary>
        /// 물건 ID로 초기화
        /// </summary>
        public void SetPropertyId(Guid propertyId, Action? goBackAction = null)
        {
            _propertyId = propertyId;
            _goBackAction = goBackAction;

            // 등기부 탭 ViewModel에 물건 ID 설정
            RegistryViewModel?.SetPropertyId(propertyId);

            // 권리분석 탭 ViewModel에 물건 ID 설정
            RightsAnalysisViewModel?.SetPropertyId(propertyId);

            // 평가 탭 ViewModel에 물건 ID 설정
            EvaluationViewModel?.SetPropertyId(propertyId);
        }

        /// <summary>
        /// 활성 탭 설정 (탭 이름으로)
        /// </summary>
        public void SetActiveTab(string tabName)
        {
            SelectedTabIndex = tabName.ToLower() switch
            {
                "basic" or "home" => 0,
                "noncore" => 1,
                "registry" => 2,
                "rights" or "rightsanalysis" => 3,
                "basicdata" => 4,
                "closing" => 5,
                "evaluation" => 6,
                _ => 0
            };
        }

        /// <summary>
        /// Property 객체로 직접 로드
        /// </summary>
        public void LoadProperty(Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _propertyId = property.Id;
            Property = property;
            
            // 아파트 여부 확인
            IsApartment = property.PropertyType?.Contains("아파트") == true 
                       || property.PropertyType?.Contains("오피스텔") == true;

            // 원본 복사 (변경 감지용)
            CopyPropertyToOriginal(property);

            // 등기부 탭 ViewModel에 물건 정보 설정
            if (RegistryViewModel != null)
            {
                RegistryViewModel.SetPropertyId(property.Id);
                RegistryViewModel.SetPropertyInfo(property);
            }

            // 권리분석 탭 ViewModel에 물건 정보 설정
            if (RightsAnalysisViewModel != null)
            {
                RightsAnalysisViewModel.SetPropertyId(property.Id);
                RightsAnalysisViewModel.SetProperty(property);
            }

            // 평가 탭 ViewModel에 물건 정보 설정
            if (EvaluationViewModel != null)
            {
                EvaluationViewModel.SetPropertyId(property.Id);
                EvaluationViewModel.SetProperty(property);
            }

            HasUnsavedChanges = false;
        }

        private void CopyPropertyToOriginal(Property property)
        {
            OriginalProperty = new Property
            {
                Id = property.Id,
                ProjectId = property.ProjectId,
                PropertyNumber = property.PropertyNumber,
                PropertyType = property.PropertyType,
                AddressFull = property.AddressFull,
                AddressRoad = property.AddressRoad,
                AddressJibun = property.AddressJibun,
                AddressDetail = property.AddressDetail,
                LandArea = property.LandArea,
                BuildingArea = property.BuildingArea,
                Floors = property.Floors,
                CompletionDate = property.CompletionDate,
                AppraisalValue = property.AppraisalValue,
                MinimumBid = property.MinimumBid,
                SalePrice = property.SalePrice,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                Status = property.Status,
                AssignedTo = property.AssignedTo,
                CreatedBy = property.CreatedBy,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt
            };
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_propertyId == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var property = await _propertyRepository.GetByIdAsync(_propertyId.Value);
                if (property != null)
                {
                    Property = property;
                    IsApartment = property.PropertyType?.Contains("아파트") == true 
                               || property.PropertyType?.Contains("오피스텔") == true;
                    
                    CopyPropertyToOriginal(property);

                    // QA, 첨부파일 로드
                    await LoadAttachmentsAsync();
                    await LoadQAListAsync();
                }
                else
                {
                    ErrorMessage = "물건을 찾을 수 없습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 첨부파일 관련

        private async Task LoadAttachmentsAsync()
        {
            if (_storageService == null || _propertyId == null) return;

            try
            {
                Attachments.Clear();
                var files = await _storageService.ListFilesAsync("attachments", $"properties/{_propertyId}");
                foreach (var file in files)
                {
                    Attachments.Add(new AttachmentItem
                    {
                        Id = Guid.NewGuid(),
                        FileName = file.Name,
                        FileSize = 0, // Supabase FileObject does not have a direct Size property
                        StoragePath = $"properties/{_propertyId}/{file.Name}",
                        CreatedAt = file.CreatedAt ?? DateTime.Now
                    });
                }
                HasNoAttachments = Attachments.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"첨부파일 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 업로드 명령
        /// </summary>
        [RelayCommand]
        private async Task UploadFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "첨부파일 선택",
                Filter = "모든 파일|*.*|PDF 파일|*.pdf|이미지 파일|*.jpg;*.jpeg;*.png;*.gif|문서 파일|*.doc;*.docx;*.xls;*.xlsx",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    if (_storageService == null)
                    {
                        ErrorMessage = "Storage 서비스가 초기화되지 않았습니다. appsettings.json을 확인해주세요.";
                        return;
                    }

                    var fileName = System.IO.Path.GetFileName(dialog.FileName);
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
                    var storagePath = $"properties/{_propertyId}/{fileName}";

                    var url = await _storageService.UploadFileAsync("attachments", storagePath, fileBytes);

                    if (!string.IsNullOrEmpty(url))
                    {
                        await LoadAttachmentsAsync();
                        SuccessMessage = "파일이 업로드되었습니다.";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"파일 업로드 실패: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 파일 보기 명령
        /// </summary>
        [RelayCommand]
        private async Task ViewFileAsync(AttachmentItem? attachment)
        {
            if (attachment == null || _storageService == null) return;

            try
            {
                var url = await _storageService.GetPublicUrlAsync("attachments", attachment.StoragePath);
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"파일 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 파일 삭제 명령
        /// </summary>
        [RelayCommand]
        private async Task DeleteFileAsync(AttachmentItem? attachment)
        {
            if (attachment == null || _storageService == null) return;

            try
            {
                IsLoading = true;
                var success = await _storageService.DeleteFileAsync("attachments", attachment.StoragePath);
                if (success)
                {
                    Attachments.Remove(attachment);
                    HasNoAttachments = Attachments.Count == 0;
                    SuccessMessage = "파일이 삭제되었습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"파일 삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region QA 관련

        private async Task LoadQAListAsync()
        {
            if (_propertyQaRepository == null || _propertyId == null) return;

            try
            {
                var list = await _propertyQaRepository.GetByPropertyIdAsync(_propertyId.Value);
                QaList.Clear();
                foreach (var qa in list)
                {
                    QaList.Add(qa);
                }
                HasNoQA = QaList.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QA 목록 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// QA 추가 명령
        /// </summary>
        [RelayCommand]
        private async Task AddQAAsync()
        {
            if (_propertyQaRepository == null || _propertyId == null || string.IsNullOrWhiteSpace(NewQuestion)) return;

            try
            {
                var newQA = new PropertyQa
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId.Value,
                    Question = NewQuestion,
                    CreatedAt = DateTime.UtcNow
                };

                await _propertyQaRepository.CreateAsync(newQA);
                NewQuestion = ""; // 입력 필드 초기화
                await LoadQAListAsync();
                
                // QA 미회신 카운트 업데이트
                if (Property != null)
                {
                    Property.QaUnansweredCount = QaList.Count(q => string.IsNullOrEmpty(q.Answer));
                    await _propertyRepository.UpdateAsync(Property);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"QA 추가 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// QA 답변 저장 명령
        /// </summary>
        [RelayCommand]
        private async Task SaveQAAnswerAsync(PropertyQa qa)
        {
            if (_propertyQaRepository == null || qa == null) return;

            try
            {
                qa.AnsweredAt = DateTime.UtcNow;
                await _propertyQaRepository.UpdateAsync(qa);
                await LoadQAListAsync();

                // QA 미회신 카운트 업데이트
                if (Property != null)
                {
                    Property.QaUnansweredCount = QaList.Count(q => string.IsNullOrEmpty(q.Answer));
                    await _propertyRepository.UpdateAsync(Property);
                }
                
                SuccessMessage = "답변이 저장되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"답변 저장 실패: {ex.Message}";
            }
        }

        #endregion

        #region KB시세 관련

        /// <summary>
        /// KB시세 조회 명령
        /// </summary>
        [RelayCommand]
        private async Task FetchKBPriceAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // TODO: 실제 KB시세 API 연동 또는 국토교통부 실거래가 API 연동
                // 현재는 더미 데이터
                await Task.Delay(500); // 시뮬레이션

                // 더미 데이터 (실제로는 API 응답으로 대체)
                KbPrice = 450000000;
                KbJeonsePrice = 350000000;
                KbPricePerPyeong = 15000000;
                KbPriceDate = DateTime.Now;

                SuccessMessage = "KB시세가 조회되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"KB시세 조회 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// KB시세 사이트 열기
        /// </summary>
        [RelayCommand]
        private void OpenKBSite()
        {
            try
            {
                var url = "https://kbland.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 로드뷰 열기
        /// </summary>
        [RelayCommand]
        private void OpenRoadView()
        {
            if (Property?.Latitude != null && Property?.Longitude != null)
            {
                var url = $"https://map.kakao.com/?map_type=TYPE_MAP&sX={Property.Longitude}&sY={Property.Latitude}&sLevel=3";
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"로드뷰 열기 실패: {ex.Message}";
                }
            }
            else if (!string.IsNullOrWhiteSpace(Property?.AddressFull))
            {
                var encodedAddress = Uri.EscapeDataString(Property.AddressFull);
                var url = $"https://map.kakao.com/?q={encodedAddress}";
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"로드뷰 열기 실패: {ex.Message}";
                }
            }
        }

        #endregion

        #region 데이터 업로드 관련 (Phase 5.4)

        /// <summary>
        /// 기초 데이터 파일 업로드 명령
        /// </summary>
        [RelayCommand]
        private async Task UploadDataFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "기초 데이터 파일 선택",
                Filter = "Excel 파일|*.xlsx;*.xls|모든 파일|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    // TODO: 엑셀 파일 파싱 및 데이터 업로드 로직 구현
                    // 현재는 업로드 시뮬레이션
                    await Task.Delay(1000);

                    LastDataUploadDate = DateTime.Now;
                    SuccessMessage = $"데이터가 성공적으로 업로드되었습니다.\n파일: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"데이터 업로드 실패: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 데이터 템플릿 다운로드 명령
        /// </summary>
        [RelayCommand]
        private void DownloadTemplate()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "템플릿 저장",
                    Filter = "Excel 파일|*.xlsx",
                    FileName = "기초데이터_템플릿.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // 템플릿 파일 생성
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("기초데이터");

                    // 헤더 추가
                    var headers = new[] { "프로젝트ID", "물건번호", "물건유형", "전체주소", "도로명주소", 
                                         "지번주소", "상세주소", "토지면적", "건물면적", "층수",
                                         "감정평가액", "최저입찰가", "매각가", "상태" };
                    
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1E3A5F");
                        cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    }

                    // 샘플 데이터 행 추가
                    worksheet.Cell(2, 1).Value = "PROJECT001";
                    worksheet.Cell(2, 2).Value = "001";
                    worksheet.Cell(2, 3).Value = "아파트";
                    worksheet.Cell(2, 4).Value = "서울시 강남구 테헤란로 123";
                    worksheet.Cell(2, 14).Value = "pending";

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);

                    SuccessMessage = $"템플릿이 저장되었습니다.\n{saveDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"템플릿 다운로드 실패: {ex.Message}";
            }
        }

        #endregion

        #region 마감 관련 명령

        /// <summary>
        /// 마감 처리/취소 명령
        /// </summary>
        [RelayCommand]
        private async Task CompleteClosingAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                if (IsClosingComplete)
                {
                    // 마감 취소
                    IsClosingComplete = false;
                    ClosingDate = null;
                    ClosingUser = null;
                    SuccessMessage = "마감이 취소되었습니다.";
                }
                else
                {
                    // 마감 처리
                    if (!ClosingChecklist.RegistryConfirmed ||
                        !ClosingChecklist.RightsAnalysisConfirmed ||
                        !ClosingChecklist.EvaluationConfirmed ||
                        !ClosingChecklist.QaComplete)
                    {
                        ErrorMessage = "모든 체크리스트 항목을 완료해주세요.";
                        return;
                    }

                    IsClosingComplete = true;
                    ClosingDate = DateTime.Now;
                    ClosingUser = Environment.UserName;
                    SuccessMessage = "마감 처리가 완료되었습니다.";
                }

                // Property 상태 업데이트
                if (Property != null)
                {
                    Property.Status = IsClosingComplete ? "completed" : "processing";
                    await _propertyRepository.UpdateAsync(Property);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"마감 처리 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        /// <summary>
        /// 저장 명령
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                await _propertyRepository.UpdateAsync(Property);
                
                // 성공 메시지
                SuccessMessage = "저장되었습니다.";
                HasUnsavedChanges = false;

                // 원본 업데이트
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 새로고침 명령
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (HasUnsavedChanges)
            {
                // TODO: 확인 다이얼로그 표시
                // 지금은 그냥 새로고침
            }

            await InitializeAsync();
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// 뒤로가기 명령
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            if (HasUnsavedChanges)
            {
                // TODO: 확인 다이얼로그 표시
                // 지금은 그냥 뒤로가기
            }

            _goBackAction?.Invoke();
        }

        /// <summary>
        /// Property 변경 시
        /// </summary>
        partial void OnPropertyChanged(Property value)
        {
            // 변경 감지 (간단 버전)
            HasUnsavedChanges = true;
            
            // 아파트 여부 업데이트
            IsApartment = value?.PropertyType?.Contains("아파트") == true 
                       || value?.PropertyType?.Contains("오피스텔") == true;
            
            // HomeTab 면적 속성 변경 알림
            OnPropertyChanged(nameof(LandAreaPyeong));
            OnPropertyChanged(nameof(BuildingAreaPyeong));
        }

        /// <summary>
        /// 탭 변경 시 저장 확인 (피드백 반영)
        /// </summary>
        partial void OnSelectedTabIndexChanging(int value)
        {
            // 탭 변경 중이 아니고, 저장되지 않은 변경사항이 있으면 확인
            if (!_isChangingTab && HasUnsavedChanges && _previousTabIndex != value)
            {
                var result = System.Windows.MessageBox.Show(
                    "저장되지 않은 변경사항이 있습니다.\n저장하시겠습니까?",
                    "저장 확인",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // 저장 후 탭 이동
                    _ = SaveAsync();
                }
                else if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    // 취소 - 이전 탭으로 복원
                    _isChangingTab = true;
                    SelectedTabIndex = _previousTabIndex;
                    _isChangingTab = false;
                    return;
                }
                // No - 저장하지 않고 이동
            }
        }

        /// <summary>
        /// 탭 변경 완료 시
        /// </summary>
        partial void OnSelectedTabIndexChanged(int value)
        {
            if (!_isChangingTab)
            {
                _previousTabIndex = value;
            }
        }
    }
}
