using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NPLogic.Services
{
    /// <summary>
    /// 브이월드(Vworld) API 서비스
    /// 주소를 PNU(필지고유번호)로 변환
    /// </summary>
    public class VworldService
    {
        private readonly HttpClient _httpClient;
        private string? _vworldApiKey;

        private const string VworldSearchUrl = "https://api.vworld.kr/req/search";

        // 시도 약어 → 정식 명칭 매핑
        private static readonly Dictionary<string, string> SidoAbbrevMap = new()
        {
            { "전북", "전북특별자치도" },
            { "강원", "강원특별자치도" },
            { "서울", "서울특별시" },
            { "부산", "부산광역시" },
            { "대구", "대구광역시" },
            { "인천", "인천광역시" },
            { "광주", "광주광역시" },
            { "대전", "대전광역시" },
            { "울산", "울산광역시" },
            { "세종", "세종특별자치시" },
            { "경기", "경기도" },
            { "충북", "충청북도" },
            { "충남", "충청남도" },
            { "전남", "전라남도" },
            { "경북", "경상북도" },
            { "경남", "경상남도" },
            { "제주", "제주특별자치도" }
        };

        public VworldService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            LoadApiKey();
        }

        /// <summary>
        /// API 키 로드 (appsettings.json 또는 환경 변수)
        /// </summary>
        private void LoadApiKey()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var configPath = Path.Combine(basePath, "appsettings.json");

                if (File.Exists(configPath))
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: true)
                        .Build();

                    _vworldApiKey = config["Vworld:ApiKey"] ?? config["VworldApiKey"];
                }

                // 환경 변수에서도 확인
                if (string.IsNullOrEmpty(_vworldApiKey))
                {
                    _vworldApiKey = Environment.GetEnvironmentVariable("VWORLD_API_KEY");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VworldService] API 키 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// API 키 설정 (수동)
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            _vworldApiKey = apiKey;
        }

        /// <summary>
        /// API 키 설정 여부 확인
        /// </summary>
        public bool HasApiKey => !string.IsNullOrEmpty(_vworldApiKey);

        /// <summary>
        /// 주소로 PNU 검색
        /// </summary>
        /// <param name="address">검색할 주소</param>
        /// <returns>검색 결과 (PNU, 주소, 좌표)</returns>
        public async Task<VworldSearchResult?> SearchAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            if (string.IsNullOrEmpty(_vworldApiKey))
            {
                System.Diagnostics.Debug.WriteLine("[VworldService] API 키가 설정되지 않았습니다.");
                return null;
            }

            try
            {
                // 시도 약어 확장
                var expandedAddress = ExpandSidoAbbrev(address);

                // 리+지번 형태인지 확인하여 검색 범위 결정
                var size = LooksLikeRiJibun(expandedAddress) ? 50 : 10;

                // PARCEL(지번) 우선 검색, 실패 시 ROAD(도로명)로 폴백
                var parcelResult = await CallVworldApiAsync(expandedAddress, "PARCEL", size);
                var data = IsValidResponse(parcelResult) ? parcelResult : null;

                if (data == null)
                {
                    var roadResult = await CallVworldApiAsync(expandedAddress, "ROAD", size);
                    data = IsValidResponse(roadResult) ? roadResult : null;
                }

                if (data == null)
                    return null;

                var items = data.Response?.Result?.Items ?? new List<VworldItem>();
                var picked = PickBestItem(items, expandedAddress);

                if (picked == null)
                    return null;

                var addressObj = picked.Address ?? new VworldAddress();
                var roadAddr = addressObj.Road?.Trim() ?? "";
                var parcelAddr = addressObj.Parcel?.Trim() ?? "";
                var titleAddr = picked.Title?.Trim() ?? "";

                // 완전한 주소 선택 (시/도 정보가 포함된 주소 우선)
                var fullAddress =
                    (!string.IsNullOrEmpty(parcelAddr) && IsFullAddress(parcelAddr) ? parcelAddr : null) ??
                    (!string.IsNullOrEmpty(roadAddr) && IsFullAddress(roadAddr) ? roadAddr : null) ??
                    parcelAddr ?? roadAddr ?? titleAddr ?? address;

                var pnu = picked.Id?.Trim() ?? "";
                var point = picked.Point ?? new VworldPoint();

                double.TryParse(point.Y, out var lat);
                double.TryParse(point.X, out var lng);

                return new VworldSearchResult
                {
                    Pnu = pnu,
                    Address = fullAddress,
                    Latitude = lat,
                    Longitude = lng
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VworldService] 주소 검색 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Vworld API 호출
        /// </summary>
        private async Task<VworldApiResponse?> CallVworldApiAsync(string address, string category, int size)
        {
            try
            {
                var url = $"{VworldSearchUrl}?service=search&request=search&version=2.0" +
                          $"&crs=EPSG:4326&size={size}&page=1&type=address" +
                          $"&query={Uri.EscapeDataString(address)}&category={Uri.EscapeDataString(category)}" +
                          $"&format=json&errorformat=json&key={Uri.EscapeDataString(_vworldApiKey!)}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VworldApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VworldService] API 호출 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 응답 유효성 검사
        /// </summary>
        private bool IsValidResponse(VworldApiResponse? data)
        {
            return data?.Response?.Status == "OK" &&
                   data.Response.Result?.Items != null &&
                   data.Response.Result.Items.Count > 0;
        }

        /// <summary>
        /// 시도 약어 확장 (예: "서울 강남구" → "서울특별시 강남구")
        /// </summary>
        private string ExpandSidoAbbrev(string address)
        {
            var trimmed = address.Trim();
            foreach (var (abbrev, full) in SidoAbbrevMap)
            {
                if (trimmed.StartsWith(abbrev + " ") || trimmed.StartsWith(abbrev + "\t"))
                {
                    return full + trimmed.Substring(abbrev.Length);
                }
            }
            return trimmed;
        }

        /// <summary>
        /// 리+지번 형태인지 확인 (예: "구어리 11-11")
        /// </summary>
        private bool LooksLikeRiJibun(string address)
        {
            var hasRi = Regex.IsMatch(address, @"[가-힣]+리(\s|$)");
            var hasJibun = Regex.IsMatch(address, @"\d+\s*-\s*\d+");
            var hasAdmin = Regex.IsMatch(address, @"[가-힣]+(특별시|광역시|특별자치시|특별자치도|자치시|자치도|도|시|군|구|읍|면|동)(\s|$)");

            return hasRi && hasJibun && !hasAdmin;
        }

        /// <summary>
        /// 완전한 주소인지 확인 (시/도 정보 포함)
        /// </summary>
        private bool IsFullAddress(string address)
        {
            return Regex.IsMatch(address, @"[가-힣]+(특별시|광역시|특별자치시|특별자치도|자치시|자치도|도|시|군|구|읍|면|동)(\s|$)");
        }

        /// <summary>
        /// 검색 결과 중 가장 적합한 항목 선택
        /// </summary>
        private VworldItem? PickBestItem(List<VworldItem> items, string address)
        {
            if (items == null || items.Count == 0)
                return null;

            if (items.Count == 1)
                return items[0];

            var queryNorm = Normalize(address);
            var tokens = new List<string>();

            // 지번 토큰 추출
            var jibunMatch = Regex.Match(address, @"\d+\s*-\s*\d+");
            if (jibunMatch.Success)
                tokens.Add(Normalize(jibunMatch.Value));

            // 리 토큰 추출
            var riMatch = Regex.Match(address, @"([가-힣]+리)");
            if (riMatch.Success)
                tokens.Add(Normalize(riMatch.Groups[1].Value));

            // 지번 숫자 부분
            var jibunParts = jibunMatch.Success
                ? Normalize(jibunMatch.Value).Split('-').Where(p => !string.IsNullOrEmpty(p)).ToList()
                : new List<string>();

            VworldItem? best = items[0];
            int bestScore = ScoreItem(best, queryNorm, tokens, jibunParts);

            for (int i = 1; i < items.Count; i++)
            {
                var score = ScoreItem(items[i], queryNorm, tokens, jibunParts);
                if (score > bestScore)
                {
                    best = items[i];
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>
        /// 항목 점수 계산
        /// </summary>
        private int ScoreItem(VworldItem item, string queryNorm, List<string> tokens, List<string> jibunParts)
        {
            var candidate = GetCandidateString(item);
            int score = 0;

            // 전체 쿼리가 포함되면 최우선
            if (!string.IsNullOrEmpty(candidate) && !string.IsNullOrEmpty(queryNorm) && candidate.Contains(queryNorm))
                score += 10;

            // 토큰 매칭
            foreach (var token in tokens)
            {
                if (!string.IsNullOrEmpty(token) && candidate.Contains(token))
                    score += 4;
            }

            // 지번 숫자 부분 매칭
            foreach (var part in jibunParts)
            {
                if (!string.IsNullOrEmpty(part) && candidate.Contains(part))
                    score += 1;
            }

            return score;
        }

        /// <summary>
        /// 항목에서 후보 문자열 추출
        /// </summary>
        private string GetCandidateString(VworldItem item)
        {
            var address = item.Address ?? new VworldAddress();
            var road = Normalize(address.Road);
            var parcel = Normalize(address.Parcel);
            var title = Normalize(item.Title);

            return !string.IsNullOrEmpty(road) ? road :
                   !string.IsNullOrEmpty(parcel) ? parcel :
                   title ?? "";
        }

        /// <summary>
        /// 문자열 정규화 (공백 제거)
        /// </summary>
        private string Normalize(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return Regex.Replace(s, @"\s+", "").Trim();
        }
    }

    /// <summary>
    /// Vworld 검색 결과
    /// </summary>
    public class VworldSearchResult
    {
        public string Pnu { get; set; } = "";
        public string Address { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        /// <summary>
        /// PNU가 유효한지 확인 (19자리)
        /// </summary>
        public bool IsValidPnu => !string.IsNullOrEmpty(Pnu) && Pnu.Length == 19;
    }

    // ========== Vworld API 응답 모델 ==========

    public class VworldApiResponse
    {
        [JsonPropertyName("response")]
        public VworldResponseBody? Response { get; set; }
    }

    public class VworldResponseBody
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("result")]
        public VworldResult? Result { get; set; }
    }

    public class VworldResult
    {
        [JsonPropertyName("items")]
        public List<VworldItem>? Items { get; set; }
    }

    public class VworldItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("address")]
        public VworldAddress? Address { get; set; }

        [JsonPropertyName("point")]
        public VworldPoint? Point { get; set; }
    }

    public class VworldAddress
    {
        [JsonPropertyName("road")]
        public string? Road { get; set; }

        [JsonPropertyName("parcel")]
        public string? Parcel { get; set; }
    }

    public class VworldPoint
    {
        [JsonPropertyName("x")]
        public string? X { get; set; }

        [JsonPropertyName("y")]
        public string? Y { get; set; }
    }
}
