using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

