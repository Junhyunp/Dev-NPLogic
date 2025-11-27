using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// CollateralSummaryView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CollateralSummaryView : UserControl
    {
        public CollateralSummaryView()
        {
            InitializeComponent();
        }

        private async void CollateralSummaryView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CollateralSummaryViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

