using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// BorrowerOverviewView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BorrowerOverviewView : UserControl
    {
        public BorrowerOverviewView()
        {
            InitializeComponent();
        }

        private async void BorrowerOverviewView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BorrowerOverviewViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

