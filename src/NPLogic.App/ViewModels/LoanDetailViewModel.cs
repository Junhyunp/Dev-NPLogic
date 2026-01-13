using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// Loan 상세 ViewModel
    /// </summary>
    public partial class LoanDetailViewModel : ObservableObject
    {
        private readonly LoanRepository _loanRepository;
        private readonly BorrowerRepository _borrowerRepository;
        private readonly AuthService _authService;
        private readonly ExcelService _excelService;

        [ObservableProperty]
        private ObservableCollection<Borrower> _borrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        // 선택된 물건 (단일 차주 모드용)
        [ObservableProperty]
        private Property? _selectedProperty;

        /// <summary>
        /// 단일 차주 모드 여부 (선택된 물건이 있으면 true)
        /// </summary>
        public bool IsSingleBorrowerMode => SelectedProperty != null;

        [ObservableProperty]
        private ObservableCollection<Loan> _loans = new();

        [ObservableProperty]
        private Loan? _selectedLoan;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // 통계
        [ObservableProperty]
        private LoanStatistics? _statistics;

        // Loan Cap 계산용 날짜
        [ObservableProperty]
        private DateTime _scenario1Date = DateTime.Today.AddMonths(6);

        [ObservableProperty]
        private DateTime _scenario2Date = DateTime.Today.AddMonths(12);

        public LoanDetailViewModel(
            LoanRepository loanRepository,
            BorrowerRepository borrowerRepository,
            AuthService authService,
            ExcelService excelService)
        {
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
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

                if (IsSingleBorrowerMode)
                {
                    // 단일 차주 모드: 선택된 물건의 차주만 로드
                    await LoadSingleBorrowerDataAsync();
                }
                else
                {
                    // 전체 모드: 모든 차주 로드
                    await LoadBorrowersAsync();
                }
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
        /// 선택된 물건으로 단일 차주 모드 설정
        /// </summary>
        public async Task SetSelectedPropertyAsync(Property property)
        {
            SelectedProperty = property;
            OnPropertyChanged(nameof(IsSingleBorrowerMode));
            await InitializeAsync();
        }

        /// <summary>
        /// 단일 차주 데이터 로드
        /// </summary>
        private async Task LoadSingleBorrowerDataAsync()
        {
            if (SelectedProperty == null) return;

            try
            {
                // 선택된 물건의 차주 찾기
                var allBorrowers = await _borrowerRepository.GetAllAsync();
                var matchingBorrower = allBorrowers.FirstOrDefault(b => 
                    b.BorrowerNumber == SelectedProperty.BorrowerNumber ||
                    b.BorrowerName == SelectedProperty.DebtorName);

                Borrowers.Clear();

                if (matchingBorrower != null)
                {
                    Borrowers.Add(matchingBorrower);
                    SelectedBorrower = matchingBorrower;
                }
                else
                {
                    // 차주가 없으면 물건 정보로 임시 차주 생성 (표시용)
                    var tempBorrower = new Borrower
                    {
                        BorrowerNumber = SelectedProperty.BorrowerNumber ?? "-",
                        BorrowerName = SelectedProperty.DebtorName ?? "-",
                        BorrowerType = "",
                        Opb = SelectedProperty.Opb ?? 0,
                        IsRestructuring = SelectedProperty.BorrowerIsRestructuring ?? false
                    };
                    Borrowers.Add(tempBorrower);
                    SelectedBorrower = tempBorrower;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 데이터 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 차주 목록 로드
        /// </summary>
        private async Task LoadBorrowersAsync()
        {
            try
            {
                var borrowers = await _borrowerRepository.GetAllAsync();

                Borrowers.Clear();
                foreach (var borrower in borrowers)
                {
                    Borrowers.Add(borrower);
                }

                if (Borrowers.Count > 0 && SelectedBorrower == null)
                {
                    SelectedBorrower = Borrowers[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 대출 목록 로드
        /// </summary>
        private async Task LoadLoansAsync()
        {
            if (SelectedBorrower == null)
            {
                Loans.Clear();
                Statistics = null;
                return;
            }

            try
            {
                IsLoading = true;

                var searchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

                var loans = await _loanRepository.GetFilteredAsync(
                    borrowerId: SelectedBorrower.Id,
                    searchText: searchText
                );

                Loans.Clear();
                foreach (var loan in loans)
                {
                    Loans.Add(loan);
                }

                // 통계 로드
                Statistics = await _loanRepository.GetStatisticsByBorrowerIdAsync(SelectedBorrower.Id);

                // 첫 번째 대출 선택
                if (Loans.Count > 0 && SelectedLoan == null)
                {
                    SelectedLoan = Loans[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대출 목록 로드 실패: {ex.Message}";
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
            SelectedLoan = null;
            _ = LoadLoansAsync();
        }

        /// <summary>
        /// 검색
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadLoansAsync();
        }

        /// <summary>
        /// Loan Cap 재계산
        /// </summary>
        [RelayCommand]
        private async Task RecalculateLoanCapAsync()
        {
            if (SelectedLoan == null) return;

            try
            {
                IsLoading = true;

                // 시나리오 1 계산
                SelectedLoan.ExpectedDividendDate1 = Scenario1Date;
                SelectedLoan.CalculateScenario1();

                // 시나리오 2 계산
                SelectedLoan.ExpectedDividendDate2 = Scenario2Date;
                SelectedLoan.CalculateScenario2();

                // DB 업데이트
                await _loanRepository.UpdateAsync(SelectedLoan);

                // UI 갱신
                OnPropertyChanged(nameof(SelectedLoan));
                await LoadLoansAsync();

                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("Loan Cap이 재계산되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Loan Cap 재계산 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 전체 대출 Loan Cap 일괄 계산
        /// </summary>
        [RelayCommand]
        private async Task RecalculateAllLoanCapsAsync()
        {
            if (SelectedBorrower == null) return;

            try
            {
                IsLoading = true;

                foreach (var loan in Loans)
                {
                    loan.ExpectedDividendDate1 = Scenario1Date;
                    loan.CalculateScenario1();
                    loan.ExpectedDividendDate2 = Scenario2Date;
                    loan.CalculateScenario2();
                    await _loanRepository.UpdateAsync(loan);
                }

                await LoadLoansAsync();

                NPLogic.UI.Services.ToastService.Instance.ShowSuccess($"{Loans.Count}건의 Loan Cap이 재계산되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"일괄 계산 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 대출 추가
        /// </summary>
        [RelayCommand]
        private async Task AddLoanAsync()
        {
            if (SelectedBorrower == null)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("먼저 차주를 선택해주세요.");
                return;
            }

            try
            {
                var newLoan = new Loan
                {
                    Id = Guid.NewGuid(),
                    BorrowerId = SelectedBorrower.Id,
                    AccountSerial = $"L-{DateTime.Now:yyyyMMddHHmmss}",
                    LoanType = "일반",
                    ExpectedDividendDate1 = Scenario1Date,
                    ExpectedDividendDate2 = Scenario2Date
                };

                await _loanRepository.CreateAsync(newLoan);
                await LoadLoansAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대출 추가 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 대출 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteLoanAsync(Loan loan)
        {
            if (loan == null) return;

            try
            {
                await _loanRepository.DeleteAsync(loan.Id);
                await LoadLoansAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대출 삭제 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 대출 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveLoanAsync()
        {
            if (SelectedLoan == null) return;

            try
            {
                IsLoading = true;

                // 채권액 합계 계산
                SelectedLoan.TotalClaimAmount = SelectedLoan.CalculateTotalClaim();

                await _loanRepository.UpdateAsync(SelectedLoan);
                await LoadLoansAsync();

                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("저장되었습니다.");
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
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadBorrowersAsync();
            await LoadLoansAsync();
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (Loans.Count == 0)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("내보낼 대출 데이터가 없습니다.");
                return;
            }

            try
            {
                IsLoading = true;
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel 파일|*.xlsx",
                    FileName = $"Loan상세_{SelectedBorrower?.BorrowerName ?? "전체"}_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _excelService.ExportLoansToExcelAsync(Loans, dialog.FileName);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("Excel 파일이 저장되었습니다.");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Excel 내보내기 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

