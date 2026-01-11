using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        public decimal? Amount { get; set; }
        public string? Floor { get; set; }
        public string? IsRegistered { get; set; }
        
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
    /// E-002: 시나리오별 배당 요약 항목
    /// </summary>
    public class ScenarioSummaryItem : ObservableObject
    {
        /// <summary>항목명 (경매비용, 배당회수, 상계회수, 현금흐름)</summary>
        public string Label { get; set; } = "";
        
        /// <summary>시나리오 1 금액</summary>
        public decimal? Scenario1Value { get; set; }
        
        /// <summary>시나리오 2 금액</summary>
        public decimal? Scenario2Value { get; set; }
        
        /// <summary>시나리오 3 금액</summary>
        public decimal? Scenario3Value { get; set; }
        
        /// <summary>합계</summary>
        public decimal? TotalValue { get; set; }
        
        // 포맷된 표시 값
        public string Scenario1Display => Scenario1Value.HasValue ? $"{Scenario1Value:N0}" : "-";
        public string Scenario2Display => Scenario2Value.HasValue ? $"{Scenario2Value:N0}" : "-";
        public string Scenario3Display => Scenario3Value.HasValue ? $"{Scenario3Value:N0}" : "-";
        public string TotalDisplay => TotalValue.HasValue ? $"{TotalValue:N0}" : "-";
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
        private Evaluation? _evaluation;

        // Supabase 설정 (App.xaml.cs에서 설정)
        public string? SupabaseUrl { get; set; }
        public string? SupabaseKey { get; set; }

        public EvaluationTabViewModel(EvaluationRepository evaluationRepository)
        {
            _evaluationRepository = evaluationRepository ?? throw new ArgumentNullException(nameof(evaluationRepository));
            _recommendService = new RecommendService();
            
            // 초기 데이터 설정
            InitializeCaseItems();
        }

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
        private bool _isApartmentType = true;

        [ObservableProperty]
        private bool _isMultiFamilyType;

        [ObservableProperty]
        private bool _isFactoryType;

        [ObservableProperty]
        private bool _isCommercialType;

        [ObservableProperty]
        private bool _isHouseLandType;

        // === 사례평가 테이블 ===
        [ObservableProperty]
        private ObservableCollection<CaseRowItem> _caseItems = new();

        // === 실거래가 ===
        [ObservableProperty]
        private ObservableCollection<RealTransactionItem> _realTransactions = new();

        // === 낙찰통계 (피드백 반영: 시/군구/동 3×3 매트릭스) ===
        [ObservableProperty]
        private string? _regionName1 = "서울특별시";

        [ObservableProperty]
        private string? _regionName2 = "강남구";

        [ObservableProperty]
        private string? _regionName3 = "대치동";

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
        private decimal? _appliedBidRate = 0.70m;

        [ObservableProperty]
        private string? _appliedBidRateDescription = "3개월 평균 낙찰가율";

        // 변경사항 추적 (피드백 반영: 저장 확인용)
        [ObservableProperty]
        private bool _isDirty;

        // === 평가결과 시나리오 1 ===
        [ObservableProperty]
        private decimal? _scenario1_Amount;

        [ObservableProperty]
        private decimal? _scenario1_Rate;

        [ObservableProperty]
        private string? _scenario1_Reason = "낙찰사례 적용";

        // === 평가결과 시나리오 2 ===
        [ObservableProperty]
        private decimal? _scenario2_Amount;

        [ObservableProperty]
        private decimal? _scenario2_Rate;

        [ObservableProperty]
        private string? _scenario2_Reason = "실거래가 적용";

        // === E-002: 평가결과 시나리오 3 ===
        [ObservableProperty]
        private decimal? _scenario3_Amount;

        [ObservableProperty]
        private decimal? _scenario3_Rate;

        [ObservableProperty]
        private string? _scenario3_Reason = "보수적 평가";

        // === E-002: 차주별 배당 요약 ===
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
            
            // 지역명 설정
            SetRegionFromAddress(property.AddressFull);
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

        private void InitializeCaseItems()
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
                new CaseRowItem { Label = "낙찰가액" },
                new CaseRowItem { Label = "낙찰가율" },
                new CaseRowItem { Label = "낙찰회차" },
                new CaseRowItem { Label = "평당낙찰가(토지)" },
                new CaseRowItem { Label = "평당낙찰가(건물)" },
                new CaseRowItem { Label = "용적율" },
                new CaseRowItem { Label = "2등 입찰가" },
                new CaseRowItem { Label = "사례 비고 사항" }
            };
        }

        private void InitializeNewEvaluation()
        {
            // 물건 정보에서 기본값 설정
            if (_property != null)
            {
                // 감정가 기반 시나리오 계산
                if (_property.AppraisalValue.HasValue && AppliedBidRate.HasValue)
                {
                    Scenario1_Amount = _property.AppraisalValue.Value * AppliedBidRate.Value;
                    Scenario1_Rate = AppliedBidRate;
                    
                    Scenario2_Amount = _property.AppraisalValue.Value * (AppliedBidRate.Value + 0.05m);
                    Scenario2_Rate = AppliedBidRate + 0.05m;
                    
                    // E-002: 시나리오 3 초기화 (보수적: -5%)
                    Scenario3_Amount = _property.AppraisalValue.Value * (AppliedBidRate.Value - 0.05m);
                    Scenario3_Rate = AppliedBidRate - 0.05m;
                }
            }
            
            // E-002: 배당 요약 테이블 초기화
            InitializeScenarioSummary();
        }

        /// <summary>
        /// E-002: 시나리오별 배당 요약 테이블 초기화
        /// </summary>
        private void InitializeScenarioSummary()
        {
            ScenarioSummaryItems = new ObservableCollection<ScenarioSummaryItem>
            {
                new ScenarioSummaryItem 
                { 
                    Label = "경매비용",
                    Scenario1Value = CalculateAuctionCost(Scenario1_Amount),
                    Scenario2Value = CalculateAuctionCost(Scenario2_Amount),
                    Scenario3Value = CalculateAuctionCost(Scenario3_Amount)
                },
                new ScenarioSummaryItem 
                { 
                    Label = "배당회수",
                    Scenario1Value = CalculateDistribution(Scenario1_Amount),
                    Scenario2Value = CalculateDistribution(Scenario2_Amount),
                    Scenario3Value = CalculateDistribution(Scenario3_Amount)
                },
                new ScenarioSummaryItem 
                { 
                    Label = "상계회수",
                    Scenario1Value = 0, // 인터림 파일에서 가져옴 (추후 구현)
                    Scenario2Value = 0,
                    Scenario3Value = 0
                },
                new ScenarioSummaryItem 
                { 
                    Label = "현금흐름",
                    Scenario1Value = Scenario1_Amount,
                    Scenario2Value = Scenario2_Amount,
                    Scenario3Value = Scenario3_Amount
                }
            };
            
            // 합계 계산
            foreach (var item in ScenarioSummaryItems)
            {
                item.TotalValue = (item.Scenario1Value ?? 0) + 
                                  (item.Scenario2Value ?? 0) + 
                                  (item.Scenario3Value ?? 0);
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

        private void LoadFromEvaluation(Evaluation evaluation)
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

        private void AutoSelectEvaluationType(string? propertyType)
        {
            if (string.IsNullOrWhiteSpace(propertyType))
            {
                IsApartmentType = true;
                return;
            }

            var type = propertyType.ToLower();
            
            IsApartmentType = type.Contains("아파트") || type.Contains("오피스텔");
            IsMultiFamilyType = type.Contains("연립") || type.Contains("다세대") || type.Contains("빌라");
            IsFactoryType = type.Contains("공장") || type.Contains("창고");
            IsCommercialType = type.Contains("상가") || type.Contains("아파트형공장");
            IsHouseLandType = type.Contains("주택") || type.Contains("토지") || type.Contains("근린");
            
            // 기본값
            if (!IsApartmentType && !IsMultiFamilyType && !IsFactoryType && !IsCommercialType && !IsHouseLandType)
            {
                IsApartmentType = true;
            }
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
                var evaluation = _evaluation ?? new Evaluation();
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
                                
                                RecommendedCases.Add(new RecommendCaseItem
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

                    RecommendStatusMessage = $"추천 결과: {RecommendedCases.Count}건";
                    if (RecommendedCases.Count == 0)
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
        /// 시나리오 1 계산 (낙찰사례 기반)
        /// </summary>
        public void CalculateScenario1()
        {
            if (_property?.AppraisalValue == null || !AppliedBidRate.HasValue)
                return;

            Scenario1_Amount = _property.AppraisalValue.Value * AppliedBidRate.Value;
            Scenario1_Rate = AppliedBidRate;
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
        /// E-002: 시나리오 3 계산 (보수적 평가: 시나리오1 - 5%)
        /// </summary>
        public void CalculateScenario3()
        {
            if (_property?.AppraisalValue == null || !AppliedBidRate.HasValue)
                return;

            var conservativeRate = AppliedBidRate.Value - 0.05m;
            if (conservativeRate < 0.3m) conservativeRate = 0.3m; // 최소 30%

            Scenario3_Amount = _property.AppraisalValue.Value * conservativeRate;
            Scenario3_Rate = conservativeRate;
        }

        /// <summary>
        /// E-002: 배당 요약 테이블 업데이트
        /// </summary>
        public void UpdateScenarioSummary()
        {
            if (ScenarioSummaryItems.Count == 0)
            {
                InitializeScenarioSummary();
                return;
            }

            // 각 항목 업데이트
            foreach (var item in ScenarioSummaryItems)
            {
                switch (item.Label)
                {
                    case "경매비용":
                        item.Scenario1Value = CalculateAuctionCost(Scenario1_Amount);
                        item.Scenario2Value = CalculateAuctionCost(Scenario2_Amount);
                        item.Scenario3Value = CalculateAuctionCost(Scenario3_Amount);
                        break;
                    case "배당회수":
                        item.Scenario1Value = CalculateDistribution(Scenario1_Amount);
                        item.Scenario2Value = CalculateDistribution(Scenario2_Amount);
                        item.Scenario3Value = CalculateDistribution(Scenario3_Amount);
                        break;
                    case "현금흐름":
                        item.Scenario1Value = Scenario1_Amount;
                        item.Scenario2Value = Scenario2_Amount;
                        item.Scenario3Value = Scenario3_Amount;
                        break;
                }
                item.TotalValue = (item.Scenario1Value ?? 0) + 
                                  (item.Scenario2Value ?? 0) + 
                                  (item.Scenario3Value ?? 0);
            }
        }

        #endregion

        #region 속성 변경 핸들러

        partial void OnAppliedBidRateChanged(decimal? value)
        {
            CalculateScenario1();
            CalculateScenario3(); // E-002: 시나리오 3도 재계산
            UpdateScenarioSummary(); // E-002: 배당 요약 업데이트
            IsDirty = true;
        }

        partial void OnIsApartmentTypeChanged(bool value)
        {
            if (value)
            {
                AppliedBidRateDescription = $"{RegionName3 ?? RegionName2} 3개월 평균 낙찰가율";
            }
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

        // E-002: 시나리오 3 변경 핸들러
        partial void OnScenario3_AmountChanged(decimal? value)
        {
            UpdateScenarioSummary();
            IsDirty = true;
        }

        partial void OnScenario3_ReasonChanged(string? value)
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
    }
}

