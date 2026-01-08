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
    /// 담보총괄 ViewModel
    /// </summary>
    public partial class CollateralSummaryViewModel : ObservableObject
    {
        private readonly BorrowerRepository _borrowerRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly AuthService _authService;
        private readonly ExcelService _excelService;

        [ObservableProperty]
        private ObservableCollection<Borrower> _borrowers = new();

        [ObservableProperty]
        private Borrower? _selectedBorrower;

        [ObservableProperty]
        private ObservableCollection<CollateralItem> _collateralItems = new();

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string? _selectedProgramId;

        [ObservableProperty]
        private ObservableCollection<string> _programs = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // 합계 정보
        [ObservableProperty]
        private int _totalPropertyCount;

        [ObservableProperty]
        private decimal _totalAppraisalValue;

        [ObservableProperty]
        private decimal _totalEstimatedValue;

        [ObservableProperty]
        private decimal _totalSeniorRights;

        [ObservableProperty]
        private decimal _totalRecoverable;

        [ObservableProperty]
        private decimal _loanCap;

        [ObservableProperty]
        private decimal _capAdjustedAmount;

        public CollateralSummaryViewModel(
            BorrowerRepository borrowerRepository,
            PropertyRepository propertyRepository,
            AuthService authService,
            ExcelService excelService)
        {
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
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

                await LoadProgramsAsync();
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
                var programId = SelectedProgramId == "전체" ? null : SelectedProgramId;
                var searchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

                var borrowers = await _borrowerRepository.GetFilteredAsync(
                    programId: programId,
                    searchText: searchText
                );

                Borrowers.Clear();
                foreach (var borrower in borrowers)
                {
                    Borrowers.Add(borrower);
                }

                // 첫 번째 차주 선택 (항상 선택)
                if (Borrowers.Count > 0)
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
        /// 선택된 차주의 담보 물건 로드
        /// </summary>
        private async Task LoadCollateralItemsAsync()
        {
            if (SelectedBorrower == null)
            {
                CollateralItems.Clear();
                ClearTotals();
                return;
            }

            try
            {
                IsLoading = true;

                // 차주명으로 물건 매칭
                var allProperties = await _propertyRepository.GetAllAsync();
                var matchingProperties = allProperties
                    .Where(p => p.DebtorName == SelectedBorrower.BorrowerName)
                    .OrderBy(p => p.PropertyNumber)
                    .ToList();

                CollateralItems.Clear();
                foreach (var property in matchingProperties)
                {
                    var item = new CollateralItem
                    {
                        PropertyId = property.Id,
                        PropertyNumber = property.PropertyNumber ?? "",
                        Address = property.AddressFull ?? property.AddressRoad ?? "",
                        CollateralType = property.PropertyType ?? "기타",
                        LandArea = property.LandArea ?? 0,
                        BuildingArea = property.BuildingArea ?? 0,
                        MachineryValue = 0, // TODO: 기계기구 필드 추가 시
                        IsFactoryMortgage = false, // TODO: 공장저당여부 필드 추가 시
                        AppraisalValue = property.AppraisalValue ?? 0,
                        EstimatedValue = property.SalePrice ?? property.AppraisalValue ?? 0,
                        SeniorRights = 0, // TODO: 선순위 필드 추가 시
                        Status = property.Status
                    };

                    // 배당가능재원 계산
                    item.RecoverableAmount = Math.Max(0, item.EstimatedValue - item.SeniorRights);
                    
                    // 진행률 계산
                    item.ProgressPercent = property.GetProgressPercent();

                    CollateralItems.Add(item);
                }

                // 합계 계산
                CalculateTotals();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"담보 물건 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 합계 계산
        /// </summary>
        private void CalculateTotals()
        {
            TotalPropertyCount = CollateralItems.Count;
            TotalAppraisalValue = CollateralItems.Sum(x => x.AppraisalValue);
            TotalEstimatedValue = CollateralItems.Sum(x => x.EstimatedValue);
            TotalSeniorRights = CollateralItems.Sum(x => x.SeniorRights);
            TotalRecoverable = CollateralItems.Sum(x => x.RecoverableAmount);

            // Loan Cap 적용 (차주의 OPB를 Cap으로 사용)
            LoanCap = SelectedBorrower?.Opb ?? 0;
            CapAdjustedAmount = Math.Min(TotalRecoverable, LoanCap);
        }

        /// <summary>
        /// 합계 초기화
        /// </summary>
        private void ClearTotals()
        {
            TotalPropertyCount = 0;
            TotalAppraisalValue = 0;
            TotalEstimatedValue = 0;
            TotalSeniorRights = 0;
            TotalRecoverable = 0;
            LoanCap = 0;
            CapAdjustedAmount = 0;
        }

        /// <summary>
        /// 선택된 차주 변경 시
        /// </summary>
        partial void OnSelectedBorrowerChanged(Borrower? value)
        {
            _ = LoadCollateralItemsAsync();
        }

        /// <summary>
        /// 프로그램 선택 변경 시
        /// </summary>
        partial void OnSelectedProgramIdChanged(string? value)
        {
            _ = ApplyFiltersAsync();
        }

        /// <summary>
        /// 검색
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadBorrowersAsync();
        }

        /// <summary>
        /// 필터 적용
        /// </summary>
        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            SelectedBorrower = null;
            await LoadBorrowersAsync();
        }

        /// <summary>
        /// 물건 상세 보기
        /// </summary>
        [RelayCommand]
        private void ViewPropertyDetail(CollateralItem item)
        {
            if (item == null) return;

            // Property 객체 찾기
            var property = new Property { Id = item.PropertyId };
            // 뒤로가기 시 담보총괄로 돌아가도록 콜백 전달
            MainWindow.Instance?.NavigateToPropertyDetail(property, () => MainWindow.Instance?.NavigateToCollateralSummary());
        }

        /// <summary>
        /// 담보물건 크게보기 (메인 화면 전체로 물건목록 표시)
        /// </summary>
        [RelayCommand]
        private void ViewCollateralDetail()
        {
            // 물건 목록 페이지(CollateralSummaryView)를 전체 화면으로 표시
            MainWindow.Instance?.NavigateToCollateralSummary();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadBorrowersAsync();
            await LoadCollateralItemsAsync();
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (CollateralItems.Count == 0)
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
                    FileName = $"담보총괄_{SelectedBorrower?.BorrowerName ?? "전체"}_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _excelService.ExportCollateralSummaryToExcelAsync(
                        CollateralItems, 
                        SelectedBorrower?.BorrowerName ?? "전체", 
                        LoanCap, 
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
    }

    /// <summary>
    /// 담보 물건 항목 (표시용)
    /// </summary>
    public class CollateralItem
    {
        public Guid PropertyId { get; set; }
        public string PropertyNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string CollateralType { get; set; } = "";
        public decimal LandArea { get; set; }
        public decimal BuildingArea { get; set; }
        public decimal MachineryValue { get; set; }
        public bool IsFactoryMortgage { get; set; }
        public decimal AppraisalValue { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal SeniorRights { get; set; }
        public decimal RecoverableAmount { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = "";

        /// <summary>
        /// 총 면적
        /// </summary>
        public decimal TotalArea => LandArea + BuildingArea;

        /// <summary>
        /// 공장저당 여부 표시
        /// </summary>
        public string FactoryMortgageDisplay => IsFactoryMortgage ? "Y" : "N";

        /// <summary>
        /// 진행률 표시
        /// </summary>
        public string ProgressDisplay => $"{ProgressPercent}%";
    }
}

