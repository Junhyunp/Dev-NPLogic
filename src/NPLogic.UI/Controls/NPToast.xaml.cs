using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace NPLogic.Controls
{
    public partial class NPToast : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(NPToast), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(NPToast), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ToastTypeProperty =
            DependencyProperty.Register(nameof(ToastType), typeof(ToastType), typeof(NPToast), 
                new PropertyMetadata(ToastType.Info, OnToastTypeChanged));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(int), typeof(NPToast), new PropertyMetadata(3000));

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register(nameof(ShowCloseButton), typeof(bool), typeof(NPToast), new PropertyMetadata(true));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public ToastType ToastType
        {
            get => (ToastType)GetValue(ToastTypeProperty);
            set => SetValue(ToastTypeProperty, value);
        }

        public int Duration
        {
            get => (int)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        public event EventHandler? Closed;

        public NPToast()
        {
            InitializeComponent();
            UpdateToastStyle();
        }

        private static void OnToastTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NPToast toast)
            {
                toast.UpdateToastStyle();
            }
        }

        private void UpdateToastStyle()
        {
            var (iconBg, iconColor, iconData) = ToastType switch
            {
                ToastType.Success => (
                    TryFindResource("SuccessBgBrush"),
                    TryFindResource("SuccessBrush"),
                    "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z"
                ),
                ToastType.Warning => (
                    TryFindResource("WarningBgBrush"),
                    TryFindResource("WarningBrush"),
                    "M13,13H11V7H13M13,17H11V15H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                ),
                ToastType.Error => (
                    TryFindResource("ErrorBgBrush"),
                    TryFindResource("ErrorBrush"),
                    "M13,13H11V7H13M13,17H11V15H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                ),
                ToastType.Info => (
                    TryFindResource("InfoBgBrush"),
                    TryFindResource("InfoBrush"),
                    "M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                ),
                _ => (
                    TryFindResource("InfoBgBrush"),
                    TryFindResource("InfoBrush"),
                    "M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                )
            };

            if (iconBg is SolidColorBrush bgBrush)
                IconBorder.Background = bgBrush;

            if (iconColor is SolidColorBrush colorBrush)
                IconPath.Fill = colorBrush;

            IconPath.Data = Geometry.Parse(iconData);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Show();
        }

        public void Show()
        {
            // Fade in animation
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            ToastBorder.BeginAnimation(OpacityProperty, fadeIn);

            // Auto hide after duration
            if (Duration > 0)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(Duration)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    Hide();
                };
                timer.Start();
            }
        }

        public void Hide()
        {
            // Fade out animation
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, args) =>
            {
                Visibility = Visibility.Collapsed;
                Closed?.Invoke(this, EventArgs.Empty);
            };
            ToastBorder.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }

    public enum ToastType
    {
        Success,
        Warning,
        Error,
        Info
    }
}

