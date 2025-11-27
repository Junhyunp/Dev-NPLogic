using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.Core;

namespace NPLogic.Views
{
    /// <summary>
    /// 카카오맵 WebView2 컨트롤 (레이어 토글 지원)
    /// </summary>
    public partial class MapView : UserControl
    {
        private bool _isInitialized = false;
        private bool _isMapReady = false;
        private bool _isRoadViewInitialized = false;
        private string? _kakaoMapApiKey;

        // 현재 레이어 상태
        private bool _showBaseMap = true;
        private bool _showSatellite = false;
        private bool _showCadastral = false;
        private bool _showLandUse = false;

        // 현재 위치 (로드뷰용)
        private double _currentLat = 37.5665;
        private double _currentLng = 126.9780;

        // 보류 중인 마커 설정 (지도 초기화 전에 호출된 경우)
        private (double lat, double lng, string? address, string? propertyNumber, string? propertyType)? _pendingMarker;

        /// <summary>
        /// 지도 준비 완료 이벤트
        /// </summary>
        public event EventHandler? MapReady;

        /// <summary>
        /// 마커 클릭 이벤트
        /// </summary>
        public event EventHandler<string>? MarkerClicked;

        /// <summary>
        /// 주소 검색 결과 이벤트
        /// </summary>
        public event EventHandler<(double lat, double lng, string address)>? GeocodeCompleted;

        public MapView()
        {
            InitializeComponent();
            LoadApiKey();
        }

        /// <summary>
        /// 설정 파일에서 API 키 로드
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

                    // KakaoMap:ApiKey 또는 KakaoMapApiKey 둘 다 지원
                    _kakaoMapApiKey = config["KakaoMap:ApiKey"] ?? config["KakaoMapApiKey"];
                }

