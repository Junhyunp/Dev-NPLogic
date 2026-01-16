using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 경(공)매 일정 상세 화면 - 산출화면 기반
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

        private void ScheduleType_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuctionScheduleDetailViewModel viewModel)
            {
                bool isAuction = AuctionTab.IsChecked == true;
                viewModel.IsAuction = isAuction;
                
                // UI 라벨 업데이트
                UpdateLabels(isAuction);
            }
        }

        private void UpdateLabels(bool isAuction)
        {
            if (isAuction)
            {
                SaleStartLabel.Text = "경매개시일";
                SaleCostLabel.Text = "경매비용";
                CostItem1Label.Text = "경매비용";
                SaleCostRowLabel.Text = "경매비용";
                LeadTimeLabel.Text = "* 경매 Lead time";
                DiscountRateLabel.Text = "* 경매 저감율";
            }
            else
            {
                SaleStartLabel.Text = "공매개시일";
                SaleCostLabel.Text = "공매비용";
                CostItem1Label.Text = "온비드수수료";
                SaleCostRowLabel.Text = "공매비용";
                LeadTimeLabel.Text = "* 공매 Lead time";
                DiscountRateLabel.Text = "* 공매 저감율";
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
