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
    /// 통합 홈 화면 ViewModel - 마스터-디테일 레이아웃
    /// 좌측: 프로그램 목록, 우측: 선택된 프로그램의 물건 목록
    /// Admin/PM/Evaluator 모든 역할에서 사용 (권한별 데이터 필터)
    /// </summary>
    public partial class AdminHomeViewModel : ObservableObject
    {
        private readonly ProgramRepository _programRepository;
        private readonly ProgramUserRepository _programUserRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        // PM 담당 프로그램 ID 목록 (권한별 필터링용)
        private List<Guid> _pmProgramIds = new();

        // ========== 사용자 정보 ==========
        [ObservableProperty]
        private User? _currentUser;

        // ========== 프로그램 목록 (좌측) ==========
        [ObservableProperty]
        private ObservableCollection<Program> _programs = new();

        [ObservableProperty]
        private Program? _selectedProgram;

        // ========== 물건 목록 (우측) ==========
        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        // 필터링을 위한 전체 목록 보관
        private List<Property> _allProperties = new();

        // 현재 필터 상태
        [ObservableProperty]
        private string _currentFilter = "all";

        [ObservableProperty]
        private ObservableCollection<User> _evaluators = new();

        // ========== 통계 ==========
        [ObservableProperty]
        private int _totalProgramCount;

        [ObservableProperty]
        private int _totalPropertyCount;

        [ObservableProperty]
        private int _pendingCount;

        [ObservableProperty]
        private int _processingCount;

        [ObservableProperty]
        private int _completedCount;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isProgramsLoading;

        [ObservableProperty]
        private bool _isPropertiesLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public string DisplayName => CurrentUser?.DisplayName ?? "관리자";

        public AdminHomeViewModel(
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

                // 프로그램 목록 로드
                await LoadProgramsAsync();
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
                foreach (var user in users.Where(u => u.IsEvaluator || u.IsPM))
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
        /// 프로그램 목록 로드 (권한별 필터링)
        /// - Admin: 전체 프로그램
        /// - PM: 담당 프로그램만
        /// - Evaluator: 배정된 물건의 프로그램만
        /// </summary>
        private async Task LoadProgramsAsync()
        {
            try
            {
                IsProgramsLoading = true;
                
                IEnumerable<Program> programs;
                
                if (CurrentUser?.IsPM == true && !CurrentUser.IsAdmin)
                {
                    // PM: 담당 프로그램만 로드
                    await LoadPMProgramIdsAsync();
                    var allPrograms = await _programRepository.GetActiveAsync();
                    programs = allPrograms.Where(p => _pmProgramIds.Contains(p.Id));
                }
                else if (CurrentUser?.IsEvaluator == true && !CurrentUser.IsAdmin && !CurrentUser.IsPM)
                {
                    // Evaluator: 배정된 물건의 프로그램만 로드
                    programs = await GetEvaluatorProgramsAsync();
                }
                else
                {
                    // Admin 또는 기타: 전체 프로그램
                    programs = await _programRepository.GetActiveAsync();
                }
                
                Programs.Clear();
                foreach (var program in programs)
                {
                    Programs.Add(program);
                }

                TotalProgramCount = Programs.Count;

                // 첫 번째 프로그램 자동 선택
                if (Programs.Count > 0 && SelectedProgram == null)
                {
                    SelectedProgram = Programs[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로그램 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsProgramsLoading = false;
            }
        }
        
        /// <summary>
        /// PM 담당 프로그램 ID 목록 로드
        /// </summary>
        private async Task LoadPMProgramIdsAsync()
        {
            _pmProgramIds.Clear();
            
            if (CurrentUser?.IsPM == true)
            {
                try
                {
                    var pmPrograms = await _programUserRepository.GetUserPMProgramsAsync(CurrentUser.Id);
                    _pmProgramIds = pmPrograms.Select(p => p.ProgramId).ToList();
                }
                catch (Exception)
                {
                    // PM 프로그램 로드 실패 시 빈 목록 유지
                }
            }
        }
        
        /// <summary>
        /// Evaluator 배정 물건의 프로그램 목록 로드
        /// </summary>
        private async Task<IEnumerable<Program>> GetEvaluatorProgramsAsync()
        {
            if (CurrentUser == null) return Enumerable.Empty<Program>();
            
            try
            {
                // 자기에게 배정된 물건 조회
                var properties = await _propertyRepository.GetFilteredAsync(assignedTo: CurrentUser.Id);
                
                // 해당 물건들의 프로그램 ID 추출
                var programIds = properties
                    .Where(p => p.ProgramId.HasValue)
                    .Select(p => p.ProgramId!.Value)
                    .Distinct()
                    .ToList();
                
                // 프로그램 목록 조회
                var allPrograms = await _programRepository.GetActiveAsync();
                return allPrograms.Where(p => programIds.Contains(p.Id));
            }
            catch (Exception)
            {
                return Enumerable.Empty<Program>();
            }
        }

        /// <summary>
        /// 프로그램 선택 변경 시 물건 목록 로드
        /// </summary>
        partial void OnSelectedProgramChanged(Program? value)
        {
            if (value != null)
            {
                _ = LoadPropertiesAsync(value.Id);
            }
            else
            {
                Properties.Clear();
                UpdateStatistics();
            }
        }

        /// <summary>
        /// 선택된 프로그램의 물건 목록 로드 (권한별 필터링)
        /// - Admin/PM: 전체 물건
        /// - Evaluator: 자기에게 배정된 물건만
        /// </summary>
        private async Task LoadPropertiesAsync(Guid programId)
        {
            try
            {
                IsPropertiesLoading = true;
                var properties = await _propertyRepository.GetByProgramIdAsync(programId);

                // Evaluator인 경우 자기에게 배정된 물건만 필터링
                IEnumerable<Property> filteredProperties = properties;
                if (CurrentUser?.IsEvaluator == true && !CurrentUser.IsAdmin && !CurrentUser.IsPM)
                {
                    filteredProperties = properties.Where(p => p.AssignedTo == CurrentUser.Id);
                }

                // 전체 목록 보관
                _allProperties = filteredProperties.ToList();
                CurrentFilter = "all";

                Properties.Clear();
                foreach (var property in _allProperties)
                {
                    Properties.Add(property);
                }

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsPropertiesLoading = false;
            }
        }

        /// <summary>
        /// 상태별 필터링 (더블클릭 시 호출)
        /// </summary>
        public void FilterByStatus(string filterType)
        {
            CurrentFilter = filterType;
            
            Properties.Clear();
            
            IEnumerable<Property> filtered = filterType switch
            {
                "pending" => _allProperties.Where(p => p.Status == "pending" || string.IsNullOrEmpty(p.Status)),
                "processing" => _allProperties.Where(p => p.Status == "processing"),
                "completed" => _allProperties.Where(p => p.Status == "completed"),
                _ => _allProperties // "all"
            };

            foreach (var property in filtered)
            {
                Properties.Add(property);
            }
        }

        /// <summary>
        /// 통계 업데이트 (전체 목록 기준)
        /// </summary>
        private void UpdateStatistics()
        {
            TotalPropertyCount = _allProperties.Count;
            PendingCount = _allProperties.Count(p => p.Status == "pending" || string.IsNullOrEmpty(p.Status));
            ProcessingCount = _allProperties.Count(p => p.Status == "processing");
            CompletedCount = _allProperties.Count(p => p.Status == "completed");
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadProgramsAsync();
            if (SelectedProgram != null)
            {
                await LoadPropertiesAsync(SelectedProgram.Id);
            }
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

        /// <summary>
        /// 프로그램 관리 화면으로 이동
        /// </summary>
        [RelayCommand]
        private void GoToProgramManagement()
        {
            MainWindow.Instance?.NavigateToProgramManagement();
        }
    }
}

