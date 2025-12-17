using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NPLogic.Core.Models;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// DashboardView.xaml에 대한 상호 작용 논리
    /// Phase 2 업데이트: 2영역 레이아웃, GridSplitter, 토글, 내부 탭
    /// Phase 4 업데이트: 단축키 네비게이션 추가
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private double _savedLeftPanelWidth = 280;
        
        // 내부 탭 순서 배열
        private readonly string[] _innerTabs = { "home", "noncore", "registry", "rights", "basicdata", "closing" };
        private int _currentTabIndex = 0;

        public DashboardView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loaded 이벤트 - ViewModel 초기화
        /// </summary>
        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
            
            // 키보드 포커스 설정
            this.Focus();
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
                // Enter: 선택된 물건의 비핵심 화면으로 이동
                MainWindow.Instance?.NavigateToNonCoreView(property);
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
                "home" => TabHome,
                "noncore" => TabNonCore,
                "registry" => TabRegistry,
                "rights" => TabRights,
                "basicdata" => TabBasicData,
                "closing" => TabClosing,
                _ => TabHome
            };
            
            radioButton.IsChecked = true;
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
        /// 프로그램 목록 선택 변경
        /// </summary>
        private void ProgramListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel && e.AddedItems.Count > 0)
            {
                // ViewModel에서 선택된 프로그램 데이터 로드
                viewModel.LoadSelectedProgramData();
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
        /// 내부 탭 변경
        /// </summary>
        private void InnerTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && DataContext is DashboardViewModel viewModel)
            {
                var tabName = radioButton.Name switch
                {
                    "TabHome" => "home",
                    "TabNonCore" => "noncore",
                    "TabRegistry" => "registry",
                    "TabRights" => "rights",
                    "TabBasicData" => "basicdata",
                    "TabClosing" => "closing",
                    _ => "home"
                };

                viewModel.SetActiveTab(tabName);
            }
        }

        /// <summary>
        /// 차주번호 클릭 - 비핵심 화면으로 이동 (Phase 4.2 관련)
        /// </summary>
        private void PropertyNumber_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Property property)
            {
                // MainWindow를 통해 비핵심 화면으로 이동
                MainWindow.Instance?.NavigateToNonCoreView(property);
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
    }
}
