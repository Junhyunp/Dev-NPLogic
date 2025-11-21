using System.Windows;
using System.Windows.Controls;

namespace NPLogic.UI.Services
{
    public class ToastService
    {
        private static ToastService? _instance;
        private Panel? _container;

        public static ToastService Instance => _instance ??= new ToastService();

        private ToastService() { }

        public void Initialize(Panel container)
        {
            _container = container;
        }

        public void ShowSuccess(string message, string title = "", int duration = 3000)
        {
            Show(message, title, Controls.ToastType.Success, duration);
        }

        public void ShowWarning(string message, string title = "", int duration = 3000)
        {
            Show(message, title, Controls.ToastType.Warning, duration);
        }

        public void ShowError(string message, string title = "", int duration = 3000)
        {
            Show(message, title, Controls.ToastType.Error, duration);
        }

        public void ShowInfo(string message, string title = "", int duration = 3000)
        {
            Show(message, title, Controls.ToastType.Info, duration);
        }

        private void Show(string message, string title, Controls.ToastType type, int duration)
        {
            if (_container == null)
            {
                throw new InvalidOperationException("ToastService not initialized. Call Initialize() first.");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new Controls.NPToast
                {
                    Title = title,
                    Message = message,
                    ToastType = type,
                    Duration = duration,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 16, 16, 0)
                };

                toast.Closed += (s, e) =>
                {
                    _container.Children.Remove(toast);
                };

                _container.Children.Add(toast);

                // Position toasts vertically
                PositionToasts();
            });
        }

        private void PositionToasts()
        {
            if (_container == null) return;

            double topOffset = 16;
            foreach (UIElement child in _container.Children)
            {
                if (child is Controls.NPToast toast)
                {
                    toast.Margin = new Thickness(0, topOffset, 16, 0);
                    topOffset += toast.ActualHeight + 8;
                }
            }
        }
    }
}

