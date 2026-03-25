using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 홈 탭 - 물건 요약 정보 표시
    /// </summary>
    public partial class HomeTab : UserControl
    {
        public HomeTab()
        {
            InitializeComponent();
            Loaded += HomeTab_Loaded;
        }

        private async void HomeTab_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel vm)
            {
                await vm.LoadNoteSummaryAsync();
            }
        }
    }
}
