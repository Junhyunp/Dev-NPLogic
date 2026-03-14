using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;
using SkiaSharp;
using EvaluationModel = NPLogic.Core.Models.Evaluation;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 사례평가 테이블 행 아이템
    /// </summary>
    public partial class CaseRowItem : ObservableObject
    {
        [ObservableProperty]
        private string _label = "";
        
        [ObservableProperty]
        private string? _baseValue;
        
        [ObservableProperty]
        private string? _case1Value;
        
        [ObservableProperty]
        private string? _case2Value;
        
        [ObservableProperty]
        private string? _case3Value;
        
        [ObservableProperty]
        private string? _case4Value;
    }

    /// <summary>
    /// 실거래가 아이템
    /// </summary>
    public class RealTransactionItem : ObservableObject
    {
        public decimal? Area { get; set; }
        public DateTime? TransactionDate { get; set; }
        /// <summary>표시용 금액: 아파트=총거래가(만원), 집합건물=단가(만원/㎡)</summary>
        public decimal? Amount { get; set; }
        /// <summary>원본 총거래가(만원) — 적용 기능 등에서 사용</summary>
        public int OriginalDealAmount { get; set; }
        public string? Floor { get; set; }
        public string? IsRegistered { get; set; }

        /// <summary>DB 매칭용 원본 값 (외부 TradeRecord 그대로)</summary>
        public int DealDateRaw { get; set; }
        public double AreaRaw { get; set; }

        private bool _isApplied;
        public bool IsApplied
        {
            get => _isApplied;
            set => SetProperty(ref _isApplied, value);
        }
    }

    /// <summary>
    /// 유사물건 추천 결과 아이템
    /// </summary>
    public class RecommendCaseItem : ObservableObject
    {
        public string? CaseNo { get; set; }
        public string? Address { get; set; }
        public string? Usage { get; set; }
        public DateTime? AuctionDate { get; set; }
        public decimal? AppraisalPrice { get; set; }
        public decimal? WinningPrice { get; set; }
        public double? BuildingArea { get; set; }
        public double? LandArea { get; set; }
        public string? RuleName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        /// <summary>
        /// E-005: 본건과의 거리 (km)
        /// </summary>
        public double? DistanceKm { get; set; }
        
        /// <summary>
        /// E-005: 거리 표시 문자열
        /// </summary>
        public string DistanceDisplay => DistanceKm.HasValue ? $"{DistanceKm:N1}km" : "-";
        
        /// <summary>
        /// 낙찰가율 (%)
        /// </summary>
        public decimal? WinningRate => AppraisalPrice > 0 ? (WinningPrice / AppraisalPrice) * 100 : null;
        
        /// <summary>
        /// 낙찰가율 표시 문자열
        /// </summary>
        public string WinningRateDisplay => WinningRate.HasValue ? $"{WinningRate:N1}%" : "-";
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    /// <summary>
    /// 시나리오별 배당 요약 항목 (피드백 반영: 시나리오 3 제거)
    /// </summary>
    public class ScenarioSummaryItem : ObservableObject
    {
        /// <summary>항목명 (경매비용, 배당회수, 상계회수, 현금흐름)</summary>
        public string Label { get; set; } = "";
        
        /// <summary>시나리오 1 (하한) 금액</summary>
        public decimal? Scenario1Value { get; set; }
        
        /// <summary>시나리오 2 (상한) 금액</summary>
        public decimal? Scenario2Value { get; set; }
        
        /// <summary>합계</summary>
        public decimal? TotalValue { get; set; }
        
        // 포맷된 표시 값
        public string Scenario1Display => Scenario1Value.HasValue ? $"{Scenario1Value:N0}" : "-";
        public string Scenario2Display => Scenario2Value.HasValue ? $"{Scenario2Value:N0}" : "-";
        public string TotalDisplay => TotalValue.HasValue ? $"{TotalValue:N0}" : "-";
    }

    /// <summary>
    /// 회수 전략 요약 행 아이템
    /// </summary>
    public class RecoveryStrategyRow : ObservableObject
    {
        public string Label { get; set; } = "";
        public string? S1CfDate { get; set; }
        public string? S1CashInOut { get; set; }
        public string? S1Ratio { get; set; }
        public string? S2CfDate { get; set; }
        public string? S2CashInOut { get; set; }
        public string? S2Ratio { get; set; }
        public bool IsTotal { get; set; }
    }

    /// <summary>
    /// 인터림 상계/회수 테이블 행 아이템
    /// </summary>
    public class InterimRecoveryRow : ObservableObject
    {
        public string? BorrowerNumber { get; set; }
        public string? BorrowerName { get; set; }
        public string? LoanNumber { get; set; }
        public string? AccountNumber { get; set; }
        public string? RecoveryType { get; set; }
        public string? RecoveryDate { get; set; }
        public string? RecoveryPrincipal { get; set; }
        public string? RecoveryInterest { get; set; }
        public string? RecoveryProvisional { get; set; }
        public string? RecoveryTotal { get; set; }
        public bool PrincipalRecoveryApplied { get; set; }
        public bool PrincipalOffsetApplied { get; set; }
        public bool InterestRecoveryApplied { get; set; }
        public bool InterestOffsetApplied { get; set; }
        public bool SubrogationApplied { get; set; }
        public bool OtherRecoveryApplied { get; set; }
    }

    /// <summary>
    /// 인터림 지출 테이블 행 아이템
    /// </summary>
    public class InterimExpenseRow : ObservableObject
    {
        public string? BorrowerNumber { get; set; }
        public string? BorrowerName { get; set; }
        public string? LoanNumber { get; set; }
        public string? AccountNumber { get; set; }
        public string? OccurrenceDate { get; set; }
        public string? InitialAmount { get; set; }
        public string? Balance { get; set; }
        public string? Remarks { get; set; }
        public bool AuctionCostApplied { get; set; }
        public bool OtherCostApplied { get; set; }
    }

    /// <summary>
    /// 탐문 내역 행 아이템
    /// </summary>
    public partial class InquiryRow : ObservableObject
    {
        [ObservableProperty]
        private string _realEstateName = "";

        [ObservableProperty]
        private string _contactNumber = "";

        [ObservableProperty]
        private string _inquiryDetails = "";
    }

    /// <summary>
    /// 탐문 결과 행 아이템
    /// </summary>
    public partial class InquiryResultRow : ObservableObject
    {
        [ObservableProperty]
        private string _category = "";

        [ObservableProperty]
        private string _appraisedValue = "";

        [ObservableProperty]
        private string _areaPyeong = "";

        [ObservableProperty]
        private string _unitPrice = "";

        [ObservableProperty]
        private string _evaluatedValue = "";
    }

    /// <summary>
    /// 지번별 평가 행
    /// </summary>
    public partial class LotEvalRow : ObservableObject
    {
        [ObservableProperty] private string _lotSerialNumber = "";
        [ObservableProperty] private string _category = "";
        [ObservableProperty] private string _lotAddress = "";
        [ObservableProperty] private string _areaPyeong = "";
        [ObservableProperty] private string _unitAppraisal = "";
        [ObservableProperty] private string _appraisalValue = "";
        [ObservableProperty] private string _plan1UnitPrice = "";
        [ObservableProperty] private string _plan1EvalAmount = "";
        [ObservableProperty] private string _plan2UnitPrice = "";
        [ObservableProperty] private string _plan2EvalAmount = "";
    }

    /// <summary>
    /// 공장/창고 평가결과 행
    /// </summary>
    public partial class FactoryEvalRow : ObservableObject
    {
        [ObservableProperty] private string _category = "";
        [ObservableProperty] private string _appraisalValue = "";
        [ObservableProperty] private string _area = "";
        [ObservableProperty] private string _unitAppraisal = "";
        [ObservableProperty] private string _unitEvalPrice = "";
        [ObservableProperty] private string _evalAmount = "";
        [ObservableProperty] private string _bidRate = "";
        [ObservableProperty] private string _landPerPyeongBid = "";
        [ObservableProperty] private string _buildingPerPyeongBid = "";
    }

    /// <summary>
    /// 현재 임대 수익가치 - 상단 DataGrid 행
    /// </summary>
    public partial class RentalIncomeRow : ObservableObject
    {
        [ObservableProperty] private string _category = "";
        [ObservableProperty] private string _area = "";
        [ObservableProperty] private string _tenant = "";
        [ObservableProperty] private string _leaseStartDate = "";
        [ObservableProperty] private string _leaseEndDate = "";
        [ObservableProperty] private string _confirmationDate = "";
        [ObservableProperty] private string _deposit = "";
        [ObservableProperty] private string _monthlyRent = "";
        [ObservableProperty] private string _annualRent = "";
    }

    /// <summary>
    /// 현재 임대 수익가치 - 하단 민감도 분석 데이터
    /// </summary>
    public partial class RentalProfitSensitivity : ObservableObject
    {
        [ObservableProperty] private string _baseDiscountRate = "";
        [ObservableProperty] private string _sensitivityBase = "";
        [ObservableProperty] private string _profitValue50 = "";
        [ObservableProperty] private string _profitValue55 = "";
        [ObservableProperty] private string _profitValue60 = "";
        [ObservableProperty] private string _profitValue65 = "";
        [ObservableProperty] private string _profitValue70 = "";
        [ObservableProperty] private string _unitPrice50 = "";
        [ObservableProperty] private string _unitPrice55 = "";
        [ObservableProperty] private string _unitPrice60 = "";
        [ObservableProperty] private string _unitPrice65 = "";
        [ObservableProperty] private string _unitPrice70 = "";
    }

    /// <summary>
    /// 탐문 수익가치 데이터
    /// </summary>
    public partial class InquiryProfitData : ObservableObject
    {
        // 왼쪽 표
        [ObservableProperty] private string _buildingArea = "";
        [ObservableProperty] private string _rentPerPyeong = "";
        [ObservableProperty] private string _monthlyRent = "";
        [ObservableProperty] private string _annualRent = "";
        [ObservableProperty] private string _deposit = "";

        // 오른쪽 표
        [ObservableProperty] private string _baseDiscountRate = "";
        [ObservableProperty] private string _sensitivityBase = "";
        [ObservableProperty] private string _profitValue50 = "";
        [ObservableProperty] private string _profitValue55 = "";
        [ObservableProperty] private string _profitValue60 = "";
        [ObservableProperty] private string _profitValue65 = "";
        [ObservableProperty] private string _profitValue70 = "";
        [ObservableProperty] private string _unitPrice50 = "";
        [ObservableProperty] private string _unitPrice55 = "";
        [ObservableProperty] private string _unitPrice60 = "";
        [ObservableProperty] private string _unitPrice65 = "";
        [ObservableProperty] private string _unitPrice70 = "";
    }

    /// <summary>
    /// 평가 탭 ViewModel
    /// </summary>
    public partial class EvaluationTabViewModel : ObservableObject
    {
        private readonly EvaluationRepository _evaluationRepository;
        private readonly RecommendService _recommendService;
        private Guid _propertyId;
        private Property? _property;
        private EvaluationModel? _evaluation;

        // Supabase 설정 (App.xaml.cs에서 설정)
        public string? SupabaseUrl { get; set; }
        public string? SupabaseKey { get; set; }

        private readonly TradeService _tradeService;
        private readonly PropertyTradeAppliedRepository? _tradeAppliedRepository;

        public EvaluationTabViewModel(EvaluationRepository evaluationRepository)
        {
            _evaluationRepository = evaluationRepository ?? throw new ArgumentNullException(nameof(evaluationRepository));
            _recommendService = new RecommendService();
            _tradeService = new TradeService();
            _tradeAppliedRepository = App.ServiceProvider?
                .GetService(typeof(PropertyTradeAppliedRepository)) as PropertyTradeAppliedRepository;

            // 초기 데이터 설정
            InitializeCaseItems();
            BuildRecoveryStrategyRows();
            InitializeFactoryEvalRows();
            InitializeCommercialEvalRows();
            InitializeHouseEvalRows();
        }

        #region 회수 전략 요약

        [ObservableProperty]
        private int _borrowerPropertyCount = 1;

        [ObservableProperty]
        private string _scenario1CapType = "해당사항 없음";

        [ObservableProperty]
        private string _scenario2CapType = "해당사항 없음";

        public ObservableCollection<RecoveryStrategyRow> RecoveryStrategyRows { get; } = new();

        public ObservableCollection<InterimRecoveryRow> InterimRecoveryRows { get; } = new();

        public ObservableCollection<InterimExpenseRow> InterimExpenseRows { get; } = new();

        public ObservableCollection<InquiryRow> InquiryRows { get; } = new();

        [ObservableProperty]
        private bool _hasInquiryTable;

        public ObservableCollection<InquiryResultRow> InquiryResultRows { get; } = new();

        [ObservableProperty]
        private bool _hasInquiryResultTable;

        // === 임대호가분석 ===
        [ObservableProperty]
        private bool _hasRentalQuoteTable;

        // === 무상임대분석 ===
        [ObservableProperty]
        private bool _hasFreeRentTable;

        [ObservableProperty]
        private InquiryProfitData? _inquiryProfitData;

        [ObservableProperty]
        private bool _hasInquiryProfitTable;

        // 현재 임대 수익가치
        public ObservableCollection<RentalIncomeRow> RentalIncomeRows { get; } = new();

        [ObservableProperty]
        private RentalProfitSensitivity? _rentalProfitSensitivity;

        [ObservableProperty]
        private bool _hasRentalProfitTable;

        // 상가 평가결과 시나리오 1
        [ObservableProperty] private string _commercialScenario1EvalAmount = "";
        [ObservableProperty] private string _commercialScenario1EvalReason = "";
        [ObservableProperty] private string _commercialScenario1ReductionAmount = "";
        public ObservableCollection<FactoryEvalRow> CommercialScenario1Rows { get; } = new();

        // 상가 평가결과 시나리오 2
        [ObservableProperty] private string _commercialScenario2EvalAmount = "";
        [ObservableProperty] private string _commercialScenario2EvalReason = "";
        [ObservableProperty] private string _commercialScenario2ReductionAmount = "";
        public ObservableCollection<FactoryEvalRow> CommercialScenario2Rows { get; } = new();

        // 주택 평가결과 시나리오 1
        [ObservableProperty] private string _houseScenario1EvalAmount = "";
        [ObservableProperty] private string _houseScenario1EvalReason = "";
        [ObservableProperty] private string _houseScenario1ReductionAmount = "";
        public ObservableCollection<FactoryEvalRow> HouseScenario1Rows { get; } = new();

        // 주택 평가결과 시나리오 2
        [ObservableProperty] private string _houseScenario2EvalAmount = "";
        [ObservableProperty] private string _houseScenario2EvalReason = "";
        [ObservableProperty] private string _houseScenario2ReductionAmount = "";
        public ObservableCollection<FactoryEvalRow> HouseScenario2Rows { get; } = new();

        // 공장/창고 평가결과 시나리오 1
        [ObservableProperty] private string _factoryScenario1EvalAmount = "";
        [ObservableProperty] private string _factoryScenario1EvalReason = "";
        [ObservableProperty] private string _factoryScenario1ReductionAmount = "";
        public ObservableCollection<FactoryEvalRow> FactoryScenario1Rows { get; } = new();

        // 공장/창고 평가결과 시나리오 2
        [ObservableProperty] private string _factoryScenario2EvalAmount = "";
        [ObservableProperty] private string _factoryScenario2EvalReason = "";
        [ObservableProperty] private string _factoryScenario2ReductionAmount = "";
        public ObservableCollection<FactoryEvalRow> FactoryScenario2Rows { get; } = new();

        public ObservableCollection<LotEvalRow> LotEvalRows { get; } = new();

        public List<string> CapTypeOptions { get; } = new() { "Loan Cap", "Loan Cap 2", "Mortgage Cap", "해당사항 없음" };

        public void SetBorrowerPropertyCount(int count)
        {
            BorrowerPropertyCount = Math.Max(1, count);
            BuildRecoveryStrategyRows();
        }

        /// <summary>
        /// PropertyRepository를 통해 차주별 물건 수를 직접 조회하여 설정
        /// </summary>
        private async Task ResolveBorrowerPropertyCountAsync()
        {
            if (_property == null || !_property.ProgramId.HasValue || string.IsNullOrEmpty(_property.BorrowerNumber))
            {
                SetBorrowerPropertyCount(1);
                return;
            }

            try
            {
                var propRepo = App.ServiceProvider?.GetService(typeof(PropertyRepository)) as PropertyRepository;
                if (propRepo != null)
                {
                    var allProps = await propRepo.GetByProgramIdAsync(_property.ProgramId.Value);
                    var count = allProps.Count(p => p.BorrowerNumber == _property.BorrowerNumber);
                    SetBorrowerPropertyCount(count);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EvaluationTab] 차주별 물건 수 조회 실패: {ex.Message}");
            }

            SetBorrowerPropertyCount(1);
        }

        private void BuildRecoveryStrategyRows()
        {
            RecoveryStrategyRows.Clear();

            RecoveryStrategyRows.Add(new() { Label = "Cdate 이후 회수/지출" });
            RecoveryStrategyRows.Add(new() { Label = "신용보증서 회수" });

            for (int i = 1; i <= BorrowerPropertyCount; i++)
                RecoveryStrategyRows.Add(new() { Label = $"Cdate 이후 경매비용 {i}" });

            for (int i = 1; i <= BorrowerPropertyCount; i++)
                RecoveryStrategyRows.Add(new() { Label = $"담보 {i} 배당회수" });

            RecoveryStrategyRows.Add(new() { Label = "MCI 회수" });
            RecoveryStrategyRows.Add(new() { Label = "합계", IsTotal = true });
            RecoveryStrategyRows.Add(new() { Label = "XNPV" });
            RecoveryStrategyRows.Add(new() { Label = "OPB" });
        }

        private void InitializeHouseEvalRows()
        {
            var categories = new[] { "토지", "토지(법면, 도로 등)", "건물", "Total" };
            HouseScenario1Rows.Clear();
            HouseScenario2Rows.Clear();
            foreach (var cat in categories)
            {
                HouseScenario1Rows.Add(new FactoryEvalRow { Category = cat });
                HouseScenario2Rows.Add(new FactoryEvalRow { Category = cat });
            }
        }

        private void InitializeCommercialEvalRows()
        {
            var categories = new[] { "토지", "건물", "Total" };
            CommercialScenario1Rows.Clear();
            CommercialScenario2Rows.Clear();
            foreach (var cat in categories)
            {
                CommercialScenario1Rows.Add(new FactoryEvalRow { Category = cat });
                CommercialScenario2Rows.Add(new FactoryEvalRow { Category = cat });
            }
        }

        private void InitializeFactoryEvalRows()
        {
            var categories = new[] { "토지", "토지(법면, 도로 등)", "건물", "기계", "Total" };

            FactoryScenario1Rows.Clear();
            FactoryScenario2Rows.Clear();

            foreach (var cat in categories)
            {
                FactoryScenario1Rows.Add(new FactoryEvalRow { Category = cat });
                FactoryScenario2Rows.Add(new FactoryEvalRow { Category = cat });
            }

            // LotEvalRows는 동적 데이터 연동 시 채워짐 (Total 행은 XAML에서 별도 표시)
        }

        #endregion

        #region 속성

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        /// <summary>
        /// E-001: 물건 주소 (지도 팝업용)
        /// </summary>
        public string? PropertyAddress => _property?.AddressFull ?? _property?.AddressJibun;

        // === 평가 유형 선택 ===
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CaseMapSectionTitle))]
        [NotifyPropertyChangedFor(nameof(IsShowRealTransaction))]
        [NotifyPropertyChangedFor(nameof(AmountColumnHeader))]
        private bool _isApartmentType = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CaseMapSectionTitle))]
        [NotifyPropertyChangedFor(nameof(IsShowRealTransaction))]
        [NotifyPropertyChangedFor(nameof(AmountColumnHeader))]
        private bool _isMultiFamilyType;

        /// <summary>
        /// 사례지도 섹션 제목 (주택/토지: "사례지도", 기타: "사례지도 및 실거래가")
        /// </summary>
        public string CaseMapSectionTitle => "사례지도 및 실거래가";

        /// <summary>
        /// 실거래가 표시 여부 (전 유형 표시)
        /// </summary>
        public bool IsShowRealTransaction => true;

        /// <summary>
        /// 거래금액 컬럼 헤더 (아파트=총거래가, 그 외 집합건물=단가)
        /// </summary>
        public string AmountColumnHeader => IsApartmentType ? "거래금액(만원)" : "단가(만원/㎡)";

        public bool IsFactoryOrCommercialType => IsFactoryType || IsCommercialType;

        public bool IsCommercialOrHouseType => IsCommercialType || IsHouseLandType;

        public bool IsFactoryOrCommercialOrHouseType => IsFactoryType || IsCommercialType || IsHouseLandType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CaseMapSectionTitle))]
        [NotifyPropertyChangedFor(nameof(IsShowRealTransaction))]
        [NotifyPropertyChangedFor(nameof(AmountColumnHeader))]
        [NotifyPropertyChangedFor(nameof(IsFactoryOrCommercialType))]
        [NotifyPropertyChangedFor(nameof(IsFactoryOrCommercialOrHouseType))]
        private bool _isFactoryType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CaseMapSectionTitle))]
        [NotifyPropertyChangedFor(nameof(IsShowRealTransaction))]
        [NotifyPropertyChangedFor(nameof(AmountColumnHeader))]
        [NotifyPropertyChangedFor(nameof(IsFactoryOrCommercialType))]
        [NotifyPropertyChangedFor(nameof(IsCommercialOrHouseType))]
        [NotifyPropertyChangedFor(nameof(IsFactoryOrCommercialOrHouseType))]
        private bool _isCommercialType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CaseMapSectionTitle))]
        [NotifyPropertyChangedFor(nameof(IsShowRealTransaction))]
        [NotifyPropertyChangedFor(nameof(AmountColumnHeader))]
        [NotifyPropertyChangedFor(nameof(IsCommercialOrHouseType))]
        [NotifyPropertyChangedFor(nameof(IsFactoryOrCommercialOrHouseType))]
        private bool _isHouseLandType;

        private bool _suppressTypeSync;

        partial void OnIsApartmentTypeChanged(bool value)
        {
            if (_suppressTypeSync) return;
            if (value)
            {
                _suppressTypeSync = true;
                IsMultiFamilyType = false; IsFactoryType = false; IsCommercialType = false; IsHouseLandType = false;
                _suppressTypeSync = false;
                // 적용낙찰가율 설명은 낙찰통계 데이터 로드 시 자동 산출
            }
            IsDirty = true;
        }

        partial void OnIsMultiFamilyTypeChanged(bool value)
        {
            if (_suppressTypeSync) return;
            if (value)
            {
                _suppressTypeSync = true;
                IsApartmentType = false; IsFactoryType = false; IsCommercialType = false; IsHouseLandType = false;
                _suppressTypeSync = false;
            }
            IsDirty = true;
        }

        partial void OnIsFactoryTypeChanged(bool value)
        {
            if (_suppressTypeSync) return;
            if (value)
            {
                _suppressTypeSync = true;
                IsApartmentType = false; IsMultiFamilyType = false; IsCommercialType = false; IsHouseLandType = false;
                _suppressTypeSync = false;
            }
            IsDirty = true;
        }

        partial void OnIsCommercialTypeChanged(bool value)
        {
            if (_suppressTypeSync) return;
            if (value)
            {
                _suppressTypeSync = true;
                IsApartmentType = false; IsMultiFamilyType = false; IsFactoryType = false; IsHouseLandType = false;
                _suppressTypeSync = false;
            }
            IsDirty = true;
        }

        partial void OnIsHouseLandTypeChanged(bool value)
        {
            if (_suppressTypeSync) return;
            if (value)
            {
                _suppressTypeSync = true;
                IsApartmentType = false; IsMultiFamilyType = false; IsFactoryType = false; IsCommercialType = false;
                _suppressTypeSync = false;
            }
            IsDirty = true;
        }

        // === 상가구분 ===
        [ObservableProperty]
        private bool _isOfficeSubType;

        [ObservableProperty]
        private bool _isMediumLargeSubType;

        [ObservableProperty]
        private bool _isSmallSubType;

        [ObservableProperty]
        private bool _isCollectiveSubType;

        [ObservableProperty]
        private bool _isNoSubType = true;

        // === 사례평가 테이블 ===
        [ObservableProperty]
        private ObservableCollection<CaseRowItem> _caseItems = new();

        // === 실거래가 ===
        [ObservableProperty]
        private ObservableCollection<RealTransactionItem> _realTransactions = new();

        // === 거래가격/건수 그래프 ===
        [ObservableProperty]
        private ISeries[] _tradeSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _tradeXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] _tradeYAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ObservableCollection<string> _availableAreas = new();

        [ObservableProperty]
        private string? _selectedArea;

        partial void OnSelectedAreaChanged(string? value)
        {
            if (value != null)
                BuildTradeChart();
        }

        // === 낙찰통계 (피드백 반영: 시/군구/동 3×3 매트릭스) ===
        [ObservableProperty]
        private string? _regionName1;

        [ObservableProperty]
        private string? _regionName2;

        [ObservableProperty]
        private string? _regionName3;

        // 1년 평균 - 시/도
        [ObservableProperty]
        private decimal? _stats1Year_Rate1;

        [ObservableProperty]
        private int? _stats1Year_Count1;

        // 1년 평균 - 군/구
        [ObservableProperty]
        private decimal? _stats1Year_Rate2;

        [ObservableProperty]
        private int? _stats1Year_Count2;

        // 1년 평균 - 동
        [ObservableProperty]
        private decimal? _stats1Year_Rate3;

        [ObservableProperty]
        private int? _stats1Year_Count3;

        // 6개월 평균 - 시/도
        [ObservableProperty]
        private decimal? _stats6Month_Rate1;

        [ObservableProperty]
        private int? _stats6Month_Count1;

        // 6개월 평균 - 군/구
        [ObservableProperty]
        private decimal? _stats6Month_Rate2;

        [ObservableProperty]
        private int? _stats6Month_Count2;

        // 6개월 평균 - 동
        [ObservableProperty]
        private decimal? _stats6Month_Rate3;

        [ObservableProperty]
        private int? _stats6Month_Count3;

        // 3개월 평균 - 시/도
        [ObservableProperty]
        private decimal? _stats3Month_Rate1;

        [ObservableProperty]
        private int? _stats3Month_Count1;

        // 3개월 평균 - 군/구
        [ObservableProperty]
        private decimal? _stats3Month_Rate2;

        [ObservableProperty]
        private int? _stats3Month_Count2;

        // 3개월 평균 - 동
        [ObservableProperty]
        private decimal? _stats3Month_Rate3;

        [ObservableProperty]
        private int? _stats3Month_Count3;

        [ObservableProperty]
        private decimal? _appliedBidRate;

        /// <summary>
        /// 적용낙찰가율 퍼센트 표시용 (70.0 = 70%)
        /// TextBox 바인딩용: 비율(0.70) ↔ 퍼센트(70.0) 변환
        /// </summary>
        public decimal? AppliedBidRatePercent
        {
            get => AppliedBidRate.HasValue ? AppliedBidRate.Value * 100 : null;
            set
            {
                AppliedBidRate = value.HasValue ? value.Value / 100 : null;
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private string? _appliedBidRateDescription;

        // 변경사항 추적 (피드백 반영: 저장 확인용)
        [ObservableProperty]
        private bool _isDirty;

        // === 평가결과 시나리오 1 ===
        [ObservableProperty]
        private decimal? _scenario1_Amount;

        [ObservableProperty]
        private decimal? _scenario1_Rate;

        [ObservableProperty]
        private string? _scenario1_Reason = "";

        // === 평가결과 시나리오 2 ===
        [ObservableProperty]
        private decimal? _scenario2_Amount;

        [ObservableProperty]
        private decimal? _scenario2_Rate;

        [ObservableProperty]
        private string? _scenario2_Reason = "";

        // === 평가결과: 토지/건물 평당 낙찰가 (피드백 반영) ===
        [ObservableProperty]
        private decimal? _scenario1_LandPerPyung;

        [ObservableProperty]
        private decimal? _scenario1_BuildingPerPyung;

        [ObservableProperty]
        private decimal? _scenario2_LandPerPyung;

        [ObservableProperty]
        private decimal? _scenario2_BuildingPerPyung;

        // === 회수 전략 요약 (신규 추가) ===
        [ObservableProperty]
        private DateTime? _recovery1_CFDate;

        [ObservableProperty]
        private decimal? _recovery1_CollateralAmount;

        [ObservableProperty]
        private decimal? _recovery1_AuctionCost;

        [ObservableProperty]
        private decimal? _recovery1_GuaranteeAmount;

        [ObservableProperty]
        private decimal? _recovery1_Ratio;

        [ObservableProperty]
        private DateTime? _recovery2_CFDate;

        [ObservableProperty]
        private decimal? _recovery2_CollateralAmount;

        [ObservableProperty]
        private decimal? _recovery2_AuctionCost;

        [ObservableProperty]
        private decimal? _recovery2_GuaranteeAmount;

        [ObservableProperty]
        private decimal? _recovery2_Ratio;

        // === 차주별 배당 요약 (피드백 반영: 시나리오 3 제거) ===
        [ObservableProperty]
        private ObservableCollection<ScenarioSummaryItem> _scenarioSummaryItems = new();

        // === 유사물건 추천 ===
        [ObservableProperty]
        private ObservableCollection<RecommendCaseItem> _recommendedCases = new();

        [ObservableProperty]
        private bool _isRecommendLoading;

        [ObservableProperty]
        private string? _recommendStatusMessage;

        [ObservableProperty]
        private int _selectedRuleIndex = 1;

        [ObservableProperty]
        private string _selectedRegionScope = "big";

        [ObservableProperty]
        private RecommendCaseItem? _selectedRecommendCase;

        // === 유사물건 추천 페이지네이션 (피드백 반영: 5개씩 보여주기) ===
        private ObservableCollection<RecommendCaseItem> _allRecommendedCases = new();
        private const int PageSize = 5;
        private int _currentPage = 1;

        [ObservableProperty]
        private int _displayedCasesCount;

        [ObservableProperty]
        private int _totalCasesCount;

        public bool HasMoreCases => DisplayedCasesCount < TotalCasesCount;

        // === 사례 적용 슬롯 관리 ===
        private int _nextCaseSlot = 1; // 다음에 사용할 사례 슬롯 (1~4)

        #endregion

        #region 초기화

        /// <summary>
        /// 물건 ID 설정
        /// </summary>
        public void SetPropertyId(Guid propertyId)
        {
            _propertyId = propertyId;
        }

        /// <summary>
        /// 물건 정보 설정
        /// </summary>
        public void SetProperty(Property property)
        {
            _property = property;

            // 물건 유형에 따라 평가 유형 자동 선택
            AutoSelectEvaluationType(property.PropertyType);

            // 유형별 사례평가 컬럼 재초기화
            InitializeCaseItems();

            // 지역명 설정
            SetRegionFromAddress(property.AddressFull);

            // 차주별 첫 번째 물건 여부 판별 (R-0009_1 → _1이면 첫 번째)
            IsFirstPropertyInBorrower = IsFirstPropertyByNumber(property.PropertyNumber);
        }

        /// <summary>
        /// PropertyNumber에서 차주별 첫 번째 물건인지 판별
        /// 형식: R-XXXX_N (N=1이면 첫 번째). 언더스코어가 없으면 첫 번째로 간주.
        /// </summary>
        private static bool IsFirstPropertyByNumber(string? propertyNumber)
        {
            if (string.IsNullOrWhiteSpace(propertyNumber))
                return true;

            var underscoreIdx = propertyNumber.LastIndexOf('_');
            if (underscoreIdx < 0 || underscoreIdx == propertyNumber.Length - 1)
                return true;

            var suffix = propertyNumber[(underscoreIdx + 1)..];
            return suffix == "1" || suffix == "01";
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        public async Task LoadAsync()
        {
            if (_propertyId == Guid.Empty)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 차주별 물건 수 자동 결정 (PropertyRepository 직접 조회)
                await ResolveBorrowerPropertyCountAsync();

                // 기존 평가 정보 로드
                _evaluation = await _evaluationRepository.GetByPropertyIdAsync(_propertyId);

                if (_evaluation != null)
                {
                    LoadFromEvaluation(_evaluation);
                }
                else
                {
                    // 새 평가 초기화
                    InitializeNewEvaluation();
                }

                // 실거래가 자동 조회 (PNU 기반)
                await LoadRealTransactionsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// PNU 기반 실거래가 자동 조회
        /// </summary>
        private async Task LoadRealTransactionsAsync()
        {
            RealTransactions.Clear();
            TradeSeries = Array.Empty<ISeries>();
            TradeXAxes = Array.Empty<Axis>();
            TradeYAxes = Array.Empty<Axis>();
            AvailableAreas.Clear();
            SelectedArea = null;

            if (_property == null)
                return;

            var pnu = _property.Pnu;

            // PNU가 없으면 VworldService로 주소 → PNU 자동 변환
            if (string.IsNullOrWhiteSpace(pnu))
            {
                var cleanAddress = CleanAddressForPnuLookup();
                Debug.WriteLine($"[EvaluationTab] PNU 없음 → 정제 주소로 PNU 자동 조회: {cleanAddress}");

                try
                {
                    var vworldService = App.ServiceProvider?.GetService(typeof(VworldService)) as VworldService;
                    if (vworldService != null && !string.IsNullOrWhiteSpace(cleanAddress))
                    {
                        var result = await vworldService.SearchAddressAsync(cleanAddress);
                        if (result != null && result.IsValidPnu)
                        {
                            pnu = result.Pnu;
                            _property.Pnu = pnu;
                            Debug.WriteLine($"[EvaluationTab] PNU 자동 확보 성공: {pnu}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EvaluationTab] PNU 자동 조회 실패: {ex.Message}");
                }
            }

            Debug.WriteLine($"[EvaluationTab] 실거래가 조회 - PNU: {pnu ?? "(없음)"}");
            if (string.IsNullOrWhiteSpace(pnu))
                return;

            // 물건 유형 → 실거래가 카테고리 매핑
            var category = MapPropertyTypeToTradeCategory(_property.PropertyType);
            var supabaseService = App.ServiceProvider?.GetService(typeof(SupabaseService)) as SupabaseService;
            var clientUserId = supabaseService?.GetCurrentUser()?.Email ?? "NPLogic-WPF";
            var trades = await _tradeService.GetTradesByPnuAsync(pnu, category, clientUserId: clientUserId);

            // 거래일자 파싱 헬퍼
            DateTime? ParseDealDate(int dealDate)
            {
                var ds = dealDate.ToString();
                if (ds.Length == 8 &&
                    int.TryParse(ds[..4], out var yy) &&
                    int.TryParse(ds[4..6], out var mm) &&
                    int.TryParse(ds[6..8], out var dd))
                {
                    try { return new DateTime(yy, mm, dd); } catch { }
                }
                return null;
            }

            // 아파트: BuildingArea 기준 동일면적 우선 + 1년 필터 + 유사면적 폴백
            var propertyArea = _property.BuildingArea.HasValue ? (double)_property.BuildingArea.Value : (double?)null;
            var oneYearAgo = DateTime.Now.AddYears(-1);

            List<TradeRecord> sorted;
            if (propertyArea.HasValue && IsApartmentType)
            {
                // 동일면적 (소수점 오차 0.01 허용) + 1년 이내 → 최근순
                var sameArea = trades
                    .Where(t => Math.Abs(t.Area - propertyArea.Value) < 0.01 && ParseDealDate(t.DealDate) >= oneYearAgo)
                    .OrderByDescending(t => t.DealDate)
                    .ToList();

                // 나머지: 면적 차이 작은 순 → 최근순
                var sameAreaSet = new HashSet<TradeRecord>(sameArea);
                var others = trades
                    .Where(t => !sameAreaSet.Contains(t))
                    .OrderBy(t => Math.Abs(t.Area - propertyArea.Value))
                    .ThenByDescending(t => t.DealDate)
                    .ToList();

                sorted = sameArea;
                sorted.AddRange(others);
                Debug.WriteLine($"[EvaluationTab] 실거래가 면적 필터: 물건면적={propertyArea.Value:F2}㎡, 동일면적 {sameArea.Count}건, 기타 {others.Count}건");
            }
            else
            {
                // 비아파트 또는 면적 정보 없음: 최근순 전체
                sorted = trades.OrderByDescending(t => t.DealDate).ToList();
            }

            foreach (var t in sorted)
            {
                // 아파트: 총거래가(만원), 비아파트 집합건물: 전용면적당 단가(만원/㎡)
                decimal displayAmount;
                if (IsApartmentType)
                {
                    displayAmount = t.DealAmount;
                }
                else
                {
                    displayAmount = t.Area > 0
                        ? Math.Round((decimal)t.DealAmount / (decimal)t.Area, 1)
                        : t.DealAmount;
                }

                RealTransactions.Add(new RealTransactionItem
                {
                    Area = (decimal)t.Area,
                    TransactionDate = ParseDealDate(t.DealDate),
                    Amount = displayAmount,
                    OriginalDealAmount = t.DealAmount,
                    Floor = t.Floor,
                    IsRegistered = t.IsRegistered ? "Y" : "N",
                    IsApplied = false,
                    DealDateRaw = t.DealDate,
                    AreaRaw = t.Area
                });
            }

            // DB에서 적용 상태 복원
            if (_tradeAppliedRepository != null && _propertyId != Guid.Empty)
            {
                try
                {
                    var appliedRows = await _tradeAppliedRepository.GetByPropertyIdAsync(_propertyId);
                    var appliedSet = new System.Collections.Generic.HashSet<string>(
                        appliedRows.Select(a => $"{a.DealDate}|{a.DealAmount}|{a.Area:R}|{a.Floor ?? ""}")
                    );

                    foreach (var txn in RealTransactions)
                    {
                        var key = $"{txn.DealDateRaw}|{txn.OriginalDealAmount}|{txn.AreaRaw:R}|{txn.Floor ?? ""}";
                        if (appliedSet.Contains(key))
                            txn.IsApplied = true;
                    }

                    Debug.WriteLine($"[EvaluationTab] 적용 상태 복원: {appliedRows.Count}건 중 매칭됨");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EvaluationTab] 적용 상태 복원 실패: {ex.Message}");
                }
            }

            // 면적 드롭다운 + 차트 초기화
            InitializeTradeChart(trades);
        }

        /// <summary>
        /// 실거래가 차트 초기화: 면적 목록 구성 + 기본 면적 선택
        /// </summary>
        private void InitializeTradeChart(List<TradeRecord> trades)
        {
            AvailableAreas.Clear();

            var areaGroups = trades
                .GroupBy(t => t.Area)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            foreach (var area in areaGroups)
                AvailableAreas.Add($"{area:F2}㎡");

            if (AvailableAreas.Count > 0)
            {
                var propertyArea = _property?.BuildingArea.HasValue == true ? (double)_property.BuildingArea.Value : 0.0;
                var match = areaGroups.FirstOrDefault(a => Math.Abs(a - propertyArea) < 0.01);
                SelectedArea = match > 0 ? $"{match:F2}㎡" : AvailableAreas[0];
            }
        }

        /// <summary>
        /// 선택된 면적 기준 거래가격/건수 이중축 차트 생성
        /// </summary>
        private void BuildTradeChart()
        {
            if (string.IsNullOrEmpty(SelectedArea) || RealTransactions.Count == 0)
                return;

            var areaStr = SelectedArea.Replace("㎡", "").Trim();
            if (!double.TryParse(areaStr, out var targetArea))
                return;

            var filtered = RealTransactions
                .Where(t => Math.Abs(t.AreaRaw - targetArea) < 0.01 && t.TransactionDate.HasValue)
                .ToList();

            if (filtered.Count == 0)
                return;

            var byYear = filtered
                .GroupBy(t => t.TransactionDate!.Value.Year)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Year = g.Key,
                    AvgAmount = (double)g.Average(t => t.Amount ?? 0) / 10000.0,
                    Count = g.Count()
                })
                .ToList();

            var years = byYear.Select(y => $"{y.Year % 100:D2}년").ToArray();
            var amounts = byYear.Select(y => y.AvgAmount).ToArray();
            var counts = byYear.Select(y => (double)y.Count).ToArray();

            var maxAmount = amounts.Max();
            var minAmount = amounts.Min();
            var amountStep = CalculateNiceStep(maxAmount - minAmount, 5);
            var amountMin = Math.Floor(minAmount / amountStep) * amountStep;
            var amountMax = Math.Ceiling(maxAmount / amountStep) * amountStep;
            if (amountMin == amountMax) amountMax = amountMin + amountStep;

            var maxCount = counts.Max();
            var countStep = CalculateNiceStep(maxCount, 5);
            var countMax = Math.Ceiling(maxCount / countStep) * countStep;
            if (countMax == 0) countMax = countStep;

            TradeSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "거래건수",
                    Values = counts,
                    Fill = new SolidColorPaint(SKColor.Parse("#90CAF9")),
                    MaxBarWidth = 30,
                    ScalesYAt = 1
                },
                new LineSeries<double>
                {
                    Name = "평균금액(억)",
                    Values = amounts,
                    Stroke = new SolidColorPaint(SKColor.Parse("#E53935"), 2.5f),
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#E53935"), 2.5f),
                    GeometrySize = 6,
                    GeometryFill = new SolidColorPaint(SKColor.Parse("#E53935")),
                    Fill = null,
                    LineSmoothness = 0,
                    ScalesYAt = 0
                }
            };

            TradeXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = years,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#666666")),
                    TextSize = 11,
                    SeparatorsPaint = null
                }
            };

            TradeYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "금액(억)",
                    NamePaint = new SolidColorPaint(SKColor.Parse("#E53935")),
                    NameTextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#E53935")),
                    TextSize = 11,
                    Labeler = v => v.ToString("F2"),
                    MinLimit = amountMin,
                    MaxLimit = amountMax,
                    MinStep = amountStep,
                    Position = LiveChartsCore.Measure.AxisPosition.Start,
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E0E0E0")) { StrokeThickness = 1 }
                },
                new Axis
                {
                    Name = "건수",
                    NamePaint = new SolidColorPaint(SKColor.Parse("#1976D2")),
                    NameTextSize = 11,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#1976D2")),
                    TextSize = 11,
                    MinLimit = 0,
                    MaxLimit = countMax,
                    MinStep = countStep,
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    SeparatorsPaint = null,
                    ShowSeparatorLines = false
                }
            };
        }

        private static double CalculateNiceStep(double range, int targetSteps)
        {
            if (range <= 0) return 1;
            var rawStep = range / targetSteps;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            var normalized = rawStep / magnitude;
            double niceStep;
            if (normalized <= 1.5) niceStep = 1;
            else if (normalized <= 3.5) niceStep = 2;
            else if (normalized <= 7.5) niceStep = 5;
            else niceStep = 10;
            return niceStep * magnitude;
        }

        /// <summary>
        /// address_full에서 건물명/동/층/호를 제거하고 순수 지번 주소만 추출
        /// 예: "경기도 안산시 상록구 사동 1536 푸른마을5단지 제515동 제3층 제302호"
        ///   → "경기도 안산시 상록구 사동 1536"
        /// </summary>
        private string? CleanAddressForPnuLookup()
        {
            if (_property == null) return null;

            var province = _property.AddressProvince?.Trim();
            var city = _property.AddressCity?.Trim();
            var district = _property.AddressDistrict?.Trim();

            // 담보소재지 1/2/3이 모두 있으면 이를 기반으로 정제
            if (!string.IsNullOrWhiteSpace(province) &&
                !string.IsNullOrWhiteSpace(city) &&
                !string.IsNullOrWhiteSpace(district))
            {
                var baseAddress = $"{province} {city} {district}";

                // address_full에서 지번번호 추출
                var full = _property.AddressFull;
                if (!string.IsNullOrWhiteSpace(full))
                {
                    var distIdx = full.IndexOf(district);
                    if (distIdx >= 0)
                    {
                        var afterDistrict = full.Substring(distIdx + district.Length).TrimStart();
                        // 지번번호 패턴: "1536", "461", "279-8", "166" 등
                        var jibunMatch = Regex.Match(afterDistrict, @"^(\d+(-\d+)?)");
                        if (jibunMatch.Success)
                        {
                            var result = $"{baseAddress} {jibunMatch.Value}";
                            Debug.WriteLine($"[EvaluationTab] 주소 정제: '{full}' → '{result}'");
                            return result;
                        }
                    }
                }

                // 지번번호 못 찾으면 기본 주소만 반환
                Debug.WriteLine($"[EvaluationTab] 주소 정제 (지번 미발견): '{baseAddress}'");
                return baseAddress;
            }

            // 담보소재지 필드 없으면 address_full에서 직접 정제
            var addressFull = _property.AddressFull;
            if (string.IsNullOrWhiteSpace(addressFull))
                return _property.DisplayAddress;

            // "제N동 제N층 제N호" 패턴 이전까지만 사용
            var cleaned = Regex.Replace(addressFull, @"\s+제?\d+동\s+제?\d+층.*$", "").Trim();
            // 건물명 제거: 지번번호 뒤의 한글 건물명
            cleaned = Regex.Replace(cleaned, @"(\d+(-\d+)?)\s+\S*[가-힣]+(아파트|맨숀|빌라|타워|단지|타운).*$", "$1").Trim();
            // 괄호 내용 제거
            cleaned = Regex.Replace(cleaned, @"\(.*?\)", "").Trim();
            // 쉼표 이후 제거 (복수 필지)
            var commaIdx = cleaned.IndexOf(',');
            if (commaIdx > 0) cleaned = cleaned.Substring(0, commaIdx).Trim();

            Debug.WriteLine($"[EvaluationTab] 주소 정제 (fallback): '{addressFull}' → '{cleaned}'");
            return cleaned;
        }

        /// <summary>
        /// 물건 유형 → 실거래가 API 카테고리 매핑
        /// </summary>
        private static string? MapPropertyTypeToTradeCategory(string? propertyType)
        {
            if (string.IsNullOrWhiteSpace(propertyType)) return null;

            if (propertyType.Contains("아파트") && !propertyType.Contains("공장"))
                return "apt";
            if (propertyType.Contains("연립") || propertyType.Contains("다세대") || propertyType.Contains("빌라"))
                return "villa";
            if (propertyType.Contains("오피스텔"))
                return "officetel";
            if (propertyType.Contains("상가") || propertyType.Contains("근린") || propertyType.Contains("업무"))
                return "commercial";
            if (propertyType.Contains("공장") || propertyType.Contains("창고"))
                return "apt_factory";

            return null; // 매핑 안 되면 전체 카테고리로 조회
        }

        private void InitializeCaseItems()
        {
            if (IsFactoryType)
            {
                CaseItems = new ObservableCollection<CaseRowItem>
                {
                    new CaseRowItem { Label = "경매사건번호" },
                    new CaseRowItem { Label = "낙찰일자" },
                    new CaseRowItem { Label = "용도" },
                    new CaseRowItem { Label = "소재지" },
                    new CaseRowItem { Label = "토지면적(평)" },
                    new CaseRowItem { Label = "건물연면적(평)" },
                    new CaseRowItem { Label = "기계기구" },
                    new CaseRowItem { Label = "보존등기일" },
                    new CaseRowItem { Label = "사용승인일" },
                    new CaseRowItem { Label = "법사가" },
                    new CaseRowItem { Label = "토지" },
                    new CaseRowItem { Label = "건물" },
                    new CaseRowItem { Label = "제시외" },
                    new CaseRowItem { Label = "기계기구" },
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
                    new CaseRowItem { Label = "토지평당낙찰가" },
                    new CaseRowItem { Label = "건물평당낙찰가" },
                    new CaseRowItem { Label = "기계인정율" },
                    new CaseRowItem { Label = "토지단가" },
                    new CaseRowItem { Label = "건물단가" },
                    new CaseRowItem { Label = "토지 거래분" },
                    new CaseRowItem { Label = "건물 거래분" },
                    new CaseRowItem { Label = "제시외 거래분" },
                    new CaseRowItem { Label = "기계기구 낙찰분" },
                    new CaseRowItem { Label = "낙찰가율" },
                    new CaseRowItem { Label = "용적율" },
                    new CaseRowItem { Label = "기계인정율" },
                    new CaseRowItem { Label = "토지평당낙찰가" },
                    new CaseRowItem { Label = "건물평당낙찰가" },
                    new CaseRowItem { Label = "공시지가" },
                    new CaseRowItem { Label = "2025" },
                    new CaseRowItem { Label = "2024" },
                    new CaseRowItem { Label = "2023" },
                    new CaseRowItem { Label = "2등 입찰가" },
                    new CaseRowItem { Label = "사례 비고 사항" }
                };
            }
            else
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
                    new CaseRowItem { Label = "토지평당낙찰가" },
                    new CaseRowItem { Label = "건물평당낙찰가" },
                    new CaseRowItem { Label = "토지건물대비" },
                    new CaseRowItem { Label = "낙찰가율" },
                    new CaseRowItem { Label = "용적율" },
                    new CaseRowItem { Label = "2등 입찰가" },
                    new CaseRowItem { Label = "사례 비고 사항" }
                };
            }
        }

        private void InitializeNewEvaluation()
        {
            // 물건 정보에서 기본값 설정
            if (_property != null)
            {
                // 감정가 기반 시나리오 계산 (피드백 반영: 시나리오 1=하한, 2=상한)
                if (_property.AppraisalValue.HasValue && AppliedBidRate.HasValue)
                {
                    // 시나리오 1 (하한): 적용 낙찰가율 - 5%
                    var lowerRate = AppliedBidRate.Value - 0.05m;
                    if (lowerRate < 0.3m) lowerRate = 0.3m; // 최소 30%
                    Scenario1_Amount = _property.AppraisalValue.Value * lowerRate;
                    Scenario1_Rate = lowerRate;
                    
                    // 시나리오 2 (상한): 적용 낙찰가율 + 5%
                    Scenario2_Amount = _property.AppraisalValue.Value * (AppliedBidRate.Value + 0.05m);
                    Scenario2_Rate = AppliedBidRate + 0.05m;
                }
            }
            
            // 배당 요약 테이블 초기화
            InitializeScenarioSummary();
        }

        /// <summary>
        /// 시나리오별 배당 요약 테이블 초기화 (피드백 반영: 시나리오 3 제거)
        /// </summary>
        private void InitializeScenarioSummary()
        {
            ScenarioSummaryItems = new ObservableCollection<ScenarioSummaryItem>
            {
                new ScenarioSummaryItem 
                { 
                    Label = "경매비용",
                    Scenario1Value = CalculateAuctionCost(Scenario1_Amount),
                    Scenario2Value = CalculateAuctionCost(Scenario2_Amount)
                },
                new ScenarioSummaryItem 
                { 
                    Label = "배당회수",
                    Scenario1Value = CalculateDistribution(Scenario1_Amount),
                    Scenario2Value = CalculateDistribution(Scenario2_Amount)
                },
                new ScenarioSummaryItem 
                { 
                    Label = "상계회수",
                    Scenario1Value = 0, // 인터림 파일에서 가져옴 (추후 구현)
                    Scenario2Value = 0
                },
                new ScenarioSummaryItem 
                { 
                    Label = "현금흐름",
                    Scenario1Value = Scenario1_Amount,
                    Scenario2Value = Scenario2_Amount
                }
            };
            
            // 합계 계산
            foreach (var item in ScenarioSummaryItems)
            {
                item.TotalValue = (item.Scenario1Value ?? 0) + 
                                  (item.Scenario2Value ?? 0);
            }
        }

        /// <summary>
        /// E-002: 경매비용 계산 (낙찰가의 약 3%)
        /// </summary>
        private decimal? CalculateAuctionCost(decimal? amount)
        {
            return amount.HasValue ? amount.Value * 0.03m : null;
        }

        /// <summary>
        /// E-002: 배당회수 계산 (낙찰가 - 경매비용 - 선순위)
        /// </summary>
        private decimal? CalculateDistribution(decimal? amount)
        {
            if (!amount.HasValue) return null;
            var cost = CalculateAuctionCost(amount) ?? 0;
            return amount.Value - cost;
        }

        private void LoadFromEvaluation(EvaluationModel evaluation)
        {
            // 평가 유형 설정
            SetEvaluationType(evaluation.EvaluationType);
            
            // 평가 결과 로드
            var details = evaluation.EvaluationDetails;
            if (details != null)
            {
                // 시나리오 1
                if (details.Scenario1 != null)
                {
                    Scenario1_Amount = details.Scenario1.EvaluatedValue;
                    Scenario1_Rate = details.Scenario1.BidRate;
                    Scenario1_Reason = details.Scenario1.EvaluationReason;
                }
                
                // 시나리오 2
                if (details.Scenario2 != null)
                {
                    Scenario2_Amount = details.Scenario2.EvaluatedValue;
                    Scenario2_Rate = details.Scenario2.BidRate;
                    Scenario2_Reason = details.Scenario2.EvaluationReason;
                }
                
                // 적용 낙찰가율
                if (details.AppliedBidRate.HasValue)
                {
                    AppliedBidRate = details.AppliedBidRate;
                }
                
                // 사례 정보 로드
                LoadCaseInfo(details);
            }
        }

        private void LoadCaseInfo(EvaluationDetails details)
        {
            // 사례 1~4 정보를 테이블에 반영
            // TODO: 실제 사례 데이터 매핑
        }

        /// <summary>
        /// 물건 유형에 따른 평가 유형 자동 선택 (원청 '평가_시트적용' 매핑 테이블 기준)
        /// </summary>
        /// <remarks>
        /// 매핑 규칙 (카테고리별):
        /// [주거용]
        ///   아파트, 오피스텔 → 1. 아파트
        ///   다세대(빌라), 연립 → 2. 연립다세대
        ///   단독주택, 다가구, 다중주택, 근린주택 → 5. 주택/근린시설/토지/기타
        /// [상업용 및 업무용]
        ///   근린상가, 사무실 → 4. 상가/아파트형공장
        ///   아파트형공장 → 4. 상가/아파트형공장
        ///   공장, 창고 → 3. 공장/창고
        ///   숙박시설, 콘도, 교육시설, 종교시설, 의료시설, 목욕탕, 노유자시설, 문화및집회시설
        ///     → 기본: 5. 주택/근린시설/토지/기타 (대체: 4. 상가/아파트형공장)
        ///   농가관련시설, 주유소, 자동차관련시설 → 5. 주택/근린시설/토지/기타
        /// [토지]
        ///   대지, 임야, 전, 답, 과수원, 도로, 묘지, 잡종지, 목장용지, 광천지, 염전, 공장용지
        ///     → 5. 주택/근린시설/토지/기타
        /// [차량 및 선박]
        ///   차량, 선박 → 5. 주택/근린시설/토지/기타
        /// [기타]
        ///   기타 → 5. 주택/근린시설/토지/기타
        /// </remarks>
        private void AutoSelectEvaluationType(string? propertyType)
        {
            // 모든 유형 초기화
            IsApartmentType = false;
            IsMultiFamilyType = false;
            IsFactoryType = false;
            IsCommercialType = false;
            IsHouseLandType = false;

            if (string.IsNullOrWhiteSpace(propertyType))
            {
                IsApartmentType = true; // 기본값: 아파트
                return;
            }

            var type = propertyType.Trim();

            // 1. 아파트 - 아파트, 오피스텔 (아파트형공장 제외)
            if ((type.Contains("아파트") && !type.Contains("아파트형공장")) ||
                type.Contains("오피스텔"))
            {
                IsApartmentType = true;
                return;
            }

            // 2. 연립다세대 - 다세대, 빌라, 연립
            if (type.Contains("다세대") || type.Contains("빌라") || type.Contains("연립"))
            {
                IsMultiFamilyType = true;
                return;
            }

            // 3. 공장/창고 - 공장, 창고 (아파트형공장 제외)
            if ((type.Contains("공장") && !type.Contains("아파트형")) || type.Contains("창고"))
            {
                IsFactoryType = true;
                return;
            }

            // 4. 상가/아파트형공장 - 근린상가, 상가, 사무실, 아파트형공장
            if (type.Contains("상가") || type.Contains("사무실") || type.Contains("아파트형공장"))
            {
                IsCommercialType = true;
                return;
            }

            // 5. 주택/근린시설/토지/기타 - 나머지 전부
            // 주거용: 단독주택, 다가구, 다중주택, 근린주택
            // 상업용: 숙박시설, 콘도, 교육시설, 종교시설, 의료시설, 목욕탕, 노유자시설, 문화및집회시설
            //         농가관련시설, 주유소, 자동차관련시설
            // 토지: 대지, 임야, 전, 답, 과수원, 도로, 묘지, 잡종지, 목장용지, 광천지, 염전, 공장용지
            // 차량/선박, 기타
            IsHouseLandType = true;
        }

        private void SetEvaluationType(string? evaluationType)
        {
            if (string.IsNullOrWhiteSpace(evaluationType))
            {
                IsApartmentType = true;
                return;
            }

            IsApartmentType = evaluationType == "아파트";
            IsMultiFamilyType = evaluationType == "연립다세대";
            IsFactoryType = evaluationType == "공장창고";
            IsCommercialType = evaluationType == "상가";
            IsHouseLandType = evaluationType == "주택토지";
        }

        private string GetSelectedEvaluationType()
        {
            if (IsApartmentType) return "아파트";
            if (IsMultiFamilyType) return "연립다세대";
            if (IsFactoryType) return "공장창고";
            if (IsCommercialType) return "상가";
            if (IsHouseLandType) return "주택토지";
            return "아파트";
        }

        private void SetRegionFromAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return;

            var parts = address.Split(' ');
            if (parts.Length >= 1)
                RegionName1 = parts[0]; // 시/도
            if (parts.Length >= 2)
                RegionName2 = parts[1]; // 시/군/구
            if (parts.Length >= 3)
                RegionName3 = parts[2]; // 동
        }

        #endregion

        #region 명령

        /// <summary>
        /// 탐문 결과 테이블 생성 (고정 4행: 토지, 건물, 기계, 합계)
        /// </summary>
        [RelayCommand]
        private void CreateInquiryResultTable()
        {
            if (HasInquiryResultTable) return;
            InquiryResultRows.Add(new InquiryResultRow { Category = "토지" });
            InquiryResultRows.Add(new InquiryResultRow { Category = "건물" });
            InquiryResultRows.Add(new InquiryResultRow { Category = "기계" });
            InquiryResultRows.Add(new InquiryResultRow { Category = "합계" });
            HasInquiryResultTable = true;
        }

        /// <summary>
        /// 탐문 결과 테이블 제거
        /// </summary>
        [RelayCommand]
        private void DestroyInquiryResultTable()
        {
            InquiryResultRows.Clear();
            HasInquiryResultTable = false;
        }

        [RelayCommand]
        private void CreateRentalQuoteTable()
        {
            HasRentalQuoteTable = true;
        }

        [RelayCommand]
        private void DestroyRentalQuoteTable()
        {
            HasRentalQuoteTable = false;
        }

        [RelayCommand]
        private void CreateFreeRentTable()
        {
            HasFreeRentTable = true;
        }

        [RelayCommand]
        private void DestroyFreeRentTable()
        {
            HasFreeRentTable = false;
        }

        /// <summary>
        /// 탐문 수익가치 테이블 생성
        /// </summary>
        [RelayCommand]
        private void CreateInquiryProfitTable()
        {
            if (HasInquiryProfitTable) return;
            InquiryProfitData = new InquiryProfitData();
            HasInquiryProfitTable = true;
        }

        /// <summary>
        /// 탐문 수익가치 테이블 제거
        /// </summary>
        [RelayCommand]
        private void DestroyInquiryProfitTable()
        {
            InquiryProfitData = null;
            HasInquiryProfitTable = false;
        }

        /// <summary>
        /// 현재 임대 수익가치 테이블 생성
        /// </summary>
        [RelayCommand]
        private void CreateRentalProfitTable()
        {
            if (HasRentalProfitTable) return;
            RentalIncomeRows.Add(new RentalIncomeRow());
            RentalProfitSensitivity = new RentalProfitSensitivity();
            HasRentalProfitTable = true;
        }

        /// <summary>
        /// 현재 임대 수익가치 테이블 제거
        /// </summary>
        [RelayCommand]
        private void DestroyRentalProfitTable()
        {
            RentalIncomeRows.Clear();
            RentalProfitSensitivity = null;
            HasRentalProfitTable = false;
        }

        /// <summary>
        /// 탐문 내역 테이블 생성 (헤더 + 버튼)
        /// </summary>
        [RelayCommand]
        private void CreateInquiryTable()
        {
            if (HasInquiryTable) return;
            InquiryRows.Add(new InquiryRow());
            HasInquiryTable = true;
        }

        /// <summary>
        /// 탐문 내역 테이블 제거
        /// </summary>
        [RelayCommand]
        private void DestroyInquiryTable()
        {
            InquiryRows.Clear();
            HasInquiryTable = false;
        }

        /// <summary>
        /// 탐문 내역 행 추가
        /// </summary>
        [RelayCommand]
        private void AddInquiryRow()
        {
            InquiryRows.Add(new InquiryRow());
        }

        /// <summary>
        /// 탐문 내역 행 삭제
        /// </summary>
        [RelayCommand]
        private void RemoveInquiryRow(InquiryRow? row)
        {
            if (row == null) return;
            InquiryRows.Remove(row);
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
                ErrorMessage = null;
                SuccessMessage = null;

                // 평가 정보 생성/업데이트
                var evaluation = _evaluation ?? new EvaluationModel();
                evaluation.PropertyId = _propertyId;
                evaluation.EvaluationType = GetSelectedEvaluationType();
                evaluation.EvaluatedValue = Scenario1_Amount;
                evaluation.RecoveryRate = Scenario1_Rate;
                evaluation.EvaluatedAt = DateTime.UtcNow;
                
                // 상세 정보 저장
                var details = evaluation.EvaluationDetails ?? new EvaluationDetails();
                details.AppliedBidRate = AppliedBidRate;
                details.Scenario1 = new ScenarioResult
                {
                    EvaluatedValue = Scenario1_Amount,
                    BidRate = Scenario1_Rate,
                    EvaluationReason = Scenario1_Reason
                };
                details.Scenario2 = new ScenarioResult
                {
                    EvaluatedValue = Scenario2_Amount,
                    BidRate = Scenario2_Rate,
                    EvaluationReason = Scenario2_Reason
                };
                evaluation.EvaluationDetails = details;

                _evaluation = await _evaluationRepository.SaveAsync(evaluation);

                // 실거래가 적용 상태 저장
                if (_tradeAppliedRepository != null)
                {
                    var appliedItems = RealTransactions
                        .Where(t => t.IsApplied)
                        .Select(t => new NPLogic.Core.Models.PropertyTradeApplied
                        {
                            PropertyId = _propertyId,
                            DealDate = t.DealDateRaw,
                            DealAmount = (int)(t.Amount ?? 0),
                            Area = t.AreaRaw,
                            Floor = t.Floor
                        })
                        .ToList();

                    await _tradeAppliedRepository.SaveAllAsync(_propertyId, appliedItems);
                    Debug.WriteLine($"[EvaluationTab] 실거래가 적용 저장: {appliedItems.Count}건");
                }

                IsDirty = false; // 저장 완료 후 변경사항 플래그 리셋
                SuccessMessage = "평가 정보가 저장되었습니다.";
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
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadAsync();
        }

        /// <summary>
        /// 사례지도 로드
        /// </summary>
        [RelayCommand]
        private void LoadCaseMap()
        {
            // TODO: 사례지도 로드 구현
            System.Windows.MessageBox.Show(
                "사례지도 기능은 추후 구현 예정입니다.\n\n소재지 기준으로 본건 위치와 주변 거래사례를 지도에 표시하는 기능입니다.",
                "사례지도",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// 경매사건 검색
        /// </summary>
        [RelayCommand]
        private void SearchAuctionCase()
        {
            // 대법원 경매정보 사이트 열기
            try
            {
                var url = "https://www.courtauction.go.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 유사물건 추천
        /// </summary>
        [RelayCommand]
        private async Task RecommendSimilarCasesAsync()
        {
            if (_property == null)
            {
                ErrorMessage = "물건 정보가 없습니다.";
                return;
            }

            try
            {
                IsRecommendLoading = true;
                RecommendStatusMessage = "유사물건을 검색 중입니다...";
                ErrorMessage = null;
                
                // 디버그: Supabase 연결 정보 확인
                Debug.WriteLine($"[EvaluationTab] SupabaseUrl: {SupabaseUrl ?? "NULL"}");
                Debug.WriteLine($"[EvaluationTab] SupabaseKey: {(string.IsNullOrEmpty(SupabaseKey) ? "NULL/EMPTY" : "SET (" + SupabaseKey.Length + " chars)")}");
                
                if (string.IsNullOrEmpty(SupabaseUrl) || string.IsNullOrEmpty(SupabaseKey))
                {
                    ErrorMessage = "Supabase 연결 정보가 설정되지 않았습니다. 앱을 재시작해주세요.";
                    return;
                }

                // 대상 물건 정보 구성
                var subject = new RecommendSubject
                {
                    PropertyId = _propertyId.ToString(),
                    Address = _property.AddressFull ?? _property.AddressJibun,
                    Usage = GetUsageFromEvaluationType(),
                    RegionBig = RegionName1,
                    RegionMid = RegionName2,
                    Latitude = _property.Latitude.HasValue ? (double?)Convert.ToDouble(_property.Latitude.Value) : null,
                    Longitude = _property.Longitude.HasValue ? (double?)Convert.ToDouble(_property.Longitude.Value) : null,
                    BuildingArea = _property.BuildingArea.HasValue ? (double?)Convert.ToDouble(_property.BuildingArea.Value) : null,
                    LandArea = _property.LandArea.HasValue ? (double?)Convert.ToDouble(_property.LandArea.Value) : null,
                    BuildingAppraisalPrice = _property.AppraisalValue
                };

                // 추천 옵션
                var options = new RecommendOptions
                {
                    RuleIndex = SelectedRuleIndex,
                    RegionScope = SelectedRegionScope,
                    TopK = 10,
                    SupabaseUrl = SupabaseUrl,
                    SupabaseKey = SupabaseKey
                };

                // 추천 실행
                var result = await _recommendService.RecommendAsync(subject, options);

                if (result.Success)
                {
                    // 피드백 반영: 전체 결과 저장 후 페이지네이션
                    _allRecommendedCases.Clear();
                    RecommendedCases.Clear();
                    
                    // E-005: 본건 좌표 추출 (거리 계산용)
                    var subjectLat = _property?.Latitude.HasValue == true ? (double)_property.Latitude.Value : (double?)null;
                    var subjectLon = _property?.Longitude.HasValue == true ? (double)_property.Longitude.Value : (double?)null;
                    
                    // 결과 변환
                    if (result.RuleResults != null)
                    {
                        foreach (var ruleResult in result.RuleResults)
                        {
                            foreach (var caseItem in ruleResult.Value)
                            {
                                // E-005: 거리 계산
                                double? distanceKm = null;
                                if (subjectLat.HasValue && subjectLon.HasValue && 
                                    caseItem.Latitude.HasValue && caseItem.Longitude.HasValue)
                                {
                                    distanceKm = CalculateDistanceKm(
                                        subjectLat.Value, subjectLon.Value,
                                        caseItem.Latitude.Value, caseItem.Longitude.Value);
                                }
                                
                                _allRecommendedCases.Add(new RecommendCaseItem
                                {
                                    CaseNo = caseItem.CaseNo,
                                    Address = caseItem.Address,
                                    Usage = caseItem.Usage,
                                    AuctionDate = DateTime.TryParse(caseItem.AuctionDate, out var date) ? date : null,
                                    AppraisalPrice = caseItem.AppraisalPrice,
                                    WinningPrice = caseItem.WinningPrice,
                                    BuildingArea = caseItem.BuildingArea,
                                    LandArea = caseItem.LandArea,
                                    Latitude = caseItem.Latitude,
                                    Longitude = caseItem.Longitude,
                                    RuleName = caseItem.RuleName,
                                    DistanceKm = distanceKm // E-005
                                });
                            }
                        }
                    }

                    // 피드백 반영: 5개씩 페이지네이션
                    TotalCasesCount = _allRecommendedCases.Count;
                    _currentPage = 1;
                    LoadPage();
                    
                    RecommendStatusMessage = $"추천 결과: {TotalCasesCount}건";
                    if (TotalCasesCount == 0)
                    {
                        RecommendStatusMessage = "조건에 맞는 유사물건이 없습니다.";
                    }
                }
                else
                {
                    ErrorMessage = result.Error ?? "추천 실패";
                    RecommendStatusMessage = null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"추천 실패: {ex.Message}";
                RecommendStatusMessage = null;
            }
            finally
            {
                IsRecommendLoading = false;
            }
        }

        /// <summary>
        /// 페이지네이션: 현재 페이지 로드
        /// </summary>
        private void LoadPage()
        {
            var itemsToShow = _allRecommendedCases.Take(_currentPage * PageSize).ToList();
            RecommendedCases.Clear();
            foreach (var item in itemsToShow)
            {
                RecommendedCases.Add(item);
            }
            DisplayedCasesCount = RecommendedCases.Count;
            OnPropertyChanged(nameof(HasMoreCases));
        }

        /// <summary>
        /// 피드백 반영: 더보기 (+5)
        /// </summary>
        [RelayCommand]
        private void LoadMoreCases()
        {
            if (HasMoreCases)
            {
                _currentPage++;
                LoadPage();
            }
        }

        /// <summary>
        /// 선택된 추천 사례를 사례평가에 적용
        /// </summary>
        [RelayCommand]
        private void ApplyRecommendedCase()
        {
            if (SelectedRecommendCase == null)
            {
                ErrorMessage = "적용할 사례를 선택하세요.";
                return;
            }

            try
            {
                // 선택된 사례의 낙찰가율을 적용
                if (SelectedRecommendCase.WinningRate.HasValue)
                {
                    AppliedBidRate = SelectedRecommendCase.WinningRate.Value / 100; // % → 비율 변환
                    AppliedBidRateDescription = $"유사물건 사례 적용 ({SelectedRecommendCase.CaseNo})";
                    
                    // 시나리오 1 재계산
                    CalculateScenario1();
                }

                // 사례평가 테이블에 데이터 반영
                ApplyCaseToEvaluationTable(SelectedRecommendCase, _nextCaseSlot);
                
                // 다음 슬롯으로 이동 (1~4 순환)
                int usedSlot = _nextCaseSlot;
                _nextCaseSlot = (_nextCaseSlot % 4) + 1;
                
                SuccessMessage = $"사례 {SelectedRecommendCase.CaseNo}가 사례{usedSlot}에 적용되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사례 적용 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 유사물건 상세보기 팝업
        /// </summary>
        [RelayCommand]
        private void ShowCaseDetail(RecommendCaseItem? caseItem)
        {
            if (caseItem == null)
                return;

            try
            {
                // 상세 정보 팝업 표시
                var message = $"사건번호: {caseItem.CaseNo}\n" +
                              $"소재지: {caseItem.Address}\n" +
                              $"용도: {caseItem.Usage}\n" +
                              $"낙찰일: {caseItem.AuctionDate:yyyy-MM-dd}\n\n" +
                              $"감정가: {caseItem.AppraisalPrice:N0}원\n" +
                              $"낙찰가: {caseItem.WinningPrice:N0}원\n" +
                              $"낙찰가율: {caseItem.WinningRateDisplay}\n\n" +
                              $"건물면적: {caseItem.BuildingArea:N1}㎡\n" +
                              $"토지면적: {caseItem.LandArea:N1}㎡\n" +
                              $"적용규칙: {caseItem.RuleName}";

                System.Windows.MessageBox.Show(message, $"유사물건 상세정보 - {caseItem.CaseNo}", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"상세보기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 사례평가 테이블 초기화
        /// </summary>
        [RelayCommand]
        private void ClearCaseEvaluation()
        {
            try
            {
                foreach (var item in CaseItems)
                {
                    item.Case1Value = null;
                    item.Case2Value = null;
                    item.Case3Value = null;
                    item.Case4Value = null;
                }
                _nextCaseSlot = 1;
                SuccessMessage = "사례평가가 초기화되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 추천 사례를 사례평가 테이블의 특정 슬롯에 적용
        /// </summary>
        private void ApplyCaseToEvaluationTable(RecommendCaseItem caseItem, int slot)
        {
            if (slot < 1 || slot > 4)
                return;

            // 각 행에 해당 슬롯의 값 설정
            foreach (var item in CaseItems)
            {
                string? value = null;

                switch (item.Label)
                {
                    case "사례구분":
                        value = $"사례{slot}";
                        break;
                    case "경매사건번호":
                        value = caseItem.CaseNo;
                        break;
                    case "낙찰일자":
                        value = caseItem.AuctionDate?.ToString("yyyy-MM-dd");
                        break;
                    case "용도":
                        value = caseItem.Usage;
                        break;
                    case "소재지":
                        value = caseItem.Address;
                        break;
                    case "토지면적(평)":
                        value = caseItem.LandArea.HasValue ? (caseItem.LandArea.Value / 3.3058).ToString("N1") : null;
                        break;
                    case "건물연면적(평)":
                        value = caseItem.BuildingArea.HasValue ? (caseItem.BuildingArea.Value / 3.3058).ToString("N1") : null;
                        break;
                    case "낙찰가액":
                        value = caseItem.WinningPrice?.ToString("N0");
                        break;
                    case "낙찰가율":
                        value = caseItem.WinningRateDisplay;
                        break;
                    case "법사가":
                    case "토지":
                    case "건물":
                        value = caseItem.AppraisalPrice?.ToString("N0");
                        break;
                }

                // 슬롯에 따라 해당 컬럼에 값 설정
                switch (slot)
                {
                    case 1:
                        item.Case1Value = value;
                        break;
                    case 2:
                        item.Case2Value = value;
                        break;
                    case 3:
                        item.Case3Value = value;
                        break;
                    case 4:
                        item.Case4Value = value;
                        break;
                }
            }
        }

        /// <summary>
        /// 평가 유형에서 용도 문자열 반환
        /// </summary>
        private string GetUsageFromEvaluationType()
        {
            if (IsApartmentType) return "아파트";
            if (IsMultiFamilyType) return "다세대";
            if (IsFactoryType) return "공장";
            if (IsCommercialType) return "근린상가";
            if (IsHouseLandType) return "주택";
            return "아파트";
        }

        /// <summary>
        /// 실거래가 조회
        /// </summary>
        [RelayCommand]
        private async Task FetchRealTransactionAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // TODO: 실제 API 연동 (국토교통부 실거래가 공개시스템)
                await Task.Delay(300);

                // 안내 메시지 표시
                System.Windows.MessageBox.Show(
                    "실거래가 API 연동은 추후 구현 예정입니다.\n\n" +
                    "현재는 샘플 데이터가 표시됩니다.\n" +
                    "실제 데이터는 '🔗 rt.molit.go.kr' 버튼을 눌러 직접 조회해주세요.",
                    "실거래가 조회",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                // 샘플 데이터 (참고용)
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
                RealTransactions.Add(new RealTransactionItem
                {
                    Area = 84.5m,
                    TransactionDate = DateTime.Now.AddMonths(-3),
                    Amount = 435000000,
                    Floor = "12",
                    IsRegistered = "Y",
                    IsApplied = true
                });

                SuccessMessage = "(샘플 데이터)";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"실거래가 조회 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 실거래가 사이트 열기
        /// </summary>
        [RelayCommand]
        private void OpenRealTransactionSite()
        {
            try
            {
                var url = "https://rt.molit.go.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// E-004: 시세 추이 사이트 열기 (KB부동산)
        /// </summary>
        [RelayCommand]
        private void OpenPriceTrendSite()
        {
            try
            {
                // KB부동산 시세 사이트 열기
                var url = "https://kbland.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        #endregion

        #region 계산 메서드

        /// <summary>
        /// 시나리오 1 (하한) 계산 - 적용 낙찰가율 - 5%
        /// </summary>
        public void CalculateScenario1()
        {
            if (_property?.AppraisalValue == null || !AppliedBidRate.HasValue)
                return;

            // 하한: 적용 낙찰가율 - 5%
            var lowerRate = AppliedBidRate.Value - 0.05m;
            if (lowerRate < 0.3m) lowerRate = 0.3m; // 최소 30%
            
            Scenario1_Amount = _property.AppraisalValue.Value * lowerRate;
            Scenario1_Rate = lowerRate;
        }

        /// <summary>
        /// 시나리오 2 계산 (실거래가 기반)
        /// </summary>
        public void CalculateScenario2()
        {
            // 적용된 실거래가 평균으로 계산
            decimal totalAmount = 0;
            int count = 0;

            foreach (var transaction in RealTransactions)
            {
                if (transaction.IsApplied && transaction.Amount.HasValue)
                {
                    totalAmount += transaction.Amount.Value;
                    count++;
                }
            }

            if (count > 0 && _property?.AppraisalValue > 0)
            {
                Scenario2_Amount = totalAmount / count;
                Scenario2_Rate = Scenario2_Amount / _property.AppraisalValue;
            }
        }

        /// <summary>
        /// 시나리오 2 (상한) 계산 - 낙찰가율 기반 (피드백 반영)
        /// </summary>
        public void CalculateScenario2FromBidRate()
        {
            if (_property?.AppraisalValue == null || !AppliedBidRate.HasValue)
                return;

            // 상한: 적용 낙찰가율 + 5%
            var upperRate = AppliedBidRate.Value + 0.05m;
            Scenario2_Amount = _property.AppraisalValue.Value * upperRate;
            Scenario2_Rate = upperRate;
        }

        /// <summary>
        /// 배당 요약 테이블 업데이트 (피드백 반영: 시나리오 3 제거)
        /// </summary>
        public void UpdateScenarioSummary()
        {
            if (ScenarioSummaryItems.Count == 0)
            {
                InitializeScenarioSummary();
                return;
            }

            // 각 항목 업데이트 (피드백 반영: 시나리오 3 제거)
            foreach (var item in ScenarioSummaryItems)
            {
                switch (item.Label)
                {
                    case "경매비용":
                        item.Scenario1Value = CalculateAuctionCost(Scenario1_Amount);
                        item.Scenario2Value = CalculateAuctionCost(Scenario2_Amount);
                        break;
                    case "배당회수":
                        item.Scenario1Value = CalculateDistribution(Scenario1_Amount);
                        item.Scenario2Value = CalculateDistribution(Scenario2_Amount);
                        break;
                    case "현금흐름":
                        item.Scenario1Value = Scenario1_Amount;
                        item.Scenario2Value = Scenario2_Amount;
                        break;
                }
                item.TotalValue = (item.Scenario1Value ?? 0) + 
                                  (item.Scenario2Value ?? 0);
            }
        }

        #endregion

        #region 속성 변경 핸들러

        partial void OnAppliedBidRateChanged(decimal? value)
        {
            OnPropertyChanged(nameof(AppliedBidRatePercent));
            CalculateScenario1();
            CalculateScenario2FromBidRate(); // 피드백 반영: 시나리오 2도 재계산
            UpdateScenarioSummary();
            IsDirty = true;
        }

        partial void OnScenario1_AmountChanged(decimal? value)
        {
            UpdateScenarioSummary(); // E-002
            IsDirty = true;
        }

        partial void OnScenario1_ReasonChanged(string? value)
        {
            IsDirty = true;
        }

        partial void OnScenario2_AmountChanged(decimal? value)
        {
            UpdateScenarioSummary(); // E-002
            IsDirty = true;
        }

        partial void OnScenario2_ReasonChanged(string? value)
        {
            IsDirty = true;
        }

        #endregion

        #region E-005: 거리 계산

        /// <summary>
        /// Haversine 공식으로 두 좌표 간 거리 계산 (km)
        /// </summary>
        public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // 지구 반지름 (km)
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;

        #endregion

        #region 인터림 데이터 연동

        // === 인터림 데이터 (차주별 합산) ===
        [ObservableProperty]
        private decimal _interimPrincipalRecovery;

        [ObservableProperty]
        private decimal _interimInterestRecovery;

        [ObservableProperty]
        private decimal _interimAuctionCost;

        [ObservableProperty]
        private decimal _interimTotalRecovery;

        [ObservableProperty]
        private decimal _interimTotalAdvance;

        [ObservableProperty]
        private decimal _interimNetAmount;

        [ObservableProperty]
        private bool _isFirstPropertyInBorrower;

        /// <summary>
        /// 인터림 데이터 설정 (외부에서 호출)
        /// </summary>
        public void SetInterimData(Services.InterimRecoveryData? interimData)
        {

            if (interimData == null)
            {
                InterimPrincipalRecovery = 0;
                InterimInterestRecovery = 0;
                InterimAuctionCost = 0;
                InterimTotalRecovery = 0;
                InterimTotalAdvance = 0;
                InterimNetAmount = 0;
                return;
            }

            InterimPrincipalRecovery = interimData.PrincipalRecovery;
            InterimInterestRecovery = interimData.InterestRecovery;
            InterimAuctionCost = interimData.AuctionCost;
            InterimTotalRecovery = interimData.TotalRecovery;
            InterimTotalAdvance = interimData.AuctionCost + interimData.Subrogation;
            InterimNetAmount = interimData.TotalRecovery - InterimTotalAdvance;

            // 회수 전략 요약에 인터림 데이터 반영
            UpdateRecoveryStrategyWithInterim(interimData);
        }

        /// <summary>
        /// 회수 전략 요약에 인터림 데이터 반영
        /// </summary>
        private void UpdateRecoveryStrategyWithInterim(Services.InterimRecoveryData interimData)
        {
            // 시나리오 1 회수 전략 업데이트
            if (Scenario1_Amount.HasValue)
            {
                var auctionCost1 = CalculateAuctionCost(Scenario1_Amount);
                var distribution1 = CalculateDistribution(Scenario1_Amount);
                
                Recovery1_AuctionCost = (auctionCost1 ?? 0) + interimData.AuctionCost;
                Recovery1_CollateralAmount = (distribution1 ?? 0) + interimData.TotalOffset;
                
                if (_property?.Opb > 0)
                {
                    Recovery1_Ratio = Recovery1_CollateralAmount / _property.Opb * 100;
                }
            }

            // 시나리오 2 회수 전략 업데이트
            if (Scenario2_Amount.HasValue)
            {
                var auctionCost2 = CalculateAuctionCost(Scenario2_Amount);
                var distribution2 = CalculateDistribution(Scenario2_Amount);
                
                Recovery2_AuctionCost = (auctionCost2 ?? 0) + interimData.AuctionCost;
                Recovery2_CollateralAmount = (distribution2 ?? 0) + interimData.TotalOffset;
                
                if (_property?.Opb > 0)
                {
                    Recovery2_Ratio = Recovery2_CollateralAmount / _property.Opb * 100;
                }
            }

            // 시나리오 요약 업데이트 (상계회수 항목 추가)
            UpdateScenarioSummaryWithInterim(interimData);
        }

        /// <summary>
        /// 시나리오 요약에 인터림 상계회수 반영
        /// </summary>
        private void UpdateScenarioSummaryWithInterim(Services.InterimRecoveryData interimData)
        {
            // 상계회수 항목 찾기 또는 추가
            var offsetItem = ScenarioSummaryItems.FirstOrDefault(i => i.Label == "상계회수");
            if (offsetItem == null)
            {
                offsetItem = new ScenarioSummaryItem { Label = "상계회수" };
                
                // 배당회수 다음에 삽입
                var distributionIndex = ScenarioSummaryItems.ToList().FindIndex(i => i.Label == "배당회수");
                if (distributionIndex >= 0 && distributionIndex < ScenarioSummaryItems.Count - 1)
                {
                    ScenarioSummaryItems.Insert(distributionIndex + 1, offsetItem);
                }
                else
                {
                    ScenarioSummaryItems.Add(offsetItem);
                }
            }

            // 상계회수 값 설정 (인터림 파일에서)
            offsetItem.Scenario1Value = interimData.TotalOffset;
            offsetItem.Scenario2Value = interimData.TotalOffset;
            offsetItem.TotalValue = interimData.TotalOffset * 2;

            // 현금흐름 업데이트 (배당회수 + 상계회수)
            var cashFlowItem = ScenarioSummaryItems.FirstOrDefault(i => i.Label == "현금흐름");
            if (cashFlowItem != null)
            {
                var distribution1 = CalculateDistribution(Scenario1_Amount) ?? 0;
                var distribution2 = CalculateDistribution(Scenario2_Amount) ?? 0;
                
                cashFlowItem.Scenario1Value = distribution1 + interimData.TotalOffset;
                cashFlowItem.Scenario2Value = distribution2 + interimData.TotalOffset;
                cashFlowItem.TotalValue = cashFlowItem.Scenario1Value + cashFlowItem.Scenario2Value;
            }
        }

        /// <summary>
        /// 인터림 표시 문자열
        /// </summary>
        public string InterimSummaryDisplay => IsFirstPropertyInBorrower && InterimNetAmount != 0
            ? $"인터림: 회수 {InterimTotalRecovery:N0}원 / 지출 {InterimTotalAdvance:N0}원 = 순 {InterimNetAmount:N0}원"
            : "";

        #endregion
    }
}

