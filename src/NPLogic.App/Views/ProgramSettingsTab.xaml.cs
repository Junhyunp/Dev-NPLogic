using System.Windows.Controls;

namespace NPLogic.Views
{
    /// <summary>
    /// 프로그램 기초데이터 설정 탭
    /// - 법원별 정보 (C-001)
    /// - 법률적용률 (C-002)
    /// - 주택 임대차 기준 (C-003)
    /// - 상가 임대차 기준 (C-004)
    /// - 경매비용 산정 데이터 (C-005)
    /// </summary>
    public partial class ProgramSettingsTab : UserControl
    {
        public ProgramSettingsTab()
        {
            InitializeComponent();
            this.Loaded += ProgramSettingsTab_Loaded;
        }

        private async void ProgramSettingsTab_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.ProgramSettingsViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}
