using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 선순위 관리 화면
    /// </summary>
    public partial class SeniorRightsView : UserControl
    {
        public SeniorRightsView()
        {
            InitializeComponent();
        }

        private async void SeniorRightsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SeniorRightsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

