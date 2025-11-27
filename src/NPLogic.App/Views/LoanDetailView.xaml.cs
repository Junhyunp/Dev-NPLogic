using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// LoanDetailView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoanDetailView : UserControl
    {
        public LoanDetailView()
        {
            InitializeComponent();
        }

        private async void LoanDetailView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoanDetailViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

