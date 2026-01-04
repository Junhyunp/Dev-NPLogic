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
            // DataContext가 PropertyDetailViewModel인 경우 (기존 방식)
            if (DataContext is PropertyDetailViewModel propertyViewModel && propertyViewModel.RightsAnalysisViewModel != null)
            {
                // 권리분석 탭의 DataContext를 RightsAnalysisViewModel로 설정
                DataContext = propertyViewModel.RightsAnalysisViewModel;
                
                // 데이터 로드
                await propertyViewModel.RightsAnalysisViewModel.LoadDataAsync();
            }
            // DataContext가 RightsAnalysisTabViewModel인 경우 (AdminHomeView에서 사용)
            else if (DataContext is RightsAnalysisTabViewModel rightsVm)
            {
                // 데이터 로드는 이미 AdminHomeView에서 호출됨
                // 추가 초기화가 필요하면 여기서 수행
            }
        }
    }
}

