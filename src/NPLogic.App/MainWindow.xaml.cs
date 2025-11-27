using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Services;
using NPLogic.Core.Models;

namespace NPLogic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isSidebarCollapsed = false;

        public static MainWindow? Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            
            Instance = this;
            
            // Initialize Toast Service
            NPLogic.UI.Services.ToastService.Instance.Initialize(ToastContainer);

            // 기본 화면으로 대시보드 표시
            NavigateToDashboard();
        }

        /// <summary>
        /// 대시보드로 이동
        /// </summary>
        private void NavigateToDashboard()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var dashboardView = serviceProvider.GetRequiredService<Views.DashboardView>();
                MainContentControl.Content = dashboardView;
            }
        }

        /// <summary>
        /// 물건 목록으로 이동
        /// </summary>
        public void NavigateToPropertyList()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var propertyListView = serviceProvider.GetRequiredService<Views.PropertyListView>();
                MainContentControl.Content = propertyListView;
            }
        }

        /// <summary>
        /// 데이터 업로드로 이동
        /// </summary>
        private void NavigateToDataUpload()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var dataUploadView = serviceProvider.GetRequiredService<Views.DataUploadView>();
                MainContentControl.Content = dataUploadView;
            }
        }

        /// <summary>
        /// 개발중 화면으로 이동
        /// </summary>
        private void NavigateToUnderDevelopment()
        {
            var underDevelopmentView = new Views.UnderDevelopmentView();
            MainContentControl.Content = underDevelopmentView;
        }

        /// <summary>
        /// 통계 분석으로 이동
        /// </summary>
        private void NavigateToStatistics()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var statisticsView = serviceProvider.GetRequiredService<Views.StatisticsView>();
                MainContentControl.Content = statisticsView;
            }
        }

        /// <summary>
        /// 물건 상세로 이동
        /// </summary>
        public void NavigateToPropertyDetail(Property property)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var propertyDetailView = serviceProvider.GetRequiredService<Views.PropertyDetailView>();
                var viewModel = propertyDetailView.DataContext as ViewModels.PropertyDetailViewModel;
                if (viewModel != null)
                {
                    viewModel.LoadProperty(property);
                    // 뒤로가기 액션 설정 (물건 목록으로)
                    viewModel.SetPropertyId(property.Id, NavigateToPropertyList);
                }
                MainContentControl.Content = propertyDetailView;
            }
        }

        /// <summary>
        /// 대시보드 버튼 클릭
        /// </summary>
        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDashboard();
        }

        /// <summary>
        /// 물건 목록 버튼 클릭
        /// </summary>
        private void PropertyListButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPropertyList();
        }

        /// <summary>
        /// 데이터 업로드 버튼 클릭
        /// </summary>
        private void DataUploadButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDataUpload();
        }

        /// <summary>
        /// 통계 분석 버튼 클릭
        /// </summary>
        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToStatistics();
        }

        /// <summary>
        /// 사용자 관리 버튼 클릭
        /// </summary>
        private void UserManagementButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUnderDevelopment();
        }

        /// <summary>
        /// 설정 버튼 클릭 (사이드바)
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUnderDevelopment();
        }

        /// <summary>
        /// 알림 버튼 클릭 (헤더)
        /// </summary>
        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUnderDevelopment();
        }

        /// <summary>
        /// 설정 버튼 클릭 (헤더)
        /// </summary>
        private void HeaderSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUnderDevelopment();
        }

        /// <summary>
        /// 사용자 정보 버튼 클릭 (헤더)
        /// </summary>
        private void UserInfoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUnderDevelopment();
        }

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;

            if (_isSidebarCollapsed)
            {
                CollapseSidebar();
            }
            else
            {
                ExpandSidebar();
            }
        }

        private void CollapseSidebar()
        {
            // Animate sidebar width
            var widthAnimation = new GridLengthAnimation
            {
                From = new GridLength(SidebarColumn.ActualWidth),
                To = new GridLength(60),
                Duration = TimeSpan.FromMilliseconds(300)
            };

            SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, widthAnimation);

            // Change icon
            ToggleIcon.Kind = PackIconKind.ChevronRight;
        }

        private void ExpandSidebar()
        {
            // Animate sidebar width
            var widthAnimation = new GridLengthAnimation
            {
                From = new GridLength(SidebarColumn.ActualWidth),
                To = new GridLength(260),
                Duration = TimeSpan.FromMilliseconds(300)
            };

            SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, widthAnimation);

            // Change icon
            ToggleIcon.Kind = PackIconKind.ChevronLeft;
        }

        /// <summary>
        /// 로그아웃 버튼 클릭
        /// </summary>
        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "로그아웃하시겠습니까?",
                "로그아웃",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var serviceProvider = App.ServiceProvider;
                    if (serviceProvider != null)
                    {
                        var authService = serviceProvider.GetRequiredService<AuthService>();
                        await authService.SignOutAsync();

                        // 로그인 창 표시
                        var loginWindow = serviceProvider.GetRequiredService<Views.LoginWindow>();
                        loginWindow.Show();

                        // 현재 창 닫기
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"로그아웃 중 오류가 발생했습니다:\n{ex.Message}",
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }

    // Helper class for GridLength animation
    public class GridLengthAnimation : AnimationTimeline
    {
        public GridLength? From { get; set; }
        public GridLength? To { get; set; }

        public override Type TargetPropertyType => typeof(GridLength);

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (animationClock.CurrentProgress == null)
                return defaultOriginValue;

            double progress = animationClock.CurrentProgress.Value;
            GridLength from = From ?? (GridLength)defaultOriginValue;
            GridLength to = To ?? (GridLength)defaultDestinationValue;

            if (from.GridUnitType != to.GridUnitType)
                return progress < 0.5 ? from : to;

            double fromValue = from.Value;
            double toValue = to.Value;
            double newValue = fromValue + (toValue - fromValue) * progress;

            return new GridLength(newValue, from.GridUnitType);
        }
    }
}


