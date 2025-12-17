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
        private readonly AuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<Borrower> _borrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        [ObservableProperty]
        private ObservableCollection<Property> _borrowerProperties = new();

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

        // 통계
        [ObservableProperty]
        private BorrowerStatistics? _statistics;

        public BorrowerOverviewViewModel(
            BorrowerRepository borrowerRepository,
            PropertyRepository propertyRepository,
            AuthService authService)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
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
            }
            catch (Exception ex)
            {
                ErrorMessage = $"담보물건 로드 실패: {ex.Message}";
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
        /// 차주 추가
        /// </summary>
        [RelayCommand]
        private async Task AddBorrowerAsync()
        {
            try
            {
                // TODO: 차주 추가 모달 구현
                var newBorrower = new Borrower
                {
                    Id = Guid.NewGuid(),
                    BorrowerNumber = $"B-{DateTime.Now:yyyyMMddHHmmss}",
                    BorrowerName = "새 차주",
                    BorrowerType = "개인",
                    ProgramId = SelectedProgramId == "전체" ? null : SelectedProgramId
                };

                await _borrowerRepository.CreateAsync(newBorrower);
                await LoadBorrowersAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 추가 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 차주 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteBorrowerAsync(Borrower borrower)
        {
            if (borrower == null) return;

            try
            {
                await _borrowerRepository.DeleteAsync(borrower.Id);
                await LoadBorrowersAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 삭제 실패: {ex.Message}";
            }
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

