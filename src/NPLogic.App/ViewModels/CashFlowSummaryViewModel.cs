using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Core.Services;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 현금흐름 집계 ViewModel
    /// </summary>
    public partial class CashFlowSummaryViewModel : ObservableObject
    {
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;

        [ObservableProperty]
        private ObservableCollection<Borrower> _borrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        [ObservableProperty]
        private ObservableCollection<CashFlowSummaryItem> _cashFlowItems = new();

        // XNPV 계산 파라미터
        [ObservableProperty]
        private decimal _discountRate = 0.08m;

        [ObservableProperty]
        private DateTime _baseDate = DateTime.Today;

        [ObservableProperty]
        private int _selectedScenario = 1;

        // 계산 결과
        [ObservableProperty]
        private XnpvResult? _xnpvResult;

        // 민감도 분석 결과
        [ObservableProperty]
        private ObservableCollection<SensitivityItem> _sensitivityResults = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public CashFlowSummaryViewModel(
            BorrowerRepository borrowerRepository,
            LoanRepository loanRepository)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
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

                await LoadBorrowersAsync();
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
        /// 현금흐름 생성 및 계산
        /// </summary>
        private async Task GenerateCashFlowsAsync()
        {
            if (SelectedBorrower == null)
            {
                CashFlowItems.Clear();
                XnpvResult = null;
                return;
            }

            try
            {
                IsLoading = true;

                // 차주의 대출 정보 조회
                var loans = await _loanRepository.GetByBorrowerIdAsync(SelectedBorrower.Id);

                // 현금흐름 생성 (간단한 예시 - 실제로는 더 복잡한 로직 필요)
                var cashFlows = GenerateSampleCashFlows(loans);
                
                // UI 표시용 집계 항목 생성
                CashFlowItems.Clear();
                decimal cumulative = 0;
                int period = 0;
                
                foreach (var cf in cashFlows.OrderBy(c => c.FlowDate))
                {
                    cumulative += cf.NetCashFlow;
                    CashFlowItems.Add(new CashFlowSummaryItem
                    {
                        Period = period++,
                        FlowDate = cf.FlowDate,
                        CashInflow = cf.CashInflow,
                        CashOutflow = cf.CashOutflow,
                        CumulativeCashFlow = cumulative,
                        Description = cf.Description ?? ""
                    });
                }

                // XNPV 계산
                if (cashFlows.Count > 0)
                {
                    XnpvResult = XnpvCalculator.CalculateResult(cashFlows, DiscountRate, SelectedScenario);
                    
                    // 민감도 분석
                    await RunSensitivityAnalysisAsync(cashFlows);
                }
                else
                {
                    XnpvResult = null;
                    SensitivityResults.Clear();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"현금흐름 계산 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 샘플 현금흐름 생성 (실제 구현 시 대출 정보 기반으로 계산)
        /// </summary>
        private List<CashFlow> GenerateSampleCashFlows(List<Loan> loans)
        {
            var cashFlows = new List<CashFlow>();
            
            // 초기 투자 (비용)
            var totalInvestment = loans.Sum(l => l.LoanPrincipalBalance ?? 0);
            if (totalInvestment > 0)
            {
                cashFlows.Add(new CashFlow
                {
                    FlowDate = BaseDate,
                    CashInflow = 0,
                    CashOutflow = totalInvestment,
                    FlowType = "INVESTMENT",
                    Description = "초기 투자금",
                    Scenario = SelectedScenario
                });
            }

            // 예상 회수 현금흐름 (시나리오별)
            foreach (var loan in loans)
            {
                var expectedDate = SelectedScenario == 1 
                    ? loan.ExpectedDividendDate1 ?? BaseDate.AddMonths(6)
                    : loan.ExpectedDividendDate2 ?? BaseDate.AddMonths(12);
                
                var loanCap = SelectedScenario == 1 ? loan.LoanCap1 : loan.LoanCap2;
                var recoveryAmount = loanCap ?? (loan.LoanPrincipalBalance ?? 0) * 0.8m;

                if (recoveryAmount > 0)
                {
                    cashFlows.Add(new CashFlow
                    {
                        FlowDate = expectedDate,
                        CashInflow = recoveryAmount,
                        CashOutflow = 0,
                        FlowType = "RECOVERY",
                        Description = $"{loan.AccountSerial} 회수",
                        Scenario = SelectedScenario
                    });
                }
            }

            return cashFlows;
        }

        /// <summary>
        /// 민감도 분석 실행
        /// </summary>
        private async Task RunSensitivityAnalysisAsync(List<CashFlow> cashFlows)
        {
            await Task.Run(() =>
            {
                var results = XnpvCalculator.SensitivityAnalysis(
                    cashFlows, 
                    0.05m, 0.15m, 0.01m, 
                    SelectedScenario);

                App.Current.Dispatcher.Invoke(() =>
                {
                    SensitivityResults.Clear();
                    foreach (var (rate, xnpv) in results)
                    {
                        SensitivityResults.Add(new SensitivityItem
                        {
                            DiscountRate = rate,
                            Xnpv = xnpv,
                            IsCurrent = Math.Abs(rate - DiscountRate) < 0.001m
                        });
                    }
                });
            });
        }

        /// <summary>
        /// 선택된 차주 변경 시
        /// </summary>
        partial void OnSelectedBorrowerChanged(Borrower? value)
        {
            _ = GenerateCashFlowsAsync();
        }

        /// <summary>
        /// 할인율 변경 시
        /// </summary>
        partial void OnDiscountRateChanged(decimal value)
        {
            _ = GenerateCashFlowsAsync();
        }

        /// <summary>
        /// 시나리오 변경 시
        /// </summary>
        partial void OnSelectedScenarioChanged(int value)
        {
            _ = GenerateCashFlowsAsync();
        }

        /// <summary>
        /// XNPV 재계산
        /// </summary>
        [RelayCommand]
        private async Task RecalculateAsync()
        {
            await GenerateCashFlowsAsync();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadBorrowersAsync();
            await GenerateCashFlowsAsync();
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private void ExportToExcel()
        {
            NPLogic.UI.Services.ToastService.Instance.ShowInfo("Excel 내보내기는 준비 중입니다.");
        }
    }

    /// <summary>
    /// 민감도 분석 항목
    /// </summary>
    public class SensitivityItem
    {
        public decimal DiscountRate { get; set; }
        public decimal Xnpv { get; set; }
        public bool IsCurrent { get; set; }

        public string RateDisplay => $"{DiscountRate:P0}";
        public string XnpvDisplay => $"{Xnpv:N0}";
    }
}

