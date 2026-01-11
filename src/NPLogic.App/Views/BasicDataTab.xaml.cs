using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        #region 팝업 이벤트 핸들러 (D-004~D-009)

        /// <summary>
        /// 지도 팝업 확대 (D-004~D-006)
        /// </summary>
        private void OpenMapPopup_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.Property?.AddressFull))
            {
                var popup = new ImagePopupWindow(
                    "지도 확대",
                    viewModel.Property.AddressFull,
                    ImagePopupWindow.PopupType.Map);
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
        }

        /// <summary>
        /// 토지이용계획 팝업 (D-007)
        /// </summary>
        private void OpenLandUsePlanPopup_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.Property?.AddressFull))
            {
                // 토지이용계획 외부 사이트로 이동
                try
                {
                    var encodedAddress = Uri.EscapeDataString(viewModel.Property.AddressFull);
                    var url = $"https://www.eum.go.kr/web/ar/lu/luLandDet.jsp";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"토지이용계획 열기 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 건축물대장 팝업 (D-008)
        /// </summary>
        private void OpenBuildingRegisterPopup_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.Property?.AddressFull))
            {
                // 건축물대장 외부 사이트로 이동 (정부24)
                try
                {
                    var url = "https://www.gov.kr/portal/service/serviceInfo/B55001000021";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"건축물대장 열기 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 등기부 요약 캡처 팝업 (D-009) - MouseLeftButtonUp 핸들러
        /// </summary>
        private void OpenRegistrySummaryPopup_Click(object sender, MouseButtonEventArgs e)
        {
            OpenRegistrySummaryPopupInternal();
        }

        /// <summary>
        /// 등기부 요약 캡처 팝업 (D-009) - Button Click 핸들러
        /// </summary>
        private void OpenRegistrySummaryPopup_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenRegistrySummaryPopupInternal();
        }

        /// <summary>
        /// 등기부 요약 캡처 팝업 내부 메서드
        /// </summary>
        private void OpenRegistrySummaryPopupInternal()
        {
            if (DataContext is PropertyDetailViewModel viewModel && viewModel.HasRegistrySummaryImage)
            {
                var popup = new ImagePopupWindow(
                    "등기부 요약",
                    viewModel.RegistrySummaryImagePath ?? "",
                    ImagePopupWindow.PopupType.Image);
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
        }

        #endregion

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
        /// 지도 업데이트 (외부 맵 서비스 연동)
        /// </summary>
        private void UpdateMap(string? address)
        {
            if (!_isWebViewInitialized || string.IsNullOrWhiteSpace(address)) return;

            try
            {
                // NPLogic 지도 서버 (중앙 관리)
                string mapUrl = AppConstants.MapServerUrl;
                
                // WebView를 외부 맵 서비스로 이동
                MapWebView.Source = new Uri(mapUrl);

                // 페이지 로드 완료 시 검색 자동 실행
                EventHandler<CoreWebView2NavigationCompletedEventArgs>? handler = null;
                handler = async (s, args) =>
                {
                    MapWebView.CoreWebView2.NavigationCompleted -= handler;
                    if (args.IsSuccess)
                    {
                        // 주소 입력 및 검색 버튼 클릭 스크립트 실행
                        string script = $@"
                            (function() {{
                                var textarea = document.getElementById('batchAddressInput');
                                var searchBtn = document.getElementById('batchSearchBtn');
                                if (textarea && searchBtn) {{
                                    textarea.value = '{EscapeJavaScript(address)}';
                                    searchBtn.click();
                                }}
                            }})();
                        ";
                        await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                };
                MapWebView.CoreWebView2.NavigationCompleted += handler;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"외부 맵 로드 실패: {ex.Message}");
            }
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
