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
    /// Loan(Ⅰ) 시트 ViewModel
    /// 4개 탭: 일반, 일반보증, 해지부보증, 일반+해지부보증
    /// </summary>
    public partial class LoanSheetViewModel : ObservableObject
    {
        private readonly LoanRepository _loanRepository;
        private readonly BorrowerRepository _borrowerRepository;
        private readonly AuthService _authService;
        private readonly ExcelService _excelService;

        // ========== 대출 데이터 ==========

        [ObservableProperty]
        private ObservableCollection<Loan> _loans = new();

        [ObservableProperty]
        private Loan? _selectedLoan;

        // ========== 차주 데이터 ==========

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

        // ========== 기준일 및 시나리오 날짜 ==========

        [ObservableProperty]
        private DateTime _cDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _scenario1Date = DateTime.Today.AddMonths(6);

        [ObservableProperty]
        private DateTime _scenario2Date = DateTime.Today.AddMonths(12);

        // ========== 보증서 요약 데이터 ==========

        [ObservableProperty]
        private ObservableCollection<GuaranteeSummaryItem> _guaranteeSummaryItems = new();

        // ========== 보증여부별 합계 ==========

        [ObservableProperty]
        private GuaranteeTypeTotals _guaranteeTypeTotals = new();

        // ========== 배분대상금액 안분 (토지/건물/기계/제시외/당해시설) ==========

        [ObservableProperty]
        private ObservableCollection<AllocationItem> _allocationItems = new();

        // ========== 배당가능재원 ==========

        [ObservableProperty]
        private DividendFundData _dividendFund1 = new();  // 1안

        [ObservableProperty]
        private DividendFundData _dividendFund2 = new();  // 2안

        // ========== 안분비율/보증기관 안분액 ==========

        [ObservableProperty]
        private ObservableCollection<ProrationItem> _prorationItems = new();

        [ObservableProperty]
        private ObservableCollection<ProrationItem> _prorationItems2 = new();  // 2차 안분 (일반+해지부보증용)

        // ========== MCI 보증 ==========

        [ObservableProperty]
        private MciData _mciData1 = new();  // 1안

        [ObservableProperty]
        private MciData _mciData2 = new();  // 2안

        // ========== 상태 ==========

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string _currentSheetType = "Basic";

        // ========== 통계 ==========

        [ObservableProperty]
        private LoanStatistics? _statistics;

        public LoanSheetViewModel(
            LoanRepository loanRepository,
            BorrowerRepository borrowerRepository,
            AuthService authService,
            ExcelService excelService)
        {
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));

            InitializeAllocationItems();
        }

        /// <summary>
        /// 배분대상금액 안분 초기화
        /// </summary>
        private void InitializeAllocationItems()
        {
            AllocationItems = new ObservableCollection<AllocationItem>
            {
                new() { Category = "토지" },
                new() { Category = "건물" },
                new() { Category = "기계" },
                new() { Category = "제시외" },
                new() { Category = "당해시설 (일부인경우 평가자입력)" }
            };
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
                    await LoadSingleBorrowerDataAsync();
                }
                else
                {
                    await LoadBorrowersAsync();
                }

                CalculateAllLoanCaps();
                UpdateSummaries();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[LoanSheetViewModel] InitializeAsync 오류: {ex}");
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
                var allBorrowers = await _borrowerRepository.GetAllAsync();
                var matchingBorrower = allBorrowers.FirstOrDefault(b =>
                    b.BorrowerNumber == SelectedProperty.BorrowerNumber ||
                    b.BorrowerName == SelectedProperty.DebtorName);

                Borrowers.Clear();

                if (matchingBorrower != null)
                {
                    Borrowers.Add(matchingBorrower);
                    SelectedBorrower = matchingBorrower;
                    await LoadLoansAsync();
                }
                else
                {
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

                await LoadLoansAsync();
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
            if (SelectedBorrower == null || SelectedBorrower.Id == Guid.Empty)
            {
                Loans.Clear();
                Statistics = null;
                return;
            }

            try
            {
                var loans = await _loanRepository.GetByBorrowerIdAsync(SelectedBorrower.Id);

                Loans.Clear();
                foreach (var loan in loans)
                {
                    Loans.Add(loan);
                }

                Statistics = await _loanRepository.GetStatisticsByBorrowerIdAsync(SelectedBorrower.Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대출 목록 로드 실패: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[LoanSheetViewModel] LoadLoansAsync 오류: {ex}");
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
        /// 모든 대출의 Loan Cap 계산
        /// </summary>
        private void CalculateAllLoanCaps()
        {
            foreach (var loan in Loans)
            {
                loan.ExpectedDividendDate1 = Scenario1Date;
                loan.ExpectedDividendDate2 = Scenario2Date;
                loan.CalculateScenario1();
                loan.CalculateScenario2();
            }
        }

        /// <summary>
        /// 보증서 요약 및 합계 업데이트
        /// </summary>
        private void UpdateSummaries()
        {
            UpdateGuaranteeSummary();
            UpdateGuaranteeTypeTotals();
        }

        /// <summary>
        /// 보증서 요약 업데이트
        /// </summary>
        private void UpdateGuaranteeSummary()
        {
            GuaranteeSummaryItems.Clear();

            foreach (var loan in Loans.Where(l => l.HasValidGuarantee || l.HasPriorSubrogation))
            {
                var item = new GuaranteeSummaryItem
                {
                    AccountSerial = loan.AccountSerial ?? "-",
                    GuaranteeOrganization = loan.GuaranteeOrganization,
                    GuaranteeBalance = loan.SubrogationAmount ?? 0,
                    TerminatedBondAmount = loan.IsTerminatedGuarantee ? (loan.LoanPrincipalBalance ?? 0) * 0.2m : 0, // 예시
                    GuaranteeRatio = 0.8m, // 예시
                    HasSubrogation = loan.HasPriorSubrogation,
                    SubrogationExpectedDate = loan.ExpectedDividendDate1,
                    OverdueInterestRate = loan.OverdueInterestRate ?? 0,
                    NormalInterestRate = loan.NormalInterestRate ?? 0,
                    SubrogationPrincipal = loan.SubrogationAmount ?? 0,
                    SubrogationAmount = loan.SubrogationAmountCalculation ?? 0
                };

                GuaranteeSummaryItems.Add(item);
            }
        }

        /// <summary>
        /// 보증여부별 합계 업데이트
        /// </summary>
        private void UpdateGuaranteeTypeTotals()
        {
            var totals = new GuaranteeTypeTotals();

            foreach (var loan in Loans)
            {
                var guaranteeType = GetGuaranteeType(loan);
                var lc1 = loan.LoanCap1 ?? 0;
                var lc2 = loan.LoanCap2 ?? 0;

                switch (guaranteeType)
                {
                    case GuaranteeTypeEnum.NonGuarantee:
                        totals.NonGuaranteePrincipal += loan.LoanPrincipalBalance ?? 0;
                        totals.NonGuaranteeInterest += loan.AccruedInterest;
                        totals.NonGuaranteeOverdue1 += loan.OverdueInterest1 ?? 0;
                        totals.NonGuaranteeOverdue2 += loan.OverdueInterest2 ?? 0;
                        totals.NonGuaranteeLC1 += lc1;
                        totals.NonGuaranteeLC2 += lc2;
                        break;
                    case GuaranteeTypeEnum.NormalGuarantee:
                        totals.NormalGuaranteePrincipal += loan.LoanPrincipalBalance ?? 0;
                        totals.NormalGuaranteeInterest += loan.AccruedInterest;
                        totals.NormalGuaranteeOverdue1 += loan.OverdueInterest1 ?? 0;
                        totals.NormalGuaranteeOverdue2 += loan.OverdueInterest2 ?? 0;
                        totals.NormalGuaranteeLC1 += lc1;
                        totals.NormalGuaranteeLC2 += lc2;
                        break;
                    case GuaranteeTypeEnum.TerminatedGuarantee:
                        totals.TerminatedGuaranteePrincipal += loan.LoanPrincipalBalance ?? 0;
                        totals.TerminatedGuaranteeInterest += loan.AccruedInterest;
                        totals.TerminatedGuaranteeOverdue1 += loan.OverdueInterest1 ?? 0;
                        totals.TerminatedGuaranteeOverdue2 += loan.OverdueInterest2 ?? 0;
                        totals.TerminatedGuaranteeLC1 += lc1;
                        totals.TerminatedGuaranteeLC2 += lc2;
                        break;
                    case GuaranteeTypeEnum.NormalValidGuarantee:
                        totals.NormalValidGuaranteePrincipal += loan.LoanPrincipalBalance ?? 0;
                        totals.NormalValidGuaranteeInterest += loan.AccruedInterest;
                        totals.NormalValidGuaranteeOverdue1 += loan.OverdueInterest1 ?? 0;
                        totals.NormalValidGuaranteeOverdue2 += loan.OverdueInterest2 ?? 0;
                        totals.NormalValidGuaranteeLC1 += lc1;
                        totals.NormalValidGuaranteeLC2 += lc2;
                        break;
                    case GuaranteeTypeEnum.TerminatedValidGuarantee:
                        totals.TerminatedValidGuaranteePrincipal += loan.LoanPrincipalBalance ?? 0;
                        totals.TerminatedValidGuaranteeInterest += loan.AccruedInterest;
                        totals.TerminatedValidGuaranteeOverdue1 += loan.OverdueInterest1 ?? 0;
                        totals.TerminatedValidGuaranteeOverdue2 += loan.OverdueInterest2 ?? 0;
                        totals.TerminatedValidGuaranteeLC1 += lc1;
                        totals.TerminatedValidGuaranteeLC2 += lc2;
                        break;
                    case GuaranteeTypeEnum.TerminatedBond:
                        totals.TerminatedBondPrincipal += loan.LoanPrincipalBalance ?? 0;
                        totals.TerminatedBondInterest += loan.AccruedInterest;
                        totals.TerminatedBondOverdue1 += loan.OverdueInterest1 ?? 0;
                        totals.TerminatedBondOverdue2 += loan.OverdueInterest2 ?? 0;
                        totals.TerminatedBondLC1 += lc1;
                        totals.TerminatedBondLC2 += lc2;
                        break;
                }
            }

            GuaranteeTypeTotals = totals;
        }

        /// <summary>
        /// 대출의 보증 유형 판별
        /// </summary>
        private GuaranteeTypeEnum GetGuaranteeType(Loan loan)
        {
            // (피담보Y, 유효보증서N, 기대위변제N) => 비보증부
            if (!loan.HasValidGuarantee && !loan.HasPriorSubrogation)
                return GuaranteeTypeEnum.NonGuarantee;

            // (피담보Y, 유효보증서N, 기대위변제Y, 해지부N) => 일반보증
            if (!loan.HasValidGuarantee && loan.HasPriorSubrogation && !loan.IsTerminatedGuarantee)
                return GuaranteeTypeEnum.NormalGuarantee;

            // (피담보Y, 유효보증서N, 기대위변제Y, 해지부Y) => 해지부보증
            if (!loan.HasValidGuarantee && loan.HasPriorSubrogation && loan.IsTerminatedGuarantee)
                return GuaranteeTypeEnum.TerminatedGuarantee;

            // (피담보Y, 유효보증서Y, 기대위변제N, 해지부N) => 일반 유효보증
            if (loan.HasValidGuarantee && !loan.HasPriorSubrogation && !loan.IsTerminatedGuarantee)
                return GuaranteeTypeEnum.NormalValidGuarantee;

            // (피담보Y, 유효보증서Y, 기대위변제N, 해지부Y) => 해지부 유효보증
            if (loan.HasValidGuarantee && !loan.HasPriorSubrogation && loan.IsTerminatedGuarantee)
                return GuaranteeTypeEnum.TerminatedValidGuarantee;

            return GuaranteeTypeEnum.NonGuarantee;
        }

        // ========== Commands ==========

        /// <summary>
        /// 전체 재계산
        /// </summary>
        [RelayCommand]
        private async Task RecalculateAllAsync()
        {
            try
            {
                IsLoading = true;

                foreach (var loan in Loans)
                {
                    loan.ExpectedDividendDate1 = Scenario1Date;
                    loan.ExpectedDividendDate2 = Scenario2Date;
                    loan.CalculateScenario1();
                    loan.CalculateScenario2();
                    await _loanRepository.UpdateAsync(loan);
                }

                UpdateSummaries();
                OnPropertyChanged(nameof(Loans));

                NPLogic.UI.Services.ToastService.Instance.ShowSuccess($"{Loans.Count}건의 Loan Cap이 재계산되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"재계산 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;

                foreach (var loan in Loans)
                {
                    loan.TotalClaimAmount = loan.CalculateTotalClaim();
                    await _loanRepository.UpdateAsync(loan);
                }

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
                    FileName = $"Loan_{CurrentSheetType}_{SelectedBorrower?.BorrowerName ?? "전체"}_{DateTime.Now:yyyyMMdd}.xlsx"
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

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }
    }

    // ========== 보조 모델 클래스 ==========

    /// <summary>
    /// 보증서 요약 항목
    /// </summary>
    public class GuaranteeSummaryItem
    {
        public string AccountSerial { get; set; } = "";
        public string? GuaranteeOrganization { get; set; }
        public decimal GuaranteeBalance { get; set; }
        public decimal TerminatedBondAmount { get; set; }
        public decimal GuaranteeRatio { get; set; }
        public bool HasSubrogation { get; set; }
        public DateTime? SubrogationExpectedDate { get; set; }
        public decimal OverdueInterestRate { get; set; }
        public decimal NormalInterestRate { get; set; }
        public decimal SubrogationPrincipal { get; set; }
        public int InterestDays { get; set; }
        public decimal AgreedInterest { get; set; }
        public decimal SubrogationAmount { get; set; }
        public decimal InterestDifference { get; set; }
        public decimal OverdueInterest1 { get; set; }
        public decimal OverdueInterest2 { get; set; }
        public decimal GuaranteeLoanCap1 { get; set; }
        public decimal GuaranteeLoanCap2 { get; set; }
    }

    /// <summary>
    /// 보증 유형 열거형
    /// </summary>
    public enum GuaranteeTypeEnum
    {
        NonGuarantee,           // 비보증부
        NormalGuarantee,        // 일반보증
        TerminatedGuarantee,    // 해지부보증
        NormalValidGuarantee,   // 일반 유효보증
        TerminatedValidGuarantee, // 해지부 유효보증
        TerminatedBond          // 해지부해지채권
    }

    /// <summary>
    /// 보증여부별 합계
    /// </summary>
    public class GuaranteeTypeTotals : ObservableObject
    {
        // 비보증부
        public decimal NonGuaranteePrincipal { get; set; }
        public decimal NonGuaranteeInterest { get; set; }
        public decimal NonGuaranteeOverdue1 { get; set; }
        public decimal NonGuaranteeOverdue2 { get; set; }
        public decimal NonGuaranteeLC1 { get; set; }
        public decimal NonGuaranteeLC2 { get; set; }

        // 일반보증
        public decimal NormalGuaranteePrincipal { get; set; }
        public decimal NormalGuaranteeInterest { get; set; }
        public decimal NormalGuaranteeOverdue1 { get; set; }
        public decimal NormalGuaranteeOverdue2 { get; set; }
        public decimal NormalGuaranteeLC1 { get; set; }
        public decimal NormalGuaranteeLC2 { get; set; }

        // 해지부보증
        public decimal TerminatedGuaranteePrincipal { get; set; }
        public decimal TerminatedGuaranteeInterest { get; set; }
        public decimal TerminatedGuaranteeOverdue1 { get; set; }
        public decimal TerminatedGuaranteeOverdue2 { get; set; }
        public decimal TerminatedGuaranteeLC1 { get; set; }
        public decimal TerminatedGuaranteeLC2 { get; set; }

        // 일반 유효보증
        public decimal NormalValidGuaranteePrincipal { get; set; }
        public decimal NormalValidGuaranteeInterest { get; set; }
        public decimal NormalValidGuaranteeOverdue1 { get; set; }
        public decimal NormalValidGuaranteeOverdue2 { get; set; }
        public decimal NormalValidGuaranteeLC1 { get; set; }
        public decimal NormalValidGuaranteeLC2 { get; set; }

        // 해지부 유효보증
        public decimal TerminatedValidGuaranteePrincipal { get; set; }
        public decimal TerminatedValidGuaranteeInterest { get; set; }
        public decimal TerminatedValidGuaranteeOverdue1 { get; set; }
        public decimal TerminatedValidGuaranteeOverdue2 { get; set; }
        public decimal TerminatedValidGuaranteeLC1 { get; set; }
        public decimal TerminatedValidGuaranteeLC2 { get; set; }

        // 해지부해지채권
        public decimal TerminatedBondPrincipal { get; set; }
        public decimal TerminatedBondInterest { get; set; }
        public decimal TerminatedBondOverdue1 { get; set; }
        public decimal TerminatedBondOverdue2 { get; set; }
        public decimal TerminatedBondLC1 { get; set; }
        public decimal TerminatedBondLC2 { get; set; }

        // 합계
        public decimal TotalLC1 => NonGuaranteeLC1 + NormalGuaranteeLC1 + TerminatedGuaranteeLC1 +
                                   NormalValidGuaranteeLC1 + TerminatedValidGuaranteeLC1 + TerminatedBondLC1;
        public decimal TotalLC2 => NonGuaranteeLC2 + NormalGuaranteeLC2 + TerminatedGuaranteeLC2 +
                                   NormalValidGuaranteeLC2 + TerminatedValidGuaranteeLC2 + TerminatedBondLC2;
    }

    /// <summary>
    /// 배분대상금액 안분 항목
    /// </summary>
    public partial class AllocationItem : ObservableObject
    {
        public string Category { get; set; } = "";
        
        [ObservableProperty]
        private decimal _appraisalValue;
        
        [ObservableProperty]
        private bool _isThisFacility;
        
        [ObservableProperty]
        private decimal _dividendFund1;
        
        [ObservableProperty]
        private decimal _dividendFund2;
        
        [ObservableProperty]
        private bool _hasFirstMortgage;
        
        [ObservableProperty]
        private bool _hasSecondMortgage;
        
        [ObservableProperty]
        private bool _hasThirdMortgage;
    }

    /// <summary>
    /// 배당가능재원 데이터
    /// </summary>
    public partial class DividendFundData : ObservableObject
    {
        [ObservableProperty]
        private decimal _thisFacilityFund;  // 당해시설 배당가능재원
        
        [ObservableProperty]
        private decimal _nonThisFacilityFund;  // 당해시설 외 배당가능재원
        
        [ObservableProperty]
        private decimal _nonGuaranteeTotal;  // 비보증부대출합계
        
        [ObservableProperty]
        private decimal _interestDifference;  // 연체이자, 약정이자차이
        
        [ObservableProperty]
        private decimal _allocationAmount;  // 안분대상금액
    }

    /// <summary>
    /// 안분비율/보증기관 안분액 항목
    /// </summary>
    public partial class ProrationItem : ObservableObject
    {
        public string GuaranteeOrganization { get; set; } = "";
        
        [ObservableProperty]
        private decimal _subrogationPrincipal;  // 대위변제원금
        
        [ObservableProperty]
        private decimal _prorationRatio;  // 안분비율
        
        [ObservableProperty]
        private decimal _allocationAmount1;  // 1안 안분액
        
        [ObservableProperty]
        private decimal _allocationAmount2;  // 2안 안분액
    }

    /// <summary>
    /// MCI 보증 데이터
    /// </summary>
    public partial class MciData : ObservableObject
    {
        [ObservableProperty]
        private decimal _mciInitialAmount;  // MCI최초가입금액
        
        [ObservableProperty]
        private string? _bondNumber;  // 채권번호
        
        [ObservableProperty]
        private decimal _mciBalance;  // MCI가입잔액
        
        [ObservableProperty]
        private DateTime? _lastInterestDate;  // 최종이수일
        
        [ObservableProperty]
        private DateTime? _expectedDividendDate;  // 예상배당일
        
        [ObservableProperty]
        private int _days;  // 일수
        
        [ObservableProperty]
        private decimal _normalInterestRate;  // 정상이자율
        
        [ObservableProperty]
        private decimal _targetPrincipal;  // 인수대상원금
        
        [ObservableProperty]
        private decimal _validCollateralValue;  // 유효담보가
        
        [ObservableProperty]
        private decimal _validCollateralInterest;  // 유효담보가의 이자
        
        [ObservableProperty]
        private decimal _validCollateralInterestLimit;  // 유효담보가의 이자한도
        
        [ObservableProperty]
        private decimal _validCollateralDividend;  // 유효담보가 배당액
        
        [ObservableProperty]
        private decimal _expectedDividend;  // 예상배당금
        
        [ObservableProperty]
        private decimal _mciNormalInterest;  // MCI 정상이자
        
        [ObservableProperty]
        private decimal _remainingMciBalance;  // 배당으로 충당되지 않은 MCI 잔액
        
        [ObservableProperty]
        private decimal _claimableAmount;  // 청구가능금액
        
        [ObservableProperty]
        private decimal _postDividendLoss;  // 배당후 손실액
        
        [ObservableProperty]
        private decimal _mciClaimAmount;  // MCI 청구액
    }
}
