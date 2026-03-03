using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 평가 탭 UserControl
    /// </summary>
    public partial class EvaluationTab : UserControl
    {
        public EvaluationTab()
        {
            InitializeComponent();
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

        /// <summary>
        /// 내부 스크롤이 있는 DataGrid용 스마트 스크롤 핸들러.
        /// 내부 스크롤이 끝에 도달했을 때만 부모 ScrollViewer로 전달한다.
        /// </summary>
        private void DataGrid_SmartScrollMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || sender is not DataGrid dataGrid) return;

            // DataGrid 내부 ScrollViewer 찾기
            var internalSv = FindChildScrollViewer(dataGrid);
            if (internalSv == null || internalSv.ScrollableHeight <= 0)
            {
                // 내부 스크롤 없음 (데이터 적음) → 부모로 전달
                DataGrid_PreviewMouseWheel(sender, e);
                return;
            }

            bool scrollingUp = e.Delta > 0;
            bool atTop = internalSv.VerticalOffset <= 0;
            bool atBottom = internalSv.VerticalOffset >= internalSv.ScrollableHeight;

            if ((scrollingUp && atTop) || (!scrollingUp && atBottom))
            {
                // 끝에 도달 → 부모로 전달
                DataGrid_PreviewMouseWheel(sender, e);
            }
            // 그 외 → 내부 스크롤 동작 (아무것도 안 함, 기본 동작 유지)
        }

        private static ScrollViewer? FindChildScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer sv) return sv;
                var found = FindChildScrollViewer(child);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// E-001: 사례지도 클릭 시 팝업으로 확대 표시
        /// </summary>
        private void MapPlaceholder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is EvaluationTabViewModel vm)
            {
                // 물건 주소를 가져와서 지도 팝업 열기
                var address = vm.PropertyAddress;
                if (string.IsNullOrWhiteSpace(address))
                {
                    address = "대한민국"; // 기본값
                }

                var popup = new ImagePopupWindow(
                    "사례지도",
                    address,
                    ImagePopupWindow.PopupType.Map);
                
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
        }
    }
}

