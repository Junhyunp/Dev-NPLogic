using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 프로그램 관리 화면
    /// </summary>
    public partial class ProgramManagementView : UserControl
    {
        public ProgramManagementView()
        {
            InitializeComponent();
        }

        private async void ProgramManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProgramManagementViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

