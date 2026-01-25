using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using NPLogic.Data.Repositories;
using NPLogic.Services;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 담보물건 탭 - 물건 기본 정보, 등기부등본 정보, 감정평가 정보를 테이블 형태로 표시
    /// WebView2를 사용하여 위성도/지적도/로드뷰/토지이용계획/건축물대장을 앱 내에서 표시
    /// </summary>
    public partial class CollateralPropertyView : UserControl
    {
        private bool _webViewInitialized = false;
        private const string KAKAO_JS_KEY = "485c9d6d11788b3a3aec6a114b3f8461";

        public CollateralPropertyView()
        {
            InitializeComponent();
            InitializeWebView();
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
        /// WebView2 패널 표시
        /// </summary>
        private void ShowWebViewPanel(string title, string iconKind)
        {
            WebViewTitle.Text = title;
            WebViewIcon.Kind = (MaterialDesignThemes.Wpf.PackIconKind)Enum.Parse(typeof(MaterialDesignThemes.Wpf.PackIconKind), iconKind);
            ButtonBarPanel.Visibility = Visibility.Collapsed;
            WebViewPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// WebView2 패널 닫기 (버튼 바 복구)
        /// </summary>
        private void CloseWebView_Click(object sender, RoutedEventArgs e)
        {
            WebViewPanel.Visibility = Visibility.Collapsed;
            ButtonBarPanel.Visibility = Visibility.Visible;
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
        /// Vworld API로 좌표 조회 및 저장
        /// </summary>
        private async Task<(decimal lat, decimal lng)?> FetchCoordinatesAsync(PropertyDetailViewModel vm)
        {
            try
            {
                var vworldService = App.ServiceProvider.GetService<VworldService>();
                if (vworldService == null || !vworldService.HasApiKey)
                    return null;

                var address = vm.Property.DisplayAddress ?? "";
                if (string.IsNullOrWhiteSpace(address))
                    return null;

                var result = await vworldService.SearchAddressAsync(address);
                if (result == null || (result.Latitude == 0 && result.Longitude == 0))
                    return null;

                var lat = (decimal)result.Latitude;
                var lng = (decimal)result.Longitude;

                // Property 모델 업데이트
                vm.Property.Latitude = lat;
                vm.Property.Longitude = lng;

                // PNU도 같이 저장 (없는 경우)
                if (string.IsNullOrEmpty(vm.Property.Pnu) && result.IsValidPnu)
                    vm.Property.Pnu = result.Pnu;

                // DB에 저장
                var propertyRepository = App.ServiceProvider.GetService<PropertyRepository>();
                if (propertyRepository != null)
                {
                    await propertyRepository.UpdateAsync(vm.Property);
                    System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 좌표 저장 완료: ({lat}, {lng})");
                }

                return (lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CollateralPropertyView] 좌표 조회 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 검색용 주소 정제 - 부가 정보 제거
        /// 예: "인천광역시 서구 원창동 40-44(토지, 주건축물제1동 건물), 40-43(토지)"
        ///   → "인천광역시 서구 원창동 40-44"
        /// </summary>
        private string CleanAddressForSearch(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return address;

            // 괄호와 그 내용 제거: (토지, ...) 등
            var cleaned = System.Text.RegularExpressions.Regex.Replace(address, @"\([^)]*\)", "");

            // 쉼표 이후 내용 제거 (여러 지번이 나열된 경우 첫 번째만)
            var commaIndex = cleaned.IndexOf(',');
            if (commaIndex > 0)
                cleaned = cleaned.Substring(0, commaIndex);

            return cleaned.Trim();
        }

        /// <summary>
        /// 토지이용계획 열기 (토지이음 - WebView2)
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

            if (!_webViewInitialized)
            {
                MessageBox.Show("WebView2가 아직 초기화되지 않았습니다. 잠시 후 다시 시도해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
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

                string html;
                if (!string.IsNullOrEmpty(pnu) && pnu.Length == 19)
                {
                    html = GenerateLandUsePlanHtml(pnu);
                }
                else
                {
                    // PNU 조회 실패 - 검색 페이지
                    html = GenerateLandUsePlanSearchHtml(vm.Property.DisplayAddress ?? "");
                }

                ShowWebViewPanel("토지이용계획", "FileDocument");
                MapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"토지이용계획을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// 건축물대장 열기 (정부24 건축물대장 열람 - WebView2)
        /// </summary>
        private void OpenBuildingRegister_Click(object sender, RoutedEventArgs e)
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
                var address = vm.Property.DisplayAddress ?? "";
                var html = GenerateBuildingRegisterHtml(address);

                ShowWebViewPanel("건축물대장", "OfficeBuildingMarker");
                MapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"건축물대장을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={KAKAO_JS_KEY}""></script>
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
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={KAKAO_JS_KEY}&libraries=services""></script>
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
            }}
        }});

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
        map.addControl(new kakao.maps.MapTypeControl(), kakao.maps.ControlPosition.TOPRIGHT);
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
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={KAKAO_JS_KEY}""></script>
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
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={KAKAO_JS_KEY}&libraries=services""></script>
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
            }}
        }});

        map.addControl(new kakao.maps.ZoomControl(), kakao.maps.ControlPosition.RIGHT);
        map.addControl(new kakao.maps.MapTypeControl(), kakao.maps.ControlPosition.TOPRIGHT);
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
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={KAKAO_JS_KEY}""></script>
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
    <script src=""https://dapi.kakao.com/v2/maps/sdk.js?appkey={KAKAO_JS_KEY}&libraries=services""></script>
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

    }
}
