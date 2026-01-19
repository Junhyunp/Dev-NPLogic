using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 공매일정(Ⅷ) 화면 - 엑셀 산출화면 기반
    /// </summary>
    public partial class PublicSaleScheduleView : UserControl
    {
        public PublicSaleScheduleView()
        {
            InitializeComponent();
        }

        private async void PublicSaleScheduleView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PublicSaleScheduleViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PublicSaleScheduleViewModel viewModel)
            {
                await viewModel.SaveAsync();
            }
        }
    }
}
