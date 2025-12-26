using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Core.Services;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// XNPV 비교 ViewModel
    /// </summary>
    public partial class XnpvComparisonViewModel : ObservableObject
    {
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;
        private readonly ExcelService _excelService;

        [ObservableProperty]
        private ObservableCollection<XnpvComparisonItem> _comparisonItems = new();

        [ObservableProperty]
        private decimal _discountRate = 0.08m;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // 합계
        [ObservableProperty]
        private decimal _totalXnpv1;

        [ObservableProperty]
        private decimal _totalXnpv2;

        [ObservableProperty]
        private string _recommendation = "";

        public XnpvComparisonViewModel(
            BorrowerRepository borrowerRepository,
            LoanRepository loanRepository,
            ExcelService excelService)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await LoadComparisonDataAsync();
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

        private async Task LoadComparisonDataAsync()
        {
            try
            {
                var borrowers = await _borrowerRepository.GetAllAsync();
                
                ComparisonItems.Clear();
                
                foreach (var borrower in borrowers)
                {
                    var loans = await _loanRepository.GetByBorrowerIdAsync(borrower.Id);
                    
                    var item = new XnpvComparisonItem
                    {
                        BorrowerId = borrower.Id,
                        BorrowerNumber = borrower.BorrowerNumber,
                        BorrowerName = borrower.BorrowerName,
                        PropertyCount = borrower.PropertyCount,
                        TotalOpb = borrower.Opb,
                        LoanCap1 = loans.Sum(l => l.LoanCap1 ?? 0),
                        LoanCap2 = loans.Sum(l => l.LoanCap2 ?? 0),
                        Xnpv1 = borrower.XnpvScenario1 ?? loans.Sum(l => l.LoanCap1 ?? 0) * 0.9m,
                        Xnpv2 = borrower.XnpvScenario2 ?? loans.Sum(l => l.LoanCap2 ?? 0) * 0.85m,
                        IsRestructuring = borrower.IsRestructuring
                    };

                    item.Ratio1 = item.TotalOpb > 0 ? item.Xnpv1 / item.TotalOpb : 0;
                    item.Ratio2 = item.TotalOpb > 0 ? item.Xnpv2 / item.TotalOpb : 0;
                    item.Difference = item.Xnpv1 - item.Xnpv2;

                    ComparisonItems.Add(item);
                }

                // 합계 계산
                TotalXnpv1 = ComparisonItems.Sum(x => x.Xnpv1);
                TotalXnpv2 = ComparisonItems.Sum(x => x.Xnpv2);
                
                // 추천
                Recommendation = TotalXnpv1 >= TotalXnpv2 
                    ? $"시나리오 1안 권장 (XNPV 차이: {TotalXnpv1 - TotalXnpv2:N0}원)"
                    : $"시나리오 2안 권장 (XNPV 차이: {TotalXnpv2 - TotalXnpv1:N0}원)";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 로드 실패: {ex.Message}";
            }
        }

        partial void OnDiscountRateChanged(decimal value)
        {
            _ = LoadComparisonDataAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadComparisonDataAsync();
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (ComparisonItems.Count == 0)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("내보낼 데이터가 없습니다.");
                return;
            }

            try
            {
                IsLoading = true;
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel 파일|*.xlsx",
                    FileName = $"XNPV비교리포트_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _excelService.ExportXnpvToExcelAsync(ComparisonItems, dialog.FileName);
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

    public class XnpvComparisonItem
    {
        public Guid BorrowerId { get; set; }
        public string BorrowerNumber { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public int PropertyCount { get; set; }
        public decimal TotalOpb { get; set; }
        public decimal LoanCap1 { get; set; }
        public decimal LoanCap2 { get; set; }
        public decimal Xnpv1 { get; set; }
        public decimal Xnpv2 { get; set; }
        public decimal Ratio1 { get; set; }
        public decimal Ratio2 { get; set; }
        public decimal Difference { get; set; }
        public bool IsRestructuring { get; set; }

        public string BetterScenario => Xnpv1 >= Xnpv2 ? "1안" : "2안";
    }
}
