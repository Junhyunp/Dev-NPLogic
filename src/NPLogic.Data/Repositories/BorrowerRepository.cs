using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 차주 Repository
    /// </summary>
    public class BorrowerRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public BorrowerRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 모든 차주 조회
        /// </summary>
        public async Task<List<Borrower>> GetAllAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<BorrowerTable>()
                    .Order(x => x.BorrowerNumber, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToBorrower).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 차주 조회
        /// </summary>
        public async Task<Borrower?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<BorrowerTable>()
                    .Where(x => x.Id == id)
                    .Single();

                return response == null ? null : MapToBorrower(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주번호로 조회
        /// </summary>
        public async Task<Borrower?> GetByBorrowerNumberAsync(string borrowerNumber)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<BorrowerTable>()
                    .Where(x => x.BorrowerNumber == borrowerNumber)
                    .Single();

                return response == null ? null : MapToBorrower(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램별 차주 목록 조회
        /// </summary>
        public async Task<List<Borrower>> GetByProgramIdAsync(string programId)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<BorrowerTable>()
                    .Where(x => x.ProgramId == programId)
                    .Order(x => x.BorrowerNumber, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToBorrower).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램별 차주 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 회생 차주만 조회
        /// </summary>
        public async Task<List<Borrower>> GetRestructuringBorrowersAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<BorrowerTable>()
                    .Where(x => x.IsRestructuring == true)
                    .Order(x => x.BorrowerNumber, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToBorrower).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"회생 차주 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 검색 (차주번호, 차주명)
        /// </summary>
        public async Task<List<Borrower>> SearchAsync(string searchText)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<BorrowerTable>()
                    .Get();

                var filtered = response.Models.Where(x =>
                    (!string.IsNullOrEmpty(x.BorrowerNumber) && x.BorrowerNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.BorrowerName) && x.BorrowerName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.BusinessNumber) && x.BusinessNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                return filtered.Select(MapToBorrower).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 검색 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 복합 필터 조회
        /// </summary>
        public async Task<List<Borrower>> GetFilteredAsync(
            string? programId = null,
            string? borrowerType = null,
            bool? isRestructuring = null,
            string? searchText = null)
        {
            try
            {
                var allBorrowers = await GetAllAsync();

                if (!string.IsNullOrWhiteSpace(programId))
                    allBorrowers = allBorrowers.Where(b => b.ProgramId == programId).ToList();

                if (!string.IsNullOrWhiteSpace(borrowerType))
                    allBorrowers = allBorrowers.Where(b => b.BorrowerType == borrowerType).ToList();

                if (isRestructuring.HasValue)
                    allBorrowers = allBorrowers.Where(b => b.IsRestructuring == isRestructuring.Value).ToList();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    allBorrowers = allBorrowers.Where(b =>
                        (!string.IsNullOrEmpty(b.BorrowerNumber) && b.BorrowerNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(b.BorrowerName) && b.BorrowerName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return allBorrowers;
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 필터 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 페이지네이션된 차주 목록 조회
        /// </summary>
        public async Task<(List<Borrower> Items, int TotalCount)> GetPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? programId = null,
            string? borrowerType = null,
            bool? isRestructuring = null,
            string? searchText = null)
        {
            try
            {
                var allItems = await GetFilteredAsync(programId, borrowerType, isRestructuring, searchText);
                var totalCount = allItems.Count;

                var pagedItems = allItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedItems, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception($"페이지네이션 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주 생성
        /// </summary>
        public async Task<Borrower> CreateAsync(Borrower borrower)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var borrowerTable = MapToBorrowerTable(borrower);
                borrowerTable.CreatedAt = DateTime.UtcNow;
                borrowerTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<BorrowerTable>()
                    .Insert(borrowerTable);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("차주 생성 후 데이터 조회 실패");

                return MapToBorrower(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주 수정
        /// </summary>
        public async Task<Borrower> UpdateAsync(Borrower borrower)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var borrowerTable = MapToBorrowerTable(borrower);
                borrowerTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<BorrowerTable>()
                    .Where(x => x.Id == borrower.Id)
                    .Update(borrowerTable);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("차주 수정 후 데이터 조회 실패");

                return MapToBorrower(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client
                    .From<BorrowerTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 통계 - 전체/유형별 개수
        /// </summary>
        public async Task<BorrowerStatistics> GetStatisticsAsync(string? programId = null)
        {
            try
            {
                var borrowers = await GetFilteredAsync(programId: programId);

                return new BorrowerStatistics
                {
                    TotalCount = borrowers.Count,
                    IndividualCount = borrowers.Count(x => x.BorrowerType == "개인"),
                    SoleProprietorCount = borrowers.Count(x => x.BorrowerType == "개인사업자"),
                    CorporationCount = borrowers.Count(x => x.BorrowerType == "법인"),
                    RestructuringCount = borrowers.Count(x => x.IsRestructuring),
                    TotalOpb = borrowers.Sum(x => x.Opb),
                    TotalMortgageAmount = borrowers.Sum(x => x.MortgageAmount)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"통계 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주의 통계 정보 업데이트 (물건 데이터 기반)
        /// </summary>
        public async Task UpdateBorrowerStatisticsAsync(Guid borrowerId, int propertyCount, decimal opb, decimal mortgageAmount)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var update = new BorrowerTable
                {
                    PropertyCount = propertyCount,
                    Opb = opb,
                    MortgageAmount = mortgageAmount,
                    UpdatedAt = DateTime.UtcNow
                };

                await client
                    .From<BorrowerTable>()
                    .Where(x => x.Id == borrowerId)
                    .Update(update);
            }
            catch (Exception ex)
            {
                throw new Exception($"차주 통계 업데이트 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// BorrowerTable -> Borrower 매핑
        /// </summary>
        private Borrower MapToBorrower(BorrowerTable table)
        {
            return new Borrower
            {
                Id = table.Id,
                BorrowerNumber = table.BorrowerNumber ?? "",
                BorrowerName = table.BorrowerName ?? "",
                BorrowerType = table.BorrowerType ?? "개인",
                BusinessNumber = table.BusinessNumber,
                PropertyCount = table.PropertyCount,
                Opb = table.Opb,
                MortgageAmount = table.MortgageAmount,
                IsRestructuring = table.IsRestructuring,
                IsOpened = table.IsOpened,
                IsDeceased = table.IsDeceased,
                XnpvScenario1 = table.XnpvScenario1,
                XnpvRatio1 = table.XnpvRatio1,
                XnpvScenario2 = table.XnpvScenario2,
                XnpvRatio2 = table.XnpvRatio2,
                Representative = table.Representative,
                Phone = table.Phone,
                Email = table.Email,
                Address = table.Address,
                ProgramId = table.ProgramId,
                CreatedBy = table.CreatedBy,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        /// <summary>
        /// Borrower -> BorrowerTable 매핑
        /// </summary>
        private BorrowerTable MapToBorrowerTable(Borrower borrower)
        {
            return new BorrowerTable
            {
                Id = borrower.Id,
                BorrowerNumber = borrower.BorrowerNumber,
                BorrowerName = borrower.BorrowerName,
                BorrowerType = borrower.BorrowerType,
                BusinessNumber = borrower.BusinessNumber,
                PropertyCount = borrower.PropertyCount,
                Opb = borrower.Opb,
                MortgageAmount = borrower.MortgageAmount,
                IsRestructuring = borrower.IsRestructuring,
                IsOpened = borrower.IsOpened,
                IsDeceased = borrower.IsDeceased,
                XnpvScenario1 = borrower.XnpvScenario1,
                XnpvRatio1 = borrower.XnpvRatio1,
                XnpvScenario2 = borrower.XnpvScenario2,
                XnpvRatio2 = borrower.XnpvRatio2,
                Representative = borrower.Representative,
                Phone = borrower.Phone,
                Email = borrower.Email,
                Address = borrower.Address,
                ProgramId = borrower.ProgramId,
                CreatedBy = borrower.CreatedBy,
                CreatedAt = borrower.CreatedAt,
                UpdatedAt = borrower.UpdatedAt
            };
        }
    }

    /// <summary>
    /// 차주 통계
    /// </summary>
    public class BorrowerStatistics
    {
        public int TotalCount { get; set; }
        public int IndividualCount { get; set; }
        public int SoleProprietorCount { get; set; }
        public int CorporationCount { get; set; }
        public int RestructuringCount { get; set; }
        public decimal TotalOpb { get; set; }
        public decimal TotalMortgageAmount { get; set; }
    }

    /// <summary>
    /// Supabase borrowers 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("borrowers")]
    internal class BorrowerTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("borrower_type")]
        public string? BorrowerType { get; set; }

        [Postgrest.Attributes.Column("business_number")]
        public string? BusinessNumber { get; set; }

        [Postgrest.Attributes.Column("property_count")]
        public int PropertyCount { get; set; }

        [Postgrest.Attributes.Column("opb")]
        public decimal Opb { get; set; }

        [Postgrest.Attributes.Column("mortgage_amount")]
        public decimal MortgageAmount { get; set; }

        [Postgrest.Attributes.Column("is_restructuring")]
        public bool IsRestructuring { get; set; }

        [Postgrest.Attributes.Column("is_opened")]
        public bool IsOpened { get; set; }

        [Postgrest.Attributes.Column("is_deceased")]
        public bool IsDeceased { get; set; }

        [Postgrest.Attributes.Column("xnpv_scenario1")]
        public decimal? XnpvScenario1 { get; set; }

        [Postgrest.Attributes.Column("xnpv_ratio1")]
        public decimal? XnpvRatio1 { get; set; }

        [Postgrest.Attributes.Column("xnpv_scenario2")]
        public decimal? XnpvScenario2 { get; set; }

        [Postgrest.Attributes.Column("xnpv_ratio2")]
        public decimal? XnpvRatio2 { get; set; }

        [Postgrest.Attributes.Column("representative")]
        public string? Representative { get; set; }

        [Postgrest.Attributes.Column("phone")]
        public string? Phone { get; set; }

        [Postgrest.Attributes.Column("email")]
        public string? Email { get; set; }

        [Postgrest.Attributes.Column("address")]
        public string? Address { get; set; }

        [Postgrest.Attributes.Column("program_id")]
        public string? ProgramId { get; set; }

        [Postgrest.Attributes.Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

