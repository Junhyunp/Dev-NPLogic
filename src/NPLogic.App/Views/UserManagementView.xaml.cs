using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// UserManagementView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
        }

        private async void UserManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel)
            {
                // PasswordBox 바인딩 설정 (보안상 직접 바인딩 불가)
                viewModel.SetPasswordProvider(() => PasswordBox.Password);
                
                await viewModel.InitializeAsync();
            }
        }
    }
}

