using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// RightsAnalysisTab.xaml의 코드 비하인드
    /// </summary>
    public partial class RightsAnalysisTab : UserControl
    {
        public RightsAnalysisTab()
        {
            InitializeComponent();
        }

        private async void RightsAnalysisTab_Loaded(object sender, RoutedEventArgs e)
        {
            // DataContext가 PropertyDetailViewModel인 경우 권리분석 ViewModel로 DataContext 변경
            if (DataContext is PropertyDetailViewModel propertyViewModel && propertyViewModel.RightsAnalysisViewModel != null)
            {
                // 권리분석 탭의 DataContext를 RightsAnalysisViewModel로 설정
                DataContext = propertyViewModel.RightsAnalysisViewModel;
                
                // 데이터 로드
                await propertyViewModel.RightsAnalysisViewModel.LoadDataAsync();
            }
        }
    }
}

