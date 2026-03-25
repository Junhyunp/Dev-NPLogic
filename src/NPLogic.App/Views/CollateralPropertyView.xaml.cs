using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 담보물건 탭 - 물건 기본 정보, 등기부등본 정보, 감정평가 정보를 테이블 형태로 표시
    /// WebView2를 사용하여 위성도/지적도/로드뷰/토지이용계획/건축물대장을 앱 내에서 표시
    /// 카카오 지도 API로 3분할 지도 패널 (위성도/지적도/로드뷰) 표시
    /// API 키는 Supabase Edge Function에서 안전하게 로드
    /// </summary>
    public partial class CollateralPropertyView : UserControl
    {
        private bool _webViewInitialized = false;
        private bool _mapWebViewsInitialized = false;
        private bool _mapConfigLoaded = false;

        private string _naverMapClientId = "";
        private string _naverMapClientSecret = "";
        private string _naverMapHtmlPath = "";
        private static readonly HttpClient _httpClient = new HttpClient();

        // MapService 인스턴스 (DI로 주입)
        private MapService? _mapService;

        // 각 지도의 마지막 HTML 콘텐츠 (팝업에서 재사용)
        private string _lastSatelliteHtml = "";
        private string _lastCadastralHtml = "";
        private string _lastRoadViewHtml = "";

        // 각 지도의 고정(lock) 상태
        private bool _isSatelliteLocked = false;
        private bool _isCadastralLocked = false;
        private bool _isRoadViewLocked = false;

        private PropertyDetailViewModel? _currentVm;

        public CollateralPropertyView()
        {
            InitializeComponent();
            InitializeMapService();
            InitializeWebView();
            InitializeMapWebViews();

            // DataContext 변경 시 지도 업데이트
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// MapService 초기화 및 설정 로드
        /// </summary>
        private async void InitializeMapService()
        {
            try
            {
                // DI에서 MapService 가져오기
                _mapService = App.ServiceProvider?.GetService<MapService>();
                if (_mapService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] MapService를 찾을 수 없습니다.");
                    return;
                }

                // AuthService에서 액세스 토큰 가져오기
                var authService = App.ServiceProvider?.GetService<AuthService>();
                if (authService == null || !authService.IsAuthenticated())
                {
                    System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] 인증되지 않음 - 지도 설정 로드 보류");
                    return;
                }

                var session = authService.GetSession();
                if (session?.AccessToken == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] 액세스 토큰 없음");
                    return;
                }

                // Edge Function에서 지도 설정 로드
                var config = await _mapService.LoadMapConfigAsync(session.AccessToken);
                if (config != null)
                {
                    _kakaoApiKey = _mapService.GetKakaoApiKey() ?? "";
                    _naverMapClientId = _mapService.GetNaverClientId() ?? "";
                    _naverMapClientSecret = _mapService.GetNaverClientSecret() ?? "";
                    _mapConfigLoaded = true;

                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 지도 설정 로드 완료 - 카카오: {(!string.IsNullOrEmpty(_kakaoApiKey) ? "설정됨" : "미설정")}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] 지도 설정 로드 실패");
                }

                // navermap.html 경로 설정 (fallback용)
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _naverMapHtmlPath = Path.Combine(appDir, "Assets", "Maps", "navermap.html");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] MapService 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 지도 설정이 로드되었는지 확인하고, 안되어 있으면 로드 시도
        /// </summary>
        private async Task EnsureMapConfigLoadedAsync()
        {
            if (_mapConfigLoaded && !string.IsNullOrEmpty(_kakaoApiKey))
                return;

            try
            {
                _mapService ??= App.ServiceProvider?.GetService<MapService>();
                if (_mapService == null) return;

                // 토큰 만료 대비: 세션 유효성 확인 및 갱신
                var supabaseService = App.ServiceProvider?.GetService<SupabaseService>();
                if (supabaseService != null)
                {
                    await supabaseService.EnsureValidSessionAsync(throwOnFailure: false);
                }

                var authService = App.ServiceProvider?.GetService<AuthService>();
                if (authService == null || !authService.IsAuthenticated()) return;

                var session = authService.GetSession();
                if (session?.AccessToken == null) return;

                var config = await _mapService.LoadMapConfigAsync(session.AccessToken);
                if (config != null)
                {
                    _kakaoApiKey = _mapService.GetKakaoApiKey() ?? "";
                    _naverMapClientId = _mapService.GetNaverClientId() ?? "";
                    _naverMapClientSecret = _mapService.GetNaverClientSecret() ?? "";
                    _mapConfigLoaded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 지도 설정 재로드 실패: {ex.Message}");
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async(null);
                _webViewInitialized = true;
                System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] WebView2 초기화 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] WebView2 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 3분할 지도 WebView2 초기화
        /// </summary>
        private async void InitializeMapWebViews()
        {
            try
            {
                // 3개의 WebView2 순차 초기화 (안정성)
                await SatelliteMapWebView.EnsureCoreWebView2Async(null);
                await CadastralMapWebView.EnsureCoreWebView2Async(null);
                await RoadViewWebView.EnsureCoreWebView2Async(null);

                // 가상 호스트 매핑 설정 (네이버 API 도메인 인증용)
                // 네이버 클라우드 플랫폼에서 "nplogic-map.com" 도메인 등록 필요
                var mapsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Maps");
                if (Directory.Exists(mapsFolder))
                {
                    SatelliteMapWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "nplogic-map.com", mapsFolder, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                    CadastralMapWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "nplogic-map.com", mapsFolder, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                    RoadViewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "nplogic-map.com", mapsFolder, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 가상 호스트 매핑 완료: nplogic-map.com -> {mapsFolder}");
                }

                // WebView2 메시지 수신 (JavaScript에서 postMessage로 전송)
                SatelliteMapWebView.CoreWebView2.WebMessageReceived += OnSatelliteMapMessageReceived;
                CadastralMapWebView.CoreWebView2.WebMessageReceived += OnCadastralMapMessageReceived;
                RoadViewWebView.CoreWebView2.WebMessageReceived += OnRoadViewMapMessageReceived;

                // DevTools 콘솔 로그 캡처
                SatelliteMapWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                CadastralMapWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                RoadViewWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // 네비게이션 완료 이벤트 (에러 감지용)
                SatelliteMapWebView.CoreWebView2.NavigationCompleted += (s, args) =>
                    System.Diagnostics.Debug.WriteLine($"[위성도] 네비게이션 완료 - 성공: {args.IsSuccess}, 상태: {args.WebErrorStatus}");
                CadastralMapWebView.CoreWebView2.NavigationCompleted += (s, args) =>
                    System.Diagnostics.Debug.WriteLine($"[지적도] 네비게이션 완료 - 성공: {args.IsSuccess}, 상태: {args.WebErrorStatus}");
                RoadViewWebView.CoreWebView2.NavigationCompleted += (s, args) =>
                    System.Diagnostics.Debug.WriteLine($"[로드뷰] 네비게이션 완료 - 성공: {args.IsSuccess}, 상태: {args.WebErrorStatus}");

                _mapWebViewsInitialized = true;
                System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] 3분할 지도 WebView2 초기화 완료");

                // 초기 로딩 메시지 표시
                var loadingHtml = GenerateLoadingHtml();
                SatelliteMapWebView.NavigateToString(loadingHtml);
                CadastralMapWebView.NavigateToString(loadingHtml);
                RoadViewWebView.NavigateToString(loadingHtml);

                // DataContext가 이미 설정되어 있으면 지도 로드
                if (DataContext is PropertyDetailViewModel vm && vm.Property != null)
                {
                    await LoadNaverMapsAsync(vm);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 3분할 지도 WebView2 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// DataContext 변경 시 지도 업데이트
        /// </summary>
        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 기존 VM 이벤트 해제
            if (_currentVm != null)
            {
                _currentVm.MortgageColumnNames.CollectionChanged -= MortgageColumnNames_CollectionChanged;
            }

            if (e.NewValue is PropertyDetailViewModel vm)
            {
                _currentVm = vm;
                // 공장저당 동적 컬럼 구독
                vm.MortgageColumnNames.CollectionChanged += MortgageColumnNames_CollectionChanged;
                // 초기 컬럼 빌드
                RebuildMachineryDynamicColumns();

                if (_mapWebViewsInitialized && vm.Property != null)
                {
                    await LoadNaverMapsAsync(vm);
                }
            }
        }

        /// <summary>
        /// 외부에서 물건 전환 시 지도 재로드용
        /// </summary>
        public async Task ReloadMapsAsync(PropertyDetailViewModel vm)
        {
            if (_mapWebViewsInitialized && vm.Property != null)
            {
                await LoadNaverMapsAsync(vm);
            }
        }

        /// <summary>
        /// 카카오 지도 3개 로드 (네이버 지도 WebView2 호환성 문제로 카카오 사용)
        /// </summary>
        private async Task LoadNaverMapsAsync(PropertyDetailViewModel vm)
        {
            // 카카오 API 키 로드 (Supabase Edge Function에서)
            await EnsureMapConfigLoadedAsync();

            if (string.IsNullOrEmpty(_kakaoApiKey))
            {
                var errorHtml = GenerateErrorHtml("카카오 지도 API 키가 설정되지 않았습니다.\n로그인 후 다시 시도해주세요.");
                SatelliteMapWebView.NavigateToString(errorHtml);
                CadastralMapWebView.NavigateToString(errorHtml);
                RoadViewWebView.NavigateToString(errorHtml);
                return;
            }

            try
            {
                decimal? lat = vm.Property.Latitude;
                decimal? lng = vm.Property.Longitude;

                // 좌표가 없으면 API로 조회
                if (!lat.HasValue || !lng.HasValue)
                {
                    var coords = await FetchCoordinatesAsync(vm);
                    if (coords.HasValue)
                    {
                        lat = coords.Value.lat;
                        lng = coords.Value.lng;
                    }
                }

                if (lat.HasValue && lng.HasValue)
                {
                    // 카카오 지도 HTML 직접 생성하여 로드
                    var satelliteHtml = GenerateKakaoMapHtml((double)lat.Value, (double)lng.Value, "HYBRID", false);
                    var roadviewHtml = GenerateKakaoRoadviewHtml((double)lat.Value, (double)lng.Value);

                    _lastSatelliteHtml = satelliteHtml;
                    _lastRoadViewHtml = roadviewHtml;

                    SatelliteMapWebView.NavigateToString(satelliteHtml);
                    RoadViewWebView.NavigateToString(roadviewHtml);

                    // 지적도: 위성도 + 해당 필지 경계선 폴리곤 오버레이
                    _ = LoadCadastralBoundaryMapAsync(vm, (double)lat.Value, (double)lng.Value);

                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 카카오 지도 로드: ({lat}, {lng})");
                }
                else
                {
                    var noCoordHtml = GenerateNoCoordinatesHtml(vm.Property.DisplayAddress ?? "주소 정보 없음");
                    _lastSatelliteHtml = noCoordHtml;
                    _lastCadastralHtml = noCoordHtml;
                    _lastRoadViewHtml = noCoordHtml;

                    SatelliteMapWebView.NavigateToString(noCoordHtml);
                    CadastralMapWebView.NavigateToString(noCoordHtml);
                    RoadViewWebView.NavigateToString(noCoordHtml);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 카카오 지도 로드 실패: {ex.Message}");
                var errorHtml = GenerateErrorHtml($"지도 로드 실패: {ex.Message}");
                SatelliteMapWebView.NavigateToString(errorHtml);
                CadastralMapWebView.NavigateToString(errorHtml);
                RoadViewWebView.NavigateToString(errorHtml);
            }
        }

        /// <summary>
        /// 카카오 지도 HTML 생성
        /// </summary>
        private string GenerateKakaoMapHtml(double lat, double lng, string mapType, bool showCadastral)
        {
            var cadastralOverlay = showCadastral
                ? "map.addOverlayMapTypeId(kakao.maps.MapTypeId.USE_DISTRICT);"
                : "";

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ width: 100%; height: 100%; overflow: hidden; }}
        #map {{ width: 100%; height: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&autoload=false""></script>
    <script>
        var map;
        kakao.maps.load(function() {{
            var container = document.getElementById('map');
            var options = {{
                center: new kakao.maps.LatLng({lat}, {lng}),
                level: 3,
                mapTypeId: kakao.maps.MapTypeId.{mapType}
            }};
            map = new kakao.maps.Map(container, options);
            {cadastralOverlay}

            var marker = new kakao.maps.Marker({{
                position: new kakao.maps.LatLng({lat}, {lng}),
                map: map
            }});

            console.log('카카오 지도 로드 완료: {mapType}');
        }});
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 카카오 로드뷰 HTML 생성
        /// </summary>
        private string GenerateKakaoRoadviewHtml(double lat, double lng)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ width: 100%; height: 100%; overflow: hidden; }}
        #roadview {{ width: 100%; height: 100%; }}
        .no-roadview {{
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            height: 100%;
            background: #f5f5f5;
            color: #666;
            font-size: 14px;
            text-align: center;
            font-family: 'Malgun Gothic', sans-serif;
        }}
        .no-roadview-icon {{ font-size: 48px; margin-bottom: 10px; opacity: 0.5; }}
    </style>
