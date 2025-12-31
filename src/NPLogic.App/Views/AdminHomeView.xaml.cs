using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NPLogic.Core.Models;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 관리자용 홈 화면 - 마스터-디테일 레이아웃
    /// </summary>
    public partial class AdminHomeView : UserControl
    {
        private double _savedLeftPanelWidth = 300;

        public AdminHomeView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 좌측 패널 접기/펼치기
        /// </summary>
        private void LeftPanelToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                if (toggleButton.IsChecked == false)
                {
                    // 패널 접기
                    _savedLeftPanelWidth = LeftPanelColumn.Width.Value;
                    LeftPanelColumn.Width = new GridLength(0);
                    LeftPanel.Visibility = Visibility.Collapsed;
                    LeftPanelOpenButton.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 좌측 패널 열기
        /// </summary>
        private void LeftPanelOpen_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelColumn.Width = new GridLength(_savedLeftPanelWidth > 0 ? _savedLeftPanelWidth : 300);
            LeftPanel.Visibility = Visibility.Visible;
            LeftPanelOpenButton.Visibility = Visibility.Collapsed;
            LeftPanelToggle.IsChecked = true;
        }

        private async void AdminHomeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminHomeViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// 담당자 할당 변경
        /// </summary>
        private async void AssignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.DataContext is Property property &&
                comboBox.SelectedValue is System.Guid userId &&
                DataContext is AdminHomeViewModel viewModel)
            {
                await viewModel.AssignToUserAsync(property.Id, userId);
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
                DataContext is AdminHomeViewModel viewModel)
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
                DataContext is AdminHomeViewModel viewModel)
            {
                var value = selectedItem.Content?.ToString() ?? "";
                await viewModel.UpdateProgressFieldAsync(property.Id, fieldName, value);
            }
        }

        /// <summary>
        /// 차주번호/차주명 클릭 - 비핵심 화면으로 이동
        /// </summary>
        private void PropertyNumber_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && 
                element.DataContext is Property property)
            {
                // MainWindow의 NavigateToNonCoreView 호출
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.NavigateToNonCoreView(property);
                }
            }
        }

        /// <summary>
        /// 필터 뱃지 더블클릭 - 해당 상태만 필터링
        /// </summary>
        private void FilterBadge_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is System.Windows.FrameworkElement element &&
                element.Tag is string filterType &&
                DataContext is AdminHomeViewModel viewModel)
            {
                viewModel.FilterByStatus(filterType);
            }
        }
    }
}