                // 환경 변수에서도 확인
                if (string.IsNullOrEmpty(_kakaoMapApiKey))
                {
                    _kakaoMapApiKey = Environment.GetEnvironmentVariable("KAKAO_MAP_API_KEY");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API 키 로드 실패: {ex.Message}");
            }
        }

        private async void MapView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;

            try
            {
                await InitializeWebView();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ShowError("WebView2 초기화 실패", ex.Message);
            }
        }

        private void MapView_Unloaded(object sender, RoutedEventArgs e)
        {
            // WebView2 리소스 정리
            if (MapWebView.CoreWebView2 != null)
            {
                MapWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }
            if (RoadViewWebView.CoreWebView2 != null)
            {
                RoadViewWebView.CoreWebView2.WebMessageReceived -= RoadViewWebView_WebMessageReceived;
            }
        }

        /// <summary>
        /// WebView2 초기화
        /// </summary>
        private async Task InitializeWebView()
        {
            // WebView2 환경 초기화
            var env = await CoreWebView2Environment.CreateAsync();
            await MapWebView.EnsureCoreWebView2Async(env);

            // 메시지 수신 핸들러 등록
            MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            // HTML 파일 로드
            var htmlPath = GetHtmlFilePath();
            
            if (File.Exists(htmlPath))
            {
                MapWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                
                // 네비게이션 완료 후 지도 초기화
                MapWebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        await InitializeMapAsync();
                    }
                    else
                    {
                        ShowError("페이지 로드 실패", "HTML 파일을 불러올 수 없습니다.");
                    }
                };
            }
            else
            {
                ShowError("지도 파일 없음", $"kakaomap.html 파일을 찾을 수 없습니다.\n경로: {htmlPath}");
            }
        }

        /// <summary>
        /// HTML 파일 경로 가져오기
        /// </summary>
        private string GetHtmlFilePath()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(basePath, "Assets", "Maps", "kakaomap.html");
        }

        /// <summary>
        /// 지도 초기화 (API 키 전달)
        /// </summary>
        private async Task InitializeMapAsync()
        {
            try
            {
                var apiKey = _kakaoMapApiKey ?? "";
                var script = $"initializeMap('{EscapeJsString(apiKey)}');";
                await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"지도 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// JavaScript에서 보낸 메시지 처리
        /// </summary>
        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(message)) return;

                var json = JsonDocument.Parse(message);
                var type = json.RootElement.GetProperty("type").GetString();

                switch (type)
                {
                    case "mapReady":
                        _isMapReady = true;
                        Dispatcher.Invoke(() =>
                        {
                            LoadingOverlay.Visibility = Visibility.Collapsed;
                            MapReady?.Invoke(this, EventArgs.Empty);

                            // 보류 중인 마커가 있으면 설정
                            if (_pendingMarker.HasValue)
                            {
                                var p = _pendingMarker.Value;
                                _ = SetLocationAsync(p.lat, p.lng, p.address, p.propertyNumber, p.propertyType);
                                _pendingMarker = null;
                            }
                        });
                        break;

                    case "markerClick":
                        var propertyId = json.RootElement.GetProperty("propertyId").GetString();
                        Dispatcher.Invoke(() =>
                        {
                            MarkerClicked?.Invoke(this, propertyId ?? "");
                        });
                        break;

                    case "geocodeResult":
                        var lat = json.RootElement.GetProperty("lat").GetDouble();
                        var lng = json.RootElement.GetProperty("lng").GetDouble();
                        var address = json.RootElement.GetProperty("address").GetString() ?? "";
                        _currentLat = lat;
                        _currentLng = lng;
                        Dispatcher.Invoke(() =>
                        {
                            CoordinateInfo.Text = $"좌표: {lat:F6}, {lng:F6}";
                            GeocodeCompleted?.Invoke(this, (lat, lng, address));
                        });
                        break;

                    case "centerChanged":
                        var centerLat = json.RootElement.GetProperty("lat").GetDouble();
                        var centerLng = json.RootElement.GetProperty("lng").GetDouble();
                        _currentLat = centerLat;
                        _currentLng = centerLng;
                        Dispatcher.Invoke(() =>
                        {
                            CoordinateInfo.Text = $"좌표: {centerLat:F6}, {centerLng:F6}";
                        });
                        break;

                    case "geocodeError":
                        // 주소 검색 실패 처리
                        System.Diagnostics.Debug.WriteLine($"주소 검색 실패: {json.RootElement.GetProperty("error").GetString()}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"메시지 처리 실패: {ex.Message}");
            }
        }

        #region 레이어 토글

        /// <summary>
        /// 레이어 체크박스 변경 이벤트
        /// </summary>
        private async void LayerCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isMapReady) return;

            _showBaseMap = ChkBaseMap.IsChecked == true;
            _showSatellite = ChkSatellite.IsChecked == true;
            _showCadastral = ChkCadastral.IsChecked == true;
            _showLandUse = ChkLandUse.IsChecked == true;

            await UpdateLayersAsync();
        }

        /// <summary>
        /// 레이어 상태 업데이트
        /// </summary>
        private async Task UpdateLayersAsync()
        {
            try
            {
                // 지도 타입 변경
                string mapType = "ROADMAP";
                if (_showSatellite && !_showBaseMap) mapType = "SKYVIEW";
                else if (_showSatellite && _showBaseMap) mapType = "HYBRID";

                await MapWebView.CoreWebView2.ExecuteScriptAsync($"setMapType('{mapType}');");

                // 지적도 레이어 토글
                await MapWebView.CoreWebView2.ExecuteScriptAsync($"toggleCadastralLayer({_showCadastral.ToString().ToLower()});");

                // 토지이용계획 레이어 (API 연동 필요 - 현재는 알림만)
                if (_showLandUse)
                {
                    MessageBox.Show(
                        "토지이용계획 레이어는 국토이용정보서비스(LURIS) API 연동이 필요합니다.\n\n" +
                        "API 신청: https://www.eum.go.kr/",
                        "안내",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    ChkLandUse.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"레이어 변경 실패: {ex.Message}");
            }
        }

        #endregion

        #region 로드뷰

        /// <summary>
        /// 로드뷰 체크박스 변경
        /// </summary>
        private async void RoadViewCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkRoadView.IsChecked == true)
            {
                await ShowRoadViewAsync();
            }
            else
            {
                HideRoadView();
            }
        }

        /// <summary>
        /// 로드뷰 표시
        /// </summary>
        private async Task ShowRoadViewAsync()
        {
            try
            {
                RoadViewContainer.Visibility = Visibility.Visible;

                if (!_isRoadViewInitialized)
                {
                    var env = await CoreWebView2Environment.CreateAsync();
                    await RoadViewWebView.EnsureCoreWebView2Async(env);
                    RoadViewWebView.CoreWebView2.WebMessageReceived += RoadViewWebView_WebMessageReceived;
                    _isRoadViewInitialized = true;
                }

                // 로드뷰 HTML 로드
                var roadViewHtml = GenerateRoadViewHtml(_currentLat, _currentLng);
                RoadViewWebView.NavigateToString(roadViewHtml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로드뷰 초기화 실패: {ex.Message}");
            }
        }

        private void RoadViewWebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // 로드뷰 메시지 처리 (필요시 확장)
        }

        /// <summary>
        /// 로드뷰 숨기기
        /// </summary>
        private void HideRoadView()
        {
            RoadViewContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 로드뷰 닫기 버튼
        /// </summary>
        private void CloseRoadView_Click(object sender, RoutedEventArgs e)
        {
            ChkRoadView.IsChecked = false;
            HideRoadView();
        }

        /// <summary>
        /// 로드뷰 HTML 생성
        /// </summary>
        private string GenerateRoadViewHtml(double lat, double lng)
        {
            var apiKey = _kakaoMapApiKey ?? "";
            
            if (string.IsNullOrEmpty(apiKey))
            {
                return @"<!DOCTYPE html>
<html><body style='background:#1A2744;color:#94A3B8;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;font-family:sans-serif;'>
<p>API 키가 필요합니다</p>
</body></html>";
            }

            return string.Format(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>body{{margin:0;padding:0;}}#roadview{{width:100%;height:100vh;}}</style>
    <script src='https://dapi.kakao.com/v2/maps/sdk.js?appkey={0}'></script>
</head>
<body>
    <div id='roadview'></div>
    <script>
        var roadviewContainer = document.getElementById('roadview');
        var roadview = new kakao.maps.Roadview(roadviewContainer);
        var roadviewClient = new kakao.maps.RoadviewClient();
        var position = new kakao.maps.LatLng({1}, {2});
        
        roadviewClient.getNearestPanoId(position, 50, function(panoId) {{
            if (panoId) {{
                roadview.setPanoId(panoId, position);
            }} else {{
                document.body.innerHTML = '<div style=""background:#1A2744;color:#94A3B8;display:flex;align-items:center;justify-content:center;height:100vh;font-family:sans-serif;""><p>이 위치에 로드뷰가 없습니다</p></div>';
            }}
        }});
    </script>
</body>
</html>", apiKey, lat, lng);
        }

        #endregion

        #region 줌 컨트롤

        private async void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMapReady) return;
            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync("zoomIn();");
            }
            catch { }
        }

        private async void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMapReady) return;
            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync("zoomOut();");
            }
            catch { }
        }

        #endregion

        #region 기타 버튼

        private async void RefreshMap_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMapReady) return;
            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync("refreshMap();");
            }
            catch { }
        }

        private void OpenExternal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = $"https://map.kakao.com/?map_type=TYPE_MAP&sX={_currentLng}&sY={_currentLat}&sLevel=3";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"브라우저를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 단일 위치 마커 설정
        /// </summary>
        public async Task SetLocationAsync(double latitude, double longitude, string? address = null, string? propertyNumber = null, string? propertyType = null)
        {
            _currentLat = latitude;
            _currentLng = longitude;

            if (!_isMapReady)
            {
                // 지도가 준비되지 않았으면 보류
                _pendingMarker = (latitude, longitude, address, propertyNumber, propertyType);
                return;
            }

            try
            {
                var script = $"setMarker({latitude}, {longitude}, '{EscapeJsString(address ?? "")}', '{EscapeJsString(propertyNumber ?? "")}', '{EscapeJsString(propertyType ?? "")}');";
                await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                CoordinateInfo.Text = $"좌표: {latitude:F6}, {longitude:F6}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"마커 설정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 여러 위치 마커 설정
        /// </summary>
        public async Task SetMarkersAsync(object[] properties)
        {
            if (!_isMapReady) return;

            try
            {
                var json = JsonSerializer.Serialize(properties);
                var script = $"setMarkers('{EscapeJsString(json)}');";
                await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"마커 설정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 마커 모두 제거
        /// </summary>
        public async Task ClearMarkersAsync()
        {
            if (!_isMapReady) return;

            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync("clearMarkers();");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"마커 제거 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 지도 중심 이동
        /// </summary>
        public async Task PanToAsync(double latitude, double longitude)
        {
            if (!_isMapReady) return;
            _currentLat = latitude;
            _currentLng = longitude;

            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync($"panTo({latitude}, {longitude});");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"지도 이동 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 지도 확대/축소 레벨 설정
        /// </summary>
        public async Task SetLevelAsync(int level)
        {
            if (!_isMapReady) return;

            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync($"setLevel({level});");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"레벨 설정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 주소로 위치 검색
        /// </summary>
        public async Task SearchAddressAsync(string address)
        {
            if (!_isMapReady) return;

            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync($"searchAddress('{EscapeJsString(address)}');");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"주소 검색 실패: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 에러 표시
        /// </summary>
        private void ShowError(string message, string? detail = null)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                ErrorOverlay.Visibility = Visibility.Visible;
                ErrorMessage.Text = message;
                ErrorDetail.Text = detail ?? "";
            });
        }

        /// <summary>
        /// 다시 시도 버튼 클릭
        /// </summary>
        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorOverlay.Visibility = Visibility.Collapsed;
            LoadingOverlay.Visibility = Visibility.Visible;
            _isInitialized = false;
            _isMapReady = false;

            try
            {
                await InitializeWebView();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ShowError("WebView2 초기화 실패", ex.Message);
            }
        }

        /// <summary>
        /// JavaScript 문자열 이스케이프
        /// </summary>
        private static string EscapeJsString(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
