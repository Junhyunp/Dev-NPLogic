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
    /// 경(공)매 일정 상세 ViewModel - 산출화면 기반
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
        
        public decimal Scenario1BidRate => AppraisalValue > 0 ? (Scenario1WinningBid / AppraisalValue) * 100 : 0;
        public decimal Scenario2BidRate => AppraisalValue > 0 ? (Scenario2WinningBid / AppraisalValue) * 100 : 0;
        
        // ========== 대금 회수 ==========
        
        [ObservableProperty]
        private DateTime? _recoveryDate1;
        
        [ObservableProperty]
        private decimal _recoveryAmount1;
        
        [ObservableProperty]
        private DateTime? _recoveryDate2;
        
        [ObservableProperty]
        private decimal _recoveryAmount2;
        
        [ObservableProperty]
        private string? _recoveryNote;
        
        // ========== 수익자변경비용 ==========
        
        [ObservableProperty]
        private DateTime? _saleStartDate1;
        
        [ObservableProperty]
        private DateTime? _saleStartDate2;
        
        // ========== 경(공)매비용 ==========
        
        [ObservableProperty]
        private decimal _auctionCost1;
        
        [ObservableProperty]
        private decimal _auctionCost2;
        
        [ObservableProperty]
        private decimal _appraisalFee1;
        
        [ObservableProperty]
        private decimal _appraisalFee2;
        
        public decimal TotalCost1 => AuctionCost1 + AppraisalFee1;
        public decimal TotalCost2 => AuctionCost2 + AppraisalFee2;
        
        // ========== 배당/회수 계산 ==========
        
        [ObservableProperty]
        private decimal _seniorDeduction1;
        
        [ObservableProperty]
        private decimal _seniorDeduction2;
        
        [ObservableProperty]
        private decimal _disposalFee1;
        
        [ObservableProperty]
        private decimal _disposalFee2;
        
        [ObservableProperty]
        private decimal _loanCap1;
        
        [ObservableProperty]
        private decimal _loanCap2;
        
        [ObservableProperty]
        private decimal _mortgageCap1;
        
        [ObservableProperty]
        private decimal _mortgageCap2;
        
        /// <summary>
        /// 배당가능액-안분전 = 추정낙찰액 - 선순위차감 - 경(공)매비용 - 환가처분보수
        /// </summary>
        public decimal DistributableBeforeProration1 => 
            Scenario1WinningBid - SeniorDeduction1 - TotalCost1 - DisposalFee1;
        
        public decimal DistributableBeforeProration2 => 
            Scenario2WinningBid - SeniorDeduction2 - TotalCost2 - DisposalFee2;
        
        /// <summary>
        /// Cap반영배당액 = Min(배당가능액-안분전, LoanCap, MortgageCap)
        /// </summary>
        public decimal CapAppliedDividend1
        {
            get
            {
                var distributable = DistributableBeforeProration1;
                if (distributable <= 0) return 0;
                
                var caps = new[] { distributable, LoanCap1, MortgageCap1 };
                decimal min = distributable;
                foreach (var cap in caps)
                {
                    if (cap > 0 && cap < min) min = cap;
                }
                return min;
            }
        }
        
        public decimal CapAppliedDividend2
        {
            get
            {
                var distributable = DistributableBeforeProration2;
                if (distributable <= 0) return 0;
                
                var caps = new[] { distributable, LoanCap2, MortgageCap2 };
                decimal min = distributable;
                foreach (var cap in caps)
                {
                    if (cap > 0 && cap < min) min = cap;
                }
                return min;
            }
        }
        
        /// <summary>
        /// 회수가능액 = Cap반영배당액
        /// </summary>
        public decimal RecoverableAmount1 => CapAppliedDividend1;
        public decimal RecoverableAmount2 => CapAppliedDividend2;
        
        // ========== Lead time 추정 ==========
        
        [ObservableProperty]
        private int _leadTimeDays = 11;
        
        [ObservableProperty]
        private decimal _discountRate = 0.1m;
        
        [ObservableProperty]
        private DateTime? _appraisalDate;
        
        [ObservableProperty]
        private ObservableCollection<LeadTimeScheduleItem> _leadTimeSchedules = new();
        
        public AuctionScheduleDetailViewModel()
        {
            // 기본 생성자 (디자인 타임용)
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
        
        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                
                // Lead time 일정 생성
                GenerateLeadTimeSchedules();
                
                // 물건 정보가 있으면 로드
                if (PropertyId.HasValue)
                {
                    await LoadPropertyDataAsync();
                }
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
        
        /// <summary>
        /// 물건 데이터 로드
        /// </summary>
        private async Task LoadPropertyDataAsync()
        {
            if (!PropertyId.HasValue) return;
            
            try
            {
                // 물건 정보 로드
                if (_propertyRepository != null)
                {
                    var property = await _propertyRepository.GetByIdAsync(PropertyId.Value);
                    if (property != null)
                    {
                        AppraisalValue = property.AppraisalValue ?? 0;
                    }
                }
                
                // 평가 정보 로드
                if (_evaluationRepository != null)
                {
                    var eval = await _evaluationRepository.GetByPropertyIdAsync(PropertyId.Value);
                    if (eval != null)
                    {
                        // 평가 데이터에서 시나리오 값 가져오기
                        var details = eval.EvaluationDetails;
                        if (details != null)
                        {
                            // 시나리오1 낙찰가
                            if (details.Scenario1?.EvaluatedValue.HasValue == true)
                            {
                                Scenario1WinningBid = details.Scenario1.EvaluatedValue.Value;
                            }
                            // 시나리오2 낙찰가
                            if (details.Scenario2?.EvaluatedValue.HasValue == true)
                            {
                                Scenario2WinningBid = details.Scenario2.EvaluatedValue.Value;
                            }
                            // 감정가
                            if (details.AppraisalValue.HasValue && AppraisalValue == 0)
                            {
                                AppraisalValue = details.AppraisalValue.Value;
                            }
                            // 감정일
                            if (details.AppraisalDate.HasValue)
                            {
                                AppraisalDate = details.AppraisalDate.Value;
                            }
                        }
                    }
                }
                
                // 선순위 정보 로드
                if (_rightAnalysisRepository != null)
                {
                    var rightAnalysis = await _rightAnalysisRepository.GetByPropertyIdAsync(PropertyId.Value);
                    if (rightAnalysis != null)
                    {
                        SeniorDeduction1 = rightAnalysis.SeniorRightsTotal ?? 0;
                        SeniorDeduction2 = rightAnalysis.SeniorRightsTotal ?? 0;
                        LoanCap1 = rightAnalysis.LoanCap ?? 0;
                        LoanCap2 = rightAnalysis.LoanCap ?? 0;
                    }
                }
                
                // 기존 경(공)매 일정 로드
                if (_scheduleRepository != null)
                {
                    var schedules = await _scheduleRepository.GetByPropertyIdAsync(PropertyId.Value);
                    if (schedules.Count > 0)
                    {
                        var schedule = schedules.First();
                        IsAuction = schedule.ScheduleType == "auction";
                        RecoveryDate1 = schedule.AuctionDate;
                        if (schedule.MinimumBid.HasValue && Scenario1WinningBid == 0)
                        {
                            Scenario1WinningBid = schedule.MinimumBid.Value;
                        }
                        if (schedule.SalePrice.HasValue && Scenario2WinningBid == 0)
                        {
                            Scenario2WinningBid = schedule.SalePrice.Value;
                        }
                    }
                }
                
                // 계산된 속성 업데이트 알림
                OnPropertyChanged(nameof(Scenario1BidRate));
                OnPropertyChanged(nameof(Scenario2BidRate));
                OnPropertyChanged(nameof(TotalCost1));
                OnPropertyChanged(nameof(TotalCost2));
                OnPropertyChanged(nameof(DistributableBeforeProration1));
                OnPropertyChanged(nameof(DistributableBeforeProration2));
                OnPropertyChanged(nameof(CapAppliedDividend1));
                OnPropertyChanged(nameof(CapAppliedDividend2));
                OnPropertyChanged(nameof(RecoverableAmount1));
                OnPropertyChanged(nameof(RecoverableAmount2));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"물건 데이터 로드 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Lead time 일정 생성
        /// </summary>
        private void GenerateLeadTimeSchedules()
        {
            LeadTimeSchedules.Clear();
            
            var startDate = DateTime.Today;
            var currentBid = AppraisalValue > 0 ? AppraisalValue : 1000000000m; // 기본값 10억
            
            for (int i = 1; i <= 22; i++)
            {
                var scheduleDate = startDate.AddDays((i - 1) * LeadTimeDays * 7 / 11); // 대략적인 간격
                var minimumBid = currentBid * (decimal)Math.Pow((double)(1 - DiscountRate), i - 1);
                
                LeadTimeSchedules.Add(new LeadTimeScheduleItem
                {
                    Round = i,
                    Date = scheduleDate,
                    MinimumBid = minimumBid
                });
            }
        }
        
        /// <summary>
        /// 저장
        /// </summary>
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
                
                // 기존 일정 확인
                var existingSchedules = await _scheduleRepository.GetByPropertyIdAsync(PropertyId.Value);
                
                if (existingSchedules.Count > 0)
                {
                    // 업데이트
                    var existing = existingSchedules.First();
                    existing.ScheduleType = IsAuction ? "auction" : "public_sale";
                    existing.AuctionDate = RecoveryDate1;
                    existing.MinimumBid = Scenario1WinningBid;
                    existing.SalePrice = Scenario2WinningBid;
                    
                    await _scheduleRepository.UpdateAsync(existing);
                }
                else
                {
                    // 새로 생성
                    var newSchedule = new AuctionSchedule
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = PropertyId.Value,
                        ScheduleType = IsAuction ? "auction" : "public_sale",
                        AuctionDate = RecoveryDate1,
                        MinimumBid = Scenario1WinningBid,
                        SalePrice = Scenario2WinningBid,
                        Status = "scheduled"
                    };
                    
                    await _scheduleRepository.CreateAsync(newSchedule);
                }
                
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경(공)매 일정이 저장되었습니다.");
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
        
        // ========== 속성 변경 시 계산 업데이트 ==========
        
        partial void OnScenario1WinningBidChanged(decimal value)
        {
            OnPropertyChanged(nameof(Scenario1BidRate));
            OnPropertyChanged(nameof(DistributableBeforeProration1));
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnScenario2WinningBidChanged(decimal value)
        {
            OnPropertyChanged(nameof(Scenario2BidRate));
            OnPropertyChanged(nameof(DistributableBeforeProration2));
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
        }
        
        partial void OnAppraisalValueChanged(decimal value)
        {
            OnPropertyChanged(nameof(Scenario1BidRate));
            OnPropertyChanged(nameof(Scenario2BidRate));
            GenerateLeadTimeSchedules();
        }
        
        partial void OnAuctionCost1Changed(decimal value)
        {
            OnPropertyChanged(nameof(TotalCost1));
            OnPropertyChanged(nameof(DistributableBeforeProration1));
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnAuctionCost2Changed(decimal value)
        {
            OnPropertyChanged(nameof(TotalCost2));
            OnPropertyChanged(nameof(DistributableBeforeProration2));
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
        }
        
        partial void OnAppraisalFee1Changed(decimal value)
        {
            OnPropertyChanged(nameof(TotalCost1));
            OnPropertyChanged(nameof(DistributableBeforeProration1));
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnAppraisalFee2Changed(decimal value)
        {
            OnPropertyChanged(nameof(TotalCost2));
            OnPropertyChanged(nameof(DistributableBeforeProration2));
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
        }
        
        partial void OnSeniorDeduction1Changed(decimal value)
        {
            OnPropertyChanged(nameof(DistributableBeforeProration1));
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnSeniorDeduction2Changed(decimal value)
        {
            OnPropertyChanged(nameof(DistributableBeforeProration2));
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
        }
        
        partial void OnDisposalFee1Changed(decimal value)
        {
            OnPropertyChanged(nameof(DistributableBeforeProration1));
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnDisposalFee2Changed(decimal value)
        {
            OnPropertyChanged(nameof(DistributableBeforeProration2));
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
        }
        
        partial void OnLoanCap1Changed(decimal value)
        {
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnLoanCap2Changed(decimal value)
        {
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
        }
        
        partial void OnMortgageCap1Changed(decimal value)
        {
            OnPropertyChanged(nameof(CapAppliedDividend1));
            OnPropertyChanged(nameof(RecoverableAmount1));
        }
        
        partial void OnMortgageCap2Changed(decimal value)
        {
            OnPropertyChanged(nameof(CapAppliedDividend2));
            OnPropertyChanged(nameof(RecoverableAmount2));
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
                // PropertyId가 변경되면 데이터 로드
                _ = LoadPropertyDataAsync();
            }
        }
        
        /// <summary>
        /// 물건 ID 설정 (외부에서 호출)
        /// </summary>
        public void SetPropertyId(Guid propertyId)
        {
            PropertyId = propertyId;
        }
    }
    
    /// <summary>
    /// Lead time 일정 항목
    /// </summary>
    public class LeadTimeScheduleItem
    {
        public int Round { get; set; }
        public DateTime Date { get; set; }
        public decimal MinimumBid { get; set; }
    }
}
