using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NPLogic.ViewModels.Evaluation
{
    /// <summary>
    /// 분기별 임대동향 항목
    /// </summary>
    public class QuarterlyRentalTrendItem
    {
        public string Quarter { get; set; } = "";
        public decimal RentalPriceIndex { get; set; }
        public decimal VacancyRate { get; set; }
        public decimal RentalPrice { get; set; }
    }
    
    /// <summary>
    /// 임대호가 항목
    /// </summary>
    public partial class RentalQuoteItem : ObservableObject
    {
        [ObservableProperty]
        private int _quoteNumber;
        
        [ObservableProperty]
        private decimal? _supplyAreaPyeong;
        
        [ObservableProperty]
        private decimal? _exclusiveAreaPyeong;
        
        [ObservableProperty]
        private decimal? _deposit;
        
        [ObservableProperty]
        private decimal? _monthlyRent;
        
        [ObservableProperty]
        private decimal? _unitRent;
        
        [ObservableProperty]
        private string? _floor;
        
        partial void OnMonthlyRentChanged(decimal? value)
        {
            CalculateUnitRent();
        }
        
        partial void OnExclusiveAreaPyeongChanged(decimal? value)
        {
            CalculateUnitRent();
        }
        
        private void CalculateUnitRent()
        {
            if (MonthlyRent.HasValue && ExclusiveAreaPyeong.HasValue && ExclusiveAreaPyeong.Value > 0)
            {
                UnitRent = MonthlyRent.Value / ExclusiveAreaPyeong.Value;
            }
        }
    }
    
    /// <summary>
    /// 상가/아파트형공장 평가 ViewModel
    /// </summary>
    public partial class EvaluationCommercialViewModel : ObservableObject
    {
        public EvaluationCommercialViewModel()
        {
            InitializeData();
        }
        
        #region 속성
        
        // === 상가구분 ===
        [ObservableProperty]
        private bool _isOffice;
        
        [ObservableProperty]
        private bool _isMediumLargeCommercial;
        
        [ObservableProperty]
        private bool _isSmallCommercial = true;
        
        [ObservableProperty]
        private bool _isCollectiveCommercial;
        
        [ObservableProperty]
        private bool _isNotApplicable;
        
        // === 임대수익률 ===
        [ObservableProperty]
        private decimal? _incomeYieldRate = 0.045m;
        
        [ObservableProperty]
        private decimal? _capitalYieldRate = 0.012m;
        
        [ObservableProperty]
        private decimal? _investmentYieldRate = 0.057m;
        
        // === 분기별 임대동향 ===
        [ObservableProperty]
        private ObservableCollection<QuarterlyRentalTrendItem> _quarterlyRentalTrends = new();
        
        // === 층별효용비율 ===
        [ObservableProperty]
        private decimal _floorEfficiencyB1 = 0.55m;
        
        [ObservableProperty]
        private decimal _floorEfficiency1 = 1.0m;
        
        [ObservableProperty]
        private decimal _floorEfficiency2 = 0.85m;
        
        [ObservableProperty]
        private decimal _floorEfficiency3 = 0.75m;
        
        [ObservableProperty]
        private decimal _floorEfficiency4 = 0.70m;
        
        [ObservableProperty]
        private decimal _floorEfficiency5to7 = 0.65m;
        
        [ObservableProperty]
        private decimal _floorEfficiency8to10 = 0.60m;
        
        [ObservableProperty]
        private decimal _floorEfficiency11plus = 0.55m;
        
        // === 임대호가분석 ===
        [ObservableProperty]
        private ObservableCollection<RentalQuoteItem> _rentalQuotes = new();
        
        // === 수익가치 분석 ===
        [ObservableProperty]
        private decimal _baseDiscountRate = 0.06m;
        
        [ObservableProperty]
        private decimal _sensitivityBasis = 0.005m;
        
        [ObservableProperty]
        private decimal? _incomeValue55;
        
        [ObservableProperty]
        private decimal? _incomeValue60;
        
        [ObservableProperty]
        private decimal? _incomeValue65;
        
        [ObservableProperty]
        private decimal? _incomeValue;
        
        [ObservableProperty]
        private decimal? _unitIncomeValue;
        
        // === 무상임대분석 ===
        [ObservableProperty]
        private int _freeRentMonths1 = 6;
        
        [ObservableProperty]
        private int _freeRentMonths2 = 3;
        
        [ObservableProperty]
        private int _freeRentMonths3 = 0;
        
        [ObservableProperty]
        private int _freeRentMonths4 = 0;
        
        [ObservableProperty]
        private int _freeRentMonths5 = 0;
        
        [ObservableProperty]
        private decimal? _irrValue = 0.052m;
        
        [ObservableProperty]
        private decimal? _totalValue;
        
        // === 낙찰통계 ===
        [ObservableProperty]
        private string? _regionName1 = "서울특별시";
        
        [ObservableProperty]
        private string? _regionName2 = "강남구";
        
        [ObservableProperty]
        private string? _regionName3 = "역삼동";
        
        [ObservableProperty]
        private string? _stats1Year_Display1 = "72.5% / 89건";
        
        [ObservableProperty]
        private string? _stats1Year_Display2 = "70.8% / 23건";
        
        [ObservableProperty]
        private string? _stats1Year_Display3 = "68.2% / 8건";
        
        [ObservableProperty]
        private string? _stats6Month_Display1 = "71.2% / 42건";
        
        [ObservableProperty]
        private string? _stats6Month_Display2 = "69.5% / 11건";
        
        [ObservableProperty]
        private string? _stats6Month_Display3 = "67.0% / 4건";
        
        [ObservableProperty]
        private string? _stats3Month_Display1 = "70.0% / 18건";
        
        [ObservableProperty]
        private string? _stats3Month_Display2 = "68.5% / 5건";
        
        [ObservableProperty]
        private string? _stats3Month_Display3 = "65.5% / 2건";
        
        [ObservableProperty]
        private decimal? _appliedBidRate = 0.68m;
        
        // === 선순위관리비 ===
        [ObservableProperty]
        private string? _managementOfficePhone;
        
        [ObservableProperty]
        private decimal? _arrearsFee;
        
        [ObservableProperty]
        private decimal? _monthlyFee;
        
        [ObservableProperty]
        private decimal? _scenario1EstimatedFee;
        
        [ObservableProperty]
        private decimal? _scenario2EstimatedFee;
        
        #endregion
        
        #region 계산된 속성
        
        public decimal AverageUnitRent => 
            RentalQuotes.Count > 0 
                ? RentalQuotes.Where(x => x.UnitRent.HasValue).Average(x => x.UnitRent ?? 0) 
                : 0;
        
        #endregion
        
        #region 초기화
        
        private void InitializeData()
        {
            // 분기별 임대동향 샘플 데이터
            QuarterlyRentalTrends = new ObservableCollection<QuarterlyRentalTrendItem>
            {
                new QuarterlyRentalTrendItem { Quarter = "2025 Q4", RentalPriceIndex = 102.5m, VacancyRate = 0.082m, RentalPrice = 28.5m },
                new QuarterlyRentalTrendItem { Quarter = "2025 Q3", RentalPriceIndex = 101.8m, VacancyRate = 0.085m, RentalPrice = 28.2m },
                new QuarterlyRentalTrendItem { Quarter = "2025 Q2", RentalPriceIndex = 101.2m, VacancyRate = 0.088m, RentalPrice = 27.9m },
                new QuarterlyRentalTrendItem { Quarter = "2025 Q1", RentalPriceIndex = 100.5m, VacancyRate = 0.091m, RentalPrice = 27.5m }
            };
            
            // 임대호가 샘플 데이터
            RentalQuotes = new ObservableCollection<RentalQuoteItem>
            {
                new RentalQuoteItem { QuoteNumber = 1, SupplyAreaPyeong = 32.5m, ExclusiveAreaPyeong = 28.2m, Deposit = 50000000, MonthlyRent = 2200000, Floor = "1층" },
                new RentalQuoteItem { QuoteNumber = 2, SupplyAreaPyeong = 35.0m, ExclusiveAreaPyeong = 30.5m, Deposit = 55000000, MonthlyRent = 2400000, Floor = "2층" },
                new RentalQuoteItem { QuoteNumber = 3, SupplyAreaPyeong = 28.0m, ExclusiveAreaPyeong = 24.2m, Deposit = 40000000, MonthlyRent = 1800000, Floor = "B1층" },
                new RentalQuoteItem { QuoteNumber = 4, SupplyAreaPyeong = 38.0m, ExclusiveAreaPyeong = 33.0m, Deposit = 60000000, MonthlyRent = 2600000, Floor = "1층" },
                new RentalQuoteItem { QuoteNumber = 5 }
            };
            
            CalculateIncomeValues();
        }
        
        #endregion
        
        #region 계산 메서드
        
        private void CalculateIncomeValues()
        {
            // 민감도 분석 계산 (수익가치)
            var annualRent = AverageUnitRent * 12 * 30; // 가정: 30평 기준
            
            IncomeValue55 = annualRent > 0 ? annualRent / 0.055m : null;
            IncomeValue60 = annualRent > 0 ? annualRent / 0.060m : null;
            IncomeValue65 = annualRent > 0 ? annualRent / 0.065m : null;
            
            IncomeValue = IncomeValue60;
            UnitIncomeValue = IncomeValue.HasValue ? IncomeValue.Value / 30 : null;
        }
        
        partial void OnMonthlyFeeChanged(decimal? value)
        {
            CalculateEstimatedFees();
        }
        
        partial void OnArrearsFeeChanged(decimal? value)
        {
            CalculateEstimatedFees();
        }
        
        private void CalculateEstimatedFees()
        {
            if (ArrearsFee.HasValue && MonthlyFee.HasValue)
            {
                // 시나리오1: 체납관리비 + 6개월 예상 관리비
                Scenario1EstimatedFee = ArrearsFee.Value + (MonthlyFee.Value * 6);
                // 시나리오2: 체납관리비 + 9개월 예상 관리비
                Scenario2EstimatedFee = ArrearsFee.Value + (MonthlyFee.Value * 9);
            }
        }
        
        #endregion
        
        #region 명령
        
        [RelayCommand]
        private void OpenROneMap()
        {
            try
            {
                var url = "https://www.r-one.co.kr/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
        
        [RelayCommand]
        private void LoadCommercialDistrictMap()
        {
            System.Windows.MessageBox.Show(
                "상권지도 연동 기능은 추후 구현 예정입니다.\n한국부동산원 R-ONE 상권정보를 가져옵니다.",
                "상권지도",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        [RelayCommand]
        private void FetchRentalTrend()
        {
            System.Windows.MessageBox.Show(
                "임대동향 조회 기능은 추후 구현 예정입니다.\n한국부동산원 R-ONE API를 통해 임대동향 데이터를 가져옵니다.",
                "임대동향",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        #endregion
    }
}
