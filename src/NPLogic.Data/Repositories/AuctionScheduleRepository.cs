using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 경매 일정 Repository
    /// </summary>
    public class AuctionScheduleRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public AuctionScheduleRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 전체 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 상태별 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetByStatusAsync(string status)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.Status == status)
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 기간별 경매 일정 조회
        /// </summary>
        public async Task<List<AuctionSchedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<AuctionScheduleTable>()
                    .Where(x => x.AuctionDate >= startDate)
                    .Where(x => x.AuctionDate <= endDate)
                    .Order(x => x.AuctionDate, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToAuctionSchedule).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경매 일정 생성
        /// </summary>
        public async Task<AuctionSchedule> CreateAsync(AuctionSchedule schedule)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAuctionScheduleTable(schedule);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AuctionScheduleTable>().Insert(table);
                return MapToAuctionSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경매 일정 수정
        /// </summary>
        public async Task<AuctionSchedule> UpdateAsync(AuctionSchedule schedule)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToAuctionScheduleTable(schedule);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client.From<AuctionScheduleTable>()
                    .Where(x => x.Id == schedule.Id)
                    .Update(table);
                return MapToAuctionSchedule(response.Models.First());
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 경매 일정 삭제
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<AuctionScheduleTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"경매 일정 삭제 실패: {ex.Message}", ex);
            }
        }

        // ========== Mapper ==========

        private AuctionSchedule MapToAuctionSchedule(AuctionScheduleTable t) => new AuctionSchedule
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            AuctionNumber = t.AuctionNumber,
            AuctionDate = t.AuctionDate,
            BidDate = t.BidDate,
            MinimumBid = t.MinimumBid,
            SalePrice = t.SalePrice,
            Status = t.Status ?? "scheduled",
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private AuctionScheduleTable MapToAuctionScheduleTable(AuctionSchedule a) => new AuctionScheduleTable
        {
            Id = a.Id,
            PropertyId = a.PropertyId,
            AuctionNumber = a.AuctionNumber,
            AuctionDate = a.AuctionDate,
            BidDate = a.BidDate,
            MinimumBid = a.MinimumBid,
            SalePrice = a.SalePrice,
            Status = a.Status,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    // ========== Table Class ==========

    [Postgrest.Attributes.Table("auction_schedules")]
    internal class AuctionScheduleTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)] public Guid Id { get; set; }
        [Postgrest.Attributes.Column("property_id")] public Guid? PropertyId { get; set; }
        [Postgrest.Attributes.Column("auction_number")] public string? AuctionNumber { get; set; }
        [Postgrest.Attributes.Column("auction_date")] public DateTime? AuctionDate { get; set; }
        [Postgrest.Attributes.Column("bid_date")] public DateTime? BidDate { get; set; }
        [Postgrest.Attributes.Column("minimum_bid")] public decimal? MinimumBid { get; set; }
        [Postgrest.Attributes.Column("sale_price")] public decimal? SalePrice { get; set; }
        [Postgrest.Attributes.Column("status")] public string? Status { get; set; }
        [Postgrest.Attributes.Column("created_at")] public DateTime CreatedAt { get; set; }
        [Postgrest.Attributes.Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}

