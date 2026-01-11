using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// 차주 선택 아이템 (S-007)
    /// </summary>
    public partial class BorrowerSelectItem : ObservableObject
    {
        [ObservableProperty]
        private Guid _borrowerId;

        [ObservableProperty]
        private string _borrowerName = "";

        [ObservableProperty]
        private string _borrowerNumber = "";

        [ObservableProperty]
        private bool _isSelected = true;

        // 선택 변경 이벤트
        public event Action? SelectionChanged;

        partial void OnIsSelectedChanged(bool value)
        {
            SelectionChanged?.Invoke();
        }
    }

    /// <summary>
    /// 월별 현금흐름 행 (S-008)
    /// </summary>
    public partial class MonthlyCashFlowRow : ObservableObject
    {
        [ObservableProperty]
        private string _borrowerName = "";

        [ObservableProperty]
        private Guid _borrowerId;

        [ObservableProperty]
        private bool _isTotal;

        /// <summary>
        /// 월별 금액 (키: "2026-01", 값: 금액)
        /// </summary>
        public Dictionary<string, decimal> MonthlyAmounts { get; set; } = new();

        /// <summary>
        /// 인덱서 - 월별 금액 접근
        /// </summary>
        public decimal this[string monthKey]
        {
            get => MonthlyAmounts.TryGetValue(monthKey, out var value) ? value : 0;
            set => MonthlyAmounts[monthKey] = value;
        }
    }

    /// <summary>
    /// 현금흐름 집계 ViewModel (S-007, S-008 재구성)
    /// </summary>
    public partial class CashFlowSummaryViewModel : ObservableObject
    {
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;
        private readonly ExcelService _excelService;

        // ========== S-007: 차주 리스트 + 검색 ==========

        /// <summary>
        /// 전체 차주 목록
        /// </summary>
        private List<BorrowerSelectItem> _allBorrowers = new();

        /// <summary>
        /// 필터링된 차주 목록
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BorrowerSelectItem> _filteredBorrowers = new();

        /// <summary>
        /// 검색어
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";

        /// <summary>
        /// 전체 선택 여부
        /// </summary>
        [ObservableProperty]
        private bool _isAllSelected = true;

        /// <summary>
        /// 선택된 차주 수
        /// </summary>
        [ObservableProperty]
        private int _selectedBorrowersCount;

        /// <summary>
        /// 전체 차주 수
        /// </summary>
        [ObservableProperty]
        private int _totalBorrowersCount;

        // ========== S-008: 120개월 월별 현금흐름 ==========

        /// <summary>
        /// 액션데이트 (기준일)
        /// </summary>
        [ObservableProperty]
        private DateTime _actionDate = DateTime.Today;

        /// <summary>
        /// 월별 현금흐름 행들
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MonthlyCashFlowRow> _monthlyCashFlowRows = new();

        /// <summary>
        /// 120개월 컬럼 키 목록 (동적 컬럼 생성용)
        /// </summary>
        public List<string> MonthColumns { get; private set; } = new();

        /// <summary>
        /// 월 컬럼 변경 이벤트 (View에서 동적 컬럼 생성)
        /// </summary>
        public event Action? MonthColumnsChanged;

        // ========== 요약 정보 ==========

        [ObservableProperty]
        private decimal _totalXnpv;

        [ObservableProperty]
        private decimal _totalInflow;

        [ObservableProperty]
        private decimal _totalOutflow;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // 기존 속성 (호환성)
        [ObservableProperty]
        private ObservableCollection<Borrower> _borrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        [ObservableProperty]
        private ObservableCollection<CashFlowSummaryItem> _cashFlowItems = new();

        [ObservableProperty]
        private decimal _discountRate = 0.08m;

        [ObservableProperty]
        private DateTime _baseDate = DateTime.Today;

        [ObservableProperty]
        private int _selectedScenario = 1;

        [ObservableProperty]
        private XnpvResult? _xnpvResult;

        [ObservableProperty]
        private ObservableCollection<SensitivityItem> _sensitivityResults = new();

        public CashFlowSummaryViewModel(
            BorrowerRepository borrowerRepository,
            LoanRepository loanRepository,
            ExcelService excelService)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));

            // 120개월 컬럼 초기화
            GenerateMonthColumns();
        }

        /// <summary>
        /// 120개월 컬럼 생성
        /// </summary>
        private void GenerateMonthColumns()
        {
            MonthColumns.Clear();
            var startDate = new DateTime(ActionDate.Year, ActionDate.Month, 1);
            
            for (int i = 0; i < 120; i++)
            {
                var monthDate = startDate.AddMonths(i);
                MonthColumns.Add(monthDate.ToString("yyyy-MM"));
            }
        }

        /// <summary>
        /// 검색어 변경 시 필터링
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            FilterBorrowers();
        }

        /// <summary>
        /// 전체 선택 변경 시
        /// </summary>
        partial void OnIsAllSelectedChanged(bool value)
        {
            foreach (var borrower in _allBorrowers)
            {
                borrower.IsSelected = value;
            }
            UpdateSelectedCount();
            _ = GenerateMonthlyCashFlowsAsync();
        }

        /// <summary>
        /// 액션데이트 변경 시
        /// </summary>
        partial void OnActionDateChanged(DateTime value)
        {
            GenerateMonthColumns();
            MonthColumnsChanged?.Invoke();
            _ = GenerateMonthlyCashFlowsAsync();
        }

        /// <summary>
        /// 차주 필터링
        /// </summary>
        private void FilterBorrowers()
        {
            FilteredBorrowers.Clear();
            
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allBorrowers
                : _allBorrowers.Where(b => 
                    b.BorrowerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    b.BorrowerNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var borrower in filtered)
            {
                FilteredBorrowers.Add(borrower);
            }
        }

        /// <summary>
        /// 선택 개수 업데이트
        /// </summary>
        private void UpdateSelectedCount()
        {
            SelectedBorrowersCount = _allBorrowers.Count(b => b.IsSelected);
            TotalBorrowersCount = _allBorrowers.Count;
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
                await GenerateMonthlyCashFlowsAsync();
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
        /// 차주 목록 로드 (S-007)
        /// </summary>
        private async Task LoadBorrowersAsync()
        {
            try
            {
                var borrowers = await _borrowerRepository.GetAllAsync();
                _allBorrowers.Clear();
                
                foreach (var borrower in borrowers)
                {
                    var item = new BorrowerSelectItem
                    {
                        BorrowerId = borrower.Id,
                        BorrowerName = borrower.BorrowerName ?? "",
                        BorrowerNumber = borrower.BorrowerNumber ?? "",
                        IsSelected = true
                    };
                    item.SelectionChanged += OnBorrowerSelectionChanged;
                    _allBorrowers.Add(item);
                }

                FilterBorrowers();
                UpdateSelectedCount();
                
                // 기존 호환성
                Borrowers.Clear();
                foreach (var borrower in borrowers)
                {
                    Borrowers.Add(borrower);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 차주 선택 변경 핸들러
        /// </summary>
        private void OnBorrowerSelectionChanged()
        {
            UpdateSelectedCount();
            
            // 전체 선택 체크박스 상태 업데이트
            var allSelected = _allBorrowers.All(b => b.IsSelected);
            var noneSelected = _allBorrowers.All(b => !b.IsSelected);
            
            if (allSelected)
            {
                _isAllSelected = true;
                OnPropertyChanged(nameof(IsAllSelected));
            }
            else if (noneSelected)
            {
                _isAllSelected = false;
                OnPropertyChanged(nameof(IsAllSelected));
            }
            
            _ = GenerateMonthlyCashFlowsAsync();
        }

        /// <summary>
        /// 월별 현금흐름 생성 (S-008)
        /// </summary>
        private async Task GenerateMonthlyCashFlowsAsync()
        {
            try
            {
                IsLoading = true;
                MonthlyCashFlowRows.Clear();

                var selectedBorrowers = _allBorrowers.Where(b => b.IsSelected).ToList();
                if (!selectedBorrowers.Any())
                {
                    TotalXnpv = 0;
                    TotalInflow = 0;
                    TotalOutflow = 0;
                    return;
                }

                decimal totalXnpv = 0;
                decimal totalInflow = 0;
                decimal totalOutflow = 0;
                var totalRow = new MonthlyCashFlowRow { BorrowerName = "합계", IsTotal = true };

                foreach (var borrowerItem in selectedBorrowers)
                {
                    // 대출 정보 조회
                    var loans = await _loanRepository.GetByBorrowerIdAsync(borrowerItem.BorrowerId);
                    
                    var row = new MonthlyCashFlowRow
                    {
                        BorrowerName = borrowerItem.BorrowerName,
                        BorrowerId = borrowerItem.BorrowerId,
                        IsTotal = false
                    };

                    // 각 대출의 예상 회수일/금액을 월별로 집계
                    foreach (var loan in loans)
                    {
                        var expectedDate = SelectedScenario == 1
                            ? loan.ExpectedDividendDate1 ?? ActionDate.AddMonths(6)
                            : loan.ExpectedDividendDate2 ?? ActionDate.AddMonths(12);

                        var monthKey = expectedDate.ToString("yyyy-MM");
                        
                        var loanCap = SelectedScenario == 1 ? loan.LoanCap1 : loan.LoanCap2;
                        var amount = loanCap ?? (loan.LoanPrincipalBalance ?? 0) * 0.8m;

                        if (MonthColumns.Contains(monthKey))
                        {
                            row[monthKey] += amount;
                            totalRow[monthKey] += amount;
                            totalInflow += amount;
                        }
                    }

                    // 초기 투자 (첫 월)
                    var firstMonth = MonthColumns.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstMonth))
                    {
                        var investment = loans.Sum(l => l.LoanPrincipalBalance ?? 0);
                        // 유출은 음수로 표시하지 않고 별도 관리
                        totalOutflow += investment;
                    }

                    MonthlyCashFlowRows.Add(row);
                }

                // 합계 행 추가
                MonthlyCashFlowRows.Add(totalRow);

                // XNPV 계산 (간단 버전)
                totalXnpv = totalInflow - totalOutflow;

                TotalXnpv = totalXnpv;
                TotalInflow = totalInflow;
                TotalOutflow = totalOutflow;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"현금흐름 생성 실패: {ex.Message}";
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
            await GenerateMonthlyCashFlowsAsync();
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (MonthlyCashFlowRows.Count == 0)
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
                    FileName = $"현금흐름집계_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 기존 ExcelService 사용 또는 새로운 월별 내보내기 구현
                    await _excelService.ExportCashFlowToExcelAsync(
                        CashFlowItems,
                        "전체",
                        DiscountRate,
                        TotalXnpv,
                        dialog.FileName);

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

        /// <summary>
        /// XNPV 재계산 (호환성)
        /// </summary>
        [RelayCommand]
        private async Task RecalculateAsync()
        {
            await GenerateMonthlyCashFlowsAsync();
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
