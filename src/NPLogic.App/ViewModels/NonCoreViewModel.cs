using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 물건 탭 아이템 모델
    /// </summary>
    public partial class PropertyTabItem : ObservableObject
    {
        [ObservableProperty]
        private Guid _propertyId;

        [ObservableProperty]
        private string _propertyNumber = "";

        [ObservableProperty]
        private string _borrowerNumber = "";

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        // ========== 필터용 추가 속성 ==========

        /// <summary>물건유형</summary>
        [ObservableProperty]
        private string _propertyType = "";

        /// <summary>차주명</summary>
        [ObservableProperty]
        private string _borrowerName = "";

        /// <summary>경매진행 여부</summary>
        [ObservableProperty]
        private bool _isAuctionInProgress;

        /// <summary>회생차주 여부</summary>
        [ObservableProperty]
        private bool _isRestructuring;

        /// <summary>개인회생 여부</summary>
        [ObservableProperty]
        private bool _isPersonalRestructuring;

        /// <summary>차주거주 여부</summary>
        [ObservableProperty]
        private bool _isBorrowerResidence;

        /// <summary>주소</summary>
        [ObservableProperty]
        private string _address = "";
    }

    /// <summary>
    /// 차주 리스트 아이템 모델 (좌측 패널용)
    /// </summary>
    public partial class BorrowerListItem : ObservableObject
    {
        [ObservableProperty]
        private Guid _borrowerId;

        [ObservableProperty]
        private string _borrowerNumber = "";

        [ObservableProperty]
        private string _borrowerName = "";

        [ObservableProperty]
        private string _borrowerType = "";

        [ObservableProperty]
        private int _propertyCount;

        [ObservableProperty]
        private decimal _opb;

        [ObservableProperty]
        private bool _isRestructuring;

        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// 표시용 차주번호 (회생인 경우 표시)
        /// </summary>
        public string DisplayNumber => IsRestructuring ? $"[회생] {BorrowerNumber}" : BorrowerNumber;

        /// <summary>
        /// OPB 표시 (백만원 단위)
        /// </summary>
        public string OpbDisplay => $"{Opb / 1_000_000:N0}백만";
    }

    /// <summary>
    /// 법원 정보 모델 (툴박스용)
    /// </summary>
    public partial class CourtInfo : ObservableObject
    {
        public string CourtCode { get; set; } = "";
        public string CourtName { get; set; } = "";
        public decimal DiscountRate1 { get; set; } = 0.20m;
        public decimal DiscountRate2 { get; set; } = 0.20m;
        public decimal DiscountRate3 { get; set; } = 0.20m;
        public decimal DiscountRate4 { get; set; } = 0.20m;
    }

    /// <summary>
    /// 비핵심 화면 ViewModel
    /// Phase 3: 비핵심 내부 구조 변경
    /// - 9개 탭 구성: 차주 개요, 론, 담보 총괄, 담보 물건, 선순위 평가, 경공매, 회생개요, 현금 흐름 집계, NPB
    /// - 좌측 차주 리스트 (수직)
    /// - 툴박스 사이드 패널
    /// </summary>
    public partial class NonCoreViewModel : ObservableObject
    {
        private readonly PropertyRepository? _propertyRepository;
        private readonly BorrowerRepository? _borrowerRepository;

        /// <summary>
        /// 초기화 여부 (탭 전환 시 재초기화 방지)
        /// </summary>
        private bool _isInitialized = false;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // ========== 차주 리스트 (좌측 패널) ==========

        /// <summary>
        /// 차주 목록 (좌측 패널에 수직으로 표시)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BorrowerListItem> _borrowerItems = new();

        /// <summary>
        /// 현재 선택된 차주
        /// </summary>
        [ObservableProperty]
        private BorrowerListItem? _selectedBorrower;

        /// <summary>
        /// 전체 차주 목록 (필터링 전)
        /// </summary>
        private List<BorrowerListItem> _allBorrowerItems = new();

        /// <summary>
        /// 물건 탭 목록 (PDF 스타일로 나열) - 레거시 호환용
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PropertyTabItem> _propertyTabs = new();

        /// <summary>
        /// 현재 선택된 물건 탭 - 레거시 호환용
        /// </summary>
        [ObservableProperty]
        private PropertyTabItem? _selectedPropertyTab;

        /// <summary>
        /// 현재 활성 기능 탭
        /// </summary>
        [ObservableProperty]
        private string _activeTab = "Home";

        /// <summary>
        /// 툴박스 표시 여부
        /// </summary>
        [ObservableProperty]
        private bool _isToolBoxVisible = false;

        // Undo 관련 (피드백 반영: 자동저장 + 되돌리기)
        /// <summary>
        /// Undo 가능 여부
        /// </summary>
        public bool CanUndo => SelectedPropertyTab?.PropertyId != null && 
                               SelectedPropertyTab.PropertyId != Guid.Empty && 
                               UndoService.Instance.CanUndo(SelectedPropertyTab.PropertyId);

        /// <summary>
        /// Undo 가능 횟수
        /// </summary>
        public int UndoCount => SelectedPropertyTab?.PropertyId != null && 
                                SelectedPropertyTab.PropertyId != Guid.Empty 
                                ? UndoService.Instance.GetUndoCount(SelectedPropertyTab.PropertyId) 
                                : 0;

        /// <summary>
        /// 일괄 작업 모드 여부
        /// </summary>
        [ObservableProperty]
        private bool _isBatchMode;

        /// <summary>
        /// 일괄 작업 유형
        /// </summary>
        [ObservableProperty]
        private string? _batchWorkType;

        /// <summary>
        /// 법원 목록 (툴박스용)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CourtInfo> _courts = new();

        /// <summary>
        /// 선택된 법원
        /// </summary>
        [ObservableProperty]
        private CourtInfo? _selectedCourt;

        /// <summary>
        /// 현재 프로그램 ID
        /// </summary>
        private Guid? _currentProgramId;

        /// <summary>
        /// 현재 차주 ID
        /// </summary>
        private Guid? _currentBorrowerId;
        
        /// <summary>
        /// 현재 프로젝트 ID (롤업용)
        /// </summary>
        [ObservableProperty]
        private string? _currentProjectId;
        
        /// <summary>
        /// 현재 프로젝트 이름 (롤업용)
        /// </summary>
        [ObservableProperty]
        private string? _currentProjectName;

        /// <summary>
        /// 현재 선택된 물건의 회생차주 여부 (회생개요 탭 표시 조건)
        /// </summary>
        [ObservableProperty]
        private bool _isRestructuringBorrower;

        // ========== 검색 및 필터 기능 (신규) ==========

        /// <summary>
        /// Ctrl+F 검색창 표시 여부
        /// </summary>
        [ObservableProperty]
        private bool _isSearchPanelVisible;

        /// <summary>
        /// 검색어
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";

        /// <summary>
        /// 필터 패널 표시 여부 (기본 접힘)
        /// </summary>
        [ObservableProperty]
        private bool _isFilterPanelVisible = false;

        /// <summary>
        /// 전체 차주/물건 목록 (필터링 전)
        /// </summary>
        private List<PropertyTabItem> _allPropertyTabs = new();

        // ========== 필터 조건들 ==========

        /// <summary>회생차주 필터</summary>
        [ObservableProperty]
        private bool _filterRestructuring;

        /// <summary>개인회생 필터</summary>
        [ObservableProperty]
        private bool _filterPersonalRestructuring;

        /// <summary>차주거주 필터</summary>
        [ObservableProperty]
        private bool _filterBorrowerResidence;

        /// <summary>경매진행 필터</summary>
        [ObservableProperty]
        private bool _filterAuctionInProgress;

        /// <summary>상가 필터</summary>
        [ObservableProperty]
        private bool _filterCommercial;

        /// <summary>주택 필터</summary>
        [ObservableProperty]
        private bool _filterResidential;

        /// <summary>
        /// 필터링된 물건 수
        /// </summary>
        [ObservableProperty]
        private int _filteredCount;

        /// <summary>
        /// 전체 물건 수
        /// </summary>
        [ObservableProperty]
        private int _totalCount;

        public NonCoreViewModel(PropertyRepository? propertyRepository = null, BorrowerRepository? borrowerRepository = null)
        {
            _propertyRepository = propertyRepository;
            _borrowerRepository = borrowerRepository;

            // 기본 법원 데이터 초기화
            InitializeCourts();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            // 이미 초기화되었고 차주 목록이 있으면 재초기화하지 않음
            if (_isInitialized && BorrowerItems.Any())
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 차주 목록 로드
                if (!string.IsNullOrEmpty(CurrentProjectId) && _borrowerRepository != null)
                {
                    await LoadBorrowersAsync();
                }

                _isInitialized = true;
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
        /// 차주 목록 로드
        /// </summary>
        public async Task LoadBorrowersAsync()
        {
            if (_borrowerRepository == null || string.IsNullOrEmpty(CurrentProjectId))
                return;

            try
            {
                var borrowers = await _borrowerRepository.GetByProgramIdAsync(CurrentProjectId);

                BorrowerItems.Clear();
                _allBorrowerItems.Clear();

                foreach (var borrower in borrowers.OrderBy(b => b.BorrowerNumber))
                {
                    var item = new BorrowerListItem
                    {
                        BorrowerId = borrower.Id,
                        BorrowerNumber = borrower.BorrowerNumber,
                        BorrowerName = borrower.BorrowerName,
                        BorrowerType = borrower.BorrowerType,
                        PropertyCount = borrower.PropertyCount,
                        Opb = borrower.Opb,
                        IsRestructuring = borrower.IsRestructuring,
                        IsSelected = false
                    };

                    BorrowerItems.Add(item);
                    _allBorrowerItems.Add(item);
                }

                FilteredCount = BorrowerItems.Count;
                TotalCount = _allBorrowerItems.Count;

                // 첫 번째 차주 선택
                if (BorrowerItems.Any())
                {
                    SelectBorrower(BorrowerItems.First().BorrowerId);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 차주 선택
        /// </summary>
        public void SelectBorrower(Guid borrowerId)
        {
            foreach (var item in BorrowerItems)
            {
                item.IsSelected = item.BorrowerId == borrowerId;
            }

            SelectedBorrower = BorrowerItems.FirstOrDefault(b => b.BorrowerId == borrowerId);

            // 회생차주 여부 업데이트 (회생개요 탭 표시 조건)
            IsRestructuringBorrower = SelectedBorrower?.IsRestructuring ?? false;

            // 선택된 차주의 물건 목록 로드
            _ = LoadPropertiesForBorrowerAsync(borrowerId);
        }

        /// <summary>
        /// 선택된 차주의 물건 목록 로드
        /// </summary>
        private async Task LoadPropertiesForBorrowerAsync(Guid borrowerId)
        {
            if (_propertyRepository == null || borrowerId == Guid.Empty)
                return;

            try
            {
                // 차주의 물건 목록 조회 (BorrowerNumber 기준)
                var borrower = SelectedBorrower;
                if (borrower == null) return;

                // PropertyTabs 갱신 (해당 차주의 물건들)
                PropertyTabs.Clear();

                if (_currentProgramId.HasValue)
                {
                    var properties = await _propertyRepository.GetByProgramIdAsync(_currentProgramId.Value);
                    var borrowerProperties = properties
                        .Where(p => p.DebtorName == borrower.BorrowerName || p.DebtorName == borrower.BorrowerNumber)
                        .ToList();

                    foreach (var property in borrowerProperties)
                    {
                        PropertyTabs.Add(new PropertyTabItem
                        {
                            PropertyId = property.Id,
                            PropertyNumber = property.PropertyNumber ?? "-",
                            BorrowerNumber = borrower.BorrowerNumber,
                            BorrowerName = borrower.BorrowerName,
                            IsSelected = false
                        });
                    }

                    // 첫 번째 물건 선택
                    if (PropertyTabs.Any())
                    {
                        SelectPropertyTab(PropertyTabs.First().PropertyId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"물건 목록 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 다음 차주로 이동 (상하 이동)
        /// </summary>
        [RelayCommand]
        public void NextBorrower()
        {
            if (SelectedBorrower == null || !BorrowerItems.Any())
                return;

            var currentIndex = BorrowerItems.IndexOf(SelectedBorrower);
            if (currentIndex < BorrowerItems.Count - 1)
            {
                SelectBorrower(BorrowerItems[currentIndex + 1].BorrowerId);
            }
        }

        /// <summary>
        /// 이전 차주로 이동 (상하 이동)
        /// </summary>
        [RelayCommand]
        public void PreviousBorrower()
        {
            if (SelectedBorrower == null || !BorrowerItems.Any())
                return;

            var currentIndex = BorrowerItems.IndexOf(SelectedBorrower);
            if (currentIndex > 0)
            {
                SelectBorrower(BorrowerItems[currentIndex - 1].BorrowerId);
            }
        }

        /// <summary>
        /// 프로그램 ID 설정
        /// </summary>
        public void SetProgramId(Guid programId)
        {
            // 프로그램이 변경되면 초기화 상태 리셋
            if (_currentProgramId != programId)
            {
                _isInitialized = false;
                PropertyTabs.Clear();
                BorrowerItems.Clear();
                _allBorrowerItems.Clear();
            }
            _currentProgramId = programId;
        }
        
        /// <summary>
        /// 프로젝트 정보 설정 (롤업용)
        /// </summary>
        public void SetProjectInfo(string projectId, string projectName)
        {
            // 프로젝트가 변경되면 초기화 상태 리셋
            if (CurrentProjectId != projectId)
            {
                _isInitialized = false;
                BorrowerItems.Clear();
                _allBorrowerItems.Clear();
                PropertyTabs.Clear();
            }
            CurrentProjectId = projectId;
            CurrentProjectName = projectName;
        }

        /// <summary>
        /// 차주 ID 설정
        /// </summary>
        public void SetBorrowerId(Guid borrowerId)
        {
            _currentBorrowerId = borrowerId;
        }

        /// <summary>
        /// 물건(Property) 기반으로 로드 (피드백 반영: 대시보드 내에서 비핵심 탭 사용 시)
        /// </summary>
        public void LoadProperty(Property property)
        {
            if (property == null) return;

            // 닫은 탭 상태 로드 (파일에서)
            NavigationStateService.Instance.LoadNonCoreTabState();

            // 프로그램이 변경되면 기존 탭 모두 클리어
            if (property.ProgramId.HasValue && _currentProgramId != property.ProgramId.Value)
            {
                PropertyTabs.Clear();
                _currentProgramId = property.ProgramId.Value;
                _isInitialized = false; // 새 프로그램이므로 초기화 상태 리셋
            }
            else if (property.ProgramId.HasValue)
            {
                _currentProgramId = property.ProgramId.Value;
            }

            // 프로젝트 정보 설정
            CurrentProjectId = property.ProjectId;
            CurrentProjectName = property.ProjectId;

            // 물건 탭 추가
            AddPropertyTab(property);

            // 선택된 물건 탭으로 전환
            if (PropertyTabs.Any())
            {
                SelectPropertyTab(property.Id);
            }
        }

        /// <summary>
        /// 물건 탭 로드
        /// </summary>
        private async Task LoadPropertyTabsAsync()
        {
            if (_propertyRepository == null || !_currentProgramId.HasValue)
                return;

            PropertyTabs.Clear();

            // 닫은 탭 목록 가져오기 (InitializeAsync에서 이미 로드됨)
            var closedTabs = NavigationStateService.Instance.GetClosedTabs(_currentProgramId.Value);

            var properties = await _propertyRepository.GetByProgramIdAsync(_currentProgramId.Value);
            
            // 처음 10개만 가져온 후, 그 중에서 닫은 탭 제외 (새 탭 추가 안 함)
            foreach (var property in properties.Take(10).Where(p => !closedTabs.Contains(p.Id)))
            {
                PropertyTabs.Add(new PropertyTabItem
                {
                    PropertyId = property.Id,
                    PropertyNumber = property.PropertyNumber ?? "-",
                    BorrowerNumber = "-", // Property에는 BorrowerNumber가 없음
                    IsSelected = false
                });
            }

            // 첫 번째 탭 선택
            if (PropertyTabs.Any())
            {
                SelectPropertyTab(PropertyTabs.First().PropertyId);
            }
        }

        /// <summary>
        /// 물건 탭 선택
        /// </summary>
        public void SelectPropertyTab(Guid propertyId)
        {
            foreach (var tab in PropertyTabs)
            {
                tab.IsSelected = tab.PropertyId == propertyId;
            }

            SelectedPropertyTab = PropertyTabs.FirstOrDefault(t => t.PropertyId == propertyId);
            
            // 회생차주 여부 업데이트 (회생개요 탭 표시 조건)
            IsRestructuringBorrower = SelectedPropertyTab?.IsRestructuring ?? false;
        }

        /// <summary>
        /// 물건 탭 닫기
        /// </summary>
        public void ClosePropertyTab(Guid propertyId)
        {
            var tab = PropertyTabs.FirstOrDefault(t => t.PropertyId == propertyId);
            if (tab != null)
            {
                var index = PropertyTabs.IndexOf(tab);
                PropertyTabs.Remove(tab);

                // 닫은 탭 정보 저장 (프로그램 재시작 시에도 유지)
                if (_currentProgramId.HasValue)
                {
                    NavigationStateService.Instance.AddClosedTab(_currentProgramId.Value, propertyId);
                }

                // 닫힌 탭이 선택된 탭이었다면 다음 탭 선택
                if (tab.IsSelected && PropertyTabs.Any())
                {
                    var newIndex = Math.Min(index, PropertyTabs.Count - 1);
                    SelectPropertyTab(PropertyTabs[newIndex].PropertyId);
                }
            }
        }
        
        /// <summary>
        /// 모든 물건 탭 닫기 (뷰 통합: 브레드크럼 클릭 시 사용)
        /// </summary>
        public void CloseAllPropertyTabs()
        {
            PropertyTabs.Clear();
            SelectedPropertyTab = null;
        }

        /// <summary>
        /// 물건 탭 추가
        /// </summary>
        public void AddPropertyTab(Property property)
        {
            // 이미 열려있는지 확인
            if (PropertyTabs.Any(t => t.PropertyId == property.Id))
            {
                SelectPropertyTab(property.Id);
                return;
            }

            // 닫은 탭 목록에서 제거 (다시 열었으므로)
            if (_currentProgramId.HasValue)
            {
                NavigationStateService.Instance.RemoveClosedTab(_currentProgramId.Value, property.Id);
            }

            var newTab = new PropertyTabItem
            {
                PropertyId = property.Id,
                PropertyNumber = property.PropertyNumber ?? "-",
                BorrowerNumber = "-", // Property에는 BorrowerNumber가 없음
                IsSelected = true
            };

            PropertyTabs.Add(newTab);
            SelectPropertyTab(property.Id);
        }

        /// <summary>
        /// 활성 탭 설정
        /// </summary>
        public void SetActiveTab(string tabName)
        {
            ActiveTab = tabName;
        }

        /// <summary>
        /// 일괄 작업 시작
        /// </summary>
        public void StartBatchWork(string workType)
        {
            IsBatchMode = true;
            BatchWorkType = workType;

            // TODO: 일괄 작업 모드 UI 표시
            // 선택된 모든 물건에 대해 해당 기능만 순차적으로 작업할 수 있게 함
        }

        /// <summary>
        /// 일괄 작업 종료
        /// </summary>
        [RelayCommand]
        private void EndBatchWork()
        {
            IsBatchMode = false;
            BatchWorkType = null;
        }

        /// <summary>
        /// 다음 물건으로 이동 (일괄 작업 모드 또는 단축키)
        /// </summary>
        [RelayCommand]
        public void NextProperty()
        {
            if (SelectedPropertyTab == null || !PropertyTabs.Any())
                return;

            var currentIndex = PropertyTabs.IndexOf(SelectedPropertyTab);
            if (currentIndex < PropertyTabs.Count - 1)
            {
                SelectPropertyTab(PropertyTabs[currentIndex + 1].PropertyId);
            }
        }

        /// <summary>
        /// 이전 물건으로 이동 (일괄 작업 모드 또는 단축키)
        /// </summary>
        [RelayCommand]
        public void PreviousProperty()
        {
            if (SelectedPropertyTab == null || !PropertyTabs.Any())
                return;

            var currentIndex = PropertyTabs.IndexOf(SelectedPropertyTab);
            if (currentIndex > 0)
            {
                SelectPropertyTab(PropertyTabs[currentIndex - 1].PropertyId);
            }
        }

        /// <summary>
        /// 법원 데이터 초기화
        /// </summary>
        private void InitializeCourts()
        {
            Courts = new ObservableCollection<CourtInfo>
            {
                new CourtInfo { CourtCode = "1101", CourtName = "서울중앙지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
                new CourtInfo { CourtCode = "1102", CourtName = "서울동부지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
                new CourtInfo { CourtCode = "1103", CourtName = "서울서부지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
                new CourtInfo { CourtCode = "1104", CourtName = "서울남부지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
                new CourtInfo { CourtCode = "1105", CourtName = "서울북부지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
                new CourtInfo { CourtCode = "1201", CourtName = "의정부지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.30m, DiscountRate4 = 0.30m },
                new CourtInfo { CourtCode = "1301", CourtName = "인천지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
                new CourtInfo { CourtCode = "1401", CourtName = "수원지방법원", DiscountRate1 = 0.20m, DiscountRate2 = 0.20m, DiscountRate3 = 0.20m, DiscountRate4 = 0.20m },
            };

            if (Courts.Any())
            {
                SelectedCourt = Courts.First();
            }
        }

        /// <summary>
        /// 저장 - 현재 선택된 물건의 변경사항 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (SelectedPropertyTab == null)
            {
                System.Windows.MessageBox.Show("저장할 물건을 선택해주세요.", "알림", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;
                
                // 현재 선택된 물건 저장
                if (_propertyRepository != null && SelectedPropertyTab.PropertyId != Guid.Empty)
                {
                    var property = await _propertyRepository.GetByIdAsync(SelectedPropertyTab.PropertyId);
                    if (property != null)
                    {
                        // 저장 전 스냅샷 저장 (Undo용)
                        UndoService.Instance.SaveSnapshot(property, "저장 전");
                        OnPropertyChanged(nameof(CanUndo));
                        OnPropertyChanged(nameof(UndoCount));

                        await _propertyRepository.UpdateAsync(property);
                        System.Windows.MessageBox.Show("저장되었습니다.", "알림",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "오류",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 되돌리기 - 이전 상태로 복원 (피드백 반영: 최대 3단계)
        /// </summary>
        [RelayCommand]
        private async Task UndoAsync()
        {
            if (SelectedPropertyTab == null || !CanUndo)
            {
                System.Windows.MessageBox.Show("되돌릴 수 없습니다.", "알림",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;

                var restoredProperty = UndoService.Instance.Undo(SelectedPropertyTab.PropertyId);
                if (restoredProperty != null && _propertyRepository != null)
                {
                    // DB에 복원된 상태 저장
                    await _propertyRepository.UpdateAsync(restoredProperty);

                    System.Windows.MessageBox.Show("이전 상태로 되돌렸습니다.", "알림",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(UndoCount));

                    // 화면 새로고침
                    await RefreshAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show("되돌리기에 실패했습니다.", "오류",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"되돌리기 중 오류가 발생했습니다: {ex.Message}", "오류",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            await InitializeAsync();
        }

        /// <summary>
        /// 현재 선택된 물건 가져오기 (Phase 7.1: 상권 지도용)
        /// </summary>
        public Property? GetCurrentProperty()
        {
            if (SelectedPropertyTab == null || _propertyRepository == null)
                return null;

            try
            {
                return _propertyRepository.GetByIdAsync(SelectedPropertyTab.PropertyId).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 현재 선택된 물건 비동기로 가져오기
        /// </summary>
        public async Task<Property?> GetCurrentPropertyAsync()
        {
            if (SelectedPropertyTab == null || _propertyRepository == null)
                return null;

            return await _propertyRepository.GetByIdAsync(SelectedPropertyTab.PropertyId);
        }

        // ========== 검색 및 필터 기능 메서드 (신규) ==========

        /// <summary>
        /// 검색창 토글 (Ctrl+F)
        /// </summary>
        [RelayCommand]
        public void ToggleSearchPanel()
        {
            IsSearchPanelVisible = !IsSearchPanelVisible;
            if (!IsSearchPanelVisible)
            {
                SearchText = "";
                ApplyFilters();
            }
        }

        /// <summary>
        /// 필터 패널 토글
        /// </summary>
        [RelayCommand]
        public void ToggleFilterPanel()
        {
            IsFilterPanelVisible = !IsFilterPanelVisible;
        }

        /// <summary>
        /// 검색 실행
        /// </summary>
        [RelayCommand]
        public void ExecuteSearch()
        {
            ApplyFilters();
        }

        /// <summary>
        /// 필터 적용 (차주 목록에 적용)
        /// </summary>
        public void ApplyFilters()
        {
            if (!_allBorrowerItems.Any())
            {
                _allBorrowerItems = BorrowerItems.ToList();
            }

            var filtered = _allBorrowerItems.AsEnumerable();

            // 텍스트 검색 (차주번호, 차주명에서 검색)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(b => 
                    (b.BorrowerNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (b.BorrowerName?.ToLower().Contains(searchLower) ?? false));
            }

            // 회생차주 필터
            if (FilterRestructuring)
            {
                filtered = filtered.Where(b => b.IsRestructuring);
            }

            var filteredList = filtered.ToList();
            
            BorrowerItems.Clear();
            foreach (var item in filteredList)
            {
                BorrowerItems.Add(item);
            }

            FilteredCount = BorrowerItems.Count;
            TotalCount = _allBorrowerItems.Count;

            // 첫 번째 항목 선택
            if (BorrowerItems.Any() && SelectedBorrower == null)
            {
                SelectBorrower(BorrowerItems.First().BorrowerId);
            }
        }

        /// <summary>
        /// 필터 초기화
        /// </summary>
        [RelayCommand]
        public void ClearFilters()
        {
            FilterRestructuring = false;
            FilterPersonalRestructuring = false;
            FilterBorrowerResidence = false;
            FilterAuctionInProgress = false;
            FilterCommercial = false;
            FilterResidential = false;
            SearchText = "";

            // 전체 차주 목록 복원
            BorrowerItems.Clear();
            foreach (var item in _allBorrowerItems)
            {
                BorrowerItems.Add(item);
            }

            FilteredCount = BorrowerItems.Count;
            TotalCount = _allBorrowerItems.Count;
        }

        /// <summary>
        /// 모든 물건 가져오기 (Excel/인쇄용)
        /// </summary>
        public async Task<List<Property>> GetAllPropertiesAsync()
        {
            if (_propertyRepository == null || !_currentProgramId.HasValue)
                return new List<Property>();

            var properties = await _propertyRepository.GetByProgramIdAsync(_currentProgramId.Value);
            return properties.ToList();
        }

        /// <summary>
        /// 차주별 물건 가져오기 (Excel/인쇄용)
        /// </summary>
        public async Task<Dictionary<string, List<Property>>> GetPropertiesGroupedByBorrowerAsync()
        {
            var properties = await GetAllPropertiesAsync();
            return properties
                .GroupBy(p => p.DebtorName ?? "미지정")
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// 검색 하이라이트용 - 현재 검색어와 일치하는 다음 차주로 이동
        /// </summary>
        [RelayCommand]
        public void FindNext()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || !BorrowerItems.Any())
                return;

            var searchLower = SearchText.ToLower();
            var currentIndex = SelectedBorrower != null ? BorrowerItems.IndexOf(SelectedBorrower) : -1;

            // 현재 위치 다음부터 검색
            for (int i = currentIndex + 1; i < BorrowerItems.Count; i++)
            {
                var item = BorrowerItems[i];
                if ((item.BorrowerNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (item.BorrowerName?.ToLower().Contains(searchLower) ?? false))
                {
                    SelectBorrower(item.BorrowerId);
                    return;
                }
            }

            // 처음부터 다시 검색
            for (int i = 0; i <= currentIndex; i++)
            {
                var item = BorrowerItems[i];
                if ((item.BorrowerNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (item.BorrowerName?.ToLower().Contains(searchLower) ?? false))
                {
                    SelectBorrower(item.BorrowerId);
                    return;
                }
            }
        }

        /// <summary>
        /// 검색 하이라이트용 - 이전 차주로 이동
        /// </summary>
        [RelayCommand]
        public void FindPrevious()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || !BorrowerItems.Any())
                return;

            var searchLower = SearchText.ToLower();
            var currentIndex = SelectedBorrower != null ? BorrowerItems.IndexOf(SelectedBorrower) : BorrowerItems.Count;

            // 현재 위치 이전부터 검색
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                var item = BorrowerItems[i];
                if ((item.BorrowerNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (item.BorrowerName?.ToLower().Contains(searchLower) ?? false))
                {
                    SelectBorrower(item.BorrowerId);
                    return;
                }
            }

            // 끝에서부터 다시 검색
            for (int i = BorrowerItems.Count - 1; i >= currentIndex; i--)
            {
                var item = BorrowerItems[i];
                if ((item.BorrowerNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (item.BorrowerName?.ToLower().Contains(searchLower) ?? false))
                {
                    SelectBorrower(item.BorrowerId);
                    return;
                }
            }
        }

        /// <summary>
        /// SelectedPropertyTab 변경 시 Undo 상태 업데이트
        /// </summary>
        partial void OnSelectedPropertyTabChanged(PropertyTabItem? value)
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(UndoCount));
        }

        /// <summary>
        /// SelectedBorrower 변경 시 처리
        /// </summary>
        partial void OnSelectedBorrowerChanged(BorrowerListItem? value)
        {
            if (value != null)
            {
                IsRestructuringBorrower = value.IsRestructuring;
            }
        }
    }
}

