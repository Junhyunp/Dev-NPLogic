using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 차주개요 ViewModel
    /// </summary>
    public partial class BorrowerOverviewViewModel : ObservableObject
    {
        private readonly BorrowerRepository _borrowerRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly PropertyQaRepository _qaRepository;
        private readonly AuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<Borrower> _borrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        [ObservableProperty]
        private ObservableCollection<Property> _borrowerProperties = new();

        // ========== QA 관련 (Q-001 ~ Q-003) ==========
        [ObservableProperty]
        private ObservableCollection<PropertyQa> _borrowerQas = new();

        [ObservableProperty]
        private string _newQuestion = "";

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string? _selectedProgramId;

        [ObservableProperty]
        private string? _selectedBorrowerType;

        [ObservableProperty]
        private bool _showRestructuringOnly;

        [ObservableProperty]
        private ObservableCollection<string> _programs = new();

        [ObservableProperty]
        private ObservableCollection<string> _borrowerTypes = new();

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 30;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _totalPages;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isAddModalOpen;

        [ObservableProperty]
        private bool _isConfirmDeleteModalOpen;

        [ObservableProperty]
        private Borrower? _borrowerToDelete;

        [ObservableProperty]
        private Borrower _newBorrower = new();

        // 통계
        [ObservableProperty]
        private BorrowerStatistics? _statistics;

        // ========== 선택된 물건 모드 (대시보드에서 진입 시) ==========
        /// <summary>
        /// 선택된 물건 (대시보드에서 진입 시 설정됨)
        /// </summary>
        [ObservableProperty]
        private Property? _selectedProperty;

        /// <summary>
        /// 단일 차주 모드 여부 (선택된 물건이 있는 경우 true)
        /// </summary>
        public bool IsSingleBorrowerMode => SelectedProperty != null;

        // ========== 담보 총괄 계산 속성 (S-004, S-005) ==========
        [ObservableProperty]
        private string _collateralTypeSummary = "-";

        [ObservableProperty]
        private decimal _totalLandArea;

        [ObservableProperty]
        private decimal _totalBuildingArea;

        [ObservableProperty]
        private decimal _totalMachineryValue;

        [ObservableProperty]
        private bool _hasFactoryMortgage;

        [ObservableProperty]
        private bool _isInIndustrialComplex;

        public BorrowerOverviewViewModel(
            BorrowerRepository borrowerRepository,
            PropertyRepository propertyRepository,
            PropertyQaRepository qaRepository,
            AuthService authService)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _qaRepository = qaRepository ?? throw new ArgumentNullException(nameof(qaRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            InitializeFilterOptions();
        }

        /// <summary>
        /// 필터 옵션 초기화
        /// </summary>
        private void InitializeFilterOptions()
        {
            // 차주 유형
            BorrowerTypes.Add("전체");
            BorrowerTypes.Add("개인");
            BorrowerTypes.Add("개인사업자");
            BorrowerTypes.Add("법인");

            SelectedBorrowerType = BorrowerTypes[0];
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

                // 단일 차주 모드인 경우 (선택된 물건이 있는 경우)
                if (IsSingleBorrowerMode && SelectedProperty != null)
                {
                    await LoadSingleBorrowerDataAsync();
                    return;
                }

                // 프로그램 목록 로드
                await LoadProgramsAsync();

                // 차주 목록 로드
                await LoadBorrowersAsync();

                // 통계 로드
                await LoadStatisticsAsync();
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
        /// 선택된 물건을 설정하고 해당 차주 정보만 로드
        /// </summary>
        public async Task SetSelectedPropertyAsync(Property property)
        {
            SelectedProperty = property;
            OnPropertyChanged(nameof(IsSingleBorrowerMode));
            
            if (property != null)
            {
                await LoadSingleBorrowerDataAsync();
            }
        }

        /// <summary>
        /// 단일 차주 모드 데이터 로드 (선택된 물건의 차주 정보만)
        /// </summary>
        private async Task LoadSingleBorrowerDataAsync()
        {
            if (SelectedProperty == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 1. 선택된 물건의 차주 정보로 SelectedBorrower 설정
                // DebtorName 또는 BorrowerNumber로 차주 찾기
                var allBorrowers = await _borrowerRepository.GetAllAsync();
                var matchingBorrower = allBorrowers.FirstOrDefault(b =>
                    b.BorrowerName == SelectedProperty.DebtorName ||
                    b.BorrowerNumber == SelectedProperty.BorrowerNumber);

                if (matchingBorrower != null)
                {
                    SelectedBorrower = matchingBorrower;
                }
                else
                {
                    // 차주가 없으면 물건 정보로 임시 차주 생성 (표시용)
                    SelectedBorrower = new Borrower
                    {
                        BorrowerNumber = SelectedProperty.BorrowerNumber ?? "-",
                        BorrowerName = SelectedProperty.DebtorName ?? "-",
                        BorrowerType = "", // Property에는 BorrowerType이 없으므로 빈 값
                        Opb = SelectedProperty.Opb ?? 0,
                        IsRestructuring = SelectedProperty.BorrowerIsRestructuring ?? false
                    };
                }

                // 2. 해당 차주의 물건 목록 로드
                await LoadBorrowerPropertiesAsync();

                // 3. 통계 계산 (선택된 차주 기준)
                await LoadSingleBorrowerStatisticsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 정보 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 단일 차주 통계 계산
        /// </summary>
        private async Task LoadSingleBorrowerStatisticsAsync()
        {
            if (SelectedBorrower == null) return;

            try
            {
                // 해당 차주의 물건 목록에서 통계 계산
                var allProperties = await _propertyRepository.GetAllAsync();
                var borrowerProperties = allProperties
                    .Where(p => p.DebtorName == SelectedBorrower.BorrowerName ||
                                p.BorrowerNumber == SelectedBorrower.BorrowerNumber)
                    .ToList();

                Statistics = new BorrowerStatistics
                {
                    TotalCount = 1, // 선택된 차주 1명
                    IndividualCount = SelectedBorrower.BorrowerType == "개인" ? 1 : 0,
                    SoleProprietorCount = SelectedBorrower.BorrowerType == "개인사업자" ? 1 : 0,
                    CorporationCount = SelectedBorrower.BorrowerType == "법인" ? 1 : 0,
                    RestructuringCount = SelectedBorrower.IsRestructuring ? 1 : 0,
                    TotalOpb = SelectedBorrower.Opb,
                    TotalMortgageAmount = SelectedBorrower.MortgageAmount // 차주의 근저당설정액 사용
                };

                TotalCount = borrowerProperties.Count; // 물건 수로 표시
            }
            catch (Exception ex)
            {
                ErrorMessage = $"통계 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 프로그램 목록 로드
        /// </summary>
        private async Task LoadProgramsAsync()
        {
            try
            {
                var allBorrowers = await _borrowerRepository.GetAllAsync();
                var programIds = allBorrowers
                    .Where(b => !string.IsNullOrWhiteSpace(b.ProgramId))
                    .Select(b => b.ProgramId!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                Programs.Clear();
                Programs.Add("전체");
                foreach (var programId in programIds)
                {
                    Programs.Add(programId);
                }

                if (Programs.Count > 0)
                {
                    SelectedProgramId = Programs[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로그램 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 차주 목록 로드
        /// </summary>
        private async Task LoadBorrowersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var programId = SelectedProgramId == "전체" ? null : SelectedProgramId;
                var borrowerType = SelectedBorrowerType == "전체" ? null : SelectedBorrowerType;
                var searchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

                var (borrowers, totalCount) = await _borrowerRepository.GetPagedAsync(
                    page: CurrentPage,
                    pageSize: PageSize,
                    programId: programId,
                    borrowerType: borrowerType,
                    isRestructuring: ShowRestructuringOnly ? true : null,
                    searchText: searchText
                );

                Borrowers.Clear();
                foreach (var borrower in borrowers)
                {
                    Borrowers.Add(borrower);
                }

                TotalCount = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 통계 로드
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var programId = SelectedProgramId == "전체" ? null : SelectedProgramId;
                Statistics = await _borrowerRepository.GetStatisticsAsync(programId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"통계 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 선택된 차주의 담보물건 로드
        /// </summary>
        private async Task LoadBorrowerPropertiesAsync()
        {
            if (SelectedBorrower == null)
            {
                BorrowerProperties.Clear();
                BorrowerQas.Clear();
                ClearCollateralSummary();
                return;
            }

            try
            {
                // borrower_id로 물건 조회 (또는 차주명으로 매칭)
                var allProperties = await _propertyRepository.GetAllAsync();
                var matchingProperties = allProperties
                    .Where(p => p.DebtorName == SelectedBorrower.BorrowerName)
                    .OrderBy(p => p.PropertyNumber)
                    .ToList();

                BorrowerProperties.Clear();
                foreach (var property in matchingProperties)
                {
                    BorrowerProperties.Add(property);
                }

                // 담보 총괄 계산 (S-004, S-005)
                CalculateCollateralSummary(matchingProperties);

                // QA 로드 (Q-003)
                await LoadBorrowerQasAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"담보물건 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 담보 총괄 정보 계산 (S-004, S-005)
        /// </summary>
        private void CalculateCollateralSummary(List<Property> properties)
        {
            if (properties == null || properties.Count == 0)
            {
                ClearCollateralSummary();
                return;
            }

            // 담보종류 요약 (유형별 개수)
            var typeGroups = properties
                .GroupBy(p => p.PropertyType ?? "기타")
                .Select(g => $"{g.Key}({g.Count()})")
                .ToList();
            CollateralTypeSummary = typeGroups.Count > 0 ? string.Join(", ", typeGroups) : "-";

            // 면적 합계
            TotalLandArea = properties.Sum(p => p.LandArea ?? 0);
            TotalBuildingArea = properties.Sum(p => p.BuildingArea ?? 0);

            // 기계기구 가치 합계 (MachineryValue 필드가 있으면 사용, 없으면 0)
            TotalMachineryValue = properties.Sum(p => p.MachineryValue ?? 0);

            // 공장저당 여부 (하나라도 있으면 true)
            HasFactoryMortgage = properties.Any(p => p.IsFactoryMortgage == true);

            // 공단 여부 (하나라도 있으면 true)
            IsInIndustrialComplex = properties.Any(p => p.IsIndustrialComplex == true);
        }

        /// <summary>
        /// 담보 총괄 정보 초기화
        /// </summary>
        private void ClearCollateralSummary()
        {
            CollateralTypeSummary = "-";
            TotalLandArea = 0;
            TotalBuildingArea = 0;
            TotalMachineryValue = 0;
            HasFactoryMortgage = false;
            IsInIndustrialComplex = false;
        }

        /// <summary>
        /// 차주별 QA 로드 (Q-003 집계)
        /// </summary>
        private async Task LoadBorrowerQasAsync()
        {
            if (SelectedBorrower == null) return;

            try
            {
                var qas = await _qaRepository.GetByBorrowerIdAsync(SelectedBorrower.Id);
                BorrowerQas.Clear();
                foreach (var qa in qas)
                {
                    BorrowerQas.Add(qa);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"QA 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// QA 답변 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveQAAnswerAsync(PropertyQa qa)
        {
            if (qa == null) return;

            try
            {
                IsLoading = true;
                qa.AnsweredAt = DateTime.UtcNow;
                // 현재 사용자 정보가 있다면 설정
                // qa.AnsweredBy = CurrentUser?.Id; 

                await _qaRepository.UpdateAsync(qa);
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("QA 답변이 저장되었습니다.");
                await LoadBorrowerQasAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"QA 저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// QA 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteQAAsync(PropertyQa qa)
        {
            if (qa == null) return;

            try
            {
                IsLoading = true;
                await _qaRepository.DeleteAsync(qa.Id);
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("QA가 삭제되었습니다.");
                await LoadBorrowerQasAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"QA 삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 선택된 차주 변경 시
        /// </summary>
        partial void OnSelectedBorrowerChanged(Borrower? value)
        {
            _ = LoadBorrowerPropertiesAsync();
        }

        /// <summary>
        /// 검색 명령
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadBorrowersAsync();
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedProgramIdChanged(string? value)
        {
            _ = ApplyFiltersAsync();
        }

        partial void OnSelectedBorrowerTypeChanged(string? value)
        {
            _ = ApplyFiltersAsync();
        }

        partial void OnShowRestructuringOnlyChanged(bool value)
        {
            _ = ApplyFiltersAsync();
        }

        /// <summary>
        /// 필터 적용
        /// </summary>
        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            CurrentPage = 1;
            await LoadBorrowersAsync();
            await LoadStatisticsAsync();
        }

        /// <summary>
        /// 필터 초기화
        /// </summary>
        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            SearchText = "";
            SelectedProgramId = Programs.FirstOrDefault();
            SelectedBorrowerType = BorrowerTypes.FirstOrDefault();
            ShowRestructuringOnly = false;
            CurrentPage = 1;
            await LoadBorrowersAsync();
            await LoadStatisticsAsync();
        }

        /// <summary>
        /// 이전 페이지
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadBorrowersAsync();
            }
        }

        private bool CanGoPreviousPage() => CurrentPage > 1;

        /// <summary>
        /// 다음 페이지
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNextPage))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadBorrowersAsync();
            }
        }

        private bool CanGoNextPage() => CurrentPage < TotalPages;

        /// <summary>
        /// 차주 추가 모달 열기
        /// </summary>
        [RelayCommand]
        private void OpenAddBorrowerModal()
        {
            NewBorrower = new Borrower
            {
                Id = Guid.NewGuid(),
                BorrowerNumber = $"B-{DateTime.Now:yyyyMMddHHmmss}",
                BorrowerType = "개인",
                ProgramId = SelectedProgramId == "전체" ? null : SelectedProgramId
            };
            IsAddModalOpen = true;
        }

        /// <summary>
        /// 차주 추가 (저장)
        /// </summary>
        [RelayCommand]
        private async Task AddBorrowerAsync()
        {
            if (string.IsNullOrWhiteSpace(NewBorrower.BorrowerName))
            {
                ErrorMessage = "차주명을 입력해주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                await _borrowerRepository.CreateAsync(NewBorrower);
                IsAddModalOpen = false;
                await LoadBorrowersAsync();
                await LoadStatisticsAsync();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("차주가 추가되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 추가 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 모달 닫기
        /// </summary>
        [RelayCommand]
        private void CloseModal()
        {
            IsAddModalOpen = false;
        }

        /// <summary>
        /// 삭제 확인 모달 열기
        /// </summary>
        [RelayCommand]
        private void ConfirmDeleteBorrower(Borrower borrower)
        {
            if (borrower == null) return;
            BorrowerToDelete = borrower;
            IsConfirmDeleteModalOpen = true;
        }

        /// <summary>
        /// 차주 삭제 실행
        /// </summary>
        [RelayCommand]
        private async Task DeleteBorrowerAsync()
        {
            if (BorrowerToDelete == null) return;

            try
            {
                IsLoading = true;
                await _borrowerRepository.DeleteAsync(BorrowerToDelete.Id);
                IsConfirmDeleteModalOpen = false;
                BorrowerToDelete = null;
                
                await LoadBorrowersAsync();
                await LoadStatisticsAsync();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("차주가 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 삭제 취소
        /// </summary>
        [RelayCommand]
        private void CancelDelete()
        {
            IsConfirmDeleteModalOpen = false;
            BorrowerToDelete = null;
        }

        /// <summary>
        /// 차주 저장 (수정)
        /// </summary>
        [RelayCommand]
        private async Task SaveBorrowerAsync()
        {
            if (SelectedBorrower == null) return;

            try
            {
                IsLoading = true;
                await _borrowerRepository.UpdateAsync(SelectedBorrower);
                await LoadBorrowersAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 담보물건 상세 보기
        /// </summary>
        [RelayCommand]
        private void ViewPropertyDetail(Property property)
        {
            if (property == null) return;
            MainWindow.Instance?.NavigateToPropertyDetail(property);
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadBorrowersAsync();
            await LoadStatisticsAsync();
            await LoadBorrowerPropertiesAsync();
        }
    }
}

