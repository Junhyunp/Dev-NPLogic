using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Services;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 등기부 Repository
    /// </summary>
    public class RegistryRepository
    {
        private readonly SupabaseService _supabaseService;

        public RegistryRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        #region RegistryDocument (등기부 문서)

        /// <summary>
        /// 물건별 등기부 문서 조회
        /// </summary>
        public async Task<List<RegistryDocument>> GetDocumentsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryDocumentTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToRegistryDocument).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부 문서 생성
        /// </summary>
        public async Task<RegistryDocument> CreateDocumentAsync(RegistryDocument document)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryDocumentTable(document);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryDocumentTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("등기부 문서 생성 후 데이터 조회 실패");

                return MapToRegistryDocument(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부 문서 수정
        /// </summary>
        public async Task<RegistryDocument> UpdateDocumentAsync(RegistryDocument document)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryDocumentTable(document);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryDocumentTable>()
                    .Where(x => x.Id == document.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("등기부 문서 수정 후 데이터 조회 실패");

                return MapToRegistryDocument(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부 문서 삭제
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistryDocumentTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region RegistryOwner (소유자 정보)

        /// <summary>
        /// 물건별 소유자 정보 조회
        /// </summary>
        public async Task<List<RegistryOwner>> GetOwnersByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryOwnerTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryOwner).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소유자 정보 생성
        /// </summary>
        public async Task<RegistryOwner> CreateOwnerAsync(RegistryOwner owner)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryOwnerTable(owner);
                table.CreatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryOwnerTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("소유자 정보 생성 후 데이터 조회 실패");

                return MapToRegistryOwner(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소유자 정보 수정
        /// </summary>
        public async Task<RegistryOwner> UpdateOwnerAsync(RegistryOwner owner)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryOwnerTable(owner);

                var response = await client
                    .From<RegistryOwnerTable>()
                    .Where(x => x.Id == owner.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("소유자 정보 수정 후 데이터 조회 실패");

                return MapToRegistryOwner(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소유자 정보 삭제
        /// </summary>
        public async Task<bool> DeleteOwnerAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistryOwnerTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region RegistryRight (권리 정보 - 갑구/을구)

        /// <summary>
        /// 전체 권리 정보 조회
        /// </summary>
        public async Task<List<RegistryRight>> GetAllRightsAsync(int limit = 500)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Limit(limit)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 권리 정보 조회 (갑구/을구 전체)
        /// </summary>
        public async Task<List<RegistryRight>> GetRightsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.RightOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 갑구 (소유권) 권리 조회
        /// </summary>
        public async Task<List<RegistryRight>> GetGapguRightsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Where(x => x.RightType == "gap")
                    .Order(x => x.RightOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 권리 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 을구 (근저당/전세권) 권리 조회
        /// </summary>
        public async Task<List<RegistryRight>> GetEulguRightsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Where(x => x.RightType == "eul")
                    .Order(x => x.RightOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 권리 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리 정보 생성
        /// </summary>
        public async Task<RegistryRight> CreateRightAsync(RegistryRight right)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryRightTable(right);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryRightTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("권리 정보 생성 후 데이터 조회 실패");

                return MapToRegistryRight(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리 정보 수정
        /// </summary>
        public async Task<RegistryRight> UpdateRightAsync(RegistryRight right)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryRightTable(right);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.Id == right.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("권리 정보 수정 후 데이터 조회 실패");

                return MapToRegistryRight(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리 정보 삭제
        /// </summary>
        public async Task<bool> DeleteRightAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistryRightTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region Mapping Methods

        private RegistryDocument MapToRegistryDocument(RegistryDocumentTable table)
        {
            return new RegistryDocument
            {
                Id = table.Id,
                PropertyId = table.PropertyId,
                FilePath = table.FilePath ?? string.Empty,
                FileName = table.FileName,
                FileSize = table.FileSize,
                OcrStatus = table.OcrStatus ?? "pending",
                OcrProcessedAt = table.OcrProcessedAt,
                OcrError = table.OcrError,
                RegistryType = table.RegistryType,
                RegistryNumber = table.RegistryNumber,
                ExtractedData = table.ExtractedData,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryDocumentTable MapToRegistryDocumentTable(RegistryDocument model)
        {
            return new RegistryDocumentTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                FilePath = model.FilePath,
                FileName = model.FileName,
                FileSize = model.FileSize,
                OcrStatus = model.OcrStatus,
                OcrProcessedAt = model.OcrProcessedAt,
                OcrError = model.OcrError,
                RegistryType = model.RegistryType,
                RegistryNumber = model.RegistryNumber,
                ExtractedData = model.ExtractedData,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        private RegistryOwner MapToRegistryOwner(RegistryOwnerTable table)
        {
            return new RegistryOwner
            {
                Id = table.Id,
                RegistryDocumentId = table.RegistryDocumentId,
                PropertyId = table.PropertyId,
                OwnerName = table.OwnerName,
                OwnerRegNo = table.OwnerRegNo,
                ShareRatio = table.ShareRatio,
                RegistrationDate = table.RegistrationDate,
                RegistrationCause = table.RegistrationCause,
                CreatedAt = table.CreatedAt
            };
        }

        private RegistryOwnerTable MapToRegistryOwnerTable(RegistryOwner model)
        {
            return new RegistryOwnerTable
            {
                Id = model.Id,
                RegistryDocumentId = model.RegistryDocumentId,
                PropertyId = model.PropertyId,
                OwnerName = model.OwnerName,
                OwnerRegNo = model.OwnerRegNo,
                ShareRatio = model.ShareRatio,
                RegistrationDate = model.RegistrationDate,
                RegistrationCause = model.RegistrationCause,
                CreatedAt = model.CreatedAt
            };
        }

        private RegistryRight MapToRegistryRight(RegistryRightTable table)
        {
            return new RegistryRight
            {
                Id = table.Id,
                RegistryDocumentId = table.RegistryDocumentId,
                PropertyId = table.PropertyId,
                RightType = table.RightType ?? "gap",
                RightOrder = table.RightOrder,
                RightHolder = table.RightHolder,
                ClaimAmount = table.ClaimAmount,
                RegistrationDate = table.RegistrationDate,
                RegistrationNumber = table.RegistrationNumber,
                RegistrationCause = table.RegistrationCause,
                Status = table.Status ?? "active",
                Notes = table.Notes,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryRightTable MapToRegistryRightTable(RegistryRight model)
        {
            return new RegistryRightTable
            {
                Id = model.Id,
                RegistryDocumentId = model.RegistryDocumentId,
                PropertyId = model.PropertyId,
                RightType = model.RightType,
                RightOrder = model.RightOrder,
                RightHolder = model.RightHolder,
                ClaimAmount = model.ClaimAmount,
                RegistrationDate = model.RegistrationDate,
                RegistrationNumber = model.RegistrationNumber,
                RegistrationCause = model.RegistrationCause,
                Status = model.Status,
                Notes = model.Notes,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        #endregion
    }

    #region Supabase Table Models

    /// <summary>
    /// Supabase registry_documents 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_documents")]
    internal class RegistryDocumentTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("file_path")]
        public string? FilePath { get; set; }

        [Postgrest.Attributes.Column("file_name")]
        public string? FileName { get; set; }

        [Postgrest.Attributes.Column("file_size")]
        public long? FileSize { get; set; }

        [Postgrest.Attributes.Column("ocr_status")]
        public string? OcrStatus { get; set; }

        [Postgrest.Attributes.Column("ocr_processed_at")]
        public DateTime? OcrProcessedAt { get; set; }

        [Postgrest.Attributes.Column("ocr_error")]
        public string? OcrError { get; set; }

        [Postgrest.Attributes.Column("registry_type")]
        public string? RegistryType { get; set; }

        [Postgrest.Attributes.Column("registry_number")]
        public string? RegistryNumber { get; set; }

        [Postgrest.Attributes.Column("extracted_data")]
        public string? ExtractedData { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_owners 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_owners")]
    internal class RegistryOwnerTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("registry_document_id")]
        public Guid? RegistryDocumentId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("owner_name")]
        public string? OwnerName { get; set; }

        [Postgrest.Attributes.Column("owner_regno")]
        public string? OwnerRegNo { get; set; }

        [Postgrest.Attributes.Column("share_ratio")]
        public string? ShareRatio { get; set; }

        [Postgrest.Attributes.Column("registration_date")]
        public DateTime? RegistrationDate { get; set; }

        [Postgrest.Attributes.Column("registration_cause")]
        public string? RegistrationCause { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_rights 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_rights")]
    internal class RegistryRightTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("registry_document_id")]
        public Guid? RegistryDocumentId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("right_type")]
        public string? RightType { get; set; }

        [Postgrest.Attributes.Column("right_order")]
        public int? RightOrder { get; set; }

        [Postgrest.Attributes.Column("right_holder")]
        public string? RightHolder { get; set; }

        [Postgrest.Attributes.Column("claim_amount")]
        public decimal? ClaimAmount { get; set; }

        [Postgrest.Attributes.Column("registration_date")]
        public DateTime? RegistrationDate { get; set; }

        [Postgrest.Attributes.Column("registration_number")]
        public string? RegistrationNumber { get; set; }

        [Postgrest.Attributes.Column("registration_cause")]
        public string? RegistrationCause { get; set; }

        [Postgrest.Attributes.Column("status")]
        public string? Status { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    #endregion
}

