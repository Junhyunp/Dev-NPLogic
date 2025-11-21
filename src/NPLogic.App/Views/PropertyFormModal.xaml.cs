using System.Windows;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// PropertyFormModal.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PropertyFormModal : Window
    {
        public PropertyFormModal()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 저장 성공 시 호출
        /// </summary>
        public void OnSaveSuccess()
        {
            DialogResult = true;
            Close();
        }
    }
}

