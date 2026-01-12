using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// PropertyDetailView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PropertyDetailView : UserControl
    {
        public PropertyDetailView()
        {
            InitializeComponent();
            
            // N-001: 키보드 탐색 이벤트 등록
            PreviewKeyDown += PropertyDetailView_PreviewKeyDown;
            Focusable = true;
        }

        private async void PropertyDetailView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel viewModel)
            {
                await viewModel.InitializeAsync();

                // 평가 탭 데이터 로드
                if (viewModel.EvaluationViewModel != null)
                {
                    await viewModel.EvaluationViewModel.LoadAsync();
                }
            }
            
            // N-001: 포커스 설정 (키보드 입력 받기 위해)
            Focus();
        }

        /// <summary>
        /// N-001: 키보드 탐색 핸들러
        /// Ctrl + 화살표: 물건 간 이동
        /// </summary>
        private void PropertyDetailView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not PropertyDetailViewModel viewModel) return;

            // Ctrl 키가 눌렸는지 확인
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            switch (e.Key)
            {
                case Key.Left:
                    // Ctrl + ←: 이전 물건
                    if (viewModel.CanNavigatePrevious && viewModel.NavigatePreviousCommand.CanExecute(null))
                    {
                        viewModel.NavigatePreviousCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Right:
                    // Ctrl + →: 다음 물건
                    if (viewModel.CanNavigateNext && viewModel.NavigateNextCommand.CanExecute(null))
                    {
                        viewModel.NavigateNextCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    // Ctrl + ↑: 첫 번째 물건
                    if (viewModel.NavigateFirstCommand.CanExecute(null))
                    {
                        viewModel.NavigateFirstCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Down:
                    // Ctrl + ↓: 마지막 물건
                    if (viewModel.NavigateLastCommand.CanExecute(null))
                    {
                        viewModel.NavigateLastCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}

