using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 대시보드 ViewModel
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private string _selectedProjectId = "";

        [ObservableProperty]
        private ObservableCollection<string> _projects = new();

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _pendingCount;

        [ObservableProperty]
        private int _processingCount;

        [ObservableProperty]
        private int _completedCount;

        [ObservableProperty]
        private ObservableCollection<Property> _recentProperties = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public DashboardViewModel(
            PropertyRepository propertyRepository,
            UserRepository userRepository,
            AuthService authService)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 현재 사용자 정보 가져오기
                await LoadCurrentUserAsync();

                // 프로젝트 목록 로드
                await LoadProjectsAsync();

                // 통계 로드
                await LoadStatisticsAsync();

                // 최근 물건 목록 로드
                await LoadRecentPropertiesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"대시보드 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 현재 사용자 정보 로드
        /// </summary>
        private async Task LoadCurrentUserAsync()
        {
            var authUser = _authService.GetSession()?.User;
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
            {
                CurrentUser = await _userRepository.GetByAuthUserIdAsync(Guid.Parse(authUser.Id));
            }
        }

        /// <summary>
        /// 프로젝트 목록 로드
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            try
            {
                // 모든 물건에서 프로젝트 ID 추출 (중복 제거)
                var allProperties = await _propertyRepository.GetAllAsync();
                var projectIds = allProperties
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProjectId))
                    .Select(p => p.ProjectId!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                Projects.Clear();
                Projects.Add("전체"); // 전체 보기 옵션
                foreach (var projectId in projectIds)
                {
                    Projects.Add(projectId);
                }

                if (Projects.Count > 0)
                {
                    SelectedProjectId = Projects[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로젝트 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 통계 로드
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;
                
                // 역할에 따라 필터링
                Guid? assignedTo = null;
                if (CurrentUser?.IsEvaluator == true)
                {
                    assignedTo = CurrentUser.Id;
                }

                var stats = await _propertyRepository.GetStatisticsAsync(projectId, assignedTo);
                
                TotalCount = stats.TotalCount;
                PendingCount = stats.PendingCount;
                ProcessingCount = stats.ProcessingCount;
                CompletedCount = stats.CompletedCount;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"통계 로드 실패:\n{ex.GetType().Name}\n\n{ex.Message}\n\n스택:\n{ex.StackTrace}";
                
                // Inner Exception도 표시
                if (ex.InnerException != null)
                {
                    ErrorMessage += $"\n\n내부 예외:\n{ex.InnerException.Message}";
                }
            }
        }

        /// <summary>
        /// 최근 물건 목록 로드
        /// </summary>
        private async Task LoadRecentPropertiesAsync()
        {
            try
            {
                var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;
                
                // 역할에 따라 필터링
                Guid? assignedTo = null;
                if (CurrentUser?.IsEvaluator == true)
                {
                    assignedTo = CurrentUser.Id;
                }

                var (properties, _) = await _propertyRepository.GetPagedAsync(
                    page: 1,
                    pageSize: 10,
                    projectId: projectId,
                    assignedTo: assignedTo
                );

                RecentProperties.Clear();
                foreach (var property in properties)
                {
                    RecentProperties.Add(property);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"최근 물건 로드 실패:\n{ex.GetType().Name}\n\n{ex.Message}\n\n스택:\n{ex.StackTrace}";
                
                // Inner Exception도 표시
                if (ex.InnerException != null)
                {
                    ErrorMessage += $"\n\n내부 예외:\n{ex.InnerException.Message}";
                }
            }
        }

        /// <summary>
        /// 프로젝트 선택 변경 시
        /// </summary>
        partial void OnSelectedProjectIdChanged(string value)
        {
            _ = RefreshDataAsync();
        }

        /// <summary>
        /// 데이터 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await LoadStatisticsAsync();
                await LoadRecentPropertiesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"새로고침 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 물건 상세 보기
        /// </summary>
        [RelayCommand]
        private void ViewPropertyDetail(Property property)
        {
            if (property == null)
                return;

            MainWindow.Instance?.NavigateToPropertyDetail(property);
        }

        /// <summary>
        /// 물건 목록 화면으로 이동
        /// </summary>
        [RelayCommand]
        private void GoToPropertyList()
        {
            MainWindow.Instance?.NavigateToPropertyList();
        }
    }
}

