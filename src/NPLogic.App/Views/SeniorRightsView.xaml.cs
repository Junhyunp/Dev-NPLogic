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

