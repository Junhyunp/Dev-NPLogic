using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 공매 일정 화면
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
    }
}

