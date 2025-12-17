using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// PM용 홈 화면 ViewModel - 담당 프로그램의 물건 목록
    /// </summary>
    public partial class PMHomeViewModel : ObservableObject
    {
        private readonly ProgramRepository _programRepository;
        private readonly ProgramUserRepository _programUserRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        // ========== 사용자 정보 ==========
        [ObservableProperty]
        private User? _currentUser;

        // ========== 담당 프로그램 ==========
        [ObservableProperty]
        private Program? _currentProgram;

        [ObservableProperty]
        private ObservableCollection<Program> _myPrograms = new();

        [ObservableProperty]
        private Program? _selectedProgram;

        // ========== 물건 목록 ==========
        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        [ObservableProperty]
        private ObservableCollection<User> _evaluators = new();

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

        [ObservableProperty]
        private bool _hasMultiplePrograms;

        public string DisplayName => CurrentUser?.DisplayName ?? "PM";
        public string ProgramDisplayName => CurrentProgram?.ProgramName ?? "프로그램";

        public PMHomeViewModel(
            ProgramRepository programRepository,
            ProgramUserRepository programUserRepository,
            PropertyRepository propertyRepository,
            UserRepository userRepository,
            AuthService authService)
        {
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
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

                // 평가자 목록 로드
                await LoadEvaluatorsAsync();

                // PM의 담당 프로그램 로드
                await LoadMyProgramsAsync();

                // 물건 목록 로드
                if (CurrentProgram != null)
                {
                    await LoadPropertiesAsync();
                }
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

        private async Task LoadEvaluatorsAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                Evaluators.Clear();
                foreach (var user in users.Where(u => u.IsEvaluator))
                {
                    Evaluators.Add(user);
                }
            }
            catch (Exception)
            {
                // 평가자 목록 로드 실패는 무시
            }
        }

        /// <summary>
        /// PM의 담당 프로그램 로드
        /// </summary>
        private async Task LoadMyProgramsAsync()
        {
            if (CurrentUser == null) return;

            try
            {
                var programUsers = await _programUserRepository.GetUserPMProgramsAsync(CurrentUser.Id);
                MyPrograms.Clear();

                foreach (var pu in programUsers)
                {
                    var program = await _programRepository.GetByIdAsync(pu.ProgramId);
                    if (program != null)
                    {
                        MyPrograms.Add(program);
                    }
                }

                HasMultiplePrograms = MyPrograms.Count > 1;

                // 첫 번째 프로그램을 기본 선택
                if (MyPrograms.Count > 0)
                {
                    CurrentProgram = MyPrograms[0];
                    SelectedProgram = MyPrograms[0];
                    OnPropertyChanged(nameof(ProgramDisplayName));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"담당 프로그램 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 프로그램 선택 변경 시
        /// </summary>
        partial void OnSelectedProgramChanged(Program? value)
        {
            if (value != null)
            {
                CurrentProgram = value;
                OnPropertyChanged(nameof(ProgramDisplayName));
                _ = LoadPropertiesAsync();
            }
        }

        /// <summary>
        /// 물건 목록 로드
        /// </summary>
        private async Task LoadPropertiesAsync()
        {
            if (CurrentProgram == null) return;

            try
            {
                var properties = await _propertyRepository.GetByProgramIdAsync(CurrentProgram.Id);

                Properties.Clear();
                foreach (var property in properties)
                {
                    Properties.Add(property);
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
            await LoadPropertiesAsync();
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

        /// <summary>
        /// 담당자 할당
        /// </summary>
        public async Task AssignToUserAsync(Guid propertyId, Guid userId)
        {
            try
            {
                await _propertyRepository.AssignToUserAsync(propertyId, userId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"할당 실패: {ex.Message}";
            }
        }
    }
}

