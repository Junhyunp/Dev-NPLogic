using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Core.Services;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 선순위 관리 ViewModel
    /// </summary>
    public partial class SeniorRightsViewModel : ObservableObject
    {
        private readonly RegistryRepository _registryRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly RightAnalysisRepository _rightAnalysisRepository;
        private readonly ReferenceDataRepository? _referenceDataRepository;
        private readonly BorrowerRepository? _borrowerRepository;
        private readonly NPLogic.Services.VworldService? _vworldService;
        private readonly LeaseItemRepository? _leaseItemRepository;
        private readonly WageClaimItemRepository? _wageClaimItemRepository;
        private readonly PropertyNoteRepository? _propertyNoteRepository;

        // 현재 물건의 권리분석 데이터
        private RightAnalysis? _currentRightAnalysis;

        // ========== 선행 경매사건 정보 ==========
        [ObservableProperty]
        private string _precedentAuctionStatus = "not_opened";

        [ObservableProperty]
        private string _precedentCourtName = "";

        [ObservableProperty]
        private string _precedentCaseNumber = "";

        [ObservableProperty]
        private string _precedentAuctionApplicant = "";

        [ObservableProperty]
        private DateTime? _precedentAuctionStartDate;

        [ObservableProperty]
        private DateTime? _precedentClaimDeadlineDate;

        [ObservableProperty]
        private decimal _precedentClaimAmount;

        [ObservableProperty]
        private decimal _precedentInitialAppraisalValue;

        [ObservableProperty]
        private DateTime? _precedentInitialAuctionDate;

        [ObservableProperty]
        private string _precedentFinalAuctionRound = "";

        [ObservableProperty]
        private DateTime? _precedentFinalAuctionDate;

        [ObservableProperty]
        private string _precedentFinalAuctionResult = "";

        // ========== 후행 경매사건 정보 ==========
        [ObservableProperty]
        private string _subsequentAuctionStatus = "not_opened";

        [ObservableProperty]
        private string _subsequentCourtName = "";

        [ObservableProperty]
        private string _subsequentCaseNumber = "";

        [ObservableProperty]
        private string _subsequentAuctionApplicant = "";

        [ObservableProperty]
        private DateTime? _subsequentAuctionStartDate;

        [ObservableProperty]
        private DateTime? _subsequentClaimDeadlineDate;

        [ObservableProperty]
        private decimal _subsequentWinningBidAmount;

        [ObservableProperty]
        private decimal _subsequentMinimumBid;

        [ObservableProperty]
        private decimal _subsequentNextMinimumBid;

        // ========== 기존 경매사건 정보 (호환성 유지) ==========
        [ObservableProperty]
        private string _auctionStatus = "not_opened"; // opened, not_opened

        [ObservableProperty]
        private bool _precedentAuction; // 선행

        [ObservableProperty]
        private bool _subsequentAuction; // 후행

        [ObservableProperty]
        private string _courtName = ""; // 관할법원

        [ObservableProperty]
        private string _caseNumber = ""; // 경매사건번호

        [ObservableProperty]
        private string _auctionApplicant = ""; // 경매신청기관

        [ObservableProperty]
        private DateTime? _auctionStartDate; // 경매개시일자

        [ObservableProperty]
        private DateTime? _claimDeadlineDate; // 배당요구종기일

        [ObservableProperty]
        private decimal _claimAmount; // 청구금액

        [ObservableProperty]
        private decimal _initialAppraisalValue; // 최초법사가 (감정가)

        [ObservableProperty]
        private DateTime? _initialAuctionDate; // 최초경매기일

        [ObservableProperty]
        private string _finalAuctionRound = ""; // 최종경매회차

        [ObservableProperty]
        private string _finalAuctionResult = ""; // 최종경매결과

        [ObservableProperty]
        private decimal _winningBidAmount; // 낙찰금액

        [ObservableProperty]
        private DateTime? _nextAuctionDate; // 차후예정경매일

        [ObservableProperty]
        private decimal _nextMinimumBid; // 차후최저입찰가

        [ObservableProperty]
        private bool _claimDeadlinePassed; // 배당요구종기일 경과

        // ========== 등기부 항목 ==========
        [ObservableProperty]
        private ObservableCollection<RegistryRight> _registryEntries = new();

        // ========== 전입/임차 우측 등기부 요약(읽기전용) ==========
        [ObservableProperty]
        private ObservableCollection<RegistryMortgageDisplayRow> _registryMortgageRows = new();

        // ========== Tool Box: 주택임대차 ==========
        [ObservableProperty]
        private ObservableCollection<LeaseItem> _residentialLeases = new();

        public decimal ResidentialLeasesTotalDeposit => ResidentialLeases.Sum(l => l.Deposit ?? 0);

        [ObservableProperty]
        private bool _hasResidentialLeaseTable;

        // ========== Tool Box: 상가임대차 ==========
        [ObservableProperty]
        private ObservableCollection<LeaseItem> _commercialLeases = new();

        public decimal CommercialLeasesTotalDeposit => CommercialLeases.Sum(l => l.Deposit ?? 0);

        [ObservableProperty]
        private bool _hasCommercialLeaseTable;

        // ========== Tool Box: 임금채권 ==========
        [ObservableProperty]
        private ObservableCollection<WageClaimItem> _wageClaims = new();

        public decimal WageClaimsTotalAmount => WageClaims.Sum(w => w.ReflectedAmount);

        [ObservableProperty]
        private bool _hasWageClaimTable;

        // ========== 열람 자료 이미지 ==========
        [ObservableProperty]
        private BitmapSource? _tenantRegistryImage;

        [ObservableProperty]
        private string _tenantRegistryImageInfo = "";

        [ObservableProperty]
        private BitmapSource? _commercialLeaseImage;

        [ObservableProperty]
        private string _commercialLeaseImageInfo = "";

        [ObservableProperty]
        private BitmapSource? _rightsAnalysisImage;

        [ObservableProperty]
        private string _rightsAnalysisImageInfo = "";

        [ObservableProperty]
        private BitmapSource? _wageDataImage;

        [ObservableProperty]
        private string _wageDataImageInfo = "";

        // ========== 당사자내역 이미지 ==========
        [ObservableProperty]
        private BitmapSource? _partyDetailsImage;

        [ObservableProperty]
        private string _partyDetailsImageInfo = "";

        // ========== 경매사건 하단 메모 ==========
        [ObservableProperty]
        private string _surveyReportNote = "";

        [ObservableProperty]
        private string _appraisalReportNote = "";

        // ========== 대법원 경매 이미지 ==========
        [ObservableProperty]
        private BitmapSource? _courtCaseSearchImage;

        [ObservableProperty]
        private string _courtCaseSearchImageInfo = "";

        [ObservableProperty]
        private BitmapSource? _courtDateSearchImage;

        [ObservableProperty]
        private string _courtDateSearchImageInfo = "";

        [ObservableProperty]
        private BitmapSource? _courtDocumentDeliveryImage;

        [ObservableProperty]
        private string _courtDocumentDeliveryImageInfo = "";

        // ========== 선순위 구분 상세 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _seniorMortgageDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _seniorMortgageReflected;

        [ObservableProperty]
        private string _seniorMortgageReason = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _currentTaxDd;

        [ObservableProperty]
        private string _currentTaxReason = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _seniorTaxClaimDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _seniorTaxClaimReflected;

        [ObservableProperty]
        private string _seniorTaxClaimReason = "";

        // ========== 선순위 요약 ==========
        public decimal TotalLeaseDeposit => LeaseDepositReflected + SmallDepositReflected;
        
        public decimal OtherSeniorRights => LienReflected + WageClaimReflected + SeniorTaxClaimReflected;

        // ========== 전입/임차 현황 체크리스트 ==========
        [ObservableProperty]
        private bool _addressMatch; // 물건지, 소유주 주소지 일치

        [ObservableProperty]
        private bool _ownerRegistered; // 소유주 전입

        [ObservableProperty]
        private bool _hasTenant; // 임차인 존재

        [ObservableProperty]
        private bool _surveyReportSubmitted; // 현황조사서 제출

        [ObservableProperty]
        private bool _hasTenantRegistry; // 전입세대열람 보유

        [ObservableProperty]
        private bool _hasCommercialLease; // 상가임대차열람 보유

        [ObservableProperty]
        private bool _tenantDateBeforeMortgage; // 임차일 근저당설정일 이전

        [ObservableProperty]
        private bool _hasAuctionDocs; // 경매열람자료 보유

        [ObservableProperty]
        private string _debtorType = "individual"; // 채무자 유형: individual, business, corporation

        [ObservableProperty]
        private string _borrowerTypeDisplay = ""; // 차주구분 (DD에서 가져온 한국어 표시용: 개인/개인사업자/법인)

        [ObservableProperty]
        private bool _hasWageClaim; // 임금채권 존재

        [ObservableProperty]
        private decimal _wageClaimEstimatedSeizure; // 임금채권 추정가압류 (금액)

        [ObservableProperty]
        private bool _hasTaxClaim; // 당해세 교부청구

        [ObservableProperty]
        private bool _hasSeniorTaxClaim; // 선순위조세 교부청구

        [ObservableProperty]
        private decimal _housingOfficialPrice; // 주택공시가격

        [ObservableProperty]
        private decimal _officialLandPrice; // 공시지가

        [ObservableProperty]
        private decimal _buildingStandardPrice; // 건물기준시가

        // ========== 배당 시뮬레이션 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CapAppliedDividend))]
        [NotifyPropertyChangedFor(nameof(RecoveryRate))]
        private decimal _loanCap; // Loan Cap

        /// <summary>
        /// 선순위 공제 후 금액
        /// </summary>
        public decimal AmountAfterSenior => RemainingAmount > 0 ? RemainingAmount : 0;

        /// <summary>
        /// Cap 반영 배당액
        /// </summary>
        public decimal CapAppliedDividend => LoanCap > 0 
            ? Math.Min(AmountAfterSenior, LoanCap) 
            : AmountAfterSenior;

        /// <summary>
        /// 예상 회수율 (%)
        /// </summary>
        public decimal RecoveryRate => LoanCap > 0 
            ? Math.Round(CapAppliedDividend / LoanCap * 100, 1) 
            : 0;

        // ========== 위험도 평가 ==========
        [ObservableProperty]
        private string _riskLevel = ""; // high, medium, low

        [ObservableProperty]
        private string _riskReason = "";

        [ObservableProperty]
        private string _recommendations = "";

        [ObservableProperty]
        private bool _isAnalysisCompleted;

        [ObservableProperty]
        private DateTime? _analyzedAt;

        [ObservableProperty]
        private ObservableCollection<RegistryRight> _rights = new();

        [ObservableProperty]
        private RegistryRight? _selectedRight;

        // ========== P-001: 선순위 근저당 목록 ==========
        [ObservableProperty]
        private ObservableCollection<RegistryRight> _seniorMortgages = new();

        // ========== P-002: 유치권 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _lienDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _lienReflected;

        [ObservableProperty]
        private string _lienReason = "";

        // ========== P-003: 선순위 소액보증금 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _smallDepositDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _smallDepositReflected;

        [ObservableProperty]
        private string _smallDepositReason = "";

        // ========== L-003: 소액임차보증금 자동 조회 ==========
        [ObservableProperty]
        private ObservableCollection<string> _smallDepositRegions = new() { "서울", "수도권", "광역시", "기타" };

        [ObservableProperty]
        private string _selectedSmallDepositRegion = "서울";

        [ObservableProperty]
        private DateTime? _mortgageSetupDate;

        [ObservableProperty]
        private decimal _lookedUpSmallDeposit;

        [ObservableProperty]
        private string _smallDepositLookupInfo = "";

        // ========== P-007: 확장 패널 토글 상태 ==========
        [ObservableProperty]
        private bool _isResidentialLeaseExpanded;

        [ObservableProperty]
        private bool _isCommercialLeaseExpanded;

        [ObservableProperty]
        private bool _isWageClaimExpanded;

        // ========== P-007: 주택임대차 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _leaseDepositDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _leaseDepositReflected;

        [ObservableProperty]
        private string _leaseDepositReason = "";

        [ObservableProperty]
        private string _tenantName = "";

        [ObservableProperty]
        private DateTime? _tenantMoveInDate;

        [ObservableProperty]
        private bool _tenantClaimSubmitted;

        // ========== P-007: 임금채권 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsDdTotal))]
        private decimal _wageClaimDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _wageClaimReflected;

        [ObservableProperty]
        private string _wageClaimReason = "";

        [ObservableProperty]
        private bool _wageClaimSubmitted;

        // ========== 필터 ==========
        [ObservableProperty]
        private ObservableCollection<string> _rightTypes = new() { "전체", "갑구", "을구" };

        [ObservableProperty]
        private string _selectedRightType = "전체";

        [ObservableProperty]
        private ObservableCollection<string> _statuses = new() { "전체", "active", "cancelled" };

        [ObservableProperty]
        private string _selectedStatus = "전체";

        [ObservableProperty]
        private string _searchKeyword = "";

        // ========== 물건 선택 ==========
        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        [ObservableProperty]
        private Property? _selectedProperty;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        private decimal _totalClaimAmount;

        // ========== 선순위 합계 섹션 (P-004) ==========
        /// <summary>
        /// 예상 낙찰가 (사용자 입력)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DistributableAmount))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        private decimal _expectedBidPrice;

        /// <summary>
        /// 배당가능 재원 (예상 낙찰가 - 경매비용)
        /// 경매비용은 일반적으로 낙찰가의 약 3~5% 정도
        /// </summary>
        public decimal DistributableAmount => ExpectedBidPrice > 0 
            ? ExpectedBidPrice - AuctionCost 
            : 0;

        /// <summary>
        /// 경매비용 (사용자 입력 또는 계산)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DistributableAmount))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        private decimal _auctionCost;

        /// <summary>
        /// 선순위 금액 합계 (근저당 + 유치권 + 소액보증금 + 임차보증금 + 임금채권 + 당해세)
        /// 반영금액 기준으로 합산
        /// </summary>
        public decimal SeniorRightsDdTotal =>
            SeniorMortgageDd +
            LienDd +
            SmallDepositDd +
            LeaseDepositDd +
            WageClaimDd +
            CurrentTaxDd +
            SeniorTaxClaimDd;

        public decimal SeniorRightsReflectedTotal =>
            SeniorMortgageReflected +
            LienReflected +
            SmallDepositReflected +
            LeaseDepositReflected +
            WageClaimReflected +
            CurrentTax +
            SeniorTaxClaimReflected;

        public decimal SeniorRightsTotal => 
            SeniorMortgagesTotal + 
            LienReflected + 
            SmallDepositReflected + 
            LeaseDepositReflected + 
            WageClaimReflected + 
            CurrentTax;

        /// <summary>
        /// 선순위 근저당 합계
        /// </summary>
        public decimal SeniorMortgagesTotal => SeniorMortgages.Where(m => m.Status == "active").Sum(m => m.ClaimAmount ?? 0);

        /// <summary>
        /// 선순위 차감 배당가능 재원
        /// </summary>
        public decimal RemainingAmount => DistributableAmount - SeniorRightsTotal;


        // ========== 당해세 처리 (P-005, P-006) ==========
        /// <summary>
        /// 당해세 금액 (사용자 입력)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        [NotifyPropertyChangedFor(nameof(SeniorRightsReflectedTotal))]
        private decimal _currentTax;

        /// <summary>
        /// etax korea URL
        /// </summary>
        private const string EtaxKoreaUrl = "https://www.etax.go.kr";

        /// <summary>
        /// 대법원 경매 사이트 URL (X-002)
        /// </summary>
        private const string CourtAuctionUrl = "https://www.courtauction.go.kr";
        private const int RegistryMortgageRowCount = 7;

        public class RegistryMortgageDisplayRow
        {
            public string Receipt { get; set; } = "";
            public string MortgageHolder { get; set; } = "";
            public decimal? MaxClaimAmount { get; set; }
        }

        // ========== 편집 ==========
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isNewRecord;

        // 편집 필드
        [ObservableProperty]
        private string _editSection = "갑구";

        [ObservableProperty]
        private int _editRightOrder = 1;

        [ObservableProperty]
        private string _editRightHolder = "";

        [ObservableProperty]
        private decimal _editClaimAmount;

        [ObservableProperty]
        private DateTime? _editRegistrationDate;

        [ObservableProperty]
        private string _editRegistrationNumber = "";

        [ObservableProperty]
        private string _editRegistrationCause = "";

        [ObservableProperty]
        private string _editStatus = "active";

        [ObservableProperty]
        private string _editNotes = "";

        // ========== 비고 사이드 패널 ==========
        [ObservableProperty]
        private string _noteText = string.Empty;

        [ObservableProperty]
        private bool _isNotePanelVisible;

        [RelayCommand]
        private void ToggleNotePanel()
        {
            IsNotePanelVisible = !IsNotePanelVisible;
        }

        private async Task LoadNoteAsync()
        {
            if (_propertyNoteRepository == null || SelectedProperty == null) return;
            try
            {
                var note = await _propertyNoteRepository.GetByPropertyAndTabAsync(SelectedProperty.Id, "senior_rights");
                NoteText = note?.NoteText ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SeniorRights] 비고 로드 실패: {ex.Message}");
            }
        }

        private async Task SaveNoteAsync()
        {
            if (_propertyNoteRepository == null || SelectedProperty == null) return;
            try
            {
                await _propertyNoteRepository.UpsertAsync(new NPLogic.Core.Models.PropertyNote
                {
                    PropertyId = SelectedProperty.Id,
                    TabName = "senior_rights",
                    NoteText = NoteText ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SeniorRights] 비고 저장 실패: {ex.Message}");
            }
        }

        public SeniorRightsViewModel(
            RegistryRepository registryRepository,
            PropertyRepository propertyRepository,
            RightAnalysisRepository rightAnalysisRepository,
            ReferenceDataRepository? referenceDataRepository = null,
            BorrowerRepository? borrowerRepository = null,
            NPLogic.Services.VworldService? vworldService = null,
            LeaseItemRepository? leaseItemRepository = null,
            WageClaimItemRepository? wageClaimItemRepository = null,
            PropertyNoteRepository? propertyNoteRepository = null)
        {
            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _rightAnalysisRepository = rightAnalysisRepository ?? throw new ArgumentNullException(nameof(rightAnalysisRepository));
            _referenceDataRepository = referenceDataRepository;
            _borrowerRepository = borrowerRepository;
            _vworldService = vworldService;
            _leaseItemRepository = leaseItemRepository;
            _wageClaimItemRepository = wageClaimItemRepository;
            _propertyNoteRepository = propertyNoteRepository;
            InitializeEmptyRegistryMortgageRows();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await Task.WhenAll(
                    LoadPropertiesAsync(),
                    LoadRightsAsync(),
                    LoadRightAnalysisAsync()
                );
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 물건 목록 로드
        /// </summary>
        private async Task LoadPropertiesAsync()
        {
            try
            {
                var properties = await _propertyRepository.GetAllAsync();
                Properties.Clear();
                foreach (var prop in properties)
                {
                    Properties.Add(prop);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 권리 목록 로드
        /// </summary>
        private async Task LoadRightsAsync()
        {
            try
            {
                IsLoading = true;

                List<RegistryRight> rights;

                if (SelectedProperty != null)
                {
                    rights = await _registryRepository.GetRightsByPropertyIdAsync(SelectedProperty.Id);
                }
                else
                {
                    rights = await _registryRepository.GetAllRightsAsync();
                }

                // 필터 적용
                if (SelectedRightType != "전체")
                {
                    rights = rights.Where(r => r.Section == SelectedRightType).ToList();
                }

                if (SelectedStatus != "전체")
                {
                    rights = rights.Where(r => r.Status == SelectedStatus).ToList();
                }

                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    rights = rights.Where(r =>
                        (r.RightHolder?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.RegistrationCause?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.RegistrationNumber?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                Rights.Clear();
                foreach (var right in rights.OrderBy(r => r.RightOrder))
                {
                    Rights.Add(right);
                }

                TotalCount = Rights.Count;
                TotalClaimAmount = Rights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);

                // P-001: 선순위 근저당 필터링 (을구에서 근저당권설정만)
                LoadSeniorMortgages();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"권리 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// P-001: 선순위 근저당 목록 로드
        /// </summary>
        private void LoadSeniorMortgages()
        {
            SeniorMortgages.Clear();
            
            // 을구(eul)에서 근저당권설정 항목만 필터링하여 순위별로 정렬
            var mortgages = Rights
                .Where(r => r.Section == "을구" &&
                           (r.RegistrationCause?.Contains("근저당") ?? false) &&
                           r.Status == "active")
                .OrderBy(r => r.RightOrder);

            foreach (var mortgage in mortgages)
            {
                SeniorMortgages.Add(mortgage);
            }

            // 선순위 합계 갱신
            OnPropertyChanged(nameof(SeniorMortgagesTotal));
            OnPropertyChanged(nameof(SeniorRightsTotal));
            OnPropertyChanged(nameof(RemainingAmount));
        }

        /// <summary>
        /// 권리분석 데이터 로드
        /// </summary>
        private async Task LoadRightAnalysisAsync()
        {
            if (SelectedProperty == null)
            {
                ResetRightAnalysisFields();
                return;
            }

            try
            {
                _currentRightAnalysis = await _rightAnalysisRepository.GetByPropertyIdAsync(SelectedProperty.Id);

                if (_currentRightAnalysis != null)
                {
                    // 선순위근저당권
                    SeniorMortgageDd = _currentRightAnalysis.SeniorMortgageDd;
                    SeniorMortgageReflected = _currentRightAnalysis.SeniorMortgageReflected;
                    SeniorMortgageReason = _currentRightAnalysis.SeniorMortgageReason ?? "";

                    // P-002: 유치권
                    LienDd = _currentRightAnalysis.LienDd;
                    LienReflected = _currentRightAnalysis.LienReflected;
                    LienReason = _currentRightAnalysis.LienReason ?? "";

                    // P-003: 소액보증금
                    SmallDepositDd = _currentRightAnalysis.SmallDepositDd;
                    SmallDepositReflected = _currentRightAnalysis.SmallDepositReflected;
                    SmallDepositReason = _currentRightAnalysis.SmallDepositReason ?? "";

                    // P-007: 주택임대차
                    LeaseDepositDd = _currentRightAnalysis.LeaseDepositDd;
                    LeaseDepositReflected = _currentRightAnalysis.LeaseDepositReflected;
                    LeaseDepositReason = _currentRightAnalysis.LeaseDepositReason ?? "";
                    TenantName = _currentRightAnalysis.TenantName ?? "";
                    TenantMoveInDate = _currentRightAnalysis.TenantMoveInDate;
                    TenantClaimSubmitted = _currentRightAnalysis.TenantClaimSubmitted ?? false;

                    // P-007: 임금채권
                    WageClaimDd = _currentRightAnalysis.WageClaimDd;
                    WageClaimReflected = _currentRightAnalysis.WageClaimReflected;
                    WageClaimReason = _currentRightAnalysis.WageClaimReason ?? "";
                    WageClaimSubmitted = _currentRightAnalysis.WageClaimSubmitted;

                    // 당해세
                    CurrentTaxDd = _currentRightAnalysis.CurrentTaxDd;
                    CurrentTax = _currentRightAnalysis.CurrentTaxReflected;
                    CurrentTaxReason = _currentRightAnalysis.CurrentTaxReason ?? "";

                    // 선순위 조세채권
                    SeniorTaxClaimDd = _currentRightAnalysis.SeniorTaxDd;
                    SeniorTaxClaimReflected = _currentRightAnalysis.SeniorTaxReflected;
                    SeniorTaxClaimReason = _currentRightAnalysis.SeniorTaxReason ?? "";

                    // 합계 섹션
                    ExpectedBidPrice = _currentRightAnalysis.ExpectedWinningBid ?? 0;
                    AuctionCost = _currentRightAnalysis.AuctionFees ?? 0;
                    LoanCap = _currentRightAnalysis.LoanCap ?? 0;

                    // 경매사건 정보
                    AuctionStatus = _currentRightAnalysis.AuctionStatus ?? "not_opened";
                    PrecedentAuction = _currentRightAnalysis.PrecedentAuction;
                    SubsequentAuction = _currentRightAnalysis.SubsequentAuction;
                    CourtName = _currentRightAnalysis.CourtName ?? "";
                    CaseNumber = _currentRightAnalysis.CaseNumber ?? "";
                    AuctionApplicant = _currentRightAnalysis.AuctionApplicant ?? "";
                    AuctionStartDate = _currentRightAnalysis.AuctionStartDate;
                    ClaimDeadlineDate = _currentRightAnalysis.ClaimDeadlineDate;
                    ClaimAmount = _currentRightAnalysis.ClaimAmount ?? 0;
                    InitialAppraisalValue = _currentRightAnalysis.InitialAppraisalValue ?? 0;
                    InitialAuctionDate = _currentRightAnalysis.InitialAuctionDate;
                    FinalAuctionRound = _currentRightAnalysis.FinalAuctionRound?.ToString() ?? "";
                    FinalAuctionResult = _currentRightAnalysis.FinalAuctionResult ?? "";
                    WinningBidAmount = _currentRightAnalysis.WinningBidAmount ?? 0;
                    NextAuctionDate = _currentRightAnalysis.NextAuctionDate;
                    NextMinimumBid = _currentRightAnalysis.NextMinimumBid ?? 0;
                    // 배당요구종기일 경과 여부: DD의 날짜 기준 자동 판단 (선행 우선, 없으면 후행)
                    var deadlineDate = SelectedProperty?.PrecedentClaimDeadline ?? SelectedProperty?.SubsequentClaimDeadline;
                    ClaimDeadlinePassed = deadlineDate.HasValue && deadlineDate.Value.Date <= DateTime.Today;

                    // 경매사건 메모/이미지
                    SurveyReportNote = _currentRightAnalysis.SurveyReportNote ?? "";
                    AppraisalReportNote = _currentRightAnalysis.AppraisalReportNote ?? "";
                    LoadPartyDetailsImageFromBase64(_currentRightAnalysis.PartyDetailsImageBase64);
                    LoadImageFromBase64(_currentRightAnalysis.CourtCaseSearchImage, v => CourtCaseSearchImage = v, v => CourtCaseSearchImageInfo = v);
                    LoadImageFromBase64(_currentRightAnalysis.CourtDateSearchImage, v => CourtDateSearchImage = v, v => CourtDateSearchImageInfo = v);
                    LoadImageFromBase64(_currentRightAnalysis.CourtDocumentDeliveryImage, v => CourtDocumentDeliveryImage = v, v => CourtDocumentDeliveryImageInfo = v);
                    LoadImageFromBase64(_currentRightAnalysis.TenantRegistryImage, v => TenantRegistryImage = v, v => TenantRegistryImageInfo = v);
                    LoadImageFromBase64(_currentRightAnalysis.CommercialLeaseImage, v => CommercialLeaseImage = v, v => CommercialLeaseImageInfo = v);
                    LoadImageFromBase64(_currentRightAnalysis.RightsAnalysisImage, v => RightsAnalysisImage = v, v => RightsAnalysisImageInfo = v);
                    LoadImageFromBase64(_currentRightAnalysis.WageDataImage, v => WageDataImage = v, v => WageDataImageInfo = v);

                    // 전입/임차 현황
                    OwnerRegistered = _currentRightAnalysis.OwnerRegistered ?? false;
                    HasTenant = _currentRightAnalysis.HasTenant ?? false;
                    SurveyReportSubmitted = _currentRightAnalysis.SurveyReportSubmitted ?? false;
                    HasTenantRegistry = _currentRightAnalysis.HasTenantRegistry;
                    HasCommercialLease = _currentRightAnalysis.HasCommercialLease;
                    TenantDateBeforeMortgage = _currentRightAnalysis.TenantDateBeforeMortgage ?? false;
                    HasAuctionDocs = _currentRightAnalysis.HasAuctionDocs;
                    DebtorType = _currentRightAnalysis.DebtorType ?? "individual";
                    HasWageClaim = _currentRightAnalysis.HasWageClaim;
                    WageClaimEstimatedSeizure = _currentRightAnalysis.WageClaimEstimatedSeizure;
                    HasTaxClaim = _currentRightAnalysis.HasTaxClaim;
                    HasSeniorTaxClaim = _currentRightAnalysis.HasSeniorTaxClaim;
                    HousingOfficialPrice = _currentRightAnalysis.HousingOfficialPrice ?? 0;
                    OfficialLandPrice = _currentRightAnalysis.OfficialLandPrice;
                    BuildingStandardPrice = _currentRightAnalysis.BuildingStandardPrice;

                    // 위험도 평가
                    RiskLevel = _currentRightAnalysis.RiskLevel ?? "";
                    RiskReason = _currentRightAnalysis.RiskReason ?? "";
                    Recommendations = _currentRightAnalysis.Recommendations ?? "";
                    IsAnalysisCompleted = _currentRightAnalysis.IsCompleted;
                    AnalyzedAt = _currentRightAnalysis.AnalyzedAt;
                }
                else
                {
                    ResetRightAnalysisFields();
                }

                // === 임대차/임금채권 테이블 데이터 로드 ===
                try
                {
                    if (_leaseItemRepository != null)
                    {
                        var residentialItems = await _leaseItemRepository.GetByPropertyIdAsync(SelectedProperty.Id, "residential");
                        ResidentialLeases.Clear();
                        foreach (var item in residentialItems) ResidentialLeases.Add(item);
                        HasResidentialLeaseTable = residentialItems.Count > 0;
                        OnPropertyChanged(nameof(ResidentialLeasesTotalDeposit));

                        var commercialItems = await _leaseItemRepository.GetByPropertyIdAsync(SelectedProperty.Id, "commercial");
                        CommercialLeases.Clear();
                        foreach (var item in commercialItems) CommercialLeases.Add(item);
                        HasCommercialLeaseTable = commercialItems.Count > 0;
                        OnPropertyChanged(nameof(CommercialLeasesTotalDeposit));
                    }

                    if (_wageClaimItemRepository != null)
                    {
                        var wageItems = await _wageClaimItemRepository.GetByPropertyIdAsync(SelectedProperty.Id);
                        WageClaims.Clear();
                        foreach (var item in wageItems) WageClaims.Add(item);
                        HasWageClaimTable = wageItems.Count > 0;
                        OnPropertyChanged(nameof(WageClaimsTotalAmount));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 임대차/임금채권 로드 실패: {ex.Message}");
                }

                // === right_analysis 유무와 무관하게 외부 데이터 로드 ===
                // 주소지 일치여부: registry_basic_info에서 가져오기
                try
                {
                    var basicInfoList = await _registryRepository.GetBasicInfoListByPropertyIdAsync(SelectedProperty.Id);
                    if (basicInfoList.Count > 0)
                        AddressMatch = basicInfoList.All(bi => bi.IsAddressMatched ?? true);
                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 주소지 일치: basicInfoCount={basicInfoList.Count}, AddressMatch={AddressMatch}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 주소지 일치 로드 실패: {ex.Message}");
                }

                // 전입/임차 우측 표(접수정보/근저당권자/채권최고액): OCR 을구에서 자동 로드
                await LoadRegistryMortgageRowsAsync(SelectedProperty.Id);

                // 차주구분: borrowers 테이블에서 가져오기 (BorrowerId 또는 BorrowerNumber로 조회)
                try
                {
                    if (_borrowerRepository != null)
                    {
                        Core.Models.Borrower? borrower = null;
                        if (SelectedProperty.BorrowerId.HasValue)
                        {
                            borrower = await _borrowerRepository.GetByIdAsync(SelectedProperty.BorrowerId.Value);
                            System.Diagnostics.Debug.WriteLine($"[SeniorRights] 차주구분: BorrowerId={SelectedProperty.BorrowerId}로 조회");
                        }
                        else if (!string.IsNullOrWhiteSpace(SelectedProperty.BorrowerNumber))
                        {
                            borrower = await _borrowerRepository.GetByBorrowerNumberAsync(SelectedProperty.BorrowerNumber);
                            System.Diagnostics.Debug.WriteLine($"[SeniorRights] 차주구분: BorrowerNumber={SelectedProperty.BorrowerNumber}로 조회");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[SeniorRights] 차주구분: BorrowerId/BorrowerNumber 모두 없음");
                        }
                        BorrowerTypeDisplay = borrower?.BorrowerType ?? "";
                        System.Diagnostics.Debug.WriteLine($"[SeniorRights] 차주구분 결과: BorrowerTypeDisplay={BorrowerTypeDisplay}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SeniorRights] 차주구분: _borrowerRepository is null");
                    }
                }
                catch (Exception ex)
                {
                    BorrowerTypeDisplay = "";
                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 차주구분 로드 실패: {ex.Message}");
                }

                // 주택공시가격: DB에 값이 없으면 VWORLD API로 자동 조회
                if (HousingOfficialPrice == 0 && _vworldService != null)
                {
                    try
                    {
                        var pnu = SelectedProperty.Pnu;

                        // PNU가 없으면 주소로 조회하여 PNU 확보 + DB 저장
                        if (string.IsNullOrWhiteSpace(pnu) && !string.IsNullOrWhiteSpace(SelectedProperty.AddressFull))
                        {
                            var searchResult = await _vworldService.SearchAddressAsync(SelectedProperty.AddressFull);
                            if (searchResult?.IsValidPnu == true)
                            {
                                pnu = searchResult.Pnu;
                                SelectedProperty.Pnu = pnu;
                                // DB에 PNU 저장 (다음번엔 바로 사용)
                                await _propertyRepository.UpdatePnuAsync(SelectedProperty.Id, pnu);
                                System.Diagnostics.Debug.WriteLine($"[SeniorRights] PNU 자동 조회 및 저장: {pnu}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[SeniorRights] PNU 조회 실패: 주소={SelectedProperty.AddressFull}");
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(pnu))
                        {
                            var priceResult = await _vworldService.GetOfficialPriceAsync(pnu, SelectedProperty.PropertyType, addressFull: SelectedProperty.AddressFull);

                            if (priceResult != null)
                            {
                                decimal totalPrice = priceResult.Price;

                                if (priceResult.IsPerSquareMeter)
                                {
                                    // 개별공시지가 (원/㎡) × 대지면적 = 총 공시지가
                                    var landArea = SelectedProperty.LandArea ?? 0;
                                    totalPrice = landArea > 0 ? priceResult.Price * landArea : priceResult.Price;
                                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 개별공시지가: {priceResult.Price:N0}원/㎡ × {landArea:N2}㎡ = {totalPrice:N0}원");
                                }
                                else
                                {
                                    // 공동주택/개별주택 공시가격 (원 단위, 변환 불필요)
                                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] {priceResult.PriceType}: {totalPrice:N0}원");
                                }

                                HousingOfficialPrice = totalPrice;
                                System.Diagnostics.Debug.WriteLine($"[SeniorRights] 공시가격 자동 조회 성공: {priceResult.PriceType} {totalPrice:N0}원 ({priceResult.StandardYear}년)");

                                // 공시지가 자동 산출: 개별공시지가(원/㎡) × 토지면적
                                if (OfficialLandPrice == 0 && priceResult.IsPerSquareMeter)
                                {
                                    // 이미 개별공시지가 결과 → 그대로 사용
                                    OfficialLandPrice = totalPrice;
                                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 공시지가 자동 산출 (개별공시지가): {OfficialLandPrice:N0}원");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[SeniorRights] 공시가격 조회 결과 없음: PNU={pnu}");
                            }

                            // 공시지가: 아파트/주택 등 주택공시가격이 아닌 경우에도 개별공시지가 별도 조회
                            if (OfficialLandPrice == 0)
                            {
                                var landPriceResult = await _vworldService.GetOfficialPriceAsync(pnu, "토지");
                                if (landPriceResult != null && landPriceResult.IsPerSquareMeter)
                                {
                                    var landArea = SelectedProperty.LandArea ?? 0;
                                    OfficialLandPrice = landArea > 0 ? landPriceResult.Price * landArea : landPriceResult.Price;
                                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 공시지가 별도 조회: {landPriceResult.Price:N0}원/㎡ × {landArea:N2}㎡ = {OfficialLandPrice:N0}원");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SeniorRights] 공시가격 자동 조회 실패: {ex.Message}");
                    }
                }

                // 건물기준시가: 기본값 = 건물감정가액 × 70%
                if (BuildingStandardPrice == 0 && SelectedProperty.BuildingAppraisalValue.HasValue && SelectedProperty.BuildingAppraisalValue.Value > 0)
                {
                    BuildingStandardPrice = Math.Round(SelectedProperty.BuildingAppraisalValue.Value * 0.7m, 0);
                    System.Diagnostics.Debug.WriteLine($"[SeniorRights] 건물기준시가 자동 산출: {SelectedProperty.BuildingAppraisalValue.Value:N0} × 70% = {BuildingStandardPrice:N0}원");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"권리분석 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 권리분석 필드 초기화
        /// </summary>
        private void ResetRightAnalysisFields()
        {
            _currentRightAnalysis = null;
            
            // 선순위 항목
            SeniorMortgageDd = 0;
            SeniorMortgageReflected = 0;
            SeniorMortgageReason = "";
            LienDd = 0;
            LienReflected = 0;
            LienReason = "";
            SmallDepositDd = 0;
            SmallDepositReflected = 0;
            SmallDepositReason = "";
            LeaseDepositDd = 0;
            LeaseDepositReflected = 0;
            LeaseDepositReason = "";
            TenantName = "";
            TenantMoveInDate = null;
            TenantClaimSubmitted = false;
            WageClaimDd = 0;
            WageClaimReflected = 0;
            WageClaimReason = "";
            WageClaimSubmitted = false;
            CurrentTaxDd = 0;
            CurrentTax = 0;
            CurrentTaxReason = "";
            SeniorTaxClaimDd = 0;
            SeniorTaxClaimReflected = 0;
            SeniorTaxClaimReason = "";
            ExpectedBidPrice = 0;
            AuctionCost = 0;
            LoanCap = 0;

            // 경매사건 정보
            AuctionStatus = "not_opened";
            PrecedentAuction = false;
            SubsequentAuction = false;
            CourtName = "";
            CaseNumber = "";
            AuctionApplicant = "";
            AuctionStartDate = null;
            ClaimDeadlineDate = null;
            ClaimAmount = 0;
            InitialAppraisalValue = 0;
            InitialAuctionDate = null;
            FinalAuctionRound = "";
            FinalAuctionResult = "";
            WinningBidAmount = 0;
            NextAuctionDate = null;
            NextMinimumBid = 0;
            // 배당요구종기일 경과 여부: DD 날짜 기준 자동 판단
            var deadlineDate = SelectedProperty?.PrecedentClaimDeadline ?? SelectedProperty?.SubsequentClaimDeadline;
            ClaimDeadlinePassed = deadlineDate.HasValue && deadlineDate.Value.Date <= DateTime.Today;
            SurveyReportNote = "";
            AppraisalReportNote = "";
            PartyDetailsImage = null;
            PartyDetailsImageInfo = "";
            CourtCaseSearchImage = null;
            CourtCaseSearchImageInfo = "";
            CourtDateSearchImage = null;
            CourtDateSearchImageInfo = "";
            CourtDocumentDeliveryImage = null;
            CourtDocumentDeliveryImageInfo = "";
            TenantRegistryImage = null;
            TenantRegistryImageInfo = "";
            CommercialLeaseImage = null;
            CommercialLeaseImageInfo = "";
            RightsAnalysisImage = null;
            RightsAnalysisImageInfo = "";
            WageDataImage = null;
            WageDataImageInfo = "";

            // 전입/임차 현황
            AddressMatch = false;
            OwnerRegistered = false;
            HasTenant = false;
            SurveyReportSubmitted = false;
            HasTenantRegistry = false;
            HasCommercialLease = false;
            TenantDateBeforeMortgage = false;
            HasAuctionDocs = false;
            DebtorType = "individual";
            BorrowerTypeDisplay = "";
            HasWageClaim = false;
            WageClaimEstimatedSeizure = 0;
            HasTaxClaim = false;
            HasSeniorTaxClaim = false;
            HousingOfficialPrice = 0;
            OfficialLandPrice = 0;
            BuildingStandardPrice = 0;

            // 위험도 평가
            RiskLevel = "";
            RiskReason = "";
            Recommendations = "";
            IsAnalysisCompleted = false;
            AnalyzedAt = null;
            InitializeEmptyRegistryMortgageRows();

            // 임대차/임금채권 테이블 리셋
            ResidentialLeases.Clear();
            HasResidentialLeaseTable = false;
            CommercialLeases.Clear();
            HasCommercialLeaseTable = false;
            WageClaims.Clear();
            HasWageClaimTable = false;
        }

        private async Task LoadRegistryMortgageRowsAsync(Guid propertyId)
        {
            try
            {
                var eulguRows = await _registryRepository.GetEulguRowsByPropertyIdAsync(propertyId);
                var filteredRows = eulguRows
                    .Where(r =>
                        !string.IsNullOrWhiteSpace(r.Receipt) ||
                        !string.IsNullOrWhiteSpace(r.MortgageHolder) ||
                        r.MaxClaimAmount.HasValue)
                    .ToList();

                var mortgageRows = filteredRows
                    .Where(r => IsMortgagePurpose(r.Purpose))
                    .ToList();

                // OCR 결과에 등기목적 누락/오인식이 있을 수 있어, 근저당 목적이 하나도 없으면 전체 을구를 사용
                var rowsForDisplay = mortgageRows.Count > 0 ? mortgageRows : filteredRows;

                var mergedRows = rowsForDisplay
                    .GroupBy(GetRegistryMergeKey)
                    .Select(g => g.OrderBy(r => r.SortIndex ?? int.MaxValue).First())
                    .OrderBy(r => r.SortIndex ?? int.MaxValue)
                    .ThenBy(r => r.ReceiptDate ?? DateTime.MaxValue)
                    .ThenBy(r => ParseRankOrder(r.RankNo))
                    .Take(RegistryMortgageRowCount)
                    .Select(r => new RegistryMortgageDisplayRow
                    {
                        Receipt = r.Receipt ?? "",
                        MortgageHolder = r.MortgageHolder ?? "",
                        MaxClaimAmount = r.MaxClaimAmount
                    })
                    .ToList();

                while (mergedRows.Count < RegistryMortgageRowCount)
                {
                    mergedRows.Add(new RegistryMortgageDisplayRow());
                }

                RegistryMortgageRows = new ObservableCollection<RegistryMortgageDisplayRow>(mergedRows);
                System.Diagnostics.Debug.WriteLine($"[SeniorRights] 우측 등기부 표 로드: raw={eulguRows.Count}, filtered={filteredRows.Count}, displayed={mergedRows.Count}");
            }
            catch (Exception ex)
            {
                InitializeEmptyRegistryMortgageRows();
                System.Diagnostics.Debug.WriteLine($"[SeniorRights] 우측 등기부 표 로드 실패: {ex.Message}");
            }
        }

        private void InitializeEmptyRegistryMortgageRows()
        {
            RegistryMortgageRows = new ObservableCollection<RegistryMortgageDisplayRow>(
                Enumerable.Range(0, RegistryMortgageRowCount).Select(_ => new RegistryMortgageDisplayRow()));
        }

        private static bool IsMortgagePurpose(string? purpose)
        {
            return !string.IsNullOrWhiteSpace(purpose) && purpose.Contains("근저당", StringComparison.Ordinal);
        }

        private static string NormalizeForMerge(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return value.Replace(" ", "").Replace("\n", "").Replace("\r", "").Trim().ToLowerInvariant();
        }

        private static string GetRegistryMergeKey(RegistryEulguRow row)
        {
            var receiptKey = NormalizeForMerge(row.Receipt);
            var ownerKey = NormalizeForMerge(row.TargetOwner);

            // 접수정보가 비어 있으면 서로 다른 행이 과도하게 병합될 수 있어 행 단위로 분리
            if (string.IsNullOrEmpty(receiptKey))
            {
                return $"row:{row.Id}";
            }

            return $"receipt:{receiptKey}|owner:{ownerKey}";
        }

        private static int ParseRankOrder(string? rankNo)
        {
            if (string.IsNullOrWhiteSpace(rankNo))
            {
                return int.MaxValue;
            }

            var digits = new string(rankNo.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedRightTypeChanged(string value) => _ = LoadRightsAsync();
        partial void OnSelectedStatusChanged(string value) => _ = LoadRightsAsync();
        partial void OnSelectedPropertyChanged(Property? value)
        {
            _ = LoadRightsAsync();
            _ = LoadRightAnalysisAsync();
            _ = LoadNoteAsync();
        }

        partial void OnHasTenantRegistryChanged(bool value)
        {
            if (value) HasCommercialLease = false;
        }

        partial void OnHasCommercialLeaseChanged(bool value)
        {
            if (value) HasTenantRegistry = false;
        }

        /// <summary>
        /// 선택된 권리 변경 시
        /// </summary>
        partial void OnSelectedRightChanged(RegistryRight? value)
        {
            if (value != null)
            {
                EditSection = value.Section;
                EditRightOrder = value.RightOrder ?? 1;
                EditRightHolder = value.RightHolder ?? "";
                EditClaimAmount = value.ClaimAmount ?? 0;
                EditRegistrationDate = value.RegistrationDate;
                EditRegistrationNumber = value.RegistrationNumber ?? "";
                EditRegistrationCause = value.RegistrationCause ?? "";
                EditStatus = value.Status;
                EditNotes = value.Notes ?? "";
                IsEditing = true;
                IsNewRecord = false;
            }
        }

        /// <summary>
        /// 새 권리 추가
        /// </summary>
        [RelayCommand]
        private void NewRight()
        {
            SelectedRight = null;
            EditSection = "갑구";
            EditRightOrder = Rights.Count > 0 ? Rights.Max(r => r.RightOrder ?? 0) + 1 : 1;
            EditRightHolder = "";
            EditClaimAmount = 0;
            EditRegistrationDate = DateTime.Today;
            EditRegistrationNumber = "";
            EditRegistrationCause = "";
            EditStatus = "active";
            EditNotes = "";
            IsEditing = true;
            IsNewRecord = true;
        }

        /// <summary>
        /// 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(EditRightHolder))
            {
                ErrorMessage = "권리자/채권자를 입력하세요.";
                return;
            }

            try
            {
                IsLoading = true;

                if (IsNewRecord)
                {
                    var newRight = new RegistryRight
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = SelectedProperty?.Id,
                        Section = EditSection,
                        RightOrder = EditRightOrder,
                        RightHolder = EditRightHolder,
                        ClaimAmount = EditClaimAmount,
                        RegistrationDate = EditRegistrationDate,
                        RegistrationNumber = EditRegistrationNumber,
                        RegistrationCause = EditRegistrationCause,
                        Status = EditStatus,
                        Notes = EditNotes
                    };

                    await _registryRepository.CreateRightAsync(newRight);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리가 생성되었습니다.");
                }
                else if (SelectedRight != null)
                {
                    SelectedRight.Section = EditSection;
                    SelectedRight.RightOrder = EditRightOrder;
                    SelectedRight.RightHolder = EditRightHolder;
                    SelectedRight.ClaimAmount = EditClaimAmount;
                    SelectedRight.RegistrationDate = EditRegistrationDate;
                    SelectedRight.RegistrationNumber = EditRegistrationNumber;
                    SelectedRight.RegistrationCause = EditRegistrationCause;
                    SelectedRight.Status = EditStatus;
                    SelectedRight.Notes = EditNotes;

                    await _registryRepository.UpdateRightAsync(SelectedRight);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리가 수정되었습니다.");
                }

                await LoadRightsAsync();
                CancelEdit();
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
        /// 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedRight == null) return;

            try
            {
                IsLoading = true;
                await _registryRepository.DeleteRightAsync(SelectedRight.Id);
                await LoadRightsAsync();
                CancelEdit();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리가 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 편집 취소
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            SelectedRight = null;
            IsEditing = false;
            IsNewRecord = false;
        }

        /// <summary>
        /// 검색
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadRightsAsync();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        /// <summary>
        /// etax korea 사이트 열기 (P-005)
        /// </summary>
        [RelayCommand]
        private void OpenEtax()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = EtaxKoreaUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"etax 사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 대법원 경매 사이트 열기 (X-002)
        /// </summary>
        [RelayCommand]
        private void OpenCourtAuction()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = CourtAuctionUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대법원 경매 사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 소액임차보증금 자동 조회 (L-003)
        /// </summary>
        [RelayCommand]
        private async Task LookupSmallDepositAsync()
        {
            if (_referenceDataRepository == null)
            {
                ErrorMessage = "참조 데이터 서비스가 초기화되지 않았습니다.";
                return;
            }

            if (string.IsNullOrEmpty(SelectedSmallDepositRegion))
            {
                ErrorMessage = "지역을 선택해주세요.";
                return;
            }

            if (!MortgageSetupDate.HasValue)
            {
                ErrorMessage = "근저당 설정일을 입력해주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var standard = await _referenceDataRepository.GetSmallDepositAsync(
                    SelectedSmallDepositRegion, 
                    MortgageSetupDate.Value, 
                    "residential");

                if (standard != null)
                {
                    LookedUpSmallDeposit = standard.CompensationAmount;
                    SmallDepositLookupInfo = $"기준: {standard.StartDate:yyyy-MM-dd} ~ {(standard.EndDate?.ToString("yyyy-MM-dd") ?? "현재")}, 보증금한도 {standard.DepositLimit:N0}원 이하";
                    
                    // DD금액에 자동 반영 (사용자가 수정 가능)
                    SmallDepositDd = standard.CompensationAmount;
                    SmallDepositReason = $"자동조회: {SelectedSmallDepositRegion} 지역, {MortgageSetupDate.Value:yyyy-MM-dd} 기준";
                    
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess($"소액임차보증금 기준액: {standard.CompensationAmount:N0}원");
                }
                else
                {
                    LookedUpSmallDeposit = 0;
                    SmallDepositLookupInfo = "해당 조건에 맞는 기준이 없습니다.";
                    ErrorMessage = "해당 지역/기간에 적용되는 소액임차보증금 기준을 찾을 수 없습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"소액임차보증금 조회 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 조회된 소액임차보증금을 반영금액에 적용 (L-003)
        /// </summary>
        [RelayCommand]
        private void ApplySmallDeposit()
        {
            if (LookedUpSmallDeposit > 0)
            {
                SmallDepositReflected = LookedUpSmallDeposit;
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("소액임차보증금이 반영금액에 적용되었습니다.");
            }
            else
            {
                ErrorMessage = "먼저 소액임차보증금을 조회해주세요.";
            }
        }

        /// <summary>
        /// 원청 매트릭스 기반 자동 판단 (반영금액 + 상세추정 근거 자동 채움)
        /// </summary>
        [RelayCommand]
        private void AutoJudge()
        {
            if (SelectedProperty == null)
            {
                ErrorMessage = "물건을 선택하세요.";
                return;
            }

            try
            {
                // 1. ViewModel → RightAnalysis 모델로 현재 조건 값 복사
                var analysis = _currentRightAnalysis ?? new RightAnalysis
                {
                    Id = Guid.NewGuid(),
                    PropertyId = SelectedProperty.Id
                };

                // DD 금액
                analysis.SeniorMortgageDd = SeniorMortgageDd;
                analysis.LienDd = LienDd;
                analysis.SmallDepositDd = SmallDepositDd;
                analysis.LeaseDepositDd = LeaseDepositDd;
                analysis.WageClaimDd = WageClaimDd;
                analysis.CurrentTaxDd = CurrentTaxDd;
                analysis.SeniorTaxDd = SeniorTaxClaimDd;

                // 판단 조건
                analysis.AuctionStatus = AuctionStatus;
                analysis.ClaimDeadlinePassed = ClaimDeadlinePassed;
                analysis.SurveyReportSubmitted = SurveyReportSubmitted;
                analysis.HasTenant = HasTenant;
                analysis.TenantClaimSubmitted = TenantClaimSubmitted;
                analysis.TenantDateBeforeMortgage = TenantDateBeforeMortgage;
                analysis.HasTenantRegistry = HasTenantRegistry;
                analysis.HasCommercialLease = HasCommercialLease;
                analysis.AddressMatch = AddressMatch;
                analysis.DebtorType = DebtorType;
                analysis.HasWageClaim = HasWageClaim;
                analysis.WageClaimSubmitted = WageClaimSubmitted;
                analysis.WageClaimEstimatedSeizure = WageClaimEstimatedSeizure;
                analysis.HasTaxClaim = HasTaxClaim;
                analysis.HasSeniorTaxClaim = HasSeniorTaxClaim;
                analysis.HousingOfficialPrice = HousingOfficialPrice;
                analysis.InitialAppraisalValue = InitialAppraisalValue;

                // 2. RuleEngine 호출
                var ruleEngine = new RightAnalysisRuleEngine();
                ruleEngine.ApplyRules(analysis, SelectedProperty);

                // 3. RightAnalysis → ViewModel로 결과 복사 (반영금액 + Reason)
                SeniorMortgageReflected = analysis.SeniorMortgageReflected;
                SeniorMortgageReason = analysis.SeniorMortgageReason ?? "";

                LienReflected = analysis.LienReflected;
                LienReason = analysis.LienReason ?? "";

                SmallDepositReflected = analysis.SmallDepositReflected;
                SmallDepositReason = analysis.SmallDepositReason ?? "";

                LeaseDepositReflected = analysis.LeaseDepositReflected;
                LeaseDepositReason = analysis.LeaseDepositReason ?? "";

                WageClaimReflected = analysis.WageClaimReflected;
                WageClaimReason = analysis.WageClaimReason ?? "";

                CurrentTax = analysis.CurrentTaxReflected;
                CurrentTaxReason = analysis.CurrentTaxReason ?? "";

                SeniorTaxClaimReflected = analysis.SeniorTaxReflected;
                SeniorTaxClaimReason = analysis.SeniorTaxReason ?? "";

                // 4. 합계는 [NotifyPropertyChangedFor] 로 자동 갱신됨

                _currentRightAnalysis = analysis;
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("자동 판단이 완료되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"자동 판단 실패: {ex.Message}";
                Debug.WriteLine($"[SeniorRights] AutoJudge error: {ex}");
            }
        }

        /// <summary>
        /// 선순위 분석 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveRightAnalysisAsync()
        {
            if (SelectedProperty == null)
            {
                ErrorMessage = "물건을 선택하세요.";
                return;
            }

            try
            {
                IsLoading = true;

                var analysis = _currentRightAnalysis ?? new RightAnalysis
                {
                    Id = Guid.NewGuid(),
                    PropertyId = SelectedProperty.Id
                };

                // 선순위근저당권
                analysis.SeniorMortgageDd = SeniorMortgageDd;
                analysis.SeniorMortgageReflected = SeniorMortgageReflected;
                analysis.SeniorMortgageReason = SeniorMortgageReason;

                // P-002: 유치권
                analysis.LienDd = LienDd;
                analysis.LienReflected = LienReflected;
                analysis.LienReason = LienReason;

                // P-003: 소액보증금
                analysis.SmallDepositDd = SmallDepositDd;
                analysis.SmallDepositReflected = SmallDepositReflected;
                analysis.SmallDepositReason = SmallDepositReason;

                // P-007: 주택임대차
                analysis.LeaseDepositDd = LeaseDepositDd;
                analysis.LeaseDepositReflected = LeaseDepositReflected;
                analysis.LeaseDepositReason = LeaseDepositReason;
                analysis.TenantName = TenantName;
                analysis.TenantMoveInDate = TenantMoveInDate;
                analysis.TenantClaimSubmitted = TenantClaimSubmitted;

                // P-007: 임금채권
                analysis.WageClaimDd = WageClaimDd;
                analysis.WageClaimReflected = WageClaimReflected;
                analysis.WageClaimReason = WageClaimReason;
                analysis.WageClaimSubmitted = WageClaimSubmitted;

                // 당해세
                analysis.CurrentTaxDd = CurrentTaxDd;
                analysis.CurrentTaxReflected = CurrentTax;
                analysis.CurrentTaxReason = CurrentTaxReason;

                // 선순위 조세채권
                analysis.SeniorTaxDd = SeniorTaxClaimDd;
                analysis.SeniorTaxReflected = SeniorTaxClaimReflected;
                analysis.SeniorTaxReason = SeniorTaxClaimReason;

                // 합계 섹션
                analysis.ExpectedWinningBid = ExpectedBidPrice;
                analysis.AuctionFees = AuctionCost;
                analysis.LoanCap = LoanCap;
                analysis.SeniorRightsTotal = SeniorRightsTotal;
                analysis.DistributableAmount = DistributableAmount;
                analysis.AmountAfterSenior = RemainingAmount;
                analysis.CapAppliedDividend = CapAppliedDividend;
                analysis.RecoveryRate = RecoveryRate;

                // 경매사건 정보
                analysis.AuctionStatus = AuctionStatus;
                analysis.PrecedentAuction = PrecedentAuction;
                analysis.SubsequentAuction = SubsequentAuction;
                analysis.CourtName = CourtName;
                analysis.CaseNumber = CaseNumber;
                analysis.AuctionApplicant = AuctionApplicant;
                analysis.AuctionStartDate = AuctionStartDate;
                analysis.ClaimDeadlineDate = ClaimDeadlineDate;
                analysis.ClaimAmount = ClaimAmount;
                analysis.InitialAppraisalValue = InitialAppraisalValue;
                analysis.InitialAuctionDate = InitialAuctionDate;
                analysis.FinalAuctionRound = int.TryParse(FinalAuctionRound, out var round) ? round : null;
                analysis.FinalAuctionResult = FinalAuctionResult;
                analysis.WinningBidAmount = WinningBidAmount;
                analysis.NextAuctionDate = NextAuctionDate;
                analysis.NextMinimumBid = NextMinimumBid;
                analysis.ClaimDeadlinePassed = ClaimDeadlinePassed;
                analysis.SurveyReportNote = SurveyReportNote;
                analysis.AppraisalReportNote = AppraisalReportNote;
                analysis.PartyDetailsImageBase64 = ConvertImageToBase64(PartyDetailsImage);
                analysis.CourtCaseSearchImage = ConvertImageToBase64(CourtCaseSearchImage);
                analysis.CourtDateSearchImage = ConvertImageToBase64(CourtDateSearchImage);
                analysis.CourtDocumentDeliveryImage = ConvertImageToBase64(CourtDocumentDeliveryImage);
                analysis.TenantRegistryImage = ConvertImageToBase64(TenantRegistryImage);
                analysis.CommercialLeaseImage = ConvertImageToBase64(CommercialLeaseImage);
                analysis.RightsAnalysisImage = ConvertImageToBase64(RightsAnalysisImage);
                analysis.WageDataImage = ConvertImageToBase64(WageDataImage);

                // 전입/임차 현황
                analysis.AddressMatch = AddressMatch;
                analysis.OwnerRegistered = OwnerRegistered;
                analysis.HasTenant = HasTenant;
                analysis.SurveyReportSubmitted = SurveyReportSubmitted;
                analysis.HasTenantRegistry = HasTenantRegistry;
                analysis.HasCommercialLease = HasCommercialLease;
                analysis.TenantDateBeforeMortgage = TenantDateBeforeMortgage;
                analysis.HasAuctionDocs = HasAuctionDocs;
                analysis.DebtorType = DebtorType;
                analysis.HasWageClaim = HasWageClaim;
                analysis.WageClaimEstimatedSeizure = WageClaimEstimatedSeizure;
                analysis.HasTaxClaim = HasTaxClaim;
                analysis.HasSeniorTaxClaim = HasSeniorTaxClaim;
                analysis.HousingOfficialPrice = HousingOfficialPrice;
                analysis.OfficialLandPrice = OfficialLandPrice;
                analysis.BuildingStandardPrice = BuildingStandardPrice;

                // 위험도 평가
                analysis.RiskLevel = string.IsNullOrWhiteSpace(RiskLevel) ? null : RiskLevel;
                analysis.RiskReason = RiskReason;
                analysis.Recommendations = Recommendations;
                analysis.IsCompleted = IsAnalysisCompleted;
                if (IsAnalysisCompleted)
                {
                    analysis.AnalyzedAt = DateTime.UtcNow;
                    AnalyzedAt = analysis.AnalyzedAt;
                }

                _currentRightAnalysis = await _rightAnalysisRepository.UpsertAsync(analysis);
                if (!_isBulkSaving) NPLogic.UI.Services.ToastService.Instance.ShowSuccess("선순위 분석이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"선순위 분석 저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// P-007: 주택임대차 패널 토글
        /// </summary>
        [RelayCommand]
        private void ToggleResidentialLease()
        {
            IsResidentialLeaseExpanded = !IsResidentialLeaseExpanded;
        }

        /// <summary>
        /// P-007: 상가임대차 패널 토글
        /// </summary>
        [RelayCommand]
        private void ToggleCommercialLease()
        {
            IsCommercialLeaseExpanded = !IsCommercialLeaseExpanded;
        }

        /// <summary>
        /// P-007: 임금채권 패널 토글
        /// </summary>
        [RelayCommand]
        private void ToggleWageClaim()
        {
            IsWageClaimExpanded = !IsWageClaimExpanded;
        }

        /// <summary>
        /// 합계 속성 갱신
        /// </summary>
        private void UpdateSummaryProperties()
        {
            OnPropertyChanged(nameof(SeniorMortgagesTotal));
            OnPropertyChanged(nameof(SeniorRightsTotal));
            OnPropertyChanged(nameof(RemainingAmount));
            OnPropertyChanged(nameof(AmountAfterSenior));
            OnPropertyChanged(nameof(CapAppliedDividend));
            OnPropertyChanged(nameof(RecoveryRate));
        }

        /// <summary>
        /// 위험도 자동 평가
        /// </summary>
        [RelayCommand]
        private void EvaluateRisk()
        {
            var recoveryRate = RecoveryRate;
            
            if (recoveryRate >= 80)
            {
                RiskLevel = "low";
                RiskReason = $"예상 회수율 {recoveryRate:N1}%로 양호한 수준입니다.";
            }
            else if (recoveryRate >= 50)
            {
                RiskLevel = "medium";
                RiskReason = $"예상 회수율 {recoveryRate:N1}%로 주의가 필요합니다.";
            }
            else
            {
                RiskLevel = "high";
                RiskReason = $"예상 회수율 {recoveryRate:N1}%로 손실 위험이 높습니다.";
            }
            
            // 추가 위험 요인 체크
            if (SeniorRightsTotal > ExpectedBidPrice * 0.5m)
            {
                RiskReason += " 선순위 금액이 낙찰가의 50%를 초과합니다.";
            }

            NPLogic.UI.Services.ToastService.Instance.ShowSuccess("위험도 평가가 완료되었습니다.");
        }

        /// <summary>
        /// 경매사건검색 열기
        /// </summary>
        [RelayCommand]
        private void OpenAuctionSearch()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.courtauction.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"경매사건검색 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 주택공시가격 조회
        /// </summary>
        [RelayCommand]
        private void OpenHousingPrice()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.realtyprice.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"주택공시가격 조회 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 공시지가 조회
        /// </summary>
        [RelayCommand]
        private void OpenLandPrice()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://kras.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"공시지가 조회 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 자동 계산 - 등기부등본 데이터 기반 선순위 자동 추출
        /// </summary>
        [RelayCommand]
        private async Task AutoCalculateAsync()
        {
            if (SelectedProperty == null)
            {
                ErrorMessage = "물건을 선택하세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 1. 등기부등본에서 권리 데이터 다시 로드
                await LoadRightsAsync();

                // 2. 선순위 근저당 자동 추출 (을구에서 근저당권)
                LoadSeniorMortgages();

                // 3. 소액보증금 자동 조회 (지역, 근저당설정일 기준)
                if (_referenceDataRepository != null && 
                    !string.IsNullOrEmpty(SelectedSmallDepositRegion) && 
                    MortgageSetupDate.HasValue)
                {
                    var standard = await _referenceDataRepository.GetSmallDepositAsync(
                        SelectedSmallDepositRegion, 
                        MortgageSetupDate.Value, 
                        "residential");
                    
                    if (standard != null)
                    {
                        SmallDepositDd = standard.CompensationAmount;
                        SmallDepositReason = $"자동계산: {SelectedSmallDepositRegion} 지역, {MortgageSetupDate.Value:yyyy-MM-dd} 기준";
                        SmallDepositLookupInfo = $"기준: {standard.StartDate:yyyy-MM-dd} ~ {(standard.EndDate?.ToString("yyyy-MM-dd") ?? "현재")}";
                    }
                }

                // 4. 합계 갱신
                UpdateSummaryProperties();

                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("자동 계산이 완료되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"자동 계산 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ========== Tool Box 테이블 생성/행 추가/삭제 Commands ==========

        [RelayCommand]
        private void CreateResidentialLeaseTable()
        {
            ErrorMessage = null;
            HasResidentialLeaseTable = true;
            if (ResidentialLeases.Count == 0)
            {
                AddResidentialLeaseRow();
            }
        }

        [RelayCommand]
        private void CreateCommercialLeaseTable()
        {
            ErrorMessage = null;
            HasCommercialLeaseTable = true;
            if (CommercialLeases.Count == 0)
            {
                AddCommercialLeaseRow();
            }
        }

        [RelayCommand]
        private void CreateWageClaimTable()
        {
            ErrorMessage = null;
            HasWageClaimTable = true;
            if (WageClaims.Count == 0)
            {
                AddWageClaimRow();
            }
        }
        
        [RelayCommand]
        private void DestroyResidentialLeaseTable()
        {
            HasResidentialLeaseTable = false;
            ResidentialLeases.Clear();
        }

        [RelayCommand]
        private void DestroyCommercialLeaseTable()
        {
            HasCommercialLeaseTable = false;
            CommercialLeases.Clear();
        }

        [RelayCommand]
        private void DestroyWageClaimTable()
        {
            HasWageClaimTable = false;
            WageClaims.Clear();
        }

        /// <summary>
        /// 주택임대차 행 추가
        /// </summary>
        [RelayCommand]
        private void AddResidentialLeaseRow()
        {
            ErrorMessage = null;
            HasResidentialLeaseTable = true;
            ResidentialLeases.Add(new LeaseItem
            {
                PropertyId = SelectedProperty?.Id,
                LeaseType = "residential"
            });
            OnPropertyChanged(nameof(ResidentialLeasesTotalDeposit));
        }

        [RelayCommand]
        private void RemoveResidentialLeaseRow(LeaseItem? row)
        {
            if (row == null)
            {
                ErrorMessage = null;
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("삭제할 행을 선택하세요.");
                return;
            }

            ErrorMessage = null;
            ResidentialLeases.Remove(row);
            OnPropertyChanged(nameof(ResidentialLeasesTotalDeposit));
        }

        /// <summary>
        /// 상가임대차 행 추가
        /// </summary>
        [RelayCommand]
        private void AddCommercialLeaseRow()
        {
            ErrorMessage = null;
            HasCommercialLeaseTable = true;
            CommercialLeases.Add(new LeaseItem
            {
                PropertyId = SelectedProperty?.Id,
                LeaseType = "commercial"
            });
            OnPropertyChanged(nameof(CommercialLeasesTotalDeposit));
        }

        [RelayCommand]
        private void RemoveCommercialLeaseRow(LeaseItem? row)
        {
            if (row == null)
            {
                ErrorMessage = null;
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("삭제할 행을 선택하세요.");
                return;
            }

            ErrorMessage = null;
            CommercialLeases.Remove(row);
            OnPropertyChanged(nameof(CommercialLeasesTotalDeposit));
        }

        /// <summary>
        /// 임금채권 행 추가
        /// </summary>
        [RelayCommand]
        private void AddWageClaimRow()
        {
            ErrorMessage = null;
            HasWageClaimTable = true;
            var sequenceNumber = WageClaims.Count > 0 ? WageClaims.Max(w => w.SequenceNumber) + 1 : 1;
            WageClaims.Add(new WageClaimItem
            {
                PropertyId = SelectedProperty?.Id,
                SequenceNumber = sequenceNumber
            });
            OnPropertyChanged(nameof(WageClaimsTotalAmount));
        }

        [RelayCommand]
        private void RemoveWageClaimRow(WageClaimItem? row)
        {
            if (row == null)
            {
                ErrorMessage = null;
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("삭제할 행을 선택하세요.");
                return;
            }

            ErrorMessage = null;
            WageClaims.Remove(row);
            OnPropertyChanged(nameof(WageClaimsTotalAmount));
        }

        // ========== 열람 자료 이미지 붙여넣기 Commands ==========

        /// <summary>
        /// 전입세대열람 이미지 붙여넣기
        /// </summary>
        [RelayCommand]
        private void PasteTenantRegistryImage()
        {
            if (Clipboard.ContainsImage())
            {
                TenantRegistryImage = Clipboard.GetImage();
                TenantRegistryImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("전입세대열람 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                ErrorMessage = "클립보드에 이미지가 없습니다.";
            }
        }

        /// <summary>
        /// 상가임대차열람 이미지 붙여넣기
        /// </summary>
        [RelayCommand]
        private void PasteCommercialLeaseImage()
        {
            if (Clipboard.ContainsImage())
            {
                CommercialLeaseImage = Clipboard.GetImage();
                CommercialLeaseImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("상가임대차열람 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                ErrorMessage = "클립보드에 이미지가 없습니다.";
            }
        }

        /// <summary>
        /// 권리분석 이미지 붙여넣기
        /// </summary>
        [RelayCommand]
        private void PasteRightsAnalysisImage()
        {
            if (Clipboard.ContainsImage())
            {
                RightsAnalysisImage = Clipboard.GetImage();
                RightsAnalysisImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리분석 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                ErrorMessage = "클립보드에 이미지가 없습니다.";
            }
        }

        /// <summary>
        /// 임금자료 이미지 붙여넣기
        /// </summary>
        [RelayCommand]
        private void PasteWageDataImage()
        {
            if (Clipboard.ContainsImage())
            {
                WageDataImage = Clipboard.GetImage();
                WageDataImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("임금자료 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                ErrorMessage = "클립보드에 이미지가 없습니다.";
            }
        }

        /// <summary>
        /// 당사자내역 이미지 붙여넣기
        /// </summary>
        [RelayCommand]
        private void PastePartyDetailsImage()
        {
            if (Clipboard.ContainsImage())
            {
                PartyDetailsImage = Clipboard.GetImage();
                PartyDetailsImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("당사자내역 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("클립보드에 이미지가 없습니다.");
            }
        }

        [RelayCommand]
        private void PasteCourtCaseSearchImage()
        {
            if (Clipboard.ContainsImage())
            {
                CourtCaseSearchImage = Clipboard.GetImage();
                CourtCaseSearchImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경매사건검색 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("클립보드에 이미지가 없습니다.");
            }
        }

        [RelayCommand]
        private void PasteCourtDateSearchImage()
        {
            if (Clipboard.ContainsImage())
            {
                CourtDateSearchImage = Clipboard.GetImage();
                CourtDateSearchImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("기일내역검색 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("클립보드에 이미지가 없습니다.");
            }
        }

        [RelayCommand]
        private void PasteCourtDocumentDeliveryImage()
        {
            if (Clipboard.ContainsImage())
            {
                CourtDocumentDeliveryImage = Clipboard.GetImage();
                CourtDocumentDeliveryImageInfo = $"이미지 붙여넣기됨 ({DateTime.Now:HH:mm:ss})";
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("문건송달내역 이미지가 붙여넣기되었습니다.");
            }
            else
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("클립보드에 이미지가 없습니다.");
            }
        }

        /// <summary>
        /// 대법원 경매사건 검색 사이트 열기
        /// </summary>
        [RelayCommand]
        private void OpenCourtAuctionSearch()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.courtauction.go.kr/pgj/index.on?w2xPath=/pgj/ui/pgj100/PGJ159M00.xml") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"사이트 열기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// BitmapSource → Base64 변환
        /// </summary>
        private static string? ConvertImageToBase64(BitmapSource? image)
        {
            if (image == null) return null;
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using var ms = new System.IO.MemoryStream();
            encoder.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Base64 → BitmapSource 변환 + 프로퍼티 설정
        /// </summary>
        private void LoadPartyDetailsImageFromBase64(string? base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                PartyDetailsImage = null;
                PartyDetailsImageInfo = "";
                return;
            }
            try
            {
                var bytes = Convert.FromBase64String(base64);
                using var ms = new System.IO.MemoryStream(bytes);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                PartyDetailsImage = bitmap;
                PartyDetailsImageInfo = "DB에서 로드됨";
            }
            catch
            {
                PartyDetailsImage = null;
                PartyDetailsImageInfo = "";
            }
        }

        /// <summary>
        /// Base64 → BitmapSource 범용 변환
        /// </summary>
        private void LoadImageFromBase64(string? base64, Action<BitmapSource?> setImage, Action<string> setInfo)
        {
            if (string.IsNullOrEmpty(base64))
            {
                setImage(null);
                setInfo("");
                return;
            }
            try
            {
                var bytes = Convert.FromBase64String(base64);
                using var ms = new System.IO.MemoryStream(bytes);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                setImage(bitmap);
                setInfo("DB에서 로드됨");
            }
            catch
            {
                setImage(null);
                setInfo("");
            }
        }

        /// <summary>
        /// 경매사건 정보 저장 (당사자내역 이미지, 현황조사서, 감정평가서 메모, 대법원 경매 이미지)
        /// </summary>
        [RelayCommand]
        private async Task SaveAuctionCaseNotesAsync()
        {
            if (SelectedProperty == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("물건을 선택하세요.");
                return;
            }

            try
            {
                if (_currentRightAnalysis == null)
                {
                    _currentRightAnalysis = new RightAnalysis
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = SelectedProperty.Id,
                    };
                }

                _currentRightAnalysis.SurveyReportNote = SurveyReportNote;
                _currentRightAnalysis.AppraisalReportNote = AppraisalReportNote;
                _currentRightAnalysis.PartyDetailsImageBase64 = ConvertImageToBase64(PartyDetailsImage);
                _currentRightAnalysis.ClaimDeadlinePassed = ClaimDeadlinePassed;
                _currentRightAnalysis.CourtCaseSearchImage = ConvertImageToBase64(CourtCaseSearchImage);
                _currentRightAnalysis.CourtDateSearchImage = ConvertImageToBase64(CourtDateSearchImage);
                _currentRightAnalysis.CourtDocumentDeliveryImage = ConvertImageToBase64(CourtDocumentDeliveryImage);

                await _rightAnalysisRepository.UpsertAsync(_currentRightAnalysis);
                if (!_isBulkSaving) NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경매사건 정보가 저장되었습니다.");
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"저장 실패: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveTenantInfoAsync()
        {
            if (SelectedProperty == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("물건을 선택하세요.");
                return;
            }

            try
            {
                if (_currentRightAnalysis == null)
                {
                    _currentRightAnalysis = new RightAnalysis
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = SelectedProperty.Id,
                    };
                }

                // 전입/임차 현황 필드
                _currentRightAnalysis.AddressMatch = AddressMatch;
                _currentRightAnalysis.HasTenant = HasTenant;
                _currentRightAnalysis.TenantName = TenantName;
                _currentRightAnalysis.TenantMoveInDate = TenantMoveInDate;
                _currentRightAnalysis.OwnerRegistered = OwnerRegistered;
                _currentRightAnalysis.HasAuctionDocs = HasAuctionDocs;
                _currentRightAnalysis.HasTenantRegistry = HasTenantRegistry;
                _currentRightAnalysis.HasCommercialLease = HasCommercialLease;
                _currentRightAnalysis.TenantClaimSubmitted = TenantClaimSubmitted;
                _currentRightAnalysis.HousingOfficialPrice = HousingOfficialPrice;
                _currentRightAnalysis.HasWageClaim = HasWageClaim;
                _currentRightAnalysis.WageClaimEstimatedSeizure = WageClaimEstimatedSeizure;
                _currentRightAnalysis.WageClaimSubmitted = WageClaimSubmitted;
                _currentRightAnalysis.HasTaxClaim = HasTaxClaim;
                _currentRightAnalysis.HasSeniorTaxClaim = HasSeniorTaxClaim;
                _currentRightAnalysis.OfficialLandPrice = OfficialLandPrice;
                _currentRightAnalysis.BuildingStandardPrice = BuildingStandardPrice;
                _currentRightAnalysis.TenantRegistryImage = ConvertImageToBase64(TenantRegistryImage);
                _currentRightAnalysis.CommercialLeaseImage = ConvertImageToBase64(CommercialLeaseImage);
                _currentRightAnalysis.RightsAnalysisImage = ConvertImageToBase64(RightsAnalysisImage);
                _currentRightAnalysis.WageDataImage = ConvertImageToBase64(WageDataImage);

                await _rightAnalysisRepository.UpsertAsync(_currentRightAnalysis);
                if (!_isBulkSaving) NPLogic.UI.Services.ToastService.Instance.ShowSuccess("전입/임차 현황이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"저장 실패: {ex.Message}");
            }
        }

        // ========== 임대차/임금채권 저장 Commands ==========

        [RelayCommand]
        private async Task SaveResidentialLeasesAsync()
        {
            if (SelectedProperty == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("물건을 선택하세요.");
                return;
            }
            if (_leaseItemRepository == null) return;

            try
            {
                await _leaseItemRepository.SaveAllAsync(
                    SelectedProperty.Id, "residential", ResidentialLeases.ToList());
                OnPropertyChanged(nameof(ResidentialLeasesTotalDeposit));
                if (!_isBulkSaving) NPLogic.UI.Services.ToastService.Instance.ShowSuccess("주택임대차가 저장되었습니다.");
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"저장 실패: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveCommercialLeasesAsync()
        {
            if (SelectedProperty == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("물건을 선택하세요.");
                return;
            }
            if (_leaseItemRepository == null) return;

            try
            {
                await _leaseItemRepository.SaveAllAsync(
                    SelectedProperty.Id, "commercial", CommercialLeases.ToList());
                OnPropertyChanged(nameof(CommercialLeasesTotalDeposit));
                if (!_isBulkSaving) NPLogic.UI.Services.ToastService.Instance.ShowSuccess("상가임대차가 저장되었습니다.");
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"저장 실패: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveWageClaimsAsync()
        {
            if (SelectedProperty == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("물건을 선택하세요.");
                return;
            }
            if (_wageClaimItemRepository == null) return;

            try
            {
                await _wageClaimItemRepository.SaveAllAsync(
                    SelectedProperty.Id, WageClaims.ToList());
                OnPropertyChanged(nameof(WageClaimsTotalAmount));
                if (!_isBulkSaving) NPLogic.UI.Services.ToastService.Instance.ShowSuccess("임금채권이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"저장 실패: {ex.Message}");
            }
        }

        // ========== 일괄 저장 Command ==========

        private bool _isBulkSaving;

        [RelayCommand]
        private async Task SaveAllAsync()
        {
            if (SelectedProperty == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("물건을 선택하세요.");
                return;
            }

            try
            {
                _isBulkSaving = true;
                await SaveAuctionCaseNotesAsync();
                await SaveTenantInfoAsync();
                await SaveRightAnalysisAsync();
                await SaveResidentialLeasesAsync();
                await SaveCommercialLeasesAsync();
                await SaveWageClaimsAsync();
                await SaveNoteAsync();
                _isBulkSaving = false;

                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("선순위 데이터가 일괄 저장되었습니다.");
            }
            catch (Exception ex)
            {
                _isBulkSaving = false;
                NPLogic.UI.Services.ToastService.Instance.ShowError($"일괄 저장 실패: {ex.Message}");
            }
        }

        // ========== 링크 버튼 Commands ==========

        /// <summary>
        /// 당사자내역 팝업
        /// </summary>
        [RelayCommand]
        private void OpenPartyDetails()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.courtauction.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"당사자내역 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 기일내역검색
        /// </summary>
        [RelayCommand]
        private void OpenScheduleSearch()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.courtauction.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"기일내역검색 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 문건송달내역
        /// </summary>
        [RelayCommand]
        private void OpenDocumentDelivery()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.courtauction.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"문건송달내역 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            NPLogic.UI.Services.ToastService.Instance.ShowSuccess("Excel 내보내기 기능은 준비 중입니다.");
            await Task.CompletedTask;
        }
    }
}
