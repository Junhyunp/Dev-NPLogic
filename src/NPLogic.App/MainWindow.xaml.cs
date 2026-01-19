using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Data.Services;
using NPLogic.Data.Exceptions;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Button? _selectedMenuButton = null;
        private User? _currentUser;
        
        // 선택된 메뉴 배경색 및 accent 색상 (적당한 밝기)
        private static readonly SolidColorBrush SelectedMenuBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xE0, 0xF4)); // 적당한 스카이 블루
        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        private static readonly SolidColorBrush AccentBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2)); // 밝은 블루 accent

        // 세션 체크 관련
        private DateTime _lastSessionCheck = DateTime.MinValue;
        private bool _isCheckingSession = false;
        private static readonly TimeSpan SessionCheckInterval = TimeSpan.FromMinutes(1); // 1분마다 체크

        public static MainWindow? Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            
            Instance = this;
            
            // Initialize Toast Service
            NPLogic.UI.Services.ToastService.Instance.Initialize(ToastContainer);

            // 앱 포커스 복귀 시 세션 확인 이벤트 등록
            this.Activated += MainWindow_Activated;

            // 현재 사용자 정보를 로드하고 초기 화면으로 이동
            _ = InitializeNavigationAsync();
        }

        /// <summary>
        /// 앱 포커스 복귀 시 세션 유효성 선제적 확인
        /// </summary>
        private async void MainWindow_Activated(object? sender, EventArgs e)
        {
            // 중복 체크 방지 및 체크 간격 제한
            if (_isCheckingSession)
                return;

            var now = DateTime.Now;
            if (now - _lastSessionCheck < SessionCheckInterval)
                return;

            _isCheckingSession = true;
            _lastSessionCheck = now;

            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null) return;

                var supabaseService = serviceProvider.GetRequiredService<SupabaseService>();
                
                // 세션 유효성 확인 및 필요 시 갱신 (예외 없이)
                var isValid = await supabaseService.CheckAndRefreshSessionAsync();
                
                if (!isValid)
                {
                    // 세션이 완전히 만료됨 - SessionExpiredException 발생시켜 전역 핸들러에서 처리
                    throw new SessionExpiredException("세션이 만료되었습니다. 다시 로그인해주세요.");
                }

                System.Diagnostics.Debug.WriteLine($"Session check on focus: valid={isValid}");
            }
            catch (SessionExpiredException)
            {
                // 세션 만료 예외는 전역 핸들러로 전파
                throw;
            }
            catch (Exception ex)
            {
                // 네트워크 오류 등은 무시 (다음 API 호출 시 처리됨)
                System.Diagnostics.Debug.WriteLine($"Session check failed: {ex.Message}");
            }
            finally
            {
                _isCheckingSession = false;
            }
        }

        /// <summary>
        /// 사용자 정보를 로드하고 역할에 따른 초기 화면으로 이동
        /// </summary>
        private async Task InitializeNavigationAsync()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null) return;

            try
            {
                // 현재 사용자 정보 가져오기
                var authService = serviceProvider.GetRequiredService<AuthService>();
                var userRepository = serviceProvider.GetRequiredService<UserRepository>();
                
                var authUser = authService.GetSession()?.User;
                if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
                {
                    _currentUser = await userRepository.GetByAuthUserIdAsync(System.Guid.Parse(authUser.Id));
                }

                // 권한에 따른 메뉴 표시 제어
                if (_currentUser != null)
                {
                    // 관리자만 사용자 관리, 설정, 이력 메뉴 표시
                    var adminVisibility = _currentUser.IsAdmin 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                    
                    UserManagementButton.Visibility = adminVisibility;
                    SettingsButton.Visibility = adminVisibility;
                    AuditLogsButton.Visibility = adminVisibility;
                }

                // 대시보드(역할 기반)로 이동
                NavigateToDashboard();

                // 로그인 후 QA 알림 확인 및 팝업 표시
                // 피드백 섹션 18: 답변이 오면 배정된 계정 로그인 시 팝업 알람
                if (_currentUser != null)
                {
                    await CheckAndShowQaNotificationsAsync();
                }
            }
            catch
            {
                // 오류 시 기본 대시보드로
                NavigateToDashboard();
            }
        }

        /// <summary>
        /// QA 알림 확인 및 팝업 표시
        /// 피드백 섹션 18: 알림 기능 - 답변 도착 시 팝업 알람
        /// </summary>
        private async Task CheckAndShowQaNotificationsAsync()
        {
            if (_currentUser == null) return;

            try
            {
                await Views.QaNotificationPopup.ShowIfHasNotificationsAsync(this, _currentUser.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QA 알림 확인 실패: {ex.Message}");
                // 알림 확인 실패는 조용히 무시 (메인 기능에 영향 없음)
            }
        }

        /// <summary>
        /// 통합 홈 화면으로 이동 (모든 역할 공통)
        /// DashboardView에서 권한별 데이터 필터링 수행
        /// </summary>
        public void NavigateToDashboardView()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var dashboardView = serviceProvider.GetRequiredService<Views.DashboardView>();
                MainContentControl.Content = dashboardView;
                UpdateSelectedMenu(DashboardButton);
            }
        }

        /// <summary>
        /// 관리자 홈 화면으로 이동 (통합: DashboardView 사용)
        /// </summary>
        public void NavigateToAdminHome()
        {
            NavigateToDashboardView();
        }

        /// <summary>
        /// PM 홈 화면으로 이동 (통합: DashboardView 사용)
        /// </summary>
        public void NavigateToPMHome()
        {
            NavigateToDashboardView();
        }

        /// <summary>
        /// 평가자 홈 화면으로 이동 (통합: DashboardView 사용)
        /// </summary>
        public void NavigateToEvaluatorHome()
        {
            NavigateToDashboardView();
        }

        /// <summary>
        /// 프로그램 관리 화면으로 이동
        /// </summary>
        public void NavigateToProgramManagement()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var programManagementView = serviceProvider.GetRequiredService<Views.ProgramManagementView>();
                MainContentControl.Content = programManagementView;
            }
        }
        
        /// <summary>
        /// 사이드바 메뉴 선택 상태 업데이트
        /// </summary>
        private void UpdateSelectedMenu(Button selectedButton)
        {
            // 이전 선택된 버튼의 스타일 초기화
            if (_selectedMenuButton != null)
            {
                _selectedMenuButton.Background = TransparentBrush;
                _selectedMenuButton.BorderBrush = TransparentBrush;
                _selectedMenuButton.BorderThickness = new Thickness(0);
            }
            
            // 새로 선택된 버튼의 스타일 설정 (배경색 + 왼쪽 accent 바)
            selectedButton.Background = SelectedMenuBrush;
            selectedButton.BorderBrush = AccentBrush;
            selectedButton.BorderThickness = new Thickness(3, 0, 0, 0);
            _selectedMenuButton = selectedButton;
        }

        /// <summary>
        /// 대시보드로 이동 (통합 홈 화면)
        /// 모든 역할이 DashboardView를 사용하고, 권한별 데이터 필터링 적용
        /// </summary>
        public void NavigateToDashboard()
        {
            NavigateToDashboardView();
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
                UpdateSelectedMenu(PropertyListButton);
            }
        }

        /// <summary>
        /// 물건 목록으로 이동 (필터 적용)
        /// </summary>
        public void NavigateToPropertyListWithFilter(string? debtorName, Action? backAction = null)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var propertyListView = serviceProvider.GetRequiredService<Views.PropertyListView>();
                var viewModel = propertyListView.DataContext as ViewModels.PropertyListViewModel;
                if (viewModel != null && !string.IsNullOrEmpty(debtorName))
                {
                    viewModel.SetDebtorFilter(debtorName, backAction, $"담보물건 - {debtorName}");
                }
                MainContentControl.Content = propertyListView;
                UpdateSelectedMenu(PropertyListButton);
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
                UpdateSelectedMenu(DataUploadButton);
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
                UpdateSelectedMenu(StatisticsButton);
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
                UpdateSelectedMenu(BorrowerOverviewButton);
            }
        }

        /// <summary>
        /// 담보 총괄로 이동 (물건 목록 전체 화면)
        /// </summary>
        public void NavigateToCollateralSummary()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var collateralSummaryView = serviceProvider.GetRequiredService<Views.CollateralSummaryView>();
                MainContentControl.Content = collateralSummaryView;
                UpdateSelectedMenu(CollateralSummaryButton);
            }
        }

        /// <summary>
        /// 대시보드로 이동 (뒤로가기용)
        /// </summary>
        public void NavigateToDashboardCollateralSummary()
        {
            // 대시보드로 이동
            NavigateToDashboard();
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
                UpdateSelectedMenu(LoanDetailButton);
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
                UpdateSelectedMenu(ToolBoxButton);
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
                UpdateSelectedMenu(CashFlowSummaryButton);
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
                UpdateSelectedMenu(XnpvComparisonButton);
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
                UpdateSelectedMenu(RestructuringOverviewButton);
            }
        }

        /// <summary>
        /// 물건 상세로 이동
        /// </summary>
        public void NavigateToPropertyDetail(Property property)
        {
            NavigateToPropertyDetail(property, null);
        }

        /// <summary>
        /// 물건 상세로 이동 (뒤로가기 액션 지정 가능)
        /// </summary>
        public void NavigateToPropertyDetail(Property property, Action? backAction)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var propertyDetailView = serviceProvider.GetRequiredService<Views.PropertyDetailView>();
                var viewModel = propertyDetailView.DataContext as ViewModels.PropertyDetailViewModel;
                if (viewModel != null)
                {
                    viewModel.LoadProperty(property);
                    // 뒤로가기 액션 설정 (지정된 액션 또는 기본값으로 물건 목록으로)
                    viewModel.SetPropertyId(property.Id, backAction ?? NavigateToPropertyList);
                }
                MainContentControl.Content = propertyDetailView;
            }
        }

        /// <summary>
        /// 홈(전체) 화면으로 이동 (차주번호 클릭 시 - 피드백 반영: 차주번호/명 클릭 시 전체로 바로 이어져야 함)
        /// </summary>
        public void NavigateToHomeView(Property property)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                // 통합 홈 화면으로 이동 후 상세 모드로 전환
                var dashboardView = serviceProvider.GetRequiredService<Views.DashboardView>();
                MainContentControl.Content = dashboardView;
                UpdateSelectedMenu(DashboardButton);
                
                // 상세 모드로 전환
                dashboardView.SelectPropertyAndSwitchToDetail(property);
            }
        }

        /// <summary>
        /// 로고 클릭 - 대시보드(홈)로 이동
        /// </summary>
        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            NavigateToDashboard();
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
                UpdateSelectedMenu(UserManagementButton);
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
                UpdateSelectedMenu(SettingsButton);
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
                UpdateSelectedMenu(AuditLogsButton);
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
                UpdateSelectedMenu(SeniorRightsButton);
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
        /// 공매 일정으로 이동
        /// </summary>
        public void NavigateToPublicSaleSchedule()
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var publicSaleScheduleView = serviceProvider.GetRequiredService<Views.PublicSaleScheduleView>();
                MainContentControl.Content = publicSaleScheduleView;
                UpdateSelectedMenu(PublicSaleScheduleButton);
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


