using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NPLogic.Core.Models;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// DashboardView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Loaded 이벤트 - ViewModel 초기화
        /// </summary>
        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// 통계 카드 클릭 - 상태 필터링
        /// </summary>
        private void StatCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string status)
            {
                if (DataContext is DashboardViewModel viewModel)
                {
                    viewModel.FilterByStatus(status);
                }
            }
        }

        /// <summary>
        /// 진행 체크박스 클릭 - 즉시 저장
        /// </summary>
        private async void ProgressCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string fieldName)
            {
                var property = checkBox.DataContext as Property;
                if (property != null && DataContext is DashboardViewModel viewModel)
                {
                    await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, checkBox.IsChecked ?? false);
                }
            }
        }

        /// <summary>
        /// 경개개시여부 콤보박스 변경 - 즉시 저장
        /// </summary>
        private async void AuctionStartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string fieldName)
            {
                var property = comboBox.DataContext as Property;
                if (property != null && DataContext is DashboardViewModel viewModel && e.AddedItems.Count > 0)
                {
                    var selectedItem = e.AddedItems[0];
                    string? value = null;
                    
                    if (selectedItem is ComboBoxItem item)
                    {
                        value = item.Content?.ToString();
                    }
                    else
                    {
                        value = selectedItem?.ToString();
                    }
                    
                    await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, value ?? "");
                }
            }
        }

        /// <summary>
        /// Assign 콤보박스 변경 - 담당자 할당
        /// </summary>
        private async void AssignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                var property = comboBox.DataContext as Property;
                if (property != null && DataContext is DashboardViewModel viewModel && comboBox.SelectedValue != null)
                {
                    var userId = (System.Guid)comboBox.SelectedValue;
                    await viewModel.AssignToUserAsync(property.Id, userId);
                }
            }
        }
    }
}
