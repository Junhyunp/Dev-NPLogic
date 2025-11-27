using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// StatisticsView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StatisticsView : UserControl
    {
        public StatisticsView()
        {
            InitializeComponent();
        }

        private async void StatisticsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is StatisticsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

