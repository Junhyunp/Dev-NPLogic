using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 경매일정(Ⅶ) 상세 화면 - 엑셀 산출화면 기반
    /// </summary>
    public partial class AuctionScheduleDetailView : UserControl
    {
        public AuctionScheduleDetailView()
        {
            InitializeComponent();
        }

        private async void AuctionScheduleDetailView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuctionScheduleDetailViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuctionScheduleDetailViewModel viewModel)
            {
                await viewModel.SaveAsync();
            }
        }
    }
}
