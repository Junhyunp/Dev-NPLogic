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
            System.Diagnostics.Debug.WriteLine($"[RegistryTab] Loaded, DataContext type: {DataContext?.GetType().Name ?? "null"}");
            
            RegistryTabViewModel? registryViewModel = null;
            
            // DataContext가 PropertyDetailViewModel인 경우 (기존 방식)
            if (DataContext is PropertyDetailViewModel viewModel)
            {
                registryViewModel = viewModel.RegistryViewModel;
                System.Diagnostics.Debug.WriteLine($"[RegistryTab] PropertyDetailViewModel found");
            }
            // DataContext가 동적 래퍼인 경우 (AdminHomeView에서 사용)
            else if (DataContext is System.Dynamic.ExpandoObject expando)
            {
                var dict = (IDictionary<string, object?>)expando;
                if (dict.TryGetValue("RegistryViewModel", out var vm) && vm is RegistryTabViewModel rvm)
                {
                    registryViewModel = rvm;
                    System.Diagnostics.Debug.WriteLine("[RegistryTab] ExpandoObject DataContext");
                }
            }
            // DataContext가 RegistryTabViewModel인 경우 (직접 사용)
            else if (DataContext is RegistryTabViewModel rvm)
            {
                registryViewModel = rvm;
                System.Diagnostics.Debug.WriteLine("[RegistryTab] RegistryTabViewModel DataContext");
            }
            // 익명 타입인 경우 (리플렉션으로 RegistryViewModel 속성 찾기)
            else if (DataContext != null)
            {
                var dataContextType = DataContext.GetType();
                var prop = dataContextType.GetProperty("RegistryViewModel");
                if (prop != null)
                {
                    registryViewModel = prop.GetValue(DataContext) as RegistryTabViewModel;
                    System.Diagnostics.Debug.WriteLine("[RegistryTab] Anonymous type - found RegistryViewModel via reflection");
                }
            }
            
            // RegistryViewModel을 찾았으면 초기화
            if (registryViewModel != null)
            {
                System.Diagnostics.Debug.WriteLine("[RegistryTab] Initializing RegistryViewModel");
                
                // 등기부 데이터 로드
                await registryViewModel.LoadDataAsync();
                
                // OCR 서버 상태 확인 (백그라운드에서)
                _ = registryViewModel.CheckOcrServerStatusAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[RegistryTab] RegistryViewModel not found!");
            }
        }
    }
}

