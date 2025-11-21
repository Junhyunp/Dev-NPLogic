using System.Windows;
using System.Windows.Controls;

namespace NPLogic.UI.Controls
{
    public partial class NPProgressBar : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(NPProgressBar), 
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NPProgressBar), 
                new PropertyMetadata(100.0, OnValueChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(NPProgressBar), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowPercentageProperty =
            DependencyProperty.Register(nameof(ShowPercentage), typeof(bool), typeof(NPProgressBar), new PropertyMetadata(true));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public bool ShowPercentage
        {
            get => (bool)GetValue(ShowPercentageProperty);
            set => SetValue(ShowPercentageProperty, value);
        }

        public NPProgressBar()
        {
            InitializeComponent();
            UpdateProgress();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NPProgressBar progressBar)
            {
                progressBar.UpdateProgress();
            }
        }

        private void UpdateProgress()
        {
            if (ProgressFill == null || ActualWidth == 0)
                return;

            double percentage = Maximum > 0 ? (Value / Maximum) * 100 : 0;
            percentage = Math.Max(0, Math.Min(100, percentage));

            // -2 for border
            double maxWidth = ActualWidth - 2;
            ProgressFill.Width = (maxWidth * percentage) / 100;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateProgress();
        }
    }
}

