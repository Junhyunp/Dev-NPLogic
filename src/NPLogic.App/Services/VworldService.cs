using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// API 키는 MapService에서 가져옴 (Supabase Edge Function에서 로드)
    /// </summary>
    public class VworldService
    {
        private readonly HttpClient _httpClient;
        private readonly MapService? _mapService;
        private readonly NPLogic.Data.Services.SupabaseService? _supabaseService;
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

        public VworldService(MapService? mapService = null, NPLogic.Data.Services.SupabaseService? supabaseService = null)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _mapService = mapService;
            _supabaseService = supabaseService;
            LoadApiKey();
        }

        /// <summary>
        /// API 키 로드 (MapService → appsettings.json → 환경 변수 순서)
        /// </summary>
        private void LoadApiKey()
        {
            try
            {
                // 1. MapService에서 API 키 가져오기 (Supabase Edge Function에서 로드된 키)
                if (_mapService != null && _mapService.IsConfigLoaded)
                {
                    _vworldApiKey = _mapService.GetVworldApiKey();
                    if (!string.IsNullOrEmpty(_vworldApiKey))
                    {
                        System.Diagnostics.Debug.WriteLine("[VworldService] MapService에서 API 키 로드 완료");
                        return;
                    }
                }

                // 2. appsettings.json에서 로드 (fallback)
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

                // 3. 환경 변수에서도 확인 (fallback)
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
        /// API 키가 로드되었는지 확인하고, 없으면 MapService에서 다시 시도
        /// </summary>
        public void EnsureApiKeyLoaded()
        {
            if (!string.IsNullOrEmpty(_vworldApiKey))
                return;

            // MapService에서 다시 시도
            if (_mapService != null && _mapService.IsConfigLoaded)
            {
                _vworldApiKey = _mapService.GetVworldApiKey();
                if (!string.IsNullOrEmpty(_vworldApiKey))
                {
                    Debug.WriteLine("[VworldService] MapService에서 API 키 재로드 완료");
                }
            }
        }

        /// <summary>
        /// API 키 비동기 로드 (MapService Edge Function → appsettings → 환경변수)
        /// </summary>
        public async Task EnsureApiKeyLoadedAsync()
        {
            if (!string.IsNullOrEmpty(_vworldApiKey))
                return;

            // 1. MapService에서 동기 시도 (이미 로드된 경우)
            EnsureApiKeyLoaded();
            if (!string.IsNullOrEmpty(_vworldApiKey))
                return;

            // 2. MapService Edge Function 호출하여 키 로드
            if (_mapService != null && _supabaseService != null)
            {
                try
                {
                    var session = _supabaseService.GetSession();
                    if (session?.AccessToken != null)
                    {
                        await _mapService.LoadMapConfigAsync(session.AccessToken);
                        _vworldApiKey = _mapService.GetVworldApiKey();
                        if (!string.IsNullOrEmpty(_vworldApiKey))
                        {
                            Debug.WriteLine("[VworldService] Edge Function을 통해 API 키 로드 완료");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VworldService] Edge Function API 키 로드 실패: {ex.Message}");
                }
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

            // API 키가 로드되었는지 확인
            await EnsureApiKeyLoadedAsync();

            if (string.IsNullOrEmpty(_vworldApiKey))
            {
                Debug.WriteLine("[VworldService] API 키가 설정되지 않았습니다.");
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

        /// <summary>
        /// PNU로 필지 경계 폴리곤 좌표 조회 (VWORLD Data API)
        /// </summary>
        /// <param name="pnu">19자리 필지고유번호</param>
        /// <returns>폴리곤 좌표 배열 [[lat, lng], ...] 또는 null</returns>
        public async Task<List<double[]>?> GetParcelBoundaryAsync(string pnu)
        {
            if (string.IsNullOrWhiteSpace(pnu) || pnu.Length != 19)
                return null;

            await EnsureApiKeyLoadedAsync();
            if (string.IsNullOrEmpty(_vworldApiKey))
                return null;

            try
            {
                var url = $"https://api.vworld.kr/req/data?service=data&version=2.0&request=GetFeature" +
                          $"&data=LP_PA_CBND_BUBUN&key={Uri.EscapeDataString(_vworldApiKey)}" +
                          $"&domain=localhost&attrFilter=pnu:=:{pnu}" +
                          $"&crs=EPSG:4326&format=json&errorFormat=json&size=1";

                System.Diagnostics.Debug.WriteLine($"[VworldService] Data API 요청: PNU={pnu}");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[VworldService] Data API 응답 (처음 500자): {content.Substring(0, Math.Min(500, content.Length))}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[VworldService] Data API 호출 실패: {response.StatusCode}");
                    return null;
                }

                // XML 응답인 경우 (에러)
                if (content.TrimStart().StartsWith("<"))
                {
                    System.Diagnostics.Debug.WriteLine($"[VworldService] Data API 응답이 XML (에러): {content.Substring(0, Math.Min(500, content.Length))}");
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // response.status 확인
                if (root.TryGetProperty("response", out var resp))
                {
                    var status = resp.GetProperty("status").GetString();
                    if (status != "OK")
                    {
                        System.Diagnostics.Debug.WriteLine($"[VworldService] Data API 상태 오류: {status}");
                        return null;
                    }

                    // result.featureCollection.features[0].geometry
                    var features = resp.GetProperty("result")
                                       .GetProperty("featureCollection")
                                       .GetProperty("features");

                    if (features.GetArrayLength() == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VworldService] Data API features 없음");
                        return null;
                    }

                    var geometry = features[0].GetProperty("geometry");
                    var geoType = geometry.GetProperty("type").GetString();
                    var coordinates = geometry.GetProperty("coordinates");

                    // MultiPolygon → 첫 번째 Polygon의 외곽선, Polygon → 외곽선
                    JsonElement ring;
                    if (geoType == "MultiPolygon")
                        ring = coordinates[0][0];
                    else
                        ring = coordinates[0];

                    var result = new List<double[]>();
                    foreach (var point in ring.EnumerateArray())
                    {
                        var lng = point[0].GetDouble();
                        var lat = point[1].GetDouble();
                        result.Add(new[] { lat, lng });
                    }

                    System.Diagnostics.Debug.WriteLine($"[VworldService] 필지 경계 조회 성공: PNU={pnu}, 좌표 {result.Count}개");
                    return result;
                }

                System.Diagnostics.Debug.WriteLine($"[VworldService] Data API 응답 구조 오류");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VworldService] 필지 경계 조회 실패: {ex.Message}");
                return null;
            }
        }

        // ========== 공시가격 API ==========

        private const string NedDataBaseUrl = "https://api.vworld.kr/ned/data";

        /// <summary>
        /// 주소에서 아파트 동호수 파싱
        /// 예: "제110동 제7층 제702호" → dong="110", ho="702"
        /// </summary>
        private (string? dong, string? ho) ParseDongHo(string address)
        {
            string? dong = null;
            string? ho = null;

            // "제110동" or "110동" 패턴
            var dongMatch = Regex.Match(address, @"제?(\d+)동");
            if (dongMatch.Success)
                dong = dongMatch.Groups[1].Value;

            // "제702호" or "702호" 패턴
            var hoMatch = Regex.Match(address, @"제?(\d+)호");
            if (hoMatch.Success)
                ho = hoMatch.Groups[1].Value;

            return (dong, ho);
        }

        /// <summary>
        /// PNU와 물건 종류로 공시가격 조회 (개별공시지가/공동주택/개별주택)
        /// </summary>
        /// <param name="pnu">19자리 필지고유번호</param>
        /// <param name="propertyType">물건 종류 (아파트, 단독주택 등)</param>
        /// <param name="stdrYear">기준연도 (미지정시 올해)</param>
        /// <param name="addressFull">전체 주소 (아파트 동호수 매칭용)</param>
        /// <returns>공시가격 결과 또는 null</returns>
        public async Task<OfficialPriceResult?> GetOfficialPriceAsync(string pnu, string? propertyType = null, string? stdrYear = null, string? addressFull = null)
        {
            if (string.IsNullOrWhiteSpace(pnu) || pnu.Length != 19)
                return null;

            await EnsureApiKeyLoadedAsync();
            if (string.IsNullOrEmpty(_vworldApiKey))
            {
                Debug.WriteLine("[VworldService] 공시가격 조회 실패: API 키 없음");
                return null;
            }

            var year = stdrYear ?? DateTime.Now.Year.ToString();
            var type = (propertyType ?? "").ToLower();

            // 물건 종류에 따라 API 결정
            var apiName = type switch
            {
                "아파트" or "apartment" or "빌라" or "villa" or "오피스텔" or "officetel" => "getApartHousingPriceAttr",
                "단독주택" or "house" or "다가구주택" or "multi-family" => "getIndvdHousingPriceAttr",
                _ => "getIndvdLandPriceAttr" // 토지, 상가, 공장, 기타
            };

            // 올해 → 작년 순서로 시도 (올해 공시가격이 아직 미발표일 수 있음)
            var yearsToTry = new[] { year, (int.Parse(year) - 1).ToString() };

            foreach (var y in yearsToTry)
            {
                var result = await CallOfficialPriceApiAsync(pnu, apiName, y, addressFull);
                if (result != null)
                    return result;

                // 공동주택 API로 조회 안 되면 개별주택으로 재시도 (빌라가 개별주택으로 분류될 수 있음)
                if (apiName == "getApartHousingPriceAttr")
                {
                    result = await CallOfficialPriceApiAsync(pnu, "getIndvdHousingPriceAttr", y, null);
                    if (result != null)
                        return result;
                }
            }

            // 마지막으로 개별공시지가 폴백 (건물 공시가가 없으면 토지라도)
            if (apiName != "getIndvdLandPriceAttr")
            {
                foreach (var y in yearsToTry)
                {
                    var result = await CallOfficialPriceApiAsync(pnu, "getIndvdLandPriceAttr", y, null);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 공시가격 API 호출
        /// </summary>
        private async Task<OfficialPriceResult?> CallOfficialPriceApiAsync(string pnu, string apiName, string stdrYear, string? addressFull = null)
        {
            try
            {
                var url = $"{NedDataBaseUrl}/{apiName}" +
                          $"?key={Uri.EscapeDataString(_vworldApiKey!)}" +
                          $"&pnu={Uri.EscapeDataString(pnu)}" +
                          $"&stdrYear={Uri.EscapeDataString(stdrYear)}" +
                          $"&format=json&numOfRows=100&pageNo=1";

                Debug.WriteLine($"[VworldService] 공시가격 조회: API={apiName}, PNU={pnu}, 기준년도={stdrYear}");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[VworldService] 공시가격 API 실패: {response.StatusCode}");
                    return null;
                }

                // XML 응답이면 에러
                if (content.TrimStart().StartsWith("<"))
                {
                    Debug.WriteLine($"[VworldService] 공시가격 API XML 응답 (에러)");
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // 응답 구조: { "xxxPrices": { "totalCount": "N", "field": [...] } }
                // 또는 공동주택: { "apartHousingPrices": { ... } }
                JsonElement dataRoot;
                string priceField;

                if (apiName == "getApartHousingPriceAttr")
                {
                    if (!root.TryGetProperty("apartHousingPrices", out dataRoot))
                        return null;
                    priceField = "pblntfPc"; // 공동주택 공시가격
                }
                else if (apiName == "getIndvdHousingPriceAttr")
                {
                    if (!root.TryGetProperty("indvdHousingPrices", out dataRoot))
                        return null;
                    priceField = "housePc"; // 개별주택 공시가격
                }
                else // getIndvdLandPriceAttr
                {
                    if (!root.TryGetProperty("indvdLandPrices", out dataRoot))
                        return null;
                    priceField = "pblntfPclnd"; // 개별공시지가 (원/㎡)
                }

                var totalCountStr = dataRoot.TryGetProperty("totalCount", out var tc) ? tc.GetString() : "0";
                if (!int.TryParse(totalCountStr, out var totalCount) || totalCount == 0)
                {
                    Debug.WriteLine($"[VworldService] 공시가격 결과 없음: API={apiName}, 년도={stdrYear}");
                    return null;
                }

                if (!dataRoot.TryGetProperty("field", out var fields))
                    return null;

                // 아파트: 동호수 매칭으로 정확한 세대 찾기, 그 외: 최고가
                decimal maxPrice = 0;
                string resultYear = stdrYear;
                string resultAddress = "";

                // 아파트 동호수 매칭 시도
                if (apiName == "getApartHousingPriceAttr" && !string.IsNullOrWhiteSpace(addressFull))
                {
                    var (dong, ho) = ParseDongHo(addressFull);
                    if (!string.IsNullOrEmpty(dong) || !string.IsNullOrEmpty(ho))
                    {
                        foreach (var field in fields.EnumerateArray())
                        {
                            var fieldDong = field.TryGetProperty("dongNm", out var d) ? d.GetString() : null;
                            var fieldHo = field.TryGetProperty("hoNm", out var h) ? h.GetString() : null;

                            bool dongMatch = string.IsNullOrEmpty(dong) || fieldDong == dong;
                            bool hoMatch = string.IsNullOrEmpty(ho) || fieldHo == ho;

                            if (dongMatch && hoMatch)
                            {
                                var priceStr = field.TryGetProperty(priceField, out var pv) ? pv.GetString() : null;
                                if (decimal.TryParse(priceStr, out var price) && price > 0)
                                {
                                    maxPrice = price;
                                    resultYear = field.TryGetProperty("stdrYear", out var sy) ? sy.GetString() ?? stdrYear : stdrYear;
                                    resultAddress = field.TryGetProperty("ldCodeNm", out var addr) ? addr.GetString() ?? "" : "";
                                    Debug.WriteLine($"[VworldService] 아파트 동호 매칭 성공: 동={fieldDong}, 호={fieldHo}, 가격={price:N0}원");
                                    break;
                                }
                            }
                        }
                    }
                }

                // 매칭 실패 시 첫 번째 유효 결과 사용 (최고가 대신)
                if (maxPrice <= 0)
                {
                    foreach (var field in fields.EnumerateArray())
                    {
                        var priceStr = field.TryGetProperty(priceField, out var pv) ? pv.GetString() : null;
                        if (decimal.TryParse(priceStr, out var price) && price > 0)
                        {
                            maxPrice = price;
                            resultYear = field.TryGetProperty("stdrYear", out var sy) ? sy.GetString() ?? stdrYear : stdrYear;
                            resultAddress = field.TryGetProperty("ldCodeNm", out var addr) ? addr.GetString() ?? "" : "";
                            break; // 첫 번째 유효 결과 사용
                        }
                    }
                }

                if (maxPrice <= 0)
                    return null;

                var apiType = apiName switch
                {
                    "getApartHousingPriceAttr" => "공동주택",
                    "getIndvdHousingPriceAttr" => "개별주택",
                    _ => "개별공시지가"
                };

                Debug.WriteLine($"[VworldService] 공시가격 조회 성공: {apiType} {maxPrice:N0}원, 기준년도={resultYear}");

                return new OfficialPriceResult
                {
                    Price = maxPrice,
                    PriceType = apiType,
                    StandardYear = resultYear,
                    IsPerSquareMeter = apiName == "getIndvdLandPriceAttr",
                    Address = resultAddress
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VworldService] 공시가격 API 호출 오류: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 공시가격 조회 결과
    /// </summary>
    public class OfficialPriceResult
    {
        /// <summary>공시가격 (원) 또는 공시지가 (원/㎡)</summary>
        public decimal Price { get; set; }

        /// <summary>가격 유형: "공동주택", "개별주택", "개별공시지가"</summary>
        public string PriceType { get; set; } = "";

        /// <summary>기준연도</summary>
        public string StandardYear { get; set; } = "";

        /// <summary>true이면 단위면적당 가격 (원/㎡)</summary>
        public bool IsPerSquareMeter { get; set; }

        /// <summary>법정동 명칭</summary>
        public string Address { get; set; } = "";
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
