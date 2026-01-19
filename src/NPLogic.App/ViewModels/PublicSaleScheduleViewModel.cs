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

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 공매일정(Ⅷ) ViewModel - 엑셀 산출화면 기반 전면 재구성
    /// </summary>
    public partial class PublicSaleScheduleViewModel : ObservableObject
    {
        private readonly SupabaseService? _supabaseService;
        private readonly PublicSaleScheduleRepository? _scheduleRepository;
        private readonly PropertyRepository? _propertyRepository;
        private readonly EvaluationRepository? _evaluationRepository;
        private readonly RightAnalysisRepository? _rightAnalysisRepository;

        // ========== 기본 정보 ==========
        
        [ObservableProperty]
        private Guid? _propertyId;
        
        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private string? _errorMessage;

        // ========== 평가결과 ==========
        
        [ObservableProperty]
        private decimal _scenario1WinningBid;
        
        [ObservableProperty]
        private decimal _scenario2WinningBid;
        
        [ObservableProperty]
        private decimal _appraisalValue;
        
        [ObservableProperty]
        private decimal _landArea;
        
        [ObservableProperty]
        private decimal _buildingArea;
        
        [ObservableProperty]
        private string? _scenario1EvalReason = "낙찰사례 적용";
        
        [ObservableProperty]
        private string? _scenario2EvalReason = "실거래가 적용";

        public decimal Scenario1BidRate => AppraisalValue > 0 ? (Scenario1WinningBid / AppraisalValue) * 100 : 0;
        public decimal Scenario2BidRate => AppraisalValue > 0 ? (Scenario2WinningBid / AppraisalValue) * 100 : 0;
        public decimal Scenario1LandPricePerPyeong => LandArea > 0 ? Scenario1WinningBid / LandArea : 0;
        public decimal Scenario2LandPricePerPyeong => LandArea > 0 ? Scenario2WinningBid / LandArea : 0;
        public decimal Scenario1BuildingPricePerPyeong => BuildingArea > 0 ? Scenario1WinningBid / BuildingArea : 0;
        public decimal Scenario2BuildingPricePerPyeong => BuildingArea > 0 ? Scenario2WinningBid / BuildingArea : 0;

        // ========== 구분별 감정평가금액 ==========
        
        [ObservableProperty]
        private decimal _landAppraisalValue;
        
        [ObservableProperty]
        private decimal _buildingAppraisalValue;
        
        [ObservableProperty]
        private decimal _machineryAppraisalValue;
        
        [ObservableProperty]
        private decimal _otherBankSeniorValue;

        public decimal TotalAppraisalValue => LandAppraisalValue + BuildingAppraisalValue + MachineryAppraisalValue + OtherBankSeniorValue;

        // ========== 공매일정 상세 (1안/2안) ==========
        
        [ObservableProperty]
        private DateTime? _scenario1StartDate;
        
        [ObservableProperty]
        private DateTime? _scenario2StartDate;
        
        [ObservableProperty]
        private decimal _scenario1BeneficiaryChangeCost;
        
        [ObservableProperty]
        private decimal _scenario2BeneficiaryChangeCost;

        public decimal Scenario1EstimatedWinningBid => Scenario1WinningBid;
        public decimal Scenario2EstimatedWinningBid => Scenario2WinningBid;

        [ObservableProperty]
        private decimal _scenario1SeniorDeduction;
        
        [ObservableProperty]
        private decimal _scenario2SeniorDeduction;

        public decimal Scenario1SaleCost => TotalSaleCost1;
        public decimal Scenario2SaleCost => TotalSaleCost2;

        [ObservableProperty]
        private decimal _scenario1DisposalFee;
        
        [ObservableProperty]
        private decimal _scenario2DisposalFee;

        public decimal Scenario1DistributableBefore => 
            Scenario1EstimatedWinningBid - Scenario1SeniorDeduction - Scenario1SaleCost - Scenario1DisposalFee;
        public decimal Scenario2DistributableBefore => 
            Scenario2EstimatedWinningBid - Scenario2SeniorDeduction - Scenario2SaleCost - Scenario2DisposalFee;

        [ObservableProperty]
        private decimal _scenario1CreditorDistribution;
        
        [ObservableProperty]
        private decimal _scenario2CreditorDistribution;

        public decimal Scenario1DistributableAfter => Scenario1DistributableBefore - Scenario1CreditorDistribution;
        public decimal Scenario2DistributableAfter => Scenario2DistributableBefore - Scenario2CreditorDistribution;

        [ObservableProperty]
        private decimal _scenario1LoanCap;
        
        [ObservableProperty]
        private decimal _scenario2LoanCap;
        
        [ObservableProperty]
        private decimal _scenario1LoanCap2;
        
        [ObservableProperty]
        private decimal _scenario2LoanCap2;
        
        [ObservableProperty]
        private decimal _scenario1MortgageCap;
        
        [ObservableProperty]
        private decimal _scenario2MortgageCap;

        public decimal Scenario1CapAppliedDividend
        {
            get
            {
                var distributable = Scenario1DistributableAfter;
                if (distributable <= 0) return 0;
                
                decimal min = distributable;
                if (Scenario1LoanCap > 0 && Scenario1LoanCap < min) min = Scenario1LoanCap;
                if (Scenario1LoanCap2 > 0 && Scenario1LoanCap2 < min) min = Scenario1LoanCap2;
                if (Scenario1MortgageCap > 0 && Scenario1MortgageCap < min) min = Scenario1MortgageCap;
                return min;
            }
        }
        
        public decimal Scenario2CapAppliedDividend
        {
            get
            {
                var distributable = Scenario2DistributableAfter;
                if (distributable <= 0) return 0;
                
                decimal min = distributable;
                if (Scenario2LoanCap > 0 && Scenario2LoanCap < min) min = Scenario2LoanCap;
                if (Scenario2LoanCap2 > 0 && Scenario2LoanCap2 < min) min = Scenario2LoanCap2;
                if (Scenario2MortgageCap > 0 && Scenario2MortgageCap < min) min = Scenario2MortgageCap;
                return min;
            }
        }

        public decimal Scenario1RecoverableAmount => Scenario1CapAppliedDividend;
        public decimal Scenario2RecoverableAmount => Scenario2CapAppliedDividend;

        // ========== 공매비용 ==========
        
        [ObservableProperty]
        private decimal _onbidFee1;
        
        [ObservableProperty]
        private decimal _onbidFee2;
        
        [ObservableProperty]
        private decimal _appraisalFee;

        public decimal TotalSaleCost1 => OnbidFee1 + AppraisalFee;
        public decimal TotalSaleCost2 => OnbidFee2 + AppraisalFee;

        // ========== 환가처분보수 ==========
        
        public decimal DisposalFeeAmount1 => CalculateDisposalFee(Scenario1WinningBid);
        public decimal DisposalFeeAmount2 => CalculateDisposalFee(Scenario2WinningBid);

        // ========== 타채권자 배분 ==========
        
        [ObservableProperty]
        private string? _creditor1Name;
        
        [ObservableProperty]
        private string? _creditor1Basis;
        
        [ObservableProperty]
        private decimal _creditor1Ratio;
        
        public decimal Creditor1Amount1 => Scenario1WinningBid * Creditor1Ratio / 100;
        public decimal Creditor1Amount2 => Scenario2WinningBid * Creditor1Ratio / 100;
        
        [ObservableProperty]
        private string? _creditor2Name;
        
        [ObservableProperty]
        private string? _creditor2Basis;
        
        [ObservableProperty]
        private decimal _creditor2Ratio;
        
        public decimal Creditor2Amount1 => Scenario1WinningBid * Creditor2Ratio / 100;
        public decimal Creditor2Amount2 => Scenario2WinningBid * Creditor2Ratio / 100;

        public decimal TotalCreditorRatio => Creditor1Ratio + Creditor2Ratio;
        public decimal TotalCreditorAmount1 => Creditor1Amount1 + Creditor2Amount1;
        public decimal TotalCreditorAmount2 => Creditor1Amount2 + Creditor2Amount2;

        // ========== Lead time 설정 ==========
        
        [ObservableProperty]
        private int _leadTimeDays = 11;
        
        [ObservableProperty]
        private decimal _discountRate = 0.10m;
        
        [ObservableProperty]
        private decimal _initialAppraisalValue;
        
        [ObservableProperty]
        private DateTime? _appraisalDate;
        
        [ObservableProperty]
        private ObservableCollection<LeadTimeScheduleItem> _leadTimeSchedules = new();

        public PublicSaleScheduleViewModel()
        {
        }

        public PublicSaleScheduleViewModel(
            SupabaseService? supabaseService,
            PublicSaleScheduleRepository? scheduleRepository,
            PropertyRepository? propertyRepository = null,
            EvaluationRepository? evaluationRepository = null,
            RightAnalysisRepository? rightAnalysisRepository = null)
        {
            _supabaseService = supabaseService;
            _scheduleRepository = scheduleRepository;
            _propertyRepository = propertyRepository;
            _evaluationRepository = evaluationRepository;
            _rightAnalysisRepository = rightAnalysisRepository;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                
                GenerateLeadTimeSchedules();
                
                if (PropertyId.HasValue)
                {
                    await LoadPropertyDataAsync();
                }
                
                CalculateSaleCosts();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
                Debug.WriteLine($"PublicSaleScheduleViewModel 초기화 실패: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPropertyDataAsync()
        {
            if (!PropertyId.HasValue) return;
            
            try
            {
                if (_propertyRepository != null)
                {
                    var property = await _propertyRepository.GetByIdAsync(PropertyId.Value);
                    if (property != null)
                    {
                        AppraisalValue = property.AppraisalValue ?? 0;
                        InitialAppraisalValue = AppraisalValue;
                    }
                }
                
                if (_evaluationRepository != null)
                {
                    var eval = await _evaluationRepository.GetByPropertyIdAsync(PropertyId.Value);
                    if (eval?.EvaluationDetails != null)
                    {
                        var details = eval.EvaluationDetails;
                        if (details.Scenario1?.EvaluatedValue.HasValue == true)
                            Scenario1WinningBid = details.Scenario1.EvaluatedValue.Value;
                        if (details.Scenario2?.EvaluatedValue.HasValue == true)
                            Scenario2WinningBid = details.Scenario2.EvaluatedValue.Value;
                        if (details.AppraisalValue.HasValue && AppraisalValue == 0)
                        {
                            AppraisalValue = details.AppraisalValue.Value;
                            InitialAppraisalValue = AppraisalValue;
                        }
                        if (details.AppraisalDate.HasValue)
                            AppraisalDate = details.AppraisalDate.Value;
                    }
                }
                
                if (_rightAnalysisRepository != null)
                {
                    var rightAnalysis = await _rightAnalysisRepository.GetByPropertyIdAsync(PropertyId.Value);
                    if (rightAnalysis != null)
                    {
                        Scenario1SeniorDeduction = rightAnalysis.SeniorRightsTotal ?? 0;
                        Scenario2SeniorDeduction = rightAnalysis.SeniorRightsTotal ?? 0;
                        Scenario1LoanCap = rightAnalysis.LoanCap ?? 0;
                        Scenario2LoanCap = rightAnalysis.LoanCap ?? 0;
                    }
                }
                
                if (_scheduleRepository != null)
                {
                    var schedules = await _scheduleRepository.GetByPropertyIdAsync(PropertyId.Value);
                    if (schedules.Count > 0)
                    {
                        LoadFromSchedule(schedules.First());
                    }
                }
                
                NotifyCalculatedPropertiesChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"물건 데이터 로드 실패: {ex.Message}");
            }
        }

        private void LoadFromSchedule(PublicSaleSchedule schedule)
        {
            Scenario1WinningBid = schedule.Scenario1WinningBid;
            Scenario2WinningBid = schedule.Scenario2WinningBid;
            Scenario1EvalReason = schedule.Scenario1EvalReason;
            Scenario2EvalReason = schedule.Scenario2EvalReason;
            
            LandAppraisalValue = schedule.LandAppraisalValue;
            BuildingAppraisalValue = schedule.BuildingAppraisalValue;
            MachineryAppraisalValue = schedule.MachineryAppraisalValue;
            OtherBankSeniorValue = schedule.OtherBankSeniorValue;
            
            Scenario1StartDate = schedule.Scenario1StartDate;
            Scenario2StartDate = schedule.Scenario2StartDate;
            Scenario1BeneficiaryChangeCost = schedule.Scenario1BeneficiaryChangeCost;
            Scenario2BeneficiaryChangeCost = schedule.Scenario2BeneficiaryChangeCost;
            Scenario1SeniorDeduction = schedule.Scenario1SeniorDeduction;
            Scenario2SeniorDeduction = schedule.Scenario2SeniorDeduction;
            Scenario1DisposalFee = schedule.Scenario1DisposalFee;
            Scenario2DisposalFee = schedule.Scenario2DisposalFee;
            Scenario1CreditorDistribution = schedule.Scenario1CreditorDistribution;
            Scenario2CreditorDistribution = schedule.Scenario2CreditorDistribution;
            Scenario1LoanCap = schedule.Scenario1LoanCap;
            Scenario2LoanCap = schedule.Scenario2LoanCap;
            Scenario1LoanCap2 = schedule.Scenario1LoanCap2;
            Scenario2LoanCap2 = schedule.Scenario2LoanCap2;
            Scenario1MortgageCap = schedule.Scenario1MortgageCap;
            Scenario2MortgageCap = schedule.Scenario2MortgageCap;
            
            OnbidFee1 = schedule.OnbidFee;
            OnbidFee2 = schedule.OnbidFee;
            AppraisalFee = schedule.AppraisalFee;
            
            LeadTimeDays = schedule.LeadTimeDays;
            DiscountRate = schedule.DiscountRate;
            InitialAppraisalValue = schedule.InitialAppraisalValue;
        }

        private void CalculateSaleCosts()
        {
            // 온비드수수료 (낙찰가 기준)
            OnbidFee1 = CalculateOnbidFee(Scenario1WinningBid);
            OnbidFee2 = CalculateOnbidFee(Scenario2WinningBid);
            
            // 감정평가료 (감정평가금액 기준)
            AppraisalFee = CalculateAppraisalFee(TotalAppraisalValue > 0 ? TotalAppraisalValue : AppraisalValue);
            
            // 환가처분보수
            Scenario1DisposalFee = DisposalFeeAmount1;
            Scenario2DisposalFee = DisposalFeeAmount2;
            
            // 타채권자 배분 합계
            Scenario1CreditorDistribution = TotalCreditorAmount1;
            Scenario2CreditorDistribution = TotalCreditorAmount2;
        }

        private decimal CalculateOnbidFee(decimal salePrice)
        {
            // 온비드 수수료 계산 (낙찰가 기준)
            if (salePrice <= 0) return 0;
            return salePrice * 0.001m; // 0.1%
        }

        private decimal CalculateAppraisalFee(decimal amount)
        {
            if (amount <= 197727272) return 290000 + 48000 + 33800;
            if (amount <= 200000000) return 10610000 + 48000 + 1065800;
            if (amount <= 500000000) return 10610000 + 88000 + 1069800;
            if (amount <= 1000000000) return 8782000 + 88000 + 887000;
            if (amount <= 5000000000) return 7908000 + 88000 + 799600;
            if (amount <= 10000000000) return 7354000 + 88000 + 744200;
            return 7200000 + 88000 + 728800;
        }

        private decimal CalculateDisposalFee(decimal salePrice)
        {
            // 환가처분보수 계산
            if (salePrice <= 0) return 0;
            return salePrice * 0.01m; // 1%
        }

        private void GenerateLeadTimeSchedules()
        {
            LeadTimeSchedules.Clear();
            
            var startDate = DateTime.Today;
            var currentBid = InitialAppraisalValue > 0 ? InitialAppraisalValue : (AppraisalValue > 0 ? AppraisalValue : 1000000000m);
            
            for (int i = 1; i <= 22; i++)
            {
                var scheduleDate = startDate.AddDays((i - 1) * LeadTimeDays);
                var minimumBid = currentBid * (decimal)Math.Pow((double)(1 - DiscountRate), i - 1);
                
                LeadTimeSchedules.Add(new LeadTimeScheduleItem
                {
                    Round = i,
                    Date = scheduleDate,
                    MinimumBid = minimumBid
                });
            }
        }

        public async Task SaveAsync()
        {
            if (_scheduleRepository == null || !PropertyId.HasValue)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError("저장할 수 없습니다.");
                return;
            }
            
            try
            {
                IsLoading = true;
                
                var existingSchedules = await _scheduleRepository.GetByPropertyIdAsync(PropertyId.Value);
                var schedule = existingSchedules.Count > 0 ? existingSchedules.First() : new PublicSaleSchedule { Id = Guid.NewGuid() };
                
                MapToSchedule(schedule);
                
                if (existingSchedules.Count > 0)
                {
                    await _scheduleRepository.UpdateAsync(schedule);
                }
                else
                {
                    await _scheduleRepository.CreateAsync(schedule);
                }
                
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("공매 일정이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
                NPLogic.UI.Services.ToastService.Instance.ShowError($"저장 실패: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void MapToSchedule(PublicSaleSchedule schedule)
        {
            schedule.PropertyId = PropertyId;
            
            schedule.Scenario1WinningBid = Scenario1WinningBid;
            schedule.Scenario2WinningBid = Scenario2WinningBid;
            schedule.Scenario1EvalReason = Scenario1EvalReason;
            schedule.Scenario2EvalReason = Scenario2EvalReason;
            
            schedule.LandAppraisalValue = LandAppraisalValue;
            schedule.BuildingAppraisalValue = BuildingAppraisalValue;
            schedule.MachineryAppraisalValue = MachineryAppraisalValue;
            schedule.OtherBankSeniorValue = OtherBankSeniorValue;
            schedule.TotalAppraisalValue = TotalAppraisalValue;
            
            schedule.Scenario1StartDate = Scenario1StartDate;
            schedule.Scenario2StartDate = Scenario2StartDate;
            schedule.Scenario1BeneficiaryChangeCost = Scenario1BeneficiaryChangeCost;
            schedule.Scenario2BeneficiaryChangeCost = Scenario2BeneficiaryChangeCost;
            schedule.Scenario1EstimatedWinningBid = Scenario1EstimatedWinningBid;
            schedule.Scenario2EstimatedWinningBid = Scenario2EstimatedWinningBid;
            schedule.Scenario1SeniorDeduction = Scenario1SeniorDeduction;
            schedule.Scenario2SeniorDeduction = Scenario2SeniorDeduction;
            schedule.Scenario1SaleCost = Scenario1SaleCost;
            schedule.Scenario2SaleCost = Scenario2SaleCost;
            schedule.Scenario1DisposalFee = Scenario1DisposalFee;
            schedule.Scenario2DisposalFee = Scenario2DisposalFee;
            schedule.Scenario1DistributableBefore = Scenario1DistributableBefore;
            schedule.Scenario2DistributableBefore = Scenario2DistributableBefore;
            schedule.Scenario1CreditorDistribution = Scenario1CreditorDistribution;
            schedule.Scenario2CreditorDistribution = Scenario2CreditorDistribution;
            schedule.Scenario1DistributableAfter = Scenario1DistributableAfter;
            schedule.Scenario2DistributableAfter = Scenario2DistributableAfter;
            schedule.Scenario1LoanCap = Scenario1LoanCap;
            schedule.Scenario2LoanCap = Scenario2LoanCap;
            schedule.Scenario1LoanCap2 = Scenario1LoanCap2;
            schedule.Scenario2LoanCap2 = Scenario2LoanCap2;
            schedule.Scenario1MortgageCap = Scenario1MortgageCap;
            schedule.Scenario2MortgageCap = Scenario2MortgageCap;
            schedule.Scenario1CapAppliedDividend = Scenario1CapAppliedDividend;
            schedule.Scenario2CapAppliedDividend = Scenario2CapAppliedDividend;
            schedule.Scenario1RecoverableAmount = Scenario1RecoverableAmount;
            schedule.Scenario2RecoverableAmount = Scenario2RecoverableAmount;
            
            schedule.OnbidFee = OnbidFee1;
            schedule.AppraisalFee = AppraisalFee;
            schedule.TotalSaleCost = TotalSaleCost1;
            
            schedule.ExpectedSalePrice = Scenario1WinningBid;
            schedule.DisposalFeeAmount = DisposalFeeAmount1;
            
            schedule.LeadTimeDays = LeadTimeDays;
            schedule.DiscountRate = DiscountRate;
            schedule.InitialAppraisalValue = InitialAppraisalValue;
        }

        private void NotifyCalculatedPropertiesChanged()
        {
            OnPropertyChanged(nameof(Scenario1BidRate));
            OnPropertyChanged(nameof(Scenario2BidRate));
            OnPropertyChanged(nameof(Scenario1LandPricePerPyeong));
            OnPropertyChanged(nameof(Scenario2LandPricePerPyeong));
            OnPropertyChanged(nameof(Scenario1BuildingPricePerPyeong));
            OnPropertyChanged(nameof(Scenario2BuildingPricePerPyeong));
            OnPropertyChanged(nameof(TotalAppraisalValue));
            OnPropertyChanged(nameof(Scenario1EstimatedWinningBid));
            OnPropertyChanged(nameof(Scenario2EstimatedWinningBid));
            OnPropertyChanged(nameof(Scenario1SaleCost));
            OnPropertyChanged(nameof(Scenario2SaleCost));
            OnPropertyChanged(nameof(Scenario1DistributableBefore));
            OnPropertyChanged(nameof(Scenario2DistributableBefore));
            OnPropertyChanged(nameof(Scenario1DistributableAfter));
            OnPropertyChanged(nameof(Scenario2DistributableAfter));
            OnPropertyChanged(nameof(Scenario1CapAppliedDividend));
            OnPropertyChanged(nameof(Scenario2CapAppliedDividend));
            OnPropertyChanged(nameof(Scenario1RecoverableAmount));
            OnPropertyChanged(nameof(Scenario2RecoverableAmount));
            OnPropertyChanged(nameof(TotalSaleCost1));
            OnPropertyChanged(nameof(TotalSaleCost2));
            OnPropertyChanged(nameof(DisposalFeeAmount1));
            OnPropertyChanged(nameof(DisposalFeeAmount2));
            OnPropertyChanged(nameof(Creditor1Amount1));
            OnPropertyChanged(nameof(Creditor1Amount2));
            OnPropertyChanged(nameof(Creditor2Amount1));
            OnPropertyChanged(nameof(Creditor2Amount2));
            OnPropertyChanged(nameof(TotalCreditorRatio));
            OnPropertyChanged(nameof(TotalCreditorAmount1));
            OnPropertyChanged(nameof(TotalCreditorAmount2));
        }

        partial void OnScenario1WinningBidChanged(decimal value)
        {
            CalculateSaleCosts();
            NotifyCalculatedPropertiesChanged();
        }

        partial void OnScenario2WinningBidChanged(decimal value)
        {
            CalculateSaleCosts();
            NotifyCalculatedPropertiesChanged();
        }

        partial void OnAppraisalValueChanged(decimal value)
        {
            if (InitialAppraisalValue == 0)
                InitialAppraisalValue = value;
            CalculateSaleCosts();
            GenerateLeadTimeSchedules();
            NotifyCalculatedPropertiesChanged();
        }

        partial void OnLandAppraisalValueChanged(decimal value)
        {
            CalculateSaleCosts();
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }

        partial void OnBuildingAppraisalValueChanged(decimal value)
        {
            CalculateSaleCosts();
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }

        partial void OnMachineryAppraisalValueChanged(decimal value)
        {
            CalculateSaleCosts();
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }

        partial void OnOtherBankSeniorValueChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }

        partial void OnCreditor1RatioChanged(decimal value)
        {
            Scenario1CreditorDistribution = TotalCreditorAmount1;
            Scenario2CreditorDistribution = TotalCreditorAmount2;
            NotifyCalculatedPropertiesChanged();
        }

        partial void OnCreditor2RatioChanged(decimal value)
        {
            Scenario1CreditorDistribution = TotalCreditorAmount1;
            Scenario2CreditorDistribution = TotalCreditorAmount2;
            NotifyCalculatedPropertiesChanged();
        }

        partial void OnLeadTimeDaysChanged(int value)
        {
            GenerateLeadTimeSchedules();
        }

        partial void OnDiscountRateChanged(decimal value)
        {
            GenerateLeadTimeSchedules();
        }

        partial void OnInitialAppraisalValueChanged(decimal value)
        {
            GenerateLeadTimeSchedules();
        }

        partial void OnPropertyIdChanged(Guid? value)
        {
            if (value.HasValue)
            {
                _ = LoadPropertyDataAsync();
            }
        }

        public void SetPropertyId(Guid propertyId)
        {
            PropertyId = propertyId;
        }
    }
}
