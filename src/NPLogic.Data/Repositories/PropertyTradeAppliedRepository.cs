using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Services;

namespace NPLogic.Data.Repositories
{
    public class PropertyTradeAppliedRepository
    {
        private readonly SupabaseService _supabaseService;

        public PropertyTradeAppliedRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 물건 ID로 적용된 실거래가 목록 조회
        /// </summary>
        public async Task<List<PropertyTradeApplied>> GetByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTradeAppliedTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Get();

                return response.Models.Select(MapToModel).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyTradeAppliedRepository] 조회 실패: {ex.Message}");
                return new List<PropertyTradeApplied>();
            }
        }

        /// <summary>
        /// 물건의 적용 실거래가 전체 저장 (DELETE + INSERT)
        /// 체크된 항목만 전달하면 됨
        /// </summary>
        public async Task SaveAllAsync(Guid propertyId, List<PropertyTradeApplied> items)
        {
            var client = await _supabaseService.GetClientAsync();

            // 기존 데이터 삭제
            await client
                .From<PropertyTradeAppliedTable>()
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
                    return table;
                }).ToList();

                await client
                    .From<PropertyTradeAppliedTable>()
                    .Insert(tables);
            }
        }

        private PropertyTradeApplied MapToModel(PropertyTradeAppliedTable t) => new()
        {
            Id = t.Id,
            PropertyId = t.PropertyId,
            DealDate = t.DealDate,
            DealAmount = t.DealAmount,
            Area = t.Area,
            Floor = t.Floor,
            CreatedAt = t.CreatedAt
        };

        private PropertyTradeAppliedTable MapToTable(PropertyTradeApplied m) => new()
        {
            Id = m.Id,
            PropertyId = m.PropertyId,
            DealDate = m.DealDate,
            DealAmount = m.DealAmount,
            Area = m.Area,
            Floor = m.Floor,
            CreatedAt = m.CreatedAt
        };
    }

    [Postgrest.Attributes.Table("property_trade_applied")]
    internal class PropertyTradeAppliedTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid PropertyId { get; set; }

        [Postgrest.Attributes.Column("deal_date")]
        public int DealDate { get; set; }

        [Postgrest.Attributes.Column("deal_amount")]
        public int DealAmount { get; set; }

        [Postgrest.Attributes.Column("area")]
        public double Area { get; set; }

        [Postgrest.Attributes.Column("floor")]
        public string? Floor { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
