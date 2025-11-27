using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 공매 일정 Repository
    /// </summary>
    public class PublicSaleScheduleRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public PublicSaleScheduleRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        public async Task<List<PublicSaleSchedule>> GetAllAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var response = await client
                    .From<PublicSaleScheduleTable>()
                    .Order(x => x.SaleDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToPublicSaleSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<PublicSaleSchedule> CreateAsync(PublicSaleSchedule schedule)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToPublicSaleScheduleTable(schedule);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<PublicSaleScheduleTable>().Insert(table);
                return MapToPublicSaleSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 생성 실패: {ex.Message}", ex);
            }
        }

        public async Task<PublicSaleSchedule> UpdateAsync(PublicSaleSchedule schedule)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var table = MapToPublicSaleScheduleTable(schedule);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<PublicSaleScheduleTable>()
                    .Where(x => x.Id == schedule.Id)
                    .Update(table);
                return MapToPublicSaleSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 수정 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = _supabaseService.GetClient();
                await client.From<PublicSaleScheduleTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"공매 일정 삭제 실패: {ex.Message}", ex);
            }
        }

        private PublicSaleSchedule MapToPublicSaleSchedule(PublicSaleScheduleTable t) => new PublicSaleSchedule
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            SaleNumber = t.SaleNumber,
            SaleDate = t.SaleDate,
            MinimumBid = t.MinimumBid,
            SalePrice = t.SalePrice,
            Status = t.Status ?? "scheduled",
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private PublicSaleScheduleTable MapToPublicSaleScheduleTable(PublicSaleSchedule s) => new PublicSaleScheduleTable
        {
            Id = s.Id,
            PropertyId = s.PropertyId,
            SaleNumber = s.SaleNumber,
            SaleDate = s.SaleDate,
            MinimumBid = s.MinimumBid,
            SalePrice = s.SalePrice,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    [Postgrest.Attributes.Table("public_sale_schedules")]
    internal class PublicSaleScheduleTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("property_id")] public Guid? PropertyId { get; set; }
        [Postgrest.Attributes.Column("sale_number")] public string? SaleNumber { get; set; }
        [Postgrest.Attributes.Column("sale_date")] public DateTime? SaleDate { get; set; }
        [Postgrest.Attributes.Column("minimum_bid")] public decimal? MinimumBid { get; set; }
        [Postgrest.Attributes.Column("sale_price")] public decimal? SalePrice { get; set; }
        [Postgrest.Attributes.Column("status")] public string? Status { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}

