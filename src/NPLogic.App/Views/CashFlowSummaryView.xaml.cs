using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// CashFlowSummaryView.xaml에 대한 상호 작용 논리
    /// S-007, S-008: 차주 리스트 + 120개월 월별 현금흐름
    /// </summary>
    public partial class CashFlowSummaryView : UserControl
    {
        private CashFlowSummaryViewModel? _viewModel;
        private DataGrid? _monthlyDataGrid;

        public CashFlowSummaryView()
        {
            InitializeComponent();
            Loaded += CashFlowSummaryView_Loaded;
        }

        private async void CashFlowSummaryView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CashFlowSummaryViewModel viewModel)
            {
                _viewModel = viewModel;
                
                // 월 컬럼 변경 이벤트 구독
                _viewModel.MonthColumnsChanged += OnMonthColumnsChanged;
                
                // DataGrid 찾기
                _monthlyDataGrid = FindDataGrid(this);
                
                // 동적 컬럼 생성
                GenerateMonthColumns();
                
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// DataGrid 찾기 (VisualTree 탐색)
        /// </summary>
        private DataGrid? FindDataGrid(DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is DataGrid dg && dg.FrozenColumnCount == 1)
                {
                    return dg;
                }
                var result = FindDataGrid(child);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// 월 컬럼 변경 이벤트 핸들러
        /// </summary>
        private void OnMonthColumnsChanged()
        {
            Dispatcher.Invoke(() => GenerateMonthColumns());
        }

        /// <summary>
        /// 120개월 동적 컬럼 생성
        /// </summary>
        private void GenerateMonthColumns()
        {
            if (_monthlyDataGrid == null || _viewModel == null) return;

            // 기존 월 컬럼 제거 (첫 번째 차주명 컬럼 제외)
            while (_monthlyDataGrid.Columns.Count > 1)
            {
                _monthlyDataGrid.Columns.RemoveAt(1);
            }

            // 120개월 컬럼 추가
            foreach (var monthKey in _viewModel.MonthColumns)
            {
                var column = new DataGridTextColumn
                {
                    Header = FormatMonthHeader(monthKey),
                    Width = 80,
                    IsReadOnly = true
                };

                // 바인딩 설정 (인덱서 사용)
                var binding = new Binding($"MonthlyAmounts[{monthKey}]")
                {
                    StringFormat = "N0",
                    FallbackValue = ""
                };
                column.Binding = binding;

                // 스타일
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4)));
                style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.0));
                
                // 금액에 따른 색상 (양수: 파랑, 0: 회색)
                var trigger = new DataTrigger
                {
                    Binding = new Binding($"MonthlyAmounts[{monthKey}]"),
                    Value = 0m
                };
                trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.LightGray));
                style.Triggers.Add(trigger);

                column.ElementStyle = style;

                _monthlyDataGrid.Columns.Add(column);
            }
        }

        /// <summary>
        /// 월 헤더 포맷 (2026-01 -> 26.01)
        /// </summary>
        private string FormatMonthHeader(string monthKey)
        {
            if (monthKey.Length == 7) // "2026-01"
            {
                return monthKey.Substring(2, 2) + "." + monthKey.Substring(5, 2);
            }
            return monthKey;
        }
    }
}
