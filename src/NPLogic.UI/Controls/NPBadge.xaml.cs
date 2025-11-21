using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NPLogic.UI.Controls
{
    public partial class NPBadge : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(NPBadge), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty BadgeTypeProperty =
            DependencyProperty.Register(nameof(BadgeType), typeof(BadgeType), typeof(NPBadge), 
                new PropertyMetadata(BadgeType.Info, OnBadgeTypeChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public BadgeType BadgeType
        {
            get => (BadgeType)GetValue(BadgeTypeProperty);
            set => SetValue(BadgeTypeProperty, value);
        }

        public NPBadge()
        {
            InitializeComponent();
            UpdateBadgeStyle();
        }

        private static void OnBadgeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NPBadge badge)
            {
                badge.UpdateBadgeStyle();
            }
        }

        private void UpdateBadgeStyle()
        {
            var (bgBrush, textBrush) = BadgeType switch
            {
                BadgeType.Success => (TryFindResource("SuccessBgBrush"), TryFindResource("SuccessTextBrush")),
                BadgeType.Warning => (TryFindResource("WarningBgBrush"), TryFindResource("WarningTextBrush")),
                BadgeType.Error => (TryFindResource("ErrorBgBrush"), TryFindResource("ErrorTextBrush")),
                BadgeType.Info => (TryFindResource("InfoBgBrush"), TryFindResource("InfoTextBrush")),
                _ => (TryFindResource("InfoBgBrush"), TryFindResource("InfoTextBrush"))
            };

            if (bgBrush is SolidColorBrush background)
                BadgeBorder.Background = background;

            if (textBrush is SolidColorBrush foreground)
                BadgeText.Foreground = foreground;
        }
    }

    public enum BadgeType
    {
        Success,
        Warning,
        Error,
        Info
    }
}

