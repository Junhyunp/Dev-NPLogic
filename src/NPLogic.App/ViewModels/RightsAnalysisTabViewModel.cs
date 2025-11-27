using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Core.Services;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 권리분석(선순위) 탭 ViewModel
    /// </summary>
    public partial class RightsAnalysisTabViewModel : ObservableObject
    {
        private readonly RightAnalysisRepository _repository;
        private readonly RightAnalysisRuleEngine _ruleEngine;
        private Guid? _propertyId;
        private Property? _property;

        #region Observable Properties

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        [ObservableProperty]
        private RightAnalysis _analysis = new();

        /// <summary>
        /// 선순위 분석 그리드 아이템
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SeniorRightItem> _seniorRightsItems = new();

        /// <summary>
        /// DD 금액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _totalDdAmount;

        /// <summary>
        /// 반영금액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _totalReflectedAmount;

        #endregion

        public RightsAnalysisTabViewModel(RightAnalysisRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ruleEngine = new RightAnalysisRuleEngine();
            InitializeSeniorRightsItems();
        }

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
        }

        /// <summary>
        /// 선순위 분석 그리드 초기화
        /// </summary>
        private void InitializeSeniorRightsItems()
        {
            SeniorRightsItems = new ObservableCollection<SeniorRightItem>
            {
                new SeniorRightItem { Category = "선순위 근저당권", Field = "SeniorMortgage" },
                new SeniorRightItem { Category = "유치권 신고금액", Field = "Lien" },
                new SeniorRightItem { Category = "선순위 소액보증금", Field = "SmallDeposit" },
                new SeniorRightItem { Category = "선순위 임차보증금", Field = "LeaseDeposit" },
                new SeniorRightItem { Category = "선순위 임금채권", Field = "WageClaim" },
                new SeniorRightItem { Category = "당해세", Field = "CurrentTax" },
                new SeniorRightItem { Category = "선순위 조세채권", Field = "SeniorTax" }
            };
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (_propertyId == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var analysis = await _repository.GetByPropertyIdAsync(_propertyId.Value);
                
                if (analysis != null)
                {
                    Analysis = analysis;
                }
                else
                {
                    // 신규 생성
                    Analysis = new RightAnalysis
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = _propertyId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    // 물건 정보에서 기본값 설정
                    if (_property != null)
                    {
                        Analysis.InitialAppraisalValue = _property.AppraisalValue;
                        Analysis.ExpectedWinningBid = _property.MinimumBid ?? _property.AppraisalValue;
                    }
                }

                UpdateSeniorRightsItems();
                CalculateTotals();
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
        /// 선순위 그리드 아이템 업데이트
        /// </summary>
        private void UpdateSeniorRightsItems()
        {
            foreach (var item in SeniorRightsItems)
            {
                switch (item.Field)
                {
                    case "SeniorMortgage":
                        item.DdAmount = Analysis.SeniorMortgageDd;
                        item.ReflectedAmount = Analysis.SeniorMortgageReflected;
                        item.Reason = Analysis.SeniorMortgageReason;
                        break;
                    case "Lien":
                        item.DdAmount = Analysis.LienDd;
                        item.ReflectedAmount = Analysis.LienReflected;
                        item.Reason = Analysis.LienReason;
                        break;
                    case "SmallDeposit":
                        item.DdAmount = Analysis.SmallDepositDd;
                        item.ReflectedAmount = Analysis.SmallDepositReflected;
                        item.Reason = Analysis.SmallDepositReason;
                        break;
                    case "LeaseDeposit":
                        item.DdAmount = Analysis.LeaseDepositDd;
                        item.ReflectedAmount = Analysis.LeaseDepositReflected;
                        item.Reason = Analysis.LeaseDepositReason;
                        break;
                    case "WageClaim":
                        item.DdAmount = Analysis.WageClaimDd;
                        item.ReflectedAmount = Analysis.WageClaimReflected;
                        item.Reason = Analysis.WageClaimReason;
                        break;
                    case "CurrentTax":
                        item.DdAmount = Analysis.CurrentTaxDd;
                        item.ReflectedAmount = Analysis.CurrentTaxReflected;
                        item.Reason = Analysis.CurrentTaxReason;
                        break;
                    case "SeniorTax":
                        item.DdAmount = Analysis.SeniorTaxDd;
                        item.ReflectedAmount = Analysis.SeniorTaxReflected;
                        item.Reason = Analysis.SeniorTaxReason;
                        break;
                }
            }
        }

        /// <summary>
        /// Analysis 모델에 그리드 값 반영
        /// </summary>
        private void UpdateAnalysisFromItems()
        {
            foreach (var item in SeniorRightsItems)
            {
                switch (item.Field)
                {
                    case "SeniorMortgage":
                        Analysis.SeniorMortgageDd = item.DdAmount;
                        Analysis.SeniorMortgageReflected = item.ReflectedAmount;
                        Analysis.SeniorMortgageReason = item.Reason;
                        break;
                    case "Lien":
                        Analysis.LienDd = item.DdAmount;
                        Analysis.LienReflected = item.ReflectedAmount;
                        Analysis.LienReason = item.Reason;
                        break;
                    case "SmallDeposit":
                        Analysis.SmallDepositDd = item.DdAmount;
                        Analysis.SmallDepositReflected = item.ReflectedAmount;
                        Analysis.SmallDepositReason = item.Reason;
                        break;
                    case "LeaseDeposit":
                        Analysis.LeaseDepositDd = item.DdAmount;
                        Analysis.LeaseDepositReflected = item.ReflectedAmount;
                        Analysis.LeaseDepositReason = item.Reason;
                        break;
                    case "WageClaim":
                        Analysis.WageClaimDd = item.DdAmount;
                        Analysis.WageClaimReflected = item.ReflectedAmount;
                        Analysis.WageClaimReason = item.Reason;
                        break;
                    case "CurrentTax":
                        Analysis.CurrentTaxDd = item.DdAmount;
                        Analysis.CurrentTaxReflected = item.ReflectedAmount;
                        Analysis.CurrentTaxReason = item.Reason;
                        break;
                    case "SeniorTax":
                        Analysis.SeniorTaxDd = item.DdAmount;
                        Analysis.SeniorTaxReflected = item.ReflectedAmount;
                        Analysis.SeniorTaxReason = item.Reason;
                        break;
                }
            }
        }

        /// <summary>
        /// 합계 계산
        /// </summary>
        private void CalculateTotals()
        {
            TotalDdAmount = SeniorRightsItems.Sum(x => x.DdAmount);
            TotalReflectedAmount = SeniorRightsItems.Sum(x => x.ReflectedAmount);
            
            Analysis.SeniorRightsTotal = TotalReflectedAmount;
        }

        #region Commands

        /// <summary>
        /// 자동 계산 Command
        /// </summary>
        [RelayCommand]
        private void AutoCalculate()
        {
            try
            {
                // 룰 엔진으로 판단 로직 실행
                _ruleEngine.ApplyRules(Analysis, _property);
                
                // 그리드 업데이트
                UpdateSeniorRightsItems();
                CalculateTotals();
                
                // 배당 시뮬레이션 계산
                CalculateDistribution();
                
                // 위험도 자동 평가
                EvaluateRisk();
                
                SuccessMessage = "자동 계산이 완료되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"자동 계산 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 배당 시뮬레이션 계산
        /// </summary>
        private void CalculateDistribution()
        {
            // 경매수수료 (낙찰가의 약 1.5% 추정)
            if (Analysis.ExpectedWinningBid.HasValue)
            {
                Analysis.AuctionFees = Math.Round(Analysis.ExpectedWinningBid.Value * 0.015m, 0);
            }
            
            // 배당가능재원 = 예상낙찰가 - 경매수수료
            Analysis.DistributableAmount = Analysis.CalculateDistributableAmount();
            
            // 선순위 공제 후 금액
            Analysis.AmountAfterSenior = Analysis.CalculateAmountAfterSenior();
            
            // Cap 반영 배당액
            if (Analysis.LoanCap.HasValue && Analysis.LoanCap.Value > 0)
            {
                Analysis.CapAppliedDividend = Math.Min(
                    Analysis.AmountAfterSenior ?? 0,
                    Analysis.LoanCap.Value
                );
                
                // 회수율 계산
                Analysis.RecoveryRate = Analysis.CalculateRecoveryRate(Analysis.LoanCap.Value);
            }
            else
            {
                Analysis.CapAppliedDividend = Analysis.AmountAfterSenior;
                Analysis.RecoveryRate = 100;
            }
            
            Analysis.RecoveryAmount = Analysis.CapAppliedDividend;
        }

        /// <summary>
        /// 위험도 자동 평가
        /// </summary>
        private void EvaluateRisk()
        {
            var recoveryRate = Analysis.RecoveryRate ?? 0;
            
            if (recoveryRate >= 80)
            {
                Analysis.RiskLevel = "low";
                Analysis.RiskReason = $"예상 회수율 {recoveryRate:N1}%로 양호한 수준입니다.";
            }
            else if (recoveryRate >= 50)
            {
                Analysis.RiskLevel = "medium";
                Analysis.RiskReason = $"예상 회수율 {recoveryRate:N1}%로 주의가 필요합니다.";
            }
            else
            {
                Analysis.RiskLevel = "high";
                Analysis.RiskReason = $"예상 회수율 {recoveryRate:N1}%로 손실 위험이 높습니다.";
            }
            
            // 추가 위험 요인 체크
            if (TotalReflectedAmount > (Analysis.ExpectedWinningBid ?? 0) * 0.5m)
            {
                Analysis.RiskReason += " 선순위 금액이 낙찰가의 50%를 초과합니다.";
            }
        }

        /// <summary>
        /// 저장 Command
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (_propertyId == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                
                // 그리드 값을 Analysis에 반영
                UpdateAnalysisFromItems();
                CalculateTotals();
                CalculateDistribution();
                
                Analysis.UpdatedAt = DateTime.UtcNow;
                Analysis.AnalyzedAt = DateTime.UtcNow;
                
                // Upsert (생성 또는 수정)
                await _repository.UpsertAsync(Analysis);
                
                SuccessMessage = "권리분석 정보가 저장되었습니다.";
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
        /// 경매사건검색 열기
        /// </summary>
        [RelayCommand]
        private void OpenAuctionSearch()
        {
            try
            {
                // 대법원 경매사건검색 페이지 열기
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.courtauction.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"경매사건검색 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 주택공시가격 조회
        /// </summary>
        [RelayCommand]
        private void OpenHousingPrice()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.realtyprice.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"주택공시가격 조회 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 공시지가 조회
        /// </summary>
        [RelayCommand]
        private void OpenLandPrice()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://kras.go.kr/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"공시지가 조회 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 판단 로직 보기
        /// </summary>
        [RelayCommand]
        private void ShowRuleLogic()
        {
            var appliedCase = Analysis.SmallDepositCase ?? "없음";
            var reason = Analysis.SmallDepositReason ?? "자동 판단 로직이 적용되지 않았습니다.";
            
            MessageBox.Show(
                $"적용된 케이스: {appliedCase}\n\n{reason}",
                "판단 로직",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        #endregion
    }

    /// <summary>
    /// 선순위 분석 그리드 아이템
    /// </summary>
    public partial class SeniorRightItem : ObservableObject
    {
        /// <summary>
        /// 선순위 구분 (표시용)
        /// </summary>
        [ObservableProperty]
        private string _category = string.Empty;

        /// <summary>
        /// 필드명 (매핑용)
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// DD 금액
        /// </summary>
        [ObservableProperty]
        private decimal _ddAmount;

        /// <summary>
        /// 평가자 반영금액
        /// </summary>
        [ObservableProperty]
        private decimal _reflectedAmount;

        /// <summary>
        /// 상세추정 근거
        /// </summary>
        [ObservableProperty]
        private string? _reason;
    }
}

