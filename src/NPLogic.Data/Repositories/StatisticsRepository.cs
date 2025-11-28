using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 통계 데이터 조회를 위한 Repository
    /// </summary>
    public class StatisticsRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public StatisticsRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 물건 유형별 분포 조회
        /// </summary>
        public async Task<List<PropertyTypeDistribution>> GetPropertyTypeDistributionAsync(string? projectId = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var query = client.From<PropertyTable>().Select("*");

                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    query = query.Filter("project_id", Postgrest.Constants.Operator.Equals, projectId);
                }

                var response = await query.Get();

                return response.Models
                    .GroupBy(p => p.PropertyType ?? "기타")
                    .Select(g => new PropertyTypeDistribution
                    {
                        PropertyType = g.Key,
                        Count = g.Count(),
                        TotalAppraisalValue = g.Sum(p => p.AppraisalValue ?? 0)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 유형별 분포 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 상태별 물건 수 조회
        /// </summary>
        public async Task<List<StatusDistribution>> GetStatusDistributionAsync(string? projectId = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var query = client.From<PropertyTable>().Select("*");

                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    query = query.Filter("project_id", Postgrest.Constants.Operator.Equals, projectId);
                }

                var response = await query.Get();

                return response.Models
                    .GroupBy(p => p.Status ?? "pending")
                    .Select(g => new StatusDistribution
                    {
                        Status = g.Key,
                        StatusLabel = GetStatusLabel(g.Key),
                        Count = g.Count()
                    })
                    .OrderBy(x => GetStatusOrder(x.Status))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"상태별 분포 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 지역별 물건 분포 조회
        /// </summary>
        public async Task<List<RegionDistribution>> GetRegionDistributionAsync(string? projectId = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var query = client.From<PropertyTable>().Select("*");

                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    query = query.Filter("project_id", Postgrest.Constants.Operator.Equals, projectId);
                }

                var response = await query.Get();

                return response.Models
                    .GroupBy(p => ExtractRegion(p.AddressFull))
                    .Select(g => new RegionDistribution
                    {
                        Region = g.Key,
                        Count = g.Count(),
                        TotalAppraisalValue = g.Sum(p => p.AppraisalValue ?? 0),
                        AverageAppraisalValue = g.Average(p => p.AppraisalValue ?? 0)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"지역별 분포 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 월별 등록 추이 조회 (최근 6개월)
        /// </summary>
        public async Task<List<MonthlyTrend>> GetMonthlyTrendAsync(string? projectId = null, int months = 6)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var startDate = DateTime.Now.AddMonths(-months);
                
                var query = client.From<PropertyTable>()
                    .Select("*")
                    .Filter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, startDate.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    query = query.Filter("project_id", Postgrest.Constants.Operator.Equals, projectId);
                }

                var response = await query.Get();

                // 월별 그룹화
                var grouped = response.Models
                    .Where(p => p.CreatedAt != default)
                    .GroupBy(p => new { Year = p.CreatedAt.Year, Month = p.CreatedAt.Month })
                    .Select(g => new MonthlyTrend
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        YearMonth = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count(),
                        TotalAppraisalValue = g.Sum(p => p.AppraisalValue ?? 0)
                    })
                    .ToList();

                // 빈 월 채우기
                var result = new List<MonthlyTrend>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var date = DateTime.Now.AddMonths(-i);
                    var existing = grouped.FirstOrDefault(g => g.Year == date.Year && g.Month == date.Month);
                    
                    result.Add(existing ?? new MonthlyTrend
                    {
                        Year = date.Year,
                        Month = date.Month,
                        YearMonth = $"{date.Year}-{date.Month:D2}",
                        Count = 0,
                        TotalAppraisalValue = 0
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"월별 추이 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 평균 지표 조회
        /// </summary>
        public async Task<AverageMetrics> GetAverageMetricsAsync(string? projectId = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var query = client.From<PropertyTable>().Select("*");

                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    query = query.Filter("project_id", Postgrest.Constants.Operator.Equals, projectId);
                }

                var response = await query.Get();
                var properties = response.Models;

                var totalCount = properties.Count;
                var completedCount = properties.Count(p => p.Status == "completed");

                // Evaluations 조회
                var evalQuery = client.From<EvaluationTable>().Select("*");
                var evalResponse = await evalQuery.Get();
                var evaluations = evalResponse.Models;

                // 해당 프로젝트의 물건 ID 목록
                var propertyIds = properties.Select(p => p.Id).ToHashSet();
                var projectEvaluations = evaluations.Where(e => e.PropertyId.HasValue && propertyIds.Contains(e.PropertyId.Value)).ToList();

                return new AverageMetrics
                {
                    TotalCount = totalCount,
                    CompletedCount = completedCount,
                    CompletionRate = totalCount > 0 ? (double)completedCount / totalCount * 100 : 0,
                    AverageAppraisalValue = properties.Any() ? properties.Average(p => p.AppraisalValue ?? 0) : 0,
                    AverageMinimumBid = properties.Any() ? properties.Average(p => p.MinimumBid ?? 0) : 0,
                    AverageSalePrice = properties.Where(p => p.SalePrice.HasValue).Any() 
                        ? properties.Where(p => p.SalePrice.HasValue).Average(p => p.SalePrice!.Value) 
                        : 0,
                    AverageRecoveryRate = projectEvaluations.Any() && projectEvaluations.Any(e => e.RecoveryRate.HasValue)
                        ? projectEvaluations.Where(e => e.RecoveryRate.HasValue).Average(e => e.RecoveryRate!.Value)
                        : 0,
                    TotalAppraisalValue = properties.Sum(p => p.AppraisalValue ?? 0),
                    EvaluationCount = projectEvaluations.Count
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"평균 지표 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로젝트 목록 조회
        /// </summary>
        public async Task<List<string>> GetProjectListAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client.From<PropertyTable>().Select("project_id").Get();

                return response.Models
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProjectId))
                    .Select(p => p.ProjectId!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로젝트 목록 조회 실패: {ex.Message}", ex);
            }
        }

        #region Helper Methods

        private static string GetStatusLabel(string status)
        {
            return status switch
            {
                "pending" => "대기",
                "processing" => "진행중",
                "completed" => "완료",
                _ => status
            };
        }

        private static int GetStatusOrder(string status)
        {
            return status switch
            {
                "pending" => 0,
                "processing" => 1,
                "completed" => 2,
                _ => 3
            };
        }

        private static string ExtractRegion(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "기타";

            // 주소에서 시/도 추출
            var parts = address.Split(' ');
            if (parts.Length > 0)
            {
                var first = parts[0];
                // 광역시/특별시/도 추출
                if (first.EndsWith("특별시") || first.EndsWith("광역시") || first.EndsWith("도"))
                {
                    // 서울특별시 -> 서울, 부산광역시 -> 부산
                    if (first.EndsWith("특별시"))
                        return first.Replace("특별시", "");
                    if (first.EndsWith("광역시"))
                        return first.Replace("광역시", "");
                    return first;
                }
            }
            return "기타";
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// 물건 유형별 분포
    /// </summary>
    public class PropertyTypeDistribution
    {
        public string PropertyType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAppraisalValue { get; set; }
    }

    /// <summary>
    /// 상태별 분포
    /// </summary>
    public class StatusDistribution
    {
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// 지역별 분포
    /// </summary>
    public class RegionDistribution
    {
        public string Region { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAppraisalValue { get; set; }
        public decimal AverageAppraisalValue { get; set; }
    }

    /// <summary>
    /// 월별 추이
    /// </summary>
    public class MonthlyTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string YearMonth { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAppraisalValue { get; set; }
    }

    /// <summary>
    /// 평균 지표
    /// </summary>
    public class AverageMetrics
    {
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate { get; set; }
        public decimal AverageAppraisalValue { get; set; }
        public decimal AverageMinimumBid { get; set; }
        public decimal AverageSalePrice { get; set; }
        public decimal AverageRecoveryRate { get; set; }
        public decimal TotalAppraisalValue { get; set; }
        public int EvaluationCount { get; set; }
    }

    #endregion

    #region Supabase Table Models

    /// <summary>
    /// Evaluations 테이블 모델
    /// </summary>
    [Postgrest.Attributes.Table("evaluations")]
    public class EvaluationTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id")]
        [Postgrest.Attributes.Column("id")]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("evaluation_type")]
        public string? EvaluationType { get; set; }

        [Postgrest.Attributes.Column("market_value")]
        public decimal? MarketValue { get; set; }

        [Postgrest.Attributes.Column("evaluated_value")]
        public decimal? EvaluatedValue { get; set; }

        [Postgrest.Attributes.Column("recovery_rate")]
        public decimal? RecoveryRate { get; set; }

        [Postgrest.Attributes.Column("evaluated_at")]
        public DateTime? EvaluatedAt { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }

    #endregion
}

