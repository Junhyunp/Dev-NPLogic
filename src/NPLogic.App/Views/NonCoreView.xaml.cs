using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NPLogic.Services;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// NonCoreView.xaml에 대한 상호 작용 논리
    /// 비핵심 화면 - 10개 탭 구성 (전체, 차주개요, Loan, 담보물건, 선순위, 회생개요, 평가, 경(공)매일정, 현금흐름, XNPV비교)
    /// Phase 4 업데이트: 단축키 네비게이션 추가
    /// </summary>
    public partial class NonCoreView : UserControl
    {
        private NonCoreViewModel? _viewModel;
        
        // 기능 탭 순서 배열 - 피드백 반영: 10개 탭
        private readonly string[] _functionTabs = 
        { 
            "Home", "BorrowerOverview", "Loan", "CollateralProperty", 
            "SeniorRights", "Restructuring", "Evaluation", "AuctionSchedule", 
            "CashFlow", "XnpvComparison" 
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
                
                // 기본 탭 로드 (전체)
                await LoadFunctionContentAsync("Home");
            }
            
            // 키보드 포커스 설정
            this.Focus();
        }
        
        /// <summary>
        /// 키보드 단축키 처리
        /// Tab: 다음 기능 탭, Shift+Tab: 이전 기능 탭
        /// Alt+Up/Down: 이전/다음 차주 (상하 이동)
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
                    case Key.Up:
                        // Alt+Up: 이전 차주로 이동 (상하 이동)
                        _viewModel?.PreviousBorrower();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        // Alt+Down: 다음 차주로 이동 (상하 이동)
                        _viewModel?.NextBorrower();
                        e.Handled = true;
                        break;
                    case Key.Left:
                        // Alt+Left: 이전 기능 탭으로 이동
                        NavigateToPreviousFunctionTab();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        // Alt+Right: 다음 기능 탭으로 이동
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
                    case Key.F:
                        // Ctrl+F: 검색창에 포커스
                        FocusSearchBox();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Escape)
            {
                // Esc: 검색어 초기화
                if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.SearchText))
                {
                    _viewModel.SearchText = "";
                    _viewModel.ApplyFilters();
                    e.Handled = true;
                }
            }
        }
        
        /// <summary>
        /// 검색창에 포커스
        /// </summary>
        private void FocusSearchBox()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                BorrowerSearchBox?.Focus();
                BorrowerSearchBox?.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
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
                "Home" => TabHome,
                "BorrowerOverview" => TabBorrowerOverview,
                "Loan" => TabLoan,
                "CollateralProperty" => TabCollateralProperty,
                "SeniorRights" => TabSeniorRights,
                "Restructuring" => TabRestructuring,
                "Evaluation" => TabEvaluation,
                "AuctionSchedule" => TabAuctionSchedule,
                "CashFlow" => TabCashFlow,
                "XnpvComparison" => TabXnpvComparison,
                _ => TabHome
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
        /// 현재 활성 탭의 컨텐츠를 새로고침 (외부에서 호출 가능)
        /// </summary>
        public async Task RefreshCurrentTabAsync()
        {
            // RadioButton UI 상태를 기준으로 현재 선택된 탭 확인
            var currentTab = GetCurrentSelectedTabFromUI();
            
            // #region agent log
            System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:RefreshCurrentTabAsync",message="RefreshCurrentTabAsync called",data=new{currentTabFromUI=currentTab,activeTabFromVM=_viewModel?.ActiveTab,selectedPropertyTab=_viewModel?.SelectedPropertyTab?.PropertyNumber},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="FIX"})+"\n");
            // #endregion
            
            if (!string.IsNullOrEmpty(currentTab))
            {
                await LoadFunctionContentAsync(currentTab);
            }
        }

        /// <summary>
        /// 현재 UI에서 선택된 탭을 RadioButton 상태로 확인
        /// </summary>
        private string? GetCurrentSelectedTabFromUI()
        {
            if (TabHome.IsChecked == true) return "Home";
            if (TabBorrowerOverview.IsChecked == true) return "BorrowerOverview";
            if (TabLoan.IsChecked == true) return "Loan";
            if (TabCollateralProperty.IsChecked == true) return "CollateralProperty";
            if (TabSeniorRights.IsChecked == true) return "SeniorRights";
            if (TabRestructuring.IsChecked == true) return "Restructuring";
            if (TabEvaluation.IsChecked == true) return "Evaluation";
            if (TabAuctionSchedule.IsChecked == true) return "AuctionSchedule";
            if (TabCashFlow.IsChecked == true) return "CashFlow";
            if (TabXnpvComparison.IsChecked == true) return "XnpvComparison";
            return "Home"; // 기본값
        }

        /// <summary>
        /// 기능 탭 체크 이벤트
        /// </summary>
        private async void FunctionTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string tabName)
            {
                await LoadFunctionContentAsync(tabName);
            }
        }

        /// <summary>
        /// 기능별 컨텐츠 로드 - 피드백 반영: 10개 탭 구조
        /// </summary>
        private async Task LoadFunctionContentAsync(string tabName)
        {
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider == null) return;

            UserControl? content = null;
            
            // 현재 선택된 물건 ID 가져오기
            var selectedPropertyId = _viewModel?.SelectedPropertyTab?.PropertyId;

            switch (tabName)
            {
                case "Home":
                    // #region agent log
                    System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:LoadFunctionContentAsync:Home",message="Home tab loading",data=new{selectedPropertyId=selectedPropertyId?.ToString(),selectedPropertyTabNumber=_viewModel?.SelectedPropertyTab?.PropertyNumber},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="E"})+"\n");
                    // #endregion
                    
                    content = serviceProvider.GetRequiredService<HomeTab>();
                    // PropertyDetailViewModel 항상 설정 (커맨드 바인딩을 위해)
                    {
                        var vm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        if (selectedPropertyId.HasValue)
                        {
                            // #region agent log
                            System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:LoadFunctionContentAsync:Home",message="Setting PropertyId and initializing",data=new{propertyId=selectedPropertyId.Value.ToString()},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="E"})+"\n");
                            // #endregion
                            
                            vm.SetPropertyId(selectedPropertyId.Value);
                            _ = vm.InitializeAsync();
                        }
                        else
                        {
                            // #region agent log
                            System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:LoadFunctionContentAsync:Home",message="selectedPropertyId is null - NOT initializing VM",data=new{},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="E"})+"\n");
                            // #endregion
                        }
                        content.DataContext = vm;
                    }
                    break;
                case "BorrowerOverview":
                    // 차주개요 탭 - 선택된 물건의 차주 정보만 표시
                    content = serviceProvider.GetRequiredService<BorrowerOverviewView>();
                    {
                        var borrowerVm = serviceProvider.GetRequiredService<BorrowerOverviewViewModel>();
                        
                        // 선택된 물건 정보가 있으면 단일 차주 모드로 설정
                        if (_viewModel?.SelectedPropertyTab != null)
                        {
                            Debug.WriteLine($"[NonCoreView] BorrowerOverview - SelectedPropertyTab: {_viewModel.SelectedPropertyTab.PropertyNumber}");
                            
                            var property = await _viewModel.GetCurrentPropertyAsync();
                            if (property != null)
                            {
                                Debug.WriteLine($"[NonCoreView] BorrowerOverview - Property: {property.PropertyNumber}, BorrowerNumber: {property.BorrowerNumber}, DebtorName: {property.DebtorName}");
                                await borrowerVm.SetSelectedPropertyAsync(property);
                            }
                            else
                            {
                                Debug.WriteLine("[NonCoreView] BorrowerOverview - Property is null!");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[NonCoreView] BorrowerOverview - SelectedPropertyTab is null");
                        }
                        
                        content.DataContext = borrowerVm;
                    }
                    break;
                case "Loan":
                    // Loan 시트 탭 - 4개 시트 (일반, 일반보증, 해지부보증, 일반+해지부보증)
                    content = serviceProvider.GetRequiredService<Loan.LoanSheetView>();
                    {
                        var loanSheetVm = serviceProvider.GetRequiredService<LoanSheetViewModel>();
                        
                        // 선택된 물건 정보가 있으면 단일 차주 모드로 설정
                        if (_viewModel?.SelectedPropertyTab != null)
                        {
                            var property = await _viewModel.GetCurrentPropertyAsync();
                            if (property != null)
                            {
                                await loanSheetVm.SetSelectedPropertyAsync(property);
                            }
                            else
                            {
                                // 물건 정보 로드 실패 시에도 초기화
                                await loanSheetVm.InitializeAsync();
                            }
                        }
                        else
                        {
                            // 선택된 물건이 없으면 전체 차주 모드로 초기화
                            await loanSheetVm.InitializeAsync();
                        }
                        
                        content.DataContext = loanSheetVm;
                    }
                    break;
                case "CollateralProperty":
                    // 담보물건 탭 - 물건 기본 정보, 등기부등본 정보, 감정평가 정보를 테이블 형태로 표시
                    // 피드백 반영: 위성도/지적도/로드뷰/토지이용계획/건축물대장 버튼을 한 행에 배치
                    content = serviceProvider.GetRequiredService<CollateralPropertyView>();
                    {
                        var vm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        if (selectedPropertyId.HasValue)
                        {
                            vm.SetPropertyId(selectedPropertyId.Value);
                            await vm.InitializeAsync();
                        }
                        content.DataContext = vm;
                    }
                    break;
                case "SeniorRights":
                    content = serviceProvider.GetRequiredService<SeniorRightsView>();
                    break;
                case "Restructuring":
                    // 회생개요 탭 - 회생차주인 경우에만 표시
                    content = serviceProvider.GetRequiredService<RestructuringOverviewView>();
                    break;
                case "Evaluation":
                    // 평가 탭 - 추천 시스템 포함
                    content = serviceProvider.GetRequiredService<EvaluationTab>();
                    // EvaluationViewModel을 DataContext로 설정 (EvaluationTab.xaml의 바인딩에 맞춤)
                    {
                        var parentVm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        // 먼저 PropertyDetailViewModel 초기화 (EvaluationViewModel 생성을 위해)
                        if (selectedPropertyId.HasValue)
                        {
                            parentVm.SetPropertyId(selectedPropertyId.Value);
                            _ = parentVm.InitializeAsync();
                        }
                        
                        if (parentVm.EvaluationViewModel != null)
                        {
                            // 선택된 물건 ID 및 Property 정보 설정
                            if (selectedPropertyId.HasValue)
                            {
                                parentVm.EvaluationViewModel.SetPropertyId(selectedPropertyId.Value);
                                // Property 정보도 설정 (지역명 추출 등에 필요)
                                if (parentVm.Property != null)
                                {
                                    parentVm.EvaluationViewModel.SetProperty(parentVm.Property);
                                }
                            }
                            content.DataContext = parentVm.EvaluationViewModel;
                            // 데이터 로드 (백그라운드에서)
                            _ = parentVm.EvaluationViewModel.LoadAsync();
                        }
                        else
                        {
                            Debug.WriteLine("[NonCoreView] EvaluationViewModel is null! PropertyDetailViewModel may not be initialized.");
                            content.DataContext = null;
                        }
                    }
                    break;
                case "AuctionSchedule":
                    // 경(공)매일정 상세 화면 (산출화면 기반)
                    content = new AuctionScheduleDetailView();
                    {
                        var auctionParentVm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        // 먼저 PropertyDetailViewModel 초기화 (AuctionScheduleDetailViewModel 생성을 위해)
                        if (selectedPropertyId.HasValue)
                        {
                            auctionParentVm.SetPropertyId(selectedPropertyId.Value);
                            _ = auctionParentVm.InitializeAsync();
                        }
                        
                        if (auctionParentVm.AuctionScheduleDetailViewModel != null)
                        {
                            // 선택된 물건 ID 설정
                            if (selectedPropertyId.HasValue)
                            {
                                auctionParentVm.AuctionScheduleDetailViewModel.SetPropertyId(selectedPropertyId.Value);
                            }
                            content.DataContext = auctionParentVm.AuctionScheduleDetailViewModel;
                        }
                        else
                        {
                            Debug.WriteLine("[NonCoreView] AuctionScheduleDetailViewModel is null!");
                            content.DataContext = null;
                        }
                    }
                    break;
                case "CashFlow":
                    content = serviceProvider.GetRequiredService<CashFlowSummaryView>();
                    break;
                case "XnpvComparison":
                    // XNPV 비교 탭
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
        /// 차주 리스트 선택 변경
        /// </summary>
        private async void BorrowerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // #region agent log
            System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="SelectionChanged fired",data=new{senderType=sender?.GetType().Name,addedCount=e.AddedItems.Count,removedCount=e.RemovedItems.Count},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="A"})+"\n");
            // #endregion
            
            if (sender is ListBox listBox && listBox.SelectedItem is BorrowerListItem selectedItem)
            {
                // #region agent log
                System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="BorrowerListItem selected",data=new{borrowerId=selectedItem.BorrowerId.ToString(),borrowerName=selectedItem.BorrowerName,activeTab=_viewModel?.ActiveTab},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="B"})+"\n");
                // #endregion
                
                // 비동기로 차주 선택 및 물건 목록 로드 완료까지 대기
                if (_viewModel != null)
                {
                    await _viewModel.SelectBorrowerAsync(selectedItem.BorrowerId);
                    
                    // #region agent log
                    System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="SelectBorrowerAsync completed",data=new{selectedPropertyTab=_viewModel.SelectedPropertyTab?.PropertyNumber,propertyTabsCount=_viewModel.PropertyTabs?.Count},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="C"})+"\n");
                    // #endregion
                }
                
                // 물건 목록 로드 완료 후 현재 탭 컨텐츠 새로고침
                if (_viewModel?.ActiveTab != null)
                {
                    // #region agent log
                    System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="Calling LoadFunctionContentAsync",data=new{activeTab=_viewModel.ActiveTab},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="D"})+"\n");
                    // #endregion
                    
                    await LoadFunctionContentAsync(_viewModel.ActiveTab);
                    
                    // #region agent log
                    System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="LoadFunctionContentAsync completed",data=new{contentAreaContent=ContentArea.Content?.GetType().Name},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="E"})+"\n");
                    // #endregion
                }
                else
                {
                    // #region agent log
                    System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="ActiveTab is null, skipping LoadFunctionContentAsync",data=new{viewModelNull=_viewModel==null,activeTab=_viewModel?.ActiveTab},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="D"})+"\n");
                    // #endregion
                }
            }
            else
            {
                // #region agent log
                System.IO.File.AppendAllText(@"c:\Users\pwm89\dev\nplogic\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new{location="NonCoreView.xaml.cs:BorrowerListBox_SelectionChanged",message="Not BorrowerListItem or no selection",data=new{selectedItemType=((sender as ListBox)?.SelectedItem)?.GetType().Name},timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),sessionId="debug-session",hypothesisId="B"})+"\n");
                // #endregion
            }
        }

        /// <summary>
        /// 차주 검색 입력
        /// </summary>
        private void BorrowerSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.Key == Key.Enter)
            {
                // Enter: 필터 적용 후 다음 찾기
                _viewModel.ApplyFilters();
                if (_viewModel.BorrowerItems.Any())
                {
                    _viewModel.SelectBorrower(_viewModel.BorrowerItems.First().BorrowerId);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Esc: 검색어 초기화
                _viewModel.SearchText = "";
                _viewModel.ClearFilters();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                // Down: 리스트로 포커스 이동
                BorrowerListBox?.Focus();
                e.Handled = true;
            }
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
                // 비동기 버전 사용으로 UI 스레드 deadlock 방지
                var property = await _viewModel?.GetCurrentPropertyAsync()!;
                
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

        // ========== 인쇄/Excel 이벤트 핸들러 ==========

        /// <summary>
        /// 인쇄 버튼 클릭 - 차주 선택 다이얼로그 표시
        /// </summary>
        private async void PrintWithSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null || !_viewModel.BorrowerItems.Any())
                {
                    MessageBox.Show("인쇄할 차주가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 차주 선택 다이얼로그 표시
                var dialog = BorrowerSelectionDialog.CreateForPrint(_viewModel.BorrowerItems);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    var selectedBorrowers = dialog.SelectedBorrowers;
                    if (selectedBorrowers.Count == 0)
                    {
                        MessageBox.Show("선택된 차주가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 선택된 차주의 데이터를 Excel로 내보내고 인쇄
                    var tempPath = Path.Combine(Path.GetTempPath(), $"비핵심_선택_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    await ExportSelectedBorrowersToExcel(selectedBorrowers, tempPath);

                    // Excel 파일 열기 (인쇄 대화상자)
                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    MessageBox.Show($"Excel 파일이 열렸습니다.\n선택된 차주: {selectedBorrowers.Count}개\nCtrl+P를 눌러 인쇄해주세요.", 
                        "인쇄", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"인쇄 준비 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Excel 버튼 클릭 - 차주 선택 다이얼로그 표시
        /// </summary>
        private async void ExcelWithSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null || !_viewModel.BorrowerItems.Any())
                {
                    MessageBox.Show("내보낼 차주가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 차주 선택 다이얼로그 표시
                var dialog = BorrowerSelectionDialog.CreateForExcel(_viewModel.BorrowerItems);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    var selectedBorrowers = dialog.SelectedBorrowers;
                    if (selectedBorrowers.Count == 0)
                    {
                        MessageBox.Show("선택된 차주가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 파일 저장 다이얼로그
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel Files (*.xlsx)|*.xlsx",
                        FileName = $"비핵심_선택차주_{DateTime.Now:yyyyMMdd}.xlsx",
                        Title = "선택한 차주 데이터 Excel 저장"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        await ExportSelectedBorrowersToExcel(selectedBorrowers, saveDialog.FileName);

                        var result = MessageBox.Show(
                            $"Excel 파일이 저장되었습니다.\n선택된 차주: {selectedBorrowers.Count}개\n\n파일을 열겠습니까?",
                            "저장 완료",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 선택된 차주의 데이터를 Excel로 내보내기
        /// </summary>
        private async System.Threading.Tasks.Task ExportSelectedBorrowersToExcel(List<SelectableBorrower> selectedBorrowers, string filePath)
        {
            if (_viewModel == null) return;

            var excelService = App.ServiceProvider?.GetService<ExcelService>();
            if (excelService == null)
            {
                MessageBox.Show("Excel 서비스를 사용할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 선택된 차주의 ID 목록
            var selectedBorrowerIds = selectedBorrowers.Select(b => b.BorrowerId).ToHashSet();
            var selectedBorrowerNumbers = selectedBorrowers.Select(b => b.BorrowerNumber).ToHashSet();
            var selectedBorrowerNames = selectedBorrowers.Select(b => b.BorrowerName).ToHashSet();

            // 전체 물건 중 선택된 차주의 물건만 필터링
            var allProperties = await _viewModel.GetAllPropertiesAsync();
            var filteredProperties = allProperties
                .Where(p => selectedBorrowerNumbers.Contains(p.DebtorName) || selectedBorrowerNames.Contains(p.DebtorName))
                .ToList();

            // 차주별로 그룹화
            var propertiesByBorrower = filteredProperties
                .GroupBy(p => p.DebtorName ?? "기타")
                .ToDictionary(g => g.Key, g => g.ToList());

            var programName = _viewModel.CurrentProjectName ?? "비핵심";

            await excelService.ExportNonCoreByBorrowerToExcelAsync(propertiesByBorrower, programName, filePath);
        }

        /// <summary>
        /// 전체 데이터 Excel 내보내기 (공통 메서드)
        /// </summary>
        private async System.Threading.Tasks.Task ExportToExcelAll(string filePath)
        {
            if (_viewModel == null) return;

            var excelService = App.ServiceProvider?.GetService<ExcelService>();
            if (excelService == null)
            {
                MessageBox.Show("Excel 서비스를 사용할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var properties = await _viewModel.GetAllPropertiesAsync();
            var programName = _viewModel.CurrentProjectName ?? "비핵심";
            
            await excelService.ExportNonCoreAllToExcelAsync(properties, programName, filePath);
        }

        /// <summary>
        /// 차주별 데이터 Excel 내보내기 (공통 메서드)
        /// </summary>
        private async System.Threading.Tasks.Task ExportToExcelByBorrower(string filePath)
        {
            if (_viewModel == null) return;

            var excelService = App.ServiceProvider?.GetService<ExcelService>();
            if (excelService == null)
            {
                MessageBox.Show("Excel 서비스를 사용할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var propertiesByBorrower = await _viewModel.GetPropertiesGroupedByBorrowerAsync();
            var programName = _viewModel.CurrentProjectName ?? "비핵심";
            
            await excelService.ExportNonCoreByBorrowerToExcelAsync(propertiesByBorrower, programName, filePath);
        }

        // ========== QA 이벤트 핸들러 (Phase 7: 피드백 #14) ==========

        /// <summary>
        /// QA 버튼 클릭 - QA 입력 다이얼로그 표시
        /// </summary>
        private void QAButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 현재 활성 탭을 기본 선택으로 설정
                var currentTab = _viewModel?.ActiveTab ?? "Home";
                
                // QA 입력 다이얼로그 표시
                var dialog = new QAInputDialog(currentTab);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // QA 질문이 저장되었음을 알림
                    var selectedMenu = dialog.SelectedMenu;
                    var question = dialog.Question;
                    
                    MessageBox.Show(
                        $"QA 질문이 등록되었습니다.\n\n메뉴: {selectedMenu}\n질문: {question}",
                        "QA 등록 완료",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"QA 다이얼로그를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}

