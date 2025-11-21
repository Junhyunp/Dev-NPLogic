using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// PropertyDetailView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PropertyDetailView : UserControl
    {
        public PropertyDetailView()
        {
            InitializeComponent();
        }

        private async void PropertyDetailView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

