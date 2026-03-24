using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Core.Services;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
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
        private readonly PropertyNoteRepository? _propertyNoteRepository;
        private Guid _noteOwnerId = Guid.Empty;

        [ObservableProperty]
        private ObservableCollection<XnpvComparisonItem> _comparisonItems = new();

        [ObservableProperty]
        private decimal _discountRate = 0.08m;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string _loadingMessage = "분석 중...";

        [ObservableProperty]
        private int _loadingProgress;

        [ObservableProperty]
        private int _loadingTotal;

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

            _propertyNoteRepository = App.ServiceProvider?
                .GetService(typeof(PropertyNoteRepository)) as PropertyNoteRepository;

            try
            {
                var authService = App.ServiceProvider?.GetService(typeof(AuthService)) as AuthService;
                var session = authService?.GetSession();
                if (session?.User?.Id != null)
                    _noteOwnerId = Guid.Parse(session.User.Id);
            }
            catch { /* 세션 없으면 비고 비활성 */ }
        }

        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("[XnpvComparisonViewModel] InitializeAsync 시작");
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await LoadComparisonDataAsync();
                System.Diagnostics.Debug.WriteLine("[XnpvComparisonViewModel] LoadComparisonDataAsync 완료");

                await LoadNoteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XnpvComparisonViewModel] 초기화 실패: {ex.Message}");
                ErrorMessage = $"초기화 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("[XnpvComparisonViewModel] IsLoading = false");
            }
        }

        private async Task LoadComparisonDataAsync()
        {
            try
            {
                LoadingMessage = "차주 목록 불러오는 중...";
                System.Diagnostics.Debug.WriteLine("[XnpvComparisonViewModel] GetAllAsync 호출...");
                var borrowers = (await _borrowerRepository.GetAllAsync()).ToList();
                System.Diagnostics.Debug.WriteLine($"[XnpvComparisonViewModel] 차주 {borrowers.Count}개 로드됨");
                
                ComparisonItems.Clear();
                LoadingTotal = borrowers.Count;
                LoadingProgress = 0;
                
                foreach (var borrower in borrowers)
                {
                    LoadingProgress++;
                    LoadingMessage = $"분석 중... ({LoadingProgress}/{LoadingTotal}) {borrower.BorrowerName}";
                    
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

        // ========== 비고 패널 ==========

        [ObservableProperty]
        private string _noteText = string.Empty;

        [ObservableProperty]
        private bool _isNotePanelVisible;

        [RelayCommand]
        private void ToggleNotePanel()
        {
            IsNotePanelVisible = !IsNotePanelVisible;
        }

        [RelayCommand]
        private async Task SaveNote()
        {
            await SaveNoteAsync();
        }

        private async Task LoadNoteAsync()
        {
            if (_propertyNoteRepository == null || _noteOwnerId == Guid.Empty) return;
            try
            {
                var note = await _propertyNoteRepository.GetByPropertyAndTabAsync(_noteOwnerId, "npv_comparison");
                NoteText = note?.NoteText ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XnpvComparison] 비고 로드 실패: {ex.Message}");
            }
        }

        private async Task SaveNoteAsync()
        {
            if (_propertyNoteRepository == null || _noteOwnerId == Guid.Empty) return;
            try
            {
                await _propertyNoteRepository.UpsertAsync(new NPLogic.Core.Models.PropertyNote
                {
                    PropertyId = _noteOwnerId,
                    TabName = "npv_comparison",
                    NoteText = NoteText ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[XnpvComparison] 비고 저장 실패: {ex.Message}");
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
