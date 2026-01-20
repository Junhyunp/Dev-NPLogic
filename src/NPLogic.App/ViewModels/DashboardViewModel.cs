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
    /// 3단계 드릴다운 네비게이션: 프로그램 -> 차주 -> 물건
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;
        private readonly ProgramUserRepository _programUserRepository;
        private readonly ProgramRepository _programRepository;
        private readonly BorrowerRepository _borrowerRepository;
        
        // PM 담당 프로그램 ID 목록 (캐시)
        private List<Guid> _pmProgramIds = new();
        
        // 프로그램 정보 캐시 (program_id -> program_name)
        private Dictionary<Guid, string> _programNameCache = new();

        // ========== 페이지네이션 상태 (서버 사이드) ==========
        private int _currentPage = 1;
        private const int PageSize = 50;
        private bool _hasMoreData = true;
        private bool _isLoadingMore = false;
        private int _totalPropertyCount = 0;

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

        // ========== 고급 필터 (F-001) ==========

        /// <summary>고급 필터 패널 표시 여부</summary>
        [ObservableProperty]
        private bool _isAdvancedFilterVisible;

        // 차주 상태 필터
        /// <summary>회생 필터</summary>
        [ObservableProperty]
        private bool _filterRestructuring;

        /// <summary>개회(면책) 필터</summary>
        [ObservableProperty]
        private bool _filterOpened;

        /// <summary>사망 필터</summary>
        [ObservableProperty]
        private bool _filterDeceased;

        /// <summary>차주거주 필터</summary>
        [ObservableProperty]
        private bool _filterBorrowerResiding;

        // 담보 유형 필터
        /// <summary>담보 유형 목록</summary>
        [ObservableProperty]
        private ObservableCollection<string> _propertyTypes = new() 
        { 
            "전체", "아파트", "상가", "토지", "빌라", "오피스텔", "단독주택", "다가구주택", "공장", "기타" 
        };

        /// <summary>선택된 담보 유형</summary>
        [ObservableProperty]
        private string _selectedPropertyType = "전체";

        // 선순위 필터
        /// <summary>소유자 전입 필터 (null=전체, true=전입, false=미전입)</summary>
        [ObservableProperty]
        private bool? _filterOwnerMoveIn;

        /// <summary>활성화된 필터 개수</summary>
        public int ActiveFilterCount
        {
            get
            {
                int count = 0;
                if (FilterRestructuring) count++;
                if (FilterOpened) count++;
                if (FilterDeceased) count++;
                if (FilterBorrowerResiding) count++;
                if (SelectedPropertyType != "전체") count++;
                if (FilterOwnerMoveIn.HasValue) count++;
                return count;
            }
        }

        // ========== 탭 상태 ==========
        [ObservableProperty]
        private string _activeTab = "noncore";

        // ========== 뎁스 모드 상태 ==========
        /// <summary>
        /// 상세 모드 여부 (false: 목록 모드, true: 상세 모드)
        /// </summary>
        [ObservableProperty]
        private bool _isDetailMode = false;

        // ========== 브레드크럼용 프로퍼티 ==========
        /// <summary>
        /// 현재 탭 표시 이름 (브레드크럼용)
        /// </summary>
        public string? CurrentTabName => IsDetailMode ? ActiveTab switch
        {
            "noncore" => "비핵심",
            "registry" => "등기부등본",
            "rights" => "권리분석",
            "basicdata" => "기초데이터",
            "closing" => "마감",
            _ => null
        } : null;

        /// <summary>
        /// 선택된 물건 번호 (브레드크럼용)
        /// </summary>
        public string? SelectedPropertyNumber => SelectedProperty?.PropertyNumber;

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

        // ========== 필터 바 접기/펼치기 ==========
        /// <summary>필터 바 펼침 여부 (기본값: true)</summary>
        [ObservableProperty]
        private bool _isFilterBarExpanded = true;

        /// <summary>
        /// 필터 바 영역 표시 여부 (리스트 모드이고 프로그램이 선택된 경우에만 표시)
        /// </summary>
        public bool IsFilterBarAreaVisible => !IsDetailMode && SelectedProgram != null;

        // ========== 데이터 ==========
        [ObservableProperty]
        private ObservableCollection<Property> _recentProperties = new();

        [ObservableProperty]
        private ObservableCollection<Property> _dashboardProperties = new();

        // ========== 선택된 물건 (피드백 반영: 대시보드 내 탭 전환용) ==========
        [ObservableProperty]
        private Property? _selectedProperty;

        [ObservableProperty]
        private ObservableCollection<User> _evaluators = new();

        // ========== 3단계 드릴다운 네비게이션 ==========
        
        /// <summary>
        /// 네비게이션 레벨: "Borrower" (차주 목록), "Property" (물건 목록), "Detail" (상세)
        /// </summary>
        [ObservableProperty]
        private string _navigationLevel = "Property";

        /// <summary>
        /// 차주 목록 (프로그램 선택 시 로드)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BorrowerListItem> _borrowerList = new();

        /// <summary>
        /// 선택된 차주
        /// </summary>
        [ObservableProperty]
        private BorrowerListItem? _selectedBorrower;

        /// <summary>
        /// 브레드크럼용 선택된 차주명
        /// </summary>
        public string? SelectedBorrowerName => SelectedBorrower?.BorrowerName;

        /// <summary>
        /// 브레드크럼용 선택된 차주번호
        /// </summary>
        public string? SelectedBorrowerNumber => SelectedBorrower?.BorrowerNumber;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // ========== 무한 스크롤 상태 (UI 바인딩용) ==========
        /// <summary>추가 데이터 로드 가능 여부</summary>
        public bool HasMoreData => _hasMoreData;
        
        /// <summary>추가 데이터 로드 중 여부</summary>
        public bool IsLoadingMore => _isLoadingMore;
        
        /// <summary>전체 물건 수 (페이지네이션용)</summary>
        public int TotalPropertyCount => _totalPropertyCount;

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
            ProgramUserRepository programUserRepository,
            ProgramRepository programRepository,
            BorrowerRepository borrowerRepository)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _programUserRepository = programUserRepository ?? throw new ArgumentNullException(nameof(programUserRepository));
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
            _borrowerRepository = borrowerRepository ?? throw new ArgumentNullException(nameof(borrowerRepository));
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
        /// 서버 사이드 통계 사용으로 최적화
        /// Phase 6: 권한별 필터링 추가
        /// - 관리자(Admin): 전체 보임
        /// - PM: 담당 프로그램만 보임
        /// - 평가자(Evaluator): 자기에게 할당된 물건만 보임
        /// </summary>
        private async Task LoadProgramSummariesAsync()
        {
            try
            {
                // 프로그램 목록을 먼저 로드하여 이름 캐시 구성
                var programs = await _programRepository.GetAllAsync();
                _programNameCache.Clear();
                
                // 회계법인 기반 필터링을 위한 프로그램 ID 목록
                var accountingFirmProgramIds = new List<Guid>();
                
                foreach (var program in programs)
                {
                    _programNameCache[program.Id] = program.ProgramName;
                    
                    // 회계법인 기반 필터링: Admin이 아닌 경우 본인 소속 회계법인 프로그램만 포함
                    if (CurrentUser?.IsAdmin == true)
                    {
                        // Admin은 모든 프로그램 접근 가능
                        accountingFirmProgramIds.Add(program.Id);
                    }
                    else if (!string.IsNullOrEmpty(CurrentUser?.AccountingFirm))
                    {
                        // 회계법인이 설정된 경우: 동일한 회계법인 프로그램만
                        if (string.Equals(program.AccountingFirm, CurrentUser.AccountingFirm, StringComparison.OrdinalIgnoreCase))
                        {
                            accountingFirmProgramIds.Add(program.Id);
                        }
                    }
                    else
                    {
                        // 회계법인이 설정되지 않은 경우: 모든 프로그램 접근 가능
                        accountingFirmProgramIds.Add(program.Id);
                    }
                }

                // 권한별 필터 설정
                List<Guid>? filterProgramIds = null;
                Guid? assignedTo = null;

                if (CurrentUser?.IsAdmin == true)
                {
                    // 관리자: 전체 보임 (필터링 없음)
                }
                else if (CurrentUser?.IsPM == true)
                {
                    // PM: 담당 프로그램 + 회계법인 필터 교집합
                    if (_pmProgramIds.Count > 0)
                    {
                        // PM 담당 프로그램과 회계법인 프로그램의 교집합
                        filterProgramIds = _pmProgramIds.Intersect(accountingFirmProgramIds).ToList();
                    }
                    else
                    {
                        // PM이지만 담당 프로그램이 없으면 회계법인 기준으로만 필터
                        filterProgramIds = accountingFirmProgramIds;
                    }
                    
                    if (filterProgramIds.Count == 0)
                    {
                        // 필터링 결과가 없으면 빈 목록
                        ProgramSummaries.Clear();
                        Projects.Clear();
                        Projects.Add("전체");
                        return;
                    }
                }
                else if (CurrentUser?.IsEvaluator == true)
                {
                    // 평가자: 자기에게 할당된 물건 + 회계법인 필터
                    assignedTo = CurrentUser.Id;
                    filterProgramIds = accountingFirmProgramIds.Count > 0 ? accountingFirmProgramIds : null;
                }

                // 서버 사이드 통계 조회 (전체 물건 로드 없이)
                var programStats = await _propertyRepository.GetProgramStatisticsAsync(
                    filterProgramIds: filterProgramIds,
                    assignedTo: assignedTo
                );

                ProgramSummaries.Clear();

                foreach (var stat in programStats.OrderBy(s => GetProgramName(s.ProgramId)))
                {
                    var programName = GetProgramName(stat.ProgramId);

                    ProgramSummaries.Add(new ProgramSummary
                    {
                        ProjectId = stat.ProgramId.ToString(),
                        ProjectName = programName,
                        TotalCount = stat.TotalCount,
                        CompletedCount = stat.CompletedCount
                    });
                }

                // 프로젝트 드롭다운도 업데이트
                Projects.Clear();
                Projects.Add("전체");
                foreach (var summary in ProgramSummaries)
                {
                    Projects.Add(summary.ProjectName);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"프로그램 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 프로그램 ID로 이름 조회 (캐시 사용)
        /// </summary>
        private string GetProgramName(Guid programId)
        {
            if (_programNameCache.TryGetValue(programId, out var name))
                return name;
            return programId.ToString(); // 캐시에 없으면 ID 반환
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
        /// 간소화된 네비게이션: 프로그램 선택 시 바로 물건 목록 표시
        /// </summary>
        public async Task LoadSelectedProgramDataAsync()
        {
            if (SelectedProgram == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                SelectedProjectId = SelectedProgram.ProjectId;
                
                // 간소화된 네비게이션: 프로그램 선택 시 바로 물건 목록으로 이동 (차주 단계 스킵)
                NavigationLevel = "Property";
                SelectedBorrower = null;
                SelectedProperty = null;

                // 프로그램의 전체 물건 목록 로드
                await LoadAllPropertiesForProgramAsync();

                // 통계 로드
                await LoadStatisticsAsync();
                
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
        /// 프로그램의 물건 목록 로드 (서버 사이드 페이지네이션)
        /// 첫 페이지만 로드하고, 스크롤 시 추가 로드
        /// </summary>
        public async Task LoadAllPropertiesForProgramAsync()
        {
            if (SelectedProgram == null)
                return;

            try
            {
                // 페이지네이션 상태 초기화
                _currentPage = 1;
                _hasMoreData = true;
                _totalPropertyCount = 0;
                DashboardProperties.Clear();
                
                OnPropertyChanged(nameof(HasMoreData));
                OnPropertyChanged(nameof(TotalPropertyCount));

                // 프로그램 ID 파싱
                Guid? programId = Guid.TryParse(SelectedProgram.ProjectId, out var pid) ? pid : null;
                
                // 권한별 필터 설정
                Guid? assignedTo = null;
                List<Guid>? filterProgramIds = null;
                
                if (CurrentUser?.IsEvaluator == true)
                {
                    // 평가자: 자기에게 할당된 물건만
                    assignedTo = CurrentUser.Id;
                }
                else if (CurrentUser?.IsPM == true && _pmProgramIds.Count > 0)
                {
                    // PM: 담당 프로그램만
                    filterProgramIds = _pmProgramIds;
                }

                // 서버 사이드 페이지네이션으로 첫 페이지 조회
                var (items, totalCount) = await _propertyRepository.GetPagedServerSideAsync(
                    page: _currentPage,
                    pageSize: PageSize,
                    programId: programId,
                    status: StatusFilter,
                    assignedTo: assignedTo,
                    filterProgramIds: filterProgramIds
                );
                
                _totalPropertyCount = totalCount;
                
                // 고급 필터는 클라이언트에서 적용 (고급 필터가 활성화된 경우)
                IEnumerable<Property> filteredItems = items;
                if (ActiveFilterCount > 0)
                {
                    filteredItems = ApplyAdvancedFilters(items);
                }

                foreach (var property in filteredItems)
                {
                    DashboardProperties.Add(property);
                }
                
                // 더 로드할 데이터가 있는지 확인
                _hasMoreData = DashboardProperties.Count < totalCount;
                OnPropertyChanged(nameof(HasMoreData));
                OnPropertyChanged(nameof(TotalPropertyCount));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 추가 물건 로드 (무한 스크롤)
        /// </summary>
        public async Task LoadMorePropertiesAsync()
        {
            if (_isLoadingMore || !_hasMoreData || SelectedProgram == null)
                return;

            try
            {
                _isLoadingMore = true;
                OnPropertyChanged(nameof(IsLoadingMore));
                
                _currentPage++;

                // 프로그램 ID 파싱
                Guid? programId = Guid.TryParse(SelectedProgram.ProjectId, out var pid) ? pid : null;
                
                // 권한별 필터 설정
                Guid? assignedTo = null;
                List<Guid>? filterProgramIds = null;
                
                if (CurrentUser?.IsEvaluator == true)
                {
                    assignedTo = CurrentUser.Id;
                }
                else if (CurrentUser?.IsPM == true && _pmProgramIds.Count > 0)
                {
                    filterProgramIds = _pmProgramIds;
                }

                // 다음 페이지 조회
                var (items, totalCount) = await _propertyRepository.GetPagedServerSideAsync(
                    page: _currentPage,
                    pageSize: PageSize,
                    programId: programId,
                    status: StatusFilter,
                    assignedTo: assignedTo,
                    filterProgramIds: filterProgramIds
                );
                
                _totalPropertyCount = totalCount;

                // 고급 필터 적용
                IEnumerable<Property> filteredItems = items;
                if (ActiveFilterCount > 0)
                {
                    filteredItems = ApplyAdvancedFilters(items);
                }

                foreach (var property in filteredItems)
                {
                    DashboardProperties.Add(property);
                }
                
                // 더 로드할 데이터가 있는지 확인
                _hasMoreData = DashboardProperties.Count < totalCount;
                OnPropertyChanged(nameof(HasMoreData));
                OnPropertyChanged(nameof(TotalPropertyCount));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"추가 로드 실패: {ex.Message}";
                _currentPage--; // 실패 시 롤백
            }
            finally
            {
                _isLoadingMore = false;
                OnPropertyChanged(nameof(IsLoadingMore));
            }
        }

        /// <summary>
        /// 선택된 프로그램의 차주 목록 로드
        /// borrowers 테이블에 데이터가 없으면 properties 테이블에서 추출
        /// </summary>
        public async Task LoadBorrowersForProgramAsync()
        {
            if (SelectedProgram == null)
                return;

            try
            {
                BorrowerList.Clear();

                // 물건 목록 조회 (차주 정보 추출용)
                var properties = await _propertyRepository.GetFilteredAsync(projectId: SelectedProgram.ProjectId);
                
                if (properties == null || !properties.Any())
                    return;

                // borrowers 테이블에서 조회 시도
                List<Borrower>? borrowers = null;
                if (_borrowerRepository != null)
                {
                    try
                    {
                        borrowers = await _borrowerRepository.GetByProgramIdAsync(SelectedProgram.ProjectId);
                    }
                    catch
                    {
                        // borrowers 테이블 조회 실패 시 무시
                    }
                }

                if (borrowers != null && borrowers.Any())
                {
                    // borrowers 테이블에 데이터가 있는 경우
                    var propertiesByBorrower = properties
                        .GroupBy(p => p.BorrowerNumber ?? p.DebtorName ?? "")
                        .ToDictionary(g => g.Key, g => g.Count());

                    foreach (var borrower in borrowers.OrderBy(b => b.BorrowerNumber))
                    {
                        var propertyCount = 0;
                        if (propertiesByBorrower.TryGetValue(borrower.BorrowerNumber, out var count1))
                            propertyCount = count1;
                        else if (propertiesByBorrower.TryGetValue(borrower.BorrowerName, out var count2))
                            propertyCount = count2;
                        else
                            propertyCount = borrower.PropertyCount;

                        BorrowerList.Add(new BorrowerListItem
                        {
                            BorrowerId = borrower.Id,
                            BorrowerNumber = borrower.BorrowerNumber,
                            BorrowerName = borrower.BorrowerName,
                            BorrowerType = borrower.BorrowerType,
                            PropertyCount = propertyCount,
                            Opb = borrower.Opb,
                            IsRestructuring = borrower.IsRestructuring,
                            IsSelected = false
                        });
                    }
                }
                else
                {
                    // borrowers 테이블에 데이터가 없는 경우: properties에서 차주 정보 추출
                    var borrowerGroups = properties
                        .GroupBy(p => p.BorrowerNumber ?? "UNKNOWN")
                        .OrderBy(g => g.Key);

                    foreach (var group in borrowerGroups)
                    {
                        var firstProperty = group.First();
                        var borrowerNumber = group.Key;
                        var borrowerName = firstProperty.DebtorName ?? borrowerNumber;
                        var isRestructuring = firstProperty.BorrowerIsRestructuring ?? false;

                        BorrowerList.Add(new BorrowerListItem
                        {
                            BorrowerId = Guid.NewGuid(), // 임시 ID
                            BorrowerNumber = borrowerNumber,
                            BorrowerName = borrowerName,
                            BorrowerType = "",
                            PropertyCount = group.Count(),
                            Opb = 0,
                            IsRestructuring = isRestructuring,
                            IsSelected = false
                        });
                    }
                }

                OnPropertyChanged(nameof(SelectedBorrowerName));
                OnPropertyChanged(nameof(SelectedBorrowerNumber));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 차주 선택 시 물건 목록 로드
        /// </summary>
        public async Task SelectBorrowerAsync(BorrowerListItem borrower)
        {
            if (borrower == null)
                return;

            try
            {
                IsLoading = true;

                // 선택된 차주 설정
                foreach (var item in BorrowerList)
                {
                    item.IsSelected = item.BorrowerId == borrower.BorrowerId;
                }
                SelectedBorrower = borrower;

                // 네비게이션 레벨을 물건 목록으로 변경
                NavigationLevel = "Property";

                // 해당 차주의 물건 목록 로드
                await LoadPropertiesForBorrowerAsync();

                OnPropertyChanged(nameof(SelectedBorrowerName));
                OnPropertyChanged(nameof(SelectedBorrowerNumber));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 선택된 차주의 물건 목록 로드
        /// </summary>
        private async Task LoadPropertiesForBorrowerAsync()
        {
            if (SelectedBorrower == null || SelectedProgram == null)
                return;

            try
            {
                DashboardProperties.Clear();

                // 프로그램의 전체 물건 중 해당 차주의 물건만 필터링
                var properties = await _propertyRepository.GetFilteredAsync(projectId: SelectedProgram.ProjectId);
                
                var borrowerProperties = properties.Where(p =>
                    p.DebtorName == SelectedBorrower.BorrowerName ||
                    p.DebtorName == SelectedBorrower.BorrowerNumber ||
                    p.BorrowerNumber == SelectedBorrower.BorrowerNumber
                ).ToList();

                foreach (var property in borrowerProperties)
                {
                    DashboardProperties.Add(property);
                }

                // 컬럼별 진행률 계산
                RecalculateColumnProgress();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 물건 목록으로 돌아가기 (브레드크럼 클릭 시)
        /// 간소화된 네비게이션: 바로 물건 목록으로 이동
        /// </summary>
        public void NavigateBackToPropertyList()
        {
            NavigationLevel = "Property";
            SelectedProperty = null;
            IsDetailMode = false;

            OnPropertyChanged(nameof(SelectedPropertyNumber));
            OnPropertyChanged(nameof(CurrentTabName));
            OnPropertyChanged(nameof(SelectedBorrowerName));
        }

        /// <summary>
        /// 프로그램 선택 화면으로 돌아가기 (브레드크럼 홈 클릭 시)
        /// </summary>
        public void NavigateBackToPrograms()
        {
            NavigationLevel = "Property";
            SelectedProperty = null;
            IsDetailMode = false;
            
            OnPropertyChanged(nameof(SelectedPropertyNumber));
            OnPropertyChanged(nameof(CurrentTabName));
            OnPropertyChanged(nameof(SelectedBorrowerName));
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
        /// F-001: 고급 필터 적용
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

                // ========== F-001: 고급 필터 적용 ==========
                filteredProperties = ApplyAdvancedFilters(filteredProperties);

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
        /// 고급 필터 적용 (F-001)
        /// </summary>
        private IEnumerable<Property> ApplyAdvancedFilters(IEnumerable<Property> properties)
        {
            var result = properties;

            // 담보 유형 필터
            if (!string.IsNullOrEmpty(SelectedPropertyType) && SelectedPropertyType != "전체")
            {
                result = result.Where(p => 
                    string.Equals(p.PropertyType, SelectedPropertyType, StringComparison.OrdinalIgnoreCase));
            }

            // 차주 상태 필터 (OR 조건 - 선택된 항목 중 하나라도 해당하면 표시)
            var borrowerStatusFilters = new List<Func<Property, bool>>();
            if (FilterRestructuring)
                borrowerStatusFilters.Add(p => p.BorrowerIsRestructuring == true);
            if (FilterOpened)
                borrowerStatusFilters.Add(p => p.BorrowerIsOpened == true);
            if (FilterDeceased)
                borrowerStatusFilters.Add(p => p.BorrowerIsDeceased == true);
            if (FilterBorrowerResiding)
                borrowerStatusFilters.Add(p => p.BorrowerResiding);

            if (borrowerStatusFilters.Count > 0)
            {
                result = result.Where(p => borrowerStatusFilters.Any(f => f(p)));
            }

            // 소유자 전입 필터
            if (FilterOwnerMoveIn.HasValue)
            {
                result = result.Where(p => p.OwnerMoveIn == FilterOwnerMoveIn.Value);
            }

            return result;
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
            // 브레드크럼 업데이트
            OnPropertyChanged(nameof(CurrentTabName));
        }

        /// <summary>
        /// 상세 모드로 전환 (물건 선택 시)
        /// 간소화된 네비게이션: 좌측에 물건 리스트 유지, 우측에 상세 페이지
        /// </summary>
        public void SwitchToDetailMode(Property property)
        {
            SelectedProperty = property;
            IsDetailMode = true;
            NavigationLevel = "Detail";
            ActiveTab = "noncore"; // 기본 탭은 비핵심
            OnPropertyChanged(nameof(CurrentTabName));
            OnPropertyChanged(nameof(SelectedPropertyNumber));
            OnPropertyChanged(nameof(SelectedBorrowerName));
        }

        /// <summary>
        /// 목록 모드로 전환 (목록으로 버튼 클릭 시)
        /// 간소화된 네비게이션: 물건 목록으로 바로 이동
        /// </summary>
        public void SwitchToListMode()
        {
            IsDetailMode = false;
            NavigationLevel = "Property";
            SelectedProperty = null;
            OnPropertyChanged(nameof(CurrentTabName));
            OnPropertyChanged(nameof(SelectedPropertyNumber));
        }

        /// <summary>
        /// 좌측 물건 리스트에서 다른 물건 선택 시 상세 화면 전환
        /// 주의: 이미 상세 모드이므로 OnPropertySelected 이벤트를 발생시키지 않음 (무한 루프 방지)
        /// </summary>
        public void SelectPropertyInDetailMode(Property property)
        {
            if (property == null) return;

            SelectedProperty = property;
            OnPropertyChanged(nameof(SelectedPropertyNumber));
            OnPropertyChanged(nameof(SelectedBorrowerName));
            // 이벤트 발생하지 않음 - View에서 직접 탭 컨텐츠 업데이트
        }

        /// <summary>
        /// 프로젝트 선택 변경 시
        /// </summary>
        partial void OnSelectedProjectIdChanged(string value)
        {
            _ = RefreshDataAsync();
        }

        /// <summary>
        /// 선택된 물건 변경 시 (브레드크럼 업데이트)
        /// </summary>
        partial void OnSelectedPropertyChanged(Property? value)
        {
            OnPropertyChanged(nameof(SelectedPropertyNumber));
        }

        /// <summary>
        /// 상세 모드 변경 시 (필터 바 영역 표시/숨김)
        /// </summary>
        partial void OnIsDetailModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsFilterBarAreaVisible));
        }

        /// <summary>
        /// 선택된 프로그램 변경 시 (필터 바 영역 표시/숨김)
        /// </summary>
        partial void OnSelectedProgramChanged(ProgramSummary? value)
        {
            OnPropertyChanged(nameof(IsFilterBarAreaVisible));
        }

        /// <summary>
        /// 상태 필터 변경
        /// </summary>
        public void FilterByStatus(string status)
        {
            StatusFilter = status == "all" ? null : status;
            _ = RefreshDataAsync();
        }

        // ========== 고급 필터 변경 감지 (F-001) ==========

        partial void OnFilterRestructuringChanged(bool value)
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            _ = RefreshDataAsync();
        }

        partial void OnFilterOpenedChanged(bool value)
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            _ = RefreshDataAsync();
        }

        partial void OnFilterDeceasedChanged(bool value)
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            _ = RefreshDataAsync();
        }

        partial void OnFilterBorrowerResidingChanged(bool value)
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            _ = RefreshDataAsync();
        }

        partial void OnSelectedPropertyTypeChanged(string value)
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            _ = RefreshDataAsync();
        }

        partial void OnFilterOwnerMoveInChanged(bool? value)
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            _ = RefreshDataAsync();
        }

        /// <summary>
        /// 고급 필터 패널 토글
        /// </summary>
        [RelayCommand]
        private void ToggleAdvancedFilter()
        {
            IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
        }

        /// <summary>
        /// 고급 필터 초기화
        /// </summary>
        [RelayCommand]
        private void ResetAdvancedFilters()
        {
            FilterRestructuring = false;
            FilterOpened = false;
            FilterDeceased = false;
            FilterBorrowerResiding = false;
            SelectedPropertyType = "전체";
            FilterOwnerMoveIn = null;
            OnPropertyChanged(nameof(ActiveFilterCount));
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

                // 현재 선택된 프로그램 ID 저장 (필터 창 유지를 위해)
                var selectedProgramId = SelectedProgram?.ProjectId;

                await LoadProgramSummariesAsync();
                
                // 이전에 선택된 프로그램이 있으면 다시 선택 (필터 창 유지)
                if (!string.IsNullOrEmpty(selectedProgramId))
                {
                    var previousProgram = ProgramSummaries.FirstOrDefault(p => p.ProjectId == selectedProgramId);
                    if (previousProgram != null)
                    {
                        SelectedProgram = previousProgram;
                    }
                    else if (ProgramSummaries.Count > 0)
                    {
                        // 이전 프로그램이 더 이상 없으면 첫 번째 프로그램 선택
                        SelectedProgram = ProgramSummaries[0];
                    }
                }
                else if (ProgramSummaries.Count > 0 && SelectedProgram == null)
                {
                    // 선택된 프로그램이 없고 목록이 있으면 첫 번째 선택
                    SelectedProgram = ProgramSummaries[0];
                }

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

                    // 헤더 (순서 변경: 차주번호, 물건번호 앞으로)
                    var headers = new[] { "차주번호", "물건번호", "차주명", "물건종류", "약정서", "보증서", 
                                         "경매개시", "경매열람", "전입열람", "선순위", "평가확정", 
                                         "경매일정", "QA", "권리분석", "경매사건번호", "상태" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    }

                    // 데이터 (순서 변경: 차주번호, 물건번호 앞으로)
                    int row = 2;
                    foreach (var p in DashboardProperties)
                    {
                        worksheet.Cell(row, 1).Value = p.BorrowerNumber;
                        worksheet.Cell(row, 2).Value = p.CollateralNumber;
                        worksheet.Cell(row, 3).Value = p.DebtorName;
                        worksheet.Cell(row, 4).Value = p.PropertyType;
                        worksheet.Cell(row, 5).Value = p.AgreementDoc ? "O" : "";
                        worksheet.Cell(row, 6).Value = p.GuaranteeDoc ? "O" : "";
                        worksheet.Cell(row, 7).Value = p.AuctionStart1;
                        worksheet.Cell(row, 8).Value = p.AuctionDocs ? "O" : "";
                        worksheet.Cell(row, 9).Value = p.TenantDocs ? "O" : "";
                        worksheet.Cell(row, 10).Value = p.SeniorRightsReview ? "O" : "";
                        worksheet.Cell(row, 11).Value = p.AppraisalConfirmed ? "O" : "";
                        worksheet.Cell(row, 12).Value = p.AuctionScheduleDate?.ToString("yyyy-MM-dd");
                        worksheet.Cell(row, 13).Value = p.QaUnansweredCount;
                        worksheet.Cell(row, 14).Value = p.RightsAnalysisStatus;
                        worksheet.Cell(row, 15).Value = p.PropertyNumber;
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
        /// 물건 상세 보기 - 상세 모드로 전환
        /// </summary>
        [RelayCommand]
        private void ViewPropertyDetail(Property property)
        {
            if (property == null)
                return;

            // 상세 모드로 전환
            SwitchToDetailMode(property);
            // View에서 UI 업데이트를 처리하도록 이벤트 발생
            OnPropertySelected?.Invoke(property);
        }

        /// <summary>
        /// 물건 선택 이벤트 (피드백 반영: 대시보드 내 탭 전환용)
        /// </summary>
        public event Action<Property>? OnPropertySelected;

        /// <summary>
        /// 물건 선택 및 상세 모드 전환 (외부에서 호출 가능)
        /// 차주번호/명 클릭 시 상세 화면으로 이동
        /// </summary>
        public void SelectPropertyAndSwitchToDetail(Property property)
        {
            SwitchToDetailMode(property);
            OnPropertySelected?.Invoke(property);
        }

        /// <summary>
        /// 물건 목록 화면으로 이동
        /// </summary>
        [RelayCommand]
        private void GoToPropertyList()
        {
            MainWindow.Instance?.NavigateToPropertyList();
        }

        /// <summary>
        /// 프로그램 추가 화면으로 이동
        /// </summary>
        [RelayCommand]
        private void AddProgram()
        {
            MainWindow.Instance?.NavigateToProgramManagement();
        }
    }
}
