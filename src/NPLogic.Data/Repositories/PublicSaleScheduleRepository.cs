using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 공매 일정 Repository - 공매일정(Ⅷ) 엑셀 산출화면 기반
    /// </summary>
    public class PublicSaleScheduleRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public PublicSaleScheduleRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        public async Task<List<PublicSaleSchedule>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PublicSaleScheduleTable>()
                    .Order(x => x.SaleDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToPublicSaleSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 공매 일정 조회
        /// </summary>
        public async Task<PublicSaleSchedule?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PublicSaleScheduleTable>()
                    .Where(x => x.Id == id)
                    .Get();

                return response.Models.FirstOrDefault() is { } t ? MapToPublicSaleSchedule(t) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 공매 일정 조회
        /// </summary>
        public async Task<List<PublicSaleSchedule>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PublicSaleScheduleTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.SaleDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToPublicSaleSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<PublicSaleSchedule> CreateAsync(PublicSaleSchedule schedule)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToPublicSaleScheduleTable(schedule);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<PublicSaleScheduleTable>().Insert(table);
                return MapToPublicSaleSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<PublicSaleSchedule> UpdateAsync(PublicSaleSchedule schedule)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToPublicSaleScheduleTable(schedule);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<PublicSaleScheduleTable>()
                    .Where(x => x.Id == schedule.Id)
                    .Update(table);
                return MapToPublicSaleSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<PublicSaleScheduleTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 삭제 실패: {ex.Message}", ex);
            }
        }

        private PublicSaleSchedule MapToPublicSaleSchedule(PublicSaleScheduleTable t) => new PublicSaleSchedule
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            SaleNumber = t.SaleNumber,
            SaleDate = t.SaleDate,
            MinimumBid = t.MinimumBid,
            SalePrice = t.SalePrice,
            Status = t.Status ?? "scheduled",
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
            TotalAppraisalValue = t.TotalAppraisalValue,
            // 공매일정 상세
            Scenario1StartDate = t.Scenario1StartDate,
            Scenario2StartDate = t.Scenario2StartDate,
            Scenario1BeneficiaryChangeCost = t.Scenario1BeneficiaryChangeCost,
            Scenario2BeneficiaryChangeCost = t.Scenario2BeneficiaryChangeCost,
            Scenario1EstimatedWinningBid = t.Scenario1EstimatedWinningBid,
            Scenario2EstimatedWinningBid = t.Scenario2EstimatedWinningBid,
            Scenario1SeniorDeduction = t.Scenario1SeniorDeduction,
            Scenario2SeniorDeduction = t.Scenario2SeniorDeduction,
            Scenario1SaleCost = t.Scenario1SaleCost,
            Scenario2SaleCost = t.Scenario2SaleCost,
            Scenario1DisposalFee = t.Scenario1DisposalFee,
            Scenario2DisposalFee = t.Scenario2DisposalFee,
            Scenario1DistributableBefore = t.Scenario1DistributableBefore,
            Scenario2DistributableBefore = t.Scenario2DistributableBefore,
            Scenario1CreditorDistribution = t.Scenario1CreditorDistribution,
            Scenario2CreditorDistribution = t.Scenario2CreditorDistribution,
            Scenario1DistributableAfter = t.Scenario1DistributableAfter,
            Scenario2DistributableAfter = t.Scenario2DistributableAfter,
            Scenario1LoanCap = t.Scenario1LoanCap,
            Scenario2LoanCap = t.Scenario2LoanCap,
            Scenario1LoanCap2 = t.Scenario1LoanCap2,
            Scenario2LoanCap2 = t.Scenario2LoanCap2,
            Scenario1MortgageCap = t.Scenario1MortgageCap,
            Scenario2MortgageCap = t.Scenario2MortgageCap,
            Scenario1CapAppliedDividend = t.Scenario1CapAppliedDividend,
            Scenario2CapAppliedDividend = t.Scenario2CapAppliedDividend,
            Scenario1RecoverableAmount = t.Scenario1RecoverableAmount,
            Scenario2RecoverableAmount = t.Scenario2RecoverableAmount,
            // 공매비용
            OnbidFee = t.OnbidFee,
            AppraisalFee = t.AppraisalFee,
            TotalSaleCost = t.TotalSaleCost,
            // 환가처분보수
            ExpectedSalePrice = t.ExpectedSalePrice,
            DisposalFeeAmount = t.DisposalFeeAmount,
            // 타채권자 배분
            CreditorDistributionsJson = t.CreditorDistributions,
            // Lead time 설정
            LeadTimeDays = t.LeadTimeDays,
            DiscountRate = t.DiscountRate,
            InitialAppraisalValue = t.InitialAppraisalValue,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private PublicSaleScheduleTable MapToPublicSaleScheduleTable(PublicSaleSchedule s) => new PublicSaleScheduleTable
        {
            Id = s.Id,
            PropertyId = s.PropertyId,
            SaleNumber = s.SaleNumber,
            SaleDate = s.SaleDate,
            MinimumBid = s.MinimumBid,
            SalePrice = s.SalePrice,
            Status = s.Status,
            // 평가결과
            Scenario1WinningBid = s.Scenario1WinningBid,
            Scenario2WinningBid = s.Scenario2WinningBid,
            Scenario1EvalReason = s.Scenario1EvalReason,
            Scenario2EvalReason = s.Scenario2EvalReason,
            // 구분별 감정평가금액
            LandAppraisalValue = s.LandAppraisalValue,
            BuildingAppraisalValue = s.BuildingAppraisalValue,
            MachineryAppraisalValue = s.MachineryAppraisalValue,
            OtherBankSeniorValue = s.OtherBankSeniorValue,
            TotalAppraisalValue = s.TotalAppraisalValue,
            // 공매일정 상세
            Scenario1StartDate = s.Scenario1StartDate,
            Scenario2StartDate = s.Scenario2StartDate,
            Scenario1BeneficiaryChangeCost = s.Scenario1BeneficiaryChangeCost,
            Scenario2BeneficiaryChangeCost = s.Scenario2BeneficiaryChangeCost,
            Scenario1EstimatedWinningBid = s.Scenario1EstimatedWinningBid,
            Scenario2EstimatedWinningBid = s.Scenario2EstimatedWinningBid,
            Scenario1SeniorDeduction = s.Scenario1SeniorDeduction,
            Scenario2SeniorDeduction = s.Scenario2SeniorDeduction,
            Scenario1SaleCost = s.Scenario1SaleCost,
            Scenario2SaleCost = s.Scenario2SaleCost,
            Scenario1DisposalFee = s.Scenario1DisposalFee,
            Scenario2DisposalFee = s.Scenario2DisposalFee,
            Scenario1DistributableBefore = s.Scenario1DistributableBefore,
            Scenario2DistributableBefore = s.Scenario2DistributableBefore,
            Scenario1CreditorDistribution = s.Scenario1CreditorDistribution,
            Scenario2CreditorDistribution = s.Scenario2CreditorDistribution,
            Scenario1DistributableAfter = s.Scenario1DistributableAfter,
            Scenario2DistributableAfter = s.Scenario2DistributableAfter,
            Scenario1LoanCap = s.Scenario1LoanCap,
            Scenario2LoanCap = s.Scenario2LoanCap,
            Scenario1LoanCap2 = s.Scenario1LoanCap2,
            Scenario2LoanCap2 = s.Scenario2LoanCap2,
            Scenario1MortgageCap = s.Scenario1MortgageCap,
            Scenario2MortgageCap = s.Scenario2MortgageCap,
            Scenario1CapAppliedDividend = s.Scenario1CapAppliedDividend,
            Scenario2CapAppliedDividend = s.Scenario2CapAppliedDividend,
            Scenario1RecoverableAmount = s.Scenario1RecoverableAmount,
            Scenario2RecoverableAmount = s.Scenario2RecoverableAmount,
            // 공매비용
            OnbidFee = s.OnbidFee,
            AppraisalFee = s.AppraisalFee,
            TotalSaleCost = s.TotalSaleCost,
            // 환가처분보수
            ExpectedSalePrice = s.ExpectedSalePrice,
            DisposalFeeAmount = s.DisposalFeeAmount,
            // 타채권자 배분
            CreditorDistributions = s.CreditorDistributionsJson,
            // Lead time 설정
            LeadTimeDays = s.LeadTimeDays,
            DiscountRate = s.DiscountRate,
            InitialAppraisalValue = s.InitialAppraisalValue,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    [Postgrest.Attributes.Table("public_sale_schedules")]
    internal class PublicSaleScheduleTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("property_id")] public Guid? PropertyId { get; set; }
        [Postgrest.Attributes.Column("sale_number")] public string? SaleNumber { get; set; }
        [Postgrest.Attributes.Column("sale_date")] public DateTime? SaleDate { get; set; }
        [Postgrest.Attributes.Column("minimum_bid")] public decimal? MinimumBid { get; set; }
        [Postgrest.Attributes.Column("sale_price")] public decimal? SalePrice { get; set; }
        [Postgrest.Attributes.Column("status")] public string? Status { get; set; }
        
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
        [Postgrest.Attributes.Column("total_appraisal_value")] public decimal TotalAppraisalValue { get; set; }
        
        // 공매일정 상세
        [Postgrest.Attributes.Column("scenario1_start_date")] public DateTime? Scenario1StartDate { get; set; }
        [Postgrest.Attributes.Column("scenario2_start_date")] public DateTime? Scenario2StartDate { get; set; }
        [Postgrest.Attributes.Column("scenario1_beneficiary_change_cost")] public decimal Scenario1BeneficiaryChangeCost { get; set; }
        [Postgrest.Attributes.Column("scenario2_beneficiary_change_cost")] public decimal Scenario2BeneficiaryChangeCost { get; set; }
        [Postgrest.Attributes.Column("scenario1_estimated_winning_bid")] public decimal Scenario1EstimatedWinningBid { get; set; }
        [Postgrest.Attributes.Column("scenario2_estimated_winning_bid")] public decimal Scenario2EstimatedWinningBid { get; set; }
        [Postgrest.Attributes.Column("scenario1_senior_deduction")] public decimal Scenario1SeniorDeduction { get; set; }
        [Postgrest.Attributes.Column("scenario2_senior_deduction")] public decimal Scenario2SeniorDeduction { get; set; }
        [Postgrest.Attributes.Column("scenario1_sale_cost")] public decimal Scenario1SaleCost { get; set; }
        [Postgrest.Attributes.Column("scenario2_sale_cost")] public decimal Scenario2SaleCost { get; set; }
        [Postgrest.Attributes.Column("scenario1_disposal_fee")] public decimal Scenario1DisposalFee { get; set; }
        [Postgrest.Attributes.Column("scenario2_disposal_fee")] public decimal Scenario2DisposalFee { get; set; }
        [Postgrest.Attributes.Column("scenario1_distributable_before")] public decimal Scenario1DistributableBefore { get; set; }
        [Postgrest.Attributes.Column("scenario2_distributable_before")] public decimal Scenario2DistributableBefore { get; set; }
        [Postgrest.Attributes.Column("scenario1_creditor_distribution")] public decimal Scenario1CreditorDistribution { get; set; }
        [Postgrest.Attributes.Column("scenario2_creditor_distribution")] public decimal Scenario2CreditorDistribution { get; set; }
        [Postgrest.Attributes.Column("scenario1_distributable_after")] public decimal Scenario1DistributableAfter { get; set; }
        [Postgrest.Attributes.Column("scenario2_distributable_after")] public decimal Scenario2DistributableAfter { get; set; }
        [Postgrest.Attributes.Column("scenario1_loan_cap")] public decimal Scenario1LoanCap { get; set; }
        [Postgrest.Attributes.Column("scenario2_loan_cap")] public decimal Scenario2LoanCap { get; set; }
        [Postgrest.Attributes.Column("scenario1_loan_cap_2")] public decimal Scenario1LoanCap2 { get; set; }
        [Postgrest.Attributes.Column("scenario2_loan_cap_2")] public decimal Scenario2LoanCap2 { get; set; }
        [Postgrest.Attributes.Column("scenario1_mortgage_cap")] public decimal Scenario1MortgageCap { get; set; }
        [Postgrest.Attributes.Column("scenario2_mortgage_cap")] public decimal Scenario2MortgageCap { get; set; }
        [Postgrest.Attributes.Column("scenario1_cap_applied_dividend")] public decimal Scenario1CapAppliedDividend { get; set; }
        [Postgrest.Attributes.Column("scenario2_cap_applied_dividend")] public decimal Scenario2CapAppliedDividend { get; set; }
        [Postgrest.Attributes.Column("scenario1_recoverable_amount")] public decimal Scenario1RecoverableAmount { get; set; }
        [Postgrest.Attributes.Column("scenario2_recoverable_amount")] public decimal Scenario2RecoverableAmount { get; set; }
        
        // 공매비용
        [Postgrest.Attributes.Column("onbid_fee")] public decimal OnbidFee { get; set; }
        [Postgrest.Attributes.Column("appraisal_fee")] public decimal AppraisalFee { get; set; }
        [Postgrest.Attributes.Column("total_sale_cost")] public decimal TotalSaleCost { get; set; }
        
        // 환가처분보수
        [Postgrest.Attributes.Column("expected_sale_price")] public decimal ExpectedSalePrice { get; set; }
        [Postgrest.Attributes.Column("disposal_fee_amount")] public decimal DisposalFeeAmount { get; set; }
        
        // 타채권자 배분
        [Postgrest.Attributes.Column("creditor_distributions")] public string? CreditorDistributions { get; set; }
        
        // Lead time 설정
        [Postgrest.Attributes.Column("lead_time_days")] public int LeadTimeDays { get; set; }
        [Postgrest.Attributes.Column("discount_rate")] public decimal DiscountRate { get; set; }
        [Postgrest.Attributes.Column("initial_appraisal_value")] public decimal InitialAppraisalValue { get; set; }
        
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
