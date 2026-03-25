using System;
using System.Diagnostics;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace NPLogic.Views
{
    /// <summary>
    /// 지도 확대 팝업 윈도우 - 위성도/지적도/로드뷰를 큰 화면으로 표시
    /// </summary>
    public partial class MapPopupWindow : Window
    {
        private readonly string _htmlContent;

        /// <summary>
        /// 지도 팝업 윈도우 생성
        /// </summary>
        /// <param name="title">윈도우 제목 (위성도/지적도/로드뷰)</param>
        /// <param name="iconKind">헤더 아이콘 (PackIconKind)</param>
        /// <param name="htmlContent">WebView2에 로드할 HTML 콘텐츠</param>
        public MapPopupWindow(string title, PackIconKind iconKind, string htmlContent)
        {
            InitializeComponent();

            Title = title;
            HeaderTitle.Text = title;
            HeaderIcon.Kind = iconKind;
            _htmlContent = htmlContent;

            Loaded += MapPopupWindow_Loaded;
        }

        private async void MapPopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await PopupWebView.EnsureCoreWebView2Async(null);
                PopupWebView.NavigateToString(_htmlContent);
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPopupWindow] WebView2 초기화 실패: {ex.Message}");
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
