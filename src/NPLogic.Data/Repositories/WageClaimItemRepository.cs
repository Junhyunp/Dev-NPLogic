using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Services;

namespace NPLogic.Data.Repositories
{
    public class WageClaimItemRepository
    {
        private readonly SupabaseService _supabaseService;

        public WageClaimItemRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        public async Task<List<WageClaimItem>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<WageClaimItemTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order("sequence_number", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WageClaimItemRepository] 조회 실패: {ex.Message}");
                return new List<WageClaimItem>();
            }
        }

        public async Task SaveAllAsync(Guid propertyId, List<WageClaimItem> items)
        {
            var client = await _supabaseService.GetClientAsync();

            // 기존 데이터 삭제
            await client
                .From<WageClaimItemTable>()
                .Where(x => x.PropertyId == propertyId)
                .Delete();

            // 새 데이터 삽입
            if (items.Count > 0)
            {
                var tables = items.Select(item =>
                {
                    var table = MapToTable(item);
                    table.Id = Guid.NewGuid();
                    table.PropertyId = propertyId;
                    table.CreatedAt = DateTime.UtcNow;
                    table.UpdatedAt = DateTime.UtcNow;
                    return table;
                }).ToList();

                await client
                    .From<WageClaimItemTable>()
                    .Insert(tables);
            }
        }

        private WageClaimItem MapToModel(WageClaimItemTable t) => new()
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            SequenceNumber = t.SequenceNumber,
            EmployeeName = t.EmployeeName ?? "",
            Notes = t.Notes ?? "",
            BirthDate = t.BirthDate,
            SeizureDate = t.SeizureDate,
            ClaimDate = t.ClaimDate,
            SeizureAmount = t.SeizureAmount,
            WageAmount = t.WageAmount,
            SeveranceAmount = t.SeveranceAmount,
            OtherAmount = t.OtherAmount,
            ThreeMonthWage = t.ThreeMonthWage,
            ThreeYearSeverance = t.ThreeYearSeverance,
            SubstitutePayment = t.SubstitutePayment,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private WageClaimItemTable MapToTable(WageClaimItem m) => new()
        {
            Id = m.Id,
            PropertyId = m.PropertyId ?? Guid.Empty,
            SequenceNumber = m.SequenceNumber,
            EmployeeName = m.EmployeeName,
            Notes = m.Notes,
            BirthDate = m.BirthDate,
            SeizureDate = m.SeizureDate,
            ClaimDate = m.ClaimDate,
            SeizureAmount = m.SeizureAmount,
            WageAmount = m.WageAmount,
            SeveranceAmount = m.SeveranceAmount,
            OtherAmount = m.OtherAmount,
            ThreeMonthWage = m.ThreeMonthWage,
            ThreeYearSeverance = m.ThreeYearSeverance,
            SubstitutePayment = m.SubstitutePayment,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };
    }

    [Postgrest.Attributes.Table("wage_claim_items")]
    internal class WageClaimItemTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid PropertyId { get; set; }

        [Postgrest.Attributes.Column("sequence_number")]
        public int SequenceNumber { get; set; }

        [Postgrest.Attributes.Column("employee_name")]
        public string? EmployeeName { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        [Postgrest.Attributes.Column("seizure_date")]
        public DateTime? SeizureDate { get; set; }

        [Postgrest.Attributes.Column("claim_date")]
        public DateTime? ClaimDate { get; set; }

        [Postgrest.Attributes.Column("seizure_amount")]
        public decimal? SeizureAmount { get; set; }

        [Postgrest.Attributes.Column("wage_amount")]
        public decimal? WageAmount { get; set; }

        [Postgrest.Attributes.Column("severance_amount")]
        public decimal? SeveranceAmount { get; set; }

        [Postgrest.Attributes.Column("other_amount")]
        public decimal? OtherAmount { get; set; }

        [Postgrest.Attributes.Column("three_month_wage")]
        public decimal? ThreeMonthWage { get; set; }

        [Postgrest.Attributes.Column("three_year_severance")]
        public decimal? ThreeYearSeverance { get; set; }

        [Postgrest.Attributes.Column("substitute_payment")]
        public decimal? SubstitutePayment { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
