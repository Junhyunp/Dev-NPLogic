using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// PropertyListView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PropertyListView : UserControl
    {
        public PropertyListView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loaded 이벤트 - ViewModel 초기화
        /// </summary>
        private async void PropertyListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is PropertyListViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}

