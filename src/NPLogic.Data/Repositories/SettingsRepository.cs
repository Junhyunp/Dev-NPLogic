using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 설정 관리 Repository
    /// </summary>
    public class SettingsRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public SettingsRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        // ========== Calculation Formulas ==========

        public async Task<List<CalculationFormula>> GetFormulasAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<CalculationFormulaTable>()
                    .Order(x => x.FormulaName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToFormula).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"수식 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<List<CalculationFormula>> GetFormulasByApplicabilityAsync(string appliesTo)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<CalculationFormulaTable>()
                    .Where(x => x.AppliesTo == appliesTo)
                    .Order(x => x.FormulaName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToFormula).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"수식 목록 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<CalculationFormula> CreateFormulaAsync(CalculationFormula formula)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToFormulaTable(formula);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<CalculationFormulaTable>().Insert(table);
                return MapToFormula(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"수식 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<CalculationFormula> UpdateFormulaAsync(CalculationFormula formula)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToFormulaTable(formula);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<CalculationFormulaTable>()
                    .Where(x => x.Id == formula.Id)
                    .Update(table);
                return MapToFormula(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"수식 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteFormulaAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<CalculationFormulaTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"수식 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Data Mappings (Settings table) ==========

        public async Task<List<DataMapping>> GetDataMappingsAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<SettingsTable>()
                    .Where(x => x.SettingType == "mapping")
                    .Order(x => x.SettingKey, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToDataMapping).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터 매핑 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<DataMapping> CreateDataMappingAsync(DataMapping mapping)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToSettingsTable(mapping);
                table.SettingType = "mapping";
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<SettingsTable>().Insert(table);
                return MapToDataMapping(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터 매핑 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<DataMapping> UpdateDataMappingAsync(DataMapping mapping)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToSettingsTable(mapping);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<SettingsTable>()
                    .Where(x => x.Id == mapping.Id)
                    .Update(table);
                return MapToDataMapping(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터 매핑 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteDataMappingAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<SettingsTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터 매핑 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== System Settings ==========

        public async Task<List<SystemSetting>> GetSystemSettingsAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<SystemSettingsTable>()
                    .Order(x => x.SettingKey, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToSystemSetting).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"시스템 설정 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<SystemSetting?> GetSystemSettingByKeyAsync(string key)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<SystemSettingsTable>()
                    .Where(x => x.SettingKey == key)
                    .Single();

                return response != null ? MapToSystemSetting(response) : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<SystemSetting> CreateSystemSettingAsync(SystemSetting setting)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToSystemSettingsTable(setting);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<SystemSettingsTable>().Insert(table);
                return MapToSystemSetting(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"시스템 설정 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<SystemSetting> UpdateSystemSettingAsync(SystemSetting setting)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToSystemSettingsTable(setting);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<SystemSettingsTable>()
                    .Where(x => x.Id == setting.Id)
                    .Update(table);
                return MapToSystemSetting(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"시스템 설정 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteSystemSettingAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<SystemSettingsTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"시스템 설정 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Mappers ==========

        private CalculationFormula MapToFormula(CalculationFormulaTable t) => new CalculationFormula
        {
            Id = t.Id,
            FormulaName = t.FormulaName ?? "",
            FormulaExpression = t.FormulaExpression ?? "",
            FormulaDescription = t.FormulaDescription,
            AppliesTo = t.AppliesTo,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private CalculationFormulaTable MapToFormulaTable(CalculationFormula f) => new CalculationFormulaTable
        {
            Id = f.Id,
            FormulaName = f.FormulaName,
            FormulaExpression = f.FormulaExpression,
            FormulaDescription = f.FormulaDescription,
            AppliesTo = f.AppliesTo,
            IsActive = f.IsActive,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        };

        private DataMapping MapToDataMapping(SettingsTable t) => new DataMapping
        {
            Id = t.Id,
            SettingKey = t.SettingKey ?? "",
            SettingValue = t.SettingValue,
            SettingType = t.SettingType,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private SettingsTable MapToSettingsTable(DataMapping m) => new SettingsTable
        {
            Id = m.Id,
            SettingKey = m.SettingKey,
            SettingValue = m.SettingValue,
            SettingType = m.SettingType ?? "mapping",
            Description = m.Description,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };

        private SystemSetting MapToSystemSetting(SystemSettingsTable t) => new SystemSetting
        {
            Id = t.Id,
            SettingKey = t.SettingKey ?? "",
            SettingValue = t.SettingValue,
            SettingType = t.SettingType ?? "STRING",
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private SystemSettingsTable MapToSystemSettingsTable(SystemSetting s) => new SystemSettingsTable
        {
            Id = s.Id,
            SettingKey = s.SettingKey,
            SettingValue = s.SettingValue,
            SettingType = s.SettingType,
            Description = s.Description,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    // ========== Table Classes ==========

    [Postgrest.Attributes.Table("calculation_formulas")]
    internal class CalculationFormulaTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("formula_name")] public string? FormulaName { get; set; }
        [Postgrest.Attributes.Column("formula_expression")] public string? FormulaExpression { get; set; }
        [Postgrest.Attributes.Column("formula_description")] public string? FormulaDescription { get; set; }
        [Postgrest.Attributes.Column("applies_to")] public string? AppliesTo { get; set; }
        [Postgrest.Attributes.Column("is_active")] public bool IsActive { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("settings")]
    internal class SettingsTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("setting_key")] public string? SettingKey { get; set; }
        [Postgrest.Attributes.Column("setting_value")] public string? SettingValue { get; set; }
        [Postgrest.Attributes.Column("setting_type")] public string? SettingType { get; set; }
        [Postgrest.Attributes.Column("description")] public string? Description { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("system_settings")]
    internal class SystemSettingsTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("setting_key")] public string? SettingKey { get; set; }
        [Postgrest.Attributes.Column("setting_value")] public string? SettingValue { get; set; }
        [Postgrest.Attributes.Column("setting_type")] public string? SettingType { get; set; }
        [Postgrest.Attributes.Column("description")] public string? Description { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}

