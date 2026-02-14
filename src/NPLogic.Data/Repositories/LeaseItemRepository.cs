using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Services;

namespace NPLogic.Data.Repositories
{
    public class LeaseItemRepository
    {
        private readonly SupabaseService _supabaseService;

        public LeaseItemRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        public async Task<List<LeaseItem>> GetByPropertyIdAsync(Guid propertyId, string leaseType)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<LeaseItemTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Where(x => x.LeaseType == leaseType)
                    .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LeaseItemRepository] 조회 실패: {ex.Message}");
                return new List<LeaseItem>();
            }
        }

        public async Task SaveAllAsync(Guid propertyId, string leaseType, List<LeaseItem> items)
        {
            var client = await _supabaseService.GetClientAsync();

            // 기존 데이터 삭제
            await client
                .From<LeaseItemTable>()
                .Where(x => x.PropertyId == propertyId)
                .Where(x => x.LeaseType == leaseType)
                .Delete();

            // 새 데이터 삽입
            if (items.Count > 0)
            {
                var tables = items.Select(item =>
                {
                    var table = MapToTable(item);
                    table.Id = Guid.NewGuid();
                    table.PropertyId = propertyId;
                    table.LeaseType = leaseType;
                    table.CreatedAt = DateTime.UtcNow;
                    table.UpdatedAt = DateTime.UtcNow;
                    return table;
                }).ToList();

                await client
                    .From<LeaseItemTable>()
                    .Insert(tables);
            }
        }

        private LeaseItem MapToModel(LeaseItemTable t) => new()
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            LeaseType = t.LeaseType ?? "residential",
            Category = t.Category ?? "",
            TenantName = t.TenantName ?? "",
            Notes = t.Notes ?? "",
            Area = t.Area,
            Deposit = t.Deposit,
            MonthlyRent = t.MonthlyRent,
            SmallDeposit = t.SmallDeposit,
            SeniorDeposit = t.SeniorDeposit,
            LeaseStartDate = t.LeaseStartDate,
            LeaseEndDate = t.LeaseEndDate,
            MoveInDate = t.MoveInDate,
            ConfirmationDate = t.ConfirmationDate,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private LeaseItemTable MapToTable(LeaseItem m) => new()
        {
            Id = m.Id,
            PropertyId = m.PropertyId ?? Guid.Empty,
            LeaseType = m.LeaseType,
            Category = m.Category,
            TenantName = m.TenantName,
            Notes = m.Notes,
            Area = m.Area,
            Deposit = m.Deposit,
            MonthlyRent = m.MonthlyRent,
            SmallDeposit = m.SmallDeposit,
            SeniorDeposit = m.SeniorDeposit,
            LeaseStartDate = m.LeaseStartDate,
            LeaseEndDate = m.LeaseEndDate,
            MoveInDate = m.MoveInDate,
            ConfirmationDate = m.ConfirmationDate,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };
    }

    [Postgrest.Attributes.Table("lease_items")]
    internal class LeaseItemTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid PropertyId { get; set; }

        [Postgrest.Attributes.Column("lease_type")]
        public string? LeaseType { get; set; }

        [Postgrest.Attributes.Column("category")]
        public string? Category { get; set; }

        [Postgrest.Attributes.Column("tenant_name")]
        public string? TenantName { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("area")]
        public decimal? Area { get; set; }

        [Postgrest.Attributes.Column("deposit")]
        public decimal? Deposit { get; set; }

        [Postgrest.Attributes.Column("monthly_rent")]
        public decimal? MonthlyRent { get; set; }

        [Postgrest.Attributes.Column("small_deposit")]
        public decimal? SmallDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_deposit")]
        public decimal? SeniorDeposit { get; set; }

        [Postgrest.Attributes.Column("lease_start_date")]
        public DateTime? LeaseStartDate { get; set; }

        [Postgrest.Attributes.Column("lease_end_date")]
        public DateTime? LeaseEndDate { get; set; }

        [Postgrest.Attributes.Column("move_in_date")]
        public DateTime? MoveInDate { get; set; }

        [Postgrest.Attributes.Column("confirmation_date")]
        public DateTime? ConfirmationDate { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
