using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NPLogic.Views
{
    /// <summary>
    /// LoginWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginWindow : Window
    {
        private LoginViewModel? _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            
            // DataContext는 App.xaml.cs에서 설정됨
            Loaded += (s, e) =>
            {
                if (DataContext is LoginViewModel vm)
                {
                    _viewModel = vm;
                }
            };
        }

        /// <summary>
        /// PasswordBox는 보안상 바인딩을 지원하지 않으므로 수동으로 처리
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox passwordBox)
            {
                _viewModel.Password = passwordBox.Password;
            }
        }
    }
}

