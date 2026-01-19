using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NPLogic.ViewModels.Evaluation
{
    /// <summary>
    /// 평가 기본 ViewModel - 아파트/연립다세대 공통
    /// </summary>
    public partial class EvaluationBaseViewModel : ObservableObject
    {
        public EvaluationBaseViewModel()
        {
            InitializeCaseItems();
        }
        
        #region 속성
        
        // === 실거래가 ===
        [ObservableProperty]
        private ObservableCollection<RealTransactionItem> _realTransactions = new();
        
        // === 낙찰통계 ===
        [ObservableProperty]
        private string? _regionName1 = "서울특별시";
        
        [ObservableProperty]
        private string? _regionName2 = "강남구";
        
        [ObservableProperty]
        private string? _regionName3 = "대치동";
        
        [ObservableProperty]
        private string? _stats1Year_Display1 = "75.2% / 124건";
        
        [ObservableProperty]
        private string? _stats1Year_Display2 = "72.8% / 45건";
        
        [ObservableProperty]
        private string? _stats1Year_Display3 = "70.5% / 12건";
        
        [ObservableProperty]
        private string? _stats6Month_Display1 = "73.8% / 62건";
        
        [ObservableProperty]
        private string? _stats6Month_Display2 = "71.2% / 23건";
        
        [ObservableProperty]
        private string? _stats6Month_Display3 = "69.0% / 6건";
        
        [ObservableProperty]
        private string? _stats3Month_Display1 = "72.5% / 31건";
        
        [ObservableProperty]
        private string? _stats3Month_Display2 = "70.0% / 11건";
        
        [ObservableProperty]
        private string? _stats3Month_Display3 = "68.5% / 3건";
        
        [ObservableProperty]
        private decimal? _appliedBidRate = 0.70m;
        
        [ObservableProperty]
        private string? _appliedBidRateDescription = "3개월 평균 낙찰가율";
        
        // === 유사물건 추천 ===
        [ObservableProperty]
        private ObservableCollection<RecommendCaseItem> _recommendedCases = new();
        
        [ObservableProperty]
        private RecommendCaseItem? _selectedRecommendCase;
        
        [ObservableProperty]
        private int _selectedRuleIndex = 1;
        
        [ObservableProperty]
        private string _selectedRegionScope = "big";
        
        // === 사례평가 ===
        [ObservableProperty]
        private ObservableCollection<CaseRowItem> _caseItems = new();
        
        // === 경매사건 정보 ===
        [ObservableProperty]
        private string? _case1AuctionInfo = "사례1 경매정보가 여기에 표시됩니다.";
        
        [ObservableProperty]
        private string? _case2AuctionInfo = "사례2 경매정보가 여기에 표시됩니다.";
        
        // === 부동산 탐문내역 ===
        [ObservableProperty]
        private string? _inquiryRealEstateName;
        
        [ObservableProperty]
        private string? _inquiryPhone;
        
        [ObservableProperty]
        private string? _inquiryContent;
        
        #endregion
        
        #region 초기화
        
        protected void InitializeCaseItems()
        {
            CaseItems = new ObservableCollection<CaseRowItem>
            {
                new CaseRowItem { Label = "사례구분" },
                new CaseRowItem { Label = "경매사건번호" },
                new CaseRowItem { Label = "낙찰일자" },
                new CaseRowItem { Label = "용도" },
                new CaseRowItem { Label = "소재지" },
                new CaseRowItem { Label = "토지면적(평)" },
                new CaseRowItem { Label = "건물연면적(평)" },
                new CaseRowItem { Label = "보존등기일" },
                new CaseRowItem { Label = "사용승인일" },
                new CaseRowItem { Label = "법사가" },
                new CaseRowItem { Label = "토지" },
                new CaseRowItem { Label = "건물" },
                new CaseRowItem { Label = "감평기준일자" },
                new CaseRowItem { Label = "평당감정가(토지)" },
                new CaseRowItem { Label = "평당감정가(건물)" },
                new CaseRowItem { Label = "토지평당법사가" },
                new CaseRowItem { Label = "건물평당법사가" },
                new CaseRowItem { Label = "낙찰가액" },
                new CaseRowItem { Label = "낙찰가율" },
                new CaseRowItem { Label = "낙찰회차" },
                new CaseRowItem { Label = "평당낙찰가(토지)" },
                new CaseRowItem { Label = "평당낙찰가(건물)" },
                new CaseRowItem { Label = "토지평당낙찰가**" },
                new CaseRowItem { Label = "건물평당낙찰가**" },
                new CaseRowItem { Label = "용적율" },
                new CaseRowItem { Label = "2등 입찰가" },
                new CaseRowItem { Label = "사례 비고 사항" }
            };
        }
        
        #endregion
        
        #region 명령
        
        [RelayCommand]
        protected virtual void LoadCaseMap()
        {
            System.Windows.MessageBox.Show(
                "사례지도 기능은 추후 구현 예정입니다.\n\n소재지 기준으로 본건 위치와 주변 거래사례를 지도에 표시하는 기능입니다.",
                "사례지도",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        [RelayCommand]
        protected virtual async Task FetchRealTransactionAsync()
        {
            await Task.Delay(100);
            
            // 샘플 데이터
            RealTransactions.Clear();
            RealTransactions.Add(new RealTransactionItem
            {
                Area = 84.5m,
                TransactionDate = DateTime.Now.AddMonths(-1),
                Amount = 450000000,
                Floor = "15",
                IsRegistered = "Y",
                IsApplied = false
            });
            RealTransactions.Add(new RealTransactionItem
            {
                Area = 84.5m,
                TransactionDate = DateTime.Now.AddMonths(-2),
                Amount = 440000000,
                Floor = "8",
                IsRegistered = "Y",
                IsApplied = false
            });
        }
        
        [RelayCommand]
        protected virtual void OpenRealTransactionSite()
        {
            try
            {
                var url = "https://rt.molit.go.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
        
        [RelayCommand]
        protected virtual async Task RecommendSimilarCasesAsync()
        {
            await Task.Delay(100);
            System.Windows.MessageBox.Show(
                "유사물건 추천 기능은 메인 EvaluationTab에서 동작합니다.",
                "유사물건 추천",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        [RelayCommand]
        protected virtual void ApplyRecommendedCase()
        {
            if (SelectedRecommendCase == null)
            {
                System.Windows.MessageBox.Show("적용할 사례를 선택하세요.", "알림");
                return;
            }
        }
        
        [RelayCommand]
        protected virtual void ClearCaseEvaluation()
        {
            foreach (var item in CaseItems)
            {
                item.Case1Value = null;
                item.Case2Value = null;
                item.Case3Value = null;
                item.Case4Value = null;
            }
        }
        
        [RelayCommand]
        protected virtual void SearchAuctionCase()
        {
            try
            {
                var url = "https://www.courtauction.go.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
        
        #endregion
    }
}
