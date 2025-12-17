using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

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
    /// - 물건 번호 탭 (PDF 스타일)
    /// - 툴박스 사이드 패널
    /// - 기능별 일괄 작업
    /// </summary>
    public partial class NonCoreViewModel : ObservableObject
    {
        private readonly PropertyRepository? _propertyRepository;
        private readonly BorrowerRepository? _borrowerRepository;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        /// <summary>
        /// 물건 탭 목록 (PDF 스타일로 나열)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PropertyTabItem> _propertyTabs = new();

        /// <summary>
        /// 현재 선택된 물건 탭
        /// </summary>
        [ObservableProperty]
        private PropertyTabItem? _selectedPropertyTab;

        /// <summary>
        /// 현재 활성 기능 탭
        /// </summary>
        [ObservableProperty]
        private string _activeTab = "BorrowerOverview";

        /// <summary>
        /// 툴박스 표시 여부
        /// </summary>
        [ObservableProperty]
        private bool _isToolBoxVisible = true;

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
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 물건 탭 초기화 (현재 프로그램의 물건들)
                if (_currentProgramId.HasValue && _propertyRepository != null)
                {
                    await LoadPropertyTabsAsync();
                }
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
        /// 프로그램 ID 설정
        /// </summary>
        public void SetProgramId(Guid programId)
        {
            _currentProgramId = programId;
        }
        
        /// <summary>
        /// 프로젝트 정보 설정 (롤업용)
        /// </summary>
        public void SetProjectInfo(string projectId, string projectName)
        {
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
        /// 물건 탭 로드
        /// </summary>
        private async Task LoadPropertyTabsAsync()
        {
            if (_propertyRepository == null || !_currentProgramId.HasValue)
                return;

            PropertyTabs.Clear();

            var properties = await _propertyRepository.GetByProgramIdAsync(_currentProgramId.Value);
            
            foreach (var property in properties.Take(10)) // 초기에는 10개만 로드
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

                // 닫힌 탭이 선택된 탭이었다면 다음 탭 선택
                if (tab.IsSelected && PropertyTabs.Any())
                {
                    var newIndex = Math.Min(index, PropertyTabs.Count - 1);
                    SelectPropertyTab(PropertyTabs[newIndex].PropertyId);
                }
            }
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
    }
}

