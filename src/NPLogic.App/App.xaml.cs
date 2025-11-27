using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Services;
using NPLogic.ViewModels;
using NPLogic.Views;

namespace NPLogic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        // Supabase 설정 (환경 변수 또는 설정 파일에서 로드하는 것이 좋습니다)
        private const string SupabaseUrl = "https://vuepmhwrizaabswlgiiy.supabase.co";
        private const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ1ZXBtaHdyaXphYWJzd2xnaWl5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM2NDA2MzUsImV4cCI6MjA3OTIxNjYzNX0.VJ6CG3alGGVK7HYfZabHd47q2RDR6EgGMPD1GS8YNu4";

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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

            // Repositories (Singleton)
            services.AddSingleton<Data.Repositories.UserRepository>();
            services.AddSingleton<Data.Repositories.PropertyRepository>();
            services.AddSingleton<Data.Repositories.StatisticsRepository>();

            // ViewModels (Transient)
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();
            services.AddTransient<ViewModels.PropertyListViewModel>();
            services.AddTransient<ViewModels.PropertyFormViewModel>();
            services.AddTransient<ViewModels.PropertyDetailViewModel>();
            services.AddTransient<ViewModels.DataUploadViewModel>();
            services.AddTransient<ViewModels.StatisticsViewModel>();

            // Views (Transient)
            services.AddTransient<Views.DashboardView>(sp =>
            {
                var view = new Views.DashboardView();
                view.DataContext = sp.GetRequiredService<ViewModels.DashboardViewModel>();
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


