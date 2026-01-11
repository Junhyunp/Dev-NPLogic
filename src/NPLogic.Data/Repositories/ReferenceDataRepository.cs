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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
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
                var client = await _supabaseService.GetClientAsync();
                await client.From<CommonCodeTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"공통 코드 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Legal Application Rates (C-002) ==========

        public async Task<List<LegalApplicationRate>> GetLegalApplicationRatesAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<LegalApplicationRateTable>()
                    .Order(x => x.PropertyType, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToLegalApplicationRate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"법률적용률 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<LegalApplicationRate> CreateLegalApplicationRateAsync(LegalApplicationRate rate)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToLegalApplicationRateTable(rate);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<LegalApplicationRateTable>().Insert(table);
                return MapToLegalApplicationRate(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"법률적용률 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<LegalApplicationRate> UpdateLegalApplicationRateAsync(LegalApplicationRate rate)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToLegalApplicationRateTable(rate);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<LegalApplicationRateTable>()
                    .Where(x => x.Id == rate.Id)
                    .Update(table);
                return MapToLegalApplicationRate(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"법률적용률 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteLegalApplicationRateAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<LegalApplicationRateTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"법률적용률 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Lease Standards (C-003, C-004) ==========

        /// <summary>
        /// 소액임차보증금 최우선변제금 조회 (L-003)
        /// </summary>
        /// <param name="region">지역 (서울, 수도권, 광역시, 기타)</param>
        /// <param name="referenceDate">기준일 (근저당 설정일)</param>
        /// <param name="leaseType">임대차 유형 (residential/commercial)</param>
        /// <returns>해당 시점/지역의 소액임차보증금 기준</returns>
        public async Task<LeaseStandard?> GetSmallDepositAsync(string region, DateTime referenceDate, string leaseType = "residential")
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // 해당 지역 & 유형 & 기준일 이전에 시작된 기준 중 가장 최근 것 조회
                var response = await client.From<LeaseStandardTable>()
                    .Where(x => x.Region == region)
                    .Where(x => x.LeaseType == leaseType)
                    .Where(x => x.IsActive == true)
                    .Order(x => x.StartDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                // 기준일 이전에 시작된 기준 중 가장 최근 것 찾기
                var applicable = response.Models
                    .Select(MapToLeaseStandard)
                    .FirstOrDefault(s => s.StartDate <= referenceDate && 
                                        (s.EndDate == null || s.EndDate >= referenceDate));

                return applicable;
            }
            catch (Exception ex)
            {
                throw new Exception($"소액임차보증금 기준 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소액임차보증금 지역 목록 조회
        /// </summary>
        public async Task<List<string>> GetSmallDepositRegionsAsync()
        {
            try
            {
                var standards = await GetLeaseStandardsAsync("residential");
                return standards
                    .Select(s => s.Region)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"소액임차보증금 지역 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<List<LeaseStandard>> GetLeaseStandardsAsync(string? leaseType = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                if (!string.IsNullOrEmpty(leaseType))
                {
                    var response = await client.From<LeaseStandardTable>()
                        .Where(x => x.LeaseType == leaseType)
                        .Order(x => x.StartDate, Postgrest.Constants.Ordering.Descending)
                        .Get();
                    return response.Models.Select(MapToLeaseStandard).ToList();
                }
                else
                {
                    var response = await client.From<LeaseStandardTable>()
                        .Order(x => x.StartDate, Postgrest.Constants.Ordering.Descending)
                        .Get();
                    return response.Models.Select(MapToLeaseStandard).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"임대차 기준 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<LeaseStandard> CreateLeaseStandardAsync(LeaseStandard standard)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToLeaseStandardTable(standard);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<LeaseStandardTable>().Insert(table);
                return MapToLeaseStandard(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"임대차 기준 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<LeaseStandard> UpdateLeaseStandardAsync(LeaseStandard standard)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToLeaseStandardTable(standard);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<LeaseStandardTable>()
                    .Where(x => x.Id == standard.Id)
                    .Update(table);
                return MapToLeaseStandard(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"임대차 기준 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteLeaseStandardAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<LeaseStandardTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"임대차 기준 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Auction Cost Standards (C-005) ==========

        public async Task<List<AuctionCostStandard>> GetAuctionCostStandardsAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionCostStandardTable>()
                    .Order(x => x.CostType, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToAuctionCostStandard).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매비용 산정 기준 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<AuctionCostStandard> CreateAuctionCostStandardAsync(AuctionCostStandard standard)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAuctionCostStandardTable(standard);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AuctionCostStandardTable>().Insert(table);
                return MapToAuctionCostStandard(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"경매비용 기준 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<AuctionCostStandard> UpdateAuctionCostStandardAsync(AuctionCostStandard standard)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAuctionCostStandardTable(standard);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AuctionCostStandardTable>()
                    .Where(x => x.Id == standard.Id)
                    .Update(table);
                return MapToAuctionCostStandard(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"경매비용 기준 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteAuctionCostStandardAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<AuctionCostStandardTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매비용 기준 삭제 실패: {ex.Message}", ex);
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

        private LegalApplicationRate MapToLegalApplicationRate(LegalApplicationRateTable t) => new LegalApplicationRate
        {
            Id = t.Id, PropertyType = t.PropertyType ?? "", AppliedRate = t.AppliedRate,
            Description = t.Description, IsActive = t.IsActive, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private LegalApplicationRateTable MapToLegalApplicationRateTable(LegalApplicationRate r) => new LegalApplicationRateTable
        {
            Id = r.Id, PropertyType = r.PropertyType, AppliedRate = r.AppliedRate,
            Description = r.Description, IsActive = r.IsActive, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
        };

        private LeaseStandard MapToLeaseStandard(LeaseStandardTable t) => new LeaseStandard
        {
            Id = t.Id, LeaseType = t.LeaseType ?? "residential", Region = t.Region ?? "",
            StartDate = t.StartDate, EndDate = t.EndDate, DepositLimit = t.DepositLimit,
            CompensationAmount = t.CompensationAmount, Description = t.Description,
            IsActive = t.IsActive, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private LeaseStandardTable MapToLeaseStandardTable(LeaseStandard s) => new LeaseStandardTable
        {
            Id = s.Id, LeaseType = s.LeaseType, Region = s.Region,
            StartDate = s.StartDate, EndDate = s.EndDate, DepositLimit = s.DepositLimit,
            CompensationAmount = s.CompensationAmount, Description = s.Description,
            IsActive = s.IsActive, CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt
        };

        private AuctionCostStandard MapToAuctionCostStandard(AuctionCostStandardTable t) => new AuctionCostStandard
        {
            Id = t.Id, CostType = t.CostType ?? "", CalculationMethod = t.CalculationMethod,
            BaseAmount = t.BaseAmount, Rate = t.Rate, Description = t.Description,
            IsActive = t.IsActive, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
        };

        private AuctionCostStandardTable MapToAuctionCostStandardTable(AuctionCostStandard s) => new AuctionCostStandardTable
        {
            Id = s.Id, CostType = s.CostType, CalculationMethod = s.CalculationMethod,
            BaseAmount = s.BaseAmount, Rate = s.Rate, Description = s.Description,
            IsActive = s.IsActive, CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt
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

    [Postgrest.Attributes.Table("legal_application_rates")]
    internal class LegalApplicationRateTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("property_type")] public string? PropertyType { get; set; }
        [Postgrest.Attributes.Column("applied_rate")] public decimal AppliedRate { get; set; }
        [Postgrest.Attributes.Column("description")] public string? Description { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("lease_standards")]
    internal class LeaseStandardTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("lease_type")] public string? LeaseType { get; set; }
        [Postgrest.Attributes.Column("region")] public string? Region { get; set; }
        [Postgrest.Attributes.Column("start_date")] public DateTime StartDate { get; set; }
        [Postgrest.Attributes.Column("end_date")] public DateTime? EndDate { get; set; }
        [Postgrest.Attributes.Column("deposit_limit")] public decimal DepositLimit { get; set; }
        [Postgrest.Attributes.Column("compensation_amount")] public decimal CompensationAmount { get; set; }
        [Postgrest.Attributes.Column("description")] public string? Description { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("auction_cost_standards")]
    internal class AuctionCostStandardTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("cost_type")] public string? CostType { get; set; }
        [Postgrest.Attributes.Column("calculation_method")] public string? CalculationMethod { get; set; }
        [Postgrest.Attributes.Column("base_amount")] public decimal? BaseAmount { get; set; }
        [Postgrest.Attributes.Column("rate")] public decimal? Rate { get; set; }
        [Postgrest.Attributes.Column("description")] public string? Description { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}

