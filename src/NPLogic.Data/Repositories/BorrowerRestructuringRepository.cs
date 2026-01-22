using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 회생 차주 정보 Repository
    /// </summary>
    public class BorrowerRestructuringRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public BorrowerRestructuringRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 모든 회생 정보 조회
        /// </summary>
        public async Task<List<BorrowerRestructuring>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<BorrowerRestructuringTable>()
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주 ID로 회생 정보 조회
        /// </summary>
        public async Task<BorrowerRestructuring?> GetByBorrowerIdAsync(Guid borrowerId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<BorrowerRestructuringTable>()
                    .Where(x => x.BorrowerId == borrowerId)
                    .Single();

                return response == null ? null : MapToModel(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 사건번호로 조회
        /// </summary>
        public async Task<BorrowerRestructuring?> GetByCaseNumberAsync(string caseNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<BorrowerRestructuringTable>()
                    .Where(x => x.CaseNumber == caseNumber)
                    .Single();

                return response == null ? null : MapToModel(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 회생 정보 생성
        /// </summary>
        public async Task<BorrowerRestructuring> CreateAsync(BorrowerRestructuring restructuring)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(restructuring);
                table.Id = Guid.NewGuid();
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<BorrowerRestructuringTable>()
                    .Insert(table);

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 회생 정보 수정
        /// </summary>
        public async Task<BorrowerRestructuring> UpdateAsync(BorrowerRestructuring restructuring)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(restructuring);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<BorrowerRestructuringTable>()
                    .Where(x => x.Id == restructuring.Id)
                    .Update(table);

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Upsert (있으면 업데이트, 없으면 생성) - borrower_id 또는 borrower_number 기준
        /// </summary>
        public async Task<BorrowerRestructuring> UpsertAsync(BorrowerRestructuring restructuring)
        {
            try
            {
                BorrowerRestructuring? existing = null;

                // BorrowerId가 있으면 해당 기준으로 조회
                if (restructuring.BorrowerId.HasValue)
                {
                    existing = await GetByBorrowerIdAsync(restructuring.BorrowerId.Value);
                }
                // BorrowerNumber가 있으면 해당 기준으로 조회
                else if (!string.IsNullOrEmpty(restructuring.BorrowerNumber))
                {
                    existing = await GetByBorrowerNumberAsync(restructuring.BorrowerNumber);
                }

                if (existing != null)
                {
                    // 업데이트
                    restructuring.Id = existing.Id;
                    return await UpdateAsync(restructuring);
                }
                else
                {
                    // 신규 생성
                    return await CreateAsync(restructuring);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 Upsert 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주일련번호로 조회
        /// </summary>
        public async Task<BorrowerRestructuring?> GetByBorrowerNumberAsync(string borrowerNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<BorrowerRestructuringTable>()
                    .Where(x => x.BorrowerNumber == borrowerNumber)
                    .Single();

                return response == null ? null : MapToModel(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 회생 정보 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<BorrowerRestructuringTable>()
                    .Where(x => x.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 정보 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== 매핑 ==========

        private static BorrowerRestructuring MapToModel(BorrowerRestructuringTable table)
        {
            return new BorrowerRestructuring
            {
                Id = table.Id,
                BorrowerId = table.BorrowerId,
                AssetType = table.AssetType,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                ApprovalStatus = table.ApprovalStatus,
                ProgressStage = table.ProgressStage,
                CourtName = table.CourtName,
                CaseNumber = table.CaseNumber,
                FilingDate = table.FilingDate,
                PreservationDate = table.PreservationDate,
                CommencementDate = table.CommencementDate,
                ClaimFilingDate = table.ClaimFilingDate,
                ApprovalDismissalDate = table.ApprovalDismissalDate,
                ExcludedClaim = table.ExcludedClaim,
                Industry = table.Industry,
                ListingStatus = table.ListingStatus,
                EmployeeCount = table.EmployeeCount,
                EstablishmentDate = table.EstablishmentDate,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private static BorrowerRestructuringTable MapToTable(BorrowerRestructuring model)
        {
            return new BorrowerRestructuringTable
            {
                Id = model.Id,
                BorrowerId = model.BorrowerId,
                AssetType = model.AssetType,
                BorrowerNumber = model.BorrowerNumber,
                BorrowerName = model.BorrowerName,
                ApprovalStatus = model.ApprovalStatus,
                ProgressStage = model.ProgressStage,
                CourtName = model.CourtName,
                CaseNumber = model.CaseNumber,
                FilingDate = model.FilingDate,
                PreservationDate = model.PreservationDate,
                CommencementDate = model.CommencementDate,
                ClaimFilingDate = model.ClaimFilingDate,
                ApprovalDismissalDate = model.ApprovalDismissalDate,
                ExcludedClaim = model.ExcludedClaim,
                Industry = model.Industry,
                ListingStatus = model.ListingStatus,
                EmployeeCount = model.EmployeeCount,
                EstablishmentDate = model.EstablishmentDate,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }

    /// <summary>
    /// Supabase borrower_restructuring 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("borrower_restructuring")]
    internal class BorrowerRestructuringTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("borrower_id")]
        public Guid? BorrowerId { get; set; }

        [Postgrest.Attributes.Column("asset_type")]
        public string? AssetType { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("approval_status")]
        public string? ApprovalStatus { get; set; }

        [Postgrest.Attributes.Column("progress_stage")]
        public string? ProgressStage { get; set; }

        [Postgrest.Attributes.Column("court_name")]
        public string? CourtName { get; set; }

        [Postgrest.Attributes.Column("case_number")]
        public string? CaseNumber { get; set; }

        [Postgrest.Attributes.Column("filing_date")]
        public DateTime? FilingDate { get; set; }

        [Postgrest.Attributes.Column("preservation_date")]
        public DateTime? PreservationDate { get; set; }

        [Postgrest.Attributes.Column("commencement_date")]
        public DateTime? CommencementDate { get; set; }

        [Postgrest.Attributes.Column("claim_filing_date")]
        public DateTime? ClaimFilingDate { get; set; }

        [Postgrest.Attributes.Column("approval_dismissal_date")]
        public DateTime? ApprovalDismissalDate { get; set; }

        [Postgrest.Attributes.Column("excluded_claim")]
        public string? ExcludedClaim { get; set; }

        // 회사 정보
        [Postgrest.Attributes.Column("industry")]
        public string? Industry { get; set; }

        [Postgrest.Attributes.Column("listing_status")]
        public string? ListingStatus { get; set; }

        [Postgrest.Attributes.Column("employee_count")]
        public int? EmployeeCount { get; set; }

        [Postgrest.Attributes.Column("establishment_date")]
        public DateTime? EstablishmentDate { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

