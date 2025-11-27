using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    public partial class XnpvComparisonView : UserControl
    {
        public XnpvComparisonView()
        {
            InitializeComponent();
        }

        private async void XnpvComparisonView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is XnpvComparisonViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

