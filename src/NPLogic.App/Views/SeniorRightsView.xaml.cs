using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NPLogic.Views
{
    /// <summary>
    /// 선순위 관리 화면
    /// </summary>
    public partial class SeniorRightsView : UserControl
    {
        public SeniorRightsView()
        {
            InitializeComponent();

            // DataGrid 셀 클릭 시 즉시 편집 모드 진입 (ScrollViewer 내부 이벤트 라우팅 이슈 대응)
            ResidentialLeaseGrid.PreviewMouseLeftButtonDown += DataGrid_PreviewMouseLeftButtonDown;
            CommercialLeaseGrid.PreviewMouseLeftButtonDown += DataGrid_PreviewMouseLeftButtonDown;
            WageClaimGrid.PreviewMouseLeftButtonDown += DataGrid_PreviewMouseLeftButtonDown;
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cell = FindVisualParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || cell.IsEditing || cell.IsReadOnly)
                return;

            if (!cell.IsFocused)
                cell.Focus();

            var dataGrid = sender as DataGrid;
            if (dataGrid == null)
                return;

            var row = FindVisualParent<DataGridRow>(cell);
            if (row != null && !row.IsSelected)
                row.IsSelected = true;

            dataGrid.BeginEdit(e);
        }

        /// <summary>
        /// 수평 ScrollViewer가 수직 마우스 휠 이벤트를 잡아먹는 문제 해결.
        /// 수직 스크롤 이벤트를 부모(수직) ScrollViewer로 전달한다.
        /// </summary>
        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;

            e.Handled = true;
            var parent = VisualTreeHelper.GetParent((DependencyObject)sender);
            while (parent != null && parent is not ScrollViewer)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ScrollViewer sv)
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                sv.RaiseEvent(eventArg);
            }
        }

        /// <summary>
        /// DataGrid 내부 스크롤이 부모 ScrollViewer 스크롤을 잡아먹는 문제 해결.
        /// DataGrid의 마우스 휠 이벤트를 부모 ScrollViewer로 전달한다.
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;

            e.Handled = true;
            var parent = VisualTreeHelper.GetParent((DependencyObject)sender);
            while (parent != null && parent is not ScrollViewer)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ScrollViewer sv)
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                sv.RaiseEvent(eventArg);
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}

