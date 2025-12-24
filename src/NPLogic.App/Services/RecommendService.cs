using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// 유사물건 추천 서비스
    /// PythonBackendService를 통해 HTTP API로 추천을 요청합니다.
    /// </summary>
    public class RecommendService : IDisposable
    {
        private bool _disposed;
        private const string RecommendEndpoint = "/api/recommend";
        private readonly JsonSerializerOptions _jsonOptions;

        public RecommendService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };
        }

        /// <summary>
        /// 유사물건 추천을 실행합니다.
        /// </summary>
        /// <param name="subject">대상 물건 정보</param>
        /// <param name="options">추천 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>추천 결과</returns>
        public async Task<RecommendResult> RecommendAsync(
            RecommendSubject subject,
            RecommendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new RecommendOptions();

            try
            {
                // 서버가 실행 중인지 확인하고, 아니면 시작
                if (!await PythonBackendService.Instance.EnsureServerRunningAsync())
                {
                    return new RecommendResult
                    {
                        Success = false,
                        Error = "추천 서버를 시작할 수 없습니다."
                    };
                }

                var httpClient = PythonBackendService.Instance.GetHttpClient();

                // 요청 객체 생성
                var request = new RecommendRequest
                {
                    Subject = subject,
                    RuleIndex = options.RuleIndex,
                    SimilarLand = options.SimilarLand,
                    RegionScope = options.RegionScope,
                    TopK = options.TopK,
                    CandidatesSource = options.CandidatesSource,
                    CandidatesPath = options.CandidatesPath,
                    SupabaseUrl = options.SupabaseUrl,
                    SupabaseKey = options.SupabaseKey
                };

                // JSON 직렬화
                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // API 호출
                var response = await httpClient.PostAsync(RecommendEndpoint, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // 응답 파싱
                var result = JsonSerializer.Deserialize<RecommendResult>(responseContent, _jsonOptions);
                
                return result ?? new RecommendResult
                {
                    Success = false,
                    Error = "결과 파싱 실패"
                };
            }
            catch (TaskCanceledException)
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = "추천이 취소되었습니다."
                };
            }
            catch (Exception ex)
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = $"추천 실행 오류: {ex.Message}"
                };
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }

    #region Request/Response Models

    /// <summary>
    /// 추천 API 요청
    /// </summary>
    internal class RecommendRequest
    {
        [JsonPropertyName("subject")]
        public RecommendSubject Subject { get; set; } = new();

        [JsonPropertyName("rule_index")]
        public int? RuleIndex { get; set; }

        [JsonPropertyName("similar_land")]
        public bool SimilarLand { get; set; }

        [JsonPropertyName("region_scope")]
        public string RegionScope { get; set; } = "big";

        [JsonPropertyName("topk")]
        public int TopK { get; set; } = 10;

        [JsonPropertyName("candidates_source")]
        public string CandidatesSource { get; set; } = "supabase";

        [JsonPropertyName("candidates_path")]
        public string? CandidatesPath { get; set; }

        [JsonPropertyName("supabase_url")]
        public string? SupabaseUrl { get; set; }

        [JsonPropertyName("supabase_key")]
        public string? SupabaseKey { get; set; }
    }

    /// <summary>
    /// 추천 대상 물건 정보
    /// </summary>
    public class RecommendSubject
    {
        [JsonPropertyName("property_id")]
        public string? PropertyId { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("region_big")]
        public string? RegionBig { get; set; }

        [JsonPropertyName("region_mid")]
        public string? RegionMid { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("building_area")]
        public double? BuildingArea { get; set; }

        [JsonPropertyName("land_area")]
        public double? LandArea { get; set; }

        [JsonPropertyName("building_appraisal_price")]
        public decimal? BuildingAppraisalPrice { get; set; }

        [JsonPropertyName("land_appraisal_price")]
        public decimal? LandAppraisalPrice { get; set; }

        [JsonPropertyName("auction_date")]
        public string? AuctionDate { get; set; }
    }

    /// <summary>
    /// 추천 옵션
    /// </summary>
    public class RecommendOptions
    {
        /// <summary>
        /// 적용할 규칙 번호 (1-based, null이면 전체)
        /// </summary>
        public int? RuleIndex { get; set; }

        /// <summary>
        /// 토지 유사 모드 (PLANT_WAREHOUSE_ETC, OTHER_BIG용)
        /// </summary>
        public bool SimilarLand { get; set; }

        /// <summary>
        /// 지역 범위 (big=시도, mid=시군구)
        /// </summary>
        public string RegionScope { get; set; } = "big";

        /// <summary>
        /// 반환할 최대 건수
        /// </summary>
        public int TopK { get; set; } = 10;

        /// <summary>
        /// 후보군 데이터 소스 (supabase, json, excel)
        /// </summary>
        public string CandidatesSource { get; set; } = "supabase";

        /// <summary>
        /// 후보군 JSON/Excel 파일 경로
        /// </summary>
        public string? CandidatesPath { get; set; }

        /// <summary>
        /// Supabase URL
        /// </summary>
        public string? SupabaseUrl { get; set; }

        /// <summary>
        /// Supabase API 키
        /// </summary>
        public string? SupabaseKey { get; set; }
    }

    /// <summary>
    /// 추천 결과
    /// </summary>
    public class RecommendResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("subject")]
        public RecommendSubjectInfo? Subject { get; set; }

        [JsonPropertyName("rule_results")]
        public Dictionary<string, List<RecommendCase>>? RuleResults { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("config")]
        public RecommendConfig? Config { get; set; }
    }

    /// <summary>
    /// 추천 결과의 대상 물건 요약 정보
    /// </summary>
    public class RecommendSubjectInfo
    {
        [JsonPropertyName("property_id")]
        public string? PropertyId { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }
    }

    /// <summary>
    /// 추천된 경매 사례
    /// </summary>
    public class RecommendCase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("case_no")]
        public string? CaseNo { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("auction_date")]
        public string? AuctionDate { get; set; }

        [JsonPropertyName("appraisal_price")]
        public decimal? AppraisalPrice { get; set; }

        [JsonPropertyName("winning_price")]
        public decimal? WinningPrice { get; set; }

        [JsonPropertyName("building_area")]
        public double? BuildingArea { get; set; }

        [JsonPropertyName("land_area")]
        public double? LandArea { get; set; }

        [JsonPropertyName("building_unit_price")]
        public decimal? BuildingUnitPrice { get; set; }

        [JsonPropertyName("land_unit_price")]
        public decimal? LandUnitPrice { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("_rule_name")]
        public string? RuleName { get; set; }

        [JsonPropertyName("_rule_index")]
        public int? RuleIndex { get; set; }

        [JsonPropertyName("_category")]
        public string? Category { get; set; }

        /// <summary>
        /// 낙찰율 (낙찰가/감정가)
        /// </summary>
        public decimal? WinningRate => 
            AppraisalPrice > 0 ? (WinningPrice / AppraisalPrice) * 100 : null;
    }

    /// <summary>
    /// 추천 설정 정보
    /// </summary>
    public class RecommendConfig
    {
        [JsonPropertyName("rule_index")]
        public int? RuleIndex { get; set; }

        [JsonPropertyName("similar_land")]
        public bool SimilarLand { get; set; }

        [JsonPropertyName("region_scope")]
        public string? RegionScope { get; set; }

        [JsonPropertyName("topk")]
        public int TopK { get; set; }
    }

    #endregion
}
