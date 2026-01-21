using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;
using NPLogic.Views;

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
        private readonly PropertyRepository _propertyRepository;
        private readonly BorrowerRepository _borrowerRepository;
        private readonly LoanRepository _loanRepository;
        private readonly BorrowerRestructuringRepository _borrowerRestructuringRepository;
        private readonly RightAnalysisRepository _rightAnalysisRepository;
        private readonly InterimRepository _interimRepository;
        private readonly AuthService _authService;
        private readonly ExcelService _excelService;
        private readonly DataDiskUploadService _uploadService;
        private readonly ProgramSheetMappingRepository _sheetMappingRepository;
        private readonly PermissionService _permissionService;

        [ObservableProperty]
        private User? _currentUser;

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

        // ========== 새 필드: 적용환율, 은행명, Pool ==========
        [ObservableProperty]
        private decimal? _formExchangeRateUsd;

        [ObservableProperty]
        private decimal? _formExchangeRateJpy;

        [ObservableProperty]
        private string _formBankName = string.Empty;

        [ObservableProperty]
        private string _formPool = string.Empty;

        // ========== 데이터디스크 업로드 ==========
        [ObservableProperty]
        private bool _hasDataDiskFile;

        [ObservableProperty]
        private string? _dataDiskFileName;

        [ObservableProperty]
        private string? _dataDiskFilePath;

        [ObservableProperty]
        private int _dataDiskTotalRows;

        [ObservableProperty]
        private ObservableCollection<string> _dataDiskColumns = new();

        [ObservableProperty]
        private string? _dataDiskErrorMessage;

        private List<Dictionary<string, object>>? _dataDiskData;

        // ========== 시트 선택 ==========
        [ObservableProperty]
        private ObservableCollection<SelectableSheetInfo> _availableSheets = new();

        [ObservableProperty]
        private bool _showSheetSelection;

        /// <summary>
        /// 선택된 시트가 있는지
        /// </summary>
        public bool HasSelectedSheets => AvailableSheets.Any(s => s.IsSelected);

        // ========== 은행 감지 ==========
        [ObservableProperty]
        private BankType _detectedBankType = BankType.Unknown;

        /// <summary>
        /// 감지된 은행 표시명
        /// </summary>
        public string DetectedBankDisplay => DetectedBankType switch
        {
            BankType.KB => "KB국민은행",
            BankType.IBK => "IBK기업은행",
            BankType.NH => "NH농협은행",
            BankType.SHB => "SH수협은행",
            _ => "미감지"
        };

        /// <summary>
        /// 은행이 감지되었는지 여부
        /// </summary>
        public bool IsBankDetected => DetectedBankType != BankType.Unknown;

        // ========== 시트 매핑 (통합 모듈) ==========
        /// <summary>
        /// 시트 매핑 정보 목록 (DataDiskUploadService 사용)
        /// </summary>
        private List<SheetMappingInfo> _sheetMappingInfoList = new();

        /// <summary>
        /// 사용 가능한 시트 타입 목록 (드롭다운용)
        /// </summary>
        public List<SheetTypeOption> AvailableSheetTypes { get; } = new()
        {
            new SheetTypeOption { Value = DataDiskSheetType.Unknown, DisplayName = "(선택안함)" },
            new SheetTypeOption { Value = DataDiskSheetType.BorrowerGeneral, DisplayName = "차주일반정보" },
            new SheetTypeOption { Value = DataDiskSheetType.BorrowerRestructuring, DisplayName = "회생차주정보" },
            new SheetTypeOption { Value = DataDiskSheetType.Loan, DisplayName = "채권정보" },
            new SheetTypeOption { Value = DataDiskSheetType.Property, DisplayName = "담보물건정보" },
            new SheetTypeOption { Value = DataDiskSheetType.RegistryDetail, DisplayName = "등기부등본정보" },
            new SheetTypeOption { Value = DataDiskSheetType.CollateralSetting, DisplayName = "담보설정정보" },
            new SheetTypeOption { Value = DataDiskSheetType.Guarantee, DisplayName = "보증정보" }
        };

        // ========== Interim 업로드 ==========
        [ObservableProperty]
        private bool _hasInterimFile;

        [ObservableProperty]
        private string? _interimFileName;

        [ObservableProperty]
        private string? _interimFilePath;

        [ObservableProperty]
        private int _interimTotalRows;

        [ObservableProperty]
        private ObservableCollection<string> _interimColumns = new();

        [ObservableProperty]
        private string? _interimErrorMessage;

        private List<Dictionary<string, object>>? _interimData;

        // ========== Interim 시트 선택 ==========
        [ObservableProperty]
        private ObservableCollection<SelectableSheetInfo> _interimAvailableSheets = new();

        [ObservableProperty]
        private bool _showInterimSheetSelection;

        /// <summary>
        /// Interim 선택된 시트가 있는지
        /// </summary>
        public bool HasSelectedInterimSheets => InterimAvailableSheets.Any(s => s.IsSelected);

        // ========== 시트 매핑 정보 ==========
        [ObservableProperty]
        private ObservableCollection<ProgramSheetMapping> _existingSheetMappings = new();

        // ========== 재업로드 모드 ==========
        [ObservableProperty]
        private bool _isDataDiskReuploadMode;

        [ObservableProperty]
        private bool _isInterimReuploadMode;

        /// <summary>
        /// 데이터디스크 시트 매핑 (데이터디스크 관련 시트만 필터링)
        /// </summary>
        public IEnumerable<ProgramSheetMapping> DataDiskSheetMappings => 
            ExistingSheetMappings.Where(m => !m.SheetType.StartsWith("Interim"));

        /// <summary>
        /// Interim 시트 매핑 (Interim 관련 시트만 필터링)
        /// </summary>
        public IEnumerable<ProgramSheetMapping> InterimSheetMappings => 
            ExistingSheetMappings.Where(m => m.SheetType.StartsWith("Interim"));

        /// <summary>
        /// 데이터디스크 시트가 업로드되어 있는지 여부
        /// </summary>
        public bool HasUploadedDataDiskSheets => 
            IsEditMode && ExistingSheetMappings.Any(m => !m.SheetType.StartsWith("Interim") && m.UploadedByName != null);

        /// <summary>
        /// Interim 시트가 업로드되어 있는지 여부
        /// </summary>
        public bool HasUploadedInterimSheets => 
            IsEditMode && ExistingSheetMappings.Any(m => m.SheetType.StartsWith("Interim") && m.UploadedByName != null);

        /// <summary>
        /// 데이터디스크 시트 정보 표시 여부 (수정 모드이고 재업로드 모드가 아닐 때)
        /// </summary>
        public bool ShowDataDiskSheetInfo => IsEditMode && !IsDataDiskReuploadMode;

        /// <summary>
        /// Interim 시트 정보 표시 여부 (수정 모드이고 재업로드 모드가 아닐 때)
        /// </summary>
        public bool ShowInterimSheetInfo => IsEditMode && !IsInterimReuploadMode;

        /// <summary>
        /// 현재 사용자 이름
        /// </summary>
        public string CurrentUserName => _authService.GetSession()?.User?.Email?.Split('@')[0] ?? "사용자";

        /// <summary>
        /// 현재 사용자 ID
        /// </summary>
        public Guid? CurrentUserId => _authService.GetSession()?.User?.Id != null 
            ? Guid.Parse(_authService.GetSession()!.User!.Id!) 
            : null;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // ========== 진행 상황 카운터 ==========
        [ObservableProperty]
        private int _progressCurrent;

        [ObservableProperty]
        private int _progressTotal;

        [ObservableProperty]
        private string _progressText = "";

        /// <summary>
        /// 진행 상황 업데이트
        /// </summary>
        private void UpdateProgress(int current, int total = 0, string? sheetName = null)
        {
            ProgressCurrent = current;
            if (total > 0) ProgressTotal = total;
            
            if (string.IsNullOrEmpty(sheetName))
            {
                ProgressText = total > 0 ? $"처리 중... {current}/{total}" : $"처리 중... {current}건";
            }
            else
            {
                ProgressText = total > 0 ? $"{sheetName}: {current}/{total}" : $"{sheetName}: {current}건";
            }
        }

        public string FormTitle => IsEditMode ? "프로그램 수정" : "새 프로그램 등록";

        public ProgramManagementViewModel(
            ProgramRepository programRepository,
            ProgramUserRepository programUserRepository,
            UserRepository userRepository,
            PropertyRepository propertyRepository,
            BorrowerRepository borrowerRepository,
            LoanRepository loanRepository,
            BorrowerRestructuringRepository borrowerRestructuringRepository,
            RightAnalysisRepository rightAnalysisRepository,
            InterimRepository interimRepository,
            AuthService authService,
            ExcelService excelService,
            DataDiskUploadService uploadService,
            ProgramSheetMappingRepository sheetMappingRepository,
            PermissionService permissionService)
        {
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
            _borrowerRestructuringRepository = borrowerRestructuringRepository ?? throw new ArgumentNullException(nameof(borrowerRestructuringRepository));
            _rightAnalysisRepository = rightAnalysisRepository ?? throw new ArgumentNullException(nameof(rightAnalysisRepository));
            _interimRepository = interimRepository ?? throw new ArgumentNullException(nameof(interimRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            _sheetMappingRepository = sheetMappingRepository ?? throw new ArgumentNullException(nameof(sheetMappingRepository));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

            // 업로드 서비스 진행 상황 콜백 설정
            _uploadService.OnProgressUpdate = UpdateProgress;
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
                CurrentUser = await _permissionService.GetCurrentUserAsync();

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
            FormExchangeRateUsd = program.ExchangeRateUsd;
            FormExchangeRateJpy = program.ExchangeRateJpy;
            FormBankName = program.BankName ?? string.Empty;
            FormPool = program.Pool ?? string.Empty;

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

            // 기존 시트 매핑 정보 로드 (수정 모드)
            await LoadExistingSheetMappingsAsync(program.Id);

            IsFormVisible = true;
            OnPropertyChanged(nameof(FormTitle));
        }

        /// <summary>
        /// 기존 시트 매핑 정보 로드
        /// </summary>
        private async Task LoadExistingSheetMappingsAsync(Guid programId)
        {
            ExistingSheetMappings.Clear();

            // DB에서 기존 매핑 정보 로드
            var existingMappings = await _sheetMappingRepository.GetByProgramIdAsync(programId);
            var existingDict = existingMappings.ToDictionary(m => m.SheetType, m => m);

            // 기본 시트 타입 목록 (DB에 없어도 표시)
            var defaultSheetTypes = new[]
            {
                ("SheetA", "차주일반정보"),
                ("SheetA1", "회생차주정보"),
                ("SheetB", "채권일반정보"),
                ("SheetC1", "물건정보"),
                ("SheetC2", "등기부등본정보"),
                ("SheetC3", "설정순위"),
                ("SheetD", "신용보증서"),
                ("SheetE", "가압류"),
                ("Interim_Advance", "추가 가지급금"),
                ("Interim_Collection", "회수정보")
            };

            foreach (var (sheetType, displayName) in defaultSheetTypes)
            {
                // DB에 저장된 매핑이 있으면 사용, 없으면 기본값
                if (existingDict.TryGetValue(sheetType, out var existing))
                {
                    ExistingSheetMappings.Add(existing);
                }
                else
                {
                    ExistingSheetMappings.Add(new ProgramSheetMapping
                    {
                        Id = Guid.NewGuid(),
                        ProgramId = programId,
                        SheetType = sheetType,
                        SheetTypeDisplayName = displayName,
                        UploadedAt = DateTime.MinValue,
                        UploadedByName = null
                    });
                }
            }

            // 관련 속성 변경 알림
            OnPropertyChanged(nameof(DataDiskSheetMappings));
            OnPropertyChanged(nameof(InterimSheetMappings));
            OnPropertyChanged(nameof(HasUploadedDataDiskSheets));
            OnPropertyChanged(nameof(HasUploadedInterimSheets));
            OnPropertyChanged(nameof(ShowDataDiskSheetInfo));
            OnPropertyChanged(nameof(ShowInterimSheetInfo));

            await Task.CompletedTask;
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
            FormExchangeRateUsd = null;
            FormExchangeRateJpy = null;
            FormBankName = string.Empty;
            FormPool = string.Empty;
            SelectedProgram = null;
            ExistingSheetMappings.Clear();
            ClearDataDiskFile();
            ClearInterimFile();
            
            // 재업로드 모드 초기화
            IsDataDiskReuploadMode = false;
            IsInterimReuploadMode = false;
            OnPropertyChanged(nameof(ShowDataDiskSheetInfo));
            OnPropertyChanged(nameof(ShowInterimSheetInfo));
            OnPropertyChanged(nameof(DataDiskSheetMappings));
            OnPropertyChanged(nameof(InterimSheetMappings));
        }

        /// <summary>
        /// 데이터디스크 파일 초기화
        /// </summary>
        private void ClearDataDiskFile()
        {
            HasDataDiskFile = false;
            DataDiskFileName = null;
            DataDiskFilePath = null;
            DataDiskTotalRows = 0;
            DataDiskColumns.Clear();
            DataDiskErrorMessage = null;
            _dataDiskData = null;
            AvailableSheets.Clear();
            ShowSheetSelection = false;
        }

        /// <summary>
        /// Interim 파일 초기화
        /// </summary>
        private void ClearInterimFile()
        {
            HasInterimFile = false;
            InterimFileName = null;
            InterimFilePath = null;
            InterimTotalRows = 0;
            InterimColumns.Clear();
            InterimErrorMessage = null;
            _interimData = null;
            InterimAvailableSheets.Clear();
            ShowInterimSheetSelection = false;
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
                    SelectedProgram.ExchangeRateUsd = FormExchangeRateUsd;
                    SelectedProgram.ExchangeRateJpy = FormExchangeRateJpy;
                    SelectedProgram.BankName = FormBankName;
                    SelectedProgram.Pool = FormPool;

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
                        ExchangeRateUsd = FormExchangeRateUsd,
                        ExchangeRateJpy = FormExchangeRateJpy,
                        BankName = FormBankName,
                        Pool = FormPool,
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

            // 권한 체크: 해당 프로그램의 PM 또는 Admin만 삭제 가능
            var canDelete = await _permissionService.CanDeleteAsync(program.Id, CurrentUser);
            if (!canDelete)
            {
                ErrorMessage = PermissionService.GetNoPermissionMessage("delete");
                return;
            }

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

        // ========== 데이터디스크 업로드 관련 ==========

        /// <summary>
        /// 데이터디스크 파일 선택
        /// </summary>
        [RelayCommand]
        private void SelectDataDiskFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "데이터디스크 파일 선택"
            };

            if (dialog.ShowDialog() == true)
            {
                HandleDataDiskFileDrop(dialog.FileName);
            }
        }

        /// <summary>
        /// 데이터디스크 파일 선택 취소
        /// </summary>
        [RelayCommand]
        private void CancelDataDiskFile()
        {
            ClearDataDiskFile();
        }

        // ========== Interim 업로드 관련 ==========

        /// <summary>
        /// Interim 파일 선택
        /// </summary>
        [RelayCommand]
        private void SelectInterimFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Interim 파일 선택"
            };

            if (dialog.ShowDialog() == true)
            {
                HandleInterimFileDrop(dialog.FileName);
            }
        }

        /// <summary>
        /// Interim 파일 선택 취소
        /// </summary>
        [RelayCommand]
        private void CancelInterimFile()
        {
            ClearInterimFile();
        }

        /// <summary>
        /// 데이터디스크 재업로드 모드 활성화
        /// </summary>
        [RelayCommand]
        private void EnableDataDiskReupload()
        {
            IsDataDiskReuploadMode = true;
            ClearDataDiskFile();
            OnPropertyChanged(nameof(ShowDataDiskSheetInfo));
        }

        /// <summary>
        /// 데이터디스크 재업로드 모드 취소
        /// </summary>
        [RelayCommand]
        private void CancelDataDiskReupload()
        {
            IsDataDiskReuploadMode = false;
            ClearDataDiskFile();
            OnPropertyChanged(nameof(ShowDataDiskSheetInfo));
        }

        /// <summary>
        /// Interim 재업로드 모드 활성화
        /// </summary>
        [RelayCommand]
        private void EnableInterimReupload()
        {
            IsInterimReuploadMode = true;
            ClearInterimFile();
            OnPropertyChanged(nameof(ShowInterimSheetInfo));
        }

        /// <summary>
        /// Interim 재업로드 모드 취소
        /// </summary>
        [RelayCommand]
        private void CancelInterimReupload()
        {
            IsInterimReuploadMode = false;
            ClearInterimFile();
            OnPropertyChanged(nameof(ShowInterimSheetInfo));
        }

        /// <summary>
        /// Interim 파일 드롭 처리
        /// </summary>
        public async void HandleInterimFileDrop(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    InterimErrorMessage = "파일을 찾을 수 없습니다.";
                    return;
                }

                var ext = Path.GetExtension(filePath).ToLower();
                if (ext != ".xlsx" && ext != ".xls")
                {
                    InterimErrorMessage = "Excel 파일만 업로드 가능합니다 (.xlsx, .xls)";
                    return;
                }

                InterimErrorMessage = null;
                InterimFilePath = filePath;
                InterimFileName = Path.GetFileName(filePath);

                // 시트 목록 감지
                var sheets = _excelService.GetSheetNames(filePath);

                if (sheets.Count > 1)
                {
                    // 여러 시트가 있으면 시트 선택 UI 표시
                    InterimAvailableSheets.Clear();
                    foreach (var sheet in sheets)
                    {
                        var selectableSheet = new SelectableSheetInfo
                        {
                            Name = sheet.Name,
                            Index = sheet.Index,
                            RowCount = sheet.RowCount,
                            Headers = sheet.Headers,
                            SheetType = sheet.SheetType,
                            HeaderRow = sheet.HeaderRow,  // 감지된 헤더 행 번호
                            IsSelected = true // Interim은 기본적으로 모두 선택
                        };
                        InterimAvailableSheets.Add(selectableSheet);
                    }
                    
                    ShowInterimSheetSelection = true;
                    HasInterimFile = true;
                    OnPropertyChanged(nameof(HasSelectedInterimSheets));
                    
                    // 총 행 수 계산
                    InterimTotalRows = sheets.Sum(s => s.RowCount);
                }
                else
                {
                    // 단일 시트: 기존 방식
                    ShowInterimSheetSelection = false;
                    var (columns, data) = await _excelService.ReadExcelFileAsync(filePath);

                    InterimColumns.Clear();
                    foreach (var col in columns)
                    {
                        InterimColumns.Add(col);
                    }

                    _interimData = data;
                    InterimTotalRows = data.Count;
                    HasInterimFile = true;
                }
            }
            catch (Exception ex)
            {
                InterimErrorMessage = $"Excel 파일 읽기 실패: {ex.Message}";
                HasInterimFile = false;
            }
        }

        /// <summary>
        /// Interim 시트 선택 토글 - 바인딩에 의해 IsSelected가 이미 변경됨, 알림만 전달
        /// </summary>
        public void ToggleInterimSheetSelection(SelectableSheetInfo sheet)
        {
            // 바인딩에 의해 IsSelected가 이미 변경되었으므로 알림만 전달
            OnPropertyChanged(nameof(HasSelectedInterimSheets));
        }

        /// <summary>
        /// 데이터디스크 파일 드롭 처리
        /// </summary>
        public async void HandleDataDiskFileDrop(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    DataDiskErrorMessage = "파일을 찾을 수 없습니다.";
                    return;
                }

                var ext = Path.GetExtension(filePath).ToLower();
                if (ext != ".xlsx" && ext != ".xls")
                {
                    DataDiskErrorMessage = "Excel 파일만 업로드 가능합니다 (.xlsx, .xls)";
                    return;
                }

                DataDiskErrorMessage = null;
                DataDiskFilePath = filePath;
                DataDiskFileName = Path.GetFileName(filePath);

                // 은행별 매핑 템플릿을 사용한 시트 로드
                var sheetMappings = _uploadService.LoadExcelSheets(filePath);
                
                // 감지된 은행 타입 저장
                DetectedBankType = _uploadService.DetectedBankType;
                OnPropertyChanged(nameof(DetectedBankDisplay));
                OnPropertyChanged(nameof(IsBankDetected));
                
                System.Diagnostics.Debug.WriteLine($"[HandleDataDiskFileDrop] 감지된 은행: {DetectedBankType} ({DetectedBankDisplay})");

                // 시트 목록 가져오기 (기존 ExcelService도 함께 사용)
                var sheets = _excelService.GetSheetNames(filePath);

                if (sheets.Count > 1)
                {
                    // 여러 시트가 있으면 시트 선택 UI 표시
                    AvailableSheets.Clear();
                    
                    foreach (var sheet in sheets)
                    {
                        // 은행별 매핑에서 시트 타입 찾기
                        var mappingInfo = sheetMappings.FirstOrDefault(m => m.ExcelSheetName == sheet.Name);
                        var sheetType = mappingInfo?.DetectedType != DataDiskSheetType.Unknown 
                            ? ConvertDataDiskTypeToSheetType(mappingInfo.DetectedType) 
                            : sheet.SheetType;

                        var selectableSheet = new SelectableSheetInfo
                        {
                            Name = sheet.Name,
                            Index = sheet.Index,
                            RowCount = sheet.RowCount,
                            Headers = sheet.Headers,
                            SheetType = sheetType,
                            HeaderRow = sheet.HeaderRow,
                            IsSelected = sheetType != SheetType.Unknown
                        };
                        AvailableSheets.Add(selectableSheet);
                    }
                    
                    // 시트 매핑 정보 저장 (나중에 업로드 시 사용)
                    _sheetMappingInfoList = sheetMappings;
                    
                    ShowSheetSelection = true;
                    HasDataDiskFile = true;
                    OnPropertyChanged(nameof(HasSelectedSheets));
                    
                    // 총 행 수 계산 (선택된 시트들)
                    DataDiskTotalRows = AvailableSheets.Where(s => s.IsSelected).Sum(s => s.RowCount);
                }
                else
                {
                    // 단일 시트: 기존 방식
                    ShowSheetSelection = false;
                    var (columns, data) = await _excelService.ReadExcelFileAsync(filePath);

                    DataDiskColumns.Clear();
                    foreach (var col in columns)
                    {
                        DataDiskColumns.Add(col);
                    }

                    _dataDiskData = data;
                    DataDiskTotalRows = data.Count;
                    HasDataDiskFile = true;
                }
            }
            catch (Exception ex)
            {
                DataDiskErrorMessage = $"Excel 파일 읽기 실패: {ex.Message}";
                HasDataDiskFile = false;
            }
        }

        /// <summary>
        /// DataDiskSheetType → SheetType 변환
        /// </summary>
        private SheetType ConvertDataDiskTypeToSheetType(DataDiskSheetType dataDiskType)
        {
            return dataDiskType switch
            {
                DataDiskSheetType.BorrowerGeneral => SheetType.BorrowerGeneral,
                DataDiskSheetType.BorrowerRestructuring => SheetType.BorrowerRestructuring,
                DataDiskSheetType.Loan => SheetType.Loan,
                DataDiskSheetType.Property => SheetType.Property,
                DataDiskSheetType.RegistryDetail => SheetType.RegistryDetail,
                DataDiskSheetType.CollateralSetting => SheetType.CollateralSetting,
                DataDiskSheetType.Guarantee => SheetType.Guarantee,
                _ => SheetType.Unknown
            };
        }

        /// <summary>
        /// 시트 선택 토글 - 바인딩에 의해 IsSelected가 이미 변경됨, 알림만 전달
        /// </summary>
        public void ToggleSheetSelection(SelectableSheetInfo sheet)
        {
            // 바인딩에 의해 IsSelected가 이미 변경되었으므로 알림만 전달
            OnPropertyChanged(nameof(HasSelectedSheets));
        }

        /// <summary>
        /// 프로그램 저장 및 물건 생성
        /// </summary>
        [RelayCommand]
        private async Task SaveProgramWithData()
        {
            if (string.IsNullOrWhiteSpace(FormProgramName))
            {
                ErrorMessage = "프로그램명을 입력해주세요.";
                return;
            }

            // 시트 선택 모드에서는 선택된 시트가 있어야 함
            if (ShowSheetSelection && !HasSelectedSheets)
            {
                DataDiskErrorMessage = "업로드할 시트를 선택해주세요.";
                return;
            }

            // 단일 시트 모드에서는 데이터가 있어야 함
            if (!ShowSheetSelection && (_dataDiskData == null || _dataDiskData.Count == 0))
            {
                DataDiskErrorMessage = "업로드할 데이터가 없습니다.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                DataDiskErrorMessage = null;
                ProgressText = "처리 중...";
                ProgressCurrent = 0;
                ProgressTotal = 0;

                // 1. 프로그램 생성
                Program savedProgram;

                if (IsEditMode && SelectedProgram != null)
                {
                    // 수정
                    SelectedProgram.ProgramName = FormProgramName;
                    SelectedProgram.Team = FormTeam;
                    SelectedProgram.AccountingFirm = FormAccountingFirm;
                    SelectedProgram.CutOffDate = FormCutOffDate;
                    SelectedProgram.BidDate = FormBidDate;
                    SelectedProgram.ExchangeRateUsd = FormExchangeRateUsd;
                    SelectedProgram.ExchangeRateJpy = FormExchangeRateJpy;
                    SelectedProgram.BankName = FormBankName;
                    SelectedProgram.Pool = FormPool;

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
                        ExchangeRateUsd = FormExchangeRateUsd,
                        ExchangeRateJpy = FormExchangeRateJpy,
                        BankName = FormBankName,
                        Pool = FormPool,
                        Status = "active"
                    };

                    savedProgram = await _programRepository.CreateAsync(newProgram);
                }

                // PM 배정
                if (FormSelectedPM != null)
                {
                    await _programUserRepository.AssignPMAsync(savedProgram.Id, FormSelectedPM.Id);
                }

                // 2. 데이터 업로드
                int totalCreated = 0;
                int totalUpdated = 0;
                int totalFailed = 0;

                if (ShowSheetSelection)
                {
                    // 시트 선택 모드: 선택된 시트들 처리
                    var selectedSheets = AvailableSheets.Where(s => s.IsSelected).ToList();
                    
                    foreach (var sheet in selectedSheets)
                    {
                        try
                        {
                            var result = await ProcessSheetAsync(DataDiskFilePath!, sheet, savedProgram.Id.ToString());
                            totalCreated += result.Created;
                            totalUpdated += result.Updated;
                            totalFailed += result.Failed;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"시트 처리 실패 ({sheet.Name}): {ex.Message}");
                            totalFailed++;
                        }
                    }
                }
                else
                {
                    // 단일 시트 모드
                    foreach (var row in _dataDiskData!)
                    {
                        try
                        {
                            var property = MapRowToProperty(row, savedProgram.Id.ToString());
                            await _propertyRepository.CreateAsync(property);
                            totalCreated++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"물건 생성 실패: {ex.Message}");
                            totalFailed++;
                        }
                    }
                }

                // 3. Interim 데이터 업로드
                int interimAdvanceCreated = 0;
                int interimCollectionCreated = 0;
                int interimFailed = 0;

                if (HasInterimFile && !string.IsNullOrEmpty(InterimFilePath))
                {
                    var interimResult = await ProcessInterimFileAsync(InterimFilePath, savedProgram.Id);
                    interimAdvanceCreated = interimResult.AdvanceCreated;
                    interimCollectionCreated = interimResult.CollectionCreated;
                    interimFailed = interimResult.Failed;
                }

                // 완료 메시지
                var message = $"프로그램이 등록되었습니다.\n";
                if (totalCreated > 0 || totalUpdated > 0 || totalFailed > 0)
                {
                    message += $"데이터디스크: 생성 {totalCreated}건";
                    if (totalUpdated > 0) message += $", 업데이트 {totalUpdated}건";
                    if (totalFailed > 0) message += $", 실패 {totalFailed}건";
                    message += "\n";
                }
                if (interimAdvanceCreated > 0 || interimCollectionCreated > 0 || interimFailed > 0)
                {
                    message += $"Interim: 가지급금 {interimAdvanceCreated}건, 회수정보 {interimCollectionCreated}건";
                    if (interimFailed > 0) message += $", 실패 {interimFailed}건";
                }

                MessageBox.Show(message, "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                
                SuccessMessage = message;
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
        /// 시트별 데이터 처리 - Property 시트만 물건으로 저장
        /// </summary>
        private async Task<(int Created, int Updated, int Failed)> ProcessSheetAsync(string filePath, SelectableSheetInfo sheet, string programId)
        {
            var headerRow = _excelService.DetectHeaderRow(filePath, sheet.Name);
            var (columns, data) = await _excelService.ReadExcelSheetAsync(filePath, sheet.Name, headerRow);
            
            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 시트 '{sheet.Name}' ({sheet.SheetType}): {data.Count}행, 헤더행={headerRow}");
            
            if (data.Count == 0)
                return (0, 0, 0);

            int created = 0, updated = 0, failed = 0;
            int totalRows = data.Count;
            int processed = 0;

            // 차주번호 -> 차주ID 캐시 (Loan, Restructuring 저장 시 사용)
            var borrowerCache = new Dictionary<string, Guid>();

            switch (sheet.SheetType)
            {
                case SheetType.BorrowerGeneral:
                    // Sheet A: 차주일반정보 -> borrowers 테이블
                    foreach (var row in data)
                    {
                        processed++;
                        UpdateProgress(processed, totalRows, sheet.Name);

                        try
                        {
                            var borrower = MapRowToBorrower(row, programId);
                            if (string.IsNullOrEmpty(borrower.BorrowerNumber))
                            {
                                failed++;
                                continue;
                            }
                            
                            var saved = await _borrowerRepository.CreateAsync(borrower);
                            borrowerCache[borrower.BorrowerNumber] = saved.Id;
                            created++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 차주 생성 실패: {ex.Message}");
                            failed++;
                        }
                    }
                    break;

                case SheetType.BorrowerRestructuring:
                    // Sheet A-1: 회생차주정보 -> borrower_restructuring 테이블
                    foreach (var row in data)
                    {
                        processed++;
                        UpdateProgress(processed, totalRows, sheet.Name);
                        
                        try
                        {
                            // 차주번호로 차주ID 조회
                            string? borrowerNumber = null;
                            foreach (var kvp in row)
                            {
                                var colName = kvp.Key?.ToLower() ?? "";
                                if (colName.Contains("차주일련번호") || colName.Contains("차주번호"))
                                {
                                    borrowerNumber = kvp.Value?.ToString();
                                    break;
                                }
                            }

                            Guid? borrowerId = null;
                            if (!string.IsNullOrEmpty(borrowerNumber))
                            {
                                var existingBorrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                                borrowerId = existingBorrower?.Id;
                            }

                            var restructuring = MapRowToBorrowerRestructuring(row, borrowerId);
                            await _borrowerRestructuringRepository.CreateAsync(restructuring);
                            created++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 회생정보 생성 실패: {ex.Message}");
                            failed++;
                        }
                    }
                    break;

                case SheetType.Loan:
                    // Sheet B: 채권정보 -> loans 테이블
                    foreach (var row in data)
                    {
                        processed++;
                        UpdateProgress(processed, totalRows, sheet.Name);
                        
                        try
                        {
                            // 차주번호로 차주ID 조회
                            string? borrowerNumber = null;
                            foreach (var kvp in row)
                            {
                                var colName = kvp.Key?.ToLower() ?? "";
                                if (colName.Contains("차주일련번호") || colName.Contains("차주번호"))
                                {
                                    borrowerNumber = kvp.Value?.ToString();
                                    break;
                                }
                            }

                            Guid? borrowerId = null;
                            if (!string.IsNullOrEmpty(borrowerNumber))
                            {
                                var existingBorrower = await _borrowerRepository.GetByBorrowerNumberAsync(borrowerNumber);
                                borrowerId = existingBorrower?.Id;
                            }

                            var loan = MapRowToLoan(row, borrowerId);
                            await _loanRepository.CreateAsync(loan);
                            created++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 대출 생성 실패: {ex.Message}");
                            failed++;
                        }
                    }
                    break;

                case SheetType.Property:
                    // Sheet C-1: 담보물건정보 -> properties 테이블 + 권리분석 데이터
                    // SheetMappingConfig 사용하여 중앙화된 매핑 적용
                    var mappingRules = SheetMappingConfig.GetMappingRules(SheetType.Property);
                    int rowIndex = 0;
                    foreach (var row in data)
                    {
                        rowIndex++;
                        processed++;
                        UpdateProgress(processed, totalRows, sheet.Name);
                        
                        try
                        {
                            // 중앙화된 매핑 규칙 사용 (권리분석 데이터 포함)
                            var (property, rightData) = MapRowToPropertyWithRules(row, columns, mappingRules, programId);
                            if (property == null)
                            {
                                failed++;
                                continue;
                            }
                            
                            // 물건번호 설정 - 우선순위: 경매사건번호(PropertyNumber) > 물건번호(CollateralNumber) > 자동생성
                            string finalPropertyNumber;
                            if (!string.IsNullOrEmpty(property.PropertyNumber) && 
                                !property.PropertyNumber.All(char.IsDigit))
                            {
                                // 경매사건번호(IBK)가 있으면 사용
                                finalPropertyNumber = property.PropertyNumber;
                            }
                            else if (!string.IsNullOrEmpty(property.CollateralNumber))
                            {
                                // 물건번호가 있으면 사용
                                finalPropertyNumber = property.CollateralNumber;
                            }
                            else
                            {
                                // 없으면 자동생성
                                var prefix = property.BorrowerNumber ?? sheet.Name;
                                finalPropertyNumber = $"{prefix}-P{rowIndex}";
                            }
                            
                            property.PropertyNumber = finalPropertyNumber;
                            
                            // CreateAsync 반환값 사용 - DB에 저장된 실제 ID 사용
                            var createdProperty = await _propertyRepository.CreateAsync(property);
                            
                            // 권리분석 데이터 저장 (createdProperty 사용)
                            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] Property '{createdProperty.CollateralNumber}' - rightData.Count = {rightData.Count}");
                            if (rightData.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 권리분석 데이터 저장 시도: {string.Join(", ", rightData.Keys)}");
                                await SaveRightAnalysisDataAsync(createdProperty, rightData);
                            }
                            else
                            {
                                // rightData가 비어있더라도 빈 권리분석 레코드 생성 (나중에 수동 입력 가능)
                                System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 권리분석 데이터 없음, 빈 레코드 생성");
                                await SaveRightAnalysisDataAsync(createdProperty, rightData);
                            }
                            
                            created++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 물건 생성 실패 (행 {rowIndex}): {ex.Message}");
                            failed++;
                        }
                    }
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"[ProcessSheet] '{sheet.Name}'은 지원하지 않는 시트 타입입니다 ({sheet.SheetType})");
                    break;
            }
            
            System.Diagnostics.Debug.WriteLine($"[ProcessSheet] 결과: 생성={created}, 실패={failed}");

            return (created, updated, failed);
        }

        /// <summary>
        /// 매핑 규칙을 사용하여 Property 객체로 변환 (권리분석 데이터 포함)
        /// </summary>
        private (Property? Property, Dictionary<string, object?> RightData) MapRowToPropertyWithRules(Dictionary<string, object> row, List<string> columns, List<ColumnMappingRule> rules, string programId)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                ProjectId = programId,
                ProgramId = Guid.TryParse(programId, out var guid) ? guid : null,
                Status = "pending"
            };

            var addressParts = new Dictionary<string, string>();
            var rightData = new Dictionary<string, object?>();

            foreach (var col in columns)
            {
                // 컬럼명에서 줄바꿈 제거하여 매칭
                var normalizedCol = col.Replace("\n", " ").Replace("\r", "").Trim();
                var rule = SheetMappingConfig.FindMappingRule(rules, normalizedCol);
                if (rule == null) continue;

                var value = row.ContainsKey(col) ? rule.ConvertValue(row[col]) : null;

                switch (rule.DbColumnName)
                {
                    // ========== Property 기본 필드 ==========
                    case "borrower_number":
                        property.BorrowerNumber = value?.ToString();
                        break;
                    case "borrower_name":
                        property.DebtorName = value?.ToString();
                        break;
                    case "property_number":
                        property.PropertyNumber = value?.ToString();
                        break;
                    case "collateral_number":
                        property.CollateralNumber = value?.ToString();
                        break;
                    case "property_type":
                        property.PropertyType = NormalizePropertyType(value?.ToString());
                        break;
                    case "land_area":
                        if (value is decimal la) property.LandArea = la;
                        break;
                    case "building_area":
                        if (value is decimal ba) property.BuildingArea = ba;
                        break;
                    case "address_province":
                        addressParts["province"] = value?.ToString() ?? "";
                        break;
                    case "address_city":
                        addressParts["city"] = value?.ToString() ?? "";
                        break;
                    case "address_district":
                        addressParts["district"] = value?.ToString() ?? "";
                        break;
                    case "address_detail":
                        addressParts["detail"] = value?.ToString() ?? "";
                        break;

                    // ========== 권리분석: 선순위 정보 ==========
                    case "senior_small_deposit":
                        rightData["small_deposit_dd"] = value;
                        break;
                    case "senior_lease_deposit":
                        rightData["lease_deposit_dd"] = value;
                        break;
                    case "senior_wage_claim":
                        rightData["wage_claim_dd"] = value;
                        break;
                    case "senior_current_tax":
                        rightData["current_tax_dd"] = value;
                        break;
                    case "senior_tax_claim":
                        rightData["senior_tax_dd"] = value;
                        break;
                    case "senior_other":
                        rightData["etc_dd"] = value;
                        break;
                    case "senior_total":
                        rightData["senior_total_dd"] = value;
                        break;

                    // ========== 권리분석: 감정평가 정보 ==========
                    case "appraisal_type":
                        rightData["appraisal_type"] = value;
                        break;
                    case "appraisal_date":
                        rightData["appraisal_date"] = value;
                        break;
                    case "appraisal_agency":
                        rightData["appraisal_agency"] = value;
                        break;
                    case "appraisal_value":
                        rightData["appraisal_value"] = value;
                        property.AppraisalValue = value as decimal?;
                        break;

                    // ========== 권리분석: 경매 정보 ==========
                    case "auction_status":
                        rightData["auction_status"] = value;
                        break;
                    case "court_name":
                        rightData["court_name"] = value;
                        break;
                    case "auction_applicant_precedent":
                        rightData["auction_applicant"] = value;
                        break;
                    case "auction_start_date_precedent":
                        rightData["auction_start_date"] = value;
                        break;
                    case "case_number_precedent":
                        rightData["case_number"] = value;
                        if (!string.IsNullOrEmpty(value?.ToString()))
                            property.PropertyNumber = value?.ToString();
                        break;
                    case "claim_deadline_precedent":
                        rightData["claim_deadline"] = value;
                        break;
                    case "initial_appraisal_value":
                        rightData["initial_appraisal"] = value;
                        break;
                    case "final_auction_round":
                        rightData["auction_count"] = value;
                        break;
                    case "final_auction_date":
                        rightData["final_auction_date"] = value;
                        break;
                    case "next_auction_date":
                        rightData["next_auction_date"] = value;
                        break;
                    case "final_minimum_bid":
                        rightData["final_min_bid"] = value;
                        property.MinimumBid = value as decimal?;
                        break;
                }
            }

            // 주소 조합
            var addressComponents = new List<string>();
            if (addressParts.TryGetValue("province", out var prov) && !string.IsNullOrEmpty(prov))
                addressComponents.Add(prov);
            if (addressParts.TryGetValue("city", out var city) && !string.IsNullOrEmpty(city))
                addressComponents.Add(city);
            if (addressParts.TryGetValue("district", out var dist) && !string.IsNullOrEmpty(dist))
                addressComponents.Add(dist);
            if (addressParts.TryGetValue("detail", out var detail) && !string.IsNullOrEmpty(detail))
                addressComponents.Add(detail);

            if (addressComponents.Count > 0)
            {
                property.AddressFull = string.Join(" ", addressComponents);
            }

            return (property, rightData);
        }

        /// <summary>
        /// 권리분석 데이터 저장 (rightData가 비어있어도 레코드 생성)
        /// </summary>
        private async Task SaveRightAnalysisDataAsync(Property property, Dictionary<string, object?> rightData)
        {
            // 빈 데이터라도 right_analysis 레코드는 생성 (나중에 수동 입력 가능)

            try
            {
                var existing = await _rightAnalysisRepository.GetByPropertyIdAsync(property.Id);
                var rightAnalysis = existing ?? new RightAnalysis
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // DD 선순위 금액 설정
                if (rightData.TryGetValue("small_deposit_dd", out var smallDeposit) && smallDeposit is decimal sd)
                    rightAnalysis.SmallDepositDd = sd;
                if (rightData.TryGetValue("lease_deposit_dd", out var leaseDeposit) && leaseDeposit is decimal ld)
                    rightAnalysis.LeaseDepositDd = ld;
                if (rightData.TryGetValue("wage_claim_dd", out var wageClaim) && wageClaim is decimal wc)
                    rightAnalysis.WageClaimDd = wc;
                if (rightData.TryGetValue("current_tax_dd", out var currentTax) && currentTax is decimal ct)
                    rightAnalysis.CurrentTaxDd = ct;
                if (rightData.TryGetValue("senior_tax_dd", out var seniorTax) && seniorTax is decimal st)
                    rightAnalysis.SeniorTaxDd = st;
                if (rightData.TryGetValue("etc_dd", out var etcDd) && etcDd is decimal etc)
                    rightAnalysis.EtcDd = etc;

                // 감정평가 정보
                if (rightData.TryGetValue("appraisal_value", out var appraisalValue) && appraisalValue is decimal av)
                    rightAnalysis.AppraisalValue = av;
                if (rightData.TryGetValue("appraisal_date", out var appraisalDate) && appraisalDate is DateTime ad)
                    rightAnalysis.AppraisalDate = ad;

                // 경매 정보
                if (rightData.TryGetValue("court_name", out var courtName) && courtName != null)
                    rightAnalysis.CourtName = courtName.ToString();
                if (rightData.TryGetValue("case_number", out var caseNumber) && caseNumber != null)
                    rightAnalysis.CaseNumber = caseNumber.ToString();
                if (rightData.TryGetValue("auction_count", out var auctionCount) && auctionCount is int ac)
                    rightAnalysis.AuctionCount = ac;
                if (rightData.TryGetValue("final_min_bid", out var minBid) && minBid is decimal mb)
                    rightAnalysis.MinimumBid = mb;

                // 선순위 합계 자동 계산
                rightAnalysis.SeniorTotalDd = 
                    rightAnalysis.SmallDepositDd +
                    rightAnalysis.LeaseDepositDd +
                    rightAnalysis.WageClaimDd +
                    rightAnalysis.CurrentTaxDd +
                    rightAnalysis.SeniorTaxDd +
                    rightAnalysis.EtcDd;

                rightAnalysis.UpdatedAt = DateTime.UtcNow;

                await _rightAnalysisRepository.UpsertAsync(rightAnalysis);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveRightAnalysisDataAsync] 에러 - Property '{property.PropertyNumber}': {ex.Message}");
            }
        }

        /// <summary>
        /// 행 데이터를 Property 객체로 매핑
        /// </summary>
        private Property MapRowToProperty(Dictionary<string, object> row, string programId)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                ProjectId = programId,
                ProgramId = Guid.TryParse(programId, out var guid) ? guid : null,
                Status = "pending"
            };

            // 컬럼 이름으로 자동 매핑 시도
            foreach (var kvp in row)
            {
                var colName = kvp.Key?.ToLower() ?? "";
                var value = kvp.Value;

                if (value == null) continue;

                // 물건번호
                if (colName.Contains("물건번호") || colName.Contains("번호") || colName.Contains("propertynumber"))
                {
                    property.PropertyNumber = value.ToString();
                }
                // 물건유형
                else if (colName.Contains("유형") || colName.Contains("종류") || colName.Contains("propertytype") || colName.Contains("담보물형태"))
                {
                    property.PropertyType = NormalizePropertyType(value.ToString());
                }
                // 전체주소
                else if (colName.Contains("전체주소") || colName.Contains("addressfull") || colName.Contains("물건지"))
                {
                    property.AddressFull = value.ToString();
                }
                // 도로명주소
                else if (colName.Contains("도로명") || colName.Contains("addressroad"))
                {
                    property.AddressRoad = value.ToString();
                }
                // 지번주소
                else if (colName.Contains("지번") || colName.Contains("addressjibun"))
                {
                    property.AddressJibun = value.ToString();
                }
                // 토지면적
                else if (colName.Contains("토지") || colName.Contains("대지면적") || colName.Contains("landarea"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var landArea))
                        property.LandArea = landArea;
                }
                // 건물면적
                else if (colName.Contains("건물면적") || colName.Contains("buildingarea"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var buildingArea))
                        property.BuildingArea = buildingArea;
                }
                // 감정가
                else if (colName.Contains("감정") || colName.Contains("평가") || colName.Contains("appraisalvalue"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var appraisal))
                        property.AppraisalValue = appraisal;
                }
                // 최저입찰가
                else if (colName.Contains("최저") || colName.Contains("입찰") || colName.Contains("minimumbid"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var minimumBid))
                        property.MinimumBid = minimumBid;
                }
            }

            return property;
        }

        /// <summary>
        /// 행 데이터를 Borrower 객체로 매핑 (Sheet A용)
        /// </summary>
        private Borrower MapRowToBorrower(Dictionary<string, object> row, string programId)
        {
            var borrower = new Borrower
            {
                Id = Guid.NewGuid(),
                ProgramId = programId
            };

            foreach (var kvp in row)
            {
                var colName = kvp.Key?.ToLower().Replace("\n", " ").Replace("\r", "").Trim() ?? "";
                var value = kvp.Value;

                if (value == null) continue;

                // 차주번호
                if (colName.Contains("차주일련번호") || colName.Contains("차주번호"))
                {
                    borrower.BorrowerNumber = value.ToString() ?? "";
                }
                // 차주명
                else if (colName.Contains("차주명"))
                {
                    borrower.BorrowerName = value.ToString() ?? "";
                }
                // 차주형태
                else if (colName.Contains("차주형태") || colName.Contains("차주유형"))
                {
                    borrower.BorrowerType = value.ToString() ?? "";
                }
                // OPB (대출원금잔액)
                else if (colName.Contains("대출원금잔액") || colName.Contains("미상환원금"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var opb))
                        borrower.Opb = opb;
                }
                // 근저당설정액
                else if (colName.Contains("근저당") && colName.Contains("설정액"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var mortgage))
                        borrower.MortgageAmount = mortgage;
                }
            }

            return borrower;
        }

        /// <summary>
        /// 행 데이터를 Loan 객체로 매핑 (Sheet B용)
        /// </summary>
        private Loan MapRowToLoan(Dictionary<string, object> row, Guid? borrowerId)
        {
            var loan = new Loan
            {
                Id = Guid.NewGuid(),
                BorrowerId = borrowerId
            };

            foreach (var kvp in row)
            {
                var colName = kvp.Key?.ToLower().Replace("\n", " ").Replace("\r", "").Trim() ?? "";
                var value = kvp.Value;

                if (value == null) continue;

                // 대출일련번호
                if (colName.Contains("대출일련번호"))
                {
                    loan.AccountSerial = value.ToString();
                }
                // 대출과목
                else if (colName.Contains("대출과목"))
                {
                    loan.LoanType = value.ToString();
                }
                // 계좌번호
                else if (colName.Contains("계좌번호"))
                {
                    loan.AccountNumber = value.ToString();
                }
                // 이자율
                else if (colName.Contains("이자율"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", "").Replace("%", ""), out var rate))
                        loan.NormalInterestRate = rate;
                }
                // 최초대출일
                else if (colName.Contains("최초대출일"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        loan.InitialLoanDate = date;
                }
                // 최종이수일
                else if (colName.Contains("최종이수일"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        loan.LastInterestDate = date;
                }
                // 최초대출금액
                else if (colName.Contains("최초대출") && (colName.Contains("금액") || colName.Contains("원금")))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var amount))
                        loan.InitialLoanAmount = amount;
                }
                // 대출금잔액
                else if (colName.Contains("대출") && colName.Contains("잔액"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var balance))
                        loan.LoanPrincipalBalance = balance;
                }
                // 미수이자
                else if (colName.Contains("미수이자"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var interest))
                        loan.AccruedInterest = interest;
                }
            }

            return loan;
        }

        /// <summary>
        /// 행 데이터를 BorrowerRestructuring 객체로 매핑 (Sheet A-1용)
        /// </summary>
        private BorrowerRestructuring MapRowToBorrowerRestructuring(Dictionary<string, object> row, Guid? borrowerId)
        {
            var restructuring = new BorrowerRestructuring
            {
                Id = Guid.NewGuid(),
                BorrowerId = borrowerId ?? Guid.Empty
            };

            foreach (var kvp in row)
            {
                var colName = kvp.Key?.ToLower().Replace("\n", " ").Replace("\r", "").Trim() ?? "";
                var value = kvp.Value;

                if (value == null) continue;

                // 인가/미인가
                if (colName.Contains("인가") || colName.Contains("미인가"))
                {
                    restructuring.ApprovalStatus = value.ToString();
                }
                // 세부진행단계
                else if (colName.Contains("진행단계") || colName.Contains("세부"))
                {
                    restructuring.ProgressStage = value.ToString();
                }
                // 관할법원
                else if (colName.Contains("관할법원") || colName.Contains("법원"))
                {
                    restructuring.CourtName = value.ToString();
                }
                // 회생사건번호
                else if (colName.Contains("사건번호"))
                {
                    restructuring.CaseNumber = value.ToString();
                }
                // 회생신청일
                else if (colName.Contains("회생신청일") || colName.Contains("신청일"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        restructuring.FilingDate = date;
                }
                // 보전처분일
                else if (colName.Contains("보전처분"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        restructuring.PreservationDate = date;
                }
                // 개시결정일
                else if (colName.Contains("개시결정"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        restructuring.CommencementDate = date;
                }
                // 채권신고일
                else if (colName.Contains("채권신고"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        restructuring.ClaimFilingDate = date;
                }
            }

            return restructuring;
        }

        // ========== Interim 데이터 처리 ==========

        /// <summary>
        /// Interim 파일 처리
        /// </summary>
        private async Task<(int AdvanceCreated, int CollectionCreated, int Failed)> ProcessInterimFileAsync(string filePath, Guid programId)
        {
            int advanceCreated = 0;
            int collectionCreated = 0;
            int failed = 0;

            try
            {
                // 시트 목록 가져오기
                var sheets = _excelService.GetSheetNames(filePath);

                foreach (var sheet in sheets)
                {
                    var sheetName = sheet.Name.ToLower();
                    
                    // 시트명으로 타입 감지
                    bool isAdvanceSheet = sheetName.Contains("가지급");
                    bool isCollectionSheet = sheetName.Contains("회수");

                    if (!isAdvanceSheet && !isCollectionSheet)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Interim] '{sheet.Name}' 시트는 Interim 타입이 아닙니다. 건너뜀.");
                        continue;
                    }

                    // 헤더 행 감지 및 데이터 읽기
                    var headerRow = _excelService.DetectHeaderRow(filePath, sheet.Name);
                    var (columns, data) = await _excelService.ReadExcelSheetAsync(filePath, sheet.Name, headerRow);

                    System.Diagnostics.Debug.WriteLine($"[Interim] '{sheet.Name}' 시트: {data.Count}행, 헤더행={headerRow}");
                    System.Diagnostics.Debug.WriteLine($"[Interim] 컬럼: {string.Join(", ", columns.Take(15))}");

                    if (data.Count == 0) continue;

                    if (isAdvanceSheet)
                    {
                        // 추가 가지급금 처리
                        var advances = new List<InterimAdvance>();
                        foreach (var row in data)
                        {
                            var advance = MapRowToInterimAdvance(row, programId);
                            if (advance != null && !string.IsNullOrEmpty(advance.BorrowerNumber))
                            {
                                advances.Add(advance);
                            }
                        }

                        if (advances.Count > 0)
                        {
                            var (created, failedCount) = await _interimRepository.CreateAdvancesBatchAsync(advances);
                            advanceCreated += created;
                            failed += failedCount;
                            System.Diagnostics.Debug.WriteLine($"[Interim] 가지급금 저장 완료: {created}건 성공, {failedCount}건 실패");
                        }
                    }
                    else if (isCollectionSheet)
                    {
                        // 회수정보 처리
                        var collections = new List<InterimCollection>();
                        foreach (var row in data)
                        {
                            var collection = MapRowToInterimCollection(row, programId);
                            if (collection != null && !string.IsNullOrEmpty(collection.BorrowerNumber))
                            {
                                collections.Add(collection);
                            }
                        }

                        if (collections.Count > 0)
                        {
                            var (created, failedCount) = await _interimRepository.CreateCollectionsBatchAsync(collections);
                            collectionCreated += created;
                            failed += failedCount;
                            System.Diagnostics.Debug.WriteLine($"[Interim] 회수정보 저장 완료: {created}건 성공, {failedCount}건 실패");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Interim] 처리 실패: {ex.Message}");
                failed++;
            }

            return (advanceCreated, collectionCreated, failed);
        }

        /// <summary>
        /// 행 데이터를 InterimAdvance 객체로 매핑
        /// </summary>
        private InterimAdvance? MapRowToInterimAdvance(Dictionary<string, object> row, Guid programId)
        {
            var advance = new InterimAdvance
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                Currency = "KRW",
                UploadedBy = CurrentUserId
            };

            foreach (var kvp in row)
            {
                var colName = kvp.Key?.ToLower().Replace("\n", " ").Replace("\r", "").Trim() ?? "";
                var value = kvp.Value;

                if (value == null || string.IsNullOrWhiteSpace(value.ToString())) continue;

                // Pool
                if (colName == "pool" || colName.Contains("pool"))
                {
                    advance.Pool = value.ToString();
                }
                // 채권구분
                else if (colName.Contains("채권구분"))
                {
                    advance.LoanType = value.ToString();
                }
                // 차주일련번호
                else if (colName.Contains("차주일련번호") || colName.Contains("차주번호"))
                {
                    advance.BorrowerNumber = value.ToString();
                }
                // 차주명
                else if (colName.Contains("차주명"))
                {
                    advance.BorrowerName = value.ToString();
                }
                // 계좌일련번호
                else if (colName.Contains("계좌일련번호"))
                {
                    advance.AccountSerial = value.ToString();
                }
                // 계좌번호
                else if (colName.Contains("계좌번호"))
                {
                    advance.AccountNumber = value.ToString();
                }
                // 가지급비용종류
                else if (colName.Contains("가지급비용종류") || colName.Contains("비용종류"))
                {
                    advance.ExpenseType = value.ToString();
                }
                // 거래일자
                else if (colName.Contains("거래일자"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        advance.TransactionDate = date;
                }
                // 통화 (잔액통화)
                else if (colName.Contains("통화") || colName.Contains("잔액"))
                {
                    var currency = value.ToString();
                    if (!string.IsNullOrEmpty(currency))
                        advance.Currency = currency;
                }
                // 지급거래금액
                else if (colName.Contains("지급거래금액") || colName.Contains("금액"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var amount))
                        advance.Amount = amount;
                }
                // 적요
                else if (colName.Contains("적요"))
                {
                    advance.Description = value.ToString();
                }
                // 비고
                else if (colName.Contains("비고"))
                {
                    advance.Notes = value.ToString();
                }
            }

            return advance;
        }

        /// <summary>
        /// 행 데이터를 InterimCollection 객체로 매핑
        /// </summary>
        private InterimCollection? MapRowToInterimCollection(Dictionary<string, object> row, Guid programId)
        {
            var collection = new InterimCollection
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                UploadedBy = CurrentUserId
            };

            foreach (var kvp in row)
            {
                var colName = kvp.Key?.ToLower().Replace("\n", " ").Replace("\r", "").Trim() ?? "";
                var value = kvp.Value;

                if (value == null || string.IsNullOrWhiteSpace(value.ToString())) continue;

                // Pool
                if (colName == "pool" || colName.Contains("pool"))
                {
                    collection.Pool = value.ToString();
                }
                // 채권구분
                else if (colName.Contains("채권구분"))
                {
                    collection.LoanType = value.ToString();
                }
                // 차주일련번호
                else if (colName.Contains("차주일련번호") || colName.Contains("차주번호"))
                {
                    collection.BorrowerNumber = value.ToString();
                }
                // 차주명
                else if (colName.Contains("차주명"))
                {
                    collection.BorrowerName = value.ToString();
                }
                // 계좌일련번호
                else if (colName.Contains("계좌일련번호"))
                {
                    collection.AccountSerial = value.ToString();
                }
                // 계좌번호
                else if (colName.Contains("계좌번호"))
                {
                    collection.AccountNumber = value.ToString();
                }
                // 회수일자
                else if (colName.Contains("회수일자"))
                {
                    if (DateTime.TryParse(value.ToString(), out var date))
                        collection.CollectionDate = date;
                }
                // 회수원금
                else if (colName.Contains("회수원금"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var amount))
                        collection.PrincipalAmount = amount;
                }
                // 회수가지급금
                else if (colName.Contains("회수가지급금"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var amount))
                        collection.AdvanceAmount = amount;
                }
                // 회수이자
                else if (colName.Contains("회수이자"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var amount))
                        collection.InterestAmount = amount;
                }
                // 총회수액
                else if (colName.Contains("총회수액"))
                {
                    if (decimal.TryParse(value.ToString()?.Replace(",", ""), out var amount))
                        collection.TotalAmount = amount;
                }
                // 비고
                else if (colName.Contains("비고"))
                {
                    collection.Notes = value.ToString();
                }
            }

            return collection;
        }

        /// <summary>
        /// 컬럼 매핑 다이얼로그 열기
        /// </summary>
        public void OpenColumnMappingDialog(SelectableSheetInfo sheet)
        {
            if (sheet == null) return;

            try
            {
                // Headers가 비어있으면 헤더 행을 감지해서 다시 읽어옴
                var headers = sheet.Headers;
                if ((headers == null || headers.Count == 0) && !string.IsNullOrEmpty(DataDiskFilePath))
                {
                    // HeaderRow가 설정되어 있으면 사용, 아니면 감지
                    var headerRow = sheet.HeaderRow > 0 ? sheet.HeaderRow : _excelService.DetectHeaderRow(DataDiskFilePath, sheet.Name);
                    
                    // Task.Run을 사용해서 UI 스레드 데드락 방지
                    var (columns, _) = Task.Run(async () => 
                        await _excelService.ReadExcelSheetAsync(DataDiskFilePath, sheet.Name, headerRow)).GetAwaiter().GetResult();
                    headers = columns.ToList();
                    sheet.Headers = headers; // 캐시
                    System.Diagnostics.Debug.WriteLine($"[OpenColumnMappingDialog] 헤더 재로드: {headers.Count}개 컬럼 감지됨 (헤더행: {headerRow})");
                }

                // SheetMappingInfo로 변환
                var sheetMappingInfo = new SheetMappingInfo
                {
                    ExcelSheetName = sheet.Name,
                    SheetIndex = sheet.Index,
                    DetectedType = ConvertToDataDiskSheetType(sheet.SheetType),
                    SelectedType = ConvertToDataDiskSheetType(sheet.SheetType),
                    Headers = headers ?? new List<string>(),
                    RowCount = sheet.RowCount,
                    IsSelected = sheet.IsSelected
                };

                // 기존 컬럼 매핑이 있는지 확인
                var existingInfo = _sheetMappingInfoList.FirstOrDefault(m => m.ExcelSheetName == sheet.Name);
                if (existingInfo != null)
                {
                    sheetMappingInfo.ColumnMappings = existingInfo.ColumnMappings;
                }

                // 다이얼로그 열기
                var dialog = new ColumnMappingDialog(_uploadService)
                {
                    Owner = Application.Current.MainWindow
                };
                dialog.SetSheetInfo(sheetMappingInfo);

                if (dialog.ShowDialog() == true)
                {
                    // 결과 저장
                    sheetMappingInfo.ColumnMappings = dialog.ResultMappings;

                    // 기존 목록에서 업데이트 또는 추가
                    var existingIndex = _sheetMappingInfoList.FindIndex(m => m.ExcelSheetName == sheet.Name);
                    if (existingIndex >= 0)
                    {
                        _sheetMappingInfoList[existingIndex] = sheetMappingInfo;
                    }
                    else
                    {
                        _sheetMappingInfoList.Add(sheetMappingInfo);
                    }

                    System.Diagnostics.Debug.WriteLine($"[OpenColumnMappingDialog] 시트 '{sheet.Name}' 컬럼 매핑 저장: {dialog.ResultMappings.Count(m => m.IsMapped)}개 매핑됨");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenColumnMappingDialog] 에러: {ex.Message}");
                DataDiskErrorMessage = $"컬럼 매핑 설정 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 제목행(헤더행) 확인 다이얼로그 열기
        /// 피드백 6번: 차주 일반 정보, 회생 차주 정보, 인테림 제목행 확인 기능
        /// </summary>
        public void OpenHeaderRowPreviewDialog(SelectableSheetInfo sheet)
        {
            if (sheet == null) return;

            try
            {
                string filePath = DataDiskFilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    DataDiskErrorMessage = "파일이 선택되지 않았습니다.";
                    return;
                }

                // 미리보기 데이터 가져오기 (처음 10행)
                var previewRows = _excelService.GetSheetPreviewRows(filePath, sheet.Name, 10);
                var totalRows = _excelService.GetSheetTotalRowCount(filePath, sheet.Name);

                if (previewRows.Count == 0)
                {
                    DataDiskErrorMessage = "시트 데이터를 읽을 수 없습니다.";
                    return;
                }

                // 현재 헤더행 (없으면 감지)
                int currentHeaderRow = sheet.HeaderRow > 0 ? sheet.HeaderRow : _excelService.DetectHeaderRow(filePath, sheet.Name);

                // 다이얼로그 열기
                var dialog = new HeaderRowPreviewDialog
                {
                    Owner = Application.Current.MainWindow
                };
                dialog.SetPreviewData(sheet.Name, previewRows, currentHeaderRow, totalRows);

                if (dialog.ShowDialog() == true)
                {
                    // 선택된 헤더행 저장
                    int selectedRow = dialog.SelectedHeaderRow;
                    sheet.HeaderRow = selectedRow;
                    
                    // 헤더 다시 읽기
                    var (columns, _) = Task.Run(async () => 
                        await _excelService.ReadExcelSheetAsync(filePath, sheet.Name, selectedRow)).GetAwaiter().GetResult();
                    sheet.Headers = columns.ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[OpenHeaderRowPreviewDialog] 시트 '{sheet.Name}' 헤더행 변경: {selectedRow}행, 컬럼 {columns.Count}개");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenHeaderRowPreviewDialog] 에러: {ex.Message}");
                DataDiskErrorMessage = $"제목행 확인 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// SheetType을 DataDiskSheetType으로 변환
        /// </summary>
        private DataDiskSheetType ConvertToDataDiskSheetType(SheetType sheetType)
        {
            return sheetType switch
            {
                SheetType.BorrowerGeneral => DataDiskSheetType.BorrowerGeneral,
                SheetType.BorrowerRestructuring => DataDiskSheetType.BorrowerRestructuring,
                SheetType.Loan => DataDiskSheetType.Loan,
                SheetType.Property => DataDiskSheetType.Property,
                SheetType.RegistryDetail => DataDiskSheetType.RegistryDetail,
                SheetType.CollateralSetting => DataDiskSheetType.CollateralSetting,
                SheetType.Guarantee => DataDiskSheetType.Guarantee,
                _ => DataDiskSheetType.Unknown
            };
        }

        /// <summary>
        /// 물건종류(담보종류) 값 정규화
        /// 피드백 9번: 물건종류 인식 오류 수정
        /// </summary>
        private static string NormalizePropertyType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "기타";

            // 공백 제거 및 소문자 변환
            var normalized = value.Trim().ToLower();

            // 알려진 유형으로 매핑
            return normalized switch
            {
                "아파트" or "apartment" or "apt" => "아파트",
                "상가" or "store" or "commercial" or "상업" or "근생" or "근린상가" or "근린생활시설" => "상가",
                "토지" or "land" or "대지" or "임야" or "전" or "답" or "잡종지" => "토지",
                "빌라" or "villa" or "연립" or "연립주택" or "다세대" or "다세대주택" => "빌라",
                "오피스텔" or "officetel" or "오피" => "오피스텔",
                "단독주택" or "house" or "단독" or "주택" => "단독주택",
                "다가구주택" or "multi-family" or "다가구" => "다가구주택",
                "공장" or "factory" or "창고" or "warehouse" or "공장창고" => "공장",
                _ => string.IsNullOrWhiteSpace(value) ? "기타" : value.Trim() // 알 수 없는 값은 원본 유지
            };
        }

        /// <summary>
        /// 시트 타입 변경
        /// </summary>
        public void ChangeSheetType(SelectableSheetInfo sheet, DataDiskSheetType newType)
        {
            if (sheet == null) return;

            // SelectableSheetInfo의 SheetType 변경
            sheet.SheetType = newType switch
            {
                DataDiskSheetType.BorrowerGeneral => SheetType.BorrowerGeneral,
                DataDiskSheetType.BorrowerRestructuring => SheetType.BorrowerRestructuring,
                DataDiskSheetType.Loan => SheetType.Loan,
                DataDiskSheetType.Property => SheetType.Property,
                DataDiskSheetType.RegistryDetail => SheetType.RegistryDetail,
                DataDiskSheetType.CollateralSetting => SheetType.CollateralSetting,
                DataDiskSheetType.Guarantee => SheetType.Guarantee,
                _ => SheetType.Unknown
            };

            // _sheetMappingInfoList도 업데이트
            var existingInfo = _sheetMappingInfoList.FirstOrDefault(m => m.ExcelSheetName == sheet.Name);
            if (existingInfo != null)
            {
                existingInfo.SelectedType = newType;
                // 시트 타입 변경 시 컬럼 매핑 초기화 (새 시트 타입의 기본 매핑 적용)
                existingInfo.ColumnMappings = _uploadService.GetDefaultColumnMappings(newType, existingInfo.Headers);
            }

            System.Diagnostics.Debug.WriteLine($"[ChangeSheetType] 시트 '{sheet.Name}' 타입 변경: {newType}");
        }
    }

    /// <summary>
    /// 시트 타입 옵션 (드롭다운용)
    /// </summary>
    public class SheetTypeOption
    {
        public DataDiskSheetType Value { get; set; }
        public string DisplayName { get; set; } = "";
    }
}

