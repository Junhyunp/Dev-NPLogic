using System;
using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Controls
{
    /// <summary>
    /// 사례 로드뷰 컨트롤 (Kakao 로드뷰 API 연동)
    /// </summary>
    public partial class CaseRoadViewControl : UserControl
    {
        public CaseRoadViewControl()
        {
            InitializeComponent();
        }
        
        #region 의존 속성
        
        // 사례 1~4 좌표
        public static readonly DependencyProperty Case1LatitudeProperty =
            DependencyProperty.Register("Case1Latitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null, OnCase1LocationChanged));
        
        public double? Case1Latitude
        {
            get => (double?)GetValue(Case1LatitudeProperty);
            set => SetValue(Case1LatitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case1LongitudeProperty =
            DependencyProperty.Register("Case1Longitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null, OnCase1LocationChanged));
        
        public double? Case1Longitude
        {
            get => (double?)GetValue(Case1LongitudeProperty);
            set => SetValue(Case1LongitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case2LatitudeProperty =
            DependencyProperty.Register("Case2Latitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null));
        
        public double? Case2Latitude
        {
            get => (double?)GetValue(Case2LatitudeProperty);
            set => SetValue(Case2LatitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case2LongitudeProperty =
            DependencyProperty.Register("Case2Longitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null));
        
        public double? Case2Longitude
        {
            get => (double?)GetValue(Case2LongitudeProperty);
            set => SetValue(Case2LongitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case3LatitudeProperty =
            DependencyProperty.Register("Case3Latitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null));
        
        public double? Case3Latitude
        {
            get => (double?)GetValue(Case3LatitudeProperty);
            set => SetValue(Case3LatitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case3LongitudeProperty =
            DependencyProperty.Register("Case3Longitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null));
        
        public double? Case3Longitude
        {
            get => (double?)GetValue(Case3LongitudeProperty);
            set => SetValue(Case3LongitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case4LatitudeProperty =
            DependencyProperty.Register("Case4Latitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null));
        
        public double? Case4Latitude
        {
            get => (double?)GetValue(Case4LatitudeProperty);
            set => SetValue(Case4LatitudeProperty, value);
        }
        
        public static readonly DependencyProperty Case4LongitudeProperty =
            DependencyProperty.Register("Case4Longitude", typeof(double?), typeof(CaseRoadViewControl), 
                new PropertyMetadata(null));
        
        public double? Case4Longitude
        {
            get => (double?)GetValue(Case4LongitudeProperty);
            set => SetValue(Case4LongitudeProperty, value);
        }
        
        #endregion
        
        #region 이벤트 핸들러
        
        private static void OnCase1LocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CaseRoadViewControl control)
            {
                control.LoadRoadViewImage(1, control.Case1Latitude, control.Case1Longitude);
            }
        }
        
        private void LoadRoadViewImage(int caseNumber, double? latitude, double? longitude)
        {
            // TODO: Kakao 로드뷰 API 연동하여 이미지 로드
            // 현재는 플레이스홀더만 표시
        }
        
        private void RoadView1_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowRoadViewWindow(1, Case1Latitude, Case1Longitude);
        }
        
        private void RoadView2_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowRoadViewWindow(2, Case2Latitude, Case2Longitude);
        }
        
        private void RoadView3_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowRoadViewWindow(3, Case3Latitude, Case3Longitude);
        }
        
        private void RoadView4_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowRoadViewWindow(4, Case4Latitude, Case4Longitude);
        }
        
        private void ShowRoadViewWindow(int caseNumber, double? latitude, double? longitude)
        {
            var message = $"사례{caseNumber} 로드뷰";
            
            if (latitude.HasValue && longitude.HasValue)
            {
                message += $"\n\n좌표: {latitude:F6}, {longitude:F6}\n\nKakao 로드뷰 API 연동 후 표시됩니다.";
            }
            else
            {
                message += "\n\n좌표 정보가 없습니다.\n사례를 먼저 적용해주세요.";
            }
            
            MessageBox.Show(message, $"사례{caseNumber} 로드뷰", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        #endregion
    }
}
