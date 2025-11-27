using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 대출 Repository
    /// </summary>
    public class LoanRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public LoanRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 모든 대출 조회
        /// </summary>
        public async Task<List<Loan>> GetAllAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<LoanTable>()
                    .Order(x => x.AccountSerial, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToLoan).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 대출 조회
        /// </summary>
        public async Task<Loan?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<LoanTable>()
                    .Where(x => x.Id == id)
                    .Single();

                return response == null ? null : MapToLoan(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주별 대출 목록 조회
        /// </summary>
        public async Task<List<Loan>> GetByBorrowerIdAsync(Guid borrowerId)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<LoanTable>()
                    .Where(x => x.BorrowerId == borrowerId)
                    .Order(x => x.AccountSerial, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToLoan).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"차주별 대출 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 계좌일련번호로 조회
        /// </summary>
        public async Task<Loan?> GetByAccountSerialAsync(string accountSerial)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<LoanTable>()
                    .Where(x => x.AccountSerial == accountSerial)
                    .Single();

                return response == null ? null : MapToLoan(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// MCI 보증이 있는 대출만 조회
        /// </summary>
        public async Task<List<Loan>> GetMciLoansAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<LoanTable>()
                    .Where(x => x.HasMciGuarantee == true)
                    .Order(x => x.AccountSerial, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToLoan).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"MCI 대출 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 복합 필터 조회
        /// </summary>
        public async Task<List<Loan>> GetFilteredAsync(
            Guid? borrowerId = null,
            string? loanType = null,
            bool? hasMciGuarantee = null,
            string? searchText = null)
        {
            try
            {
                var allLoans = await GetAllAsync();

                if (borrowerId.HasValue)
                    allLoans = allLoans.Where(l => l.BorrowerId == borrowerId.Value).ToList();

                if (!string.IsNullOrWhiteSpace(loanType))
                    allLoans = allLoans.Where(l => l.LoanType == loanType).ToList();

                if (hasMciGuarantee.HasValue)
                    allLoans = allLoans.Where(l => l.HasMciGuarantee == hasMciGuarantee.Value).ToList();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    allLoans = allLoans.Where(l =>
                        (!string.IsNullOrEmpty(l.AccountSerial) && l.AccountSerial.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(l.AccountNumber) && l.AccountNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return allLoans;
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 필터 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 대출 생성
        /// </summary>
        public async Task<Loan> CreateAsync(Loan loan)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var loanTable = MapToLoanTable(loan);
                loanTable.CreatedAt = DateTime.UtcNow;
                loanTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<LoanTable>()
                    .Insert(loanTable);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("대출 생성 후 데이터 조회 실패");

                return MapToLoan(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 대출 수정
        /// </summary>
        public async Task<Loan> UpdateAsync(Loan loan)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var loanTable = MapToLoanTable(loan);
                loanTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<LoanTable>()
                    .Where(x => x.Id == loan.Id)
                    .Update(loanTable);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("대출 수정 후 데이터 조회 실패");

                return MapToLoan(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 대출 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client
                    .From<LoanTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주별 대출 통계
        /// </summary>
        public async Task<LoanStatistics> GetStatisticsByBorrowerIdAsync(Guid borrowerId)
        {
            try
            {
                var loans = await GetByBorrowerIdAsync(borrowerId);

                return new LoanStatistics
                {
                    TotalCount = loans.Count,
                    TotalPrincipalBalance = loans.Sum(l => l.LoanPrincipalBalance ?? 0),
                    TotalClaimAmount = loans.Sum(l => l.TotalClaimAmount ?? l.CalculateTotalClaim()),
                    TotalLoanCap1 = loans.Sum(l => l.LoanCap1 ?? 0),
                    TotalLoanCap2 = loans.Sum(l => l.LoanCap2 ?? 0),
                    MciLoanCount = loans.Count(l => l.HasMciGuarantee)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"대출 통계 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LoanTable -> Loan 매핑
        /// </summary>
        private Loan MapToLoan(LoanTable table)
        {
            return new Loan
            {
                Id = table.Id,
                BorrowerId = table.BorrowerId,
                AccountSerial = table.AccountSerial,
                LoanType = table.LoanType,
                LoanCategory = table.LoanCategory,
                AccountNumber = table.AccountNumber,
                InitialLoanAmount = table.InitialLoanAmount,
                LoanPrincipalBalance = table.LoanPrincipalBalance,
                AdvancePayment = table.AdvancePayment,
                AccruedInterest = table.AccruedInterest,
                TotalClaimAmount = table.TotalClaimAmount,
                InitialLoanDate = table.InitialLoanDate,
                LastInterestDate = table.LastInterestDate,
                NormalInterestRate = table.NormalInterestRate,
                OverdueInterestRate = table.OverdueInterestRate,
                Collateral1Id = table.Collateral1Id,
                Collateral2Id = table.Collateral2Id,
                Collateral3Id = table.Collateral3Id,
                HasAgreementDoc = table.HasAgreementDoc,
                HasAuctionCost = table.HasAuctionCost,
                HasValidGuarantee = table.HasValidGuarantee,
                HasPriorSubrogation = table.HasPriorSubrogation,
                IsTerminatedGuarantee = table.IsTerminatedGuarantee,
                HasMciGuarantee = table.HasMciGuarantee,
                PrincipalOffset = table.PrincipalOffset,
                PrincipalRecovery = table.PrincipalRecovery,
                InterestOffset = table.InterestOffset,
                InterestRecovery = table.InterestRecovery,
                ExpectedDividendDate1 = table.ExpectedDividendDate1,
                OverdueInterest1 = table.OverdueInterest1,
                LoanCap1 = table.LoanCap1,
                ExpectedDividendDate2 = table.ExpectedDividendDate2,
                OverdueInterest2 = table.OverdueInterest2,
                LoanCap2 = table.LoanCap2,
                MciInitialAmount = table.MciInitialAmount,
                MciBalance = table.MciBalance,
                MciGuaranteeNumber = table.MciGuaranteeNumber,
                MciBondNumber = table.MciBondNumber,
                Notes = table.Notes,
                CreatedBy = table.CreatedBy,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        /// <summary>
        /// Loan -> LoanTable 매핑
        /// </summary>
        private LoanTable MapToLoanTable(Loan loan)
        {
            return new LoanTable
            {
                Id = loan.Id,
                BorrowerId = loan.BorrowerId,
                AccountSerial = loan.AccountSerial,
                LoanType = loan.LoanType,
                LoanCategory = loan.LoanCategory,
                AccountNumber = loan.AccountNumber,
                InitialLoanAmount = loan.InitialLoanAmount,
                LoanPrincipalBalance = loan.LoanPrincipalBalance,
                AdvancePayment = loan.AdvancePayment,
                AccruedInterest = loan.AccruedInterest,
                TotalClaimAmount = loan.TotalClaimAmount,
                InitialLoanDate = loan.InitialLoanDate,
                LastInterestDate = loan.LastInterestDate,
                NormalInterestRate = loan.NormalInterestRate,
                OverdueInterestRate = loan.OverdueInterestRate,
                Collateral1Id = loan.Collateral1Id,
                Collateral2Id = loan.Collateral2Id,
                Collateral3Id = loan.Collateral3Id,
                HasAgreementDoc = loan.HasAgreementDoc,
                HasAuctionCost = loan.HasAuctionCost,
                HasValidGuarantee = loan.HasValidGuarantee,
                HasPriorSubrogation = loan.HasPriorSubrogation,
                IsTerminatedGuarantee = loan.IsTerminatedGuarantee,
                HasMciGuarantee = loan.HasMciGuarantee,
                PrincipalOffset = loan.PrincipalOffset,
                PrincipalRecovery = loan.PrincipalRecovery,
                InterestOffset = loan.InterestOffset,
                InterestRecovery = loan.InterestRecovery,
                ExpectedDividendDate1 = loan.ExpectedDividendDate1,
                OverdueInterest1 = loan.OverdueInterest1,
                LoanCap1 = loan.LoanCap1,
                ExpectedDividendDate2 = loan.ExpectedDividendDate2,
                OverdueInterest2 = loan.OverdueInterest2,
                LoanCap2 = loan.LoanCap2,
                MciInitialAmount = loan.MciInitialAmount,
                MciBalance = loan.MciBalance,
                MciGuaranteeNumber = loan.MciGuaranteeNumber,
                MciBondNumber = loan.MciBondNumber,
                Notes = loan.Notes,
                CreatedBy = loan.CreatedBy,
                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt
            };
        }
    }

    /// <summary>
    /// 대출 통계
    /// </summary>
    public class LoanStatistics
    {
        public int TotalCount { get; set; }
        public decimal TotalPrincipalBalance { get; set; }
        public decimal TotalClaimAmount { get; set; }
        public decimal TotalLoanCap1 { get; set; }
        public decimal TotalLoanCap2 { get; set; }
        public int MciLoanCount { get; set; }
    }

    /// <summary>
    /// Supabase loans 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("loans")]
    internal class LoanTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("borrower_id")]
        public Guid? BorrowerId { get; set; }

        [Postgrest.Attributes.Column("account_serial")]
        public string? AccountSerial { get; set; }

        [Postgrest.Attributes.Column("loan_type")]
        public string? LoanType { get; set; }

        [Postgrest.Attributes.Column("loan_category")]
        public string? LoanCategory { get; set; }

        [Postgrest.Attributes.Column("account_number")]
        public string? AccountNumber { get; set; }

        [Postgrest.Attributes.Column("initial_loan_amount")]
        public decimal? InitialLoanAmount { get; set; }

        [Postgrest.Attributes.Column("loan_principal_balance")]
        public decimal? LoanPrincipalBalance { get; set; }

        [Postgrest.Attributes.Column("advance_payment")]
        public decimal AdvancePayment { get; set; }

        [Postgrest.Attributes.Column("accrued_interest")]
        public decimal AccruedInterest { get; set; }

        [Postgrest.Attributes.Column("total_claim_amount")]
        public decimal? TotalClaimAmount { get; set; }

        [Postgrest.Attributes.Column("initial_loan_date")]
        public DateTime? InitialLoanDate { get; set; }

        [Postgrest.Attributes.Column("last_interest_date")]
        public DateTime? LastInterestDate { get; set; }

        [Postgrest.Attributes.Column("normal_interest_rate")]
        public decimal? NormalInterestRate { get; set; }

        [Postgrest.Attributes.Column("overdue_interest_rate")]
        public decimal? OverdueInterestRate { get; set; }

        [Postgrest.Attributes.Column("collateral_1_id")]
        public Guid? Collateral1Id { get; set; }

        [Postgrest.Attributes.Column("collateral_2_id")]
        public Guid? Collateral2Id { get; set; }

        [Postgrest.Attributes.Column("collateral_3_id")]
        public Guid? Collateral3Id { get; set; }

        [Postgrest.Attributes.Column("has_agreement_doc")]
        public bool HasAgreementDoc { get; set; }

        [Postgrest.Attributes.Column("has_auction_cost")]
        public bool HasAuctionCost { get; set; }

        [Postgrest.Attributes.Column("has_valid_guarantee")]
        public bool HasValidGuarantee { get; set; }

        [Postgrest.Attributes.Column("has_prior_subrogation")]
        public bool HasPriorSubrogation { get; set; }

        [Postgrest.Attributes.Column("is_terminated_guarantee")]
        public bool IsTerminatedGuarantee { get; set; }

        [Postgrest.Attributes.Column("has_mci_guarantee")]
        public bool HasMciGuarantee { get; set; }

        [Postgrest.Attributes.Column("principal_offset")]
        public decimal PrincipalOffset { get; set; }

        [Postgrest.Attributes.Column("principal_recovery")]
        public decimal PrincipalRecovery { get; set; }

        [Postgrest.Attributes.Column("interest_offset")]
        public decimal InterestOffset { get; set; }

        [Postgrest.Attributes.Column("interest_recovery")]
        public decimal InterestRecovery { get; set; }

        [Postgrest.Attributes.Column("expected_dividend_date_1")]
        public DateTime? ExpectedDividendDate1 { get; set; }

        [Postgrest.Attributes.Column("overdue_interest_1")]
        public decimal? OverdueInterest1 { get; set; }

        [Postgrest.Attributes.Column("loan_cap_1")]
        public decimal? LoanCap1 { get; set; }

        [Postgrest.Attributes.Column("expected_dividend_date_2")]
        public DateTime? ExpectedDividendDate2 { get; set; }

        [Postgrest.Attributes.Column("overdue_interest_2")]
        public decimal? OverdueInterest2 { get; set; }

        [Postgrest.Attributes.Column("loan_cap_2")]
        public decimal? LoanCap2 { get; set; }

        [Postgrest.Attributes.Column("mci_initial_amount")]
        public decimal? MciInitialAmount { get; set; }

        [Postgrest.Attributes.Column("mci_balance")]
        public decimal? MciBalance { get; set; }

        [Postgrest.Attributes.Column("mci_guarantee_number")]
        public string? MciGuaranteeNumber { get; set; }

        [Postgrest.Attributes.Column("mci_bond_number")]
        public string? MciBondNumber { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

