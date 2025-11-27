using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 대시보드 ViewModel - 15컬럼 진행 관리 그리드 지원
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        // ========== 사용자 정보 ==========
        [ObservableProperty]
        private User? _currentUser;

        // ========== 필터 ==========
        [ObservableProperty]
        private string _selectedProjectId = "";

        [ObservableProperty]
        private ObservableCollection<string> _projects = new();

        [ObservableProperty]
        private string? _statusFilter;

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

                // 현재 사용자 정보 가져오기
                await LoadCurrentUserAsync();

                // 평가자 목록 로드 (PM/Admin용)
                await LoadEvaluatorsAsync();

                // 프로젝트 목록 로드
                await LoadProjectsAsync();

                // 통계 로드
                await LoadStatisticsAsync();

                // 대시보드 물건 목록 로드
                await LoadDashboardPropertiesAsync();
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
        /// 통계 로드
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
                
                TotalCount = stats.TotalCount;
                PendingCount = stats.PendingCount;
                ProcessingCount = stats.ProcessingCount;
                CompletedCount = stats.CompletedCount;

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
        /// 대시보드 물건 목록 로드 (15컬럼 그리드용)
        /// </summary>
        private async Task LoadDashboardPropertiesAsync()
        {
            try
            {
                var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;
                
                Guid? assignedTo = null;
                if (CurrentUser?.IsEvaluator == true)
                {
                    assignedTo = CurrentUser.Id;
                }

                var (properties, _) = await _propertyRepository.GetPagedAsync(
                    page: 1,
                    pageSize: 100,
                    projectId: projectId,
                    status: StatusFilter,
                    assignedTo: assignedTo
                );

                DashboardProperties.Clear();
                RecentProperties.Clear();

                foreach (var property in properties)
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

                await LoadStatisticsAsync();
                await LoadDashboardPropertiesAsync();
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
                    FileName = $"대시보드_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
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
