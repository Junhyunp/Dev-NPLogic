using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 작업 이력 화면
    /// </summary>
    public partial class AuditLogsView : UserControl
    {
        public AuditLogsView()
        {
            InitializeComponent();
        }

        private async void AuditLogsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuditLogsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

