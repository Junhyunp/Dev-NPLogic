using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NPLogic.UI.Controls
{
    public partial class NPModal : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(NPModal), new PropertyMetadata(string.Empty));

        public static new readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(ModalContent), typeof(object), typeof(NPModal), new PropertyMetadata(null));

        public static readonly DependencyProperty FooterContentProperty =
            DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(NPModal), new PropertyMetadata(null));

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(NPModal), 
                new PropertyMetadata(false, OnIsOpenChanged));

        public static readonly DependencyProperty CloseOnOverlayClickProperty =
            DependencyProperty.Register(nameof(CloseOnOverlayClick), typeof(bool), typeof(NPModal), new PropertyMetadata(true));

        public static readonly DependencyProperty ModalMaxWidthProperty =
            DependencyProperty.Register(nameof(ModalMaxWidth), typeof(double), typeof(NPModal), new PropertyMetadata(800.0));

        public static readonly DependencyProperty ModalMaxHeightProperty =
            DependencyProperty.Register(nameof(ModalMaxHeight), typeof(double), typeof(NPModal), new PropertyMetadata(600.0));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public object ModalContent
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public object FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public bool CloseOnOverlayClick
        {
            get => (bool)GetValue(CloseOnOverlayClickProperty);
            set => SetValue(CloseOnOverlayClickProperty, value);
        }

        public double ModalMaxWidth
        {
            get => (double)GetValue(ModalMaxWidthProperty);
            set => SetValue(ModalMaxWidthProperty, value);
        }

        public double ModalMaxHeight
        {
            get => (double)GetValue(ModalMaxHeightProperty);
            set => SetValue(ModalMaxHeightProperty, value);
        }

        public event EventHandler? Closed;

        public NPModal()
        {
            InitializeComponent();
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NPModal modal)
            {
                modal.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CloseOnOverlayClick && e.OriginalSource == ModalOverlay)
            {
                Close();
            }
        }

        private void Dialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent closing when clicking inside the dialog
            e.Handled = true;
        }

        public void Open()
        {
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}

