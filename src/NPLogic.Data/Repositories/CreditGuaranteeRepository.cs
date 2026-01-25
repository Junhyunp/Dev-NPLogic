using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 신용보증서 Repository
    /// </summary>
    public class CreditGuaranteeRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public CreditGuaranteeRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 대출 ID로 신용보증서 조회
        /// </summary>
        public async Task<List<CreditGuarantee>> GetByLoanIdAsync(Guid loanId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<CreditGuaranteeTable>()
                    .Where(x => x.LoanId == loanId)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주 ID로 신용보증서 조회
        /// </summary>
        public async Task<List<CreditGuarantee>> GetByBorrowerIdAsync(Guid borrowerId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<CreditGuaranteeTable>()
                    .Where(x => x.BorrowerId == borrowerId)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 신용보증서 생성
        /// </summary>
        public async Task<CreditGuarantee> CreateAsync(CreditGuarantee guarantee)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(guarantee);
                table.Id = Guid.NewGuid();
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<CreditGuaranteeTable>()
                    .Insert(table);

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 신용보증서 수정
        /// </summary>
        public async Task<CreditGuarantee> UpdateAsync(CreditGuarantee guarantee)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(guarantee);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<CreditGuaranteeTable>()
                    .Where(x => x.Id == guarantee.Id)
                    .Update(table);

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Upsert - 차주일련번호 + 보증서번호 기준
        /// </summary>
        public async Task<CreditGuarantee> UpsertAsync(CreditGuarantee guarantee)
        {
            try
            {
                CreditGuarantee? existing = null;

                // 차주일련번호 + 보증서번호로 기존 데이터 조회
                if (!string.IsNullOrEmpty(guarantee.BorrowerNumber) && !string.IsNullOrEmpty(guarantee.GuaranteeNumber))
                {
                    var client = await _supabaseService.GetClientAsync();
                    var response = await client
                        .From<CreditGuaranteeTable>()
                        .Where(x => x.BorrowerNumber == guarantee.BorrowerNumber)
                        .Where(x => x.GuaranteeNumber == guarantee.GuaranteeNumber)
                        .Single();

                    existing = response != null ? MapToModel(response) : null;
                }

                if (existing != null)
                {
                    guarantee.Id = existing.Id;
                    return await UpdateAsync(guarantee);
                }
                else
                {
                    return await CreateAsync(guarantee);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 Upsert 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 신용보증서 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<CreditGuaranteeTable>()
                    .Where(x => x.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== 매핑 ==========

        private static CreditGuarantee MapToModel(CreditGuaranteeTable table)
        {
            return new CreditGuarantee
            {
                Id = table.Id,
                LoanId = table.LoanId,
                BorrowerId = table.BorrowerId,
                AssetType = table.AssetType,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                AccountSerial = table.AccountSerial,
                RelatedLoanAccountNumber = table.RelatedLoanAccountNumber,
                GuaranteeNumber = table.GuaranteeNumber,
                GuaranteeType = table.GuaranteeType,
                GuaranteeInstitution = table.GuaranteeInstitution,
                GuaranteeRatio = table.GuaranteeRatio,
                ConvertedGuaranteeBalance = table.ConvertedGuaranteeBalance,
                GuaranteeAmount = table.GuaranteeAmount,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private static CreditGuaranteeTable MapToTable(CreditGuarantee model)
        {
            return new CreditGuaranteeTable
            {
                Id = model.Id,
                LoanId = model.LoanId,
                BorrowerId = model.BorrowerId,
                AssetType = model.AssetType,
                BorrowerNumber = model.BorrowerNumber,
                BorrowerName = model.BorrowerName,
                AccountSerial = model.AccountSerial,
                RelatedLoanAccountNumber = model.RelatedLoanAccountNumber,
                GuaranteeNumber = model.GuaranteeNumber,
                GuaranteeType = model.GuaranteeType,
                GuaranteeInstitution = model.GuaranteeInstitution,
                GuaranteeRatio = model.GuaranteeRatio,
                ConvertedGuaranteeBalance = model.ConvertedGuaranteeBalance,
                GuaranteeAmount = model.GuaranteeAmount,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        /// <summary>
        /// 여러 차주의 신용보증서 일괄 삭제
        /// </summary>
        public async Task DeleteByBorrowerIdsAsync(List<Guid> borrowerIds)
        {
            if (borrowerIds == null || borrowerIds.Count == 0) return;

            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<CreditGuaranteeTable>()
                    .Filter("borrower_id", Postgrest.Constants.Operator.In, borrowerIds)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"신용보증서 일괄 삭제 실패: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Supabase credit_guarantees 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("credit_guarantees")]
    internal class CreditGuaranteeTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("loan_id")]
        public Guid? LoanId { get; set; }

        [Postgrest.Attributes.Column("borrower_id")]
        public Guid? BorrowerId { get; set; }

        [Postgrest.Attributes.Column("asset_type")]
        public string? AssetType { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("account_serial")]
        public string? AccountSerial { get; set; }

        [Postgrest.Attributes.Column("related_loan_account_number")]
        public string? RelatedLoanAccountNumber { get; set; }

        [Postgrest.Attributes.Column("guarantee_number")]
        public string? GuaranteeNumber { get; set; }

        [Postgrest.Attributes.Column("guarantee_type")]
        public string? GuaranteeType { get; set; }

        [Postgrest.Attributes.Column("guarantee_institution")]
        public string? GuaranteeInstitution { get; set; }

        [Postgrest.Attributes.Column("guarantee_ratio")]
        public decimal? GuaranteeRatio { get; set; }

        [Postgrest.Attributes.Column("converted_guarantee_balance")]
        public decimal? ConvertedGuaranteeBalance { get; set; }

        [Postgrest.Attributes.Column("guarantee_amount")]
        public decimal? GuaranteeAmount { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
