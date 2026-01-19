using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 경매 일정 Repository - 경매일정(Ⅶ) 엑셀 산출화면 기반
    /// </summary>
    public class AuctionScheduleRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public AuctionScheduleRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 전체 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 경매 일정 조회
        /// </summary>
        public async Task<AuctionSchedule?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.Id == id)
                    .Get();

                return response.Models.FirstOrDefault() is { } t ? MapToAuctionSchedule(t) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 상태별 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetByStatusAsync(string status)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.Status == status)
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 기간별 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.AuctionDate >= startDate)
                    .Where(x => x.AuctionDate <= endDate)
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경매 일정 생성
        /// </summary>
        public async Task<AuctionSchedule> CreateAsync(AuctionSchedule schedule)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAuctionScheduleTable(schedule);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AuctionScheduleTable>().Insert(table);
                return MapToAuctionSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경매 일정 수정
        /// </summary>
        public async Task<AuctionSchedule> UpdateAsync(AuctionSchedule schedule)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAuctionScheduleTable(schedule);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AuctionScheduleTable>()
                    .Where(x => x.Id == schedule.Id)
                    .Update(table);
                return MapToAuctionSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경매 일정 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<AuctionScheduleTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Mapper ==========

        private AuctionSchedule MapToAuctionSchedule(AuctionScheduleTable t) => new AuctionSchedule
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            ScheduleType = t.ScheduleType ?? "auction",
            AuctionNumber = t.AuctionNumber,
            AuctionDate = t.AuctionDate,
            BidDate = t.BidDate,
            MinimumBid = t.MinimumBid,
            SalePrice = t.SalePrice,
            Status = t.Status ?? "scheduled",
            InterimPrincipalOffset = t.InterimPrincipalOffset,
            InterimPrincipalRecovery = t.InterimPrincipalRecovery,
            InterimInterestOffset = t.InterimInterestOffset,
            InterimInterestRecovery = t.InterimInterestRecovery,
            CaseCaptureUrl = t.CaseCaptureUrl,
            ScheduleCaptureUrl = t.ScheduleCaptureUrl,
            DocumentCaptureUrl = t.DocumentCaptureUrl,
            // 평가결과
            Scenario1WinningBid = t.Scenario1WinningBid,
            Scenario2WinningBid = t.Scenario2WinningBid,
            Scenario1EvalReason = t.Scenario1EvalReason,
            Scenario2EvalReason = t.Scenario2EvalReason,
            // 구분별 감정평가금액
            LandAppraisalValue = t.LandAppraisalValue,
            BuildingAppraisalValue = t.BuildingAppraisalValue,
            MachineryAppraisalValue = t.MachineryAppraisalValue,
            OtherBankSeniorValue = t.OtherBankSeniorValue,
            // 차주사항
            IsBorrowerDeceased = t.IsBorrowerDeceased,
            IsOwnerMovedIn = t.IsOwnerMovedIn,
            IsSessionOpened = t.IsSessionOpened,
            LoanType = t.LoanType,
            HasSubrogationRegistration = t.HasSubrogationRegistration,
            SubrogationEntity = t.SubrogationEntity,
            IsHousingMortgageLoan = t.IsHousingMortgageLoan,
            SubrogationCost = t.SubrogationCost,
            FinalCompletionDate = t.FinalCompletionDate,
            JurisdictionCourt = t.JurisdictionCourt,
            CourtDiscountRate = t.CourtDiscountRate,
            AuctionRequestType = t.AuctionRequestType ?? "*1",
            // 시나리오별 일정
            Scenario1AuctionRequestDate = t.Scenario1AuctionRequestDate,
            Scenario2AuctionRequestDate = t.Scenario2AuctionRequestDate,
            Scenario1ProcedureMonths = t.Scenario1ProcedureMonths,
            Scenario2ProcedureMonths = t.Scenario2ProcedureMonths,
            Scenario1FirstRoundMonths = t.Scenario1FirstRoundMonths,
            Scenario2FirstRoundMonths = t.Scenario2FirstRoundMonths,
            Scenario1LegalPrice = t.Scenario1LegalPrice,
            Scenario2LegalPrice = t.Scenario2LegalPrice,
            Scenario1ExpectedRound = t.Scenario1ExpectedRound,
            Scenario2ExpectedRound = t.Scenario2ExpectedRound,
            Scenario1MinBidRate = t.Scenario1MinBidRate,
            Scenario2MinBidRate = t.Scenario2MinBidRate,
            Scenario1MinLegalPrice = t.Scenario1MinLegalPrice,
            Scenario2MinLegalPrice = t.Scenario2MinLegalPrice,
            // 비용
            NewspaperAdFee = t.NewspaperAdFee,
            SurveyFee = t.SurveyFee,
            AuctionSaleFee = t.AuctionSaleFee,
            AppraisalFee = t.AppraisalFee,
            DeliveryFee = t.DeliveryFee,
            RegistrationEducationFee = t.RegistrationEducationFee,
            OtherCost = t.OtherCost,
            AdditionalCostRate = t.AdditionalCostRate,
            PropertyCount = t.PropertyCount,
            CreditorCount = t.CreditorCount,
            // 배당/회수
            Scenario1SeniorDeduction = t.Scenario1SeniorDeduction,
            Scenario2SeniorDeduction = t.Scenario2SeniorDeduction,
            Scenario1AuctionFees = t.Scenario1AuctionFees,
            Scenario2AuctionFees = t.Scenario2AuctionFees,
            Scenario1DistributableAfterSale = t.Scenario1DistributableAfterSale,
            Scenario2DistributableAfterSale = t.Scenario2DistributableAfterSale,
            Scenario1DistributableAfterSenior = t.Scenario1DistributableAfterSenior,
            Scenario2DistributableAfterSenior = t.Scenario2DistributableAfterSenior,
            Scenario1LoanCap = t.Scenario1LoanCap,
            Scenario2LoanCap = t.Scenario2LoanCap,
            Scenario1LoanCap2 = t.Scenario1LoanCap2,
            Scenario2LoanCap2 = t.Scenario2LoanCap2,
            Scenario1MortgageCap = t.Scenario1MortgageCap,
            Scenario2MortgageCap = t.Scenario2MortgageCap,
            Scenario1CapAppliedDividend = t.Scenario1CapAppliedDividend,
            Scenario2CapAppliedDividend = t.Scenario2CapAppliedDividend,
            Scenario1PrepaidFeeRecovery = t.Scenario1PrepaidFeeRecovery,
            Scenario2PrepaidFeeRecovery = t.Scenario2PrepaidFeeRecovery,
            Scenario1DividendRecoverable = t.Scenario1DividendRecoverable,
            Scenario2DividendRecoverable = t.Scenario2DividendRecoverable,
            // 예납금액 회수
            ActualPaidDeposit = t.ActualPaidDeposit,
            DepositRecoveryRate = t.DepositRecoveryRate,
            // 3자선행/중복경매
            ThirdPartyAuctionAmount = t.ThirdPartyAuctionAmount,
            DuplicateAuctionAmount = t.DuplicateAuctionAmount,
            ThirdPartyAuctionCostsJson = t.ThirdPartyAuctionCosts,
            DuplicateAuctionCostsJson = t.DuplicateAuctionCosts,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private AuctionScheduleTable MapToAuctionScheduleTable(AuctionSchedule a) => new AuctionScheduleTable
        {
            Id = a.Id,
            PropertyId = a.PropertyId,
            ScheduleType = a.ScheduleType,
            AuctionNumber = a.AuctionNumber,
            AuctionDate = a.AuctionDate,
            BidDate = a.BidDate,
            MinimumBid = a.MinimumBid,
            SalePrice = a.SalePrice,
            Status = a.Status,
            InterimPrincipalOffset = a.InterimPrincipalOffset,
            InterimPrincipalRecovery = a.InterimPrincipalRecovery,
            InterimInterestOffset = a.InterimInterestOffset,
            InterimInterestRecovery = a.InterimInterestRecovery,
            CaseCaptureUrl = a.CaseCaptureUrl,
            ScheduleCaptureUrl = a.ScheduleCaptureUrl,
            DocumentCaptureUrl = a.DocumentCaptureUrl,
            // 평가결과
            Scenario1WinningBid = a.Scenario1WinningBid,
            Scenario2WinningBid = a.Scenario2WinningBid,
            Scenario1EvalReason = a.Scenario1EvalReason,
            Scenario2EvalReason = a.Scenario2EvalReason,
            // 구분별 감정평가금액
            LandAppraisalValue = a.LandAppraisalValue,
            BuildingAppraisalValue = a.BuildingAppraisalValue,
            MachineryAppraisalValue = a.MachineryAppraisalValue,
            OtherBankSeniorValue = a.OtherBankSeniorValue,
            // 차주사항
            IsBorrowerDeceased = a.IsBorrowerDeceased,
            IsOwnerMovedIn = a.IsOwnerMovedIn,
            IsSessionOpened = a.IsSessionOpened,
            LoanType = a.LoanType,
            HasSubrogationRegistration = a.HasSubrogationRegistration,
            SubrogationEntity = a.SubrogationEntity,
            IsHousingMortgageLoan = a.IsHousingMortgageLoan,
            SubrogationCost = a.SubrogationCost,
            FinalCompletionDate = a.FinalCompletionDate,
            JurisdictionCourt = a.JurisdictionCourt,
            CourtDiscountRate = a.CourtDiscountRate,
            AuctionRequestType = a.AuctionRequestType,
            // 시나리오별 일정
            Scenario1AuctionRequestDate = a.Scenario1AuctionRequestDate,
            Scenario2AuctionRequestDate = a.Scenario2AuctionRequestDate,
            Scenario1ProcedureMonths = a.Scenario1ProcedureMonths,
            Scenario2ProcedureMonths = a.Scenario2ProcedureMonths,
            Scenario1FirstRoundMonths = a.Scenario1FirstRoundMonths,
            Scenario2FirstRoundMonths = a.Scenario2FirstRoundMonths,
            Scenario1LegalPrice = a.Scenario1LegalPrice,
            Scenario2LegalPrice = a.Scenario2LegalPrice,
            Scenario1ExpectedRound = a.Scenario1ExpectedRound,
            Scenario2ExpectedRound = a.Scenario2ExpectedRound,
            Scenario1MinBidRate = a.Scenario1MinBidRate,
            Scenario2MinBidRate = a.Scenario2MinBidRate,
            Scenario1MinLegalPrice = a.Scenario1MinLegalPrice,
            Scenario2MinLegalPrice = a.Scenario2MinLegalPrice,
            // 비용
            NewspaperAdFee = a.NewspaperAdFee,
            SurveyFee = a.SurveyFee,
            AuctionSaleFee = a.AuctionSaleFee,
            AppraisalFee = a.AppraisalFee,
            DeliveryFee = a.DeliveryFee,
            RegistrationEducationFee = a.RegistrationEducationFee,
            OtherCost = a.OtherCost,
            AdditionalCostRate = a.AdditionalCostRate,
            PropertyCount = a.PropertyCount,
            CreditorCount = a.CreditorCount,
            // 배당/회수
            Scenario1SeniorDeduction = a.Scenario1SeniorDeduction,
            Scenario2SeniorDeduction = a.Scenario2SeniorDeduction,
            Scenario1AuctionFees = a.Scenario1AuctionFees,
            Scenario2AuctionFees = a.Scenario2AuctionFees,
            Scenario1DistributableAfterSale = a.Scenario1DistributableAfterSale,
            Scenario2DistributableAfterSale = a.Scenario2DistributableAfterSale,
            Scenario1DistributableAfterSenior = a.Scenario1DistributableAfterSenior,
            Scenario2DistributableAfterSenior = a.Scenario2DistributableAfterSenior,
            Scenario1LoanCap = a.Scenario1LoanCap,
            Scenario2LoanCap = a.Scenario2LoanCap,
            Scenario1LoanCap2 = a.Scenario1LoanCap2,
            Scenario2LoanCap2 = a.Scenario2LoanCap2,
            Scenario1MortgageCap = a.Scenario1MortgageCap,
            Scenario2MortgageCap = a.Scenario2MortgageCap,
            Scenario1CapAppliedDividend = a.Scenario1CapAppliedDividend,
            Scenario2CapAppliedDividend = a.Scenario2CapAppliedDividend,
            Scenario1PrepaidFeeRecovery = a.Scenario1PrepaidFeeRecovery,
            Scenario2PrepaidFeeRecovery = a.Scenario2PrepaidFeeRecovery,
            Scenario1DividendRecoverable = a.Scenario1DividendRecoverable,
            Scenario2DividendRecoverable = a.Scenario2DividendRecoverable,
            // 예납금액 회수
            ActualPaidDeposit = a.ActualPaidDeposit,
            DepositRecoveryRate = a.DepositRecoveryRate,
            // 3자선행/중복경매
            ThirdPartyAuctionAmount = a.ThirdPartyAuctionAmount,
            DuplicateAuctionAmount = a.DuplicateAuctionAmount,
            ThirdPartyAuctionCosts = a.ThirdPartyAuctionCostsJson,
            DuplicateAuctionCosts = a.DuplicateAuctionCostsJson,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    // ========== Table Class ==========

    [Postgrest.Attributes.Table("auction_schedules")]
    internal class AuctionScheduleTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("property_id")] public Guid? PropertyId { get; set; }
        [Postgrest.Attributes.Column("schedule_type")] public string? ScheduleType { get; set; }
        [Postgrest.Attributes.Column("auction_number")] public string? AuctionNumber { get; set; }
        [Postgrest.Attributes.Column("auction_date")] public DateTime? AuctionDate { get; set; }
        [Postgrest.Attributes.Column("bid_date")] public DateTime? BidDate { get; set; }
        [Postgrest.Attributes.Column("minimum_bid")] public decimal? MinimumBid { get; set; }
        [Postgrest.Attributes.Column("sale_price")] public decimal? SalePrice { get; set; }
        [Postgrest.Attributes.Column("status")] public string? Status { get; set; }
        [Postgrest.Attributes.Column("interim_principal_offset")] public decimal InterimPrincipalOffset { get; set; }
        [Postgrest.Attributes.Column("interim_principal_recovery")] public decimal InterimPrincipalRecovery { get; set; }
        [Postgrest.Attributes.Column("interim_interest_offset")] public decimal InterimInterestOffset { get; set; }
        [Postgrest.Attributes.Column("interim_interest_recovery")] public decimal InterimInterestRecovery { get; set; }
        [Postgrest.Attributes.Column("case_capture_url")] public string? CaseCaptureUrl { get; set; }
        [Postgrest.Attributes.Column("schedule_capture_url")] public string? ScheduleCaptureUrl { get; set; }
        [Postgrest.Attributes.Column("document_capture_url")] public string? DocumentCaptureUrl { get; set; }
        
        // 평가결과
        [Postgrest.Attributes.Column("scenario1_winning_bid")] public decimal Scenario1WinningBid { get; set; }
        [Postgrest.Attributes.Column("scenario2_winning_bid")] public decimal Scenario2WinningBid { get; set; }
        [Postgrest.Attributes.Column("scenario1_eval_reason")] public string? Scenario1EvalReason { get; set; }
        [Postgrest.Attributes.Column("scenario2_eval_reason")] public string? Scenario2EvalReason { get; set; }
        
        // 구분별 감정평가금액
        [Postgrest.Attributes.Column("land_appraisal_value")] public decimal LandAppraisalValue { get; set; }
        [Postgrest.Attributes.Column("building_appraisal_value")] public decimal BuildingAppraisalValue { get; set; }
        [Postgrest.Attributes.Column("machinery_appraisal_value")] public decimal MachineryAppraisalValue { get; set; }
        [Postgrest.Attributes.Column("other_bank_senior_value")] public decimal OtherBankSeniorValue { get; set; }
        
        // 차주사항
        [Postgrest.Attributes.Column("is_borrower_deceased")] public bool IsBorrowerDeceased { get; set; }
        [Postgrest.Attributes.Column("is_owner_moved_in")] public bool IsOwnerMovedIn { get; set; }
        [Postgrest.Attributes.Column("is_session_opened")] public bool IsSessionOpened { get; set; }
        [Postgrest.Attributes.Column("loan_type")] public string? LoanType { get; set; }
        [Postgrest.Attributes.Column("has_subrogation_registration")] public bool HasSubrogationRegistration { get; set; }
        [Postgrest.Attributes.Column("subrogation_entity")] public string? SubrogationEntity { get; set; }
        [Postgrest.Attributes.Column("is_housing_mortgage_loan")] public bool IsHousingMortgageLoan { get; set; }
        [Postgrest.Attributes.Column("subrogation_cost")] public decimal SubrogationCost { get; set; }
        [Postgrest.Attributes.Column("final_completion_date")] public DateTime? FinalCompletionDate { get; set; }
        [Postgrest.Attributes.Column("jurisdiction_court")] public string? JurisdictionCourt { get; set; }
        [Postgrest.Attributes.Column("court_discount_rate")] public decimal CourtDiscountRate { get; set; }
        [Postgrest.Attributes.Column("auction_request_type")] public string? AuctionRequestType { get; set; }
        
        // 시나리오별 일정
        [Postgrest.Attributes.Column("scenario1_auction_request_date")] public DateTime? Scenario1AuctionRequestDate { get; set; }
        [Postgrest.Attributes.Column("scenario2_auction_request_date")] public DateTime? Scenario2AuctionRequestDate { get; set; }
        [Postgrest.Attributes.Column("scenario1_procedure_months")] public int Scenario1ProcedureMonths { get; set; }
        [Postgrest.Attributes.Column("scenario2_procedure_months")] public int Scenario2ProcedureMonths { get; set; }
        [Postgrest.Attributes.Column("scenario1_first_round_months")] public int Scenario1FirstRoundMonths { get; set; }
        [Postgrest.Attributes.Column("scenario2_first_round_months")] public int Scenario2FirstRoundMonths { get; set; }
        [Postgrest.Attributes.Column("scenario1_legal_price")] public decimal Scenario1LegalPrice { get; set; }
        [Postgrest.Attributes.Column("scenario2_legal_price")] public decimal Scenario2LegalPrice { get; set; }
        [Postgrest.Attributes.Column("scenario1_expected_round")] public int? Scenario1ExpectedRound { get; set; }
        [Postgrest.Attributes.Column("scenario2_expected_round")] public int? Scenario2ExpectedRound { get; set; }
        [Postgrest.Attributes.Column("scenario1_min_bid_rate")] public decimal Scenario1MinBidRate { get; set; }
        [Postgrest.Attributes.Column("scenario2_min_bid_rate")] public decimal Scenario2MinBidRate { get; set; }
        [Postgrest.Attributes.Column("scenario1_min_legal_price")] public decimal Scenario1MinLegalPrice { get; set; }
        [Postgrest.Attributes.Column("scenario2_min_legal_price")] public decimal Scenario2MinLegalPrice { get; set; }
        
        // 비용
        [Postgrest.Attributes.Column("newspaper_ad_fee")] public decimal NewspaperAdFee { get; set; }
        [Postgrest.Attributes.Column("survey_fee")] public decimal SurveyFee { get; set; }
        [Postgrest.Attributes.Column("auction_sale_fee")] public decimal AuctionSaleFee { get; set; }
        [Postgrest.Attributes.Column("appraisal_fee")] public decimal AppraisalFee { get; set; }
        [Postgrest.Attributes.Column("delivery_fee")] public decimal DeliveryFee { get; set; }
        [Postgrest.Attributes.Column("registration_education_fee")] public decimal RegistrationEducationFee { get; set; }
        [Postgrest.Attributes.Column("other_cost")] public decimal OtherCost { get; set; }
        [Postgrest.Attributes.Column("additional_cost_rate")] public decimal AdditionalCostRate { get; set; }
        [Postgrest.Attributes.Column("property_count")] public int PropertyCount { get; set; }
        [Postgrest.Attributes.Column("creditor_count")] public int CreditorCount { get; set; }
        
        // 배당/회수
        [Postgrest.Attributes.Column("scenario1_senior_deduction")] public decimal Scenario1SeniorDeduction { get; set; }
        [Postgrest.Attributes.Column("scenario2_senior_deduction")] public decimal Scenario2SeniorDeduction { get; set; }
        [Postgrest.Attributes.Column("scenario1_auction_fees")] public decimal Scenario1AuctionFees { get; set; }
        [Postgrest.Attributes.Column("scenario2_auction_fees")] public decimal Scenario2AuctionFees { get; set; }
        [Postgrest.Attributes.Column("scenario1_distributable_after_sale")] public decimal Scenario1DistributableAfterSale { get; set; }
        [Postgrest.Attributes.Column("scenario2_distributable_after_sale")] public decimal Scenario2DistributableAfterSale { get; set; }
        [Postgrest.Attributes.Column("scenario1_distributable_after_senior")] public decimal Scenario1DistributableAfterSenior { get; set; }
        [Postgrest.Attributes.Column("scenario2_distributable_after_senior")] public decimal Scenario2DistributableAfterSenior { get; set; }
        [Postgrest.Attributes.Column("scenario1_loan_cap")] public decimal Scenario1LoanCap { get; set; }
        [Postgrest.Attributes.Column("scenario2_loan_cap")] public decimal Scenario2LoanCap { get; set; }
        [Postgrest.Attributes.Column("scenario1_loan_cap_2")] public decimal Scenario1LoanCap2 { get; set; }
        [Postgrest.Attributes.Column("scenario2_loan_cap_2")] public decimal Scenario2LoanCap2 { get; set; }
        [Postgrest.Attributes.Column("scenario1_mortgage_cap")] public decimal Scenario1MortgageCap { get; set; }
        [Postgrest.Attributes.Column("scenario2_mortgage_cap")] public decimal Scenario2MortgageCap { get; set; }
        [Postgrest.Attributes.Column("scenario1_cap_applied_dividend")] public decimal Scenario1CapAppliedDividend { get; set; }
        [Postgrest.Attributes.Column("scenario2_cap_applied_dividend")] public decimal Scenario2CapAppliedDividend { get; set; }
        [Postgrest.Attributes.Column("scenario1_prepaid_fee_recovery")] public decimal Scenario1PrepaidFeeRecovery { get; set; }
        [Postgrest.Attributes.Column("scenario2_prepaid_fee_recovery")] public decimal Scenario2PrepaidFeeRecovery { get; set; }
        [Postgrest.Attributes.Column("scenario1_dividend_recoverable")] public decimal Scenario1DividendRecoverable { get; set; }
        [Postgrest.Attributes.Column("scenario2_dividend_recoverable")] public decimal Scenario2DividendRecoverable { get; set; }
        
        // 예납금액 회수
        [Postgrest.Attributes.Column("actual_paid_deposit")] public decimal ActualPaidDeposit { get; set; }
        [Postgrest.Attributes.Column("deposit_recovery_rate")] public decimal DepositRecoveryRate { get; set; }
        
        // 3자선행/중복경매
        [Postgrest.Attributes.Column("third_party_auction_amount")] public decimal ThirdPartyAuctionAmount { get; set; }
        [Postgrest.Attributes.Column("duplicate_auction_amount")] public decimal DuplicateAuctionAmount { get; set; }
        [Postgrest.Attributes.Column("third_party_auction_costs")] public string? ThirdPartyAuctionCosts { get; set; }
        [Postgrest.Attributes.Column("duplicate_auction_costs")] public string? DuplicateAuctionCosts { get; set; }
        
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
