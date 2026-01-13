using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views
{
    /// <summary>
    /// 프로그램(Pool) 수준 기초데이터 화면
    /// - 프로그램정보관리, Data Disk 정보관리, 기초정보관리
    /// - QA, 고유정보관리, 사용인 설정, Assign
    /// </summary>
    public partial class BasicDataTab : UserControl
    {
        public BasicDataTab()
        {
            InitializeComponent();
        }

        private void BasicDataTab_Loaded(object sender, RoutedEventArgs e)
        {
            // 화면 로드 시 초기화 로직
            // ViewModel의 InitializeAsync() 호출 등
        }
    }
}
