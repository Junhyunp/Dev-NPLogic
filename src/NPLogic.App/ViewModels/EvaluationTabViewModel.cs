using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 사례평가 테이블 행 아이템
    /// </summary>
    public class CaseRowItem : ObservableObject
    {
        public string Label { get; set; } = "";
        public string? BaseValue { get; set; }
        public string? Case1Value { get; set; }
        public string? Case2Value { get; set; }
        public string? Case3Value { get; set; }
        public string? Case4Value { get; set; }
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

        // === 낙찰통계 ===
        [ObservableProperty]
        private string? _regionName1 = "부산";

        [ObservableProperty]
        private string? _regionName2 = "남구";

        [ObservableProperty]
        private decimal? _stats1Year_Rate1;

        [ObservableProperty]
        private int? _stats1Year_Count1;

        [ObservableProperty]
        private decimal? _stats1Year_Rate2;

        [ObservableProperty]
        private int? _stats1Year_Count2;

        [ObservableProperty]
        private decimal? _stats6Month_Rate1;

        [ObservableProperty]
        private int? _stats6Month_Count1;

        [ObservableProperty]
        private decimal? _stats6Month_Rate2;

        [ObservableProperty]
        private int? _stats6Month_Count2;

        [ObservableProperty]
        private decimal? _stats3Month_Rate1;

        [ObservableProperty]
        private int? _stats3Month_Count1;

        [ObservableProperty]
        private decimal? _stats3Month_Rate2;

        [ObservableProperty]
        private int? _stats3Month_Count2;

        [ObservableProperty]
        private decimal? _appliedBidRate = 0.70m;

        [ObservableProperty]
        private string? _appliedBidRateDescription = "대연동 3개월 평균 낙찰가율";

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
                }
            }
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
            SuccessMessage = "사례지도 기능은 추후 구현 예정입니다.";
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
                    
                    // 결과 변환
                    if (result.RuleResults != null)
                    {
                        foreach (var ruleResult in result.RuleResults)
                        {
                            foreach (var caseItem in ruleResult.Value)
                            {
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
                                    RuleName = caseItem.RuleName
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
                    
                    SuccessMessage = $"사례 {SelectedRecommendCase.CaseNo}의 낙찰가율이 적용되었습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사례 적용 실패: {ex.Message}";
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

                // TODO: 실제 API 연동
                await Task.Delay(500); // 시뮬레이션

                // 더미 데이터
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

                SuccessMessage = "실거래가가 조회되었습니다.";
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

        #endregion

        #region 속성 변경 핸들러

        partial void OnAppliedBidRateChanged(decimal? value)
        {
            CalculateScenario1();
        }

        partial void OnIsApartmentTypeChanged(bool value)
        {
            if (value)
            {
                AppliedBidRateDescription = $"{RegionName2} 3개월 평균 낙찰가율";
            }
        }

        #endregion
    }
}

