using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NPLogic.Data.Services
{
    /// <summary>
    /// 소상공인 상권 정보 서비스
    /// Phase 7.1: 소상공인 상권 데이터 지도 연동
    /// data.go.kr 소상공인 상권정보 API 연동
    /// </summary>
    public class CommercialDistrictService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        // API 기본 URL (소상공인 상권정보 서비스)
        private const string BaseUrl = "https://apis.data.go.kr/B553077/api/open/sdsc";

        public CommercialDistrictService(string? apiKey = null)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }

        /// <summary>
        /// 좌표 기반 반경 내 상권 정보 조회
        /// </summary>
        /// <param name="latitude">위도</param>
        /// <param name="longitude">경도</param>
        /// <param name="radius">반경 (미터, 기본 500m)</param>
        public async Task<CommercialDistrictResult> GetNearbyDistrictsAsync(
            double latitude, 
            double longitude, 
            int radius = 500)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new CommercialDistrictResult
                {
                    Success = false,
                    ErrorMessage = "API 키가 설정되지 않았습니다. appsettings.json에 CommercialDistrictApiKey를 설정해주세요.",
                    Districts = new List<CommercialDistrict>()
                };
            }

            try
            {
                // 상권 정보 조회 API (반경 기준)
                var url = $"{BaseUrl}/storeListInRadius" +
                          $"?serviceKey={Uri.EscapeDataString(_apiKey)}" +
                          $"&radius={radius}" +
                          $"&cx={longitude}" +
                          $"&cy={latitude}" +
                          $"&type=json";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new CommercialDistrictResult
                    {
                        Success = false,
                        ErrorMessage = $"API 요청 실패: {response.StatusCode}",
                        Districts = new List<CommercialDistrict>()
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                return ParseStoreListResponse(json);
            }
            catch (Exception ex)
            {
                return new CommercialDistrictResult
                {
                    Success = false,
                    ErrorMessage = $"API 호출 오류: {ex.Message}",
                    Districts = new List<CommercialDistrict>()
                };
            }
        }

        /// <summary>
        /// 좌표 기반 상권 분석 정보 조회
        /// </summary>
        public async Task<CommercialAnalysisResult> GetDistrictAnalysisAsync(
            double latitude, 
            double longitude)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new CommercialAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "API 키가 설정되지 않았습니다."
                };
            }

            try
            {
                // 상권 분석 정보 조회 API
                var url = $"{BaseUrl}/storeZoneOne" +
                          $"?serviceKey={Uri.EscapeDataString(_apiKey)}" +
                          $"&cx={longitude}" +
                          $"&cy={latitude}" +
                          $"&type=json";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new CommercialAnalysisResult
                    {
                        Success = false,
                        ErrorMessage = $"API 요청 실패: {response.StatusCode}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                return ParseAnalysisResponse(json);
            }
            catch (Exception ex)
            {
                return new CommercialAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"API 호출 오류: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 업종별 점포 수 조회
        /// </summary>
        public async Task<IndustryStatsResult> GetIndustryStatsAsync(
            double latitude, 
            double longitude, 
            int radius = 500)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new IndustryStatsResult
                {
                    Success = false,
                    ErrorMessage = "API 키가 설정되지 않았습니다.",
                    Stats = new List<IndustryStat>()
                };
            }

            try
            {
                // 업종별 점포 수 API
                var url = $"{BaseUrl}/storeListInRadius" +
                          $"?serviceKey={Uri.EscapeDataString(_apiKey)}" +
                          $"&radius={radius}" +
                          $"&cx={longitude}" +
                          $"&cy={latitude}" +
                          $"&type=json";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new IndustryStatsResult
                    {
                        Success = false,
                        ErrorMessage = $"API 요청 실패: {response.StatusCode}",
                        Stats = new List<IndustryStat>()
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                return AggregateIndustryStats(json);
            }
            catch (Exception ex)
            {
                return new IndustryStatsResult
                {
                    Success = false,
                    ErrorMessage = $"API 호출 오류: {ex.Message}",
                    Stats = new List<IndustryStat>()
                };
            }
        }

        /// <summary>
        /// API 연결 테스트
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            if (string.IsNullOrEmpty(_apiKey)) return false;

            try
            {
                // 간단한 API 호출로 연결 테스트
                var url = $"{BaseUrl}/storeZoneInRadius" +
                          $"?serviceKey={Uri.EscapeDataString(_apiKey)}" +
                          $"&radius=100" +
                          $"&cx=126.9780&cy=37.5665" +
                          $"&type=json";

                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 점포 목록 응답 파싱
        /// </summary>
        private CommercialDistrictResult ParseStoreListResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var districts = new List<CommercialDistrict>();

                // API 응답 구조에 따라 파싱
                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("items", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var district = new CommercialDistrict
                        {
                            StoreName = GetStringValue(item, "bizesNm"),
                            IndustryName = GetStringValue(item, "indsLclsNm"),
                            IndustryDetail = GetStringValue(item, "indsMclsNm"),
                            Address = GetStringValue(item, "lnoAdr"),
                            RoadAddress = GetStringValue(item, "rdnmAdr"),
                            Latitude = GetDoubleValue(item, "lat"),
                            Longitude = GetDoubleValue(item, "lon")
                        };
                        districts.Add(district);
                    }
                }

                return new CommercialDistrictResult
                {
                    Success = true,
                    Districts = districts,
                    TotalCount = districts.Count
                };
            }
            catch (Exception ex)
            {
                return new CommercialDistrictResult
                {
                    Success = false,
                    ErrorMessage = $"응답 파싱 오류: {ex.Message}",
                    Districts = new List<CommercialDistrict>()
                };
            }
        }

        /// <summary>
        /// 상권 분석 응답 파싱
        /// </summary>
        private CommercialAnalysisResult ParseAnalysisResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("items", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        return new CommercialAnalysisResult
                        {
                            Success = true,
                            DistrictName = GetStringValue(item, "trarNm"),
                            DistrictCode = GetStringValue(item, "trarCd"),
                            DistrictType = GetStringValue(item, "trarClsNm"),
                            StoreCount = GetIntValue(item, "storCo"),
                            OpenCount = GetIntValue(item, "openStorCo"),
                            CloseCount = GetIntValue(item, "clsBizCo")
                        };
                    }
                }

                return new CommercialAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "상권 정보를 찾을 수 없습니다."
                };
            }
            catch (Exception ex)
            {
                return new CommercialAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"응답 파싱 오류: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 업종별 통계 집계
        /// </summary>
        private IndustryStatsResult AggregateIndustryStats(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var industryCount = new Dictionary<string, int>();

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("items", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var industry = GetStringValue(item, "indsLclsNm");
                        if (!string.IsNullOrEmpty(industry))
                        {
                            industryCount[industry] = industryCount.GetValueOrDefault(industry, 0) + 1;
                        }
                    }
                }

                var stats = new List<IndustryStat>();
                foreach (var kvp in industryCount)
                {
                    stats.Add(new IndustryStat
                    {
                        IndustryName = kvp.Key,
                        StoreCount = kvp.Value
                    });
                }

                // 점포 수 기준 내림차순 정렬
                stats.Sort((a, b) => b.StoreCount.CompareTo(a.StoreCount));

                return new IndustryStatsResult
                {
                    Success = true,
                    Stats = stats,
                    TotalStores = stats.Count > 0 ? stats.Sum(s => s.StoreCount) : 0
                };
            }
            catch (Exception ex)
            {
                return new IndustryStatsResult
                {
                    Success = false,
                    ErrorMessage = $"통계 집계 오류: {ex.Message}",
                    Stats = new List<IndustryStat>()
                };
            }
        }

        private string GetStringValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) 
                ? prop.GetString() ?? "" 
                : "";
        }

        private double GetDoubleValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDouble();
                if (prop.ValueKind == JsonValueKind.String && 
                    double.TryParse(prop.GetString(), out var val))
                    return val;
            }
            return 0;
        }

        private int GetIntValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                if (prop.ValueKind == JsonValueKind.String && 
                    int.TryParse(prop.GetString(), out var val))
                    return val;
            }
            return 0;
        }
    }

    #region Result Models

    /// <summary>
    /// 상권 정보 결과
    /// </summary>
    public class CommercialDistrictResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<CommercialDistrict> Districts { get; set; } = new();
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 개별 상권(점포) 정보
    /// </summary>
    public class CommercialDistrict
    {
        public string? StoreName { get; set; }
        public string? IndustryName { get; set; }
        public string? IndustryDetail { get; set; }
        public string? Address { get; set; }
        public string? RoadAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// 상권 분석 결과
    /// </summary>
    public class CommercialAnalysisResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DistrictName { get; set; }
        public string? DistrictCode { get; set; }
        public string? DistrictType { get; set; }
        public int StoreCount { get; set; }
        public int OpenCount { get; set; }
        public int CloseCount { get; set; }
    }

    /// <summary>
    /// 업종별 통계 결과
    /// </summary>
    public class IndustryStatsResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<IndustryStat> Stats { get; set; } = new();
        public int TotalStores { get; set; }
    }

    /// <summary>
    /// 업종별 점포 수
    /// </summary>
    public class IndustryStat
    {
        public string? IndustryName { get; set; }
        public int StoreCount { get; set; }
    }

    #endregion
}










