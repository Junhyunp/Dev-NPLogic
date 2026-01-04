using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// RegistryTab.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RegistryTab : UserControl
    {
        public RegistryTab()
        {
            InitializeComponent();
        }

        private async void RegistryTab_Loaded(object sender, RoutedEventArgs e)
        {
            // DataContext가 PropertyDetailViewModel인 경우 (기존 방식)
            if (DataContext is PropertyDetailViewModel viewModel && viewModel.RegistryViewModel != null)
            {
                // 등기부 데이터 로드
                await viewModel.RegistryViewModel.LoadDataAsync();
                
                // OCR 서버 상태 확인 (백그라운드에서)
                _ = viewModel.RegistryViewModel.CheckOcrServerStatusAsync();
            }
            // DataContext가 동적 래퍼인 경우 (AdminHomeView에서 사용)
            else if (DataContext is System.Dynamic.ExpandoObject expando)
            {
                var dict = (IDictionary<string, object?>)expando;
                if (dict.TryGetValue("RegistryViewModel", out var vm) && vm is RegistryTabViewModel registryVm)
                {
                    // 데이터 로드는 이미 AdminHomeView에서 호출됨
                    // OCR 서버 상태 확인만 수행
                    _ = registryVm.CheckOcrServerStatusAsync();
                }
            }
            // DataContext가 RegistryTabViewModel인 경우 (직접 사용)
            else if (DataContext is RegistryTabViewModel registryVm)
            {
                // 데이터 로드는 이미 호출됨
                _ = registryVm.CheckOcrServerStatusAsync();
            }
        }
    }
}

