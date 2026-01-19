using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views
{
    /// <summary>
    /// QA 입력 다이얼로그 (Phase 7: 피드백 #14)
    /// 사용자가 특정 메뉴에 대한 QA 질문을 작성할 수 있는 다이얼로그
    /// </summary>
    public partial class QAInputDialog : Window
    {
        /// <summary>
        /// 선택된 메뉴 이름
        /// </summary>
        public string SelectedMenu { get; private set; } = string.Empty;

        /// <summary>
        /// 입력된 질문 내용
        /// </summary>
        public string Question { get; private set; } = string.Empty;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="defaultTab">기본 선택될 탭 이름 (Home, BorrowerOverview, Loan, etc.)</param>
        public QAInputDialog(string defaultTab = "Home")
        {
            InitializeComponent();
            
            // 기본 탭 선택
            SelectTabByName(defaultTab);
        }

        /// <summary>
        /// 탭 이름으로 ComboBox 항목 선택
        /// </summary>
        private void SelectTabByName(string tabName)
        {
            for (int i = 0; i < MenuComboBox.Items.Count; i++)
            {
                if (MenuComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tabName)
                {
                    MenuComboBox.SelectedIndex = i;
                    return;
                }
            }
            // 찾지 못하면 첫 번째 항목 선택
            MenuComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 등록 버튼 클릭
        /// </summary>
        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            // 입력값 검증
            if (string.IsNullOrWhiteSpace(QuestionTextBox.Text))
            {
                MessageBox.Show("질문 내용을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuestionTextBox.Focus();
                return;
            }

            // 선택된 메뉴 가져오기
            if (MenuComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedMenu = selectedItem.Content?.ToString() ?? "전체";
            }
            else
            {
                SelectedMenu = "전체";
            }

            Question = QuestionTextBox.Text.Trim();

            // TODO: 실제로 QA 질문을 DB에 저장하는 로직 구현
            // 현재는 다이얼로그만 표시하고 결과 반환

            DialogResult = true;
            Close();
        }
    }
}
