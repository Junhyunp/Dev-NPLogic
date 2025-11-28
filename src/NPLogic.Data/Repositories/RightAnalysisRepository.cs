using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Services;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 권리분석 Repository
    /// </summary>
    public class RightAnalysisRepository
    {
        private readonly SupabaseService _supabaseService;

        public RightAnalysisRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 물건별 권리분석 조회
        /// </summary>
        public async Task<RightAnalysis?> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RightAnalysisTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Single();

                return response != null ? MapToRightAnalysis(response) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"권리분석 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 권리분석 조회
        /// </summary>
        public async Task<RightAnalysis?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RightAnalysisTable>()
                    .Where(x => x.Id == id)
                    .Single();

                return response != null ? MapToRightAnalysis(response) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"권리분석 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리분석 생성
        /// </summary>
        public async Task<RightAnalysis> CreateAsync(RightAnalysis analysis)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRightAnalysisTable(analysis);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RightAnalysisTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("권리분석 생성 후 데이터 조회 실패");

                return MapToRightAnalysis(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"권리분석 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리분석 수정
        /// </summary>
        public async Task<RightAnalysis> UpdateAsync(RightAnalysis analysis)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRightAnalysisTable(analysis);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RightAnalysisTable>()
                    .Where(x => x.Id == analysis.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("권리분석 수정 후 데이터 조회 실패");

                return MapToRightAnalysis(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"권리분석 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리분석 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RightAnalysisTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"권리분석 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리분석 생성 또는 수정 (Upsert)
        /// </summary>
        public async Task<RightAnalysis> UpsertAsync(RightAnalysis analysis)
        {
            var existing = await GetByPropertyIdAsync(analysis.PropertyId ?? Guid.Empty);
            if (existing != null)
            {
                analysis.Id = existing.Id;
                return await UpdateAsync(analysis);
            }
            return await CreateAsync(analysis);
        }

        /// <summary>
        /// 미완료 권리분석 목록 조회
        /// </summary>
        public async Task<List<RightAnalysis>> GetPendingAnalysisAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RightAnalysisTable>()
                    .Where(x => x.IsCompleted == false)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToRightAnalysis).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"미완료 권리분석 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 위험도별 권리분석 목록 조회
        /// </summary>
        public async Task<List<RightAnalysis>> GetByRiskLevelAsync(string riskLevel)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RightAnalysisTable>()
                    .Where(x => x.RiskLevel == riskLevel)
                    .Order(x => x.UpdatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToRightAnalysis).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"위험도별 권리분석 조회 실패: {ex.Message}", ex);
            }
        }

        #region Mapping Methods

        private RightAnalysis MapToRightAnalysis(RightAnalysisTable table)
        {
            return new RightAnalysis
            {
                Id = table.Id,
                PropertyId = table.PropertyId,

                // 경매사건 정보
                AuctionStatus = table.AuctionStatus,
                PrecedentAuction = table.PrecedentAuction,
                SubsequentAuction = table.SubsequentAuction,
                CourtName = table.CourtName,
                CaseNumber = table.CaseNumber,
                AuctionApplicant = table.AuctionApplicant,
                AuctionStartDate = table.AuctionStartDate,
                ClaimDeadlineDate = table.ClaimDeadlineDate,
                ClaimAmount = table.ClaimAmount,
                InitialAppraisalValue = table.InitialAppraisalValue,
                InitialAuctionDate = table.InitialAuctionDate,
                FinalAuctionRound = table.FinalAuctionRound,
                FinalAuctionDate = table.FinalAuctionDate,
                FinalAuctionResult = table.FinalAuctionResult,
                WinningBidAmount = table.WinningBidAmount,
                NextAuctionDate = table.NextAuctionDate,
                NextMinimumBid = table.NextMinimumBid,
                ClaimDeadlinePassed = table.ClaimDeadlinePassed,

                // 전입/임차 현황
                AddressMatch = table.AddressMatch,
                OwnerRegistered = table.OwnerRegistered,
                TenantName = table.TenantName,
                TenantMoveInDate = table.TenantMoveInDate,
                HousingOfficialPrice = table.HousingOfficialPrice,
                HasAuctionDocs = table.HasAuctionDocs,
                HasTenantRegistry = table.HasTenantRegistry,
                HasCommercialLease = table.HasCommercialLease,
                HasTenant = table.HasTenant,
                TenantClaimSubmitted = table.TenantClaimSubmitted,
                TenantDateBeforeMortgage = table.TenantDateBeforeMortgage,
                SurveyReportSubmitted = table.SurveyReportSubmitted,
                SurveyReportDate = table.SurveyReportDate,
                DebtorType = table.DebtorType,
                HasWageClaim = table.HasWageClaim,
                WageClaimSubmitted = table.WageClaimSubmitted,
                WageClaimEstimatedSeizure = table.WageClaimEstimatedSeizure,
                HasTaxClaim = table.HasTaxClaim,
                HasSeniorTaxClaim = table.HasSeniorTaxClaim,

                // 선순위 분석
                SeniorMortgageDd = table.SeniorMortgageDd,
                SeniorMortgageReflected = table.SeniorMortgageReflected,
                SeniorMortgageReason = table.SeniorMortgageReason,
                LienDd = table.LienDd,
                LienReflected = table.LienReflected,
                LienReason = table.LienReason,
                SmallDepositDd = table.SmallDepositDd,
                SmallDepositReflected = table.SmallDepositReflected,
                SmallDepositReason = table.SmallDepositReason,
                SmallDepositCase = table.SmallDepositCase,
                LeaseDepositDd = table.LeaseDepositDd,
                LeaseDepositReflected = table.LeaseDepositReflected,
                LeaseDepositReason = table.LeaseDepositReason,
                WageClaimDd = table.WageClaimDd,
                WageClaimReflected = table.WageClaimReflected,
                WageClaimReason = table.WageClaimReason,
                CurrentTaxDd = table.CurrentTaxDd,
                CurrentTaxReflected = table.CurrentTaxReflected,
                CurrentTaxReason = table.CurrentTaxReason,
                SeniorTaxDd = table.SeniorTaxDd,
                SeniorTaxReflected = table.SeniorTaxReflected,
                SeniorTaxReason = table.SeniorTaxReason,

                // 배당 시뮬레이션
                SeniorRightsTotal = table.SeniorRightsTotal,
                MortgageCount = table.MortgageCount,
                SeizureCount = table.SeizureCount,
                ExpectedWinningBid = table.ExpectedWinningBid,
                AuctionFees = table.AuctionFees,
                DistributableAmount = table.DistributableAmount,
                AmountAfterSenior = table.AmountAfterSenior,
                LoanCap = table.LoanCap,
                CapAppliedDividend = table.CapAppliedDividend,
                RecoveryAmount = table.RecoveryAmount,
                RecoveryRate = table.RecoveryRate,

                // 위험도 평가
                RiskLevel = table.RiskLevel,
                RiskReason = table.RiskReason,
                Recommendations = table.Recommendations,
                DistributionAnalysis = table.DistributionAnalysis,

                // 메타데이터
                AnalyzedBy = table.AnalyzedBy,
                AnalyzedAt = table.AnalyzedAt,
                IsCompleted = table.IsCompleted,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RightAnalysisTable MapToRightAnalysisTable(RightAnalysis model)
        {
            return new RightAnalysisTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,

                // 경매사건 정보
                AuctionStatus = model.AuctionStatus,
                PrecedentAuction = model.PrecedentAuction,
                SubsequentAuction = model.SubsequentAuction,
                CourtName = model.CourtName,
                CaseNumber = model.CaseNumber,
                AuctionApplicant = model.AuctionApplicant,
                AuctionStartDate = model.AuctionStartDate,
                ClaimDeadlineDate = model.ClaimDeadlineDate,
                ClaimAmount = model.ClaimAmount,
                InitialAppraisalValue = model.InitialAppraisalValue,
                InitialAuctionDate = model.InitialAuctionDate,
                FinalAuctionRound = model.FinalAuctionRound,
                FinalAuctionDate = model.FinalAuctionDate,
                FinalAuctionResult = model.FinalAuctionResult,
                WinningBidAmount = model.WinningBidAmount,
                NextAuctionDate = model.NextAuctionDate,
                NextMinimumBid = model.NextMinimumBid,
                ClaimDeadlinePassed = model.ClaimDeadlinePassed,

                // 전입/임차 현황
                AddressMatch = model.AddressMatch,
                OwnerRegistered = model.OwnerRegistered,
                TenantName = model.TenantName,
                TenantMoveInDate = model.TenantMoveInDate,
                HousingOfficialPrice = model.HousingOfficialPrice,
                HasAuctionDocs = model.HasAuctionDocs,
                HasTenantRegistry = model.HasTenantRegistry,
                HasCommercialLease = model.HasCommercialLease,
                HasTenant = model.HasTenant,
                TenantClaimSubmitted = model.TenantClaimSubmitted,
                TenantDateBeforeMortgage = model.TenantDateBeforeMortgage,
                SurveyReportSubmitted = model.SurveyReportSubmitted,
                SurveyReportDate = model.SurveyReportDate,
                DebtorType = model.DebtorType,
                HasWageClaim = model.HasWageClaim,
                WageClaimSubmitted = model.WageClaimSubmitted,
                WageClaimEstimatedSeizure = model.WageClaimEstimatedSeizure,
                HasTaxClaim = model.HasTaxClaim,
                HasSeniorTaxClaim = model.HasSeniorTaxClaim,

                // 선순위 분석
                SeniorMortgageDd = model.SeniorMortgageDd,
                SeniorMortgageReflected = model.SeniorMortgageReflected,
                SeniorMortgageReason = model.SeniorMortgageReason,
                LienDd = model.LienDd,
                LienReflected = model.LienReflected,
                LienReason = model.LienReason,
                SmallDepositDd = model.SmallDepositDd,
                SmallDepositReflected = model.SmallDepositReflected,
                SmallDepositReason = model.SmallDepositReason,
                SmallDepositCase = model.SmallDepositCase,
                LeaseDepositDd = model.LeaseDepositDd,
                LeaseDepositReflected = model.LeaseDepositReflected,
                LeaseDepositReason = model.LeaseDepositReason,
                WageClaimDd = model.WageClaimDd,
                WageClaimReflected = model.WageClaimReflected,
                WageClaimReason = model.WageClaimReason,
                CurrentTaxDd = model.CurrentTaxDd,
                CurrentTaxReflected = model.CurrentTaxReflected,
                CurrentTaxReason = model.CurrentTaxReason,
                SeniorTaxDd = model.SeniorTaxDd,
                SeniorTaxReflected = model.SeniorTaxReflected,
                SeniorTaxReason = model.SeniorTaxReason,

                // 배당 시뮬레이션
                SeniorRightsTotal = model.SeniorRightsTotal,
                MortgageCount = model.MortgageCount,
                SeizureCount = model.SeizureCount,
                ExpectedWinningBid = model.ExpectedWinningBid,
                AuctionFees = model.AuctionFees,
                DistributableAmount = model.DistributableAmount,
                AmountAfterSenior = model.AmountAfterSenior,
                LoanCap = model.LoanCap,
                CapAppliedDividend = model.CapAppliedDividend,
                RecoveryAmount = model.RecoveryAmount,
                RecoveryRate = model.RecoveryRate,

                // 위험도 평가
                RiskLevel = model.RiskLevel,
                RiskReason = model.RiskReason,
                Recommendations = model.Recommendations,
                DistributionAnalysis = model.DistributionAnalysis,

                // 메타데이터
                AnalyzedBy = model.AnalyzedBy,
                AnalyzedAt = model.AnalyzedAt,
                IsCompleted = model.IsCompleted,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        #endregion
    }

    #region Supabase Table Model

    /// <summary>
    /// Supabase right_analysis 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("right_analysis")]
    internal class RightAnalysisTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        // 경매사건 정보
        [Postgrest.Attributes.Column("auction_status")]
        public string? AuctionStatus { get; set; }

        [Postgrest.Attributes.Column("precedent_auction")]
        public bool PrecedentAuction { get; set; }

        [Postgrest.Attributes.Column("subsequent_auction")]
        public bool SubsequentAuction { get; set; }

        [Postgrest.Attributes.Column("court_name")]
        public string? CourtName { get; set; }

        [Postgrest.Attributes.Column("case_number")]
        public string? CaseNumber { get; set; }

        [Postgrest.Attributes.Column("auction_applicant")]
        public string? AuctionApplicant { get; set; }

        [Postgrest.Attributes.Column("auction_start_date")]
        public DateTime? AuctionStartDate { get; set; }

        [Postgrest.Attributes.Column("claim_deadline_date")]
        public DateTime? ClaimDeadlineDate { get; set; }

        [Postgrest.Attributes.Column("claim_amount")]
        public decimal? ClaimAmount { get; set; }

        [Postgrest.Attributes.Column("initial_appraisal_value")]
        public decimal? InitialAppraisalValue { get; set; }

        [Postgrest.Attributes.Column("initial_auction_date")]
        public DateTime? InitialAuctionDate { get; set; }

        [Postgrest.Attributes.Column("final_auction_round")]
        public int? FinalAuctionRound { get; set; }

        [Postgrest.Attributes.Column("final_auction_date")]
        public DateTime? FinalAuctionDate { get; set; }

        [Postgrest.Attributes.Column("final_auction_result")]
        public string? FinalAuctionResult { get; set; }

        [Postgrest.Attributes.Column("winning_bid_amount")]
        public decimal? WinningBidAmount { get; set; }

        [Postgrest.Attributes.Column("next_auction_date")]
        public DateTime? NextAuctionDate { get; set; }

        [Postgrest.Attributes.Column("next_minimum_bid")]
        public decimal? NextMinimumBid { get; set; }

        [Postgrest.Attributes.Column("claim_deadline_passed")]
        public bool ClaimDeadlinePassed { get; set; }

        // 전입/임차 현황
        [Postgrest.Attributes.Column("address_match")]
        public bool? AddressMatch { get; set; }

        [Postgrest.Attributes.Column("owner_registered")]
        public bool? OwnerRegistered { get; set; }

        [Postgrest.Attributes.Column("tenant_name")]
        public string? TenantName { get; set; }

        [Postgrest.Attributes.Column("tenant_move_in_date")]
        public DateTime? TenantMoveInDate { get; set; }

        [Postgrest.Attributes.Column("housing_official_price")]
        public decimal? HousingOfficialPrice { get; set; }

        [Postgrest.Attributes.Column("has_auction_docs")]
        public bool HasAuctionDocs { get; set; }

        [Postgrest.Attributes.Column("has_tenant_registry")]
        public bool HasTenantRegistry { get; set; }

        [Postgrest.Attributes.Column("has_commercial_lease")]
        public bool HasCommercialLease { get; set; }

        [Postgrest.Attributes.Column("has_tenant")]
        public bool? HasTenant { get; set; }

        [Postgrest.Attributes.Column("tenant_claim_submitted")]
        public bool? TenantClaimSubmitted { get; set; }

        [Postgrest.Attributes.Column("tenant_date_before_mortgage")]
        public bool? TenantDateBeforeMortgage { get; set; }

        [Postgrest.Attributes.Column("survey_report_submitted")]
        public bool? SurveyReportSubmitted { get; set; }

        [Postgrest.Attributes.Column("survey_report_date")]
        public DateTime? SurveyReportDate { get; set; }

        [Postgrest.Attributes.Column("debtor_type")]
        public string? DebtorType { get; set; }

        [Postgrest.Attributes.Column("has_wage_claim")]
        public bool HasWageClaim { get; set; }

        [Postgrest.Attributes.Column("wage_claim_submitted")]
        public bool WageClaimSubmitted { get; set; }

        [Postgrest.Attributes.Column("wage_claim_estimated_seizure")]
        public bool WageClaimEstimatedSeizure { get; set; }

        [Postgrest.Attributes.Column("has_tax_claim")]
        public bool HasTaxClaim { get; set; }

        [Postgrest.Attributes.Column("has_senior_tax_claim")]
        public bool HasSeniorTaxClaim { get; set; }

        // 선순위 분석
        [Postgrest.Attributes.Column("senior_mortgage_dd")]
        public decimal SeniorMortgageDd { get; set; }

        [Postgrest.Attributes.Column("senior_mortgage_reflected")]
        public decimal SeniorMortgageReflected { get; set; }

        [Postgrest.Attributes.Column("senior_mortgage_reason")]
        public string? SeniorMortgageReason { get; set; }

        [Postgrest.Attributes.Column("lien_dd")]
        public decimal LienDd { get; set; }

        [Postgrest.Attributes.Column("lien_reflected")]
        public decimal LienReflected { get; set; }

        [Postgrest.Attributes.Column("lien_reason")]
        public string? LienReason { get; set; }

        [Postgrest.Attributes.Column("small_deposit_dd")]
        public decimal SmallDepositDd { get; set; }

        [Postgrest.Attributes.Column("small_deposit_reflected")]
        public decimal SmallDepositReflected { get; set; }

        [Postgrest.Attributes.Column("small_deposit_reason")]
        public string? SmallDepositReason { get; set; }

        [Postgrest.Attributes.Column("small_deposit_case")]
        public string? SmallDepositCase { get; set; }

        [Postgrest.Attributes.Column("lease_deposit_dd")]
        public decimal LeaseDepositDd { get; set; }

        [Postgrest.Attributes.Column("lease_deposit_reflected")]
        public decimal LeaseDepositReflected { get; set; }

        [Postgrest.Attributes.Column("lease_deposit_reason")]
        public string? LeaseDepositReason { get; set; }

        [Postgrest.Attributes.Column("wage_claim_dd")]
        public decimal WageClaimDd { get; set; }

        [Postgrest.Attributes.Column("wage_claim_reflected")]
        public decimal WageClaimReflected { get; set; }

        [Postgrest.Attributes.Column("wage_claim_reason")]
        public string? WageClaimReason { get; set; }

        [Postgrest.Attributes.Column("current_tax_dd")]
        public decimal CurrentTaxDd { get; set; }

        [Postgrest.Attributes.Column("current_tax_reflected")]
        public decimal CurrentTaxReflected { get; set; }

        [Postgrest.Attributes.Column("current_tax_reason")]
        public string? CurrentTaxReason { get; set; }

        [Postgrest.Attributes.Column("senior_tax_dd")]
        public decimal SeniorTaxDd { get; set; }

        [Postgrest.Attributes.Column("senior_tax_reflected")]
        public decimal SeniorTaxReflected { get; set; }

        [Postgrest.Attributes.Column("senior_tax_reason")]
        public string? SeniorTaxReason { get; set; }

        // 배당 시뮬레이션
        [Postgrest.Attributes.Column("senior_rights_total")]
        public decimal? SeniorRightsTotal { get; set; }

        [Postgrest.Attributes.Column("mortgage_count")]
        public int? MortgageCount { get; set; }

        [Postgrest.Attributes.Column("seizure_count")]
        public int? SeizureCount { get; set; }

        [Postgrest.Attributes.Column("expected_winning_bid")]
        public decimal? ExpectedWinningBid { get; set; }

        [Postgrest.Attributes.Column("auction_fees")]
        public decimal? AuctionFees { get; set; }

        [Postgrest.Attributes.Column("distributable_amount")]
        public decimal? DistributableAmount { get; set; }

        [Postgrest.Attributes.Column("amount_after_senior")]
        public decimal? AmountAfterSenior { get; set; }

        [Postgrest.Attributes.Column("loan_cap")]
        public decimal? LoanCap { get; set; }

        [Postgrest.Attributes.Column("cap_applied_dividend")]
        public decimal? CapAppliedDividend { get; set; }

        [Postgrest.Attributes.Column("recovery_amount")]
        public decimal? RecoveryAmount { get; set; }

        [Postgrest.Attributes.Column("recovery_rate")]
        public decimal? RecoveryRate { get; set; }

        // 위험도 평가
        [Postgrest.Attributes.Column("risk_level")]
        public string? RiskLevel { get; set; }

        [Postgrest.Attributes.Column("risk_reason")]
        public string? RiskReason { get; set; }

        [Postgrest.Attributes.Column("recommendations")]
        public string? Recommendations { get; set; }

        [Postgrest.Attributes.Column("distribution_analysis")]
        public string? DistributionAnalysis { get; set; }

        // 메타데이터
        [Postgrest.Attributes.Column("analyzed_by")]
        public Guid? AnalyzedBy { get; set; }

        [Postgrest.Attributes.Column("analyzed_at")]
        public DateTime? AnalyzedAt { get; set; }

        [Postgrest.Attributes.Column("is_completed")]
        public bool IsCompleted { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    #endregion
}

