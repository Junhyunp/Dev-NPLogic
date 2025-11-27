using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// BasicDataTab.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BasicDataTab : UserControl
    {
        private bool _isWebViewInitialized = false;

        public BasicDataTab()
        {
            InitializeComponent();
        }

        private async void BasicDataTab_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeMapWebViewAsync();
        }

        /// <summary>
        /// WebView2 초기화
        /// </summary>
        private async System.Threading.Tasks.Task InitializeMapWebViewAsync()
        {
            if (_isWebViewInitialized) return;

            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;

                // 주소 변경 시 지도 업데이트
                if (DataContext is PropertyDetailViewModel viewModel)
                {
                    viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    
                    // 초기 지도 로드
                    UpdateMap(viewModel.Property?.AddressFull);
                }

                MapLoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 초기화 실패: {ex.Message}");
                // WebView 초기화 실패 시 오버레이 유지
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PropertyDetailViewModel.Property))
            {
                if (sender is PropertyDetailViewModel viewModel)
                {
                    UpdateMap(viewModel.Property?.AddressFull);
                }
            }
        }

        /// <summary>
        /// 지도 업데이트 (카카오맵 API)
        /// </summary>
        private void UpdateMap(string? address)
        {
            if (!_isWebViewInitialized || string.IsNullOrWhiteSpace(address)) return;

            // 카카오맵 JavaScript API Key는 appsettings.json에서 가져옴
            var kakaoApiKey = GetKakaoApiKey();
            
            var html = GenerateKakaoMapHtml(address, kakaoApiKey);
            MapWebView.NavigateToString(html);
        }

        private string GetKakaoApiKey()
        {
            // appsettings.json에서 API 키 로드
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonDocument.Parse(json);
                    if (config.RootElement.TryGetProperty("KakaoMap", out var kakaoSection))
                    {
                        if (kakaoSection.TryGetProperty("ApiKey", out var apiKey))
                        {
                            return apiKey.GetString() ?? "";
                        }
                    }
                }
            }
            catch
            {
                // 설정 파일 읽기 실패 시 빈 문자열 반환
            }
            return "";
        }

        private string GenerateKakaoMapHtml(string address, string apiKey)
        {
            // API 키가 없으면 안내 메시지 표시
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { 
            margin: 0; 
            padding: 20px; 
            font-family: 'Segoe UI', sans-serif;
            background: #F0F5F9;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            box-sizing: border-box;
        }
        .message {
            text-align: center;
            color: #64748B;
        }
        .message h3 {
            color: #1E40AF;
            margin-bottom: 12px;
        }
        .message p {
            font-size: 13px;
            line-height: 1.6;
        }
        .message code {
            background: #E2E8F0;
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 12px;
        }
    </style>
</head>
<body>
    <div class='message'>
        <h3>카카오맵 API 키 필요</h3>
        <p>지도 기능을 사용하려면 <code>appsettings.json</code>에<br/>
        카카오맵 API 키를 설정해주세요.</p>
        <p style='margin-top: 16px; font-size: 12px;'>
            API 키 발급: <a href='https://developers.kakao.com/' target='_blank'>developers.kakao.com</a>
        </p>
    </div>
</body>
</html>";
            }

            // 카카오맵 HTML
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ margin: 0; padding: 0; }}
        #map {{ width: 100%; height: 100vh; }}
    </style>
    <script src='https://dapi.kakao.com/v2/maps/sdk.js?appkey={apiKey}&libraries=services'></script>
</head>
<body>
    <div id='map'></div>
    <script>
        var mapContainer = document.getElementById('map');
        var mapOption = {{
            center: new kakao.maps.LatLng(37.5665, 126.9780),
            level: 3
        }};
        
        var map = new kakao.maps.Map(mapContainer, mapOption);
        var geocoder = new kakao.maps.services.Geocoder();
        
        // 주소로 좌표 검색
        geocoder.addressSearch('{EscapeJavaScript(address)}', function(result, status) {{
            if (status === kakao.maps.services.Status.OK) {{
                var coords = new kakao.maps.LatLng(result[0].y, result[0].x);
                
                // 마커 표시
                var marker = new kakao.maps.Marker({{
                    map: map,
                    position: coords
                }});
                
                // 인포윈도우
                var infowindow = new kakao.maps.InfoWindow({{
                    content: '<div style=""padding:5px;font-size:12px;"">{EscapeJavaScript(address)}</div>'
                }});
                infowindow.open(map, marker);
                
                map.setCenter(coords);
            }}
        }});
    </script>
</body>
</html>";
        }

        private string EscapeJavaScript(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }
    }
}
