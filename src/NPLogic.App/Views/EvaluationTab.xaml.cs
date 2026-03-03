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

