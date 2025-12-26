using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NPLogic.Controls
{
    public partial class NPButton : UserControl
    {
        public static new readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(ButtonContent), typeof(object), typeof(NPButton), new PropertyMetadata(null));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(NPButton), new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(NPButton), new PropertyMetadata(null));

        public static readonly DependencyProperty ButtonStyleProperty =
            DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(NPButton), new PropertyMetadata(null));

        public static readonly DependencyProperty ButtonTypeProperty =
            DependencyProperty.Register(nameof(ButtonType), typeof(ButtonType), typeof(NPButton), 
                new PropertyMetadata(ButtonType.Primary, OnButtonTypeChanged));

        public object ButtonContent
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public Style ButtonStyle
        {
            get => (Style)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        public ButtonType ButtonType
        {
            get => (ButtonType)GetValue(ButtonTypeProperty);
            set => SetValue(ButtonTypeProperty, value);
        }

        public event RoutedEventHandler? Click;

        public NPButton()
        {
            InitializeComponent();
            UpdateButtonStyle();
        }

        private static void OnButtonTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NPButton button)
            {
                button.UpdateButtonStyle();
            }
        }

        private void UpdateButtonStyle()
        {
            if (ButtonStyle != null)
                return;

            string styleName = ButtonType switch
            {
                ButtonType.Primary => "PrimaryButton",
                ButtonType.Secondary => "SecondaryButton",
                ButtonType.Danger => "DangerButton",
                ButtonType.Icon => "IconButton",
                _ => "PrimaryButton"
            };

            if (TryFindResource(styleName) is Style style)
            {
                ButtonStyle = style;
            }
        }

        private void InnerButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }

    public enum ButtonType
    {
        Primary,
        Secondary,
        Danger,
        Icon
    }
}

