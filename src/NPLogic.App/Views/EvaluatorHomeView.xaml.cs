using System.Windows;
using System.Windows.Controls;
using NPLogic.Core.Models;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 평가자용 홈 화면
    /// </summary>
    public partial class EvaluatorHomeView : UserControl
    {
        public EvaluatorHomeView()
        {
            InitializeComponent();
        }

        private async void EvaluatorHomeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EvaluatorHomeViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// 진행 체크박스 클릭
        /// </summary>
        private async void ProgressCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is Property property &&
                checkBox.Tag is string fieldName &&
                DataContext is EvaluatorHomeViewModel viewModel)
            {
                await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, checkBox.IsChecked ?? false);
            }
        }

        /// <summary>
        /// 경개개시여부 콤보박스 변경
        /// </summary>
        private async void AuctionStartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.DataContext is Property property &&
                comboBox.Tag is string fieldName &&
                comboBox.SelectedItem is ComboBoxItem selectedItem &&
                DataContext is EvaluatorHomeViewModel viewModel)
            {
                var value = selectedItem.Content?.ToString() ?? "";
                await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, value);
            }
        }
    }
}

