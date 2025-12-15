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
            // DataContext가 PropertyDetailViewModel인 경우 등기부 데이터 로드
            if (DataContext is PropertyDetailViewModel viewModel && viewModel.RegistryViewModel != null)
            {
                // 등기부 데이터 로드
                await viewModel.RegistryViewModel.LoadDataAsync();
                
                // OCR 서버 상태 확인 (백그라운드에서)
                _ = viewModel.RegistryViewModel.CheckOcrServerStatusAsync();
            }
        }
    }
}

