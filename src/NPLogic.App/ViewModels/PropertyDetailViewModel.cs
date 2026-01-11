using System;
using System.Collections.Generic;
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
    /// 미리보기 필드 데이터 모델
    /// </summary>
    public class PreviewFieldData
    {
        public string FieldName { get; set; } = "";
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// 등기부 요약 정보 모델 (D-002, D-009)
    /// </summary>
    public class RegistrySummaryModel
    {
        /// <summary>
        /// 표제부 요약
        /// </summary>
        public string TitleSummary { get; set; } = "";

        /// <summary>
        /// 갑구 요약 (소유권)
        /// </summary>
        public string Section1Summary { get; set; } = "";

        /// <summary>
        /// 을구 요약 (근저당)
        /// </summary>
        public string Section2Summary { get; set; } = "";

        /// <summary>
        /// 등기부 주소
        /// </summary>
        public string RegistryAddress { get; set; } = "";
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
        private readonly SupabaseService? _supabaseService;
        private readonly ProgramRepository? _programRepository;
        
        // 프로그램 이름 캐시 (성능 최적화)
        private static readonly Dictionary<Guid, string> _programNameCache = new();

        [ObservableProperty]
        private Property _property = new();

        [ObservableProperty]
        private Property _originalProperty = new();

        /// <summary>
        /// 프로그램명 (프로젝트명)
        /// </summary>
        [ObservableProperty]
        private string _programName = "-";

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

        // Undo 관련 (피드백 반영: 자동저장 + 되돌리기)
        /// <summary>
        /// Undo 가능 여부
        /// </summary>
        public bool CanUndo => Property?.Id != null && Property.Id != Guid.Empty && UndoService.Instance.CanUndo(Property.Id);

        /// <summary>
        /// Undo 가능 횟수
        /// </summary>
        public int UndoCount => Property?.Id != null && Property.Id != Guid.Empty ? UndoService.Instance.GetUndoCount(Property.Id) : 0;

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

        #region D-011: 감정평가 세부 정보

        // 토지 세부 정보
        [ObservableProperty]
        private string? _landCategory;

        [ObservableProperty]
        private decimal _landUnitPrice;

        [ObservableProperty]
        private decimal _landPublicPrice;

        // 건물 세부 정보
        [ObservableProperty]
        private string? _buildingStructure;

        [ObservableProperty]
        private decimal _buildingUnitPrice;

        [ObservableProperty]
        private string? _buildingAge;

        // 기계기구 세부 정보
        [ObservableProperty]
        private string? _machineType;

        [ObservableProperty]
        private int _machineCount;

        [ObservableProperty]
        private decimal _machineDepreciationRate;

        #endregion

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

        // 데이터디스크 컬럼 매핑 관련
        [ObservableProperty]
        private bool _isColumnMappingVisible;

        [ObservableProperty]
        private string? _selectedExcelFileName;

        [ObservableProperty]
        private string? _selectedExcelFilePath;

        [ObservableProperty]
        private ObservableCollection<string> _excelColumns = new();

        [ObservableProperty]
        private ObservableCollection<ColumnMapping> _dataDiskMappings = new();

        [ObservableProperty]
        private ObservableCollection<PreviewFieldData> _previewMappedData = new();

        [ObservableProperty]
        private bool _hasMatchedRow;

        [ObservableProperty]
        private bool _hasNoMatchedRow;

        private List<Dictionary<string, object>>? _allExcelData;
        private Dictionary<string, object>? _matchedRowData;

        #region 등기부 관련 속성 (D-002, D-009, D-010)

        /// <summary>
        /// 등기부 요약 정보
        /// </summary>
        [ObservableProperty]
        private RegistrySummaryModel _registrySummary = new();

        /// <summary>
        /// 등기부 데이터 존재 여부
        /// </summary>
        [ObservableProperty]
        private bool _hasRegistryData;

        /// <summary>
        /// DD 주소와 등기부 주소 일치 여부
        /// </summary>
        [ObservableProperty]
        private bool _isAddressMatched = true;

        /// <summary>
        /// 등기부 요약 이미지 경로
        /// </summary>
        [ObservableProperty]
        private string? _registrySummaryImagePath;

        /// <summary>
        /// 등기부 요약 이미지 존재 여부
        /// </summary>
        [ObservableProperty]
        private bool _hasRegistrySummaryImage;

        /// <summary>
        /// 등기부 요약 이미지 없음 여부
        /// </summary>
        [ObservableProperty]
        private bool _hasNoRegistrySummaryImage = true;

        /// <summary>
        /// 토지이용계획 상태
        /// </summary>
        [ObservableProperty]
        private string _landUsePlanStatus = "클릭하여 조회";

        /// <summary>
        /// 건축물대장 상태
        /// </summary>
        [ObservableProperty]
        private string _buildingRegisterStatus = "클릭하여 조회";

        #endregion

        #region HomeTab 관련 속성

        /// <summary>
        /// 토지 면적 (평)
        /// </summary>
        public decimal LandAreaPyeong => Property?.LandArea != null ? Property.LandArea.Value / 3.3058m : 0;

        /// <summary>
        /// 건물 면적 (평)
        /// </summary>
        public decimal BuildingAreaPyeong => Property?.BuildingArea != null ? Property.BuildingArea.Value / 3.3058m : 0;

        /// <summary>
        /// 표시용 주소 (AddressFull → AddressJibun → AddressRoad 순서로 fallback)
        /// </summary>
        public string DisplayAddress
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Property?.AddressFull))
                    return Property.AddressFull;
                if (!string.IsNullOrWhiteSpace(Property?.AddressJibun))
                    return Property.AddressJibun;
                if (!string.IsNullOrWhiteSpace(Property?.AddressRoad))
                    return Property.AddressRoad;
                return "주소 정보 없음";
            }
        }

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

        #region 담보총괄 통계 (프로그램 레벨)

        /// <summary>
        /// 프로그램 내 담보물건 수
        /// </summary>
        [ObservableProperty]
        private int _collateralPropertyCount;

        /// <summary>
        /// 감정평가액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _totalAppraisalValue;

        /// <summary>
        /// 평가액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _totalEstimatedValue;

        /// <summary>
        /// Loan Cap 합계
        /// </summary>
        [ObservableProperty]
        private decimal _loanCap;

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
        
        // ========== N-001: 키보드 탐색 ==========
        private List<Property>? _propertyList;
        private int _currentIndex = -1;
        private Action<Guid>? _navigateToPropertyAction;

        /// <summary>
        /// 현재 물건 인덱스 (1-based for display)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanNavigatePrevious))]
        [NotifyPropertyChangedFor(nameof(CanNavigateNext))]
        [NotifyPropertyChangedFor(nameof(NavigationInfo))]
        private int _currentPropertyIndex;

        /// <summary>
        /// 전체 물건 수
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NavigationInfo))]
        private int _totalPropertyCount;

        /// <summary>
        /// 이전 물건으로 이동 가능 여부
        /// </summary>
        public bool CanNavigatePrevious => _currentIndex > 0 && _propertyList != null && _propertyList.Count > 0;

        /// <summary>
        /// 다음 물건으로 이동 가능 여부
        /// </summary>
        public bool CanNavigateNext => _propertyList != null && _currentIndex < _propertyList.Count - 1;

        /// <summary>
        /// 네비게이션 정보 표시 (예: "2 / 15")
        /// </summary>
        public string NavigationInfo => TotalPropertyCount > 0 
            ? $"{CurrentPropertyIndex} / {TotalPropertyCount}" 
            : "-";

        public PropertyDetailViewModel(
            PropertyRepository propertyRepository, 
            StorageService? storageService = null, 
            RegistryRepository? registryRepository = null, 
            RightAnalysisRepository? rightAnalysisRepository = null, 
            EvaluationRepository? evaluationRepository = null, 
            RegistryOcrService? registryOcrService = null,
            PropertyQaRepository? propertyQaRepository = null,
            SupabaseService? supabaseService = null,
            ProgramRepository? programRepository = null)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _storageService = storageService;
            _registryRepository = registryRepository;
            _rightAnalysisRepository = rightAnalysisRepository;
            _evaluationRepository = evaluationRepository;
            _registryOcrService = registryOcrService;
            _propertyQaRepository = propertyQaRepository;
            _supabaseService = supabaseService;
            _programRepository = programRepository;
            
            // 프로그램 이름 캐시가 비어있으면 미리 로드 (첫 ViewModel 생성 시)
            if (_programRepository != null && _programNameCache.Count == 0)
            {
                _ = PreloadProgramNamesAsync();
            }

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

            // 평가 탭 ViewModel 초기화 - 항상 생성 (null이면 바인딩 실패함)
            if (_evaluationRepository != null)
            {
                EvaluationViewModel = new EvaluationTabViewModel(_evaluationRepository);
                Debug.WriteLine($"[PropertyDetailViewModel] EvaluationViewModel created successfully");
                
                // Supabase 연결 정보 설정 (유사물건 추천 API용)
                if (_supabaseService != null)
                {
                    EvaluationViewModel.SupabaseUrl = _supabaseService.Url;
                    EvaluationViewModel.SupabaseKey = _supabaseService.Key;
                    Debug.WriteLine($"[PropertyDetailViewModel] Supabase configured: {_supabaseService.Url}");
                }
                else
                {
                    Debug.WriteLine("[PropertyDetailViewModel] WARNING: SupabaseService is null, recommendation will not work");
                }
            }
            else
            {
                Debug.WriteLine("[PropertyDetailViewModel] ERROR: EvaluationRepository is null! EvaluationViewModel will be null!");
                Debug.WriteLine("[PropertyDetailViewModel] WARNING: EvaluationRepository is null, EvaluationViewModel not created");
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
        /// 물건 ID와 네비게이션 정보로 초기화 (N-001)
        /// </summary>
        public void SetPropertyId(Guid propertyId, List<Property> propertyList, Action<Guid>? navigateAction, Action? goBackAction = null)
        {
            SetPropertyId(propertyId, goBackAction);
            
            _propertyList = propertyList;
            _navigateToPropertyAction = navigateAction;
            
            // 현재 인덱스 찾기
            _currentIndex = propertyList.FindIndex(p => p.Id == propertyId);
            CurrentPropertyIndex = _currentIndex + 1; // 1-based
            TotalPropertyCount = propertyList.Count;
            
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
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

            // 담보총괄 통계 로드 (프로그램 레벨)
            _ = LoadCollateralStatisticsAsync(property.ProgramId);

            // 프로그램 이름 설정 (캐시 우선, 동기적)
            SetProgramNameFromCache(property.ProgramId, property.ProjectId);
            // 캐시에 없으면 비동기로 로드
            _ = LoadProgramNameAsync(property.ProgramId, property.ProjectId);

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

                    // 등기부 요약 정보 로드 (D-002, D-009, D-010)
                    await LoadRegistrySummaryAsync(property.Id);

                    // 담보총괄 통계 로드 (프로그램 레벨)
                    await LoadCollateralStatisticsAsync(property.ProgramId);

                    // 프로그램 이름 설정 (캐시 우선, 동기적)
                    SetProgramNameFromCache(property.ProgramId, property.ProjectId);
                    // 캐시에 없으면 비동기로 로드
                    await LoadProgramNameAsync(property.ProgramId, property.ProjectId);
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

        /// <summary>
        /// 캐시에서 프로그램 이름 설정 (동기적)
        /// </summary>
        private void SetProgramNameFromCache(Guid? programId, string? projectId)
        {
            // ProgramId가 있고 캐시에 있으면 바로 설정
            if (programId.HasValue && _programNameCache.TryGetValue(programId.Value, out var cachedName))
            {
                ProgramName = cachedName;
                return;
            }
            
            // ProjectId를 GUID로 파싱해서 캐시 확인
            if (!string.IsNullOrWhiteSpace(projectId) && Guid.TryParse(projectId, out var projectGuid))
            {
                if (_programNameCache.TryGetValue(projectGuid, out var cachedName2))
                {
                    ProgramName = cachedName2;
                    return;
                }
            }
            
            // ProjectId가 GUID가 아니면 프로그램명으로 간주
            if (!string.IsNullOrWhiteSpace(projectId) && !Guid.TryParse(projectId, out _))
            {
                ProgramName = projectId;
                return;
            }
            
            // 기본값
            ProgramName = "-";
        }

        /// <summary>
        /// 프로그램 이름 로드 (캐시 우선 사용)
        /// </summary>
        private async Task LoadProgramNameAsync(Guid? programId, string? projectId)
        {
            // 1. 캐시에서 먼저 확인 (동기적으로 빠르게 표시)
            Guid? targetProgramId = programId;
            
            // ProgramId가 없으면 ProjectId를 GUID로 파싱 시도
            if (!targetProgramId.HasValue && !string.IsNullOrWhiteSpace(projectId) && Guid.TryParse(projectId, out var projectGuid))
            {
                targetProgramId = projectGuid;
            }
            
            // 캐시에서 확인
            if (targetProgramId.HasValue && _programNameCache.TryGetValue(targetProgramId.Value, out var cachedName))
            {
                ProgramName = cachedName;
                return;
            }
            
            // ProjectId가 GUID가 아닌 경우 프로그램명으로 간주
            if (!string.IsNullOrWhiteSpace(projectId) && !Guid.TryParse(projectId, out _))
            {
                ProgramName = projectId;
                return;
            }

            ProgramName = "-";

            if (_programRepository == null)
            {
                return;
            }

            try
            {
                // DB에서 조회
                if (targetProgramId.HasValue)
                {
                    var program = await _programRepository.GetByIdAsync(targetProgramId.Value);
                    if (program != null && !string.IsNullOrWhiteSpace(program.ProgramName))
                    {
                        // 캐시에 저장
                        _programNameCache[targetProgramId.Value] = program.ProgramName;
                        ProgramName = program.ProgramName;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"프로그램 이름 로드 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 프로그램 이름 캐시 초기화 (앱 시작 시 호출 권장)
        /// </summary>
        public async Task PreloadProgramNamesAsync()
        {
            if (_programRepository == null) return;
            
            try
            {
                var programs = await _programRepository.GetAllAsync();
                foreach (var program in programs)
                {
                    if (!string.IsNullOrWhiteSpace(program.ProgramName))
                    {
                        _programNameCache[program.Id] = program.ProgramName;
                    }
                }
                Debug.WriteLine($"프로그램 이름 캐시 로드 완료: {_programNameCache.Count}개");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"프로그램 이름 캐시 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 담보총괄 통계 로드 (프로그램 레벨)
        /// </summary>
        private async Task LoadCollateralStatisticsAsync(Guid? programId)
        {
            if (!programId.HasValue)
            {
                // 프로그램 ID가 없으면 통계 초기화
                CollateralPropertyCount = 0;
                TotalAppraisalValue = 0;
                TotalEstimatedValue = 0;
                LoanCap = 0;
                return;
            }

            try
            {
                // 프로그램에 속한 모든 물건 조회
                var properties = await _propertyRepository.GetByProgramIdAsync(programId.Value);

                // 통계 계산
                CollateralPropertyCount = properties.Count;
                TotalAppraisalValue = properties.Sum(p => p.AppraisalValue ?? 0);
                TotalEstimatedValue = properties.Sum(p => p.SalePrice ?? p.AppraisalValue ?? 0);

                // Loan Cap: right_analysis에서 조회 (있는 경우)
                if (_rightAnalysisRepository != null)
                {
                    decimal totalLoanCap = 0;
                    foreach (var prop in properties)
                    {
                        var analysis = await _rightAnalysisRepository.GetByPropertyIdAsync(prop.Id);
                        if (analysis != null)
                        {
                            totalLoanCap += analysis.LoanCap ?? 0;
                        }
                    }
                    LoanCap = totalLoanCap;
                }
                else
                {
                    LoanCap = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"담보총괄 통계 로드 실패: {ex.Message}");
                // 실패 시 기본값 유지
            }
        }

        #region 등기부 요약 관련 (D-002, D-009, D-010)

        /// <summary>
        /// 등기부 요약 정보 로드
        /// </summary>
        private async Task LoadRegistrySummaryAsync(Guid propertyId)
        {
            if (_registryRepository == null) 
            {
                HasRegistryData = false;
                HasRegistrySummaryImage = false;
                HasNoRegistrySummaryImage = true;
                return;
            }

            try
            {
                // 등기부 문서 조회
                var documents = await _registryRepository.GetDocumentsByPropertyIdAsync(propertyId);
                var latestDoc = documents.FirstOrDefault();
                
                if (latestDoc != null)
                {
                    HasRegistryData = true;

                    // 갑구/을구 권리 정보 조회
                    var gapguRights = await _registryRepository.GetGapguRightsAsync(propertyId);
                    var eulguRights = await _registryRepository.GetEulguRightsAsync(propertyId);
                    var owners = await _registryRepository.GetOwnersByPropertyIdAsync(propertyId);

                    // 등기부 요약 정보 구성
                    var titleSummary = !string.IsNullOrWhiteSpace(latestDoc.RegistryNumber)
                        ? $"등기번호: {latestDoc.RegistryNumber}\n등기유형: {latestDoc.RegistryType ?? "-"}"
                        : "표제부 정보 없음";

                    var section1Summary = owners.Any()
                        ? $"소유자: {string.Join(", ", owners.Select(o => o.OwnerName))}"
                        : (gapguRights.Any()
                            ? $"갑구 권리 {gapguRights.Count}건"
                            : "갑구 정보 없음");

                    var section2Summary = eulguRights.Any()
                        ? $"근저당/전세권 {eulguRights.Count}건\n총액: {eulguRights.Sum(r => r.ClaimAmount ?? 0):N0}원"
                        : "을구 정보 없음";

                    RegistrySummary = new RegistrySummaryModel
                    {
                        TitleSummary = titleSummary,
                        Section1Summary = section1Summary,
                        Section2Summary = section2Summary,
                        RegistryAddress = "" // 추후 ExtractedData에서 파싱 가능
                    };

                    // 주소 일치 여부 확인 (D-010) - 추후 ExtractedData에서 주소 추출 시 구현
                    IsAddressMatched = true;

                    // 등기부 요약 이미지 확인 (D-009) - 추후 OCR 시 캡처 이미지 저장 경로 사용
                    RegistrySummaryImagePath = null;
                    HasRegistrySummaryImage = false;
                    HasNoRegistrySummaryImage = true;
                }
                else
                {
                    HasRegistryData = false;
                    HasRegistrySummaryImage = false;
                    HasNoRegistrySummaryImage = true;
                    RegistrySummary = new RegistrySummaryModel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"등기부 요약 로드 실패: {ex.Message}");
                HasRegistryData = false;
                HasRegistrySummaryImage = false;
                HasNoRegistrySummaryImage = true;
            }
        }

        /// <summary>
        /// 주소 정규화 (비교용)
        /// </summary>
        private static string NormalizeAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return "";
            return address
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(",", "")
                .Replace(".", "")
                .Trim();
        }

        /// <summary>
        /// 등기부 탭으로 이동 명령
        /// </summary>
        [RelayCommand]
        private void GoToRegistryTab()
        {
            // PropertyDetailView의 탭 인덱스 변경 (등기부 탭은 인덱스 기반으로 찾아야 함)
            // 현재 PropertyDetailView에서 탭 순서: 차주개요(0), 론(1), 담보물건(2), 선순위(3), 평가(4), 경공매일정(5)
            // 등기부 탭은 상단 메뉴에 있으므로 DashboardView로 이동 필요
            // 여기서는 SuccessMessage로 안내
            SuccessMessage = "등기부 탭은 상단 '등기부(OCR)' 메뉴에서 확인할 수 있습니다.";
        }

        #endregion

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
        /// 기초 데이터 파일 업로드 명령 - 파일 선택 후 컬럼 매핑 모달 표시
        /// </summary>
        [RelayCommand]
        private async Task UploadDataFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "데이터디스크 파일 선택",
                Filter = "Excel 파일|*.xlsx;*.xls|모든 파일|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    SelectedExcelFilePath = dialog.FileName;
                    SelectedExcelFileName = System.IO.Path.GetFileName(dialog.FileName);

                    // 엑셀 파일 파싱
                    await ParseExcelFileAsync(dialog.FileName);

                    // 컬럼 매핑 초기화
                    InitializeDataDiskMappings();

                    // 자동 매핑 시도
                    AutoMapColumns();

                    // 첫 번째 행을 사용 (물건번호 매칭 없이)
                    UseFirstRow();

                    // 컬럼 매핑 모달 표시
                    IsColumnMappingVisible = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"파일 읽기 실패: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 엑셀 파일 파싱
        /// </summary>
        private async Task ParseExcelFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();

                // 첫 번째 행에서 컬럼명 추출
                var headerRow = worksheet.Row(1);
                var columns = new List<string> { "" }; // 빈 옵션 추가
                var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

                for (int col = 1; col <= lastColumn; col++)
                {
                    var cellValue = headerRow.Cell(col).GetString();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        columns.Add(cellValue);
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ExcelColumns.Clear();
                    foreach (var col in columns)
                    {
                        ExcelColumns.Add(col);
                    }
                });

                // 데이터 행 파싱
                var data = new List<Dictionary<string, object>>();
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                for (int row = 2; row <= lastRow; row++)
                {
                    var rowData = new Dictionary<string, object>();
                    for (int col = 1; col <= lastColumn; col++)
                    {
                        var colName = headerRow.Cell(col).GetString();
                        if (!string.IsNullOrWhiteSpace(colName))
                        {
                            var cell = worksheet.Cell(row, col);
                            rowData[colName] = cell.Value.ToString() ?? "";
                        }
                    }
                    if (rowData.Any())
                    {
                        data.Add(rowData);
                    }
                }

                _allExcelData = data;
            });
        }

        /// <summary>
        /// 데이터디스크 컬럼 매핑 초기화
        /// </summary>
        private void InitializeDataDiskMappings()
        {
            DataDiskMappings.Clear();
            
            // 매핑 대상 필드들
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "물건유형" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "전체주소" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "도로명주소" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "지번주소" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "상세주소" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "토지면적" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "건물면적" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "층수" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "감정가" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "최저입찰가" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "매각가" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "차주명" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "담보번호" });
            DataDiskMappings.Add(new ColumnMapping { TargetColumn = "OPB" });
        }

        /// <summary>
        /// 자동 컬럼 매핑
        /// </summary>
        [RelayCommand]
        private void AutoMapColumns()
        {
            var mappingRules = new Dictionary<string, string[]>
            {
                { "물건유형", new[] { "PropertyType", "물건유형", "property_type", "유형", "담보물형태" } },
                { "전체주소", new[] { "AddressFull", "전체주소", "address_full", "주소", "소재지" } },
                { "도로명주소", new[] { "AddressRoad", "도로명주소", "address_road", "도로명" } },
                { "지번주소", new[] { "AddressJibun", "지번주소", "address_jibun", "지번" } },
                { "상세주소", new[] { "AddressDetail", "상세주소", "address_detail" } },
                { "토지면적", new[] { "LandArea", "토지면적", "land_area", "대지면적" } },
                { "건물면적", new[] { "BuildingArea", "건물면적", "building_area", "연면적" } },
                { "층수", new[] { "Floors", "층수", "floors", "층" } },
                { "감정가", new[] { "AppraisalValue", "감정가", "appraisal_value", "감정평가액" } },
                { "최저입찰가", new[] { "MinimumBid", "최저입찰가", "minimum_bid", "최저가" } },
                { "매각가", new[] { "SalePrice", "매각가", "sale_price" } },
                { "차주명", new[] { "DebtorName", "차주명", "debtor_name", "채무자" } },
                { "담보번호", new[] { "CollateralNumber", "담보번호", "collateral_number" } },
                { "OPB", new[] { "OPB", "opb", "대출잔액" } }
            };

            foreach (var mapping in DataDiskMappings)
            {
                if (mappingRules.TryGetValue(mapping.TargetColumn, out var possibleNames))
                {
                    var matchedColumn = ExcelColumns.FirstOrDefault(col =>
                        possibleNames.Any(name => 
                            col.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                            col.Contains(name, StringComparison.OrdinalIgnoreCase)));
                    
                    if (!string.IsNullOrEmpty(matchedColumn))
                    {
                        mapping.SourceColumn = matchedColumn;
                    }
                }
            }
        }

        /// <summary>
        /// 첫 번째 행을 사용 (물건번호 매칭 없이 단일 물건 업데이트)
        /// </summary>
        private void UseFirstRow()
        {
            _matchedRowData = null;
            HasMatchedRow = false;
            HasNoMatchedRow = true;
            PreviewMappedData.Clear();

            if (_allExcelData == null || !_allExcelData.Any())
                return;

            // 첫 번째 행 사용
            _matchedRowData = _allExcelData.First();
            HasMatchedRow = true;
            HasNoMatchedRow = false;

            // 미리보기 데이터 생성
            UpdatePreviewData();
        }

        /// <summary>
        /// 미리보기 데이터 업데이트
        /// </summary>
        private void UpdatePreviewData()
        {
            PreviewMappedData.Clear();

            if (_matchedRowData == null) return;

            foreach (var mapping in DataDiskMappings)
            {
                if (!string.IsNullOrEmpty(mapping.SourceColumn) && 
                    _matchedRowData.TryGetValue(mapping.SourceColumn, out var value))
                {
                    PreviewMappedData.Add(new PreviewFieldData
                    {
                        FieldName = mapping.TargetColumn,
                        Value = value?.ToString() ?? ""
                    });
                }
            }
        }

        /// <summary>
        /// 컬럼 매핑 모달 닫기
        /// </summary>
        [RelayCommand]
        private void CloseColumnMapping()
        {
            IsColumnMappingVisible = false;
            _allExcelData = null;
            _matchedRowData = null;
        }

        /// <summary>
        /// 데이터디스크 매핑 적용
        /// </summary>
        [RelayCommand]
        private async Task ApplyDataDiskMappingAsync()
        {
            if (_matchedRowData == null || Property == null) return;
            
            // Property ID가 유효한지 확인
            if (Property.Id == Guid.Empty)
            {
                ErrorMessage = "물건이 선택되지 않았습니다. 물건 목록에서 물건을 선택한 후 다시 시도하세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 매핑된 데이터로 Property 업데이트
                foreach (var mapping in DataDiskMappings)
                {
                    if (string.IsNullOrEmpty(mapping.SourceColumn)) continue;
                    if (!_matchedRowData.TryGetValue(mapping.SourceColumn, out var value)) continue;
                    
                    var strValue = value?.ToString();
                    if (string.IsNullOrEmpty(strValue)) continue;

                    switch (mapping.TargetColumn)
                    {
                        case "물건유형":
                            Property.PropertyType = strValue;
                            break;
                        case "전체주소":
                            Property.AddressFull = strValue;
                            break;
                        case "도로명주소":
                            Property.AddressRoad = strValue;
                            break;
                        case "지번주소":
                            Property.AddressJibun = strValue;
                            break;
                        case "상세주소":
                            Property.AddressDetail = strValue;
                            break;
                        case "토지면적":
                            if (decimal.TryParse(strValue, out var landArea))
                                Property.LandArea = landArea;
                            break;
                        case "건물면적":
                            if (decimal.TryParse(strValue, out var buildingArea))
                                Property.BuildingArea = buildingArea;
                            break;
                        case "층수":
                            Property.Floors = strValue;
                            break;
                        case "감정가":
                            if (decimal.TryParse(strValue.Replace(",", ""), out var appraisalValue))
                                Property.AppraisalValue = appraisalValue;
                            break;
                        case "최저입찰가":
                            if (decimal.TryParse(strValue.Replace(",", ""), out var minimumBid))
                                Property.MinimumBid = minimumBid;
                            break;
                        case "매각가":
                            if (decimal.TryParse(strValue.Replace(",", ""), out var salePrice))
                                Property.SalePrice = salePrice;
                            break;
                        case "차주명":
                            Property.DebtorName = strValue;
                            break;
                        case "담보번호":
                            Property.CollateralNumber = strValue;
                            break;
                        case "OPB":
                            if (decimal.TryParse(strValue.Replace(",", ""), out var opb))
                                Property.Opb = opb;
                            break;
                    }
                }

                // DB에 저장
                await _propertyRepository.UpdateAsync(Property);
                
                LastDataUploadDate = DateTime.Now;
                IsColumnMappingVisible = false;
                SuccessMessage = "데이터디스크 데이터가 성공적으로 적용되었습니다.";

                // Property 변경 알림
                OnPropertyChanged(nameof(Property));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 적용 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 데이터 템플릿 다운로드 명령 - 현재 물건 정보가 모두 채워진 단일 행 템플릿
        /// </summary>
        [RelayCommand]
        private void DownloadTemplate()
        {
            try
            {
                var propertyNumber = Property?.PropertyNumber ?? "물건";
                var saveDialog = new SaveFileDialog
                {
                    Title = "템플릿 저장",
                    Filter = "Excel 파일|*.xlsx",
                    FileName = $"물건정보_{propertyNumber}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // 템플릿 파일 생성
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("물건정보");

                    // 헤더 추가 (영문 + 한글 설명)
                    var headers = new[] 
                    { 
                        "PropertyType", "AddressFull", "AddressRoad", "AddressJibun", "AddressDetail",
                        "LandArea", "BuildingArea", "Floors", "AppraisalValue", "MinimumBid", 
                        "SalePrice", "DebtorName", "CollateralNumber", "OPB"
                    };
                    
                    var headerLabels = new[] 
                    { 
                        "물건유형", "전체주소", "도로명주소", "지번주소", "상세주소",
                        "토지면적(㎡)", "건물면적(㎡)", "층수", "감정가", "최저입찰가",
                        "매각가", "차주명", "담보번호", "OPB"
                    };

                    // 첫 번째 행: 영문 헤더
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1E3A5F");
                        cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    }

                    // 두 번째 행: 한글 설명
                    for (int i = 0; i < headerLabels.Length; i++)
                    {
                        var cell = worksheet.Cell(2, i + 1);
                        cell.Value = headerLabels[i];
                        cell.Style.Font.Italic = true;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E8EEF4");
                    }

                    // 세 번째 행: 현재 물건 정보 채우기
                    if (Property != null)
                    {
                        worksheet.Cell(3, 1).Value = Property.PropertyType ?? "";
                        worksheet.Cell(3, 2).Value = Property.AddressFull ?? "";
                        worksheet.Cell(3, 3).Value = Property.AddressRoad ?? "";
                        worksheet.Cell(3, 4).Value = Property.AddressJibun ?? "";
                        worksheet.Cell(3, 5).Value = Property.AddressDetail ?? "";
                        worksheet.Cell(3, 6).Value = Property.LandArea ?? 0;
                        worksheet.Cell(3, 7).Value = Property.BuildingArea ?? 0;
                        worksheet.Cell(3, 8).Value = Property.Floors ?? "";
                        worksheet.Cell(3, 9).Value = Property.AppraisalValue ?? 0;
                        worksheet.Cell(3, 10).Value = Property.MinimumBid ?? 0;
                        worksheet.Cell(3, 11).Value = Property.SalePrice ?? 0;
                        worksheet.Cell(3, 12).Value = Property.DebtorName ?? "";
                        worksheet.Cell(3, 13).Value = Property.CollateralNumber ?? "";
                        worksheet.Cell(3, 14).Value = Property.Opb ?? 0;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);

                    SuccessMessage = $"템플릿이 저장되었습니다: {System.IO.Path.GetFileName(saveDialog.FileName)}";
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

                // 저장 전 스냅샷 저장 (Undo용)
                if (Property != null && Property.Id != Guid.Empty)
                {
                    UndoService.Instance.SaveSnapshot(Property, "저장 전");
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(UndoCount));
                }

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
        /// 되돌리기 명령 (피드백 반영: 최대 3단계)
        /// </summary>
        [RelayCommand]
        private async Task UndoAsync()
        {
            if (Property == null || Property.Id == Guid.Empty || !CanUndo)
            {
                ErrorMessage = "되돌릴 수 없습니다.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var restoredProperty = UndoService.Instance.Undo(Property.Id);
                if (restoredProperty != null)
                {
                    // DB에 복원된 상태 저장
                    await _propertyRepository.UpdateAsync(restoredProperty);
                    
                    // Property 업데이트
                    Property = restoredProperty;
                    
                    SuccessMessage = "이전 상태로 되돌렸습니다.";
                    HasUnsavedChanges = false;
                    
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(UndoCount));
                }
                else
                {
                    ErrorMessage = "되돌리기에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"되돌리기 실패: {ex.Message}";
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

        // ========== N-001: 키보드 탐색 Commands ==========

        /// <summary>
        /// 이전 물건으로 이동 (N-001)
        /// </summary>
        [RelayCommand]
        private async Task NavigatePreviousAsync()
        {
            if (!CanNavigatePrevious || _propertyList == null) return;
            
            // 변경사항이 있으면 자동 저장
            if (HasUnsavedChanges)
            {
                await SaveAsync();
            }
            
            _currentIndex--;
            var targetProperty = _propertyList[_currentIndex];
            _navigateToPropertyAction?.Invoke(targetProperty.Id);
        }

        /// <summary>
        /// 다음 물건으로 이동 (N-001)
        /// </summary>
        [RelayCommand]
        private async Task NavigateNextAsync()
        {
            if (!CanNavigateNext || _propertyList == null) return;
            
            // 변경사항이 있으면 자동 저장
            if (HasUnsavedChanges)
            {
                await SaveAsync();
            }
            
            _currentIndex++;
            var targetProperty = _propertyList[_currentIndex];
            _navigateToPropertyAction?.Invoke(targetProperty.Id);
        }

        /// <summary>
        /// 첫 번째 물건으로 이동 (N-001)
        /// </summary>
        [RelayCommand]
        private async Task NavigateFirstAsync()
        {
            if (_propertyList == null || _propertyList.Count == 0 || _currentIndex == 0) return;
            
            // 변경사항이 있으면 자동 저장
            if (HasUnsavedChanges)
            {
                await SaveAsync();
            }
            
            _currentIndex = 0;
            var targetProperty = _propertyList[_currentIndex];
            _navigateToPropertyAction?.Invoke(targetProperty.Id);
        }

        /// <summary>
        /// 마지막 물건으로 이동 (N-001)
        /// </summary>
        [RelayCommand]
        private async Task NavigateLastAsync()
        {
            if (_propertyList == null || _propertyList.Count == 0 || _currentIndex == _propertyList.Count - 1) return;
            
            // 변경사항이 있으면 자동 저장
            if (HasUnsavedChanges)
            {
                await SaveAsync();
            }
            
            _currentIndex = _propertyList.Count - 1;
            var targetProperty = _propertyList[_currentIndex];
            _navigateToPropertyAction?.Invoke(targetProperty.Id);
        }

        /// <summary>
        /// 특정 인덱스의 물건으로 이동 (N-001)
        /// </summary>
        public async Task NavigateToIndexAsync(int index)
        {
            if (_propertyList == null || index < 0 || index >= _propertyList.Count || index == _currentIndex) return;
            
            // 변경사항이 있으면 자동 저장
            if (HasUnsavedChanges)
            {
                await SaveAsync();
            }
            
            _currentIndex = index;
            var targetProperty = _propertyList[_currentIndex];
            _navigateToPropertyAction?.Invoke(targetProperty.Id);
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
            OnPropertyChanged(nameof(DisplayAddress));
        }

        /// <summary>
        /// 탭 변경 시 자동저장 (피드백 반영: 팝업 제거, 자동저장 + 되돌리기)
        /// </summary>
        partial void OnSelectedTabIndexChanging(int value)
        {
            // 탭 변경 중이 아니고, 저장되지 않은 변경사항이 있으면 자동 저장
            if (!_isChangingTab && HasUnsavedChanges && _previousTabIndex != value)
            {
                // 자동 저장 (팝업 없이)
                _ = SaveAsync();
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
