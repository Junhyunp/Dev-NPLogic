using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// ToolBoxView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ToolBoxView : UserControl
    {
        public ToolBoxView()
        {
            InitializeComponent();
        }

        private async void ToolBoxView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolBoxViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

