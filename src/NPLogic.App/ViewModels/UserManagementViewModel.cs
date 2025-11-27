using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 사용자 관리 ViewModel
    /// </summary>
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;
        private Func<string>? _passwordProvider;

        // 사용자 목록
        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private User _editingUser = new();

        [ObservableProperty]
        private bool _isAddingNew;

        // 필터
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedRoleFilter = "전체";

        [ObservableProperty]
        private string _selectedStatusFilter = "전체";

        [ObservableProperty]
        private ObservableCollection<string> _roleFilters = new() { "전체", "관리자", "PM", "평가자" };

        [ObservableProperty]
        private ObservableCollection<string> _statusFilters = new() { "전체", "활성", "비활성" };

        // 상태
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private int _totalUserCount;

        // 필터링된 사용자 목록
        public ICollectionView FilteredUsers { get; }

        public UserManagementViewModel(UserRepository userRepository, AuthService authService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // CollectionView 설정
            FilteredUsers = CollectionViewSource.GetDefaultView(Users);
            FilteredUsers.Filter = FilterUser;
        }

        /// <summary>
        /// PasswordBox의 비밀번호 제공자 설정
        /// </summary>
        public void SetPasswordProvider(Func<string> passwordProvider)
        {
            _passwordProvider = passwordProvider;
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadUsersAsync();
        }

        /// <summary>
        /// 사용자 목록 로드
        /// </summary>
        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                var users = await _userRepository.GetAllAsync();
                Users.Clear();
                foreach (var user in users.OrderBy(u => u.Name))
                {
                    Users.Add(user);
                }

                TotalUserCount = Users.Count;
                FilteredUsers.Refresh();
            }
            catch (Exception ex)
            {
                ShowError($"사용자 목록 로드 실패: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 사용자 필터링
        /// </summary>
        private bool FilterUser(object obj)
        {
            if (obj is not User user)
                return false;

            // 검색어 필터
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!user.Name.ToLower().Contains(searchLower) &&
                    !user.Email.ToLower().Contains(searchLower))
                {
                    return false;
                }
            }

            // 역할 필터
            if (SelectedRoleFilter != "전체")
            {
                var roleMapping = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "관리자", "admin" },
                    { "PM", "pm" },
                    { "평가자", "evaluator" }
                };

                if (roleMapping.TryGetValue(SelectedRoleFilter, out var role))
                {
                    if (user.Role.ToLower() != role)
                        return false;
                }
            }

            // 상태 필터
            if (SelectedStatusFilter != "전체")
            {
                var statusMapping = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "활성", "active" },
                    { "비활성", "inactive" }
                };

                if (statusMapping.TryGetValue(SelectedStatusFilter, out var status))
                {
                    if (user.Status.ToLower() != status)
                        return false;
                }
            }

            return true;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredUsers.Refresh();
        }

        partial void OnSelectedRoleFilterChanged(string value)
        {
            FilteredUsers.Refresh();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            FilteredUsers.Refresh();
        }

        partial void OnSelectedUserChanged(User? value)
        {
            if (value != null && !IsAddingNew)
            {
                // 선택된 사용자 복사하여 편집
                EditingUser = new User
                {
                    Id = value.Id,
                    AuthUserId = value.AuthUserId,
                    Email = value.Email,
                    Name = value.Name,
                    Role = value.Role,
                    Status = value.Status,
                    CreatedAt = value.CreatedAt,
                    UpdatedAt = value.UpdatedAt
                };
            }
        }

        /// <summary>
        /// 사용자 추가
        /// </summary>
        [RelayCommand]
        private void AddUser()
        {
            SelectedUser = null;
            IsAddingNew = true;
            EditingUser = new User
            {
                Id = Guid.NewGuid(),
                Role = "evaluator",
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 사용자 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveUserAsync()
        {
            try
            {
                // 유효성 검사
                if (string.IsNullOrWhiteSpace(EditingUser.Name))
                {
                    ShowError("이름을 입력해주세요.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingUser.Email))
                {
                    ShowError("이메일을 입력해주세요.");
                    return;
                }

                if (!IsValidEmail(EditingUser.Email))
                {
                    ShowError("올바른 이메일 형식이 아닙니다.");
                    return;
                }

                IsLoading = true;
                ClearError();

                if (IsAddingNew)
                {
                    // 새 사용자 생성
                    var password = _passwordProvider?.Invoke() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        ShowError("비밀번호를 입력해주세요.");
                        IsLoading = false;
                        return;
                    }

                    if (password.Length < 6)
                    {
                        ShowError("비밀번호는 최소 6자 이상이어야 합니다.");
                        IsLoading = false;
                        return;
                    }

                    // Supabase Auth에 사용자 생성
                    try
                    {
                        var authUser = await _authService.CreateUserAsync(EditingUser.Email, password);
                        if (authUser != null)
                        {
                            EditingUser.AuthUserId = Guid.Parse(authUser.Id ?? Guid.NewGuid().ToString());
                        }
                    }
                    catch (Exception authEx)
                    {
                        ShowError($"인증 사용자 생성 실패: {authEx.Message}");
                        IsLoading = false;
                        return;
                    }

                    // users 테이블에 추가
                    await _userRepository.CreateAsync(EditingUser);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("사용자가 추가되었습니다.");
                }
                else
                {
                    // 기존 사용자 수정
                    await _userRepository.UpdateAsync(EditingUser);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("저장되었습니다.");
                }

                IsAddingNew = false;
                await LoadUsersAsync();

                // 저장된 사용자 선택
                SelectedUser = Users.FirstOrDefault(u => u.Id == EditingUser.Id);
            }
            catch (Exception ex)
            {
                ShowError($"저장 실패: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 사용자 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"'{SelectedUser.Name}' 사용자를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                "사용자 삭제",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                ClearError();

                await _userRepository.DeleteAsync(SelectedUser.Id);
                
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("사용자가 삭제되었습니다.");
                
                SelectedUser = null;
                EditingUser = new User();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                ShowError($"삭제 실패: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 편집 취소
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            if (IsAddingNew)
            {
                IsAddingNew = false;
                EditingUser = new User();
            }
            else if (SelectedUser != null)
            {
                // 원래 값으로 복원
                OnSelectedUserChanged(SelectedUser);
            }
            
            ClearError();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadUsersAsync();
        }

        /// <summary>
        /// 이메일 유효성 검사
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 에러 표시
        /// </summary>
        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;

            // 5초 후 에러 메시지 자동 숨김
            Task.Delay(5000).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ErrorMessage == message)
                    {
                        ClearError();
                    }
                });
            });
        }

        /// <summary>
        /// 에러 초기화
        /// </summary>
        private void ClearError()
        {
            ErrorMessage = null;
            HasError = false;
        }
    }
}

