using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 평가자용 홈 화면 ViewModel - 배정된 물건만 표시
    /// </summary>
    public partial class EvaluatorHomeViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        // ========== 사용자 정보 ==========
        [ObservableProperty]
        private User? _currentUser;

        // ========== 프로그램 정보 (선택적) ==========
        [ObservableProperty]
        private string? _currentProgramName;

        // ========== 물건 목록 ==========
        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        // ========== 통계 ==========
        [ObservableProperty]
        private int _totalPropertyCount;

        [ObservableProperty]
        private int _pendingCount;

        [ObservableProperty]
        private int _processingCount;

        [ObservableProperty]
        private int _completedCount;

        [ObservableProperty]
        private int _overallProgressPercent;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public string DisplayName => CurrentUser?.DisplayName ?? "평가자";

        public EvaluatorHomeViewModel(
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

                // 현재 사용자 정보 로드
                await LoadCurrentUserAsync();

                // 배정된 물건 목록 로드
                await LoadAssignedPropertiesAsync();
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

        private async Task LoadCurrentUserAsync()
        {
            var authUser = _authService.GetSession()?.User;
            if (authUser != null && !string.IsNullOrEmpty(authUser.Id))
            {
                CurrentUser = await _userRepository.GetByAuthUserIdAsync(Guid.Parse(authUser.Id));
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary>
        /// 배정된 물건 목록 로드
        /// </summary>
        private async Task LoadAssignedPropertiesAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                var properties = await _propertyRepository.GetByAssignedUserAsync(CurrentUser.Id);

                Properties.Clear();
                foreach (var property in properties)
                {
                    Properties.Add(property);
                }

                // 프로그램명 설정 (첫 번째 물건 기준)
                if (Properties.Count > 0 && !string.IsNullOrEmpty(Properties[0].ProjectId))
                {
                    CurrentProgramName = Properties[0].ProjectId;
                }

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 통계 업데이트
        /// </summary>
        private void UpdateStatistics()
        {
            TotalPropertyCount = Properties.Count;
            PendingCount = Properties.Count(p => p.Status == "pending");
            ProcessingCount = Properties.Count(p => p.Status == "processing");
            CompletedCount = Properties.Count(p => p.Status == "completed");

            if (TotalPropertyCount > 0)
            {
                OverallProgressPercent = (int)Math.Round((double)CompletedCount / TotalPropertyCount * 100);
            }
            else
            {
                OverallProgressPercent = 0;
            }
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadAssignedPropertiesAsync();
        }

        /// <summary>
        /// 물건 상세 보기
        /// </summary>
        [RelayCommand]
        private void ViewPropertyDetail(Property property)
        {
            if (property == null) return;
            MainWindow.Instance?.NavigateToPropertyDetail(property);
        }

        /// <summary>
        /// 진행 체크박스 필드 업데이트
        /// </summary>
        public async Task UpdateProgressFieldAsync(Guid propertyId, string fieldName, object value)
        {
            try
            {
                await _propertyRepository.UpdateProgressFieldAsync(propertyId, fieldName, value);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
        }
    }
}

