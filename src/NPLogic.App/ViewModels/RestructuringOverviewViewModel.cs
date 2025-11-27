using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 회생개요 ViewModel
    /// </summary>
    public partial class RestructuringOverviewViewModel : ObservableObject
    {
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;

        [ObservableProperty]
        private ObservableCollection<Borrower> _restructuringBorrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        [ObservableProperty]
        private ObservableCollection<Loan> _borrowerLoans = new();

        // 회생 통계
        [ObservableProperty]
        private int _totalRestructuringCount;

        [ObservableProperty]
        private decimal _totalRestructuringOpb;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public RestructuringOverviewViewModel(
            BorrowerRepository borrowerRepository,
            LoanRepository loanRepository)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await LoadRestructuringBorrowersAsync();
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

        private async Task LoadRestructuringBorrowersAsync()
        {
            try
            {
                var borrowers = await _borrowerRepository.GetRestructuringBorrowersAsync();

                RestructuringBorrowers.Clear();
                foreach (var borrower in borrowers)
                {
                    RestructuringBorrowers.Add(borrower);
                }

                TotalRestructuringCount = borrowers.Count;
                TotalRestructuringOpb = borrowers.Sum(b => b.Opb);

                if (RestructuringBorrowers.Count > 0 && SelectedBorrower == null)
                {
                    SelectedBorrower = RestructuringBorrowers[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"회생 차주 로드 실패: {ex.Message}";
            }
        }

        private async Task LoadBorrowerLoansAsync()
        {
            if (SelectedBorrower == null)
            {
                BorrowerLoans.Clear();
                return;
            }

            try
            {
                var loans = await _loanRepository.GetByBorrowerIdAsync(SelectedBorrower.Id);
                BorrowerLoans.Clear();
                foreach (var loan in loans)
                {
                    BorrowerLoans.Add(loan);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대출 정보 로드 실패: {ex.Message}";
            }
        }

        partial void OnSelectedBorrowerChanged(Borrower? value)
        {
            _ = LoadBorrowerLoansAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadRestructuringBorrowersAsync();
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            NPLogic.UI.Services.ToastService.Instance.ShowInfo("Excel 내보내기는 준비 중입니다.");
        }
    }
}

