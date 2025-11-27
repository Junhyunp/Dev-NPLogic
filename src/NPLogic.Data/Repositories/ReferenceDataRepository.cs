using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 참조 데이터 Repository
    /// </summary>
    public class ReferenceDataRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public ReferenceDataRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        // ========== Courts ==========

        public async Task<List<Court>> GetCourtsAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<CourtTable>()
                    .Order(x => x.CourtName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToCourt).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"법원 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<Court> CreateCourtAsync(Court court)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToCourtTable(court);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<CourtTable>().Insert(table);
                return MapToCourt(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"법원 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<Court> UpdateCourtAsync(Court court)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToCourtTable(court);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<CourtTable>()
                    .Where(x => x.Id == court.Id)
                    .Update(table);
                return MapToCourt(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"법원 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteCourtAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client.From<CourtTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"법원 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Financial Institutions ==========

        public async Task<List<FinancialInstitution>> GetFinancialInstitutionsAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<FinancialInstitutionTable>()
                    .Order(x => x.InstitutionName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToFinancialInstitution).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"금융기관 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<FinancialInstitution> CreateFinancialInstitutionAsync(FinancialInstitution fi)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToFinancialInstitutionTable(fi);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<FinancialInstitutionTable>().Insert(table);
                return MapToFinancialInstitution(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"금융기관 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteFinancialInstitutionAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client.From<FinancialInstitutionTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"금융기관 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Appraisal Firms ==========

        public async Task<List<AppraisalFirm>> GetAppraisalFirmsAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<AppraisalFirmTable>()
                    .Order(x => x.FirmName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToAppraisalFirm).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"감정평가기관 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<AppraisalFirm> CreateAppraisalFirmAsync(AppraisalFirm firm)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToAppraisalFirmTable(firm);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AppraisalFirmTable>().Insert(table);
                return MapToAppraisalFirm(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"감정평가기관 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteAppraisalFirmAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client.From<AppraisalFirmTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"감정평가기관 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Common Codes ==========

        public async Task<List<CommonCode>> GetCommonCodesAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<CommonCodeTable>()
                    .Order(x => x.CodeGroup, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToCommonCode).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"공통 코드 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<List<CommonCode>> GetCommonCodesByGroupAsync(string codeGroup)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<CommonCodeTable>()
                    .Where(x => x.CodeGroup == codeGroup)
                    .Order(x => x.SortOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToCommonCode).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"공통 코드 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<CommonCode> CreateCommonCodeAsync(CommonCode code)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToCommonCodeTable(code);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<CommonCodeTable>().Insert(table);
                return MapToCommonCode(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"공통 코드 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteCommonCodeAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client.From<CommonCodeTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"공통 코드 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Mappers ==========

        private Court MapToCourt(CourtTable t) => new Court
        {
            Id = t.Id, CourtCode = t.CourtCode ?? "", CourtName = t.CourtName ?? "",
            Region = t.Region, Address = t.Address, Phone = t.Phone,
            DiscountRate1 = t.DiscountRate1, DiscountRate2 = t.DiscountRate2,
            DiscountRate3 = t.DiscountRate3, DiscountRate4 = t.DiscountRate4,
            IsActive = t.IsActive, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private CourtTable MapToCourtTable(Court c) => new CourtTable
        {
            Id = c.Id, CourtCode = c.CourtCode, CourtName = c.CourtName,
            Region = c.Region, Address = c.Address, Phone = c.Phone,
            DiscountRate1 = c.DiscountRate1, DiscountRate2 = c.DiscountRate2,
            DiscountRate3 = c.DiscountRate3, DiscountRate4 = c.DiscountRate4,
            IsActive = c.IsActive, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
        };

        private FinancialInstitution MapToFinancialInstitution(FinancialInstitutionTable t) => new FinancialInstitution
        {
            Id = t.Id, InstitutionCode = t.InstitutionCode ?? "", InstitutionName = t.InstitutionName ?? "",
            InstitutionType = t.InstitutionType, Address = t.Address, Phone = t.Phone, SwiftCode = t.SwiftCode,
            IsActive = t.IsActive, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private FinancialInstitutionTable MapToFinancialInstitutionTable(FinancialInstitution f) => new FinancialInstitutionTable
        {
            Id = f.Id, InstitutionCode = f.InstitutionCode, InstitutionName = f.InstitutionName,
            InstitutionType = f.InstitutionType, Address = f.Address, Phone = f.Phone, SwiftCode = f.SwiftCode,
            IsActive = f.IsActive, CreatedAt = f.CreatedAt, UpdatedAt = f.UpdatedAt
        };

        private AppraisalFirm MapToAppraisalFirm(AppraisalFirmTable t) => new AppraisalFirm
        {
            Id = t.Id, FirmCode = t.FirmCode ?? "", FirmName = t.FirmName ?? "",
            Representative = t.Representative, Address = t.Address, Phone = t.Phone, Email = t.Email,
            LicenseNumber = t.LicenseNumber, IsActive = t.IsActive, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private AppraisalFirmTable MapToAppraisalFirmTable(AppraisalFirm a) => new AppraisalFirmTable
        {
            Id = a.Id, FirmCode = a.FirmCode, FirmName = a.FirmName,
            Representative = a.Representative, Address = a.Address, Phone = a.Phone, Email = a.Email,
            LicenseNumber = a.LicenseNumber, IsActive = a.IsActive, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt
        };

        private CommonCode MapToCommonCode(CommonCodeTable t) => new CommonCode
        {
            Id = t.Id, CodeGroup = t.CodeGroup ?? "", CodeValue = t.CodeValue ?? "", CodeName = t.CodeName ?? "",
            SortOrder = t.SortOrder, Description = t.Description, IsActive = t.IsActive,
            CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private CommonCodeTable MapToCommonCodeTable(CommonCode c) => new CommonCodeTable
        {
            Id = c.Id, CodeGroup = c.CodeGroup, CodeValue = c.CodeValue, CodeName = c.CodeName,
            SortOrder = c.SortOrder, Description = c.Description, IsActive = c.IsActive,
            CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
        };
    }

    // ========== Table Classes ==========

    [Postgrest.Attributes.Table("courts")]
    internal class CourtTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("court_code")] public string? CourtCode { get; set; }
        [Postgrest.Attributes.Column("court_name")] public string? CourtName { get; set; }
        [Postgrest.Attributes.Column("region")] public string? Region { get; set; }
        [Postgrest.Attributes.Column("address")] public string? Address { get; set; }
        [Postgrest.Attributes.Column("phone")] public string? Phone { get; set; }
        [Postgrest.Attributes.Column("discount_rate_1")] public decimal? DiscountRate1 { get; set; }
        [Postgrest.Attributes.Column("discount_rate_2")] public decimal? DiscountRate2 { get; set; }
        [Postgrest.Attributes.Column("discount_rate_3")] public decimal? DiscountRate3 { get; set; }
        [Postgrest.Attributes.Column("discount_rate_4")] public decimal? DiscountRate4 { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("financial_institutions")]
    internal class FinancialInstitutionTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("institution_code")] public string? InstitutionCode { get; set; }
        [Postgrest.Attributes.Column("institution_name")] public string? InstitutionName { get; set; }
        [Postgrest.Attributes.Column("institution_type")] public string? InstitutionType { get; set; }
        [Postgrest.Attributes.Column("address")] public string? Address { get; set; }
        [Postgrest.Attributes.Column("phone")] public string? Phone { get; set; }
        [Postgrest.Attributes.Column("swift_code")] public string? SwiftCode { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("appraisal_firms")]
    internal class AppraisalFirmTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("firm_code")] public string? FirmCode { get; set; }
        [Postgrest.Attributes.Column("firm_name")] public string? FirmName { get; set; }
        [Postgrest.Attributes.Column("representative")] public string? Representative { get; set; }
        [Postgrest.Attributes.Column("address")] public string? Address { get; set; }
        [Postgrest.Attributes.Column("phone")] public string? Phone { get; set; }
        [Postgrest.Attributes.Column("email")] public string? Email { get; set; }
        [Postgrest.Attributes.Column("license_number")] public string? LicenseNumber { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("common_codes")]
    internal class CommonCodeTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("code_group")] public string? CodeGroup { get; set; }
        [Postgrest.Attributes.Column("code_value")] public string? CodeValue { get; set; }
        [Postgrest.Attributes.Column("code_name")] public string? CodeName { get; set; }
        [Postgrest.Attributes.Column("sort_order")] public int SortOrder { get; set; }
        [Postgrest.Attributes.Column("description")] public string? Description { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}

