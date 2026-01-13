using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// Interim 데이터 Repository (추가 가지급금, 회수정보)
    /// </summary>
    public class InterimRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public InterimRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        #region InterimAdvance (추가 가지급금)

        /// <summary>
        /// 추가 가지급금 생성
        /// </summary>
        public async Task<InterimAdvance> CreateAdvanceAsync(InterimAdvance advance)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAdvanceTable(advance);
                table.CreatedAt = DateTime.UtcNow;

                var response = await client
                    .From<InterimAdvanceTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("가지급금 생성 후 데이터 조회 실패");

                return MapToAdvance(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"가지급금 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 추가 가지급금 일괄 생성
        /// </summary>
        public async Task<(int Created, int Failed)> CreateAdvancesBatchAsync(List<InterimAdvance> advances)
        {
            int created = 0, failed = 0;

            try
            {
                var client = await _supabaseService.GetClientAsync();
                var tables = advances.Select(a =>
                {
                    var table = MapToAdvanceTable(a);
                    table.CreatedAt = DateTime.UtcNow;
                    return table;
                }).ToList();

                // 배치로 삽입
                var response = await client
                    .From<InterimAdvanceTable>()
                    .Insert(tables);

                created = response.Models.Count;
                failed = advances.Count - created;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"가지급금 일괄 생성 실패: {ex.Message}");
                // 실패 시 개별 삽입 시도
                foreach (var advance in advances)
                {
                    try
                    {
                        await CreateAdvanceAsync(advance);
                        created++;
                    }
                    catch
                    {
                        failed++;
                    }
                }
            }

            return (created, failed);
        }

        /// <summary>
        /// 프로그램별 가지급금 조회
        /// </summary>
        public async Task<List<InterimAdvance>> GetAdvancesByProgramAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<InterimAdvanceTable>()
                    .Where(x => x.ProgramId == programId)
                    .Order(x => x.TransactionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAdvance).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"가지급금 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주번호별 가지급금 조회
        /// </summary>
        public async Task<List<InterimAdvance>> GetAdvancesByBorrowerAsync(Guid programId, string borrowerNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<InterimAdvanceTable>()
                    .Where(x => x.ProgramId == programId && x.BorrowerNumber == borrowerNumber)
                    .Order(x => x.TransactionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAdvance).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"가지급금 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램의 가지급금 합계
        /// </summary>
        public async Task<decimal> GetAdvancesTotalByProgramAsync(Guid programId)
        {
            try
            {
                var advances = await GetAdvancesByProgramAsync(programId);
                return advances.Sum(a => a.Amount ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 프로그램의 가지급금 삭제 (재업로드용)
        /// </summary>
        public async Task<bool> DeleteAdvancesByProgramAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<InterimAdvanceTable>()
                    .Where(x => x.ProgramId == programId)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"가지급금 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region InterimCollection (회수정보)

        /// <summary>
        /// 회수정보 생성
        /// </summary>
        public async Task<InterimCollection> CreateCollectionAsync(InterimCollection collection)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToCollectionTable(collection);
                table.CreatedAt = DateTime.UtcNow;

                var response = await client
                    .From<InterimCollectionTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("회수정보 생성 후 데이터 조회 실패");

                return MapToCollection(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"회수정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 회수정보 일괄 생성
        /// </summary>
        public async Task<(int Created, int Failed)> CreateCollectionsBatchAsync(List<InterimCollection> collections)
        {
            int created = 0, failed = 0;

            try
            {
                var client = await _supabaseService.GetClientAsync();
                var tables = collections.Select(c =>
                {
                    var table = MapToCollectionTable(c);
                    table.CreatedAt = DateTime.UtcNow;
                    return table;
                }).ToList();

                // 배치로 삽입
                var response = await client
                    .From<InterimCollectionTable>()
                    .Insert(tables);

                created = response.Models.Count;
                failed = collections.Count - created;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"회수정보 일괄 생성 실패: {ex.Message}");
                // 실패 시 개별 삽입 시도
                foreach (var collection in collections)
                {
                    try
                    {
                        await CreateCollectionAsync(collection);
                        created++;
                    }
                    catch
                    {
                        failed++;
                    }
                }
            }

            return (created, failed);
        }

        /// <summary>
        /// 프로그램별 회수정보 조회
        /// </summary>
        public async Task<List<InterimCollection>> GetCollectionsByProgramAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<InterimCollectionTable>()
                    .Where(x => x.ProgramId == programId)
                    .Order(x => x.CollectionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToCollection).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"회수정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주번호별 회수정보 조회
        /// </summary>
        public async Task<List<InterimCollection>> GetCollectionsByBorrowerAsync(Guid programId, string borrowerNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<InterimCollectionTable>()
                    .Where(x => x.ProgramId == programId && x.BorrowerNumber == borrowerNumber)
                    .Order(x => x.CollectionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToCollection).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"회수정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램의 총회수액 합계
        /// </summary>
        public async Task<decimal> GetCollectionsTotalByProgramAsync(Guid programId)
        {
            try
            {
                var collections = await GetCollectionsByProgramAsync(programId);
                return collections.Sum(c => c.TotalAmount ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 프로그램의 회수정보 삭제 (재업로드용)
        /// </summary>
        public async Task<bool> DeleteCollectionsByProgramAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<InterimCollectionTable>()
                    .Where(x => x.ProgramId == programId)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"회수정보 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region 통계

        /// <summary>
        /// 프로그램별 Interim 통계
        /// </summary>
        public async Task<InterimStatistics> GetStatisticsAsync(Guid programId)
        {
            try
            {
                var advances = await GetAdvancesByProgramAsync(programId);
                var collections = await GetCollectionsByProgramAsync(programId);

                return new InterimStatistics
                {
                    AdvanceCount = advances.Count,
                    AdvanceTotal = advances.Sum(a => a.Amount ?? 0),
                    CollectionCount = collections.Count,
                    CollectionTotal = collections.Sum(c => c.TotalAmount ?? 0),
                    PrincipalTotal = collections.Sum(c => c.PrincipalAmount ?? 0),
                    InterestTotal = collections.Sum(c => c.InterestAmount ?? 0)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Interim 통계 조회 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region 매핑 메서드

        private InterimAdvance MapToAdvance(InterimAdvanceTable table)
        {
            return new InterimAdvance
            {
                Id = table.Id,
                ProgramId = table.ProgramId,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                Pool = table.Pool,
                LoanType = table.LoanType,
                AccountSerial = table.AccountSerial,
                AccountNumber = table.AccountNumber,
                ExpenseType = table.ExpenseType,
                TransactionDate = table.TransactionDate,
                Currency = table.Currency ?? "KRW",
                Amount = table.Amount,
                Description = table.Description,
                Notes = table.Notes,
                UploadedBy = table.UploadedBy,
                CreatedAt = table.CreatedAt
            };
        }

        private InterimAdvanceTable MapToAdvanceTable(InterimAdvance advance)
        {
            return new InterimAdvanceTable
            {
                Id = advance.Id == Guid.Empty ? Guid.NewGuid() : advance.Id,
                ProgramId = advance.ProgramId,
                BorrowerNumber = advance.BorrowerNumber,
                BorrowerName = advance.BorrowerName,
                Pool = advance.Pool,
                LoanType = advance.LoanType,
                AccountSerial = advance.AccountSerial,
                AccountNumber = advance.AccountNumber,
                ExpenseType = advance.ExpenseType,
                TransactionDate = advance.TransactionDate,
                Currency = advance.Currency,
                Amount = advance.Amount,
                Description = advance.Description,
                Notes = advance.Notes,
                UploadedBy = advance.UploadedBy,
                CreatedAt = advance.CreatedAt
            };
        }

        private InterimCollection MapToCollection(InterimCollectionTable table)
        {
            return new InterimCollection
            {
                Id = table.Id,
                ProgramId = table.ProgramId,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                Pool = table.Pool,
                LoanType = table.LoanType,
                AccountSerial = table.AccountSerial,
                AccountNumber = table.AccountNumber,
                CollectionDate = table.CollectionDate,
                PrincipalAmount = table.PrincipalAmount,
                AdvanceAmount = table.AdvanceAmount,
                InterestAmount = table.InterestAmount,
                TotalAmount = table.TotalAmount,
                Notes = table.Notes,
                UploadedBy = table.UploadedBy,
                CreatedAt = table.CreatedAt
            };
        }

        private InterimCollectionTable MapToCollectionTable(InterimCollection collection)
        {
            return new InterimCollectionTable
            {
                Id = collection.Id == Guid.Empty ? Guid.NewGuid() : collection.Id,
                ProgramId = collection.ProgramId,
                BorrowerNumber = collection.BorrowerNumber,
                BorrowerName = collection.BorrowerName,
                Pool = collection.Pool,
                LoanType = collection.LoanType,
                AccountSerial = collection.AccountSerial,
                AccountNumber = collection.AccountNumber,
                CollectionDate = collection.CollectionDate,
                PrincipalAmount = collection.PrincipalAmount,
                AdvanceAmount = collection.AdvanceAmount,
                InterestAmount = collection.InterestAmount,
                TotalAmount = collection.TotalAmount,
                Notes = collection.Notes,
                UploadedBy = collection.UploadedBy,
                CreatedAt = collection.CreatedAt
            };
        }

        #endregion
    }

    /// <summary>
    /// Interim 통계
    /// </summary>
    public class InterimStatistics
    {
        public int AdvanceCount { get; set; }
        public decimal AdvanceTotal { get; set; }
        public int CollectionCount { get; set; }
        public decimal CollectionTotal { get; set; }
        public decimal PrincipalTotal { get; set; }
        public decimal InterestTotal { get; set; }
    }

    /// <summary>
    /// Supabase interim_advances 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("interim_advances")]
    internal class InterimAdvanceTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("program_id")]
        public Guid? ProgramId { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("pool")]
        public string? Pool { get; set; }

        [Postgrest.Attributes.Column("loan_type")]
        public string? LoanType { get; set; }

        [Postgrest.Attributes.Column("account_serial")]
        public string? AccountSerial { get; set; }

        [Postgrest.Attributes.Column("account_number")]
        public string? AccountNumber { get; set; }

        [Postgrest.Attributes.Column("expense_type")]
        public string? ExpenseType { get; set; }

        [Postgrest.Attributes.Column("transaction_date")]
        public DateTime? TransactionDate { get; set; }

        [Postgrest.Attributes.Column("currency")]
        public string? Currency { get; set; }

        [Postgrest.Attributes.Column("amount")]
        public decimal? Amount { get; set; }

        [Postgrest.Attributes.Column("description")]
        public string? Description { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("uploaded_by")]
        public Guid? UploadedBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Supabase interim_collections 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("interim_collections")]
    internal class InterimCollectionTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("program_id")]
        public Guid? ProgramId { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("pool")]
        public string? Pool { get; set; }

        [Postgrest.Attributes.Column("loan_type")]
        public string? LoanType { get; set; }

        [Postgrest.Attributes.Column("account_serial")]
        public string? AccountSerial { get; set; }

        [Postgrest.Attributes.Column("account_number")]
        public string? AccountNumber { get; set; }

        [Postgrest.Attributes.Column("collection_date")]
        public DateTime? CollectionDate { get; set; }

        [Postgrest.Attributes.Column("principal_amount")]
        public decimal? PrincipalAmount { get; set; }

        [Postgrest.Attributes.Column("advance_amount")]
        public decimal? AdvanceAmount { get; set; }

        [Postgrest.Attributes.Column("interest_amount")]
        public decimal? InterestAmount { get; set; }

        [Postgrest.Attributes.Column("total_amount")]
        public decimal? TotalAmount { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("uploaded_by")]
        public Guid? UploadedBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
