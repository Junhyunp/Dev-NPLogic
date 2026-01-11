using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
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
        
        // 현재 물건의 권리분석 데이터
        private RightAnalysis? _currentRightAnalysis;

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
        private decimal _lienDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        private decimal _lienReflected;

        [ObservableProperty]
        private string _lienReason = "";

        // ========== P-003: 선순위 소액보증금 ==========
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        private decimal _smallDepositDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
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
        private decimal _leaseDepositDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
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
        private decimal _wageClaimDd;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
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

        /// <summary>
        /// 롱캡 (Long Cap) - 원금+이자+연체이자
        /// </summary>
        [ObservableProperty]
        private decimal _longCap;

        // ========== 당해세 처리 (P-005, P-006) ==========
        /// <summary>
        /// 당해세 금액 (사용자 입력)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SeniorRightsTotal))]
        [NotifyPropertyChangedFor(nameof(RemainingAmount))]
        private decimal _currentTax;

        /// <summary>
        /// etax korea URL
        /// </summary>
        private const string EtaxKoreaUrl = "https://www.etax.go.kr";

        /// <summary>
        /// 대법원 경매 사이트 URL (X-002)
        /// </summary>
        private const string CourtAuctionUrl = "https://www.courtauction.go.kr";

        // ========== 편집 ==========
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isNewRecord;

        // 편집 필드
        [ObservableProperty]
        private string _editRightType = "gap";

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

        public SeniorRightsViewModel(
            RegistryRepository registryRepository, 
            PropertyRepository propertyRepository,
            RightAnalysisRepository rightAnalysisRepository,
            ReferenceDataRepository? referenceDataRepository = null)
        {
            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _rightAnalysisRepository = rightAnalysisRepository ?? throw new ArgumentNullException(nameof(rightAnalysisRepository));
            _referenceDataRepository = referenceDataRepository;
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

                await LoadPropertiesAsync();
                await LoadRightsAsync();
                await LoadRightAnalysisAsync();
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
                    var typeFilter = SelectedRightType == "갑구" ? "gap" : "eul";
                    rights = rights.Where(r => r.RightType == typeFilter).ToList();
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
                .Where(r => r.RightType == "eul" && 
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

                    // 당해세 (기존)
                    CurrentTax = _currentRightAnalysis.CurrentTaxReflected;

                    // 합계 섹션
                    ExpectedBidPrice = _currentRightAnalysis.ExpectedWinningBid ?? 0;
                    AuctionCost = _currentRightAnalysis.AuctionFees ?? 0;
                    LongCap = _currentRightAnalysis.LoanCap ?? 0;
                }
                else
                {
                    ResetRightAnalysisFields();
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
            CurrentTax = 0;
            ExpectedBidPrice = 0;
            AuctionCost = 0;
            LongCap = 0;
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
        }

        /// <summary>
        /// 선택된 권리 변경 시
        /// </summary>
        partial void OnSelectedRightChanged(RegistryRight? value)
        {
            if (value != null)
            {
                EditRightType = value.RightType;
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
            EditRightType = "gap";
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
                        RightType = EditRightType,
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
                    SelectedRight.RightType = EditRightType;
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
                analysis.CurrentTaxReflected = CurrentTax;

                // 합계 섹션
                analysis.ExpectedWinningBid = ExpectedBidPrice;
                analysis.AuctionFees = AuctionCost;
                analysis.LoanCap = LongCap;
                analysis.SeniorRightsTotal = SeniorRightsTotal;
                analysis.DistributableAmount = DistributableAmount;
                analysis.AmountAfterSenior = RemainingAmount;

                _currentRightAnalysis = await _rightAnalysisRepository.UpsertAsync(analysis);
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("선순위 분석이 저장되었습니다.");
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
        }
    }
}

