using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NPLogic.Services
{
    /// <summary>
    /// 카카오 Static Map API 서비스
    /// 지적도, 위성지도, 로드뷰 정적 이미지를 가져옴
    /// </summary>
    public class StaticMapService
    {
        private readonly HttpClient _httpClient;
        private string? _kakaoApiKey;

        // 카카오 Static Map API URL
        private const string KakaoStaticMapUrl = "https://dapi.kakao.com/v2/local/geo/coord2address.json";
        
        // 네이버 Static Map API URL (대안)
        private const string NaverStaticMapUrl = "https://naveropenapi.apigw.ntruss.com/map-static/v2/raster";

        public StaticMapService()
        {
            _httpClient = new HttpClient();
            LoadApiKey();
        }

        /// <summary>
        /// API 키 로드
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

                    _kakaoApiKey = config["KakaoMap:ApiKey"] ?? config["KakaoMapApiKey"];
                }

                // 환경 변수에서도 확인
                if (string.IsNullOrEmpty(_kakaoApiKey))
                {
                    _kakaoApiKey = Environment.GetEnvironmentVariable("KAKAO_MAP_API_KEY");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 키 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 지적도/위치도 이미지 가져오기
        /// </summary>
        /// <param name="lat">위도</param>
        /// <param name="lng">경도</param>
        /// <param name="width">이미지 너비 (기본 600)</param>
        /// <param name="height">이미지 높이 (기본 400)</param>
        /// <param name="level">줌 레벨 (기본 3)</param>
        /// <returns>이미지 바이트 배열</returns>
        public async Task<byte[]?> GetCadastralMapImageAsync(
            double lat, 
            double lng, 
            int width = 600, 
            int height = 400, 
            int level = 3)
        {
            if (string.IsNullOrEmpty(_kakaoApiKey))
            {
                // API 키가 없으면 OSM 타일 서버 사용 (대안)
                return await GetOsmMapImageAsync(lat, lng, width, height);
            }

            try
            {
                // 카카오 Static Map API
                // https://developers.kakao.com/docs/latest/ko/local/dev-guide#staticmap
                var url = $"https://dapi.kakao.com/v2/maps/v2/staticmap" +
                          $"?center={lng},{lat}" +
                          $"&level={level}" +
                          $"&size={width}x{height}" +
                          $"&maptype=roadmap,traffic,bicycle" +
                          $"&marker=type:default|size:mid|pos:{lng} {lat}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"KakaoAK {_kakaoApiKey}");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    // 카카오 API 실패 시 OSM 사용
                    System.Diagnostics.Debug.WriteLine($"카카오 API 실패: {response.StatusCode}");
                    return await GetOsmMapImageAsync(lat, lng, width, height);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Static Map API 실패: {ex.Message}");
                return await GetOsmMapImageAsync(lat, lng, width, height);
            }
        }

        /// <summary>
        /// 위성지도 이미지 가져오기
        /// </summary>
        public async Task<byte[]?> GetSatelliteMapImageAsync(
            double lat, 
            double lng, 
            int width = 600, 
            int height = 400, 
            int level = 3)
        {
            if (string.IsNullOrEmpty(_kakaoApiKey))
            {
                return null;
            }

            try
            {
                var url = $"https://dapi.kakao.com/v2/maps/v2/staticmap" +
                          $"?center={lng},{lat}" +
                          $"&level={level}" +
                          $"&size={width}x{height}" +
                          $"&maptype=skyview" +
                          $"&marker=type:default|size:mid|pos:{lng} {lat}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"KakaoAK {_kakaoApiKey}");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"위성지도 API 실패: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 로드뷰 이미지 가져오기
        /// 카카오 로드뷰 API는 Static 이미지를 직접 제공하지 않으므로
        /// 다른 방법으로 처리 (현재는 placeholder 이미지)
        /// </summary>
        public async Task<byte[]?> GetRoadViewImageAsync(
            double lat, 
            double lng, 
            int width = 600, 
            int height = 400)
        {
            // 카카오 로드뷰는 Static 이미지 API를 제공하지 않음
            // 대안: 거리뷰가 있는 경우 위성지도로 대체하거나
            // WebView2 스크린샷을 사용해야 함
            
            // 현재는 위성지도로 대체
            return await GetSatelliteMapImageAsync(lat, lng, width, height);
        }

        /// <summary>
        /// OSM 타일 서버에서 지도 이미지 가져오기 (API 키 없을 때 대안)
        /// </summary>
        private async Task<byte[]?> GetOsmMapImageAsync(
            double lat, 
            double lng, 
            int width = 600, 
            int height = 400)
        {
            try
            {
                // OpenStreetMap Static API (OSM Static Maps)
                // https://staticmaps.openstreetmap.de/
                var zoom = 17; // 약 1:4000 스케일
                var url = $"https://staticmaps.openstreetmap.de/staticmap.php" +
                          $"?center={lat},{lng}" +
                          $"&zoom={zoom}" +
                          $"&size={width}x{height}" +
                          $"&markers={lat},{lng},red-pushpin";

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OSM API 실패: {ex.Message}");
            }

            // 모든 방법 실패 시 placeholder 이미지 생성
            return CreatePlaceholderImage(width, height, $"지도 ({lat:F4}, {lng:F4})");
        }

        /// <summary>
        /// Placeholder 이미지 생성
        /// </summary>
        private byte[]? CreatePlaceholderImage(int width, int height, string text)
        {
            try
            {
                // System.Drawing을 사용한 간단한 placeholder 생성
                // WPF에서는 DrawingContext를 사용할 수도 있음
                using var bitmap = new System.Drawing.Bitmap(width, height);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);
                
                // 배경색
                graphics.Clear(System.Drawing.Color.FromArgb(240, 245, 249));
                
                // 테두리
                using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(148, 163, 184), 2);
                graphics.DrawRectangle(pen, 1, 1, width - 3, height - 3);
                
                // 텍스트
                using var font = new System.Drawing.Font("Segoe UI", 14);
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(100, 116, 139));
                var textSize = graphics.MeasureString(text, font);
                var x = (width - textSize.Width) / 2;
                var y = (height - textSize.Height) / 2;
                graphics.DrawString(text, font, brush, x, y);

                // 아이콘 (지도 아이콘 대신 텍스트)
                using var iconFont = new System.Drawing.Font("Segoe UI Emoji", 32);
                var iconSize = graphics.MeasureString("🗺️", iconFont);
                var iconX = (width - iconSize.Width) / 2;
                var iconY = y - iconSize.Height - 10;
                graphics.DrawString("🗺️", iconFont, brush, iconX, iconY);

                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Placeholder 이미지 생성 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 주소로 좌표 검색 (Geocoding)
        /// </summary>
        public async Task<(double lat, double lng)?> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrEmpty(_kakaoApiKey) || string.IsNullOrEmpty(address))
            {
                return null;
            }

            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://dapi.kakao.com/v2/local/search/address.json?query={encodedAddress}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"KakaoAK {_kakaoApiKey}");

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var documents = doc.RootElement.GetProperty("documents");
                    
                    if (documents.GetArrayLength() > 0)
                    {
                        var first = documents[0];
                        var x = double.Parse(first.GetProperty("x").GetString() ?? "0");
                        var y = double.Parse(first.GetProperty("y").GetString() ?? "0");
                        return (y, x); // y=lat, x=lng
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding 실패: {ex.Message}");
            }

            return null;
        }
    }
}

