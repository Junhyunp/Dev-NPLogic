using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 프로그램별 요약 정보 모델
    /// </summary>
    public class ProgramSummary : ObservableObject
    {
        public string ProjectId { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int ProgressPercent => TotalCount > 0 ? (int)Math.Round((double)CompletedCount / TotalCount * 100) : 0;
    }

    /// <summary>
    /// 컬럼별 진행률 정보 모델
    /// </summary>
    public partial class ColumnProgressInfo : ObservableObject
    {
        [ObservableProperty]
        private int _agreementDocPercent;

        [ObservableProperty]
        private int _guaranteeDocPercent;

        [ObservableProperty]
        private int _auctionDocsPercent;

        [ObservableProperty]
        private int _tenantDocsPercent;

        [ObservableProperty]
        private int _seniorRightsPercent;

        [ObservableProperty]
        private int _appraisalConfirmedPercent;

        [ObservableProperty]
        private int _overallPercent;
    }

    /// <summary>
    /// 대시보드 ViewModel - Phase 2 업데이트
    /// 2영역 레이아웃, 프로그램별 진행률, 컬럼별 진행률 지원
    /// Phase 6: 권한별 화면 필터링 추가
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;
        private readonly ProgramUserRepository _programUserRepository;
        
        // PM 담당 프로그램 ID 목록 (캐시)
        private List<Guid> _pmProgramIds = new();

        // ========== 사용자 정보 ==========
        [ObservableProperty]
        private User? _currentUser;

        // ========== 프로그램 목록 (좌측 패널) ==========
        [ObservableProperty]
        private ObservableCollection<ProgramSummary> _programSummaries = new();

        [ObservableProperty]
        private ProgramSummary? _selectedProgram;

        // ========== 필터 ==========
        [ObservableProperty]
        private string _selectedProjectId = "";

        [ObservableProperty]
        private ObservableCollection<string> _projects = new();

        [ObservableProperty]
        private string? _statusFilter;

        // ========== 탭 상태 ==========
        [ObservableProperty]
        private string _activeTab = "home";

        // ========== 통계 ==========
        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _pendingCount;

        [ObservableProperty]
        private int _processingCount;

        [ObservableProperty]
        private int _completedCount;

        [ObservableProperty]
        private int _overallProgressPercent;

        // ========== 컬럼별 진행률 (Phase 2.6) ==========
        [ObservableProperty]
        private ColumnProgressInfo _columnProgress = new();

        // ========== 데이터 ==========
        [ObservableProperty]
        private ObservableCollection<Property> _recentProperties = new();

        [ObservableProperty]
        private ObservableCollection<Property> _dashboardProperties = new();

        [ObservableProperty]
        private ObservableCollection<User> _evaluators = new();

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // ========== 역할별 뷰 표시 여부 ==========
        public bool IsPMView => CurrentUser?.IsPM == true || CurrentUser?.IsAdmin == true;
        public bool IsEvaluatorView => CurrentUser?.IsEvaluator == true;
        public bool IsAdminView => CurrentUser?.IsAdmin == true;
        
        public string RoleBadgeText => CurrentUser?.Role?.ToUpper() switch
        {
            "PM" => "PM",
            "EVALUATOR" => "평가자",
            "ADMIN" => "관리자",
            _ => ""
        };

        public DashboardViewModel(
            PropertyRepository propertyRepository,
            UserRepository userRepository,
            AuthService authService,
            ProgramUserRepository programUserRepository)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
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

                // PM인 경우 담당 프로그램 ID 목록 로드 (Phase 6.2)
                await LoadPMProgramIdsAsync();

                // 평가자 목록 로드 (PM/Admin용)
                await LoadEvaluatorsAsync();

                // 프로그램 목록 로드 (좌측 패널)
                await LoadProgramSummariesAsync();

                // 첫 번째 프로그램 자동 선택
                if (ProgramSummaries.Count > 0)
                {
                    SelectedProgram = ProgramSummaries[0];
                    await LoadSelectedProgramDataAsync();
                }
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
        /// PM인 경우 담당 프로그램 ID 목록 로드 (Phase 6.2)
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
                    // PM 프로그램 로드 실패 시 빈 목록 유지 (전체 접근 불가)
                }
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
                OnPropertyChanged(nameof(IsPMView));
                OnPropertyChanged(nameof(IsEvaluatorView));
                OnPropertyChanged(nameof(IsAdminView));
                OnPropertyChanged(nameof(RoleBadgeText));
            }
        }

        /// <summary>
        /// 평가자 목록 로드
        /// </summary>
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
        /// 프로그램(프로젝트) 요약 목록 로드 (Phase 2.2 - 좌측 패널)
        /// Phase 6: 권한별 필터링 추가
        /// - 관리자(Admin): 전체 보임
        /// - PM: 담당 프로그램만 보임
        /// - 평가자(Evaluator): 자기에게 할당된 물건만 보임
        /// </summary>
        private async Task LoadProgramSummariesAsync()
        {
            try
            {
                var allProperties = await _propertyRepository.GetAllAsync();
                
                // Phase 6: 권한별 필터링
                IEnumerable<Property> filteredProperties = allProperties
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProjectId));

                if (CurrentUser?.IsAdmin == true)
                {
                    // 관리자: 전체 보임 (필터링 없음)
                }
                else if (CurrentUser?.IsPM == true)
                {
                    // PM: 담당 프로그램만 보임
                    if (_pmProgramIds.Count > 0)
                    {
                        filteredProperties = filteredProperties
                            .Where(p => p.ProgramId.HasValue && _pmProgramIds.Contains(p.ProgramId.Value));
                    }
                    else
                    {
                        // PM이지만 담당 프로그램이 없으면 빈 목록
                        filteredProperties = Enumerable.Empty<Property>();
                    }
                }
                else if (CurrentUser?.IsEvaluator == true)
                {
                    // 평가자: 자기에게 할당된 물건만 보임
                    filteredProperties = filteredProperties
                        .Where(p => p.AssignedTo == CurrentUser.Id);
                }

                // 프로젝트별로 그룹화
                var projectGroups = filteredProperties
                    .GroupBy(p => p.ProjectId)
                    .OrderBy(g => g.Key);

                ProgramSummaries.Clear();

                foreach (var group in projectGroups)
                {
                    var properties = group.ToList();
                    var completedCount = properties.Count(p => p.Status == "completed");

                    ProgramSummaries.Add(new ProgramSummary
                    {
                        ProjectId = group.Key!,
                        ProjectName = group.Key!,
                        TotalCount = properties.Count,
                        CompletedCount = completedCount
                    });
                }

                // 프로젝트 드롭다운도 업데이트
                Projects.Clear();
                Projects.Add("전체");
                foreach (var summary in ProgramSummaries)
                {
                    Projects.Add(summary.ProjectId);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로그램 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 선택된 프로그램의 데이터 로드 (code-behind에서 호출)
        /// </summary>
        public void LoadSelectedProgramData()
        {
            _ = LoadSelectedProgramDataAsync();
        }

        /// <summary>
        /// 선택된 프로그램의 데이터 로드 (비동기)
        /// </summary>
        private async Task LoadSelectedProgramDataAsync()
        {
            if (SelectedProgram == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                SelectedProjectId = SelectedProgram.ProjectId;

                // 통계 로드
                await LoadStatisticsAsync();

                // 대시보드 물건 목록 로드
                await LoadDashboardPropertiesAsync();

                // 컬럼별 진행률 계산
                RecalculateColumnProgress();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로그램 데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 통계 로드
        /// Phase 6: 권한별 필터링 추가
        /// </summary>
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;
                
                Guid? assignedTo = null;
                if (CurrentUser?.IsEvaluator == true)
                {
                    assignedTo = CurrentUser.Id;
                }

                var stats = await _propertyRepository.GetStatisticsAsync(projectId, assignedTo);
                
                // Phase 6: PM 권한인 경우 담당 프로그램만 통계 계산
                if (CurrentUser?.IsPM == true)
                {
                    var allProperties = await _propertyRepository.GetFilteredAsync(projectId: projectId, assignedTo: assignedTo);
                    var pmFilteredProperties = _pmProgramIds.Count > 0
                        ? allProperties.Where(p => p.ProgramId.HasValue && _pmProgramIds.Contains(p.ProgramId.Value)).ToList()
                        : new List<Property>();

                    TotalCount = pmFilteredProperties.Count;
                    PendingCount = pmFilteredProperties.Count(x => x.Status == "pending");
                    ProcessingCount = pmFilteredProperties.Count(x => x.Status == "processing");
                    CompletedCount = pmFilteredProperties.Count(x => x.Status == "completed");
                }
                else
                {
                    TotalCount = stats.TotalCount;
                    PendingCount = stats.PendingCount;
                    ProcessingCount = stats.ProcessingCount;
                    CompletedCount = stats.CompletedCount;
                }

                // 진행률 계산
                if (TotalCount > 0)
                {
                    OverallProgressPercent = (int)Math.Round((double)CompletedCount / TotalCount * 100);
                }
                else
                {
                    OverallProgressPercent = 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"통계 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 대시보드 물건 목록 로드
        /// Phase 6: 권한별 필터링 추가
        /// </summary>
        private async Task LoadDashboardPropertiesAsync()
        {
            try
            {
                var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;
                
                Guid? assignedTo = null;
                if (CurrentUser?.IsEvaluator == true)
                {
                    // 평가자: 자기에게 할당된 물건만
                    assignedTo = CurrentUser.Id;
                }

                var (properties, _) = await _propertyRepository.GetPagedAsync(
                    page: 1,
                    pageSize: 100,
                    projectId: projectId,
                    status: StatusFilter,
                    assignedTo: assignedTo
                );

                // Phase 6: PM 권한 추가 필터링 (담당 프로그램만)
                IEnumerable<Property> filteredProperties = properties;
                if (CurrentUser?.IsPM == true && _pmProgramIds.Count > 0)
                {
                    filteredProperties = properties
                        .Where(p => p.ProgramId.HasValue && _pmProgramIds.Contains(p.ProgramId.Value));
                }
                else if (CurrentUser?.IsPM == true && _pmProgramIds.Count == 0)
                {
                    // PM이지만 담당 프로그램이 없으면 빈 목록
                    filteredProperties = Enumerable.Empty<Property>();
                }

                DashboardProperties.Clear();
                RecentProperties.Clear();

                foreach (var property in filteredProperties)
                {
                    DashboardProperties.Add(property);
                    if (RecentProperties.Count < 10)
                    {
                        RecentProperties.Add(property);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 컬럼별 진행률 재계산 (Phase 2.6)
        /// </summary>
        public void RecalculateColumnProgress()
        {
            if (DashboardProperties.Count == 0)
            {
                ColumnProgress = new ColumnProgressInfo();
                return;
            }

            var total = DashboardProperties.Count;

            ColumnProgress.AgreementDocPercent = (int)Math.Round(DashboardProperties.Count(p => p.AgreementDoc) * 100.0 / total);
            ColumnProgress.GuaranteeDocPercent = (int)Math.Round(DashboardProperties.Count(p => p.GuaranteeDoc) * 100.0 / total);
            ColumnProgress.AuctionDocsPercent = (int)Math.Round(DashboardProperties.Count(p => p.AuctionDocs) * 100.0 / total);
            ColumnProgress.TenantDocsPercent = (int)Math.Round(DashboardProperties.Count(p => p.TenantDocs) * 100.0 / total);
            ColumnProgress.SeniorRightsPercent = (int)Math.Round(DashboardProperties.Count(p => p.SeniorRightsReview) * 100.0 / total);
            ColumnProgress.AppraisalConfirmedPercent = (int)Math.Round(DashboardProperties.Count(p => p.AppraisalConfirmed) * 100.0 / total);
            ColumnProgress.OverallPercent = (int)Math.Round(DashboardProperties.Count(p => p.Status == "completed") * 100.0 / total);
        }

        /// <summary>
        /// 활성 탭 설정 (Phase 2.5)
        /// </summary>
        public void SetActiveTab(string tabName)
        {
            ActiveTab = tabName;
            // 탭 변경 시 필요한 추가 로직은 여기에
        }

        /// <summary>
        /// 프로젝트 선택 변경 시
        /// </summary>
        partial void OnSelectedProjectIdChanged(string value)
        {
            _ = RefreshDataAsync();
        }

        /// <summary>
        /// 상태 필터 변경
        /// </summary>
        public void FilterByStatus(string status)
        {
            StatusFilter = status == "all" ? null : status;
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

                await LoadProgramSummariesAsync();
                await LoadStatisticsAsync();
                await LoadDashboardPropertiesAsync();
                RecalculateColumnProgress();
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
        /// 에러 메시지 닫기
        /// </summary>
        [RelayCommand]
        private void ClearError()
        {
            ErrorMessage = null;
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
        /// Excel 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"대시보드_{SelectedProgram?.ProjectName ?? "전체"}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    
                    // ClosedXML을 사용한 Excel 내보내기
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("대시보드");

                    // 헤더
                    var headers = new[] { "차주번호", "차주명", "담보번호", "물건종류", "약정서", "보증서", 
                                         "경개1", "경개2", "경매열람", "전입열람", "선순위", "평가확정", 
                                         "경매일정", "QA", "권리분석", "상태" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    }

                    // 데이터
                    int row = 2;
                    foreach (var p in DashboardProperties)
                    {
                        worksheet.Cell(row, 1).Value = p.PropertyNumber;
                        worksheet.Cell(row, 2).Value = p.DebtorName;
                        worksheet.Cell(row, 3).Value = p.CollateralNumber;
                        worksheet.Cell(row, 4).Value = p.PropertyType;
                        worksheet.Cell(row, 5).Value = p.AgreementDoc ? "O" : "";
                        worksheet.Cell(row, 6).Value = p.GuaranteeDoc ? "O" : "";
                        worksheet.Cell(row, 7).Value = p.AuctionStart1;
                        worksheet.Cell(row, 8).Value = p.AuctionStart2;
                        worksheet.Cell(row, 9).Value = p.AuctionDocs ? "O" : "";
                        worksheet.Cell(row, 10).Value = p.TenantDocs ? "O" : "";
                        worksheet.Cell(row, 11).Value = p.SeniorRightsReview ? "O" : "";
                        worksheet.Cell(row, 12).Value = p.AppraisalConfirmed ? "O" : "";
                        worksheet.Cell(row, 13).Value = p.AuctionScheduleDate?.ToString("yyyy-MM-dd");
                        worksheet.Cell(row, 14).Value = p.QaUnansweredCount;
                        worksheet.Cell(row, 15).Value = p.RightsAnalysisStatus;
                        worksheet.Cell(row, 16).Value = p.Status;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveFileDialog.FileName);

                    System.Windows.MessageBox.Show($"파일이 저장되었습니다.\n{saveFileDialog.FileName}", 
                        "Excel 내보내기", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Excel 내보내기 실패: {ex.Message}";
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
