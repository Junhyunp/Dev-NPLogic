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
    /// 경매일정(Ⅶ) ViewModel - 엑셀 산출화면 기반 전면 재구성
    /// </summary>
    public partial class AuctionScheduleDetailViewModel : ObservableObject
    {
        private readonly SupabaseService? _supabaseService;
        private readonly AuctionScheduleRepository? _scheduleRepository;
        private readonly PropertyRepository? _propertyRepository;
        private readonly EvaluationRepository? _evaluationRepository;
        private readonly RightAnalysisRepository? _rightAnalysisRepository;
        
        // ========== 기본 정보 ==========
        
        [ObservableProperty]
        private Guid? _propertyId;
        
        [ObservableProperty]
        private bool _isAuction = true;
        
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

        // ========== 차주사항 관련 ==========
        
        [ObservableProperty]
        private bool _isBorrowerDeceased;
        
        [ObservableProperty]
        private bool _isOwnerMovedIn;
        
        [ObservableProperty]
        private bool _isSessionOpened;
        
        [ObservableProperty]
        private string? _loanType;
        
        [ObservableProperty]
        private bool _hasSubrogationRegistration;
        
        [ObservableProperty]
        private string? _subrogationEntity;
        
        [ObservableProperty]
        private bool _isHousingMortgageLoan;
        
        [ObservableProperty]
        private decimal _subrogationCost;
        
        [ObservableProperty]
        private DateTime? _finalCompletionDate;
        
        [ObservableProperty]
        private string? _jurisdictionCourt;
        
        [ObservableProperty]
        private decimal _courtDiscountRate;
        
        [ObservableProperty]
        private string _auctionRequestType = "*1";

        // ========== 시나리오별 일정 상세 ==========
        
        [ObservableProperty]
        private decimal _scenario1AuctionRequestAmount;
        
        [ObservableProperty]
        private decimal _scenario2AuctionRequestAmount;
        
        [ObservableProperty]
        private DateTime? _scenario1AuctionRequestDate;
        
        [ObservableProperty]
        private DateTime? _scenario2AuctionRequestDate;
        
        [ObservableProperty]
        private int _scenario1ProcedureMonths;
        
        [ObservableProperty]
        private int _scenario2ProcedureMonths;
        
        [ObservableProperty]
        private int _scenario1FirstRoundMonths = 6;
        
        [ObservableProperty]
        private int _scenario2FirstRoundMonths = 6;
        
        [ObservableProperty]
        private int? _scenario1ExpectedRound;
        
        [ObservableProperty]
        private int? _scenario2ExpectedRound;
        
        [ObservableProperty]
        private decimal _scenario1MinBidRate;
        
        [ObservableProperty]
        private decimal _scenario2MinBidRate;

        // 계산된 속성들
        public decimal Scenario1LegalPrice => AppraisalValue;
        public decimal Scenario2LegalPrice => AppraisalValue;
        public decimal Scenario1MinLegalPrice => Scenario1LegalPrice * (1 - Scenario1MinBidRate / 100);
        public decimal Scenario2MinLegalPrice => Scenario2LegalPrice * (1 - Scenario2MinBidRate / 100);
        
        public int Scenario1ProcedureLeadTime => Scenario1ProcedureMonths;
        public int Scenario2ProcedureLeadTime => Scenario2ProcedureMonths;
        public int Scenario1LegalPriceLeadTime => Scenario1ProcedureMonths + Scenario1FirstRoundMonths;
        public int Scenario2LegalPriceLeadTime => Scenario2ProcedureMonths + Scenario2FirstRoundMonths;
        public int Scenario1MinBidRateLeadTime => Scenario1LegalPriceLeadTime + (Scenario1ExpectedRound ?? 0);
        public int Scenario2MinBidRateLeadTime => Scenario2LegalPriceLeadTime + (Scenario2ExpectedRound ?? 0);
        public int Scenario1TotalLeadTime => Scenario1MinBidRateLeadTime;
        public int Scenario2TotalLeadTime => Scenario2MinBidRateLeadTime;

        // ========== 비용 관련 (7개 항목) ==========
        
        [ObservableProperty]
        private decimal _newspaperAdFee = 220000;
        
        [ObservableProperty]
        private decimal _surveyFee = 70000;
        
        [ObservableProperty]
        private decimal _auctionSaleFee;
        
        [ObservableProperty]
        private decimal _appraisalFee;
        
        [ObservableProperty]
        private decimal _deliveryFee;
        
        [ObservableProperty]
        private decimal _registrationEducationFee;
        
        [ObservableProperty]
        private decimal _otherCost = 5000;
        
        [ObservableProperty]
        private decimal _additionalCostRate = 0.10m;
        
        [ObservableProperty]
        private int _propertyCount = 1;
        
        [ObservableProperty]
        private int _creditorCount = 1;

        public decimal TotalAuctionCost => NewspaperAdFee + SurveyFee + AuctionSaleFee + AppraisalFee + DeliveryFee + RegistrationEducationFee + OtherCost;
        public decimal TotalCostWithAdditional => TotalAuctionCost * (1 + AdditionalCostRate);

        // ========== 배당/회수 관련 (시나리오별) ==========
        
        [ObservableProperty]
        private decimal _scenario1SeniorDeduction;
        
        [ObservableProperty]
        private decimal _scenario2SeniorDeduction;
        
        [ObservableProperty]
        private decimal _scenario1AuctionFees;
        
        [ObservableProperty]
        private decimal _scenario2AuctionFees;

        public decimal Scenario1DistributableAfterSale => Scenario1WinningBid - Scenario1AuctionFees;
        public decimal Scenario2DistributableAfterSale => Scenario2WinningBid - Scenario2AuctionFees;
        public decimal Scenario1DistributableAfterSenior => Scenario1DistributableAfterSale - Scenario1SeniorDeduction;
        public decimal Scenario2DistributableAfterSenior => Scenario2DistributableAfterSale - Scenario2SeniorDeduction;

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
                var distributable = Scenario1DistributableAfterSenior;
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
                var distributable = Scenario2DistributableAfterSenior;
                if (distributable <= 0) return 0;
                
                decimal min = distributable;
                if (Scenario2LoanCap > 0 && Scenario2LoanCap < min) min = Scenario2LoanCap;
                if (Scenario2LoanCap2 > 0 && Scenario2LoanCap2 < min) min = Scenario2LoanCap2;
                if (Scenario2MortgageCap > 0 && Scenario2MortgageCap < min) min = Scenario2MortgageCap;
                return min;
            }
        }

        [ObservableProperty]
        private decimal _scenario1PrepaidFeeRecovery;
        
        [ObservableProperty]
        private decimal _scenario2PrepaidFeeRecovery;

        public decimal Scenario1DividendRecoverable => Scenario1CapAppliedDividend + Scenario1PrepaidFeeRecovery;
        public decimal Scenario2DividendRecoverable => Scenario2CapAppliedDividend + Scenario2PrepaidFeeRecovery;

        // ========== 경매예납금액 회수액 추정 ==========
        
        [ObservableProperty]
        private decimal _actualPaidDeposit;
        
        [ObservableProperty]
        private decimal _depositRecoveryRate = 0.90m;

        public decimal EstimatedDepositRecovery => ActualPaidDeposit * DepositRecoveryRate;

        // ========== 3자선행/중복경매 관련 ==========
        
        [ObservableProperty]
        private decimal _thirdPartyAuctionAmount;
        
        [ObservableProperty]
        private decimal _duplicateAuctionAmount;

        // ========== Lead time ==========
        
        [ObservableProperty]
        private int _leadTimeDays = 11;
        
        [ObservableProperty]
        private decimal _discountRate = 0.10m;
        
        [ObservableProperty]
        private DateTime? _appraisalDate;
        
        [ObservableProperty]
        private ObservableCollection<LeadTimeScheduleItem> _leadTimeSchedules = new();

        public AuctionScheduleDetailViewModel()
        {
        }
        
        public AuctionScheduleDetailViewModel(
            SupabaseService? supabaseService,
            AuctionScheduleRepository? scheduleRepository,
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
                
                CalculateAuctionFees();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
                Debug.WriteLine($"AuctionScheduleDetailViewModel 초기화 실패: {ex}");
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
                            AppraisalValue = details.AppraisalValue.Value;
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
                        var schedule = schedules.First();
                        LoadFromSchedule(schedule);
                    }
                }
                
                NotifyCalculatedPropertiesChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"물건 데이터 로드 실패: {ex.Message}");
            }
        }
        
        private void LoadFromSchedule(AuctionSchedule schedule)
        {
            IsAuction = schedule.ScheduleType == "auction";
            
            Scenario1WinningBid = schedule.Scenario1WinningBid;
            Scenario2WinningBid = schedule.Scenario2WinningBid;
            Scenario1EvalReason = schedule.Scenario1EvalReason;
            Scenario2EvalReason = schedule.Scenario2EvalReason;
            
            LandAppraisalValue = schedule.LandAppraisalValue;
            BuildingAppraisalValue = schedule.BuildingAppraisalValue;
            MachineryAppraisalValue = schedule.MachineryAppraisalValue;
            OtherBankSeniorValue = schedule.OtherBankSeniorValue;
            
            IsBorrowerDeceased = schedule.IsBorrowerDeceased;
            IsOwnerMovedIn = schedule.IsOwnerMovedIn;
            IsSessionOpened = schedule.IsSessionOpened;
            LoanType = schedule.LoanType;
            HasSubrogationRegistration = schedule.HasSubrogationRegistration;
            SubrogationEntity = schedule.SubrogationEntity;
            IsHousingMortgageLoan = schedule.IsHousingMortgageLoan;
            SubrogationCost = schedule.SubrogationCost;
            FinalCompletionDate = schedule.FinalCompletionDate;
            JurisdictionCourt = schedule.JurisdictionCourt;
            CourtDiscountRate = schedule.CourtDiscountRate;
            AuctionRequestType = schedule.AuctionRequestType;
            
            Scenario1AuctionRequestDate = schedule.Scenario1AuctionRequestDate;
            Scenario2AuctionRequestDate = schedule.Scenario2AuctionRequestDate;
            Scenario1ProcedureMonths = schedule.Scenario1ProcedureMonths;
            Scenario2ProcedureMonths = schedule.Scenario2ProcedureMonths;
            Scenario1FirstRoundMonths = schedule.Scenario1FirstRoundMonths;
            Scenario2FirstRoundMonths = schedule.Scenario2FirstRoundMonths;
            Scenario1ExpectedRound = schedule.Scenario1ExpectedRound;
            Scenario2ExpectedRound = schedule.Scenario2ExpectedRound;
            Scenario1MinBidRate = schedule.Scenario1MinBidRate;
            Scenario2MinBidRate = schedule.Scenario2MinBidRate;
            
            NewspaperAdFee = schedule.NewspaperAdFee;
            SurveyFee = schedule.SurveyFee;
            AuctionSaleFee = schedule.AuctionSaleFee;
            AppraisalFee = schedule.AppraisalFee;
            DeliveryFee = schedule.DeliveryFee;
            RegistrationEducationFee = schedule.RegistrationEducationFee;
            OtherCost = schedule.OtherCost;
            AdditionalCostRate = schedule.AdditionalCostRate;
            PropertyCount = schedule.PropertyCount;
            CreditorCount = schedule.CreditorCount;
            
            Scenario1SeniorDeduction = schedule.Scenario1SeniorDeduction;
            Scenario2SeniorDeduction = schedule.Scenario2SeniorDeduction;
            Scenario1AuctionFees = schedule.Scenario1AuctionFees;
            Scenario2AuctionFees = schedule.Scenario2AuctionFees;
            Scenario1LoanCap = schedule.Scenario1LoanCap;
            Scenario2LoanCap = schedule.Scenario2LoanCap;
            Scenario1LoanCap2 = schedule.Scenario1LoanCap2;
            Scenario2LoanCap2 = schedule.Scenario2LoanCap2;
            Scenario1MortgageCap = schedule.Scenario1MortgageCap;
            Scenario2MortgageCap = schedule.Scenario2MortgageCap;
            Scenario1PrepaidFeeRecovery = schedule.Scenario1PrepaidFeeRecovery;
            Scenario2PrepaidFeeRecovery = schedule.Scenario2PrepaidFeeRecovery;
            
            ActualPaidDeposit = schedule.ActualPaidDeposit;
            DepositRecoveryRate = schedule.DepositRecoveryRate;
            
            ThirdPartyAuctionAmount = schedule.ThirdPartyAuctionAmount;
            DuplicateAuctionAmount = schedule.DuplicateAuctionAmount;
        }
        
        /// <summary>
        /// 경매비용 자동 계산 (엑셀 수식 기반)
        /// </summary>
        private void CalculateAuctionFees()
        {
            // 3. 경매매각수수료 계산 (매각대금 기준)
            AuctionSaleFee = CalculateSaleFee(Scenario1WinningBid);
            
            // 4. 감정수수료 계산 (평가금액 기준)
            AppraisalFee = CalculateAppraisalFee(TotalAppraisalValue > 0 ? TotalAppraisalValue : AppraisalValue);
            
            // 5. 송달수수료 계산 (채권자수 + 3) * 10회분 * 3,020원
            DeliveryFee = (CreditorCount + 3) * 10 * 3020;
            
            // 6. 경매신청 등록/교육세 계산
            RegistrationEducationFee = CalculateRegistrationFee(Scenario1AuctionRequestAmount);
            
            // 시나리오별 경매수수료 및 제세
            Scenario1AuctionFees = TotalCostWithAdditional;
            Scenario2AuctionFees = TotalCostWithAdditional;
        }
        
        /// <summary>
        /// 매각수수료 계산 (과표범위별)
        /// </summary>
        private decimal CalculateSaleFee(decimal amount)
        {
            if (amount <= 100000) return 5000;
            if (amount <= 10000000) return 5000 + (amount - 100000) * 0.02m;
            if (amount <= 50000000) return 203000 + (amount - 10000000) * 0.015m;
            if (amount <= 100000000) return 803000 + (amount - 50000000) * 0.01m;
            if (amount <= 300000000) return 1303000 + (amount - 100000000) * 0.005m;
            if (amount <= 500000000) return 2303000 + (amount - 300000000) * 0.003m;
            if (amount <= 1000000000) return 2903000 + (amount - 500000000) * 0.002m;
            return 3903000;
        }
        
        /// <summary>
        /// 감정수수료 계산 (평가금액 기준)
        /// </summary>
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
        
        /// <summary>
        /// 등록/교육세 계산
        /// </summary>
        private decimal CalculateRegistrationFee(decimal claimAmount)
        {
            return claimAmount * 0.002m + claimAmount * 0.0002m;
        }
        
        private void GenerateLeadTimeSchedules()
        {
            LeadTimeSchedules.Clear();
            
            var startDate = DateTime.Today;
            var currentBid = AppraisalValue > 0 ? AppraisalValue : 1000000000m;
            
            for (int i = 1; i <= 22; i++)
            {
                var scheduleDate = startDate.AddDays((i - 1) * LeadTimeDays * 7 / 11);
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
                var schedule = existingSchedules.Count > 0 ? existingSchedules.First() : new AuctionSchedule { Id = Guid.NewGuid() };
                
                MapToSchedule(schedule);
                
                if (existingSchedules.Count > 0)
                {
                    await _scheduleRepository.UpdateAsync(schedule);
                }
                else
                {
                    await _scheduleRepository.CreateAsync(schedule);
                }
                
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경매 일정이 저장되었습니다.");
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
        
        private void MapToSchedule(AuctionSchedule schedule)
        {
            schedule.PropertyId = PropertyId;
            schedule.ScheduleType = IsAuction ? "auction" : "public_sale";
            
            schedule.Scenario1WinningBid = Scenario1WinningBid;
            schedule.Scenario2WinningBid = Scenario2WinningBid;
            schedule.Scenario1EvalReason = Scenario1EvalReason;
            schedule.Scenario2EvalReason = Scenario2EvalReason;
            
            schedule.LandAppraisalValue = LandAppraisalValue;
            schedule.BuildingAppraisalValue = BuildingAppraisalValue;
            schedule.MachineryAppraisalValue = MachineryAppraisalValue;
            schedule.OtherBankSeniorValue = OtherBankSeniorValue;
            
            schedule.IsBorrowerDeceased = IsBorrowerDeceased;
            schedule.IsOwnerMovedIn = IsOwnerMovedIn;
            schedule.IsSessionOpened = IsSessionOpened;
            schedule.LoanType = LoanType;
            schedule.HasSubrogationRegistration = HasSubrogationRegistration;
            schedule.SubrogationEntity = SubrogationEntity;
            schedule.IsHousingMortgageLoan = IsHousingMortgageLoan;
            schedule.SubrogationCost = SubrogationCost;
            schedule.FinalCompletionDate = FinalCompletionDate;
            schedule.JurisdictionCourt = JurisdictionCourt;
            schedule.CourtDiscountRate = CourtDiscountRate;
            schedule.AuctionRequestType = AuctionRequestType;
            
            schedule.Scenario1AuctionRequestDate = Scenario1AuctionRequestDate;
            schedule.Scenario2AuctionRequestDate = Scenario2AuctionRequestDate;
            schedule.Scenario1ProcedureMonths = Scenario1ProcedureMonths;
            schedule.Scenario2ProcedureMonths = Scenario2ProcedureMonths;
            schedule.Scenario1FirstRoundMonths = Scenario1FirstRoundMonths;
            schedule.Scenario2FirstRoundMonths = Scenario2FirstRoundMonths;
            schedule.Scenario1LegalPrice = Scenario1LegalPrice;
            schedule.Scenario2LegalPrice = Scenario2LegalPrice;
            schedule.Scenario1ExpectedRound = Scenario1ExpectedRound;
            schedule.Scenario2ExpectedRound = Scenario2ExpectedRound;
            schedule.Scenario1MinBidRate = Scenario1MinBidRate;
            schedule.Scenario2MinBidRate = Scenario2MinBidRate;
            schedule.Scenario1MinLegalPrice = Scenario1MinLegalPrice;
            schedule.Scenario2MinLegalPrice = Scenario2MinLegalPrice;
            
            schedule.NewspaperAdFee = NewspaperAdFee;
            schedule.SurveyFee = SurveyFee;
            schedule.AuctionSaleFee = AuctionSaleFee;
            schedule.AppraisalFee = AppraisalFee;
            schedule.DeliveryFee = DeliveryFee;
            schedule.RegistrationEducationFee = RegistrationEducationFee;
            schedule.OtherCost = OtherCost;
            schedule.AdditionalCostRate = AdditionalCostRate;
            schedule.PropertyCount = PropertyCount;
            schedule.CreditorCount = CreditorCount;
            
            schedule.Scenario1SeniorDeduction = Scenario1SeniorDeduction;
            schedule.Scenario2SeniorDeduction = Scenario2SeniorDeduction;
            schedule.Scenario1AuctionFees = Scenario1AuctionFees;
            schedule.Scenario2AuctionFees = Scenario2AuctionFees;
            schedule.Scenario1DistributableAfterSale = Scenario1DistributableAfterSale;
            schedule.Scenario2DistributableAfterSale = Scenario2DistributableAfterSale;
            schedule.Scenario1DistributableAfterSenior = Scenario1DistributableAfterSenior;
            schedule.Scenario2DistributableAfterSenior = Scenario2DistributableAfterSenior;
            schedule.Scenario1LoanCap = Scenario1LoanCap;
            schedule.Scenario2LoanCap = Scenario2LoanCap;
            schedule.Scenario1LoanCap2 = Scenario1LoanCap2;
            schedule.Scenario2LoanCap2 = Scenario2LoanCap2;
            schedule.Scenario1MortgageCap = Scenario1MortgageCap;
            schedule.Scenario2MortgageCap = Scenario2MortgageCap;
            schedule.Scenario1CapAppliedDividend = Scenario1CapAppliedDividend;
            schedule.Scenario2CapAppliedDividend = Scenario2CapAppliedDividend;
            schedule.Scenario1PrepaidFeeRecovery = Scenario1PrepaidFeeRecovery;
            schedule.Scenario2PrepaidFeeRecovery = Scenario2PrepaidFeeRecovery;
            schedule.Scenario1DividendRecoverable = Scenario1DividendRecoverable;
            schedule.Scenario2DividendRecoverable = Scenario2DividendRecoverable;
            
            schedule.ActualPaidDeposit = ActualPaidDeposit;
            schedule.DepositRecoveryRate = DepositRecoveryRate;
            
            schedule.ThirdPartyAuctionAmount = ThirdPartyAuctionAmount;
            schedule.DuplicateAuctionAmount = DuplicateAuctionAmount;
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
            OnPropertyChanged(nameof(TotalAuctionCost));
            OnPropertyChanged(nameof(TotalCostWithAdditional));
            OnPropertyChanged(nameof(Scenario1LegalPrice));
            OnPropertyChanged(nameof(Scenario2LegalPrice));
            OnPropertyChanged(nameof(Scenario1MinLegalPrice));
            OnPropertyChanged(nameof(Scenario2MinLegalPrice));
            OnPropertyChanged(nameof(Scenario1DistributableAfterSale));
            OnPropertyChanged(nameof(Scenario2DistributableAfterSale));
            OnPropertyChanged(nameof(Scenario1DistributableAfterSenior));
            OnPropertyChanged(nameof(Scenario2DistributableAfterSenior));
            OnPropertyChanged(nameof(Scenario1CapAppliedDividend));
            OnPropertyChanged(nameof(Scenario2CapAppliedDividend));
            OnPropertyChanged(nameof(Scenario1DividendRecoverable));
            OnPropertyChanged(nameof(Scenario2DividendRecoverable));
            OnPropertyChanged(nameof(EstimatedDepositRecovery));
            OnPropertyChanged(nameof(Scenario1TotalLeadTime));
            OnPropertyChanged(nameof(Scenario2TotalLeadTime));
        }
        
        partial void OnScenario1WinningBidChanged(decimal value)
        {
            CalculateAuctionFees();
            NotifyCalculatedPropertiesChanged();
        }
        
        partial void OnScenario2WinningBidChanged(decimal value)
        {
            NotifyCalculatedPropertiesChanged();
        }
        
        partial void OnAppraisalValueChanged(decimal value)
        {
            CalculateAuctionFees();
            GenerateLeadTimeSchedules();
            NotifyCalculatedPropertiesChanged();
        }
        
        partial void OnLandAppraisalValueChanged(decimal value)
        {
            CalculateAuctionFees();
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }
        
        partial void OnBuildingAppraisalValueChanged(decimal value)
        {
            CalculateAuctionFees();
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }
        
        partial void OnMachineryAppraisalValueChanged(decimal value)
        {
            CalculateAuctionFees();
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }
        
        partial void OnOtherBankSeniorValueChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalAppraisalValue));
        }
        
        partial void OnCreditorCountChanged(int value)
        {
            CalculateAuctionFees();
        }
        
        partial void OnLeadTimeDaysChanged(int value)
        {
            GenerateLeadTimeSchedules();
        }
        
        partial void OnDiscountRateChanged(decimal value)
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
    
    public class LeadTimeScheduleItem
    {
        public int Round { get; set; }
        public DateTime Date { get; set; }
        public decimal MinimumBid { get; set; }
    }
}
