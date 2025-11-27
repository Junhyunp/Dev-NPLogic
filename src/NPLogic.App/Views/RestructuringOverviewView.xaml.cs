using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    public partial class RestructuringOverviewView : UserControl
    {
        public RestructuringOverviewView()
        {
            InitializeComponent();
        }

        private async void RestructuringOverviewView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RestructuringOverviewViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

