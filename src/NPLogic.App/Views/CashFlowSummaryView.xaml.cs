using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// CashFlowSummaryView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CashFlowSummaryView : UserControl
    {
        public CashFlowSummaryView()
        {
            InitializeComponent();
        }

        private async void CashFlowSummaryView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CashFlowSummaryViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

