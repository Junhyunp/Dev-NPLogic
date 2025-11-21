using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 물건 목록 ViewModel
    /// </summary>
    public partial class PropertyListViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        [ObservableProperty]
        private Property? _selectedProperty;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string? _selectedProjectId;

        [ObservableProperty]
        private string? _selectedPropertyType;

        [ObservableProperty]
        private string? _selectedStatus;

        [ObservableProperty]
        private ObservableCollection<string> _projects = new();

        [ObservableProperty]
        private ObservableCollection<string> _propertyTypes = new();

        [ObservableProperty]
        private ObservableCollection<string> _statuses = new();

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 50;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _totalPages;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public PropertyListViewModel(
            PropertyRepository propertyRepository,
            UserRepository userRepository,
            AuthService authService)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // 필터 옵션 초기화
            InitializeFilterOptions();
        }

        /// <summary>
        /// 필터 옵션 초기화
        /// </summary>
        private void InitializeFilterOptions()
        {
            // 물건 유형
            PropertyTypes.Add("전체");
            PropertyTypes.Add("아파트");
            PropertyTypes.Add("상가");
            PropertyTypes.Add("토지");
            PropertyTypes.Add("빌라");
            PropertyTypes.Add("오피스텔");
            PropertyTypes.Add("단독주택");
            PropertyTypes.Add("다가구주택");
            PropertyTypes.Add("공장");

            // 상태
            Statuses.Add("전체");
            Statuses.Add("pending");
            Statuses.Add("processing");
            Statuses.Add("completed");

            // 기본값 설정
            SelectedPropertyType = PropertyTypes[0];
            SelectedStatus = Statuses[0];
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

                // 물건 목록 로드
                await LoadPropertiesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
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
                var allProperties = await _propertyRepository.GetAllAsync();
                var projectIds = allProperties
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProjectId))
                    .Select(p => p.ProjectId!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                Projects.Clear();
                Projects.Add("전체");
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
        /// 물건 목록 로드
        /// </summary>
        private async Task LoadPropertiesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;
                var propertyType = SelectedPropertyType == "전체" ? null : SelectedPropertyType;
                var status = SelectedStatus == "전체" ? null : SelectedStatus;
                
                // 역할에 따라 필터링
                Guid? assignedTo = null;
                if (CurrentUser?.IsEvaluator == true)
                {
                    assignedTo = CurrentUser.Id;
                }

                var searchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

                var (properties, totalCount) = await _propertyRepository.GetPagedAsync(
                    page: CurrentPage,
                    pageSize: PageSize,
                    projectId: projectId,
                    propertyType: propertyType,
                    status: status,
                    assignedTo: assignedTo,
                    searchText: searchText
                );

                Properties.Clear();
                foreach (var property in properties)
                {
                    Properties.Add(property);
                }

                TotalCount = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패:\n{ex.GetType().Name}\n\n{ex.Message}\n\n스택:\n{ex.StackTrace}";
                
                // Inner Exception도 표시
                if (ex.InnerException != null)
                {
                    ErrorMessage += $"\n\n내부 예외:\n{ex.InnerException.Message}";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 검색 명령
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadPropertiesAsync();
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedProjectIdChanged(string? value)
        {
            _ = ApplyFiltersAsync();
        }

        partial void OnSelectedPropertyTypeChanged(string? value)
        {
            _ = ApplyFiltersAsync();
        }

        partial void OnSelectedStatusChanged(string? value)
        {
            _ = ApplyFiltersAsync();
        }

        /// <summary>
        /// 필터 적용
        /// </summary>
        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            CurrentPage = 1;
            await LoadPropertiesAsync();
        }

        /// <summary>
        /// 필터 초기화
        /// </summary>
        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            SearchText = "";
            SelectedProjectId = Projects.FirstOrDefault();
            SelectedPropertyType = PropertyTypes.FirstOrDefault();
            SelectedStatus = Statuses.FirstOrDefault();
            CurrentPage = 1;
            await LoadPropertiesAsync();
        }

        /// <summary>
        /// 이전 페이지
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadPropertiesAsync();
            }
        }

        private bool CanGoPreviousPage() => CurrentPage > 1;

        /// <summary>
        /// 다음 페이지
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNextPage))]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadPropertiesAsync();
            }
        }

        private bool CanGoNextPage() => CurrentPage < TotalPages;

        /// <summary>
        /// 특정 페이지로 이동
        /// </summary>
        [RelayCommand]
        private async Task GoToPageAsync(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                await LoadPropertiesAsync();
            }
        }

        /// <summary>
        /// 물건 추가
        /// </summary>
        [RelayCommand]
        private void AddProperty()
        {
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null) return;

                var viewModel = serviceProvider.GetService<PropertyFormViewModel>();
                if (viewModel == null) return;

                viewModel.InitializeForCreate();

                var modal = new Views.PropertyFormModal
                {
                    DataContext = viewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                viewModel.SetWindow(modal);

                if (modal.ShowDialog() == true)
                {
                    // 성공 시 목록 새로고침
                    _ = LoadPropertiesAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 추가 모달 표시 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 물건 수정
        /// </summary>
        [RelayCommand]
        private void EditProperty(Property property)
        {
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null) return;

                var viewModel = serviceProvider.GetService<PropertyFormViewModel>();
                if (viewModel == null) return;

                viewModel.InitializeForEdit(property);

                var modal = new Views.PropertyFormModal
                {
                    DataContext = viewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                viewModel.SetWindow(modal);

                if (modal.ShowDialog() == true)
                {
                    // 성공 시 목록 새로고침
                    _ = LoadPropertiesAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 수정 모달 표시 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 물건 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeletePropertyAsync(Property property)
        {
            // TODO: 확인 다이얼로그 표시
            try
            {
                await _propertyRepository.DeleteAsync(property.Id);
                await LoadPropertiesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 삭제 실패: {ex.Message}";
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
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadPropertiesAsync();
        }
    }
}

