using System;
using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Controls
{
    /// <summary>
    /// 사례지도 컨트롤
    /// </summary>
    public partial class CaseMapControl : UserControl
    {
        public CaseMapControl()
        {
            InitializeComponent();
        }
        
        #region 의존 속성
        
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(CaseMapControl), 
                new PropertyMetadata(null, OnAddressChanged));
        
        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }
        
        public static readonly DependencyProperty LatitudeProperty =
            DependencyProperty.Register("Latitude", typeof(double?), typeof(CaseMapControl), 
                new PropertyMetadata(null));
        
        public double? Latitude
        {
            get => (double?)GetValue(LatitudeProperty);
            set => SetValue(LatitudeProperty, value);
        }
        
        public static readonly DependencyProperty LongitudeProperty =
            DependencyProperty.Register("Longitude", typeof(double?), typeof(CaseMapControl), 
                new PropertyMetadata(null));
        
        public double? Longitude
        {
            get => (double?)GetValue(LongitudeProperty);
            set => SetValue(LongitudeProperty, value);
        }
        
        #endregion
        
        #region 이벤트
        
        public event EventHandler? LoadMapRequested;
        public event EventHandler? ExpandMapRequested;
        
        #endregion
        
        #region 이벤트 핸들러
        
        private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CaseMapControl control && e.NewValue is string address)
            {
                // 주소가 변경되면 지도 업데이트
                control.UpdateMapForAddress(address);
            }
        }
        
        private void UpdateMapForAddress(string address)
        {
            // TODO: WebView2를 사용하여 지도 표시
            // 현재는 플레이스홀더만 표시
        }
        
        private void BtnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            LoadMapRequested?.Invoke(this, EventArgs.Empty);
            
            // 기본 동작: 안내 메시지 표시
            if (LoadMapRequested == null)
            {
                MessageBox.Show(
                    "사례지도 기능은 추후 구현 예정입니다.\n\n" +
                    "Naver/Kakao Map API를 연동하여 본건과 사례 위치를 표시합니다.",
                    "사례지도",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        
        private void BtnExpandMap_Click(object sender, RoutedEventArgs e)
        {
            ExpandMapRequested?.Invoke(this, EventArgs.Empty);
            
            if (ExpandMapRequested == null)
            {
                ShowExpandedMapWindow();
            }
        }
        
        private void MapContainer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowExpandedMapWindow();
        }
        
        private void ShowExpandedMapWindow()
        {
            // 확대 지도 창 표시
            var mapWindow = new Window
            {
                Title = "사례지도 - 확대보기",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new Grid
                {
                    Background = System.Windows.Media.Brushes.LightGray,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"확대 지도 (추후 WebView2 연동 예정)\n\n주소: {Address ?? "미설정"}",
                            FontSize = 16,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center
                        }
                    }
                }
            };
            mapWindow.ShowDialog();
        }
        
        #endregion
    }
}
