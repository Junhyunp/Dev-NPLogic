using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NPLogic.ViewModels.Evaluation
{
    /// <summary>
    /// 임대차 정보 항목
    /// </summary>
    public partial class LeaseInfoItem : ObservableObject
    {
        [ObservableProperty]
        private string _tenantName = "";
        
        [ObservableProperty]
        private decimal? _deposit;
        
        [ObservableProperty]
        private decimal? _monthlyRent;
        
        [ObservableProperty]
        private DateTime? _leaseStartDate;
        
        [ObservableProperty]
        private DateTime? _leaseEndDate;
        
        [ObservableProperty]
        private DateTime? _confirmationDate;
        
        [ObservableProperty]
        private decimal? _annualRent;
        
        partial void OnMonthlyRentChanged(decimal? value)
        {
            if (value.HasValue)
            {
                AnnualRent = value.Value * 12;
            }
        }
    }
    
    /// <summary>
    /// 주택/근린시설/토지 평가 ViewModel
    /// </summary>
    public partial class EvaluationHouseLandViewModel : ObservableObject
    {
        public EvaluationHouseLandViewModel()
        {
            InitializeData();
        }
        
        #region 속성
        
        // === 탐문결과 ===
        [ObservableProperty]
        private decimal? _inquiryLandUnitPrice;
        
        [ObservableProperty]
        private decimal? _inquiryBuildingUnitPrice;
        
        [ObservableProperty]
        private decimal? _inquiryMachineryUnitPrice;
        
        [ObservableProperty]
        private decimal? _landAreaPyeong = 50.5m;
        
        [ObservableProperty]
        private decimal? _buildingAreaPyeong = 35.2m;
        
        [ObservableProperty]
        private decimal? _inquiryLandValue;
        
        [ObservableProperty]
        private decimal? _inquiryBuildingValue;
        
        [ObservableProperty]
        private decimal? _inquiryMachineryValue;
        
        // === 탐문수익가치 (민감도 분석) ===
        [ObservableProperty]
        private decimal _inquiryDiscountRate = 0.06m;
        
        [ObservableProperty]
        private decimal? _inquiryIncomeValue5;
        
        [ObservableProperty]
        private decimal? _inquiryIncomeValue6;
        
        [ObservableProperty]
        private decimal? _inquiryIncomeValue7;
        
        // === 수익가치 (임대차 정보) ===
        [ObservableProperty]
        private ObservableCollection<LeaseInfoItem> _leaseInfoList = new();
        
        [ObservableProperty]
        private decimal? _leaseIncomeValue;
        
        // === 유사물건 추천 ===
        [ObservableProperty]
        private ObservableCollection<RecommendCaseItem> _recommendedCases = new();
        
        [ObservableProperty]
        private RecommendCaseItem? _selectedRecommendCase;
        
        [ObservableProperty]
        private int _selectedRuleIndex = 1;
        
        // === 낙찰통계 ===
        [ObservableProperty]
        private string? _regionName1 = "서울특별시";
        
        [ObservableProperty]
        private string? _regionName2 = "마포구";
        
        [ObservableProperty]
        private string? _regionName3 = "연남동";
        
        [ObservableProperty]
        private string? _stats1Year_Display1 = "68.5% / 156건";
        
        [ObservableProperty]
        private string? _stats1Year_Display2 = "65.2% / 34건";
        
        [ObservableProperty]
        private string? _stats1Year_Display3 = "62.8% / 12건";
        
        [ObservableProperty]
        private string? _stats6Month_Display1 = "67.2% / 78건";
        
        [ObservableProperty]
        private string? _stats6Month_Display2 = "64.5% / 18건";
        
        [ObservableProperty]
        private string? _stats6Month_Display3 = "61.0% / 5건";
        
        [ObservableProperty]
        private string? _stats3Month_Display1 = "65.8% / 35건";
        
        [ObservableProperty]
        private string? _stats3Month_Display2 = "63.2% / 8건";
        
        [ObservableProperty]
        private string? _stats3Month_Display3 = "60.5% / 2건";
        
        [ObservableProperty]
        private decimal? _appliedBidRate = 0.63m;
        
        // === 사례평가 ===
        [ObservableProperty]
        private ObservableCollection<CaseRowItem> _caseItems = new();
        
        // === 부동산 탐문내역 ===
        [ObservableProperty]
        private string? _inquiryRealEstateName;
        
        [ObservableProperty]
        private string? _inquiryPhone;
        
        [ObservableProperty]
        private string? _inquiryContent;
        
        #endregion
        
        #region 계산된 속성
        
        public decimal TotalInquiryValue => 
            (InquiryLandValue ?? 0) + (InquiryBuildingValue ?? 0) + (InquiryMachineryValue ?? 0);
            
        public decimal TotalAnnualRent => 
            LeaseInfoList.Sum(x => x.AnnualRent ?? 0);
        
        #endregion
        
        #region 초기화
        
        private void InitializeData()
        {
            // 임대차 정보 샘플 데이터
            LeaseInfoList = new ObservableCollection<LeaseInfoItem>
            {
                new LeaseInfoItem 
                { 
                    TenantName = "임차인1", 
                    Deposit = 30000000, 
                    MonthlyRent = 800000,
                    LeaseStartDate = DateTime.Now.AddYears(-1),
                    LeaseEndDate = DateTime.Now.AddYears(1),
                    ConfirmationDate = DateTime.Now.AddYears(-1).AddDays(7)
                },
                new LeaseInfoItem 
                { 
                    TenantName = "임차인2", 
                    Deposit = 20000000, 
                    MonthlyRent = 600000,
                    LeaseStartDate = DateTime.Now.AddMonths(-6),
                    LeaseEndDate = DateTime.Now.AddMonths(18),
                    ConfirmationDate = DateTime.Now.AddMonths(-6).AddDays(5)
                }
            };
            
            // 사례평가 테이블 초기화
            CaseItems = new ObservableCollection<CaseRowItem>
            {
                new CaseRowItem { Label = "사례구분" },
                new CaseRowItem { Label = "경매사건번호" },
                new CaseRowItem { Label = "낙찰일자" },
                new CaseRowItem { Label = "용도" },
                new CaseRowItem { Label = "소재지" },
                new CaseRowItem { Label = "토지면적(평)" },
                new CaseRowItem { Label = "건물연면적(평)" },
                new CaseRowItem { Label = "법사가" },
                new CaseRowItem { Label = "낙찰가액" },
                new CaseRowItem { Label = "낙찰가율" },
                new CaseRowItem { Label = "낙찰회차" }
            };
            
            CalculateInquiryValues();
            CalculateLeaseIncomeValue();
        }
        
        #endregion
        
        #region 계산 메서드
        
        partial void OnInquiryLandUnitPriceChanged(decimal? value)
        {
            if (value.HasValue && LandAreaPyeong.HasValue)
            {
                InquiryLandValue = value.Value * LandAreaPyeong.Value;
            }
            CalculateSensitivityAnalysis();
            OnPropertyChanged(nameof(TotalInquiryValue));
        }
        
        partial void OnInquiryBuildingUnitPriceChanged(decimal? value)
        {
            if (value.HasValue && BuildingAreaPyeong.HasValue)
            {
                InquiryBuildingValue = value.Value * BuildingAreaPyeong.Value;
            }
            CalculateSensitivityAnalysis();
            OnPropertyChanged(nameof(TotalInquiryValue));
        }
        
        partial void OnInquiryMachineryUnitPriceChanged(decimal? value)
        {
            InquiryMachineryValue = value;
            CalculateSensitivityAnalysis();
            OnPropertyChanged(nameof(TotalInquiryValue));
        }
        
        private void CalculateInquiryValues()
        {
            // 초기 값 계산
            if (InquiryLandUnitPrice.HasValue && LandAreaPyeong.HasValue)
                InquiryLandValue = InquiryLandUnitPrice.Value * LandAreaPyeong.Value;
                
            if (InquiryBuildingUnitPrice.HasValue && BuildingAreaPyeong.HasValue)
                InquiryBuildingValue = InquiryBuildingUnitPrice.Value * BuildingAreaPyeong.Value;
        }
        
        private void CalculateSensitivityAnalysis()
        {
            // 연간 수익 계산 (임대료 기반)
            var annualIncome = TotalAnnualRent;
            
            if (annualIncome > 0)
            {
                InquiryIncomeValue5 = annualIncome / 0.05m;
                InquiryIncomeValue6 = annualIncome / 0.06m;
                InquiryIncomeValue7 = annualIncome / 0.07m;
            }
        }
        
        private void CalculateLeaseIncomeValue()
        {
            var totalAnnual = TotalAnnualRent;
            if (totalAnnual > 0 && InquiryDiscountRate > 0)
            {
                LeaseIncomeValue = totalAnnual / InquiryDiscountRate;
            }
            OnPropertyChanged(nameof(TotalAnnualRent));
        }
        
        #endregion
        
        #region 명령
        
        [RelayCommand]
        private void LoadCaseMap()
        {
            System.Windows.MessageBox.Show(
                "주택/토지 사례지도 기능은 추후 구현 예정입니다.",
                "사례지도",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        [RelayCommand]
        private async Task RecommendSimilarCasesAsync()
        {
            // TODO: 실제 추천 로직 구현
            await Task.Delay(100);
            System.Windows.MessageBox.Show(
                "유사물건 추천 기능은 메인 EvaluationTab에서 동작합니다.",
                "유사물건 추천",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        [RelayCommand]
        private void ApplyRecommendedCase()
        {
            if (SelectedRecommendCase == null)
            {
                System.Windows.MessageBox.Show("적용할 사례를 선택하세요.", "알림");
                return;
            }
            
            // 적용 로직
            System.Windows.MessageBox.Show($"사례 {SelectedRecommendCase.CaseNo}가 적용되었습니다.", "알림");
        }
        
        [RelayCommand]
        private void SearchAuctionCase()
        {
            try
            {
                var url = "https://www.courtauction.go.kr/";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
        
        #endregion
    }
}
