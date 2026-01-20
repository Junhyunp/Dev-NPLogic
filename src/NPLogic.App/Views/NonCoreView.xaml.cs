using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
        
        // 기능 탭 순서 배열 - 피드백 반영: 11개 탭 (인터림 추가)
        private readonly string[] _functionTabs = 
        { 
            "Home", "BorrowerOverview", "Loan", "CollateralProperty", 
            "SeniorRights", "Restructuring", "Evaluation", "AuctionSchedule", 
            "Interim", "CashFlow", "XnpvComparison" 
        };
        private int _currentFunctionTabIndex = 0;
        
        // ========== 탭별 View/ViewModel 캐시 (성능 최적화) ==========
        private readonly Dictionary<string, UserControl> _tabViewCache = new();
        private readonly Dictionary<string, object> _tabViewModelCache = new();
        private Guid? _lastPropertyId; // 마지막으로 로드한 물건 ID (캐시 무효화용)
        
        // ========== 탭 전환 성능 최적화 (Race Condition 방지) ==========
        private CancellationTokenSource? _tabLoadCts; // 탭 전환 취소용
        private bool _isLoadingTab = false; // 로딩 중 여부 (중복 실행 방지)
        private string? _pendingTabName = null; // 대기 중인 탭 이름

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
            if (TabInterim.IsChecked == true) return "Interim";
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
        /// 기능별 컨텐츠 로드 - 성능 최적화: View 캐싱, CancellationToken, 로딩 Lock 적용
        /// </summary>
        private async Task LoadFunctionContentAsync(string tabName)
        {
            // 로딩 중이면 대기 탭으로 저장하고 리턴 (중복 실행 방지)
            if (_isLoadingTab)
            {
                _pendingTabName = tabName;
                return;
            }
            
            // 이전 로드 작업 취소
            _tabLoadCts?.Cancel();
            _tabLoadCts = new CancellationTokenSource();
            var token = _tabLoadCts.Token;
            
            _isLoadingTab = true;
            
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null) return;

                // 취소 확인
                token.ThrowIfCancellationRequested();

                // 현재 선택된 물건 ID 가져오기
                var selectedPropertyId = _viewModel?.SelectedPropertyTab?.PropertyId;
                
                // 물건이 변경되었는지 확인 (캐시 데이터 갱신 필요 여부)
                var propertyChanged = selectedPropertyId != _lastPropertyId;
                _lastPropertyId = selectedPropertyId;
                
                UserControl? content;
                
                // 캐시에서 View 조회
                if (_tabViewCache.TryGetValue(tabName, out var cachedView))
                {
                    content = cachedView;
                    
                    // 물건이 변경된 경우에만 데이터 갱신
                    if (propertyChanged)
                    {
                        token.ThrowIfCancellationRequested();
                        await RefreshTabDataAsync(tabName, selectedPropertyId, token);
                    }
                }
                else
                {
                    // 최초 접근 시 View 생성 후 캐시에 저장
                    token.ThrowIfCancellationRequested();
                    content = await CreateAndCacheTabViewAsync(tabName, selectedPropertyId, serviceProvider, token);
                }

                // 취소되지 않은 경우에만 UI 업데이트
                token.ThrowIfCancellationRequested();
                ContentArea.Content = content;
                
                // ViewModel에 현재 탭 알림
                _viewModel?.SetActiveTab(tabName);
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 무시 - 새로운 탭 로드가 진행 중
                Debug.WriteLine($"탭 로드 취소됨: {tabName}");
            }
            finally
            {
                _isLoadingTab = false;
                
                // 대기 중인 탭이 있으면 로드
                if (_pendingTabName != null && _pendingTabName != tabName)
                {
                    var pending = _pendingTabName;
                    _pendingTabName = null;
                    await LoadFunctionContentAsync(pending);
                }
            }
        }
        
        /// <summary>
        /// 탭 View 생성 및 캐시에 저장
        /// </summary>
        private async Task<UserControl> CreateAndCacheTabViewAsync(string tabName, Guid? selectedPropertyId, IServiceProvider serviceProvider, CancellationToken token = default)
        {
            UserControl content;
            object? viewModel = null;
            
            switch (tabName)
            {
                case "Home":
                    content = serviceProvider.GetRequiredService<HomeTab>();
                    {
                        var vm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        if (selectedPropertyId.HasValue)
                        {
                            vm.SetPropertyId(selectedPropertyId.Value);
                            token.ThrowIfCancellationRequested();
                            await vm.InitializeAsync();
                        }
                        content.DataContext = vm;
                        viewModel = vm;
                    }
                    break;
                    
                case "BorrowerOverview":
                    content = serviceProvider.GetRequiredService<BorrowerOverviewView>();
                    {
                        var borrowerVm = serviceProvider.GetRequiredService<BorrowerOverviewViewModel>();
                        
                        if (_viewModel?.SelectedPropertyTab != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var property = await _viewModel.GetCurrentPropertyCachedAsync();
                            if (property != null)
                            {
                                token.ThrowIfCancellationRequested();
                                await borrowerVm.SetSelectedPropertyAsync(property);
                            }
                        }
                        
                        content.DataContext = borrowerVm;
                        viewModel = borrowerVm;
                    }
                    break;
                    
                case "Loan":
                    content = serviceProvider.GetRequiredService<Loan.LoanSheetView>();
                    {
                        var loanSheetVm = serviceProvider.GetRequiredService<LoanSheetViewModel>();
                        
                        if (_viewModel?.SelectedPropertyTab != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var property = await _viewModel.GetCurrentPropertyCachedAsync();
                            if (property != null)
                            {
                                token.ThrowIfCancellationRequested();
                                await loanSheetVm.SetSelectedPropertyAsync(property);
                            }
                            else
                            {
                                token.ThrowIfCancellationRequested();
                                await loanSheetVm.InitializeAsync();
                            }
                        }
                        else
                        {
                            token.ThrowIfCancellationRequested();
                            await loanSheetVm.InitializeAsync();
                        }
                        
                        content.DataContext = loanSheetVm;
                        viewModel = loanSheetVm;
                    }
                    break;
                    
                case "CollateralProperty":
                    content = serviceProvider.GetRequiredService<CollateralPropertyView>();
                    {
                        var vm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        if (selectedPropertyId.HasValue)
                        {
                            vm.SetPropertyId(selectedPropertyId.Value);
                            token.ThrowIfCancellationRequested();
                            await vm.InitializeAsync();
                        }
                        content.DataContext = vm;
                        viewModel = vm;
                    }
                    break;
                    
                case "SeniorRights":
                    content = serviceProvider.GetRequiredService<SeniorRightsView>();
                    break;
                    
                case "Restructuring":
                    content = serviceProvider.GetRequiredService<RestructuringOverviewView>();
                    break;
                    
                case "Evaluation":
                    content = serviceProvider.GetRequiredService<EvaluationTab>();
                    {
                        var parentVm = serviceProvider.GetRequiredService<PropertyDetailViewModel>();
                        if (selectedPropertyId.HasValue)
                        {
                            parentVm.SetPropertyId(selectedPropertyId.Value);
                            token.ThrowIfCancellationRequested();
                            await parentVm.InitializeAsync();
                        }
                        
                        if (parentVm.EvaluationViewModel != null)
                        {
                            if (selectedPropertyId.HasValue)
                            {
                                parentVm.EvaluationViewModel.SetPropertyId(selectedPropertyId.Value);
                                if (parentVm.Property != null)
                                {
                                    parentVm.EvaluationViewModel.SetProperty(parentVm.Property);
                                }
                            }
                            content.DataContext = parentVm.EvaluationViewModel;
                            token.ThrowIfCancellationRequested();
                            await parentVm.EvaluationViewModel.LoadAsync();
                            viewModel = parentVm.EvaluationViewModel;
                        }
                        else
                        {
                            content.DataContext = null;
                        }
                    }
                    break;
                    
                case "AuctionSchedule":
                    // 경매/공매 일정 통합 뷰 사용
                    try
                    {
                        var auctionPublicSaleView = new AuctionPublicSaleView();
                        
                        // ViewModel들 가져오기
                        var auctionVm = serviceProvider.GetRequiredService<AuctionScheduleDetailViewModel>();
                        var publicSaleVm = serviceProvider.GetRequiredService<PublicSaleScheduleViewModel>();
                        
                        // PropertyId 설정
                        if (selectedPropertyId.HasValue)
                        {
                            auctionVm.SetPropertyId(selectedPropertyId.Value);
                            publicSaleVm.SetPropertyId(selectedPropertyId.Value);
                        }
                        
                        // ViewModels 연결
                        auctionPublicSaleView.SetViewModels(auctionVm, publicSaleVm);
                        viewModel = auctionVm;
                        content = auctionPublicSaleView;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"AuctionSchedule 탭 로드 실패: {ex.Message}");
                        content = CreatePlaceholder("AuctionSchedule");
                    }
                    break;
                    
                case "Interim":
                    content = serviceProvider.GetRequiredService<InterimTab>();
                    {
                        var interimVm = serviceProvider.GetRequiredService<InterimTabViewModel>();
                        
                        // 프로그램 ID 가져오기
                        var programId = _viewModel?.SelectedPropertyTab?.ProgramId ?? Guid.Empty;
                        var borrowerNumber = _viewModel?.SelectedPropertyTab?.BorrowerNumber;
                        
                        if (programId != Guid.Empty)
                        {
                            token.ThrowIfCancellationRequested();
                            await interimVm.InitializeAsync(programId, borrowerNumber);
                        }
                        
                        content.DataContext = interimVm;
                        viewModel = interimVm;
                    }
                    break;
                    
                case "CashFlow":
                    content = serviceProvider.GetRequiredService<CashFlowSummaryView>();
                    break;
                    
                case "XnpvComparison":
                    content = serviceProvider.GetRequiredService<XnpvComparisonView>();
                    break;
                    
                default:
                    content = CreatePlaceholder(tabName);
                    break;
            }
            
            // 취소 확인 후 캐시에 저장
            token.ThrowIfCancellationRequested();
            _tabViewCache[tabName] = content;
            if (viewModel != null)
            {
                _tabViewModelCache[tabName] = viewModel;
            }
            
            return content;
        }
        
        /// <summary>
        /// 캐시된 탭의 데이터만 갱신 (View 재생성 없이)
        /// </summary>
        private async Task RefreshTabDataAsync(string tabName, Guid? selectedPropertyId, CancellationToken token = default)
        {
            if (!_tabViewModelCache.TryGetValue(tabName, out var viewModel))
                return;
            
            token.ThrowIfCancellationRequested();
            var property = await _viewModel?.GetCurrentPropertyCachedAsync()!;
            
            switch (tabName)
            {
                case "Home":
                case "CollateralProperty":
                    if (viewModel is PropertyDetailViewModel propVm && selectedPropertyId.HasValue)
                    {
                        propVm.SetPropertyId(selectedPropertyId.Value);
                        token.ThrowIfCancellationRequested();
                        await propVm.InitializeAsync();
                    }
                    break;
                    
                case "BorrowerOverview":
                    if (viewModel is BorrowerOverviewViewModel borrowerVm && property != null)
                    {
                        token.ThrowIfCancellationRequested();
                        await borrowerVm.SetSelectedPropertyAsync(property);
                    }
                    break;
                    
                case "Loan":
                    if (viewModel is LoanSheetViewModel loanVm && property != null)
                    {
                        token.ThrowIfCancellationRequested();
                        await loanVm.SetSelectedPropertyAsync(property);
                    }
                    break;
                    
                case "Evaluation":
                    if (viewModel is EvaluationTabViewModel evalVm && selectedPropertyId.HasValue)
                    {
                        evalVm.SetPropertyId(selectedPropertyId.Value);
                        if (property != null)
                        {
                            evalVm.SetProperty(property);
                        }
                        token.ThrowIfCancellationRequested();
                        await evalVm.LoadAsync();
                    }
                    break;
                    
                case "AuctionSchedule":
                    if (viewModel is AuctionScheduleDetailViewModel auctionVm && selectedPropertyId.HasValue)
                    {
                        auctionVm.SetPropertyId(selectedPropertyId.Value);
                        token.ThrowIfCancellationRequested();
                        await auctionVm.InitializeAsync();
                    }
                    break;
                    
                case "Interim":
                    if (viewModel is InterimTabViewModel interimVm)
                    {
                        var programId = _viewModel?.SelectedPropertyTab?.ProgramId ?? Guid.Empty;
                        var borrowerNumber = _viewModel?.SelectedPropertyTab?.BorrowerNumber;
                        
                        if (programId != Guid.Empty)
                        {
                            token.ThrowIfCancellationRequested();
                            await interimVm.InitializeAsync(programId, borrowerNumber);
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 탭 캐시 초기화 (프로그램 전환 시 호출)
        /// </summary>
        public void ClearTabCache()
        {
            _tabViewCache.Clear();
            _tabViewModelCache.Clear();
            _lastPropertyId = null;
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
            if (sender is ListBox listBox && listBox.SelectedItem is BorrowerListItem selectedItem)
            {
                // 비동기로 차주 선택 및 물건 목록 로드 완료까지 대기
                if (_viewModel != null)
                {
                    await _viewModel.SelectBorrowerAsync(selectedItem.BorrowerId);
                }
                
                // 물건 목록 로드 완료 후 현재 탭 컨텐츠 새로고침
                if (_viewModel?.ActiveTab != null)
                {
                    await LoadFunctionContentAsync(_viewModel.ActiveTab);
                }
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

