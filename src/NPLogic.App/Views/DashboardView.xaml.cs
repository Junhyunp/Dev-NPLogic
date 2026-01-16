using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Core.Models;
using NPLogic.Services;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// DashboardView.xaml에 대한 상호 작용 논리
    /// Phase 2 업데이트: 2영역 레이아웃, GridSplitter, 토글, 내부 탭
    /// Phase 4 업데이트: 단축키 네비게이션 추가
    /// Phase 7 업데이트: 상태 유지 기능 추가
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private double _savedLeftPanelWidth = 280;
        
        // 내부 탭 순서 배열 (홈 탭 제거)
        private readonly string[] _innerTabs = { "noncore", "registry", "rights", "basicdata", "closing" };
        private int _currentTabIndex = 0;

        // 탭 View 캐싱 (탭 전환 시 상태 유지)
        private NonCoreView? _cachedNonCoreView;
        private NonCoreViewModel? _cachedNonCoreViewModel;

        // 상태 복원 중 플래그 (이벤트 중복 방지)
        private bool _isRestoringState = false;

        public DashboardView()
        {
            InitializeComponent();
            
            // Unloaded 이벤트 구독 (상태 저장)
            this.Unloaded += DashboardView_Unloaded;
        }

        /// <summary>
        /// Loaded 이벤트 - ViewModel 초기화 및 상태 복원
        /// </summary>
        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                // 물건 선택 이벤트 구독 (상세 버튼 클릭 시 상세 모드로 전환)
                viewModel.OnPropertySelected += OnPropertySelectedHandler;
                
                await viewModel.InitializeAsync();
                
                // 저장된 상태가 있으면 복원
                await RestoreNavigationStateAsync(viewModel);
                
                // 외부에서 SelectedProperty가 설정된 상태로 진입한 경우 상세 모드로 전환
                if (viewModel.SelectedProperty != null && !_isRestoringState)
                {
                    SwitchToDetailMode(viewModel.SelectedProperty);
                }
                
                // 항상 NavigationLevel에 맞게 UI 업데이트 (3단계 네비게이션)
                UpdateNavigationUI();
            }
            
            // 키보드 포커스 설정
            this.Focus();
        }

        /// <summary>
        /// Unloaded 이벤트 - 상태 저장
        /// </summary>
        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveNavigationState();
        }

        /// <summary>
        /// 현재 상태 저장
        /// </summary>
        private void SaveNavigationState()
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                var programId = viewModel.SelectedProgram?.ProjectId;
                var tabName = viewModel.ActiveTab;
                var propertyId = viewModel.SelectedProperty?.Id;
                
                NavigationStateService.Instance.SaveDashboardState(programId, tabName, propertyId);
            }
        }

        /// <summary>
        /// 저장된 상태 복원
        /// </summary>
        private async System.Threading.Tasks.Task RestoreNavigationStateAsync(DashboardViewModel viewModel)
        {
            var navService = NavigationStateService.Instance;
            
            if (!navService.HasSavedDashboardState())
                return;
                
            _isRestoringState = true;
            
            try
            {
                var savedState = navService.DashboardState;
                
                // 1. 프로그램 복원
                if (!string.IsNullOrEmpty(savedState.SelectedProgramId))
                {
                    var program = viewModel.ProgramSummaries.FirstOrDefault(p => p.ProjectId == savedState.SelectedProgramId);
                    if (program != null)
                    {
                        viewModel.SelectedProgram = program;
                        await viewModel.LoadSelectedProgramDataAsync();
                    }
                }
                
                // 2. 물건 복원 및 상세 모드 전환
                if (savedState.SelectedPropertyId.HasValue)
                {
                    var property = viewModel.DashboardProperties.FirstOrDefault(p => p.Id == savedState.SelectedPropertyId.Value);
                    if (property != null)
                    {
                        // 상세 모드로 전환
                        SwitchToDetailMode(property);
                        
                        // 3. 탭 복원
                        if (!string.IsNullOrEmpty(savedState.SelectedTab))
                        {
                            _currentTabIndex = System.Array.IndexOf(_innerTabs, savedState.SelectedTab);
                            if (_currentTabIndex < 0) _currentTabIndex = 0;
                            
                            SelectInnerTab(savedState.SelectedTab);
                        }
                    }
                }
            }
            finally
            {
                _isRestoringState = false;
            }
        }

        /// <summary>
        /// 물건 선택 이벤트 핸들러 - 상세 모드로 전환
        /// </summary>
        private void OnPropertySelectedHandler(Property property)
        {
            SwitchToDetailMode(property);
        }

        /// <summary>
        /// 목록 모드 DataGrid 선택 변경 - 상세 모드로 전환
        /// </summary>
        private void ProgressDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 상태 복원 중이면 자동 모드 전환 건너뛰기
            if (_isRestoringState)
                return;
            
            // 목록 모드에서 물건 선택 시 상세 모드로 전환
            if (e.AddedItems.Count > 0 && 
                e.AddedItems[0] is Property property &&
                DataContext is DashboardViewModel viewModel &&
                !viewModel.IsDetailMode)
            {
                SwitchToDetailMode(property);
                
                // 상태 저장
                SaveNavigationState();
            }
        }

        /// <summary>
        /// DataGrid 스크롤 변경 - 무한 스크롤 (서버 사이드 페이지네이션)
        /// 스크롤이 하단 90% 지점에 도달하면 추가 데이터 로드
        /// </summary>
        private async void ProgressDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is not DashboardViewModel viewModel)
                return;
            
            // 수직 스크롤만 처리 (수평 스크롤 무시)
            if (e.VerticalChange == 0)
                return;
            
            // 이미 로드 중이거나 더 이상 데이터가 없으면 무시
            if (viewModel.IsLoadingMore || !viewModel.HasMoreData)
                return;
            
            // 스크롤 가능한 영역이 있는지 확인
            if (e.ExtentHeight <= e.ViewportHeight)
                return;
            
            // 스크롤이 하단 90% 지점에 도달했는지 확인
            var scrollableHeight = e.ExtentHeight - e.ViewportHeight;
            var scrollPercentage = e.VerticalOffset / scrollableHeight;
            
            if (scrollPercentage >= 0.9)
            {
                // 추가 데이터 로드
                await viewModel.LoadMorePropertiesAsync();
            }
        }
        
        /// <summary>
        /// 키보드 단축키 처리 (Phase 4.3)
        /// Shift+Tab: 이전 탭, Tab: 다음 탭
        /// Alt+Left/Right: 이전/다음 프로그램
        /// Alt+Up/Down: 이전/다음 내부 탭
        /// Enter: 선택된 항목 비핵심 화면으로 이동
        /// </summary>
        private void DashboardView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Tab)
            {
                // Shift+Tab: 이전 내부 탭으로 이동
                NavigateToPreviousInnerTab();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Tab && 
                     !(e.OriginalSource is TextBox)) // TextBox 내에서는 기본 동작 유지
            {
                // Tab: 다음 내부 탭으로 이동
                NavigateToNextInnerTab();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                switch (e.SystemKey)
                {
                    case Key.Left:
                        // Alt+Left: 이전 프로그램으로 이동
                        NavigateToPreviousProgram();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        // Alt+Right: 다음 프로그램으로 이동
                        NavigateToNextProgram();
                        e.Handled = true;
                        break;
                    case Key.Up:
                        // Alt+Up: 이전 내부 탭으로 이동
                        NavigateToPreviousInnerTab();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        // Alt+Down: 다음 내부 탭으로 이동
                        NavigateToNextInnerTab();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Enter && ProgressDataGrid.SelectedItem is Property property)
            {
                // Enter: 선택된 물건의 상세 모드로 전환
                SwitchToDetailMode(property);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && DataContext is DashboardViewModel vm && vm.IsDetailMode)
            {
                // Escape: 상세 모드에서 목록 모드로 전환
                SwitchToListMode();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.R:
                        // Ctrl+R: 새로고침
                        if (DataContext is DashboardViewModel viewModel)
                        {
                            viewModel.RefreshDataCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;
                }
            }
        }
        
        /// <summary>
        /// 다음 내부 탭으로 이동
        /// </summary>
        private void NavigateToNextInnerTab()
        {
            _currentTabIndex = (_currentTabIndex + 1) % _innerTabs.Length;
            SelectInnerTab(_innerTabs[_currentTabIndex]);
        }
        
        /// <summary>
        /// 이전 내부 탭으로 이동
        /// </summary>
        private void NavigateToPreviousInnerTab()
        {
            _currentTabIndex = (_currentTabIndex - 1 + _innerTabs.Length) % _innerTabs.Length;
            SelectInnerTab(_innerTabs[_currentTabIndex]);
        }
        
        /// <summary>
        /// 특정 내부 탭 선택
        /// </summary>
        private void SelectInnerTab(string tabName)
        {
            var radioButton = tabName switch
            {
                "noncore" => TabNonCore,
                "registry" => TabRegistry,
                "rights" => TabRights,
                "basicdata" => TabBasicData,
                "closing" => TabClosing,
                _ => TabNonCore
            };
            
            radioButton.IsChecked = true;
        }
        
        /// <summary>
        /// 외부에서 호출 가능한 상세 모드 전환 (MainWindow에서 사용)
        /// </summary>
        public void SelectPropertyAndSwitchToDetail(Property property)
        {
            SwitchToDetailMode(property);
        }

        /// <summary>
        /// 상세 모드로 전환
        /// </summary>
        private void SwitchToDetailMode(Property property)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.SwitchToDetailMode(property);
                UpdateNavigationUI();
            }
            
            // 기본 탭(비핵심) 선택 및 컨텐츠 로드
            TabNonCore.IsChecked = true;
            _currentTabIndex = 0;
            LoadTabView("noncore", property);
        }
        
        /// <summary>
        /// 목록 모드로 전환 (물건 목록으로 이동)
        /// </summary>
        private void SwitchToListMode()
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.SwitchToListMode();
                UpdateNavigationUI();
            }
        }
        
        /// <summary>
        /// 목록으로 버튼 클릭
        /// </summary>
        private void BackToListButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToListMode();
            SaveNavigationState();
        }
        
        /// <summary>
        /// 다음 프로그램으로 이동
        /// </summary>
        private void NavigateToNextProgram()
        {
            if (DataContext is DashboardViewModel viewModel && viewModel.ProgramSummaries.Count > 0)
            {
                var currentIndex = viewModel.SelectedProgram != null 
                    ? viewModel.ProgramSummaries.IndexOf(viewModel.SelectedProgram) 
                    : -1;
                    
                var nextIndex = (currentIndex + 1) % viewModel.ProgramSummaries.Count;
                viewModel.SelectedProgram = viewModel.ProgramSummaries[nextIndex];
                viewModel.LoadSelectedProgramData();
            }
        }
        
        /// <summary>
        /// 이전 프로그램으로 이동
        /// </summary>
        private void NavigateToPreviousProgram()
        {
            if (DataContext is DashboardViewModel viewModel && viewModel.ProgramSummaries.Count > 0)
            {
                var currentIndex = viewModel.SelectedProgram != null 
                    ? viewModel.ProgramSummaries.IndexOf(viewModel.SelectedProgram) 
                    : 0;
                    
                var prevIndex = (currentIndex - 1 + viewModel.ProgramSummaries.Count) % viewModel.ProgramSummaries.Count;
                viewModel.SelectedProgram = viewModel.ProgramSummaries[prevIndex];
                viewModel.LoadSelectedProgramData();
            }
        }
        
        /// <summary>
        /// 롤업 버튼 클릭 (Phase 4.5)
        /// </summary>
        private async void RollupButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel && viewModel.SelectedProgram != null)
            {
                var projectId = viewModel.SelectedProgram.ProjectId;
                var projectName = viewModel.SelectedProgram.ProjectName;
                await RollupWindow.OpenRollupAsync(projectId, projectName);
            }
            else
            {
                MessageBox.Show("프로그램을 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 프로그램 목록 선택 변경 - 3단계 네비게이션: 차주 목록으로 이동
        /// </summary>
        private async void ProgramListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel && e.AddedItems.Count > 0)
            {
                // ViewModel에서 선택된 프로그램 데이터 로드 (비동기)
                await viewModel.LoadSelectedProgramDataAsync();
                
                // UI 업데이트 (차주 목록 표시)
                UpdateNavigationUI();
                
                // 상태 저장 (상태 복원 중이 아닐 때만)
                if (!_isRestoringState)
                {
                    SaveNavigationState();
                }
            }
        }

        /// <summary>
        /// 좌측 패널 토글 (접기)
        /// </summary>
        private void LeftPanelToggle_Click(object sender, RoutedEventArgs e)
        {
            if (LeftPanelToggle.IsChecked == false)
            {
                // 패널 접기
                _savedLeftPanelWidth = LeftPanelColumn.Width.Value;
                LeftPanelColumn.Width = new GridLength(0);
                LeftPanel.Visibility = Visibility.Collapsed;
                LeftPanelOpenButton.Visibility = Visibility.Visible;
            }
            else
            {
                // 패널 펼치기
                LeftPanelColumn.Width = new GridLength(_savedLeftPanelWidth);
                LeftPanel.Visibility = Visibility.Visible;
                LeftPanelOpenButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 좌측 패널 열기 (접힌 상태에서)
        /// </summary>
        private void LeftPanelOpen_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelColumn.Width = new GridLength(_savedLeftPanelWidth);
            LeftPanel.Visibility = Visibility.Visible;
            LeftPanelOpenButton.Visibility = Visibility.Collapsed;
            LeftPanelToggle.IsChecked = true;
        }

        /// <summary>
        /// 내부 탭 변경 (상세 모드에서만 동작)
        /// </summary>
        private void InnerTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && DataContext is DashboardViewModel viewModel)
            {
                var tabName = radioButton.Name switch
                {
                    "TabNonCore" => "noncore",
                    "TabRegistry" => "registry",
                    "TabRights" => "rights",
                    "TabBasicData" => "basicdata",
                    "TabClosing" => "closing",
                    _ => "noncore"
                };

                // 탭 인덱스 업데이트
                _currentTabIndex = System.Array.IndexOf(_innerTabs, tabName);
                if (_currentTabIndex < 0) _currentTabIndex = 0;

                viewModel.SetActiveTab(tabName);
                
                // 상세 모드에서만 탭 컨텐츠 전환
                if (viewModel.IsDetailMode && viewModel.SelectedProperty != null)
                {
                    LoadTabView(tabName, viewModel.SelectedProperty);
                }
                
                // 상태 저장 (상태 복원 중이 아닐 때만)
                if (!_isRestoringState)
                {
                    SaveNavigationState();
                }
            }
        }
        
        
        /// <summary>
        /// 탭에 맞는 View 로드 (상세 모드)
        /// </summary>
        private void LoadTabView(string tabName, Property property)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null) return;
            
            UserControl? view = null;
            
            switch (tabName)
            {
                case "noncore":
                    // 비핵심 탭: NonCoreView를 대시보드 내에서 표시 (피드백 반영)
                    // 탭 상태 유지를 위해 캐싱된 인스턴스 재사용
                    if (_cachedNonCoreView == null)
                    {
                        _cachedNonCoreView = serviceProvider.GetService<NonCoreView>();
                        _cachedNonCoreViewModel = serviceProvider.GetService<NonCoreViewModel>();
                    }
                    
                    if (_cachedNonCoreView != null && _cachedNonCoreViewModel != null)
                    {
                        // 프로그램 ID 설정 (물건 탭 로드를 위해 필수!)
                        if (property.ProgramId.HasValue)
                        {
                            _cachedNonCoreViewModel.SetProgramId(property.ProgramId.Value);
                        }
                        _cachedNonCoreViewModel.LoadProperty(property);
                        _cachedNonCoreView.DataContext = _cachedNonCoreViewModel;
                        view = _cachedNonCoreView;
                    }
                    break;
                    
                case "registry":
                    // 등기부 탭: RegistryTab 로드
                    var registryTab = serviceProvider.GetService<RegistryTab>();
                    if (registryTab != null)
                    {
                        var registryViewModel = serviceProvider.GetService<RegistryTabViewModel>();
                        if (registryViewModel != null)
                        {
                            registryViewModel.SetPropertyId(property.Id);
                            registryViewModel.SetPropertyInfo(property);
                            _ = registryViewModel.LoadDataAsync();
                            registryTab.DataContext = new { RegistryViewModel = registryViewModel };
                        }
                        view = registryTab;
                    }
                    break;
                    
                case "rights":
                    // 권리분석 탭: RightsAnalysisTab 로드
                    var rightsTab = serviceProvider.GetService<RightsAnalysisTab>();
                    if (rightsTab != null)
                    {
                        var rightsViewModel = serviceProvider.GetService<RightsAnalysisTabViewModel>();
                        if (rightsViewModel != null)
                        {
                            rightsViewModel.SetPropertyId(property.Id);
                            rightsViewModel.SetProperty(property);
                            _ = rightsViewModel.LoadDataAsync();
                            rightsTab.DataContext = rightsViewModel;
                        }
                        view = rightsTab;
                    }
                    break;
                    
                case "basicdata":
                    // 기초데이터 탭: ProgramSettingsTab 로드 (프로그램 레벨 설정 화면)
                    // 참고: 물건별 담보물건 정보는 PropertyDetailView의 BasicDataTab에서 관리
                    var programSettingsTab = serviceProvider.GetService<ProgramSettingsTab>();
                    if (programSettingsTab != null)
                    {
                        view = programSettingsTab;
                    }
                    break;
                    
                case "closing":
                    // 마감 탭: ClosingTab 로드
                    var closingTab = serviceProvider.GetService<ClosingTab>();
                    if (closingTab != null)
                    {
                        closingTab.DataContext = new { Property = property };
                        view = closingTab;
                    }
                    break;
            }
            
            if (view != null)
            {
                TabContentControl.Content = view;
            }
        }

        /// <summary>
        /// 차주번호 클릭 - 상세 모드로 전환 및 프로그램 목록 자동 접기
        /// </summary>
        private void PropertyNumber_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Property property)
            {
                SwitchToDetailMode(property);
                
                // 프로그램 목록 자동 접기
                CollapseLeftPanel();
            }
        }
        
        /// <summary>
        /// 좌측 패널(프로그램 목록) 접기
        /// </summary>
        private void CollapseLeftPanel()
        {
            if (LeftPanel.Visibility == Visibility.Visible)
            {
                _savedLeftPanelWidth = LeftPanelColumn.Width.Value > 0 ? LeftPanelColumn.Width.Value : _savedLeftPanelWidth;
                LeftPanelColumn.Width = new GridLength(0);
                LeftPanel.Visibility = Visibility.Collapsed;
                LeftPanelOpenButton.Visibility = Visibility.Visible;
                LeftPanelToggle.IsChecked = false;
            }
        }

        /// <summary>
        /// 통계 카드 클릭 - 상태 필터링
        /// </summary>
        private void StatCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string status)
            {
                if (DataContext is DashboardViewModel viewModel)
                {
                    viewModel.FilterByStatus(status);
                }
            }
        }

        /// <summary>
        /// 상태 필터 버튼 클릭 - 상태별 필터링
        /// </summary>
        private void StatusFilter_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string status)
            {
                if (DataContext is DashboardViewModel viewModel)
                {
                    viewModel.FilterByStatus(status);
                    UpdateFilterButtonStyles(status);
                }
            }
        }

        /// <summary>
        /// F-001: 소유자 전입 필터 콤보박스 변경
        /// </summary>
        private void OwnerMoveInFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && DataContext is DashboardViewModel viewModel)
            {
                if (comboBox.SelectedItem is ComboBoxItem item)
                {
                    var tag = item.Tag?.ToString();
                    if (string.IsNullOrEmpty(tag))
                    {
                        viewModel.FilterOwnerMoveIn = null;
                    }
                    else if (bool.TryParse(tag, out var value))
                    {
                        viewModel.FilterOwnerMoveIn = value;
                    }
                }
            }
        }

        /// <summary>
        /// 필터 버튼 스타일 업데이트 - 선택된 버튼 강조
        /// </summary>
        private void UpdateFilterButtonStyles(string selectedStatus)
        {
            // 모든 필터 버튼을 기본 스타일로 리셋
            var defaultBrush = FindResource("TextSecondaryBrush") as System.Windows.Media.Brush;
            var primaryBrush = FindResource("PrimaryBrush") as System.Windows.Media.Brush;

            FilterAll.Foreground = defaultBrush;
            FilterAll.FontWeight = FontWeights.Normal;
            FilterPending.Foreground = defaultBrush;
            FilterPending.FontWeight = FontWeights.Normal;
            FilterProcessing.Foreground = defaultBrush;
            FilterProcessing.FontWeight = FontWeights.Normal;
            FilterCompleted.Foreground = defaultBrush;
            FilterCompleted.FontWeight = FontWeights.Normal;

            // 선택된 버튼 강조
            TextBlock selectedButton = selectedStatus switch
            {
                "all" => FilterAll,
                "pending" => FilterPending,
                "processing" => FilterProcessing,
                "completed" => FilterCompleted,
                _ => FilterAll
            };

            selectedButton.Foreground = primaryBrush;
            selectedButton.FontWeight = FontWeights.SemiBold;
        }

        /// <summary>
        /// 진행 체크박스 클릭 - 즉시 저장
        /// </summary>
        private async void ProgressCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string fieldName)
            {
                var property = checkBox.DataContext as Property;
                if (property != null && DataContext is DashboardViewModel viewModel)
                {
                    await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, checkBox.IsChecked ?? false);
                    // 진행률 다시 계산
                    viewModel.RecalculateColumnProgress();
                }
            }
        }

        /// <summary>
        /// 경개개시여부 콤보박스 변경 - 즉시 저장
        /// </summary>
        private async void AuctionStartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string fieldName)
            {
                var property = comboBox.DataContext as Property;
                if (property != null && DataContext is DashboardViewModel viewModel && e.AddedItems.Count > 0)
                {
                    var selectedItem = e.AddedItems[0];
                    string? value = null;
                    
                    if (selectedItem is ComboBoxItem item)
                    {
                        value = item.Content?.ToString();
                    }
                    else
                    {
                        value = selectedItem?.ToString();
                    }
                    
                    await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, value ?? "");
                }
            }
        }

        /// <summary>
        /// Assign 콤보박스 변경 - 담당자 할당
        /// </summary>
        private async void AssignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                var property = comboBox.DataContext as Property;
                if (property != null && DataContext is DashboardViewModel viewModel && comboBox.SelectedValue != null)
                {
                    var userId = (System.Guid)comboBox.SelectedValue;
                    await viewModel.AssignToUserAsync(property.Id, userId);
                }
            }
        }

        #region 브레드크럼 클릭 핸들러

        /// <summary>
        /// 브레드크럼 - 홈 클릭 (프로그램 선택 화면으로 이동)
        /// </summary>
        private void Breadcrumb_Home_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                // 물건 목록으로 이동
                viewModel.NavigateBackToPropertyList();
                UpdateNavigationUI();
            }
        }

        /// <summary>
        /// 브레드크럼 - 프로그램 클릭 (물건 목록으로 이동)
        /// </summary>
        private void Breadcrumb_Program_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                // 물건 목록으로 이동
                viewModel.NavigateBackToPropertyList();
                UpdateNavigationUI();
            }
        }

        /// <summary>
        /// 브레드크럼 - 탭 클릭 (해당 탭으로 이동, 물건은 유지)
        /// </summary>
        private void Breadcrumb_Tab_Click(object sender, MouseButtonEventArgs e)
        {
            // 현재 탭 유지 (이미 해당 탭에 있으므로 아무 동작 안 함)
            // 필요 시 탭 재로드 로직 추가 가능
        }

        #endregion

        #region 상세 모드 물건 리스트 핸들러

        /// <summary>
        /// 물건 리스트 뒤로가기 버튼 클릭 (상세 모드에서 목록으로)
        /// </summary>
        private void PropertyListBackButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToListMode();
        }

        /// <summary>
        /// 좌측 패널 물건 리스트 선택 변경 - 상세 화면 전환
        /// </summary>
        private void PropertySideListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRestoringState) return;

            if (e.AddedItems.Count > 0 && 
                e.AddedItems[0] is Property property && 
                DataContext is DashboardViewModel viewModel &&
                viewModel.IsDetailMode)
            {
                // 이미 상세 모드이므로 선택된 물건만 변경
                viewModel.SelectPropertyInDetailMode(property);
                
                // 탭 컨텐츠 업데이트
                LoadTabView(viewModel.ActiveTab, property);
            }
        }

        #endregion

        #region 간소화된 네비게이션 지원

        /// <summary>
        /// NavigationLevel에 따라 UI 업데이트
        /// 간소화된 네비게이션: Property와 Detail 모드만 사용
        /// </summary>
        private void UpdateNavigationUI()
        {
            if (DataContext is not DashboardViewModel viewModel) return;

            switch (viewModel.NavigationLevel)
            {
                case "Detail":
                    // 상세 모드 표시
                    ProgressDataGrid.Visibility = Visibility.Collapsed;
                    DetailModeTabs.Visibility = Visibility.Visible;
                    TabContentControl.Visibility = Visibility.Visible;
                    
                    // 좌측 패널: 물건 리스트 표시 및 패널 열기
                    ProgramListPanel.Visibility = Visibility.Collapsed;
                    PropertyListPanel.Visibility = Visibility.Visible;
                    
                    // 좌측 패널 자동 열기 (상세 모드 진입 시)
                    if (LeftPanel.Visibility == Visibility.Collapsed)
                    {
                        LeftPanelColumn.Width = new GridLength(_savedLeftPanelWidth > 0 ? _savedLeftPanelWidth : 280);
                        LeftPanel.Visibility = Visibility.Visible;
                        LeftPanelOpenButton.Visibility = Visibility.Collapsed;
                    }
                    break;
                    
                default: // "Property" 및 기타
                    // 물건 목록 표시
                    ProgressDataGrid.Visibility = Visibility.Visible;
                    ProgressDataGrid.SelectedItem = null;
                    DetailModeTabs.Visibility = Visibility.Collapsed;
                    TabContentControl.Visibility = Visibility.Collapsed;
                    
                    // 좌측 패널: 프로그램 목록 표시
                    ProgramListPanel.Visibility = Visibility.Visible;
                    PropertyListPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        #endregion
    }
}
