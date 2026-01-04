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
        private readonly AuthService _authService;
        private readonly ExcelService _excelService;

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
            PropertyRepository propertyRepository,
            AuthService authService,
            ExcelService excelService)
        {
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
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
            ClearDataDiskFile();
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

                // Excel 파일 읽기
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
            catch (Exception ex)
            {
                DataDiskErrorMessage = $"Excel 파일 읽기 실패: {ex.Message}";
                HasDataDiskFile = false;
            }
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

            if (_dataDiskData == null || _dataDiskData.Count == 0)
            {
                DataDiskErrorMessage = "업로드할 데이터가 없습니다.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                DataDiskErrorMessage = null;

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

                // 2. 물건 데이터 생성
                int createdCount = 0;
                int errorCount = 0;

                foreach (var row in _dataDiskData)
                {
                    try
                    {
                        var property = MapRowToProperty(row, savedProgram.Id.ToString());
                        await _propertyRepository.CreateAsync(property);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"물건 생성 실패: {ex.Message}");
                        errorCount++;
                    }
                }

                // 완료 메시지
                var message = $"프로그램이 등록되었습니다.\n총 {createdCount}개의 물건이 생성되었습니다.";
                if (errorCount > 0)
                {
                    message += $"\n({errorCount}개 실패)";
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
                    property.PropertyType = value.ToString();
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
    }
}

