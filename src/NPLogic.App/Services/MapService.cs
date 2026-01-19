using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NPLogic.Services
{
    /// <summary>
    /// 지도 마커 정보
    /// </summary>
    public class MapMarker
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string MarkerType { get; set; } = "default"; // subject, case, transaction
        public string? Info { get; set; }
    }
    
    /// <summary>
    /// 지도 서비스 (Naver/Kakao Map 연동)
    /// </summary>
    public class MapService
    {
        private readonly HttpClient _httpClient;
        
        // Kakao API 키 (실제 사용 시 환경변수나 설정에서 가져옴)
        private string? _kakaoApiKey;
        
        // Naver API 키
        private string? _naverClientId;
        private string? _naverClientSecret;
        
        public MapService()
        {
            _httpClient = new HttpClient();
        }
        
        /// <summary>
        /// API 키 설정
        /// </summary>
        public void SetApiKeys(string? kakaoApiKey = null, string? naverClientId = null, string? naverClientSecret = null)
        {
            _kakaoApiKey = kakaoApiKey;
            _naverClientId = naverClientId;
            _naverClientSecret = naverClientSecret;
        }
        
        /// <summary>
        /// 주소를 좌표로 변환 (Kakao)
        /// </summary>
        public async Task<(double? Latitude, double? Longitude)> GeoCodeAddressKakaoAsync(string address)
        {
            if (string.IsNullOrEmpty(_kakaoApiKey))
                return (null, null);
            
            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://dapi.kakao.com/v2/local/search/address.json?query={encodedAddress}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"KakaoAK {_kakaoApiKey}");
                
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<KakaoGeoCodeResponse>(content);
                    
                    if (result?.Documents?.Length > 0)
                    {
                        var doc = result.Documents[0];
                        return (double.Parse(doc.Y), double.Parse(doc.X));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapService] GeoCode failed: {ex.Message}");
            }
            
            return (null, null);
        }
        
        /// <summary>
        /// 지도 HTML 생성 (Kakao Map)
        /// </summary>
        public string GenerateKakaoMapHtml(double centerLat, double centerLng, List<MapMarker> markers, int zoom = 15)
        {
            var markersJson = JsonSerializer.Serialize(markers);
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>사례지도</title>
    <script type='text/javascript' src='//dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}'></script>
    <style>
        body {{ margin: 0; padding: 0; }}
        #map {{ width: 100%; height: 100vh; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var mapContainer = document.getElementById('map');
        var mapOption = {{
            center: new kakao.maps.LatLng({centerLat}, {centerLng}),
            level: {zoom}
        }};
        
        var map = new kakao.maps.Map(mapContainer, mapOption);
        
        var markers = {markersJson};
        
        markers.forEach(function(m) {{
            var markerPosition = new kakao.maps.LatLng(m.Latitude, m.Longitude);
            
            var markerImage = null;
            if (m.MarkerType === 'subject') {{
                // 본건: 빨간색
                markerImage = new kakao.maps.MarkerImage(
                    'https://t1.daumcdn.net/localimg/localimages/07/mapapidoc/marker_red.png',
                    new kakao.maps.Size(28, 35)
                );
            }} else if (m.MarkerType === 'case') {{
                // 사례: 파란색
                markerImage = new kakao.maps.MarkerImage(
                    'https://t1.daumcdn.net/localimg/localimages/07/mapapidoc/marker_blue.png',
                    new kakao.maps.Size(28, 35)
                );
            }}
            
            var marker = new kakao.maps.Marker({{
                position: markerPosition,
                image: markerImage
            }});
            marker.setMap(map);
            
            if (m.Info) {{
                var infowindow = new kakao.maps.InfoWindow({{
                    content: '<div style=""padding:5px;"">' + m.Label + '<br>' + m.Info + '</div>'
                }});
                kakao.maps.event.addListener(marker, 'click', function() {{
                    infowindow.open(map, marker);
                }});
            }}
        }});
    </script>
</body>
</html>";
        }
        
        /// <summary>
        /// 로드뷰 HTML 생성 (Kakao)
        /// </summary>
        public string GenerateKakaoRoadViewHtml(double latitude, double longitude)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>로드뷰</title>
    <script type='text/javascript' src='//dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}'></script>
    <style>
        body {{ margin: 0; padding: 0; }}
        #roadview {{ width: 100%; height: 100vh; }}
    </style>
</head>
<body>
    <div id='roadview'></div>
    <script>
        var roadviewContainer = document.getElementById('roadview');
        var roadview = new kakao.maps.Roadview(roadviewContainer);
        var roadviewClient = new kakao.maps.RoadviewClient();
        
        var position = new kakao.maps.LatLng({latitude}, {longitude});
        
        roadviewClient.getNearestPanoId(position, 50, function(panoId) {{
            if (panoId) {{
                roadview.setPanoId(panoId, position);
            }} else {{
                roadviewContainer.innerHTML = '<div style=""text-align:center;padding-top:100px;"">해당 위치에 로드뷰가 없습니다.</div>';
            }}
        }});
    </script>
</body>
</html>";
        }
        
        /// <summary>
        /// 외부 지도 서비스 열기 (Naver Map)
        /// </summary>
        public void OpenNaverMapInBrowser(double latitude, double longitude, string? label = null)
        {
            try
            {
                var url = $"https://map.naver.com/p/search/{latitude},{longitude}";
                if (!string.IsNullOrEmpty(label))
                {
                    url = $"https://map.naver.com/p/search/{Uri.EscapeDataString(label)}";
                }
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
        
        /// <summary>
        /// 외부 지도 서비스 열기 (Kakao Map)
        /// </summary>
        public void OpenKakaoMapInBrowser(double latitude, double longitude, string? label = null)
        {
            try
            {
                var url = $"https://map.kakao.com/link/map/{label ?? "위치"},{latitude},{longitude}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
        
        /// <summary>
        /// 두 좌표 간 거리 계산 (km)
        /// </summary>
        public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // 지구 반지름 (km)
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
        
        private static double ToRad(double deg) => deg * Math.PI / 180;
    }
    
    /// <summary>
    /// Kakao GeoCode API 응답
    /// </summary>
    public class KakaoGeoCodeResponse
    {
        [JsonPropertyName("documents")]
        public KakaoGeoCodeDocument[]? Documents { get; set; }
    }
    
    public class KakaoGeoCodeDocument
    {
        [JsonPropertyName("x")]
        public string X { get; set; } = "";
        
        [JsonPropertyName("y")]
        public string Y { get; set; } = "";
        
        [JsonPropertyName("address_name")]
        public string? AddressName { get; set; }
    }
}
