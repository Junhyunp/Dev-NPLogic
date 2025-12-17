using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// NonCoreView.xaml에 대한 상호 작용 논리
    /// 비핵심 화면 - 9개 탭 구성
    /// Phase 4 업데이트: 단축키 네비게이션 추가
    /// </summary>
    public partial class NonCoreView : UserControl
    {
        private NonCoreViewModel? _viewModel;
        
        // 기능 탭 순서 배열
        private readonly string[] _functionTabs = 
        { 
            "BorrowerOverview", "Loan", "CollateralSummary", "CollateralProperty", 
            "SeniorRights", "Auction", "Restructuring", "CashFlow", "NPB" 
        };
        private int _currentFunctionTabIndex = 0;

        public NonCoreView()
        {
            InitializeComponent();
        }

        private async void NonCoreView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as NonCoreViewModel;
            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync();
                
                // 기본 탭 로드 (차주 개요)
                LoadFunctionContent("BorrowerOverview");
            }
            
            // 키보드 포커스 설정
            this.Focus();
        }
        
        /// <summary>
        /// 키보드 단축키 처리 (Phase 4.3)
        /// Shift+Tab: 이전 탭, Tab: 다음 탭
        /// Alt+Left/Right: 이전/다음 물건
        /// Alt+Up/Down: 이전/다음 기능 탭
        /// </summary>
        private void NonCoreView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Tab)
            {
                // Shift+Tab: 이전 기능 탭으로 이동
                NavigateToPreviousFunctionTab();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Tab && 
                     !(e.OriginalSource is TextBox)) // TextBox 내에서는 기본 동작 유지
            {
                // Tab: 다음 기능 탭으로 이동
                NavigateToNextFunctionTab();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                switch (e.SystemKey)
                {
                    case Key.Left:
                        // Alt+Left: 이전 물건으로 이동
                        _viewModel?.PreviousProperty();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        // Alt+Right: 다음 물건으로 이동
                        _viewModel?.NextProperty();
                        e.Handled = true;
                        break;
                    case Key.Up:
                        // Alt+Up: 이전 기능 탭으로 이동
                        NavigateToPreviousFunctionTab();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        // Alt+Down: 다음 기능 탭으로 이동
                        NavigateToNextFunctionTab();
                        e.Handled = true;
                        break;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D:
                        // Ctrl+D: 대시보드로 돌아가기
                        MainWindow.Instance?.NavigateToDashboard();
                        e.Handled = true;
                        break;
                }
            }
        }
        
        /// <summary>
        /// 다음 기능 탭으로 이동
        /// </summary>
        private void NavigateToNextFunctionTab()
        {
            _currentFunctionTabIndex = (_currentFunctionTabIndex + 1) % _functionTabs.Length;
            SelectFunctionTab(_functionTabs[_currentFunctionTabIndex]);
        }
        
        /// <summary>
        /// 이전 기능 탭으로 이동
        /// </summary>
        private void NavigateToPreviousFunctionTab()
        {
            _currentFunctionTabIndex = (_currentFunctionTabIndex - 1 + _functionTabs.Length) % _functionTabs.Length;
            SelectFunctionTab(_functionTabs[_currentFunctionTabIndex]);
        }
        
        /// <summary>
        /// 특정 기능 탭 선택
        /// </summary>
        private void SelectFunctionTab(string tabName)
        {
            var radioButton = tabName switch
            {
                "BorrowerOverview" => TabBorrowerOverview,
                "Loan" => TabLoan,
                "CollateralSummary" => TabCollateralSummary,
                "CollateralProperty" => TabCollateralProperty,
                "SeniorRights" => TabSeniorRights,
                "Auction" => TabAuction,
                "Restructuring" => TabRestructuring,
                "CashFlow" => TabCashFlow,
                "NPB" => TabNPB,
                _ => TabBorrowerOverview
            };
            
            radioButton.IsChecked = true;
        }
        
        /// <summary>
        /// 특정 기능 탭으로 외부에서 이동 (Phase 4.4)
        /// </summary>
        public void NavigateToTab(string tabName)
        {
            var index = Array.IndexOf(_functionTabs, tabName);
            if (index >= 0)
            {
                _currentFunctionTabIndex = index;
                SelectFunctionTab(tabName);
            }
        }

        /// <summary>
        /// 기능 탭 체크 이벤트
        /// </summary>
        private void FunctionTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string tabName)
            {
                LoadFunctionContent(tabName);
            }
        }

        /// <summary>
        /// 기능별 컨텐츠 로드
        /// </summary>
        private void LoadFunctionContent(string tabName)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null) return;

            UserControl? content = null;

            switch (tabName)
            {
                case "BorrowerOverview":
                    content = serviceProvider.GetRequiredService<BorrowerOverviewView>();
                    break;
                case "Loan":
                    content = serviceProvider.GetRequiredService<LoanDetailView>();
                    break;
                case "CollateralSummary":
                    content = serviceProvider.GetRequiredService<CollateralSummaryView>();
                    break;
                case "CollateralProperty":
                    // 담보 물건 탭 - PropertyDetailView의 담보물건 탭과 유사
                    content = CreatePlaceholder("담보 물건 탭 (추후 구현)");
                    break;
                case "SeniorRights":
                    content = serviceProvider.GetRequiredService<SeniorRightsView>();
                    break;
                case "Auction":
                    content = serviceProvider.GetRequiredService<AuctionScheduleView>();
                    break;
                case "Restructuring":
                    content = serviceProvider.GetRequiredService<RestructuringOverviewView>();
                    break;
                case "CashFlow":
                    content = serviceProvider.GetRequiredService<CashFlowSummaryView>();
                    break;
                case "NPB":
                    content = serviceProvider.GetRequiredService<XnpvComparisonView>();
                    break;
                default:
                    content = CreatePlaceholder(tabName);
                    break;
            }

            ContentArea.Content = content;
            
            // ViewModel에 현재 탭 알림
            _viewModel?.SetActiveTab(tabName);
        }

        /// <summary>
        /// 플레이스홀더 컨텐츠 생성
        /// </summary>
        private UserControl CreatePlaceholder(string name)
        {
            var placeholder = new UserControl();
            var textBlock = new TextBlock
            {
                Text = $"{name} 탭 (추후 구현)",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.5
            };
            placeholder.Content = textBlock;
            return placeholder;
        }

        /// <summary>
        /// 물건 탭 클릭
        /// </summary>
        private void PropertyTab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PropertyTabItem tabItem)
            {
                _viewModel?.SelectPropertyTab(tabItem.PropertyId);
            }
        }

        /// <summary>
        /// 물건 탭 닫기
        /// </summary>
        private void ClosePropertyTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid propertyId)
            {
                _viewModel?.ClosePropertyTab(propertyId);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 기능별 일괄작업 버튼 클릭
        /// </summary>
        private void BatchWorkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.IsOpen = true;
            }
        }

        /// <summary>
        /// 선순위만 일괄작업
        /// </summary>
        private void BatchSeniorRights_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.StartBatchWork("SeniorRights");
            TabSeniorRights.IsChecked = true;
        }

        /// <summary>
        /// 평가만 일괄작업
        /// </summary>
        private void BatchEvaluation_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.StartBatchWork("Evaluation");
        }

        /// <summary>
        /// 경공매만 일괄작업
        /// </summary>
        private void BatchAuction_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.StartBatchWork("Auction");
            TabAuction.IsChecked = true;
        }

        /// <summary>
        /// 전체 물건 일괄작업
        /// </summary>
        private void BatchAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.StartBatchWork("All");
        }

        /// <summary>
        /// 툴박스 닫기
        /// </summary>
        private void CloseToolBox_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsToolBoxVisible = false;
            }
        }

        /// <summary>
        /// 툴박스 열기
        /// </summary>
        private void OpenToolBox_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsToolBoxVisible = true;
            }
        }

        /// <summary>
        /// 빠른 계산
        /// </summary>
        private void QuickCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(QuickCalcAppraisalValue.Text.Replace(",", ""), out decimal appraisalValue))
                {
                    var discountText = QuickCalcDiscountRate.Text.Replace("%", "").Trim();
                    if (decimal.TryParse(discountText, out decimal discountRate))
                    {
                        discountRate = discountRate / 100m;
                        var result = appraisalValue * (1 - discountRate);
                        QuickCalcResult.Text = result.ToString("N0") + "원";
                    }
                }
            }
            catch
            {
                QuickCalcResult.Text = "계산 오류";
            }
        }

        /// <summary>
        /// 대법원 경매정보 열기
        /// </summary>
        private void OpenCourtAuction_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.courtauction.go.kr");
        }

        /// <summary>
        /// 온비드 공매 열기
        /// </summary>
        private void OpenOnbid_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.onbid.co.kr");
        }

        /// <summary>
        /// KB 부동산 열기
        /// </summary>
        private void OpenKBLand_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://kbland.kr");
        }

        /// <summary>
        /// 국토교통부 실거래가 열기
        /// </summary>
        private void OpenRealTransaction_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://rt.molit.go.kr");
        }

        /// <summary>
        /// URL 열기
        /// </summary>
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"링크를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 롤업 버튼 클릭 (Phase 4.5)
        /// </summary>
        private async void RollupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                var projectId = _viewModel.CurrentProjectId ?? "default";
                var projectName = _viewModel.CurrentProjectName ?? "비핵심";
                await RollupWindow.OpenRollupAsync(projectId, projectName);
            }
        }
        
        /// <summary>
        /// 상권 지도 열기 (Phase 7.1)
        /// </summary>
        private async void OpenCommercialMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var property = _viewModel?.GetCurrentProperty();
                if (property == null)
                {
                    MessageBox.Show("분석할 물건을 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 좌표가 없는 경우 안내
                if (!property.Latitude.HasValue || !property.Longitude.HasValue)
                {
                    var result = MessageBox.Show(
                        "선택한 물건의 좌표 정보가 없습니다.\n주소 기반으로 상권 분석을 진행하시겠습니까?",
                        "좌표 정보 없음",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                        return;
                }

                await CommercialDistrictMapWindow.OpenAsync(property, Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상권 지도를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