</head>
<body>
    <div id=""roadview""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&autoload=false""></script>
    <script>
        kakao.maps.load(function() {{
            var container = document.getElementById('roadview');
            var position = new kakao.maps.LatLng({lat}, {lng});

            var roadviewClient = new kakao.maps.RoadviewClient();
            roadviewClient.getNearestPanoId(position, 50, function(panoId) {{
                if (panoId) {{
                    var roadview = new kakao.maps.Roadview(container);
                    roadview.setPanoId(panoId, position);
                    console.log('카카오 로드뷰 로드 완료');
                }} else {{
                    container.innerHTML =
                        '<div class=""no-roadview"">' +
                            '<div class=""no-roadview-icon"">&#128694;</div>' +
                            '<div>이 위치의 거리뷰를 사용할 수 없습니다.</div>' +
                            '<div style=""margin-top:8px;font-size:12px;color:#999;"">좌표: {lat:F6}, {lng:F6}</div>' +
                        '</div>';
                }}
            }});
        }});
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 네이버 지도 HTML 생성
        /// </summary>
        private string GenerateNaverMapHtml(double lat, double lng, string mapType)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <script type=""text/javascript"" src=""https://oapi.map.naver.com/openapi/v3/maps.js?ncpClientId={_naverMapClientId}&submodules=panorama""></script>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ width: 100%; height: 100%; overflow: hidden; font-family: 'Malgun Gothic', sans-serif; }}
        #map {{ width: 100%; height: 100%; }}
        .no-panorama {{
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            height: 100%;
            background: #f5f5f5;
            color: #666;
            font-size: 12px;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script>
        (function() {{
            var mapContainer = document.getElementById('map');
            var lat = {lat};
            var lng = {lng};
            var mapType = '{mapType}';

            if (mapType === 'roadview') {{
                // 로드뷰 (파노라마)
                var position = new naver.maps.LatLng(lat, lng);
                naver.maps.Panorama.findNearestPanoId(position, 50, function(panoId) {{
                    if (panoId) {{
                        var panorama = new naver.maps.Panorama(mapContainer, {{
                            panoId: panoId,
                            pov: {{ pan: 0, tilt: 0, fov: 100 }}
                        }});
                    }} else {{
                        mapContainer.innerHTML = '<div class=""no-panorama""><div style=""font-size:24px;margin-bottom:8px;"">&#128694;</div><div>이 위치의 거리뷰를<br>사용할 수 없습니다.</div></div>';
                    }}
                }});
            }} else {{
                // 일반 지도 (위성도/지적도)
                var mapTypeId = (mapType === 'satellite') ? naver.maps.MapTypeId.HYBRID : naver.maps.MapTypeId.NORMAL;

                var map = new naver.maps.Map(mapContainer, {{
                    center: new naver.maps.LatLng(lat, lng),
                    zoom: 17,
                    mapTypeId: mapTypeId
                }});

                // 지적도 레이어 추가
                if (mapType === 'cadastral') {{
                    if (naver.maps.LayerGroup && naver.maps.LayerGroup.CADASTRAL) {{
                        map.addLayer(naver.maps.LayerGroup.CADASTRAL);
                    }}
                }}

                // 마커 추가
                var marker = new naver.maps.Marker({{
                    position: new naver.maps.LatLng(lat, lng),
                    map: map,
                    icon: {{
                        content: '<div style=""width:12px;height:12px;background:#d32f2f;border:2px solid white;border-radius:50%;box-shadow:0 2px 4px rgba(0,0,0,0.3);""></div>',
                        anchor: new naver.maps.Point(6, 6)
                    }}
                }});
            }}
        }})();
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 로딩 중 HTML
        /// </summary>
        private string GenerateLoadingHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body { display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;
               font-family: 'Malgun Gothic', sans-serif; background: #f5f5f5; color: #666; }
        .loading { text-align: center; }
        .spinner { width: 24px; height: 24px; border: 3px solid #ddd; border-top-color: #1976d2;
                   border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto 8px; }
        @keyframes spin { to { transform: rotate(360deg); } }
    </style>
</head>
<body>
    <div class=""loading"">
        <div class=""spinner""></div>
        <div>지도 로딩 중...</div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// 좌표 없음 HTML
        /// </summary>
        private string GenerateNoCoordinatesHtml(string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;
               font-family: 'Malgun Gothic', sans-serif; background: #fff3e0; color: #e65100; text-align: center; padding: 16px; }}
        .icon {{ font-size: 32px; margin-bottom: 8px; }}
        .message {{ font-size: 12px; }}
        .address {{ font-size: 11px; color: #999; margin-top: 8px; word-break: break-all; }}
    </style>
</head>
<body>
    <div>
        <div class=""icon"">&#128205;</div>
        <div class=""message"">좌표 정보가 없습니다</div>
        <div class=""address"">{EscapeHtmlString(address)}</div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// 에러 HTML
        /// </summary>
        private string GenerateErrorHtml(string message)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0;
               font-family: 'Malgun Gothic', sans-serif; background: #ffebee; color: #c62828; text-align: center; padding: 16px; }}
        .icon {{ font-size: 32px; margin-bottom: 8px; }}
        .message {{ font-size: 12px; }}
    </style>
</head>
<body>
    <div>
        <div class=""icon"">&#9888;</div>
        <div class=""message"">{EscapeHtmlString(message)}</div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// WebView2 패널 표시
        /// </summary>
        private void ShowWebViewPanel(string title, string iconKind)
        {
            WebViewTitle.Text = title;
            WebViewIcon.Kind = (MaterialDesignThemes.Wpf.PackIconKind)Enum.Parse(typeof(MaterialDesignThemes.Wpf.PackIconKind), iconKind);
            WebViewPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// WebView2 패널 닫기 (버튼 바 복구)
        /// </summary>
        private void CloseWebView_Click(object sender, RoutedEventArgs e)
        {
            WebViewPanel.Visibility = Visibility.Collapsed;
            MapWebView.NavigateToString("<html><body></body></html>");
        }

        /// <summary>
        /// 위성도 열기 (카카오맵 위성지도 - WebView2)
        /// </summary>
        private async void OpenSatelliteMap_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_webViewInitialized)
            {
                MessageBox.Show("WebView2가 아직 초기화되지 않았습니다. 잠시 후 다시 시도해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                decimal? lat = vm.Property.Latitude;
                decimal? lng = vm.Property.Longitude;

                // 좌표가 없으면 Vworld API로 조회
                if (!lat.HasValue || !lng.HasValue)
                {
                    var coords = await FetchCoordinatesAsync(vm);
                    if (coords.HasValue)
                    {
                        lat = coords.Value.lat;
                        lng = coords.Value.lng;
                    }
                }

                string html;
                if (lat.HasValue && lng.HasValue)
                {
                    html = GenerateSatelliteMapHtml((double)lat.Value, (double)lng.Value, vm.Property.DisplayAddress ?? "");
                }
                else
                {
                    // 좌표 없으면 주소로 검색
                    var address = CleanAddressForSearch(vm.Property.DisplayAddress ?? "");
                    html = GenerateSatelliteMapByAddressHtml(address);
                }

                ShowWebViewPanel("위성도", "Satellite");
                MapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"위성도를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 지적도 열기 (카카오맵 지적편집도 - WebView2)
        /// </summary>
        private async void OpenCadastralMap_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_webViewInitialized)
            {
                MessageBox.Show("WebView2가 아직 초기화되지 않았습니다. 잠시 후 다시 시도해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                decimal? lat = vm.Property.Latitude;
                decimal? lng = vm.Property.Longitude;

                // 좌표가 없으면 Vworld API로 조회
                if (!lat.HasValue || !lng.HasValue)
                {
                    var coords = await FetchCoordinatesAsync(vm);
                    if (coords.HasValue)
                    {
                        lat = coords.Value.lat;
                        lng = coords.Value.lng;
                    }
                }

                string html;
                if (lat.HasValue && lng.HasValue)
                {
                    html = GenerateCadastralMapHtml((double)lat.Value, (double)lng.Value, vm.Property.DisplayAddress ?? "");
                }
                else
                {
                    var address = CleanAddressForSearch(vm.Property.DisplayAddress ?? "");
                    html = GenerateCadastralMapByAddressHtml(address);
                }

                ShowWebViewPanel("지적도", "Map");
                MapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"지적도를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 로드뷰 열기 (카카오 로드뷰 SDK - WebView2)
        /// 좌표가 없으면 Vworld API로 좌표 조회 후 열기
        /// </summary>
        private async void OpenRoadView_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_webViewInitialized)
            {
                MessageBox.Show("WebView2가 아직 초기화되지 않았습니다. 잠시 후 다시 시도해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                decimal? lat = vm.Property.Latitude;
                decimal? lng = vm.Property.Longitude;

                // 좌표가 없으면 Vworld API로 조회
                if (!lat.HasValue || !lng.HasValue)
                {
                    var coords = await FetchCoordinatesAsync(vm);
                    if (coords.HasValue)
                    {
                        lat = coords.Value.lat;
                        lng = coords.Value.lng;
                    }
                }

                string html;
                if (lat.HasValue && lng.HasValue)
                {
                    html = GenerateRoadViewHtml((double)lat.Value, (double)lng.Value, vm.Property.DisplayAddress ?? "");
                }
                else
                {
                    // 좌표 없으면 주소로 검색 후 로드뷰
                    var address = CleanAddressForSearch(vm.Property.DisplayAddress ?? "");
                    html = GenerateRoadViewByAddressHtml(address);
                }

                ShowWebViewPanel("로드뷰", "Walk");
                MapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로드뷰를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 좌표 조회 및 저장 (Naver Geocoding API 우선, Vworld fallback)
        /// </summary>
        private async Task<(decimal lat, decimal lng)?> FetchCoordinatesAsync(PropertyDetailViewModel vm)
        {
            try
            {
                var rawAddress = vm.Property.DisplayAddress ?? "";
                if (string.IsNullOrWhiteSpace(rawAddress))
                    return null;

                // 지도 검색용 주소 정제 (괄호, 쉼표 뒤 지번, 동호수 정보 제거)
                var cleanedAddress = CleanAddressForSearch(rawAddress);
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 주소 정제: '{rawAddress}' → '{cleanedAddress}'");

                decimal? lat = null;
                decimal? lng = null;

                // 1. Naver Geocoding API 시도
                if (!string.IsNullOrEmpty(_naverMapClientId) && !string.IsNullOrEmpty(_naverMapClientSecret))
                {
                    var naverResult = await FetchCoordinatesFromNaverAsync(cleanedAddress);
                    if (naverResult.HasValue)
                    {
                        lat = naverResult.Value.lat;
                        lng = naverResult.Value.lng;
                        System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] Naver 지오코딩 성공: ({lat}, {lng})");
                    }
                }

                // 2. Naver 실패 시 Vworld API fallback
                if (!lat.HasValue || !lng.HasValue)
                {
                    var vworldService = App.ServiceProvider.GetService<VworldService>();
                    if (vworldService != null && vworldService.HasApiKey)
                    {
                        var result = await vworldService.SearchAddressAsync(cleanedAddress);
                        if (result != null && (result.Latitude != 0 || result.Longitude != 0))
                        {
                            lat = (decimal)result.Latitude;
                            lng = (decimal)result.Longitude;
                            System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] Vworld 지오코딩 성공: ({lat}, {lng})");

                            // PNU도 같이 저장 (없는 경우)
                            if (string.IsNullOrEmpty(vm.Property.Pnu) && result.IsValidPnu)
                                vm.Property.Pnu = result.Pnu;
                        }
                    }
                }

                if (!lat.HasValue || !lng.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 좌표 조회 실패: {cleanedAddress}");
                    return null;
                }

                // Property 모델 업데이트
                vm.Property.Latitude = lat;
                vm.Property.Longitude = lng;

                // DB에 저장
                var propertyRepository = App.ServiceProvider.GetService<PropertyRepository>();
                if (propertyRepository != null)
                {
                    await propertyRepository.UpdateAsync(vm.Property);
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 좌표 저장 완료: ({lat}, {lng})");
                }

                return (lat.Value, lng.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 좌표 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Naver Geocoding API로 좌표 조회
        /// https://api.ncloud-docs.com/docs/ai-naver-mapsgeocoding-geocode
        /// </summary>
        private async Task<(decimal lat, decimal lng)?> FetchCoordinatesFromNaverAsync(string address)
        {
            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://maps.apigw.ntruss.com/map-geocode/v2/geocode?query={encodedAddress}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-ncp-apigw-api-key-id", _naverMapClientId);
                request.Headers.Add("x-ncp-apigw-api-key", _naverMapClientSecret);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] Naver 지오코딩 HTTP 오류: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // status 확인
                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetString() != "OK")
                {
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] Naver 지오코딩 상태: {statusProp.GetString()}");
                    return null;
                }

                // addresses 배열 확인
                if (!root.TryGetProperty("addresses", out var addresses) || addresses.GetArrayLength() == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] Naver 지오코딩 결과 없음");
                    return null;
                }

                var firstResult = addresses[0];
                if (firstResult.TryGetProperty("y", out var yProp) && firstResult.TryGetProperty("x", out var xProp))
                {
                    var latStr = yProp.GetString();
                    var lngStr = xProp.GetString();

                    if (decimal.TryParse(latStr, out var lat) && decimal.TryParse(lngStr, out var lng))
                    {
                        return (lat, lng);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] Naver 지오코딩 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 검색용 주소 정제 - 부가 정보 제거하고 첫 번째 지번만 추출
        ///
        /// 데이터디스크의 담보소재지4 형식 예시:
        /// - "56-3(토지, 1동, 2동, 3동, 4동 건물 4개동), 56-22, 56-23(토지 2필지)"
        /// - "164-1 청라풍림엑슬루타워 제101동 제29층 제2901호"
        /// - "1029 유-타워 제32층 제3204호"
        /// - "산192-3(토지)"
        ///
        /// 정제 결과:
        /// - "경기도 용인시 처인구 포곡읍 삼계리 56-3"
        /// - "인천광역시 서구 청라동 164-1"
        /// </summary>
        private string CleanAddressForSearch(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return address;

            var cleaned = address;

            // 1. 괄호와 그 내용 제거: (토지, ...), (건물), (토지 2필지) 등
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\([^)]*\)", "");

            // 2. 쉼표 이후 내용 제거 (여러 지번이 나열된 경우 첫 번째만)
            var commaIndex = cleaned.IndexOf(',');
            if (commaIndex > 0)
                cleaned = cleaned.Substring(0, commaIndex);

            // 3. "제X동", "제X층", "제X호" 패턴 제거 (아파트/집합건물 상세주소)
            //    예: "292 우정에쉐르아파트 제102동 제15층 제1502호" → "292 우정에쉐르아파트"
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*제\d+동.*$", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*제\S*층.*$", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*제\S*호.*$", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*제지하층.*$", "");

            // 4. 건물명 뒤의 동/층/호 정보 제거 (한글 동호수)
            //    예: "1동", "에이동", "가동" 등 - 지번 뒤에 오는 건물동 정보
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+\d+동\s*$", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+[가-힣]+동\s*$", "");

            // 5. 연속 공백 정리
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }

        /// <summary>
        /// 토지이용계획 열기 (토지이음 - 외부 브라우저)
        /// PNU가 있으면 POST로 바로 조회, 없으면 Vworld API로 PNU 조회 후 열기
        /// </summary>
        private async void OpenLandUsePlan_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var pnu = vm.Property.Pnu;

                // PNU가 없으면 Vworld API로 조회
                if (string.IsNullOrEmpty(pnu) || pnu.Length != 19)
                {
                    var address = vm.Property.DisplayAddress ?? "";
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        MessageBox.Show("주소 정보가 없어 PNU를 조회할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    pnu = await FetchAndSavePnuAsync(vm, address);
                }

                if (!string.IsNullOrEmpty(pnu) && pnu.Length == 19)
                {
                    // 외부 브라우저에서 토지이음 열기
                    OpenLandUsePlanInBrowser(pnu);
                }
                else
                {
                    // PNU 조회 실패 - 토지이음 검색 페이지를 브라우저에서 열기
                    var searchUrl = "https://www.eum.go.kr/web/ar/lu/luLandSrch.jsp";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(searchUrl) { UseShellExecute = true });
                    MessageBox.Show("PNU 자동 조회에 실패했습니다.\n토지이음 검색 페이지에서 직접 주소를 검색해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"토지이용계획을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 토지이음 토지이용계획 페이지를 외부 브라우저에서 열기
        /// POST 방식이 필요하므로 임시 HTML 파일 생성 후 브라우저로 열기
        /// </summary>
        private void OpenLandUsePlanInBrowser(string pnu)
        {
            try
            {
                var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>토지이용계획 조회 중...</title>
</head>
<body onload=""document.getElementById('eumForm').submit();"">
    <form id=""eumForm"" method=""POST"" action=""https://www.eum.go.kr/web/ar/lu/luLandDet.jsp"">
        <input type=""hidden"" name=""selGbn"" value=""umd"">
        <input type=""hidden"" name=""isNoScr"" value=""script"">
        <input type=""hidden"" name=""s_type"" value=""1"">
        <input type=""hidden"" name=""mode"" value=""search"">
        <input type=""hidden"" name=""pnu"" value=""{pnu}"">
    </form>
    <p style=""font-family: 'Malgun Gothic', sans-serif; text-align: center; margin-top: 100px; color: #666;"">
        토지이음으로 이동 중입니다...
    </p>
</body>
</html>";

                // 임시 HTML 파일 생성
                var tempPath = Path.Combine(Path.GetTempPath(), $"eum_land_{pnu}.html");
                File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);

                // 기본 브라우저로 열기
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });

                // 5초 후 임시 파일 삭제
                Task.Delay(5000).ContinueWith(_ =>
                {
                    try { File.Delete(tempPath); } catch { }
                });

                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 토지이음 열기 완료: PNU={pnu}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 토지이음 열기 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Vworld API로 PNU 조회 후 DB에 저장
        /// </summary>
        private async Task<string?> FetchAndSavePnuAsync(PropertyDetailViewModel vm, string address)
        {
            try
            {
                var vworldService = App.ServiceProvider.GetService<VworldService>();
                if (vworldService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] VworldService를 찾을 수 없습니다.");
                    return null;
                }

                if (!vworldService.HasApiKey)
                {
                    System.Diagnostics.Debug.WriteLine("[CollateralPropertyView] Vworld API 키가 설정되지 않았습니다.");
                    return null;
                }

                // API 호출
                var result = await vworldService.SearchAddressAsync(address);
                if (result == null || !result.IsValidPnu)
                {
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] PNU 조회 실패: {address}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] PNU 조회 성공: {result.Pnu} ({result.Address})");

                // Property 모델 업데이트
                vm.Property.Pnu = result.Pnu;

                // 좌표도 업데이트 (기존 좌표가 없는 경우)
                if (!vm.Property.Latitude.HasValue && result.Latitude != 0)
                    vm.Property.Latitude = (decimal)result.Latitude;
                if (!vm.Property.Longitude.HasValue && result.Longitude != 0)
                    vm.Property.Longitude = (decimal)result.Longitude;

                // DB에 저장
                var propertyRepository = App.ServiceProvider.GetService<PropertyRepository>();
                if (propertyRepository != null)
                {
                    await propertyRepository.UpdateAsync(vm.Property);
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] PNU 저장 완료: {result.Pnu}");
                }

                return result.Pnu;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] PNU 조회/저장 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 건축물대장 열기 (세움터 - 외부 브라우저)
        /// </summary>
        private void OpenBuildingRegister_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var address = vm.Property.DisplayAddress ?? "";

                // 세움터 건축물대장 열람 페이지를 외부 브라우저에서 열기
                OpenBuildingRegisterInBrowser(address);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"건축물대장을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 세움터 건축물대장 열람 페이지를 외부 브라우저에서 열기
        /// 주소를 클립보드에 복사하고 세움터 검색 페이지로 이동
        /// </summary>
        private void OpenBuildingRegisterInBrowser(string address)
        {
            try
            {
                // 세움터 건축물대장 열람 URL
                var seumteoUrl = "https://cloud.eais.go.kr/moct/bci/aaa02/BCIAAA02L01";

                // 주소를 클립보드에 복사 (사용자 편의)
                if (!string.IsNullOrWhiteSpace(address))
                {
                    var cleanedAddress = CleanAddressForSearch(address);
                    Clipboard.SetText(cleanedAddress);
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 주소 클립보드 복사: {cleanedAddress}");
                }

                // 기본 브라우저로 세움터 열기
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(seumteoUrl) { UseShellExecute = true });

                // 사용자에게 안내
                MessageBox.Show(
                    "세움터 건축물대장 열람 페이지가 브라우저에서 열렸습니다.\n\n" +
                    "주소가 클립보드에 복사되었습니다.\n" +
                    "검색창에 붙여넣기(Ctrl+V)하여 조회하세요.",
                    "건축물대장",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 세움터 열기 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 세움터 열기 실패: {ex.Message}");
                throw;
            }
        }

        #region HTML 생성 메서드

        /// <summary>
        /// 위성도 HTML 생성 (좌표 기반)
        /// </summary>
        private string GenerateSatelliteMapHtml(double lat, double lng, string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>위성도</title>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ width: 100%; height: 100%; }}
        #map {{ width: 100%; height: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}""></script>
    <script>
        var mapContainer = document.getElementById('map');
        var mapOption = {{
            center: new kakao.maps.LatLng({lat}, {lng}),
            level: 3,
            mapTypeId: kakao.maps.MapTypeId.HYBRID
        }};
        var map = new kakao.maps.Map(mapContainer, mapOption);

        // 마커 추가
        var marker = new kakao.maps.Marker({{
            position: new kakao.maps.LatLng({lat}, {lng}),
            map: map
        }});

        // 인포윈도우
        var infowindow = new kakao.maps.InfoWindow({{
            content: '<div style=""padding:5px;font-size:12px;"">{EscapeJsString(address)}</div>'
        }});
        infowindow.open(map, marker);

        // 지도 컨트롤 추가
        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
        map.addControl(new kakao.maps.MapTypeControl(), kakao.maps.ControlPosition.TOPRIGHT);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 위성도 HTML 생성 (주소 검색 기반)
        /// </summary>
        private string GenerateSatelliteMapByAddressHtml(string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>위성도</title>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ width: 100%; height: 100%; }}
        #map {{ width: 100%; height: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&libraries=services""></script>
    <script>
        var mapContainer = document.getElementById('map');
        var mapOption = {{
            center: new kakao.maps.LatLng(37.5665, 126.9780),
            level: 3,
            mapTypeId: kakao.maps.MapTypeId.HYBRID
        }};
        var map = new kakao.maps.Map(mapContainer, mapOption);
        var geocoder = new kakao.maps.services.Geocoder();

        geocoder.addressSearch('{EscapeJsString(address)}', function(result, status) {{
            if (status === kakao.maps.services.Status.OK) {{
                var coords = new kakao.maps.LatLng(result[0].y, result[0].x);
                var marker = new kakao.maps.Marker({{
                    map: map,
                    position: coords
                }});
                var infowindow = new kakao.maps.InfoWindow({{
                    content: '<div style=""padding:5px;font-size:12px;"">{EscapeJsString(address)}</div>'
                }});
                infowindow.open(map, marker);
                map.setCenter(coords);
                setTimeout(function() {{ map.relayout(); map.setCenter(coords); }}, 200);
            }}
        }});

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
        map.addControl(new kakao.maps.MapTypeControl(), kakao.maps.ControlPosition.TOPRIGHT);
        setTimeout(function() {{ map.relayout(); }}, 300);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 지적도 HTML 생성 (좌표 기반)
        /// </summary>
        private string GenerateCadastralMapHtml(double lat, double lng, string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>지적도</title>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ width: 100%; height: 100%; }}
        #map {{ width: 100%; height: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}""></script>
    <script>
        var mapContainer = document.getElementById('map');
        var mapOption = {{
            center: new kakao.maps.LatLng({lat}, {lng}),
            level: 2
        }};
        var map = new kakao.maps.Map(mapContainer, mapOption);

        // 지적편집도 오버레이
        map.addOverlayMapTypeId(kakao.maps.MapTypeId.USE_DISTRICT);

        // 마커 추가
        var marker = new kakao.maps.Marker({{
            position: new kakao.maps.LatLng({lat}, {lng}),
            map: map
        }});

        var infowindow = new kakao.maps.InfoWindow({{
            content: '<div style=""padding:5px;font-size:12px;"">{EscapeJsString(address)}</div>'
        }});
        infowindow.open(map, marker);

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
        map.addControl(new kakao.maps.MapTypeControl(), kakao.maps.ControlPosition.TOPRIGHT);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 지적도 HTML 생성 (주소 검색 기반)
        /// </summary>
        private string GenerateCadastralMapByAddressHtml(string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>지적도</title>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ width: 100%; height: 100%; }}
        #map {{ width: 100%; height: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&libraries=services""></script>
    <script>
        var mapContainer = document.getElementById('map');
        var mapOption = {{
            center: new kakao.maps.LatLng(37.5665, 126.9780),
            level: 2
        }};
        var map = new kakao.maps.Map(mapContainer, mapOption);
        map.addOverlayMapTypeId(kakao.maps.MapTypeId.USE_DISTRICT);

        var geocoder = new kakao.maps.services.Geocoder();
        geocoder.addressSearch('{EscapeJsString(address)}', function(result, status) {{
            if (status === kakao.maps.services.Status.OK) {{
                var coords = new kakao.maps.LatLng(result[0].y, result[0].x);
                var marker = new kakao.maps.Marker({{
                    map: map,
                    position: coords
                }});
                var infowindow = new kakao.maps.InfoWindow({{
                    content: '<div style=""padding:5px;font-size:12px;"">{EscapeJsString(address)}</div>'
                }});
                infowindow.open(map, marker);
                map.setCenter(coords);
                setTimeout(function() {{ map.relayout(); map.setCenter(coords); }}, 200);
            }}
        }});

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
        map.addControl(new kakao.maps.MapTypeControl(), kakao.maps.ControlPosition.TOPRIGHT);
        setTimeout(function() {{ map.relayout(); }}, 300);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 로드뷰 HTML 생성 (좌표 기반)
        /// </summary>
        private string GenerateRoadViewHtml(double lat, double lng, string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>로드뷰</title>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ width: 100%; height: 100%; }}
        #container {{ width: 100%; height: 100%; display: flex; }}
        #map {{ width: 30%; height: 100%; }}
        #roadview {{ width: 70%; height: 100%; }}
        .no-roadview {{ display: flex; align-items: center; justify-content: center; background: #f5f5f5; font-family: sans-serif; color: #666; }}
    </style>
</head>
<body>
    <div id=""container"">
        <div id=""map""></div>
        <div id=""roadview""></div>
    </div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}""></script>
    <script>
        var mapContainer = document.getElementById('map');
        var rvContainer = document.getElementById('roadview');
        var position = new kakao.maps.LatLng({lat}, {lng});

        var mapOption = {{
            center: position,
            level: 3
        }};
        var map = new kakao.maps.Map(mapContainer, mapOption);
        var roadview = new kakao.maps.Roadview(rvContainer);
        var roadviewClient = new kakao.maps.RoadviewClient();

        // 가장 가까운 로드뷰 파노라마 찾기
        roadviewClient.getNearestPanoId(position, 50, function(panoId) {{
            if (panoId !== null) {{
                roadview.setPanoId(panoId, position);
            }} else {{
                rvContainer.innerHTML = '<div class=""no-roadview"">이 위치에서는 로드뷰를 사용할 수 없습니다.</div>';
                rvContainer.classList.add('no-roadview');
            }}
        }});

        // 마커 추가
        var marker = new kakao.maps.Marker({{
            position: position,
            map: map
        }});

        var infowindow = new kakao.maps.InfoWindow({{
            content: '<div style=""padding:5px;font-size:11px;"">{EscapeJsString(address)}</div>'
        }});
        infowindow.open(map, marker);

        // 지도 클릭 시 로드뷰 이동
        kakao.maps.event.addListener(map, 'click', function(mouseEvent) {{
            var clickPosition = mouseEvent.latLng;
            roadviewClient.getNearestPanoId(clickPosition, 50, function(panoId) {{
                if (panoId !== null) {{
                    roadview.setPanoId(panoId, clickPosition);
                    marker.setPosition(clickPosition);
                }}
            }});
        }});

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 로드뷰 HTML 생성 (주소 검색 기반)
        /// </summary>
        private string GenerateRoadViewByAddressHtml(string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>로드뷰</title>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ width: 100%; height: 100%; }}
        #container {{ width: 100%; height: 100%; display: flex; }}
        #map {{ width: 30%; height: 100%; }}
        #roadview {{ width: 70%; height: 100%; }}
        .no-roadview {{ display: flex; align-items: center; justify-content: center; background: #f5f5f5; font-family: sans-serif; color: #666; }}
    </style>
</head>
<body>
    <div id=""container"">
        <div id=""map""></div>
        <div id=""roadview""></div>
    </div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&libraries=services""></script>
    <script>
        var mapContainer = document.getElementById('map');
        var rvContainer = document.getElementById('roadview');

        var mapOption = {{
            center: new kakao.maps.LatLng(37.5665, 126.9780),
            level: 3
        }};
        var map = new kakao.maps.Map(mapContainer, mapOption);
        var roadview = new kakao.maps.Roadview(rvContainer);
        var roadviewClient = new kakao.maps.RoadviewClient();
        var marker = new kakao.maps.Marker({{ map: map }});

        var geocoder = new kakao.maps.services.Geocoder();
        geocoder.addressSearch('{EscapeJsString(address)}', function(result, status) {{
            if (status === kakao.maps.services.Status.OK) {{
                var position = new kakao.maps.LatLng(result[0].y, result[0].x);
                map.setCenter(position);
                marker.setPosition(position);

                var infowindow = new kakao.maps.InfoWindow({{
                    content: '<div style=""padding:5px;font-size:11px;"">{EscapeJsString(address)}</div>'
                }});
                infowindow.open(map, marker);

                roadviewClient.getNearestPanoId(position, 50, function(panoId) {{
                    if (panoId !== null) {{
                        roadview.setPanoId(panoId, position);
                    }} else {{
                        rvContainer.innerHTML = '<div class=""no-roadview"">이 위치에서는 로드뷰를 사용할 수 없습니다.</div>';
                        rvContainer.classList.add('no-roadview');
                    }}
                }});
            }}
        }});

        kakao.maps.event.addListener(map, 'click', function(mouseEvent) {{
            var clickPosition = mouseEvent.latLng;
            roadviewClient.getNearestPanoId(clickPosition, 50, function(panoId) {{
                if (panoId !== null) {{
                    roadview.setPanoId(panoId, clickPosition);
                    marker.setPosition(clickPosition);
                }}
            }});
        }});

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 토지이용계획 HTML 생성 (PNU로 바로 조회)
        /// </summary>
        private string GenerateLandUsePlanHtml(string pnu)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>토지이용계획</title>
</head>
<body onload=""document.getElementById('eumForm').submit();"">
    <form id=""eumForm"" method=""POST"" action=""https://www.eum.go.kr/web/ar/lu/luLandDet.jsp"">
        <input type=""hidden"" name=""selGbn"" value=""umd"">
        <input type=""hidden"" name=""isNoScr"" value=""script"">
        <input type=""hidden"" name=""s_type"" value=""1"">
        <input type=""hidden"" name=""mode"" value=""search"">
        <input type=""hidden"" name=""pnu"" value=""{pnu}"">
    </form>
    <p style=""font-family: sans-serif; text-align: center; margin-top: 50px;"">
        토지이음으로 이동 중입니다...<br>
        자동으로 이동하지 않으면 <a href=""#"" onclick=""document.getElementById('eumForm').submit(); return false;"">여기를 클릭</a>하세요.
    </p>
</body>
</html>";
        }

        /// <summary>
        /// 토지이용계획 검색 페이지 HTML 생성
        /// </summary>
        private string GenerateLandUsePlanSearchHtml(string address)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>토지이용계획</title>
    <style>
        body {{ font-family: sans-serif; padding: 20px; }}
        .info {{ background: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 8px; margin-bottom: 20px; }}
        .address {{ background: #e3f2fd; padding: 10px; border-radius: 4px; font-family: monospace; margin: 10px 0; }}
        button {{ background: #1976d2; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; margin-right: 10px; }}
        button:hover {{ background: #1565c0; }}
        iframe {{ width: 100%; height: calc(100vh - 200px); border: 1px solid #ddd; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class=""info"">
        <strong>PNU 자동 조회에 실패했습니다.</strong><br>
        아래 검색창에서 직접 주소를 검색해주세요.
    </div>
    <div class=""address"">
        주소: {EscapeHtmlString(address)}
    </div>
    <button onclick=""copyAddress()"">주소 복사</button>
    <button onclick=""openInBrowser()"">새 창에서 열기</button>
    <br><br>
    <iframe src=""https://www.eum.go.kr/web/ar/lu/luLandSrch.jsp""></iframe>
    <script>
        function copyAddress() {{
            navigator.clipboard.writeText('{EscapeJsString(address)}');
            alert('주소가 클립보드에 복사되었습니다.');
        }}
        function openInBrowser() {{
            window.open('https://www.eum.go.kr/web/ar/lu/luLandSrch.jsp', '_blank');
        }}
    </script>
</body>
</html>";
        }

        /// <summary>
        /// 건축물대장 HTML 생성
        /// </summary>
        private string GenerateBuildingRegisterHtml(string address)
        {
            // 세움터 건축물대장 열람 URL 사용
            var seumteoUrl = "https://cloud.eais.go.kr/moct/bci/aaa02/BCIAAA02L01";

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>건축물대장</title>
    <style>
        body {{ font-family: sans-serif; padding: 20px; }}
        .info {{ background: #e3f2fd; border: 1px solid #1976d2; padding: 15px; border-radius: 8px; margin-bottom: 20px; }}
        .address {{ background: #f5f5f5; padding: 10px; border-radius: 4px; font-family: monospace; margin: 10px 0; }}
        button {{ background: #1976d2; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; margin-right: 10px; }}
        button:hover {{ background: #1565c0; }}
        iframe {{ width: 100%; height: calc(100vh - 200px); border: 1px solid #ddd; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class=""info"">
        <strong>세움터 건축물대장 열람</strong><br>
        아래 검색창에서 주소를 검색하여 건축물대장을 조회하세요.
    </div>
    <div class=""address"">
        주소: {EscapeHtmlString(address)}
    </div>
    <button onclick=""copyAddress()"">주소 복사</button>
    <button onclick=""openInBrowser()"">새 창에서 열기</button>
    <br><br>
    <iframe src=""{seumteoUrl}""></iframe>
    <script>
        function copyAddress() {{
            navigator.clipboard.writeText('{EscapeJsString(address)}');
            alert('주소가 클립보드에 복사되었습니다.');
        }}
        function openInBrowser() {{
            window.open('{seumteoUrl}', '_blank');
        }}
    </script>
</body>
</html>";
        }

        /// <summary>
        /// JavaScript 문자열 이스케이프
        /// </summary>
        private string EscapeJsString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\")
                      .Replace("'", "\\'")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r");
        }

        /// <summary>
        /// HTML 문자열 이스케이프
        /// </summary>
        private string EscapeHtmlString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return System.Web.HttpUtility.HtmlEncode(str);
        }

        #endregion

        #region 네이버/카카오 지도 Fallback 처리

        private string _kakaoApiKey = "";

        /// <summary>
        /// 위성도 WebView 메시지 수신
        /// </summary>
        private void OnSatelliteMapMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            HandleMapMessage("위성도", e, SatelliteMapWebView, "satellite");
        }

        /// <summary>
        /// 지적도 WebView 메시지 수신
        /// </summary>
        private void OnCadastralMapMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            HandleMapMessage("지적도", e, CadastralMapWebView, "cadastral");
        }

        /// <summary>
        /// 로드뷰 WebView 메시지 수신
        /// </summary>
        private void OnRoadViewMapMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            HandleMapMessage("로드뷰", e, RoadViewWebView, "roadview");
        }

        /// <summary>
        /// 지도 메시지 처리 (인증 실패 시 카카오 지도로 fallback)
        /// </summary>
        private void HandleMapMessage(string mapName, CoreWebView2WebMessageReceivedEventArgs e,
            Microsoft.Web.WebView2.Wpf.WebView2 webView, string mapType)
        {
            try
            {
                var json = e.WebMessageAsJson;
                System.Diagnostics.Debug.WriteLine($"[{mapName} WebMessage] {json}");

                var message = JsonSerializer.Deserialize<JsonElement>(json);

                if (message.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "authFailure")
                    {
                        System.Diagnostics.Debug.WriteLine($"[{mapName}] 네이버 지도 인증 실패 - 카카오 지도로 전환");

                        // 좌표 추출
                        decimal lat = 37.5665m, lng = 126.9780m;
                        if (message.TryGetProperty("lat", out var latEl)) lat = latEl.GetDecimal();
                        if (message.TryGetProperty("lng", out var lngEl)) lng = lngEl.GetDecimal();

                        // 카카오 지도로 fallback
                        Dispatcher.Invoke(() => LoadKakaoMapFallback(webView, mapType, lat, lng));
                    }
                    else if (type == "mapSuccess")
                    {
                        System.Diagnostics.Debug.WriteLine($"[{mapName}] 네이버 지도 로드 성공");
                    }
                    else if (type == "debug")
                    {
                        // 디버그 메시지는 이미 위에서 출력됨
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{mapName}] 메시지 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 카카오 지도 fallback 로드
        /// </summary>
        private async void LoadKakaoMapFallback(Microsoft.Web.WebView2.Wpf.WebView2 webView, string mapType, decimal lat, decimal lng)
        {
            try
            {
                // 카카오 API 키 로드 (Supabase Edge Function에서)
                await EnsureMapConfigLoadedAsync();

                if (string.IsNullOrEmpty(_kakaoApiKey))
                {
                    System.Diagnostics.Debug.WriteLine("[Fallback] 카카오 API 키도 설정되지 않음");
                    var errorHtml = GenerateErrorHtml("네이버 지도 인증 실패. 카카오 API 키도 설정되지 않았습니다.\n로그인 후 다시 시도해주세요.");
                    webView.NavigateToString(errorHtml);
                    return;
                }

                // 카카오 지도 HTML 생성 및 로드
                var kakaoMapType = mapType switch
                {
                    "satellite" => "HYBRID",
                    "cadastral" => "ROADMAP",
                    "roadview" => "ROADVIEW",
                    _ => "HYBRID"
                };

                System.Diagnostics.Debug.WriteLine($"[Fallback] 카카오 지도 로드: {mapType} -> {kakaoMapType}");

                var html = GenerateKakaoMapFallbackHtml((double)lat, (double)lng, kakaoMapType, mapType == "cadastral");
                webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Fallback] 카카오 지도 로드 실패: {ex.Message}");
                var errorHtml = GenerateErrorHtml($"지도 로드 실패: {ex.Message}");
                webView.NavigateToString(errorHtml);
            }
        }

        /// <summary>
        /// 카카오 지도 Fallback HTML 생성
        /// </summary>
        private string GenerateKakaoMapFallbackHtml(double lat, double lng, string mapType, bool showCadastral)
        {
            var cadastralOverlay = showCadastral
                ? "map.addOverlayMapTypeId(kakao.maps.MapTypeId.USE_DISTRICT);"
                : "";

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ width: 100%; height: 100%; overflow: hidden; }}
        #map {{ width: 100%; height: 100%; }}
        .fallback-badge {{
            position: absolute;
            top: 8px;
            left: 8px;
            background: rgba(255, 193, 7, 0.9);
            color: #333;
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 11px;
            font-family: 'Malgun Gothic', sans-serif;
            z-index: 1000;
        }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <div class=""fallback-badge"">카카오 지도 (Fallback)</div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&autoload=false""></script>
    <script>
        kakao.maps.load(function() {{
            var container = document.getElementById('map');
            var options = {{
                center: new kakao.maps.LatLng({lat}, {lng}),
                level: 3,
                mapTypeId: kakao.maps.MapTypeId.{mapType}
            }};
            var map = new kakao.maps.Map(container, options);
            {cadastralOverlay}

            // 마커 추가
            var marker = new kakao.maps.Marker({{
                position: new kakao.maps.LatLng({lat}, {lng}),
                map: map
            }});

            console.log('카카오 지도 (Fallback) 로드 완료');
        }});
    </script>
</body>
</html>";
        }

        #endregion

        #region 필지경계 지도

        /// <summary>
        /// 지적도 로드: VWORLD Data API로 필지 폴리곤 조회 → 카카오 위성도 위에 경계선 표시
        /// </summary>
        private async Task LoadCadastralBoundaryMapAsync(PropertyDetailViewModel vm, double lat, double lng)
        {
            try
            {
                // PNU 확보
                var pnu = vm.Property.Pnu;
                if (string.IsNullOrEmpty(pnu) || pnu.Length != 19)
                {
                    var address = vm.Property.DisplayAddress ?? "";
                    if (!string.IsNullOrWhiteSpace(address))
                        pnu = await FetchAndSavePnuAsync(vm, CleanAddressForSearch(address));
                }

                List<double[]>? boundary = null;
                if (!string.IsNullOrEmpty(pnu) && pnu.Length == 19)
                {
                    var vworldService = App.ServiceProvider?.GetService<VworldService>();
                    if (vworldService != null)
                        boundary = await vworldService.GetParcelBoundaryAsync(pnu);
                }

                string html;
                if (boundary != null && boundary.Count > 0)
                {
                    html = GenerateKakaoSatelliteWithBoundaryHtml(lat, lng, boundary);
                    System.Diagnostics.Debug.WriteLine($"[지적도] 필지 경계 폴리곤 표시: 좌표 {boundary.Count}개");
                }
                else
                {
                    // 폴리곤 없으면 위성도+마커만 표시
                    html = GenerateKakaoMapHtml(lat, lng, "HYBRID", false);
                    System.Diagnostics.Debug.WriteLine("[지적도] 필지 경계 폴리곤 없음 - 위성도 fallback");
                }

                _lastCadastralHtml = html;
                CadastralMapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[지적도] 로드 실패: {ex.Message}");
                var fallbackHtml = GenerateKakaoMapHtml(lat, lng, "HYBRID", false);
                _lastCadastralHtml = fallbackHtml;
                CadastralMapWebView.NavigateToString(fallbackHtml);
            }
        }

        /// <summary>
        /// 카카오 위성도 + 필지 경계 폴리곤 오버레이 HTML 생성
        /// </summary>
        private string GenerateKakaoSatelliteWithBoundaryHtml(double lat, double lng, List<double[]> boundary)
        {
            var pathJs = string.Join(",\n                    ",
                boundary.Select(p => $"new kakao.maps.LatLng({p[0].ToString("F8")}, {p[1].ToString("F8")})"));

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ width: 100%; height: 100%; overflow: hidden; }}
        #map {{ width: 100%; height: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={_kakaoApiKey}&autoload=false""></script>
    <script>
        var map;
        kakao.maps.load(function() {{
            var container = document.getElementById('map');
            var options = {{
                center: new kakao.maps.LatLng({lat}, {lng}),
                level: 2,
                mapTypeId: kakao.maps.MapTypeId.HYBRID
            }};
            map = new kakao.maps.Map(container, options);

            var path = [
                    {pathJs}
            ];

            var polygon = new kakao.maps.Polygon({{
                map: map,
                path: path,
                strokeWeight: 3,
                strokeColor: '#FF0000',
                strokeOpacity: 0.9,
                strokeStyle: 'solid',
                fillColor: '#FF0000',
                fillOpacity: 0.15
            }});

            // 폴리곤 영역에 맞게 지도 범위 조정
            var bounds = new kakao.maps.LatLngBounds();
            for (var i = 0; i < path.length; i++) {{
                bounds.extend(path[i]);
            }}
            map.setBounds(bounds, 50, 50, 50, 50);

            var marker = new kakao.maps.Marker({{
                position: new kakao.maps.LatLng({lat}, {lng}),
                map: map
            }});
        }});
    </script>
</body>
</html>";
        }

        #endregion

        #region 기계기구 감정가 동적 컬럼

        // XAML에서 고정 컬럼 7개 (번호~감정가) 뒤에 동적 공장저당 컬럼이 삽입됨
        private const int MachineryFixedColumnCountBefore = 7;
        // 뒤쪽 고정 컬럼: 담보여부, 평가율, 평가액, 삭제
        private readonly List<DataGridColumn> _machineryTrailingColumns = new();
        private bool _machineryTrailingColumnsInitialized = false;

        private void MortgageColumnNames_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildMachineryDynamicColumns();
        }

        private void RebuildMachineryDynamicColumns()
        {
            if (MachineryDataGrid == null) return;

            // 최초 1회: 뒤쪽 고정 컬럼 생성 (담보여부, 평가율, 평가액, 삭제)
            if (!_machineryTrailingColumnsInitialized)
            {
                _machineryTrailingColumnsInitialized = true;
                InitializeTrailingColumns();
            }

            // 기존 동적 컬럼 + 뒤쪽 고정 컬럼 제거
            while (MachineryDataGrid.Columns.Count > MachineryFixedColumnCountBefore)
                MachineryDataGrid.Columns.RemoveAt(MachineryDataGrid.Columns.Count - 1);

            var vm = DataContext as PropertyDetailViewModel;
            if (vm == null) return;

            // 동적 공장저당 텍스트 컬럼 추가
            for (int i = 0; i < vm.MortgageColumnNames.Count; i++)
            {
                var colName = vm.MortgageColumnNames[i];
                var textCol = new DataGridTextColumn
                {
                    Header = colName,
                    Width = new DataGridLength(90),
                    Binding = new Binding($"MortgageValues[{i}].Value")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                };
                textCol.ElementStyle = CreateCenterStyle();
                textCol.EditingElementStyle = CreateCenterEditStyle();
                MachineryDataGrid.Columns.Add(textCol);
            }

            // 뒤쪽 고정 컬럼 추가
            foreach (var col in _machineryTrailingColumns)
                MachineryDataGrid.Columns.Add(col);

            // 합계 행 재구성 (동적 컬럼에 맞춰 위치 조정)
            RebuildMachineryTotalsRow(vm.MortgageColumnNames.Count);
        }

        /// <summary>
        /// 기계기구 감정가 합계 행을 동적 컬럼 수에 맞춰 재구성
        /// </summary>
        private void RebuildMachineryTotalsRow(int mortgageColumnCount)
        {
            if (MachineryTotalsGrid == null) return;

            MachineryTotalsGrid.ColumnDefinitions.Clear();
            MachineryTotalsGrid.Children.Clear();

            // 고정 앞쪽 컬럼: 번호(50), 기계기구명(150), 제조사(100), 제작일자(90), 수량(60), 단가(100), 감정가(120)
            var frontWidths = new double[] { 50, 150, 100, 90, 60, 100, 120 };
            for (int i = 0; i < frontWidths.Length; i++)
                MachineryTotalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(frontWidths[i]) });

            // 동적 공장저당 컬럼 (각 90)
            for (int i = 0; i < mortgageColumnCount; i++)
                MachineryTotalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            // 뒤쪽 고정 컬럼: 담보여부(70), 평가율(80), 평가액(120), 삭제(40)
            var trailingWidths = new double[] { 70, 80, 120, 40 };
            for (int i = 0; i < trailingWidths.Length; i++)
                MachineryTotalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(trailingWidths[i]) });

            int colCount = frontWidths.Length + mortgageColumnCount + trailingWidths.Length;

            // "합계" 텍스트 (Column 0)
            var totalLabel = new System.Windows.Controls.TextBlock
            {
                Text = "합계",
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };
            Grid.SetColumn(totalLabel, 0);
            MachineryTotalsGrid.Children.Add(totalLabel);

            // 감정가 합계 (Column 6 = 감정가)
            var appraisalTotal = new System.Windows.Controls.TextBlock
            {
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };
            appraisalTotal.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
                new Binding("MachineryAppraisalTotalValue") { StringFormat = "N0" });
            Grid.SetColumn(appraisalTotal, 6);
            MachineryTotalsGrid.Children.Add(appraisalTotal);

            // 평가액 합계 (Column = 7 + mortgageColumnCount + 2 = 평가액 컬럼 위치)
            int evalValueColIndex = 7 + mortgageColumnCount + 2; // 담보여부(+0), 평가율(+1), 평가액(+2)
            var evalTotal = new System.Windows.Controls.TextBlock
            {
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };
            evalTotal.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
                new Binding("MachineryEvaluationTotalValue") { StringFormat = "N0" });
            Grid.SetColumn(evalTotal, evalValueColIndex);
            MachineryTotalsGrid.Children.Add(evalTotal);
        }

        private void InitializeTrailingColumns()
        {
            _machineryTrailingColumns.Clear();

            // 담보여부
            _machineryTrailingColumns.Add(new DataGridCheckBoxColumn
            {
                Header = "담보여부",
                Width = new DataGridLength(70),
                Binding = new Binding("IsCollateral")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            });

            // 평가율
            var evalRateCol = new DataGridTextColumn
            {
                Header = "평가율",
                Width = new DataGridLength(80),
                Binding = new Binding("EvaluationRate")
                {
                    StringFormat = "N2",
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            };
            evalRateCol.ElementStyle = CreateCenterStyle();
            evalRateCol.EditingElementStyle = CreateCenterEditStyle();
            _machineryTrailingColumns.Add(evalRateCol);

            // 평가액
            var evalValueCol = new DataGridTextColumn
            {
                Header = "평가액",
                Width = new DataGridLength(120),
                Binding = new Binding("EvaluationValue")
                {
                    StringFormat = "N0",
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            };
            evalValueCol.ElementStyle = CreateCenterStyle();
            evalValueCol.EditingElementStyle = CreateCenterEditStyle();
            _machineryTrailingColumns.Add(evalValueCol);

            // 삭제 버튼
            var deleteCol = new DataGridTemplateColumn
            {
                Header = "",
                Width = new DataGridLength(40),
            };
            var cellTemplate = new DataTemplate();
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.ContentProperty, "✕");
            buttonFactory.SetValue(Button.FontSizeProperty, 11.0);
            buttonFactory.SetValue(Button.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            buttonFactory.SetValue(Button.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35)));
            buttonFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            buttonFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);
            buttonFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            buttonFactory.SetValue(FrameworkElement.ToolTipProperty, "행 삭제");
            buttonFactory.SetBinding(Button.CommandProperty, new Binding("DataContext.RemoveMachineryAppraisalRowCommand")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl), 1)
            });
            buttonFactory.SetBinding(Button.CommandParameterProperty, new Binding());
            cellTemplate.VisualTree = buttonFactory;
            deleteCol.CellTemplate = cellTemplate;
            _machineryTrailingColumns.Add(deleteCol);
        }

        private static Style CreateCenterStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            return style;
        }

        private static Style CreateCenterEditStyle()
        {
            var style = new Style(typeof(TextBox));
            style.Setters.Add(new Setter(TextBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            return style;
        }

        private void MortgageColumnName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            var vm = DataContext as PropertyDetailViewModel;
            if (vm == null) return;

            var oldName = textBox.Tag as string;
            var newName = textBox.Text?.Trim();
            if (string.IsNullOrEmpty(newName) || oldName == newName) return;

            var index = vm.MortgageColumnNames.IndexOf(oldName!);
            if (index >= 0)
            {
                vm.MortgageColumnNames[index] = newName;
                // 컬럼 헤더 갱신
                RebuildMachineryDynamicColumns();
            }
        }

        private void RemoveMortgageColumn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var vm = DataContext as PropertyDetailViewModel;
            if (vm == null) return;

            // 부모 DataTemplate의 DataContext에서 컬럼명 가져오기
            var colName = btn.DataContext as string;
            if (colName == null) return;

            var index = vm.MortgageColumnNames.IndexOf(colName);
            if (index >= 0)
                vm.RemoveMortgageColumnCommand.Execute(index);
        }

        #endregion

        #region DataGrid 스크롤 전달

        /// <summary>
        /// DataGrid 내부 스크롤이 부모 ScrollViewer 스크롤을 잡아먹는 문제 해결.
        /// DataGrid의 마우스 휠 이벤트를 부모 ScrollViewer로 전달한다.
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;

            e.Handled = true;
            var parent = VisualTreeHelper.GetParent((DependencyObject)sender);
            while (parent != null && parent is not ScrollViewer)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ScrollViewer sv)
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                sv.RaiseEvent(eventArg);
            }
        }

        #endregion

        #region WebView2 에어스페이스 보정

        /// <summary>
        /// 스크롤 시 WebView2가 뷰포트를 벗어나면 숨기고, 들어오면 표시한다.
        /// WebView2는 네이티브 윈도우로 렌더링되어 WPF 클리핑을 무시하므로 수동 제어 필요.
        /// </summary>
        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (MapPanelGrid == null || MainScrollViewer == null) return;

            try
            {
                var mapTransform = MapPanelGrid.TransformToAncestor(MainScrollViewer);
                var mapTopLeft = mapTransform.Transform(new Point(0, 0));
                var mapBottomRight = mapTransform.Transform(new Point(MapPanelGrid.ActualWidth, MapPanelGrid.ActualHeight));

                double viewportTop = 0;
                double viewportBottom = MainScrollViewer.ViewportHeight;

                // WebView2는 네이티브 HWND이므로 부분 표시 시 클리핑이 안 됨
                // 완전히 뷰포트 안에 있을 때만 표시, 조금이라도 벗어나면 숨김
                bool fullyVisible = mapTopLeft.Y >= viewportTop && mapBottomRight.Y <= viewportBottom;

                var targetVisibility = fullyVisible ? Visibility.Visible : Visibility.Collapsed;

                if (SatelliteMapWebView.Visibility != targetVisibility)
                    SatelliteMapWebView.Visibility = targetVisibility;
                if (CadastralMapWebView.Visibility != targetVisibility)
                    CadastralMapWebView.Visibility = targetVisibility;
                if (RoadViewWebView.Visibility != targetVisibility)
                    RoadViewWebView.Visibility = targetVisibility;
            }
            catch
            {
                // TransformToAncestor 실패 시 무시 (로드 전 등)
            }
        }

        #endregion

        #region 지도 확대/고정 버튼 이벤트

        /// <summary>
        /// 위성도 확대 팝업 열기
        /// </summary>
        private void SatelliteExpand_Click(object sender, RoutedEventArgs e)
        {
            OpenMapPopup("위성도", MaterialDesignThemes.Wpf.PackIconKind.Satellite, _lastSatelliteHtml);
        }

        /// <summary>
        /// 지적도 확대 팝업 열기
        /// </summary>
        private void CadastralExpand_Click(object sender, RoutedEventArgs e)
        {
            OpenMapPopup("지적도", MaterialDesignThemes.Wpf.PackIconKind.Map, _lastCadastralHtml);
        }

        /// <summary>
        /// 로드뷰 확대 팝업 열기
        /// </summary>
        private void RoadViewExpand_Click(object sender, RoutedEventArgs e)
        {
            OpenMapPopup("로드뷰", MaterialDesignThemes.Wpf.PackIconKind.Walk, _lastRoadViewHtml);
        }

        /// <summary>
        /// 지도 확대 팝업 공통 로직
        /// </summary>
        private void OpenMapPopup(string title, MaterialDesignThemes.Wpf.PackIconKind iconKind, string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
            {
                MessageBox.Show("지도가 아직 로드되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var popup = new MapPopupWindow(title, iconKind, htmlContent)
            {
                Owner = Window.GetWindow(this)
            };
            popup.Show();
        }

        /// <summary>
        /// 위성도 고정/해제 토글
        /// </summary>
        private async void SatelliteLock_Click(object sender, RoutedEventArgs e)
        {
            _isSatelliteLocked = !_isSatelliteLocked;
            SatelliteLockButton.Content = _isSatelliteLocked ? "해제" : "고정";
            SatelliteLockButton.ToolTip = _isSatelliteLocked ? "지도 드래그/줌 해제" : "지도 드래그/줌 고정";
            await ToggleMapInteraction(SatelliteMapWebView, !_isSatelliteLocked);
        }

        /// <summary>
        /// 지적도 고정/해제 토글
        /// </summary>
        private async void CadastralLock_Click(object sender, RoutedEventArgs e)
        {
            _isCadastralLocked = !_isCadastralLocked;
            CadastralLockButton.Content = _isCadastralLocked ? "해제" : "고정";
            CadastralLockButton.ToolTip = _isCadastralLocked ? "지도 드래그/줌 해제" : "지도 드래그/줌 고정";
            await ToggleMapInteraction(CadastralMapWebView, !_isCadastralLocked);
        }

        /// <summary>
        /// 로드뷰 고정/해제 토글
        /// </summary>
        private async void RoadViewLock_Click(object sender, RoutedEventArgs e)
        {
            _isRoadViewLocked = !_isRoadViewLocked;
            RoadViewLockButton.Content = _isRoadViewLocked ? "해제" : "고정";
            RoadViewLockButton.ToolTip = _isRoadViewLocked ? "지도 드래그/줌 해제" : "지도 드래그/줌 고정";
            await ToggleMapInteraction(RoadViewWebView, !_isRoadViewLocked);
        }

        /// <summary>
        /// 지도 드래그/줌 활성화 또는 비활성화
        /// </summary>
        private async Task ToggleMapInteraction(Microsoft.Web.WebView2.Wpf.WebView2 webView, bool enabled)
        {
            try
            {
                if (webView.CoreWebView2 == null) return;

                // 카카오 Map 객체용 (위성도, 지적도)
                var mapScript = enabled
                    ? "if(typeof map !== 'undefined') { map.setDraggable(true); map.setZoomable(true); }"
                    : "if(typeof map !== 'undefined') { map.setDraggable(false); map.setZoomable(false); }";
                await webView.ExecuteScriptAsync(mapScript);

                // CSS overlay 방식 (로드뷰 포함 — 로드뷰는 Map API setDraggable 미지원)
                var overlayScript = enabled
                    ? "var ov=document.getElementById('lock-overlay');if(ov)ov.style.display='none';"
                    : "var ov=document.getElementById('lock-overlay');if(!ov){ov=document.createElement('div');ov.id='lock-overlay';ov.style.cssText='position:fixed;top:0;left:0;width:100%;height:100%;z-index:9999;background:transparent;cursor:not-allowed;';document.body.appendChild(ov);}else{ov.style.display=\"block\";}";
                await webView.ExecuteScriptAsync(overlayScript);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[지도 고정/해제] 오류: {ex.Message}");
            }
        }

        #endregion

    }
}
