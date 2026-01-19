using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 프로그램 Repository
    /// </summary>
    public class ProgramRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public ProgramRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 모든 프로그램 조회
        /// </summary>
        public async Task<List<Program>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramTable>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToProgram).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 활성 프로그램만 조회
        /// </summary>
        public async Task<List<Program>> GetActiveAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramTable>()
                    .Where(x => x.Status == "active")
                    .Order(x => x.ProgramName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToProgram).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"활성 프로그램 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 프로그램 조회
        /// </summary>
        public async Task<Program?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramTable>()
                    .Where(x => x.Id == id)
                    .Single();

                return response == null ? null : MapToProgram(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램명으로 조회
        /// </summary>
        public async Task<Program?> GetByNameAsync(string programName)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramTable>()
                    .Where(x => x.ProgramName == programName)
                    .Single();

                return response == null ? null : MapToProgram(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램 생성
        /// </summary>
        public async Task<Program> CreateAsync(Program program)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToProgramTable(program);
                table.Id = Guid.NewGuid();
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<ProgramTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("프로그램 생성 후 데이터 조회 실패");

                return MapToProgram(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램 수정
        /// </summary>
        public async Task<Program> UpdateAsync(Program program)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToProgramTable(program);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<ProgramTable>()
                    .Where(x => x.Id == program.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("프로그램 수정 후 데이터 조회 실패");

                return MapToProgram(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<ProgramTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램 상태 변경
        /// </summary>
        public async Task<bool> UpdateStatusAsync(Guid id, string status)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var update = new ProgramTable
                {
                    Status = status,
                    UpdatedAt = DateTime.UtcNow
                };

                await client
                    .From<ProgramTable>()
                    .Where(x => x.Id == id)
                    .Update(update);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 상태 변경 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사용자가 담당하는 프로그램 목록 조회 (PM)
        /// </summary>
        public async Task<List<Program>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                // 프로그램-사용자 매핑 테이블을 통해 조회
                var client = await _supabaseService.GetClientAsync();
                
                // 먼저 program_users에서 해당 사용자의 프로그램 ID들을 가져옴
                var programUserResponse = await client
                    .From<ProgramUserTable>()
                    .Where(x => x.UserId == userId)
                    .Get();

                var programIds = programUserResponse.Models.Select(x => x.ProgramId).ToList();
                
                if (!programIds.Any())
                    return new List<Program>();

                // 해당 프로그램들 조회
                var allPrograms = await GetAllAsync();
                return allPrograms.Where(p => programIds.Contains(p.Id)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"사용자별 프로그램 조회 실패: {ex.Message}", ex);
            }
        }

        private Program MapToProgram(ProgramTable table)
        {
            return new Program
            {
                Id = table.Id,
                ProgramName = table.ProgramName ?? string.Empty,
                Team = table.Team,
                AccountingFirm = table.AccountingFirm,
                BorrowerCount = table.BorrowerCount,
                PropertyCount = table.PropertyCount,
                CutOffDate = table.CutOffDate,
                BidDate = table.BidDate,
                Status = table.Status ?? "active",
                CreatedBy = table.CreatedBy,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt,
                ExchangeRateUsd = table.ExchangeRateUsd,
                ExchangeRateJpy = table.ExchangeRateJpy,
                BankName = table.BankName,
                Pool = table.Pool,
                ActionDate = table.ActionDate,
                // 기초정보관리
                Irr = table.Irr,
                CreditGuaranteeRecoveryRate = table.CreditGuaranteeRecoveryRate,
                InterimRecoveryRate = table.InterimRecoveryRate,
                AuctionFirstLeadTimeMonths = table.AuctionFirstLeadTimeMonths,
                RoundLeadTime = table.RoundLeadTime,
                DistributionLeadTime = table.DistributionLeadTime,
                OpeningToAuctionLeadTime = table.OpeningToAuctionLeadTime,
                SubrogationToAuctionLeadTime = table.SubrogationToAuctionLeadTime,
                CombinedLeadTime = table.CombinedLeadTime,
                // 사용인 설정
                AgentAffiliation = table.AgentAffiliation,
                AgentName = table.AgentName,
                AgentGrade = table.AgentGrade ?? "A",
                AgentContact = table.AgentContact,
                AgentKakaoId = table.AgentKakaoId,
                GoodAuctionId = table.GoodAuctionId,
                GoodAuctionPw = table.GoodAuctionPw
            };
        }

        private ProgramTable MapToProgramTable(Program program)
        {
            return new ProgramTable
            {
                Id = program.Id,
                ProgramName = program.ProgramName,
                Team = program.Team,
                AccountingFirm = program.AccountingFirm,
                BorrowerCount = program.BorrowerCount,
                PropertyCount = program.PropertyCount,
                CutOffDate = program.CutOffDate,
                BidDate = program.BidDate,
                Status = program.Status,
                CreatedBy = program.CreatedBy,
                CreatedAt = program.CreatedAt,
                UpdatedAt = program.UpdatedAt,
                ExchangeRateUsd = program.ExchangeRateUsd,
                ExchangeRateJpy = program.ExchangeRateJpy,
                BankName = program.BankName,
                Pool = program.Pool,
                ActionDate = program.ActionDate,
                // 기초정보관리
                Irr = program.Irr,
                CreditGuaranteeRecoveryRate = program.CreditGuaranteeRecoveryRate,
                InterimRecoveryRate = program.InterimRecoveryRate,
                AuctionFirstLeadTimeMonths = program.AuctionFirstLeadTimeMonths,
                RoundLeadTime = program.RoundLeadTime,
                DistributionLeadTime = program.DistributionLeadTime,
                OpeningToAuctionLeadTime = program.OpeningToAuctionLeadTime,
                SubrogationToAuctionLeadTime = program.SubrogationToAuctionLeadTime,
                CombinedLeadTime = program.CombinedLeadTime,
                // 사용인 설정
                AgentAffiliation = program.AgentAffiliation,
                AgentName = program.AgentName,
                AgentGrade = program.AgentGrade,
                AgentContact = program.AgentContact,
                AgentKakaoId = program.AgentKakaoId,
                GoodAuctionId = program.GoodAuctionId,
                GoodAuctionPw = program.GoodAuctionPw
            };
        }
    }

    /// <summary>
    /// Supabase programs 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("programs")]
    internal class ProgramTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("program_name")]
        public string? ProgramName { get; set; }

        [Postgrest.Attributes.Column("team")]
        public string? Team { get; set; }

        [Postgrest.Attributes.Column("accounting_firm")]
        public string? AccountingFirm { get; set; }

        [Postgrest.Attributes.Column("borrower_count")]
        public int BorrowerCount { get; set; }

        [Postgrest.Attributes.Column("property_count")]
        public int PropertyCount { get; set; }

        [Postgrest.Attributes.Column("cut_off_date")]
        public DateTime? CutOffDate { get; set; }

        [Postgrest.Attributes.Column("bid_date")]
        public DateTime? BidDate { get; set; }

        [Postgrest.Attributes.Column("status")]
        public string? Status { get; set; }

        [Postgrest.Attributes.Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Postgrest.Attributes.Column("exchange_rate_usd")]
        public decimal? ExchangeRateUsd { get; set; }

        [Postgrest.Attributes.Column("exchange_rate_jpy")]
        public decimal? ExchangeRateJpy { get; set; }

        [Postgrest.Attributes.Column("bank_name")]
        public string? BankName { get; set; }

        [Postgrest.Attributes.Column("pool")]
        public string? Pool { get; set; }

        [Postgrest.Attributes.Column("action_date")]
        public DateTime? ActionDate { get; set; }

        // ========== 기초정보관리 ==========

        [Postgrest.Attributes.Column("irr")]
        public decimal Irr { get; set; }

        [Postgrest.Attributes.Column("credit_guarantee_recovery_rate")]
        public decimal CreditGuaranteeRecoveryRate { get; set; }

        [Postgrest.Attributes.Column("interim_recovery_rate")]
        public decimal InterimRecoveryRate { get; set; }

        [Postgrest.Attributes.Column("auction_first_lead_time_months")]
        public int AuctionFirstLeadTimeMonths { get; set; }

        [Postgrest.Attributes.Column("round_lead_time")]
        public int RoundLeadTime { get; set; }

        [Postgrest.Attributes.Column("distribution_lead_time")]
        public int DistributionLeadTime { get; set; }

        [Postgrest.Attributes.Column("opening_to_auction_lead_time")]
        public int OpeningToAuctionLeadTime { get; set; }

        [Postgrest.Attributes.Column("subrogation_to_auction_lead_time")]
        public int SubrogationToAuctionLeadTime { get; set; }

        [Postgrest.Attributes.Column("combined_lead_time")]
        public int CombinedLeadTime { get; set; }

        // ========== 사용인 설정 ==========

        [Postgrest.Attributes.Column("agent_affiliation")]
        public string? AgentAffiliation { get; set; }

        [Postgrest.Attributes.Column("agent_name")]
        public string? AgentName { get; set; }

        [Postgrest.Attributes.Column("agent_grade")]
        public string? AgentGrade { get; set; }

        [Postgrest.Attributes.Column("agent_contact")]
        public string? AgentContact { get; set; }

        [Postgrest.Attributes.Column("agent_kakao_id")]
        public string? AgentKakaoId { get; set; }

        [Postgrest.Attributes.Column("good_auction_id")]
        public string? GoodAuctionId { get; set; }

        [Postgrest.Attributes.Column("good_auction_pw")]
        public string? GoodAuctionPw { get; set; }
    }
}

