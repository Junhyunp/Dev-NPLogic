using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// QASummaryTab.xaml에 대한 상호 작용 논리
    /// QA 집계 화면
    /// 피드백 섹션 11: QA 집계 메뉴 추가
    /// - 차주, 파트, 질문, 답변 형식으로 총괄 관리
    /// - 답변 엑셀 업로드 시 Q-A 매칭
    /// - 답변 도착 시 팝업 알람 기능
    /// </summary>
    public partial class QASummaryTab : UserControl
    {
        private QASummaryViewModel? _viewModel;

        public QASummaryTab()
        {
            InitializeComponent();
        }

        private async void QASummaryTab_Loaded(object sender, RoutedEventArgs e)
        {
            // ViewModel이 이미 설정되어 있으면 초기화
            if (DataContext is QASummaryViewModel vm)
            {
                _viewModel = vm;
                await _viewModel.InitializeAsync();
                return;
            }

            // ViewModel 생성 및 초기화
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null)
                {
                    MessageBox.Show("서비스 초기화 실패", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var supabaseService = serviceProvider.GetRequiredService<SupabaseService>();
                var qaRepository = new PropertyQaRepository(supabaseService);
                var notificationRepository = new QaNotificationRepository(supabaseService);
                var authService = serviceProvider.GetRequiredService<AuthService>();

                _viewModel = new QASummaryViewModel(qaRepository, notificationRepository, authService);
                DataContext = _viewModel;

                await _viewModel.InitializeAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"QA 집계 화면 초기화 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 오버레이 클릭 시 편집 모드 취소
        /// </summary>
        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CancelEditCommand.Execute(null);
            }
        }
    }
}
