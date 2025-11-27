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
        /// 차주 개요로 이동
        /// </summary>
        public void NavigateToBorrowerOverview()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var borrowerOverviewView = serviceProvider.GetRequiredService<Views.BorrowerOverviewView>();
                MainContentControl.Content = borrowerOverviewView;
            }
        }

        /// <summary>
        /// 담보 총괄로 이동
        /// </summary>
        public void NavigateToCollateralSummary()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var collateralSummaryView = serviceProvider.GetRequiredService<Views.CollateralSummaryView>();
                MainContentControl.Content = collateralSummaryView;
            }
        }

        /// <summary>
        /// Loan 상세로 이동
        /// </summary>
        public void NavigateToLoanDetail()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var loanDetailView = serviceProvider.GetRequiredService<Views.LoanDetailView>();
                MainContentControl.Content = loanDetailView;
            }
        }

        /// <summary>
        /// Tool Box로 이동
        /// </summary>
        public void NavigateToToolBox()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var toolBoxView = serviceProvider.GetRequiredService<Views.ToolBoxView>();
                MainContentControl.Content = toolBoxView;
            }
        }

        /// <summary>
        /// 현금흐름 집계로 이동
        /// </summary>
        public void NavigateToCashFlowSummary()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var cashFlowSummaryView = serviceProvider.GetRequiredService<Views.CashFlowSummaryView>();
                MainContentControl.Content = cashFlowSummaryView;
            }
        }

        /// <summary>
        /// XNPV 비교로 이동
        /// </summary>
        public void NavigateToXnpvComparison()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var xnpvComparisonView = serviceProvider.GetRequiredService<Views.XnpvComparisonView>();
                MainContentControl.Content = xnpvComparisonView;
            }
        }

        /// <summary>
        /// 회생 개요로 이동
        /// </summary>
        public void NavigateToRestructuringOverview()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var restructuringOverviewView = serviceProvider.GetRequiredService<Views.RestructuringOverviewView>();
                MainContentControl.Content = restructuringOverviewView;
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
        /// 차주 개요 버튼 클릭
        /// </summary>
        private void BorrowerOverviewButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToBorrowerOverview();
        }

        /// <summary>
        /// 담보 총괄 버튼 클릭
        /// </summary>
        private void CollateralSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToCollateralSummary();
        }

        /// <summary>
        /// Loan 상세 버튼 클릭
        /// </summary>
        private void LoanDetailButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToLoanDetail();
        }

        /// <summary>
        /// Tool Box 버튼 클릭
        /// </summary>
        private void ToolBoxButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToToolBox();
        }

        /// <summary>
        /// 현금흐름 집계 버튼 클릭
        /// </summary>
        private void CashFlowSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToCashFlowSummary();
        }

        /// <summary>
        /// XNPV 비교 버튼 클릭
        /// </summary>
        private void XnpvComparisonButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToXnpvComparison();
        }

        /// <summary>
        /// 회생 개요 버튼 클릭
        /// </summary>
        private void RestructuringOverviewButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRestructuringOverview();
        }

        /// <summary>
        /// 사용자 관리로 이동
        /// </summary>
        public void NavigateToUserManagement()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var userManagementView = serviceProvider.GetRequiredService<Views.UserManagementView>();
                MainContentControl.Content = userManagementView;
            }
        }

        /// <summary>
        /// 사용자 관리 버튼 클릭
        /// </summary>
        private void UserManagementButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUserManagement();
        }

        /// <summary>
        /// 설정 관리로 이동
        /// </summary>
        public void NavigateToSettings()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var settingsView = serviceProvider.GetRequiredService<Views.SettingsView>();
                MainContentControl.Content = settingsView;
            }
        }

        /// <summary>
        /// 설정 버튼 클릭 (사이드바)
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSettings();
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
            NavigateToSettings();
        }

        /// <summary>
        /// 작업 이력으로 이동
        /// </summary>
        public void NavigateToAuditLogs()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var auditLogsView = serviceProvider.GetRequiredService<Views.AuditLogsView>();
                MainContentControl.Content = auditLogsView;
            }
        }

        /// <summary>
        /// 작업 이력 버튼 클릭
        /// </summary>
        private void AuditLogsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToAuditLogs();
        }

        /// <summary>
        /// 선순위 관리로 이동
        /// </summary>
        public void NavigateToSeniorRights()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var seniorRightsView = serviceProvider.GetRequiredService<Views.SeniorRightsView>();
                MainContentControl.Content = seniorRightsView;
            }
        }

        /// <summary>
        /// 선순위 관리 버튼 클릭
        /// </summary>
        private void SeniorRightsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSeniorRights();
        }

        /// <summary>
        /// 경매 일정으로 이동
        /// </summary>
        public void NavigateToAuctionSchedule()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var auctionScheduleView = serviceProvider.GetRequiredService<Views.AuctionScheduleView>();
                MainContentControl.Content = auctionScheduleView;
            }
        }

        /// <summary>
        /// 경매 일정 버튼 클릭
        /// </summary>
        private void AuctionScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToAuctionSchedule();
        }

        /// <summary>
        /// 공매 일정으로 이동
        /// </summary>
        public void NavigateToPublicSaleSchedule()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var publicSaleScheduleView = serviceProvider.GetRequiredService<Views.PublicSaleScheduleView>();
                MainContentControl.Content = publicSaleScheduleView;
            }
        }

        /// <summary>
        /// 공매 일정 버튼 클릭
        /// </summary>
        private void PublicSaleScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPublicSaleSchedule();
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


