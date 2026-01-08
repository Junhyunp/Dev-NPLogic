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

        /// <summary>
        /// 뒤로가기 버튼 클릭 - 대시보드 → 비핵심 → 담보 총괄로 이동
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance?.NavigateToDashboardCollateralSummary();
        }
    }
}

