using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace NPLogic.Views
{
    /// <summary>
    /// 지도 탭 뷰 (5분할: 지적도, 위치도, 토지이용계획확인원, 요약건축물대장, 건축물대장 세부)
    /// </summary>
    public partial class MapView : UserControl
    {
        private bool _isInitialized = false;
        private bool _isMapReady = false;
        private bool _isLocationMapInitialized = false;
        private bool _isLandUsePlanInitialized = false;

        // 현재 위치 (설정된 물건 위치)
        private double _currentLat = 37.5665;
        private double _currentLng = 126.9780;
        private string? _currentAddress;

        // 보류 중인 위치 설정
        private (double lat, double lng, string? address)? _pendingLocation;

        // Export 관련 필드
        private byte[]? _capturedMapImage;
        private byte[]? _capturedLocationMapImage;
        private byte[]? _capturedLandUsePlanImage;
        private string? _landUsePlanPath;
        private string? _buildingRegisterPath;
        private string? _buildingDetailPath;

        /// <summary>
        /// 지도 준비 완료 이벤트
        /// </summary>
        public event EventHandler? MapReady;

        public MapView()
        {
            InitializeComponent();
        }

        private async void MapView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;

            try
            {
                await InitializeAndCaptureAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ShowMapError();
                ShowLocationMapError();
                ShowLandUsePlanError("초기화 실패");
                System.Diagnostics.Debug.WriteLine($"초기화 실패: {ex.Message}");
            }
        }

        private void MapView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (MapWebView.CoreWebView2 != null)
            {
                MapWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }
        }

        /// <summary>
        /// WebView2 초기화 및 자동 캡처
        /// </summary>
        private async Task InitializeAndCaptureAsync()
        {
            // 지도 WebView2 초기화
            var env = await CoreWebView2Environment.CreateAsync();
            await MapWebView.EnsureCoreWebView2Async(env);
            
            MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            // NPLogic 지도 서버 (중앙 관리)
            string mapUrl = AppConstants.MapServerUrl;
            
            var mapTcs = new TaskCompletionSource<bool>();
            
            MapWebView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                if (args.IsSuccess)
                {
                    _isMapReady = true;
                    mapTcs.TrySetResult(true);
                }
                else
                {
                    mapTcs.TrySetResult(false);
                }
            };
            
            MapWebView.CoreWebView2.Navigate(mapUrl);
            
            // 지도 로드 완료 대기 (최대 10초)
            var mapLoaded = await Task.WhenAny(mapTcs.Task, Task.Delay(10000)) == mapTcs.Task && mapTcs.Task.Result;
            
            if (mapLoaded)
            {
                // 보류 중인 위치가 있으면 설정
                if (_pendingLocation.HasValue)
                {
                    await SetLocationInternalAsync(_pendingLocation.Value.lat, _pendingLocation.Value.lng, _pendingLocation.Value.address);
                    _pendingLocation = null;
                    await Task.Delay(2000); // 위치 설정 후 대기
                }
                else
                {
                    await Task.Delay(1500); // 기본 로딩 대기
                }
                
                // 지적도 캡처
                await CaptureMapImageAsync();
                MapReady?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowMapError();
            }
            
            // 위치도 초기화 및 캡처
            await InitializeAndCaptureLocationMapAsync();
            
            // 토지이용계획확인원 자동 조회 (주소가 있는 경우)
            await InitializeAndCaptureLandUsePlanAsync();
        }

        /// <summary>
        /// 위치도 초기화 및 캡처 (외부 맵 서비스 - 일반 지도)
        /// </summary>
        private async Task InitializeAndCaptureLocationMapAsync()
        {
            try
            {
                if (!_isLocationMapInitialized)
                {
                    var env = await CoreWebView2Environment.CreateAsync();
                    await LocationMapWebView.EnsureCoreWebView2Async(env);
                    _isLocationMapInitialized = true;
                }

                var locationMapTcs = new TaskCompletionSource<bool>();
                
                void NavigationHandler(object? s, CoreWebView2NavigationCompletedEventArgs args)
                {
                    locationMapTcs.TrySetResult(args.IsSuccess);
                }
                
                LocationMapWebView.CoreWebView2.NavigationCompleted += NavigationHandler;
                
                // NPLogic 지도 서버 - 위치도 모드 (중앙 관리)
                string locationMapUrl = AppConstants.MapServerLocationUrl;
                LocationMapWebView.CoreWebView2.Navigate(locationMapUrl);
                
                // 위치도 로드 완료 대기 (최대 10초)
                var locationMapLoaded = await Task.WhenAny(locationMapTcs.Task, Task.Delay(10000)) == locationMapTcs.Task && locationMapTcs.Task.Result;
                
                LocationMapWebView.CoreWebView2.NavigationCompleted -= NavigationHandler;
                
                if (locationMapLoaded)
                {
                    // 주소가 있으면 검색
                    if (!string.IsNullOrEmpty(_currentAddress))
                    {
                        await SetLocationMapAddressAsync(_currentAddress);
                        await Task.Delay(2000); // 검색 후 대기
                    }
                    else
                    {
                        await Task.Delay(1500); // 기본 로딩 대기
                    }
                    
                    await CaptureLocationMapImageAsync();
                }
                else
                {
                    ShowLocationMapError();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"위치도 초기화 실패: {ex.Message}");
                ShowLocationMapError();
            }
        }

        /// <summary>
        /// 위치도에 주소 설정
        /// </summary>
        private async Task SetLocationMapAddressAsync(string address)
        {
            try
            {
                if (LocationMapWebView.CoreWebView2 == null) return;
                
                string script = $@"
                    (function() {{
                        var textarea = document.getElementById('batchAddressInput');
                        var searchBtn = document.getElementById('batchSearchBtn');
                        if (textarea && searchBtn) {{
                            textarea.value = '{EscapeJsString(address)}';
                            searchBtn.click();
                        }}
                    }})();
                ";
                await LocationMapWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"위치도 주소 설정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 위치도 이미지 캡처
        /// </summary>
        private async Task CaptureLocationMapImageAsync()
        {
            try
            {
                if (LocationMapWebView.CoreWebView2 == null)
                {
                    ShowLocationMapError();
                    return;
                }

                using var stream = new MemoryStream();
                await LocationMapWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                _capturedLocationMapImage = stream.ToArray();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(_capturedLocationMapImage);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                Dispatcher.Invoke(() =>
                {
                    PreviewLocationMapImage.Source = bitmap;
                    LocationMapLoadingState.Visibility = Visibility.Collapsed;
                    LocationMapEmptyState.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"위치도 캡처 실패: {ex.Message}");
                ShowLocationMapError();
            }
        }

        #region 토지이음 자동 캡처

        /// <summary>
        /// 토지이용계획확인원 초기화 및 캡처 (토지이음)
        /// </summary>
        private async Task InitializeAndCaptureLandUsePlanAsync()
        {
            try
            {
                // 주소 파싱
                var addressParts = ParseKoreanAddress(_currentAddress);
                if (addressParts == null)
                {
                    ShowLandUsePlanError("주소 정보가 필요합니다");
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    LandUsePlanLoadingState.Visibility = Visibility.Visible;
                    LandUsePlanEmptyState.Visibility = Visibility.Collapsed;
                    PreviewLandUsePlanImage.Source = null;
                });

                if (!_isLandUsePlanInitialized)
                {
                    var env = await CoreWebView2Environment.CreateAsync();
                    await LandUsePlanWebView.EnsureCoreWebView2Async(env);
                    _isLandUsePlanInitialized = true;
                }

                // 토지이음 페이지로 이동
                var landUseTcs = new TaskCompletionSource<bool>();
                
                void NavigationHandler(object? s, CoreWebView2NavigationCompletedEventArgs args)
                {
                    landUseTcs.TrySetResult(args.IsSuccess);
                }
                
                LandUsePlanWebView.CoreWebView2.NavigationCompleted += NavigationHandler;
                LandUsePlanWebView.CoreWebView2.Navigate("https://www.eum.go.kr/web/ar/lu/luLandDet.jsp");
                
                // 페이지 로드 완료 대기
                var pageLoaded = await Task.WhenAny(landUseTcs.Task, Task.Delay(15000)) == landUseTcs.Task && landUseTcs.Task.Result;
                LandUsePlanWebView.CoreWebView2.NavigationCompleted -= NavigationHandler;
                
                if (!pageLoaded)
                {
                    ShowLandUsePlanError("토지이음 접속 실패");
                    return;
                }

                await Task.Delay(1000); // 페이지 렌더링 대기

                // 자동 검색 실행
                var searchSuccess = await ExecuteLandUsePlanSearchAsync(addressParts);
                
                if (searchSuccess)
                {
                    await Task.Delay(3000); // 검색 결과 렌더링 대기
                    await CaptureLandUsePlanImageAsync();
                }
                else
                {
                    ShowLandUsePlanError("토지이음 조회 실패");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토지이용계획확인원 조회 실패: {ex.Message}");
                ShowLandUsePlanError("조회 중 오류 발생");
            }
        }

        /// <summary>
        /// 토지이음에서 주소 검색 실행
        /// </summary>
        private async Task<bool> ExecuteLandUsePlanSearchAsync(AddressParts addressParts)
        {
            try
            {
                // 시도 선택
                var script1 = $@"
                    (function() {{
                        var sidoSelect = document.querySelector('select[title=""광역시도 선택""]');
                        if (sidoSelect) {{
                            for (var i = 0; i < sidoSelect.options.length; i++) {{
                                if (sidoSelect.options[i].text.includes('{EscapeJsString(addressParts.Sido)}')) {{
                                    sidoSelect.selectedIndex = i;
                                    sidoSelect.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                    return true;
                                }}
                            }}
                        }}
                        return false;
                    }})();
                ";
                await LandUsePlanWebView.CoreWebView2.ExecuteScriptAsync(script1);
                await Task.Delay(800);

                // 시군구 선택
                var script2 = $@"
                    (function() {{
                        var sggSelect = document.querySelector('select[title=""시군구 선택""]');
                        if (sggSelect) {{
                            for (var i = 0; i < sggSelect.options.length; i++) {{
                                if (sggSelect.options[i].text.includes('{EscapeJsString(addressParts.Sigungu)}')) {{
                                    sggSelect.selectedIndex = i;
                                    sggSelect.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                    return true;
                                }}
                            }}
                        }}
                        return false;
                    }})();
                ";
                await LandUsePlanWebView.CoreWebView2.ExecuteScriptAsync(script2);
                await Task.Delay(800);

                // 읍면동 선택
                var script3 = $@"
                    (function() {{
                        var emdSelect = document.querySelector('select[title=""읍면동 선택""]');
                        if (emdSelect) {{
                            for (var i = 0; i < emdSelect.options.length; i++) {{
                                if (emdSelect.options[i].text.includes('{EscapeJsString(addressParts.Dong)}')) {{
                                    emdSelect.selectedIndex = i;
                                    emdSelect.dispatchEvent(new Event('change', {{ bubbles: true }}));
                                    return true;
                                }}
                            }}
                        }}
                        return false;
                    }})();
                ";
                await LandUsePlanWebView.CoreWebView2.ExecuteScriptAsync(script3);
                await Task.Delay(500);

                // 본번/부번 입력
                var script4 = $@"
                    (function() {{
                        var bonbunInput = document.querySelector('input[title=""본번""]');
                        var bubunInput = document.querySelector('input[title=""부번""]');
                        if (bonbunInput) {{
                            bonbunInput.value = '{addressParts.Bonbun}';
                            bonbunInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        }}
                        if (bubunInput && '{addressParts.Bubun}' !== '') {{
                            bubunInput.value = '{addressParts.Bubun}';
                            bubunInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        }}
                        return true;
                    }})();
                ";
                await LandUsePlanWebView.CoreWebView2.ExecuteScriptAsync(script4);
                await Task.Delay(300);

                // 열람 버튼 클릭
                var script5 = @"
                    (function() {
                        var searchBtn = document.querySelector('a.btn_sear');
                        if (searchBtn) {
                            searchBtn.click();
                            return true;
                        }
                        return false;
                    })();
                ";
                var result = await LandUsePlanWebView.CoreWebView2.ExecuteScriptAsync(script5);
                
                return result.Contains("true");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토지이음 검색 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 토지이용계획확인원 캡처
        /// </summary>
        private async Task CaptureLandUsePlanImageAsync()
        {
            try
            {
                if (LandUsePlanWebView.CoreWebView2 == null)
                {
                    ShowLandUsePlanError("WebView 초기화 실패");
                    return;
                }

                using var stream = new MemoryStream();
                await LandUsePlanWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                _capturedLandUsePlanImage = stream.ToArray();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(_capturedLandUsePlanImage);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                Dispatcher.Invoke(() =>
                {
                    PreviewLandUsePlanImage.Source = bitmap;
                    LandUsePlanLoadingState.Visibility = Visibility.Collapsed;
                    LandUsePlanEmptyState.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"토지이용계획확인원 캡처 실패: {ex.Message}");
                ShowLandUsePlanError("캡처 실패");
            }
        }

        /// <summary>
        /// 한국 주소 파싱
        /// </summary>
        private AddressParts? ParseKoreanAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                // 정규식으로 주소 파싱 (아파트 등 부가 정보 포함)
                // 예: "대구광역시 달서구 이곡동 1000-258번지"
                // 예: "서울특별시 강남구 역삼동 123-45 역삼아파트 101동 1501호"
                var pattern = @"^(.+?(?:특별시|광역시|특별자치시|특별자치도|[시도]))\s+(.+?[시군구])\s+(.+?[동읍면리가로])\s+(\d+)(?:-(\d+))?";
                var match = Regex.Match(address, pattern);

                if (match.Success)
                {
                    return new AddressParts
                    {
                        Sido = match.Groups[1].Value.Trim(),
                        Sigungu = match.Groups[2].Value.Trim(),
                        Dong = match.Groups[3].Value.Trim(),
                        Bonbun = match.Groups[4].Value.Trim(),
                        Bubun = match.Groups[5].Success ? match.Groups[5].Value.Trim() : ""
                    };
                }

                // 더 유연한 패턴 시도 - 공백으로 분리
                var parts = address.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    // 시도 찾기
                    string sido = "";
                    string sigungu = "";
                    string dong = "";
                    string jibunStr = "";
                    
                    int idx = 0;
                    
                    // 시도 (특별시, 광역시, 도)
                    if (parts[idx].EndsWith("시") || parts[idx].EndsWith("도"))
                    {
                        sido = parts[idx];
                        idx++;
                    }
                    
                    // 시군구
                    if (idx < parts.Length && (parts[idx].EndsWith("시") || parts[idx].EndsWith("군") || parts[idx].EndsWith("구")))
                    {
                        sigungu = parts[idx];
                        idx++;
                    }
                    
                    // 읍면동리
                    if (idx < parts.Length && (parts[idx].EndsWith("동") || parts[idx].EndsWith("읍") || 
                        parts[idx].EndsWith("면") || parts[idx].EndsWith("리") || parts[idx].EndsWith("가") || parts[idx].EndsWith("로")))
                    {
                        dong = parts[idx];
                        idx++;
                    }
                    
                    // 지번 찾기 (숫자-숫자 또는 숫자 패턴)
                    for (int i = idx; i < parts.Length; i++)
                    {
                        var jibunMatch = Regex.Match(parts[i], @"^(\d+)(?:-(\d+))?(?:번지)?$");
                        if (jibunMatch.Success)
                        {
                            jibunStr = parts[i].Replace("번지", "");
                            break;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(sido) && !string.IsNullOrEmpty(sigungu) && 
                        !string.IsNullOrEmpty(dong) && !string.IsNullOrEmpty(jibunStr))
                    {
                        var jibunParts = jibunStr.Split('-');
                        return new AddressParts
                        {
                            Sido = sido,
                            Sigungu = sigungu,
                            Dong = dong,
                            Bonbun = jibunParts[0],
                            Bubun = jibunParts.Length > 1 ? jibunParts[1] : ""
                        };
                    }
                }

                System.Diagnostics.Debug.WriteLine($"주소 파싱 실패: {address}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"주소 파싱 예외: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 주소 구성 요소
        /// </summary>
        private class AddressParts
        {
            public string Sido { get; set; } = "";
            public string Sigungu { get; set; } = "";
            public string Dong { get; set; } = "";
            public string Bonbun { get; set; } = "";
            public string Bubun { get; set; } = "";
        }

        #endregion

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
                        break;

                    case "geocodeResult":
                        var lat = json.RootElement.GetProperty("lat").GetDouble();
                        var lng = json.RootElement.GetProperty("lng").GetDouble();
                        _currentLat = lat;
                        _currentLng = lng;
                        break;

                    case "centerChanged":
                        var centerLat = json.RootElement.GetProperty("lat").GetDouble();
                        var centerLng = json.RootElement.GetProperty("lng").GetDouble();
                        _currentLat = centerLat;
                        _currentLng = centerLng;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"메시지 처리 실패: {ex.Message}");
            }
        }

        #region UI 이벤트 핸들러

        /// <summary>
        /// 지도 다시 시도 버튼 클릭
        /// </summary>
        private async void RetryMap_Click(object sender, RoutedEventArgs e)
        {
            MapLoadingState.Visibility = Visibility.Visible;
            MapEmptyState.Visibility = Visibility.Collapsed;
            
            try
            {
                if (_isMapReady)
                {
                    await CaptureMapImageAsync();
                }
                else
                {
                    await InitializeAndCaptureAsync();
                }
            }
            catch
            {
                ShowMapError();
            }
        }

        /// <summary>
        /// 위치도 다시 시도 버튼 클릭
        /// </summary>
        private async void RetryLocationMap_Click(object sender, RoutedEventArgs e)
        {
            LocationMapLoadingState.Visibility = Visibility.Visible;
            LocationMapEmptyState.Visibility = Visibility.Collapsed;
            
            try
            {
                await InitializeAndCaptureLocationMapAsync();
            }
            catch
            {
                ShowLocationMapError();
            }
        }

        /// <summary>
        /// 토지이용계획확인원 다시 시도 버튼 클릭
        /// </summary>
        private async void RetryLandUsePlan_Click(object sender, RoutedEventArgs e)
        {
            LandUsePlanLoadingState.Visibility = Visibility.Visible;
            LandUsePlanEmptyState.Visibility = Visibility.Collapsed;
            
            try
            {
                await InitializeAndCaptureLandUsePlanAsync();
            }
            catch
            {
                ShowLandUsePlanError("다시 시도 실패");
            }
        }

        /// <summary>
        /// 토지이용계획확인원 업로드 버튼 클릭
        /// </summary>
        private void UploadLandUsePlan_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "토지이용계획확인원 선택",
                Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.pdf|모든 파일|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _landUsePlanPath = dialog.FileName;
                _capturedLandUsePlanImage = null; // 수동 업로드 시 캡처 이미지 초기화
                LoadPreviewImage(PreviewLandUsePlanImage, LandUsePlanEmptyState, _landUsePlanPath);
                LandUsePlanLoadingState.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 요약건축물대장 업로드 버튼 클릭
        /// </summary>
        private void UploadBuildingRegister_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "요약건축물대장 선택",
                Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.pdf|모든 파일|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _buildingRegisterPath = dialog.FileName;
                LoadPreviewImage(PreviewBuildingRegisterImage, BuildingRegisterEmptyState, _buildingRegisterPath);
            }
        }

        /// <summary>
        /// 건축물대장 세부 업로드 버튼 클릭
        /// </summary>
        private void UploadBuildingDetail_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "건축물대장 세부 선택",
                Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.pdf|모든 파일|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _buildingDetailPath = dialog.FileName;
                LoadPreviewImage(PreviewBuildingDetailImage, BuildingDetailEmptyState, _buildingDetailPath);
            }
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 위치 설정 (외부에서 호출)
        /// </summary>
        public async Task SetLocationAsync(double latitude, double longitude, string? address = null)
        {
            _currentLat = latitude;
            _currentLng = longitude;
            _currentAddress = address;

            if (!_isMapReady)
            {
                _pendingLocation = (latitude, longitude, address);
                return;
            }

            await SetLocationInternalAsync(latitude, longitude, address);
            
            // 위치 변경 후 다시 캡처
            await Task.Delay(1500);
            await CaptureMapImageAsync();
            await InitializeAndCaptureLocationMapAsync();
            await InitializeAndCaptureLandUsePlanAsync();
        }

        /// <summary>
        /// 내부 위치 설정
        /// </summary>
        private async Task SetLocationInternalAsync(double latitude, double longitude, string? address)
        {
            try
            {
                if (!string.IsNullOrEmpty(address))
                {
                    string script = $@"
                        (function() {{
                            var textarea = document.getElementById('batchAddressInput');
                            var searchBtn = document.getElementById('batchSearchBtn');
                            if (textarea && searchBtn) {{
                                textarea.value = '{EscapeJsString(address)}';
                                searchBtn.click();
                            }}
                        }})();
                    ";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"위치 설정 실패: {ex.Message}");
            }
        }

        #endregion

        #region 캡처 기능

        /// <summary>
        /// 지도 이미지 캡처
        /// </summary>
        private async Task CaptureMapImageAsync()
        {
            try
            {
                if (MapWebView.CoreWebView2 == null)
                {
                    ShowMapError();
                    return;
                }

                using var stream = new MemoryStream();
                await MapWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                _capturedMapImage = stream.ToArray();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(_capturedMapImage);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                Dispatcher.Invoke(() =>
                {
                    PreviewMapImage.Source = bitmap;
                    MapLoadingState.Visibility = Visibility.Collapsed;
                    MapEmptyState.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"지도 캡처 실패: {ex.Message}");
                ShowMapError();
            }
        }

        /// <summary>
        /// 파일에서 미리보기 이미지 로드
        /// </summary>
        private void LoadPreviewImage(System.Windows.Controls.Image imageControl, StackPanel emptyState, string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".pdf")
                {
                    emptyState.Visibility = Visibility.Visible;
                    var children = emptyState.Children;
                    if (children.Count > 1 && children[1] is TextBlock textBlock)
                    {
                        textBlock.Text = Path.GetFileName(filePath);
                    }
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                imageControl.Source = bitmap;
                emptyState.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
            }
        }

        #endregion

        #region 에러 표시

        private void ShowMapError()
        {
            Dispatcher.Invoke(() =>
            {
                MapLoadingState.Visibility = Visibility.Collapsed;
                MapEmptyState.Visibility = Visibility.Visible;
            });
        }

        private void ShowLocationMapError()
        {
            Dispatcher.Invoke(() =>
            {
                LocationMapLoadingState.Visibility = Visibility.Collapsed;
                LocationMapEmptyState.Visibility = Visibility.Visible;
            });
        }

        private void ShowLandUsePlanError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LandUsePlanLoadingState.Visibility = Visibility.Collapsed;
                LandUsePlanEmptyState.Visibility = Visibility.Visible;
                LandUsePlanStatusText.Text = message;
            });
        }

        #endregion


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

