using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 등기부등본정보 시트 데이터 Repository (데이터디스크 업로드용)
    /// </summary>
    public class RegistrySheetDataRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public RegistrySheetDataRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 물건 ID로 등기부등본정보 조회
        /// </summary>
        public async Task<List<RegistrySheetData>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistrySheetDataTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부등본정보 생성
        /// </summary>
        public async Task<RegistrySheetData> CreateAsync(RegistrySheetData data)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(data);
                table.Id = Guid.NewGuid();
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistrySheetDataTable>()
                    .Insert(table);

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부등본정보 수정
        /// </summary>
        public async Task<RegistrySheetData> UpdateAsync(RegistrySheetData data)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(data);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistrySheetDataTable>()
                    .Where(x => x.Id == data.Id)
                    .Update(table);

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본정보 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부등본정보 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistrySheetDataTable>()
                    .Where(x => x.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본정보 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 ID로 등기부등본정보 전체 삭제
        /// </summary>
        public async Task DeleteByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistrySheetDataTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본정보 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Upsert - 차주일련번호 + 물건번호 + 지번번호 기준
        /// </summary>
        public async Task<RegistrySheetData> UpsertAsync(RegistrySheetData data)
        {
            try
            {
                RegistrySheetData? existing = null;

                // 차주일련번호 + 물건번호 + 지번번호로 기존 데이터 조회
                if (!string.IsNullOrEmpty(data.BorrowerNumber) && !string.IsNullOrEmpty(data.PropertyNumber))
                {
                    var client = await _supabaseService.GetClientAsync();
                    var query = client
                        .From<RegistrySheetDataTable>()
                        .Where(x => x.BorrowerNumber == data.BorrowerNumber)
                        .Where(x => x.PropertyNumber == data.PropertyNumber);

                    if (!string.IsNullOrEmpty(data.JibunNumber))
                    {
                        query = query.Where(x => x.JibunNumber == data.JibunNumber);
                    }

                    var response = await query.Single();
                    existing = response != null ? MapToModel(response) : null;
                }

                if (existing != null)
                {
                    data.Id = existing.Id;
                    return await UpdateAsync(data);
                }
                else
                {
                    return await CreateAsync(data);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본정보 Upsert 실패: {ex.Message}", ex);
            }
        }

        // ========== 매핑 ==========

        private static RegistrySheetData MapToModel(RegistrySheetDataTable table)
        {
            return new RegistrySheetData
            {
                Id = table.Id,
                PropertyId = table.PropertyId,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                PropertyNumber = table.PropertyNumber,
                JibunNumber = table.JibunNumber,
                AddressProvince = table.AddressProvince,
                AddressCity = table.AddressCity,
                AddressDistrict = table.AddressDistrict,
                AddressDetail = table.AddressDetail,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private static RegistrySheetDataTable MapToTable(RegistrySheetData model)
        {
            return new RegistrySheetDataTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                BorrowerNumber = model.BorrowerNumber,
                BorrowerName = model.BorrowerName,
                PropertyNumber = model.PropertyNumber,
                JibunNumber = model.JibunNumber,
                AddressProvince = model.AddressProvince,
                AddressCity = model.AddressCity,
                AddressDistrict = model.AddressDistrict,
                AddressDetail = model.AddressDetail,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        /// <summary>
        /// 여러 물건의 등기부등본 데이터 일괄 삭제
        /// </summary>
        public async Task DeleteByPropertyIdsAsync(List<Guid> propertyIds)
        {
            if (propertyIds == null || propertyIds.Count == 0) return;

            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistrySheetDataTable>()
                    .Filter("property_id", Postgrest.Constants.Operator.In, propertyIds)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부등본 데이터 일괄 삭제 실패: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Supabase registry_sheet_data 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_sheet_data")]
    internal class RegistrySheetDataTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("property_number")]
        public string? PropertyNumber { get; set; }

        [Postgrest.Attributes.Column("jibun_number")]
        public string? JibunNumber { get; set; }

        [Postgrest.Attributes.Column("address_province")]
        public string? AddressProvince { get; set; }

        [Postgrest.Attributes.Column("address_city")]
        public string? AddressCity { get; set; }

        [Postgrest.Attributes.Column("address_district")]
        public string? AddressDistrict { get; set; }

        [Postgrest.Attributes.Column("address_detail")]
        public string? AddressDetail { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
