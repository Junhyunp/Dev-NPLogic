using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Data.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 로그인 화면 ViewModel
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly SupabaseService _supabaseService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        // 회원가입 시 역할 선택
        [ObservableProperty]
        private string _selectedRole = "evaluator";

        [ObservableProperty]
        private string _signUpName = string.Empty;

        // 비밀번호 재설정 관련
        [ObservableProperty]
        private bool _isPasswordResetMode;

        [ObservableProperty]
        private string _passwordResetEmail = string.Empty;

        [ObservableProperty]
        private string? _passwordResetErrorMessage;

        [ObservableProperty]
        private string? _passwordResetSuccessMessage;

        public LoginViewModel(AuthService authService, SupabaseService supabaseService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 회원가입 명령
        /// </summary>
        [RelayCommand]
        private async Task SignUpAsync()
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrWhiteSpace(Email))
                {
                    ErrorMessage = "이메일을 입력해주세요.";
                    return;
                }

                if (!IsValidEmail(Email))
                {
                    ErrorMessage = "올바른 이메일 형식이 아닙니다.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "비밀번호를 입력해주세요.";
                    return;
                }

                if (Password.Length < 6)
                {
                    ErrorMessage = "비밀번호는 최소 6자 이상이어야 합니다.";
                    return;
                }

                IsLoading = true;
                ErrorMessage = null;

                // 회원가입 시도 (이름, 역할 포함)
                var name = string.IsNullOrWhiteSpace(SignUpName) ? null : SignUpName;
                var (success, errorMessage, user) = await _authService.SignUpWithEmailAsync(Email, Password, name, SelectedRole);

                if (success && user != null)
                {
                    // 회원가입 성공
                    ErrorMessage = null;
                    // Supabase는 이메일 확인이 필요할 수 있으므로 메시지 표시
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "회원가입이 완료되었습니다!\n이메일을 확인하여 계정을 활성화해주세요.",
                            "회원가입 성공",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
                else
                {
                    ErrorMessage = errorMessage ?? "회원가입에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"회원가입 오류: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 이메일/비밀번호 로그인 명령
        /// </summary>
        [RelayCommand]
        private async Task SignInWithEmailAsync()
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrWhiteSpace(Email))
                {
                    ErrorMessage = "이메일을 입력해주세요.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "비밀번호를 입력해주세요.";
                    return;
                }

                if (!IsValidEmail(Email))
                {
                    ErrorMessage = "올바른 이메일 형식이 아닙니다.";
                    return;
                }

                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // 로그인 시도
                var (success, errorMessage, user) = await _authService.SignInWithEmailAsync(Email, Password, RememberMe);

                if (success && user != null)
                {
                    // 로그인 성공
                    OnLoginSuccess();
                }
                else
                {
                    ErrorMessage = errorMessage ?? "로그인에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"로그인 오류: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 비밀번호 재설정

        /// <summary>
        /// 비밀번호 재설정 모드 표시
        /// </summary>
        [RelayCommand]
        private void ShowPasswordReset()
        {
            IsPasswordResetMode = true;
            PasswordResetEmail = Email; // 로그인 폼의 이메일을 미리 채움
            PasswordResetErrorMessage = null;
            PasswordResetSuccessMessage = null;
        }

        /// <summary>
        /// 비밀번호 재설정 모드 숨기기
        /// </summary>
        [RelayCommand]
        private void HidePasswordReset()
        {
            IsPasswordResetMode = false;
            PasswordResetErrorMessage = null;
            PasswordResetSuccessMessage = null;
        }

        /// <summary>
        /// 비밀번호 재설정 이메일 전송
        /// </summary>
        [RelayCommand]
        private async Task SendPasswordResetEmailAsync()
        {
            try
            {
                // 입력 검증
                if (string.IsNullOrWhiteSpace(PasswordResetEmail))
                {
                    PasswordResetErrorMessage = "이메일을 입력해주세요.";
                    return;
                }

                if (!IsValidEmail(PasswordResetEmail))
                {
                    PasswordResetErrorMessage = "올바른 이메일 형식이 아닙니다.";
                    return;
                }

                IsLoading = true;
                PasswordResetErrorMessage = null;
                PasswordResetSuccessMessage = null;

                // Supabase 비밀번호 재설정 이메일 전송
                var success = await _authService.SendPasswordResetEmailAsync(PasswordResetEmail);

                if (success)
                {
                    PasswordResetSuccessMessage = $"비밀번호 재설정 링크가 {PasswordResetEmail}로 전송되었습니다.\n이메일을 확인해주세요.";
                }
                else
                {
                    PasswordResetErrorMessage = "비밀번호 재설정 이메일 전송에 실패했습니다.\n이메일 주소를 다시 확인해주세요.";
                }
            }
            catch (Exception ex)
            {
                PasswordResetErrorMessage = $"오류가 발생했습니다: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        /// <summary>
        /// 이메일 형식 검증
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
        /// 로그인 성공 시 호출
        /// </summary>
        private void OnLoginSuccess()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // 현재 로그인 창 찾기
                    var loginWindow = Application.Current.Windows
                        .OfType<Views.LoginWindow>()
                        .FirstOrDefault();

                    // 메인 윈도우 생성 및 표시
                    var serviceProvider = App.ServiceProvider;
                    if (serviceProvider != null)
                    {
                        var mainWindow = serviceProvider.GetService(typeof(MainWindow)) as MainWindow;
                        if (mainWindow != null)
                        {
                            Application.Current.MainWindow = mainWindow;
                            mainWindow.Show();
                            loginWindow?.Close();
                        }
                        else
                        {
                            MessageBox.Show("메인 윈도우를 생성할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("서비스 프로바이더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"메인 윈도우 표시 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        /// 테스트 로그인 (개발용)
        /// </summary>
        [RelayCommand]
        private void TestLogin()
        {
            // 개발 중 테스트용
            OnLoginSuccess();
        }
    }
}
