using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NPLogic.ViewModels.Evaluation
{
    /// <summary>
    /// 지번별 평가 항목 (토지/건물)
    /// </summary>
    public partial class LandParcelItem : ObservableObject
    {
        [ObservableProperty]
        private string _parcelNumber = "";
        
        [ObservableProperty]
        private string _parcelType = "토지";
        
        [ObservableProperty]
        private string _address = "";
        
        [ObservableProperty]
        private decimal? _areaPyeong;
        
        [ObservableProperty]
        private decimal? _unitAppraisal;
        
        [ObservableProperty]
        private decimal? _appraisalValue;
        
        [ObservableProperty]
        private decimal? _scenario1UnitPrice;
        
        [ObservableProperty]
        private decimal? _scenario1Value;
        
        [ObservableProperty]
        private decimal? _scenario2UnitPrice;
        
        [ObservableProperty]
        private decimal? _scenario2Value;
        
        partial void OnAreaPyeongChanged(decimal? value)
        {
            CalculateValues();
        }
        
        partial void OnUnitAppraisalChanged(decimal? value)
        {
            CalculateValues();
        }
        
        partial void OnScenario1UnitPriceChanged(decimal? value)
        {
            if (AreaPyeong.HasValue && value.HasValue)
                Scenario1Value = AreaPyeong.Value * value.Value;
        }
        
        partial void OnScenario2UnitPriceChanged(decimal? value)
        {
            if (AreaPyeong.HasValue && value.HasValue)
                Scenario2Value = AreaPyeong.Value * value.Value;
        }
        
        private void CalculateValues()
        {
            if (AreaPyeong.HasValue && UnitAppraisal.HasValue)
            {
                AppraisalValue = AreaPyeong.Value * UnitAppraisal.Value;
            }
        }
    }
    
    /// <summary>
    /// 기계기구 항목
    /// </summary>
    public partial class MachineryItem : ObservableObject
    {
        [ObservableProperty]
        private int _itemNumber;
        
        [ObservableProperty]
        private string _machineryName = "";
        
        [ObservableProperty]
        private string _manufacturer = "";
        
        [ObservableProperty]
        private DateTime? _manufactureDate;
        
        [ObservableProperty]
        private int _quantity = 1;
        
        [ObservableProperty]
        private decimal? _unitPrice;
        
        [ObservableProperty]
        private decimal? _appraisalValue;
        
        [ObservableProperty]
        private bool _factoryMortgage1;
        
        [ObservableProperty]
        private bool _factoryMortgage2;
        
        [ObservableProperty]
        private bool _factoryMortgage3;
        
        [ObservableProperty]
        private bool _factoryMortgage4;
        
        partial void OnQuantityChanged(int value)
        {
            CalculateAppraisalValue();
        }
        
        partial void OnUnitPriceChanged(decimal? value)
        {
            CalculateAppraisalValue();
        }
        
        private void CalculateAppraisalValue()
        {
            if (UnitPrice.HasValue)
            {
                AppraisalValue = UnitPrice.Value * Quantity;
            }
        }
    }
    
    /// <summary>
    /// 공장/창고 평가 ViewModel
    /// </summary>
    public partial class EvaluationFactoryViewModel : ObservableObject
    {
        public EvaluationFactoryViewModel()
        {
            InitializeData();
        }
        
        #region 속성
        
        // === 공시지가 ===
        [ObservableProperty]
        private decimal? _officialLandPrice2023;
        
        [ObservableProperty]
        private decimal? _officialLandPrice2024;
        
        [ObservableProperty]
        private decimal? _officialLandPrice2025;
        
        // === 지번별 평가 ===
        [ObservableProperty]
        private ObservableCollection<LandParcelItem> _landParcels = new();
        
        [ObservableProperty]
        private ObservableCollection<LandParcelItem> _buildingParcels = new();
        
        // === 기계기구 ===
        [ObservableProperty]
        private ObservableCollection<MachineryItem> _machineryList = new();
        
        [ObservableProperty]
        private MachineryItem? _selectedMachinery;
        
        [ObservableProperty]
        private decimal _machineryRecognitionRate = 0.2m; // 기본 20%
        
        // === 낙찰통계 ===
        [ObservableProperty]
        private string? _regionName1 = "경기도";
        
        [ObservableProperty]
        private string? _regionName2 = "화성시";
        
        [ObservableProperty]
        private string? _regionName3 = "봉담읍";
        
        [ObservableProperty]
        private string? _stats1Year_Display1 = "65.2% / 45건";
        
        [ObservableProperty]
        private string? _stats1Year_Display2 = "62.8% / 12건";
        
        [ObservableProperty]
        private string? _stats1Year_Display3 = "60.5% / 3건";
        
        [ObservableProperty]
        private string? _stats6Month_Display1 = "63.8% / 22건";
        
        [ObservableProperty]
        private string? _stats6Month_Display2 = "61.2% / 6건";
        
        [ObservableProperty]
        private string? _stats6Month_Display3 = "59.0% / 1건";
        
        [ObservableProperty]
        private string? _stats3Month_Display1 = "62.5% / 11건";
        
        [ObservableProperty]
        private string? _stats3Month_Display2 = "60.0% / 3건";
        
        [ObservableProperty]
        private string? _stats3Month_Display3 = "-";
        
        [ObservableProperty]
        private decimal? _appliedBidRate = 0.62m;
        
        [ObservableProperty]
        private string? _appliedBidRateDescription = "6개월 평균";
        
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
        
        public decimal TotalLandBuildingAppraisal => 
            LandParcels.Sum(x => x.AppraisalValue ?? 0) + 
            BuildingParcels.Sum(x => x.AppraisalValue ?? 0);
            
        public decimal TotalScenario1Value => 
            LandParcels.Sum(x => x.Scenario1Value ?? 0) + 
            BuildingParcels.Sum(x => x.Scenario1Value ?? 0);
            
        public decimal TotalScenario2Value => 
            LandParcels.Sum(x => x.Scenario2Value ?? 0) + 
            BuildingParcels.Sum(x => x.Scenario2Value ?? 0);
            
        public decimal TotalMachineryAppraisal => 
            MachineryList.Sum(x => x.AppraisalValue ?? 0);
            
        public decimal RecognizedMachineryValue => 
            MachineryList.Where(x => x.FactoryMortgage1 || x.FactoryMortgage2 || 
                                     x.FactoryMortgage3 || x.FactoryMortgage4)
                        .Sum(x => x.AppraisalValue ?? 0);
                        
        public decimal MachineryEvaluationValue => 
            RecognizedMachineryValue * MachineryRecognitionRate;
        
        #endregion
        
        #region 초기화
        
        private void InitializeData()
        {
            // 샘플 지번별 토지 데이터
            LandParcels = new ObservableCollection<LandParcelItem>
            {
                new LandParcelItem 
                { 
                    ParcelNumber = "R-035-1-1", 
                    ParcelType = "토지",
                    Address = "경기도 화성시 봉담읍",
                    AreaPyeong = 150.5m,
                    UnitAppraisal = 1200000m
                }
            };
            
            // 샘플 지번별 건물 데이터
            BuildingParcels = new ObservableCollection<LandParcelItem>
            {
                new LandParcelItem 
                { 
                    ParcelNumber = "R-035-1-1", 
                    ParcelType = "건물",
                    Address = "경기도 화성시 봉담읍",
                    AreaPyeong = 200.0m,
                    UnitAppraisal = 800000m
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
        }
        
        #endregion
        
        #region 명령
        
        [RelayCommand]
        private void FetchOfficialLandPrice()
        {
            // TODO: 공시지가 API 연동
            System.Windows.MessageBox.Show(
                "공시지가 조회 기능은 추후 구현 예정입니다.",
                "공시지가",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        [RelayCommand]
        private void AddLandParcel(string parcelType)
        {
            var newItem = new LandParcelItem
            {
                ParcelNumber = $"R-{LandParcels.Count + BuildingParcels.Count + 1:D3}",
                ParcelType = parcelType
            };
            
            if (parcelType == "토지")
                LandParcels.Add(newItem);
            else
                BuildingParcels.Add(newItem);
                
            OnPropertyChanged(nameof(TotalLandBuildingAppraisal));
        }
        
        [RelayCommand]
        private void AddMachinery()
        {
            var newItem = new MachineryItem
            {
                ItemNumber = MachineryList.Count + 1,
                Quantity = 1
            };
            MachineryList.Add(newItem);
            OnPropertyChanged(nameof(TotalMachineryAppraisal));
        }
        
        [RelayCommand]
        private void DeleteMachinery()
        {
            if (SelectedMachinery != null)
            {
                MachineryList.Remove(SelectedMachinery);
                // 번호 재정렬
                for (int i = 0; i < MachineryList.Count; i++)
                {
                    MachineryList[i].ItemNumber = i + 1;
                }
                OnPropertyChanged(nameof(TotalMachineryAppraisal));
                OnPropertyChanged(nameof(RecognizedMachineryValue));
                OnPropertyChanged(nameof(MachineryEvaluationValue));
            }
        }
        
        [RelayCommand]
        private void LoadCaseMap()
        {
            System.Windows.MessageBox.Show(
                "공장/창고 사례지도 기능은 추후 구현 예정입니다.",
                "사례지도",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
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
