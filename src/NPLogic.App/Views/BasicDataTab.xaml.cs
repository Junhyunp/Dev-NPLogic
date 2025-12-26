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
        /// 지도 업데이트 (외부 맵 서비스 연동)
        /// </summary>
        private void UpdateMap(string? address)
        {
            if (!_isWebViewInitialized || string.IsNullOrWhiteSpace(address)) return;

            try
            {
                // 외부 맵 주소
                string mapUrl = "http://54.116.25.55:8080/";
                
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
