using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;

namespace NPLogic.Views
{
    /// <summary>
    /// 이미지/지도 팝업 윈도우
    /// </summary>
    public partial class ImagePopupWindow : Window
    {
        public enum PopupType
        {
            Image,
            Map
        }

        public new string Title { get; set; } = "";
        public string SubTitle { get; set; } = "";

        private readonly PopupType _popupType;
        private readonly string _content;

        public ImagePopupWindow(string title, string content, PopupType type)
        {
            InitializeComponent();
            DataContext = this;

            Title = title;
            SubTitle = content;
            _popupType = type;
            _content = content;

            Loaded += ImagePopupWindow_Loaded;
        }

        private async void ImagePopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_popupType)
                {
                    case PopupType.Map:
                        MapContainer.Visibility = Visibility.Visible;
                        ImageContainer.Visibility = Visibility.Collapsed;
                        await LoadMapAsync();
                        break;

                    case PopupType.Image:
                        MapContainer.Visibility = Visibility.Collapsed;
                        ImageContainer.Visibility = Visibility.Visible;
                        LoadImage();
                        break;
                }

                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"팝업 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 지도 로드
        /// </summary>
        private async System.Threading.Tasks.Task LoadMapAsync()
        {
            try
            {
                await PopupMapWebView.EnsureCoreWebView2Async();

                // NPLogic 지도 서버 사용
                string mapUrl = AppConstants.MapServerUrl;
                PopupMapWebView.Source = new Uri(mapUrl);

                // 페이지 로드 완료 시 검색 자동 실행
                EventHandler<CoreWebView2NavigationCompletedEventArgs>? handler = null;
                handler = async (s, args) =>
                {
                    PopupMapWebView.CoreWebView2.NavigationCompleted -= handler;
                    if (args.IsSuccess)
                    {
                        string script = $@"
                            (function() {{
                                var textarea = document.getElementById('batchAddressInput');
                                var searchBtn = document.getElementById('batchSearchBtn');
                                if (textarea && searchBtn) {{
                                    textarea.value = '{EscapeJavaScript(_content)}';
                                    searchBtn.click();
                                }}
                            }})();
                        ";
                        await PopupMapWebView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                };
                PopupMapWebView.CoreWebView2.NavigationCompleted += handler;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"지도 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 이미지 로드
        /// </summary>
        private void LoadImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(_content))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    
                    // URL 또는 로컬 파일 경로 지원
                    if (_content.StartsWith("http://") || _content.StartsWith("https://"))
                    {
                        bitmap.UriSource = new Uri(_content);
                    }
                    else if (System.IO.File.Exists(_content))
                    {
                        bitmap.UriSource = new Uri(_content);
                    }
                    else
                    {
                        Debug.WriteLine($"이미지 파일을 찾을 수 없음: {_content}");
                        return;
                    }
                    
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PopupImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
