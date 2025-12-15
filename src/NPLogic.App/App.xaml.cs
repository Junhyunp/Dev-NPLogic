using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Services;
using NPLogic.ViewModels;
using NPLogic.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace NPLogic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private RegistryOcrService? _registryOcrService;

        // Supabase 설정 (환경 변수 또는 설정 파일에서 로드하는 것이 좋습니다)
        private const string SupabaseUrl = "https://vuepmhwrizaabswlgiiy.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ1ZXBtaHdyaXphYWJzd2xnaWl5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM2NDA2MzUsImV4cCI6MjA3OTIxNjYzNX0.VJ6CG3alGGVK7HYfZabHd47q2RDR6EgGMPD1GS8YNu4";

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // LiveCharts 한글 폰트 설정 (SkiaSharp 렌더링에 필요)
            LiveCharts.Configure(config => 
                config.HasGlobalSKTypeface(SKFontManager.Default.MatchFamily("Malgun Gothic")));

            // 전역 예외 핸들러 추가
            this.DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    $"예외 발생:\n\n{ex.Exception.GetType().Name}\n\n{ex.Exception.Message}\n\n스택 트레이스:\n{ex.Exception.StackTrace}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                ex.Handled = true;
            };

            try
            {
                // DI 컨테이너 설정
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // OCR 서비스 참조 저장 (종료 시 서버 종료용)
                _registryOcrService = _serviceProvider.GetService<RegistryOcrService>();

                // Supabase 초기화
                var supabaseService = _serviceProvider.GetRequiredService<SupabaseService>();
                await supabaseService.InitializeAsync();

                // 로그인 상태 확인
                var authService = _serviceProvider.GetRequiredService<AuthService>();
                
                // 먼저 자동 로그인 시도
                var autoSignInSuccess = await authService.TryAutoSignInAsync();
                
                // 자동 로그인 성공 여부에 따라 다른 창 표시
                if (autoSignInSuccess || authService.IsAuthenticated())
                {
                    // 자동 로그인 또는 이미 로그인된 경우 메인 윈도우 표시
                    var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                    mainWindow.Show();
                }
                else
                {
                    // 로그인 안 된 경우 로그인 창 표시
                    var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
                    loginWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"시작 오류:\n\n{ex.GetType().Name}\n\n{ex.Message}\n\n스택 트레이스:\n{ex.StackTrace}",
                    "시작 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }

            // 기본 MainWindow는 표시하지 않음
            // (StartupUri를 제거하고 수동으로 창을 표시)
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Supabase Services (Singleton)
            services.AddSingleton(new SupabaseService(SupabaseUrl, SupabaseKey));
            services.AddSingleton<AuthService>();
            services.AddSingleton<ExcelService>();
            services.AddSingleton<StorageService>();
            services.AddSingleton<RegistryOcrService>();

            // Repositories (Singleton)
            services.AddSingleton<Data.Repositories.UserRepository>();
            services.AddSingleton<Data.Repositories.PropertyRepository>();
            services.AddSingleton<Data.Repositories.ProgramRepository>();
            services.AddSingleton<Data.Repositories.ProgramUserRepository>();
            services.AddSingleton<Data.Repositories.StatisticsRepository>();
            services.AddSingleton<Data.Repositories.RegistryRepository>();
            services.AddSingleton<Data.Repositories.RightAnalysisRepository>();
            services.AddSingleton<Data.Repositories.BorrowerRepository>();
            services.AddSingleton<Data.Repositories.LoanRepository>();
            services.AddSingleton<Data.Repositories.ReferenceDataRepository>();
            services.AddSingleton<Data.Repositories.EvaluationRepository>();
            services.AddSingleton<Data.Repositories.SettingsRepository>();
            services.AddSingleton<Data.Repositories.AuditLogRepository>();
            services.AddSingleton<Data.Repositories.AuctionScheduleRepository>();
            services.AddSingleton<Data.Repositories.PublicSaleScheduleRepository>();

            // ViewModels (Transient)
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();
            services.AddTransient<ViewModels.AdminHomeViewModel>();
            services.AddTransient<ViewModels.PMHomeViewModel>();
            services.AddTransient<ViewModels.EvaluatorHomeViewModel>();
            services.AddTransient<ViewModels.PropertyListViewModel>();
            services.AddTransient<ViewModels.PropertyFormViewModel>();
            services.AddTransient<ViewModels.PropertyDetailViewModel>(sp =>
            {
                return new ViewModels.PropertyDetailViewModel(
                    sp.GetRequiredService<Data.Repositories.PropertyRepository>(),
                    sp.GetRequiredService<StorageService>(),
                    sp.GetRequiredService<Data.Repositories.RegistryRepository>(),
                    sp.GetRequiredService<Data.Repositories.RightAnalysisRepository>(),
                    sp.GetRequiredService<Data.Repositories.EvaluationRepository>(),
                    sp.GetRequiredService<RegistryOcrService>()
                );
            });
            services.AddTransient<ViewModels.DataUploadViewModel>();
            services.AddTransient<ViewModels.StatisticsViewModel>();
            services.AddTransient<ViewModels.BorrowerOverviewViewModel>();
            services.AddTransient<ViewModels.CollateralSummaryViewModel>();
            services.AddTransient<ViewModels.LoanDetailViewModel>();
            services.AddTransient<ViewModels.ToolBoxViewModel>();
            services.AddTransient<ViewModels.CashFlowSummaryViewModel>();
            services.AddTransient<ViewModels.XnpvComparisonViewModel>();
            services.AddTransient<ViewModels.RestructuringOverviewViewModel>();
            services.AddTransient<ViewModels.UserManagementViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();
            services.AddTransient<ViewModels.AuditLogsViewModel>();
            services.AddTransient<ViewModels.SeniorRightsViewModel>();
            services.AddTransient<ViewModels.AuctionScheduleViewModel>();
            services.AddTransient<ViewModels.PublicSaleScheduleViewModel>();
            services.AddTransient<ViewModels.ProgramManagementViewModel>();

            // Views (Transient)
            services.AddTransient<Views.DashboardView>(sp =>
            {
                var view = new Views.DashboardView();
                view.DataContext = sp.GetRequiredService<ViewModels.DashboardViewModel>();
                return view;
            });

            services.AddTransient<Views.AdminHomeView>(sp =>
            {
                var view = new Views.AdminHomeView();
                view.DataContext = sp.GetRequiredService<ViewModels.AdminHomeViewModel>();
                return view;
            });

            services.AddTransient<Views.PMHomeView>(sp =>
            {
                var view = new Views.PMHomeView();
                view.DataContext = sp.GetRequiredService<ViewModels.PMHomeViewModel>();
                return view;
            });

            services.AddTransient<Views.EvaluatorHomeView>(sp =>
            {
                var view = new Views.EvaluatorHomeView();
                view.DataContext = sp.GetRequiredService<ViewModels.EvaluatorHomeViewModel>();
                return view;
            });

            services.AddTransient<Views.PropertyListView>(sp =>
            {
                var view = new Views.PropertyListView();
                view.DataContext = sp.GetRequiredService<ViewModels.PropertyListViewModel>();
                return view;
            });

            services.AddTransient<Views.DataUploadView>(sp =>
            {
                var view = new Views.DataUploadView();
                view.DataContext = sp.GetRequiredService<ViewModels.DataUploadViewModel>();
                return view;
            });

            services.AddTransient<Views.PropertyDetailView>(sp =>
            {
                var view = new Views.PropertyDetailView();
                view.DataContext = sp.GetRequiredService<ViewModels.PropertyDetailViewModel>();
                return view;
            });

            services.AddTransient<Views.StatisticsView>(sp =>
            {
                var view = new Views.StatisticsView();
                view.DataContext = sp.GetRequiredService<ViewModels.StatisticsViewModel>();
                return view;
            });

            services.AddTransient<Views.BorrowerOverviewView>(sp =>
            {
                var view = new Views.BorrowerOverviewView();
                view.DataContext = sp.GetRequiredService<ViewModels.BorrowerOverviewViewModel>();
                return view;
            });

            services.AddTransient<Views.CollateralSummaryView>(sp =>
            {
                var view = new Views.CollateralSummaryView();
                view.DataContext = sp.GetRequiredService<ViewModels.CollateralSummaryViewModel>();
                return view;
            });

            services.AddTransient<Views.LoanDetailView>(sp =>
            {
                var view = new Views.LoanDetailView();
                view.DataContext = sp.GetRequiredService<ViewModels.LoanDetailViewModel>();
                return view;
            });

            services.AddTransient<Views.ToolBoxView>(sp =>
            {
                var view = new Views.ToolBoxView();
                view.DataContext = sp.GetRequiredService<ViewModels.ToolBoxViewModel>();
                return view;
            });

            services.AddTransient<Views.CashFlowSummaryView>(sp =>
            {
                var view = new Views.CashFlowSummaryView();
                view.DataContext = sp.GetRequiredService<ViewModels.CashFlowSummaryViewModel>();
                return view;
            });

            services.AddTransient<Views.XnpvComparisonView>(sp =>
            {
                var view = new Views.XnpvComparisonView();
                view.DataContext = sp.GetRequiredService<ViewModels.XnpvComparisonViewModel>();
                return view;
            });

            services.AddTransient<Views.RestructuringOverviewView>(sp =>
            {
                var view = new Views.RestructuringOverviewView();
                view.DataContext = sp.GetRequiredService<ViewModels.RestructuringOverviewViewModel>();
                return view;
            });

            services.AddTransient<Views.UserManagementView>(sp =>
            {
                var view = new Views.UserManagementView();
                view.DataContext = sp.GetRequiredService<ViewModels.UserManagementViewModel>();
                return view;
            });

            services.AddTransient<Views.SettingsView>(sp =>
            {
                var view = new Views.SettingsView();
                view.DataContext = sp.GetRequiredService<ViewModels.SettingsViewModel>();
                return view;
            });

            services.AddTransient<Views.AuditLogsView>(sp =>
            {
                var view = new Views.AuditLogsView();
                view.DataContext = sp.GetRequiredService<ViewModels.AuditLogsViewModel>();
                return view;
            });

            services.AddTransient<Views.SeniorRightsView>(sp =>
            {
                var view = new Views.SeniorRightsView();
                view.DataContext = sp.GetRequiredService<ViewModels.SeniorRightsViewModel>();
                return view;
            });

            services.AddTransient<Views.AuctionScheduleView>(sp =>
            {
                var view = new Views.AuctionScheduleView();
                view.DataContext = sp.GetRequiredService<ViewModels.AuctionScheduleViewModel>();
                return view;
            });

            services.AddTransient<Views.PublicSaleScheduleView>(sp =>
            {
                var view = new Views.PublicSaleScheduleView();
                view.DataContext = sp.GetRequiredService<ViewModels.PublicSaleScheduleViewModel>();
                return view;
            });

            services.AddTransient<Views.ProgramManagementView>(sp =>
            {
                var view = new Views.ProgramManagementView();
                view.DataContext = sp.GetRequiredService<ViewModels.ProgramManagementViewModel>();
                return view;
            });

            // Windows (Transient)
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>(sp =>
            {
                var window = new LoginWindow();
                var viewModel = sp.GetRequiredService<LoginViewModel>();
                window.DataContext = viewModel;
                return window;
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // OCR 서버 종료
            try
            {
                _registryOcrService?.StopServer();
                _registryOcrService?.Dispose();
            }
            catch
            {
                // 종료 시 예외 무시
            }

            _serviceProvider?.Dispose();
            base.OnExit(e);
        }


        /// <summary>
        /// 서비스 프로바이더 가져오기 (전역 접근용)
        /// </summary>
        public static ServiceProvider? ServiceProvider => 
            (Current as App)?._serviceProvider;
    }
}


