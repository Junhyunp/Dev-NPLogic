using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using Postgrest.Attributes;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 프로그램 시트 매핑 Repository
    /// </summary>
    public class ProgramSheetMappingRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public ProgramSheetMappingRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 프로그램의 모든 시트 매핑 조회
        /// </summary>
        public async Task<List<ProgramSheetMapping>> GetByProgramIdAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramSheetMappingTable>()
                    .Where(x => x.ProgramId == programId)
                    .Order(x => x.SheetType, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProgramSheetMappingRepository] GetByProgramIdAsync 실패: {ex.Message}");
                return new List<ProgramSheetMapping>();
            }
        }

        /// <summary>
        /// 특정 시트 매핑 조회
        /// </summary>
        public async Task<ProgramSheetMapping?> GetByProgramAndTypeAsync(Guid programId, string sheetType)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<ProgramSheetMappingTable>()
                    .Where(x => x.ProgramId == programId)
                    .Where(x => x.SheetType == sheetType)
                    .Get();

                var model = response.Models.FirstOrDefault();
                return model == null ? null : MapToModel(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProgramSheetMappingRepository] GetByProgramAndTypeAsync 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 시트 매핑 생성
        /// </summary>
        public async Task<ProgramSheetMapping> CreateAsync(ProgramSheetMapping mapping)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(mapping);
                
                var response = await client
                    .From<ProgramSheetMappingTable>()
                    .Insert(table);

                if (response.Models.Count == 0)
                    throw new Exception("시트 매핑 생성 실패: 응답이 없습니다.");

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"시트 매핑 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 시트 매핑 업데이트
        /// </summary>
        public async Task<ProgramSheetMapping> UpdateAsync(ProgramSheetMapping mapping)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(mapping);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<ProgramSheetMappingTable>()
                    .Where(x => x.Id == mapping.Id)
                    .Update(table);

                if (response.Models.Count == 0)
                    throw new Exception("시트 매핑 업데이트 실패: 응답이 없습니다.");

                return MapToModel(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"시트 매핑 업데이트 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 시트 매핑 Upsert (있으면 업데이트, 없으면 생성)
        /// </summary>
        public async Task<ProgramSheetMapping> UpsertAsync(ProgramSheetMapping mapping)
        {
            var existing = await GetByProgramAndTypeAsync(mapping.ProgramId, mapping.SheetType);
            
            if (existing != null)
            {
                mapping.Id = existing.Id;
                return await UpdateAsync(mapping);
            }
            else
            {
                return await CreateAsync(mapping);
            }
        }

        /// <summary>
        /// 여러 시트 매핑 일괄 저장
        /// </summary>
        public async Task SaveMappingsAsync(Guid programId, List<SheetMappingInfo> mappings, Guid userId, string fileName)
        {
            foreach (var mapping in mappings.Where(m => m.IsSelected))
            {
                var sheetMapping = new ProgramSheetMapping
                {
                    ProgramId = programId,
                    SheetType = ProgramSheetMapping.ToSheetTypeString(mapping.SelectedType),
                    SheetTypeDisplayName = mapping.SelectedTypeDisplay,
                    ExcelSheetName = mapping.ExcelSheetName,
                    ColumnMappings = mapping.ToColumnMappingsDictionary(),
                    RowCount = mapping.RowCount,
                    FileName = fileName,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = userId
                };

                await UpsertAsync(sheetMapping);
            }
        }

        /// <summary>
        /// 시트 매핑 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<ProgramSheetMappingTable>()
                    .Where(x => x.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"시트 매핑 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램의 모든 시트 매핑 삭제
        /// </summary>
        public async Task DeleteByProgramIdAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<ProgramSheetMappingTable>()
                    .Where(x => x.ProgramId == programId)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 시트 매핑 삭제 실패: {ex.Message}", ex);
            }
        }

        #region Mapping Methods

        private static ProgramSheetMapping MapToModel(ProgramSheetMappingTable table)
        {
            Dictionary<string, string>? columnMappings = null;
            if (!string.IsNullOrEmpty(table.ColumnMappingsJson))
            {
                try
                {
                    columnMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(table.ColumnMappingsJson);
                }
                catch
                {
                    columnMappings = null;
                }
            }

            return new ProgramSheetMapping
            {
                Id = table.Id,
                ProgramId = table.ProgramId,
                SheetType = table.SheetType ?? string.Empty,
                SheetTypeDisplayName = table.SheetTypeDisplay ?? ProgramSheetMapping.GetDisplayName(table.SheetType ?? ""),
                ExcelSheetName = table.ExcelSheetName,
                ColumnMappings = columnMappings,
                RowCount = table.RowCount,
                FileName = table.FileName,
                UploadedAt = table.UploadedAt ?? DateTime.MinValue,
                UploadedBy = table.UploadedBy
            };
        }

        private static ProgramSheetMappingTable MapToTable(ProgramSheetMapping model)
        {
            string? columnMappingsJson = null;
            if (model.ColumnMappings != null && model.ColumnMappings.Count > 0)
            {
                columnMappingsJson = JsonSerializer.Serialize(model.ColumnMappings);
            }

            return new ProgramSheetMappingTable
            {
                Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id,
                ProgramId = model.ProgramId,
                SheetType = model.SheetType,
                SheetTypeDisplay = model.SheetTypeDisplayName,
                ExcelSheetName = model.ExcelSheetName,
                ColumnMappingsJson = columnMappingsJson,
                RowCount = model.RowCount,
                FileName = model.FileName,
                UploadedAt = model.UploadedAt == DateTime.MinValue ? null : model.UploadedAt,
                UploadedBy = model.UploadedBy
            };
        }

        #endregion
    }

    /// <summary>
    /// Supabase 테이블 매핑용 클래스
    /// </summary>
    [Table("program_sheet_mappings")]
    internal class ProgramSheetMappingTable : Postgrest.Models.BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("program_id")]
        public Guid ProgramId { get; set; }

        [Column("sheet_type")]
        public string? SheetType { get; set; }

        [Column("sheet_type_display")]
        public string? SheetTypeDisplay { get; set; }

        [Column("excel_sheet_name")]
        public string? ExcelSheetName { get; set; }

        [Column("column_mappings")]
        public string? ColumnMappingsJson { get; set; }

        [Column("row_count")]
        public int RowCount { get; set; }

        [Column("file_name")]
        public string? FileName { get; set; }

        [Column("uploaded_at")]
        public DateTime? UploadedAt { get; set; }

        [Column("uploaded_by")]
        public Guid? UploadedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
