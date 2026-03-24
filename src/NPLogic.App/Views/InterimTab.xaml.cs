using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// InterimTab.xaml에 대한 상호 작용 논리
    /// 인터림(가지급금/회수정보) 관리 탭
    /// </summary>
    public partial class InterimTab : UserControl
    {
        public InterimTab()
        {
            InitializeComponent();
        }

        private void NoteTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is InterimTabViewModel vm)
            {
                vm.SaveNoteCommand.Execute(null);
            }
        }
    }
}
