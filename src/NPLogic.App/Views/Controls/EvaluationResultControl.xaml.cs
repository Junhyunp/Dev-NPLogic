using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Controls
{
    /// <summary>
    /// 평가결과 컨트롤 (시나리오 1/2)
    /// </summary>
    public partial class EvaluationResultControl : UserControl
    {
        public EvaluationResultControl()
        {
            InitializeComponent();
        }
        
        #region 의존 속성
        
        // 시나리오 1
        public static readonly DependencyProperty Scenario1AmountProperty =
            DependencyProperty.Register("Scenario1Amount", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnScenario1AmountChanged));
        
        public decimal? Scenario1Amount
        {
            get => (decimal?)GetValue(Scenario1AmountProperty);
            set => SetValue(Scenario1AmountProperty, value);
        }
        
        public static readonly DependencyProperty Scenario1RateProperty =
            DependencyProperty.Register("Scenario1Rate", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnScenario1RateChanged));
        
        public decimal? Scenario1Rate
        {
            get => (decimal?)GetValue(Scenario1RateProperty);
            set => SetValue(Scenario1RateProperty, value);
        }
        
        public static readonly DependencyProperty Scenario1ReasonProperty =
            DependencyProperty.Register("Scenario1Reason", typeof(string), typeof(EvaluationResultControl), 
                new PropertyMetadata("하한", OnScenario1ReasonChanged));
        
        public string Scenario1Reason
        {
            get => (string)GetValue(Scenario1ReasonProperty);
            set => SetValue(Scenario1ReasonProperty, value);
        }
        
        // 시나리오 2
        public static readonly DependencyProperty Scenario2AmountProperty =
            DependencyProperty.Register("Scenario2Amount", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnScenario2AmountChanged));
        
        public decimal? Scenario2Amount
        {
            get => (decimal?)GetValue(Scenario2AmountProperty);
            set => SetValue(Scenario2AmountProperty, value);
        }
        
        public static readonly DependencyProperty Scenario2RateProperty =
            DependencyProperty.Register("Scenario2Rate", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnScenario2RateChanged));
        
        public decimal? Scenario2Rate
        {
            get => (decimal?)GetValue(Scenario2RateProperty);
            set => SetValue(Scenario2RateProperty, value);
        }
        
        public static readonly DependencyProperty Scenario2ReasonProperty =
            DependencyProperty.Register("Scenario2Reason", typeof(string), typeof(EvaluationResultControl), 
                new PropertyMetadata("상한", OnScenario2ReasonChanged));
        
        public string Scenario2Reason
        {
            get => (string)GetValue(Scenario2ReasonProperty);
            set => SetValue(Scenario2ReasonProperty, value);
        }
        
        // 공장/창고용 추가 속성
        public static readonly DependencyProperty ShowLandBuildingMachineryProperty =
            DependencyProperty.Register("ShowLandBuildingMachinery", typeof(bool), typeof(EvaluationResultControl), 
                new PropertyMetadata(false, OnShowLandBuildingMachineryChanged));
        
        public bool ShowLandBuildingMachinery
        {
            get => (bool)GetValue(ShowLandBuildingMachineryProperty);
            set => SetValue(ShowLandBuildingMachineryProperty, value);
        }
        
        public static readonly DependencyProperty LandValueProperty =
            DependencyProperty.Register("LandValue", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnLandValueChanged));
        
        public decimal? LandValue
        {
            get => (decimal?)GetValue(LandValueProperty);
            set => SetValue(LandValueProperty, value);
        }
        
        public static readonly DependencyProperty BuildingValueProperty =
            DependencyProperty.Register("BuildingValue", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnBuildingValueChanged));
        
        public decimal? BuildingValue
        {
            get => (decimal?)GetValue(BuildingValueProperty);
            set => SetValue(BuildingValueProperty, value);
        }
        
        public static readonly DependencyProperty MachineryValueProperty =
            DependencyProperty.Register("MachineryValue", typeof(decimal?), typeof(EvaluationResultControl), 
                new PropertyMetadata(null, OnMachineryValueChanged));
        
        public decimal? MachineryValue
        {
            get => (decimal?)GetValue(MachineryValueProperty);
            set => SetValue(MachineryValueProperty, value);
        }
        
        // 저감반영액 표시 여부
        public static readonly DependencyProperty ShowReductionProperty =
            DependencyProperty.Register("ShowReduction", typeof(bool), typeof(EvaluationResultControl), 
                new PropertyMetadata(false, OnShowReductionChanged));
        
        public bool ShowReduction
        {
            get => (bool)GetValue(ShowReductionProperty);
            set => SetValue(ShowReductionProperty, value);
        }
        
        #endregion
        
        #region 속성 변경 핸들러
        
        private static void OnScenario1AmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal amount)
                control.TxtScenario1Amount.Text = $"{amount:N0}원";
        }
        
        private static void OnScenario1RateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal rate)
                control.TxtScenario1Rate.Text = rate.ToString("P1");
        }
        
        private static void OnScenario1ReasonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control)
                control.TxtScenario1Reason.Text = e.NewValue?.ToString() ?? "하한";
        }
        
        private static void OnScenario2AmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal amount)
                control.TxtScenario2Amount.Text = $"{amount:N0}원";
        }
        
        private static void OnScenario2RateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal rate)
                control.TxtScenario2Rate.Text = rate.ToString("P1");
        }
        
        private static void OnScenario2ReasonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control)
                control.TxtScenario2Reason.Text = e.NewValue?.ToString() ?? "상한";
        }
        
        private static void OnShowLandBuildingMachineryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is bool show)
                control.LandBuildingMachinerySection.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private static void OnLandValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal value)
                control.TxtLandValue.Text = value.ToString("N0");
        }
        
        private static void OnBuildingValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal value)
                control.TxtBuildingValue.Text = value.ToString("N0");
        }
        
        private static void OnMachineryValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is decimal value)
                control.TxtMachineryValue.Text = value.ToString("N0");
        }
        
        private static void OnShowReductionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EvaluationResultControl control && e.NewValue is bool show)
            {
                var visibility = show ? Visibility.Visible : Visibility.Collapsed;
                control.LblScenario1Reduction.Visibility = visibility;
                control.TxtScenario1Reduction.Visibility = visibility;
                control.LblScenario2Reduction.Visibility = visibility;
                control.TxtScenario2Reduction.Visibility = visibility;
            }
        }
        
        #endregion
    }
}
