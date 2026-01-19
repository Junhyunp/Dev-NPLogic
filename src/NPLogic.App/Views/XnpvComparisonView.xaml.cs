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
            System.Diagnostics.Debug.WriteLine($"[XnpvComparisonView] Loaded, DataContext: {DataContext?.GetType().Name ?? "null"}");
            
            if (DataContext is XnpvComparisonViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("[XnpvComparisonView] InitializeAsync 시작");
                await viewModel.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("[XnpvComparisonView] InitializeAsync 완료");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[XnpvComparisonView] DataContext is not XnpvComparisonViewModel!");
            }
        }
    }
}

