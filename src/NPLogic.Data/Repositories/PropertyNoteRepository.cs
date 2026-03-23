using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 물건별 비고 Repository
    /// </summary>
    public class PropertyNoteRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public PropertyNoteRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 특정 물건의 전체 비고 조회
        /// </summary>
        public async Task<List<PropertyNote>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyNoteTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.TabName, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToPropertyNote).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"비고 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 특정 물건의 특정 탭 비고 조회
        /// </summary>
        public async Task<PropertyNote?> GetByPropertyAndTabAsync(Guid propertyId, string tabName)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyNoteTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Filter("tab_name", Postgrest.Constants.Operator.Equals, tabName)
                    .Get();

                var table = response.Models.FirstOrDefault();
                return table != null ? MapToPropertyNote(table) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"비고 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 비고 저장 (생성 또는 업데이트 - property_id + tab_name 유니크 제약에 의한 Upsert)
        /// </summary>
        public async Task<PropertyNote> UpsertAsync(PropertyNote note)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToTable(note);
                table.UpdatedAt = DateTime.UtcNow;

                if (table.Id == Guid.Empty)
                {
                    table.Id = Guid.NewGuid();
                }

                var response = await client
                    .From<PropertyNoteTable>()
                    .Upsert(table);

                var result = response.Models.FirstOrDefault();
                return result != null ? MapToPropertyNote(result) : note;
            }
            catch (Exception ex)
            {
                throw new Exception($"비고 저장 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 비고 삭제
        /// </summary>
        public async Task DeleteAsync(Guid noteId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<PropertyNoteTable>()
                    .Where(x => x.Id == noteId)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"비고 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Mapper ==========

        private PropertyNote MapToPropertyNote(PropertyNoteTable t) => new PropertyNote
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            TabName = t.TabName ?? string.Empty,
            NoteText = t.NoteText ?? string.Empty,
            UpdatedAt = t.UpdatedAt
        };

        private PropertyNoteTable MapToTable(PropertyNote n) => new PropertyNoteTable
        {
            Id = n.Id,
            PropertyId = n.PropertyId,
            TabName = n.TabName,
            NoteText = n.NoteText,
            UpdatedAt = n.UpdatedAt
        };
    }

    // ========== Table Class ==========

    [Postgrest.Attributes.Table("property_notes")]
    internal class PropertyNoteTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("property_id")] public Guid PropertyId { get; set; }
        [Postgrest.Attributes.Column("tab_name")] public string? TabName { get; set; }
        [Postgrest.Attributes.Column("note_text")] public string? NoteText { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
