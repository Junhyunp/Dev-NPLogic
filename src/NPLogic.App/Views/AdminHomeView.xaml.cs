using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Core.Models;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 통합 홈 화면 - 마스터-디테일 레이아웃 + 상세 모드 전환
    /// Admin/PM/Evaluator 모든 역할에서 사용
    /// </summary>
    public partial class AdminHomeView : UserControl
    {
        private double _savedLeftPanelWidth = 300;
        
        // NonCoreView 캐싱 (탭 상태 유지)
        private NonCoreView? _cachedNonCoreView;
        private NonCoreViewModel? _cachedNonCoreViewModel;
        
        // 현재 선택된 물건 (상세 모드)
        private Property? _selectedProperty;
        

        public AdminHomeView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 좌측 패널 접기/펼치기
        /// </summary>
        private void LeftPanelToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                if (toggleButton.IsChecked == false)
                {
                    // 패널 접기
                    _savedLeftPanelWidth = LeftPanelColumn.Width.Value;
                    LeftPanelColumn.Width = new GridLength(0);
                    LeftPanel.Visibility = Visibility.Collapsed;
                    LeftPanelOpenButton.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 좌측 패널 열기
        /// </summary>
        private void LeftPanelOpen_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelColumn.Width = new GridLength(_savedLeftPanelWidth > 0 ? _savedLeftPanelWidth : 300);
            LeftPanel.Visibility = Visibility.Visible;
            LeftPanelOpenButton.Visibility = Visibility.Collapsed;
            LeftPanelToggle.IsChecked = true;
        }

        private async void AdminHomeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminHomeViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// 담당자 할당 변경
        /// </summary>
        private async void AssignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.DataContext is Property property &&
                comboBox.SelectedValue is System.Guid userId &&
                DataContext is AdminHomeViewModel viewModel)
            {
                await viewModel.AssignToUserAsync(property.Id, userId);
            }
        }

        /// <summary>
        /// 진행 체크박스 클릭
        /// </summary>
        private async void ProgressCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is Property property &&
                checkBox.Tag is string fieldName &&
                DataContext is AdminHomeViewModel viewModel)
            {
                await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, checkBox.IsChecked ?? false);
            }
        }

        /// <summary>
        /// 경개개시여부 콤보박스 변경
        /// </summary>
        private async void AuctionStartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.DataContext is Property property &&
                comboBox.Tag is string fieldName &&
                comboBox.SelectedItem is ComboBoxItem selectedItem &&
                DataContext is AdminHomeViewModel viewModel)
            {
                var value = selectedItem.Content?.ToString() ?? "";
                await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, value);
            }
        }

        /// <summary>
        /// 차주번호/차주명 클릭 - 상세 모드로 전환 (우측 영역만 변경)
        /// 피드백 #11 반영: Header 유지 + 내부만 변경
        /// </summary>
        private void PropertyNumber_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && 
                element.DataContext is Property property)
            {
                SwitchToDetailMode(property);
            }
        }
        
        #region 목록/상세 모드 전환
        
        /// <summary>
        /// 외부에서 호출 가능한 상세 모드 전환 (MainWindow에서 사용)
        /// </summary>
        public void SelectPropertyAndSwitchToDetail(Property property)
        {
            SwitchToDetailMode(property);
        }
        
        /// <summary>
        /// 상세 모드로 전환 (우측 영역만 NonCoreView로 교체)
        /// </summary>
        private void SwitchToDetailMode(Property property)
        {
            _selectedProperty = property;
            
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null) return;
            
            // NonCoreView 캐싱
            if (_cachedNonCoreView == null)
            {
                _cachedNonCoreView = serviceProvider.GetService<NonCoreView>();
                _cachedNonCoreViewModel = serviceProvider.GetService<NonCoreViewModel>();
            }
            
            if (_cachedNonCoreView != null && _cachedNonCoreViewModel != null)
            {
                // 프로그램 정보 전달 및 프로그램 ID 설정
                if (DataContext is AdminHomeViewModel viewModel && viewModel.SelectedProgram != null)
                {
                    _cachedNonCoreViewModel.SetProjectInfo(
                        viewModel.SelectedProgram.Id.ToString(), 
                        viewModel.SelectedProgram.ProgramName);
                    
                    // 프로그램 ID 설정 (물건 탭 로드를 위해 필수!)
                    _cachedNonCoreViewModel.SetProgramId(viewModel.SelectedProgram.Id);
                }
                
                // 물건 로드
                _cachedNonCoreViewModel.LoadProperty(property);
                
                // DataContext 설정 및 초기화
                _cachedNonCoreView.DataContext = _cachedNonCoreViewModel;
                
                // ContentControl에 NonCoreView 설정
                DetailContentControl.Content = _cachedNonCoreView;
            }
            
            // UI 업데이트: 목록 숨기고 상세 표시
            PropertiesDataGrid.Visibility = Visibility.Collapsed;
            StatisticsSummary.Visibility = Visibility.Collapsed;
            DetailModeTabs.Visibility = Visibility.Visible;
            DetailContentControl.Visibility = Visibility.Visible;
            BackToListButton.Visibility = Visibility.Visible;
            
            // 기본 탭(비핵심) 선택
            TabNonCore.IsChecked = true;
            LoadTabView("noncore");
        }
        
        /// <summary>
        /// 목록 모드로 전환
        /// </summary>
        private void SwitchToListMode()
        {
            _selectedProperty = null;
            
            // UI 업데이트: 상세 숨기고 목록 표시
            DetailContentControl.Visibility = Visibility.Collapsed;
            DetailModeTabs.Visibility = Visibility.Collapsed;
            BackToListButton.Visibility = Visibility.Collapsed;
            PropertiesDataGrid.Visibility = Visibility.Visible;
            StatisticsSummary.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// 목록으로 버튼 클릭
        /// </summary>
        private void BackToListButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToListMode();
        }
        
        /// <summary>
        /// 상세 모드 탭 변경
        /// </summary>
        private void DetailTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string tabName)
            {
                LoadTabView(tabName);
            }
        }
        
        /// <summary>
        /// 탭별 뷰 로드
        /// </summary>
        private void LoadTabView(string tabName)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null || _selectedProperty == null) return;
            
            switch (tabName)
            {
                case "noncore":
                    // NonCoreView 표시
                    if (_cachedNonCoreView != null)
                    {
                        DetailContentControl.Content = _cachedNonCoreView;
                    }
                    break;
                    
                case "registry":
                    // 등기부등본 탭 - RegistryTab + RegistryTabViewModel 로드
                    // RegistryTab.xaml은 RegistryViewModel.* 바인딩을 사용하므로 래퍼 필요
                    var registryTab = serviceProvider.GetService<RegistryTab>();
                    var registryVm = serviceProvider.GetService<ViewModels.RegistryTabViewModel>();
                    if (registryTab != null && registryVm != null && _selectedProperty != null)
                    {
                        registryVm.SetPropertyId(_selectedProperty.Id);
                        registryVm.SetPropertyInfo(_selectedProperty);
                        
                        // 바인딩 경로 호환을 위한 동적 래퍼 사용
                        dynamic wrapper = new System.Dynamic.ExpandoObject();
                        wrapper.RegistryViewModel = registryVm;
                        registryTab.DataContext = wrapper;
                        
                        DetailContentControl.Content = registryTab;
                        // 데이터 로드
                        _ = registryVm.LoadDataAsync();
                    }
                    break;
                    
                case "rights":
                    // 권리분석 탭 - RightsAnalysisTab + RightsAnalysisTabViewModel 로드
                    var rightsTab = serviceProvider.GetService<RightsAnalysisTab>();
                    var rightsVm = serviceProvider.GetService<ViewModels.RightsAnalysisTabViewModel>();
                    if (rightsTab != null && rightsVm != null && _selectedProperty != null)
                    {
                        rightsVm.SetPropertyId(_selectedProperty.Id);
                        rightsTab.DataContext = rightsVm;
                        DetailContentControl.Content = rightsTab;
                        // 데이터 로드
                        _ = rightsVm.LoadDataAsync();
                    }
                    break;
                    
                case "basicdata":
                    // 기초데이터 탭 - ProgramSettingsTab 로드 (프로그램 레벨 설정 화면)
                    // 참고: 물건별 담보물건 정보는 NonCoreView의 "담보물건" 탭에서 관리
                    var programSettingsView = serviceProvider.GetService<ProgramSettingsTab>();
                    if (programSettingsView != null)
                    {
                        DetailContentControl.Content = programSettingsView;
                    }
                    break;
                    
                case "closing":
                    // 마감 탭 - ClosingTab 로드
                    var closingTab = serviceProvider.GetService<ClosingTab>();
                    if (closingTab != null)
                    {
                        DetailContentControl.Content = closingTab;
                    }
                    break;
            }
        }
        
        #endregion

        /// <summary>
        /// 필터 뱃지 클릭 - 해당 상태만 필터링 (단일 클릭)
        /// </summary>
        private void FilterBadge_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element &&
                element.Tag is string filterType &&
                DataContext is AdminHomeViewModel viewModel)
            {
                viewModel.FilterByStatus(filterType);
                UpdateFilterButtonStyles(filterType);
            }
        }
        
        /// <summary>
        /// 필터 버튼 선택 스타일 업데이트
        /// </summary>
        private void UpdateFilterButtonStyles(string selectedFilter)
        {
            // 모든 필터 버튼의 스타일 초기화 후 선택된 버튼만 하이라이트
            var filterButtons = new[] { FilterAll, FilterPending, FilterProcessing, FilterCompleted };
            
            foreach (var button in filterButtons)
            {
                if (button == null) continue;
                
                bool isSelected = button.Tag?.ToString() == selectedFilter;
                button.BorderThickness = isSelected ? new Thickness(2) : new Thickness(0);
                button.BorderBrush = isSelected ? System.Windows.Media.Brushes.White : null;
            }
        }
    }
}

