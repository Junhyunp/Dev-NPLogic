using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 경매 일정 화면
    /// </summary>
    public partial class AuctionScheduleView : UserControl
    {
        public AuctionScheduleView()
        {
            InitializeComponent();
        }

        private async void AuctionScheduleView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuctionScheduleViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

