using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 프로그램 관리 ViewModel (관리자용)
    /// </summary>
    public partial class ProgramManagementViewModel : ObservableObject
    {
        private readonly ProgramRepository _programRepository;
        private readonly ProgramUserRepository _programUserRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        // ========== 프로그램 목록 ==========
        [ObservableProperty]
        private ObservableCollection<Program> _programs = new();

        [ObservableProperty]
        private Program? _selectedProgram;

        // ========== PM 목록 ==========
        [ObservableProperty]
        private ObservableCollection<User> _pmUsers = new();

        [ObservableProperty]
        private User? _selectedPM;

        // ========== 신규/수정 폼 ==========
        [ObservableProperty]
        private bool _isFormVisible;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _formProgramName = string.Empty;

        [ObservableProperty]
        private string _formTeam = string.Empty;

        [ObservableProperty]
        private string _formAccountingFirm = string.Empty;

        [ObservableProperty]
        private DateTime? _formCutOffDate;

        [ObservableProperty]
        private DateTime? _formBidDate;

        [ObservableProperty]
        private User? _formSelectedPM;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        public string FormTitle => IsEditMode ? "프로그램 수정" : "새 프로그램 등록";

        public ProgramManagementViewModel(
            ProgramRepository programRepository,
            ProgramUserRepository programUserRepository,
            UserRepository userRepository,
            AuthService authService)
        {
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
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

                await LoadPMUsersAsync();
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

        /// <summary>
        /// PM 사용자 목록 로드
        /// </summary>
        private async Task LoadPMUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetByRoleAsync("pm");
                PmUsers.Clear();
                foreach (var user in users)
                {
                    PmUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"PM 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 프로그램 목록 로드
        /// </summary>
        private async Task LoadProgramsAsync()
        {
            try
            {
                var programs = await _programRepository.GetAllAsync();
                Programs.Clear();
                foreach (var program in programs)
                {
                    Programs.Add(program);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로그램 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 새 프로그램 추가 폼 열기
        /// </summary>
        [RelayCommand]
        private void ShowAddForm()
        {
            IsEditMode = false;
            ClearForm();
            IsFormVisible = true;
            OnPropertyChanged(nameof(FormTitle));
        }

        /// <summary>
        /// 프로그램 수정 폼 열기
        /// </summary>
        [RelayCommand]
        private async Task ShowEditForm(Program program)
        {
            if (program == null) return;

            IsEditMode = true;
            SelectedProgram = program;

            FormProgramName = program.ProgramName;
            FormTeam = program.Team ?? string.Empty;
            FormAccountingFirm = program.AccountingFirm ?? string.Empty;
            FormCutOffDate = program.CutOffDate;
            FormBidDate = program.BidDate;

            // 현재 PM 조회
            try
            {
                var pmMapping = await _programUserRepository.GetProgramPMAsync(program.Id);
                if (pmMapping != null)
                {
                    FormSelectedPM = PmUsers.FirstOrDefault(u => u.Id == pmMapping.UserId);
                }
            }
            catch { }

            IsFormVisible = true;
            OnPropertyChanged(nameof(FormTitle));
        }

        /// <summary>
        /// 폼 닫기
        /// </summary>
        [RelayCommand]
        private void CloseForm()
        {
            IsFormVisible = false;
            ClearForm();
        }

        /// <summary>
        /// 폼 초기화
        /// </summary>
        private void ClearForm()
        {
            FormProgramName = string.Empty;
            FormTeam = string.Empty;
            FormAccountingFirm = string.Empty;
            FormCutOffDate = null;
            FormBidDate = null;
            FormSelectedPM = null;
            SelectedProgram = null;
        }

        /// <summary>
        /// 프로그램 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveProgram()
        {
            if (string.IsNullOrWhiteSpace(FormProgramName))
            {
                ErrorMessage = "프로그램명을 입력해주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                Program savedProgram;

                if (IsEditMode && SelectedProgram != null)
                {
                    // 수정
                    SelectedProgram.ProgramName = FormProgramName;
                    SelectedProgram.Team = FormTeam;
                    SelectedProgram.AccountingFirm = FormAccountingFirm;
                    SelectedProgram.CutOffDate = FormCutOffDate;
                    SelectedProgram.BidDate = FormBidDate;

                    savedProgram = await _programRepository.UpdateAsync(SelectedProgram);
                }
                else
                {
                    // 신규 생성
                    var newProgram = new Program
                    {
                        ProgramName = FormProgramName,
                        Team = FormTeam,
                        AccountingFirm = FormAccountingFirm,
                        CutOffDate = FormCutOffDate,
                        BidDate = FormBidDate,
                        Status = "active"
                    };

                    savedProgram = await _programRepository.CreateAsync(newProgram);
                }

                // PM 배정
                if (FormSelectedPM != null)
                {
                    await _programUserRepository.AssignPMAsync(savedProgram.Id, FormSelectedPM.Id);
                }

                SuccessMessage = IsEditMode ? "프로그램이 수정되었습니다." : "프로그램이 등록되었습니다.";
                
                CloseForm();
                await LoadProgramsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 프로그램 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteProgram(Program program)
        {
            if (program == null) return;

            var result = MessageBox.Show(
                $"'{program.ProgramName}' 프로그램을 삭제하시겠습니까?\n\n주의: 프로그램에 연결된 물건이 있으면 삭제할 수 없습니다.",
                "프로그램 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await _programRepository.DeleteAsync(program.Id);
                
                SuccessMessage = "프로그램이 삭제되었습니다.";
                await LoadProgramsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadProgramsAsync();
        }

        /// <summary>
        /// 관리자 홈으로 돌아가기
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            MainWindow.Instance?.NavigateToAdminHome();
        }
    }
}

