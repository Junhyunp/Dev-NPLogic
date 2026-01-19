using System;
using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Controls
{
    /// <summary>
    /// 회수전략 요약 컨트롤
    /// </summary>
    public partial class RecoveryStrategySummaryControl : UserControl
    {
        public RecoveryStrategySummaryControl()
        {
            InitializeComponent();
        }
        
        #region 의존 속성
        
        // Cap 관련
        public static readonly DependencyProperty IsCapAppliedProperty =
            DependencyProperty.Register("IsCapApplied", typeof(bool), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(false, OnIsCapAppliedChanged));
        
        public bool IsCapApplied
        {
            get => (bool)GetValue(IsCapAppliedProperty);
            set => SetValue(IsCapAppliedProperty, value);
        }
        
        public static readonly DependencyProperty LoanCapProperty =
            DependencyProperty.Register("LoanCap", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnLoanCapChanged));
        
        public decimal? LoanCap
        {
            get => (decimal?)GetValue(LoanCapProperty);
            set => SetValue(LoanCapProperty, value);
        }
        
        public static readonly DependencyProperty LoanCap2Property =
            DependencyProperty.Register("LoanCap2", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnLoanCap2Changed));
        
        public decimal? LoanCap2
        {
            get => (decimal?)GetValue(LoanCap2Property);
            set => SetValue(LoanCap2Property, value);
        }
        
        public static readonly DependencyProperty MortgageCapProperty =
            DependencyProperty.Register("MortgageCap", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnMortgageCapChanged));
        
        public decimal? MortgageCap
        {
            get => (decimal?)GetValue(MortgageCapProperty);
            set => SetValue(MortgageCapProperty, value);
        }
        
        // CF 관련
        public static readonly DependencyProperty CfInDateProperty =
            DependencyProperty.Register("CfInDate", typeof(DateTime?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnCfInDateChanged));
        
        public DateTime? CfInDate
        {
            get => (DateTime?)GetValue(CfInDateProperty);
            set => SetValue(CfInDateProperty, value);
        }
        
        public static readonly DependencyProperty CashInProperty =
            DependencyProperty.Register("CashIn", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnCashInChanged));
        
        public decimal? CashIn
        {
            get => (decimal?)GetValue(CashInProperty);
            set => SetValue(CashInProperty, value);
        }
        
        public static readonly DependencyProperty CashOutProperty =
            DependencyProperty.Register("CashOut", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnCashOutChanged));
        
        public decimal? CashOut
        {
            get => (decimal?)GetValue(CashOutProperty);
            set => SetValue(CashOutProperty, value);
        }
        
        // XNPV / OPB
        public static readonly DependencyProperty RatioProperty =
            DependencyProperty.Register("Ratio", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnRatioChanged));
        
        public decimal? Ratio
        {
            get => (decimal?)GetValue(RatioProperty);
            set => SetValue(RatioProperty, value);
        }
        
        public static readonly DependencyProperty XnpvProperty =
            DependencyProperty.Register("Xnpv", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnXnpvChanged));
        
        public decimal? Xnpv
        {
            get => (decimal?)GetValue(XnpvProperty);
            set => SetValue(XnpvProperty, value);
        }
        
        public static readonly DependencyProperty OpbProperty =
            DependencyProperty.Register("Opb", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnOpbChanged));
        
        public decimal? Opb
        {
            get => (decimal?)GetValue(OpbProperty);
            set => SetValue(OpbProperty, value);
        }
        
        // Interim 상계/회수
        public static readonly DependencyProperty PrincipalRecoveryProperty =
            DependencyProperty.Register("PrincipalRecovery", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnPrincipalRecoveryChanged));
        
        public decimal? PrincipalRecovery
        {
            get => (decimal?)GetValue(PrincipalRecoveryProperty);
            set => SetValue(PrincipalRecoveryProperty, value);
        }
        
        public static readonly DependencyProperty PrincipalOffsetProperty =
            DependencyProperty.Register("PrincipalOffset", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnPrincipalOffsetChanged));
        
        public decimal? PrincipalOffset
        {
            get => (decimal?)GetValue(PrincipalOffsetProperty);
            set => SetValue(PrincipalOffsetProperty, value);
        }
        
        public static readonly DependencyProperty InterestRecoveryProperty =
            DependencyProperty.Register("InterestRecovery", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnInterestRecoveryChanged));
        
        public decimal? InterestRecovery
        {
            get => (decimal?)GetValue(InterestRecoveryProperty);
            set => SetValue(InterestRecoveryProperty, value);
        }
        
        public static readonly DependencyProperty InterestOffsetProperty =
            DependencyProperty.Register("InterestOffset", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnInterestOffsetChanged));
        
        public decimal? InterestOffset
        {
            get => (decimal?)GetValue(InterestOffsetProperty);
            set => SetValue(InterestOffsetProperty, value);
        }
        
        public static readonly DependencyProperty SubrogationProperty =
            DependencyProperty.Register("Subrogation", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnSubrogationChanged));
        
        public decimal? Subrogation
        {
            get => (decimal?)GetValue(SubrogationProperty);
            set => SetValue(SubrogationProperty, value);
        }
        
        public static readonly DependencyProperty AuctionCostProperty =
            DependencyProperty.Register("AuctionCost", typeof(decimal?), typeof(RecoveryStrategySummaryControl), 
                new PropertyMetadata(null, OnAuctionCostChanged));
        
        public decimal? AuctionCost
        {
            get => (decimal?)GetValue(AuctionCostProperty);
            set => SetValue(AuctionCostProperty, value);
        }
        
        #endregion
        
        #region 속성 변경 핸들러
        
        private static void OnIsCapAppliedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control && e.NewValue is bool applied)
                control.TxtCapApplied.Text = applied ? "Y" : "N";
        }
        
        private static void OnLoanCapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtLoanCap.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnLoanCap2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtLoanCap2.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnMortgageCapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtMortgageCap.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnCfInDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtCfInDate.Text = e.NewValue is DateTime dt ? dt.ToString("yyyy-MM-dd") : "-";
        }
        
        private static void OnCashInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtCashIn.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnCashOutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtCashOut.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtRatio.Text = e.NewValue is decimal v ? v.ToString("P1") : "0.0%";
        }
        
        private static void OnXnpvChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtXnpv.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnOpbChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtOpb.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnPrincipalRecoveryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtPrincipalRecovery.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnPrincipalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtPrincipalOffset.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnInterestRecoveryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtInterestRecovery.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnInterestOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtInterestOffset.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnSubrogationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtSubrogation.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        private static void OnAuctionCostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecoveryStrategySummaryControl control)
                control.TxtAuctionCost.Text = e.NewValue is decimal v ? v.ToString("N0") : "0";
        }
        
        #endregion
    }
}
