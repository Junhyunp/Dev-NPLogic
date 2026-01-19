using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Controls
{
    /// <summary>
    /// 낙찰통계 3x3 매트릭스 컨트롤
    /// </summary>
    public partial class BidStatisticsControl : UserControl
    {
        public BidStatisticsControl()
        {
            InitializeComponent();
        }
        
        #region 의존 속성
        
        // 지역명
        public static readonly DependencyProperty RegionName1Property =
            DependencyProperty.Register("RegionName1", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("시/도", OnRegionName1Changed));
        
        public string RegionName1
        {
            get => (string)GetValue(RegionName1Property);
            set => SetValue(RegionName1Property, value);
        }
        
        public static readonly DependencyProperty RegionName2Property =
            DependencyProperty.Register("RegionName2", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("시/군/구", OnRegionName2Changed));
        
        public string RegionName2
        {
            get => (string)GetValue(RegionName2Property);
            set => SetValue(RegionName2Property, value);
        }
        
        public static readonly DependencyProperty RegionName3Property =
            DependencyProperty.Register("RegionName3", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("동", OnRegionName3Changed));
        
        public string RegionName3
        {
            get => (string)GetValue(RegionName3Property);
            set => SetValue(RegionName3Property, value);
        }
        
        // 1년 평균
        public static readonly DependencyProperty Stats1Year1Property =
            DependencyProperty.Register("Stats1Year1", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats1Year1Changed));
        
        public string Stats1Year1
        {
            get => (string)GetValue(Stats1Year1Property);
            set => SetValue(Stats1Year1Property, value);
        }
        
        public static readonly DependencyProperty Stats1Year2Property =
            DependencyProperty.Register("Stats1Year2", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats1Year2Changed));
        
        public string Stats1Year2
        {
            get => (string)GetValue(Stats1Year2Property);
            set => SetValue(Stats1Year2Property, value);
        }
        
        public static readonly DependencyProperty Stats1Year3Property =
            DependencyProperty.Register("Stats1Year3", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats1Year3Changed));
        
        public string Stats1Year3
        {
            get => (string)GetValue(Stats1Year3Property);
            set => SetValue(Stats1Year3Property, value);
        }
        
        // 6개월 평균
        public static readonly DependencyProperty Stats6Month1Property =
            DependencyProperty.Register("Stats6Month1", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats6Month1Changed));
        
        public string Stats6Month1
        {
            get => (string)GetValue(Stats6Month1Property);
            set => SetValue(Stats6Month1Property, value);
        }
        
        public static readonly DependencyProperty Stats6Month2Property =
            DependencyProperty.Register("Stats6Month2", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats6Month2Changed));
        
        public string Stats6Month2
        {
            get => (string)GetValue(Stats6Month2Property);
            set => SetValue(Stats6Month2Property, value);
        }
        
        public static readonly DependencyProperty Stats6Month3Property =
            DependencyProperty.Register("Stats6Month3", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats6Month3Changed));
        
        public string Stats6Month3
        {
            get => (string)GetValue(Stats6Month3Property);
            set => SetValue(Stats6Month3Property, value);
        }
        
        // 3개월 평균
        public static readonly DependencyProperty Stats3Month1Property =
            DependencyProperty.Register("Stats3Month1", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats3Month1Changed));
        
        public string Stats3Month1
        {
            get => (string)GetValue(Stats3Month1Property);
            set => SetValue(Stats3Month1Property, value);
        }
        
        public static readonly DependencyProperty Stats3Month2Property =
            DependencyProperty.Register("Stats3Month2", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats3Month2Changed));
        
        public string Stats3Month2
        {
            get => (string)GetValue(Stats3Month2Property);
            set => SetValue(Stats3Month2Property, value);
        }
        
        public static readonly DependencyProperty Stats3Month3Property =
            DependencyProperty.Register("Stats3Month3", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("-", OnStats3Month3Changed));
        
        public string Stats3Month3
        {
            get => (string)GetValue(Stats3Month3Property);
            set => SetValue(Stats3Month3Property, value);
        }
        
        // 적용 낙찰가율
        public static readonly DependencyProperty AppliedBidRateProperty =
            DependencyProperty.Register("AppliedBidRate", typeof(decimal?), typeof(BidStatisticsControl), 
                new PropertyMetadata(0.7m, OnAppliedBidRateChanged));
        
        public decimal? AppliedBidRate
        {
            get => (decimal?)GetValue(AppliedBidRateProperty);
            set => SetValue(AppliedBidRateProperty, value);
        }
        
        public static readonly DependencyProperty AppliedBidRateDescriptionProperty =
            DependencyProperty.Register("AppliedBidRateDescription", typeof(string), typeof(BidStatisticsControl), 
                new PropertyMetadata("3개월 평균", OnAppliedBidRateDescriptionChanged));
        
        public string AppliedBidRateDescription
        {
            get => (string)GetValue(AppliedBidRateDescriptionProperty);
            set => SetValue(AppliedBidRateDescriptionProperty, value);
        }
        
        #endregion
        
        #region 속성 변경 핸들러
        
        private static void OnRegionName1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtRegion1.Text = e.NewValue?.ToString() ?? "시/도";
        }
        
        private static void OnRegionName2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtRegion2.Text = e.NewValue?.ToString() ?? "시/군/구";
        }
        
        private static void OnRegionName3Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtRegion3.Text = e.NewValue?.ToString() ?? "동";
        }
        
        private static void OnStats1Year1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats1Y1.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats1Year2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats1Y2.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats1Year3Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats1Y3.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats6Month1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats6M1.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats6Month2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats6M2.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats6Month3Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats6M3.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats3Month1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats3M1.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats3Month2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats3M2.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnStats3Month3Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtStats3M3.Text = e.NewValue?.ToString() ?? "-";
        }
        
        private static void OnAppliedBidRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control && e.NewValue is decimal rate)
                control.TxtAppliedRate.Text = rate.ToString("P1");
        }
        
        private static void OnAppliedBidRateDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BidStatisticsControl control)
                control.TxtAppliedDesc.Text = e.NewValue?.ToString() ?? "";
        }
        
        #endregion
    }
}
