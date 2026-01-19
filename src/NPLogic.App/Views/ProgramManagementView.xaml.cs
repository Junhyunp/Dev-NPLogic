using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 프로그램 관리 화면
    /// </summary>
    public partial class ProgramManagementView : UserControl
    {
        public ProgramManagementView()
        {
            InitializeComponent();
        }

        private async void ProgramManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProgramManagementViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// 데이터디스크 드롭존 드래그 오버 핸들러
        /// </summary>
        private void DataDiskDropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// 데이터디스크 드롭존 드롭 핸들러
        /// </summary>
        private void DataDiskDropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var filePath = files.FirstOrDefault(f =>
                    {
                        var ext = Path.GetExtension(f).ToLower();
                        return ext == ".xlsx" || ext == ".xls";
                    });

                    if (!string.IsNullOrEmpty(filePath) && DataContext is ProgramManagementViewModel viewModel)
                    {
                        viewModel.HandleDataDiskFileDrop(filePath);
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// 시트 아이템 클릭 핸들러
        /// </summary>
        private void SheetItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SelectableSheetInfo sheet)
            {
                if (DataContext is ProgramManagementViewModel viewModel)
                {
                    viewModel.ToggleSheetSelection(sheet);
                }
            }
        }

        /// <summary>
        /// Interim 드롭존 드래그 오버 핸들러
        /// </summary>
        private void InterimDropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Interim 드롭존 드롭 핸들러
        /// </summary>
        private void InterimDropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var filePath = files.FirstOrDefault(f =>
                    {
                        var ext = Path.GetExtension(f).ToLower();
                        return ext == ".xlsx" || ext == ".xls";
                    });

                    if (!string.IsNullOrEmpty(filePath) && DataContext is ProgramManagementViewModel viewModel)
                    {
                        viewModel.HandleInterimFileDrop(filePath);
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Interim 시트 아이템 클릭 핸들러
        /// </summary>
        private void InterimSheetItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SelectableSheetInfo sheet)
            {
                if (DataContext is ProgramManagementViewModel viewModel)
                {
                    viewModel.ToggleInterimSheetSelection(sheet);
                }
            }
        }

        /// <summary>
        /// 시트 체크박스 클릭 핸들러
        /// </summary>
        private void SheetCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is SelectableSheetInfo sheet)
            {
                if (DataContext is ProgramManagementViewModel viewModel)
                {
                    // 선택 상태가 이미 바인딩으로 변경되었으므로 알림만 전달
                    viewModel.ToggleSheetSelection(sheet);
                }
            }
        }

        /// <summary>
        /// 컬럼 매핑 버튼 클릭 핸들러
        /// </summary>
        private void ColumnMappingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SelectableSheetInfo sheet)
            {
                if (DataContext is ProgramManagementViewModel viewModel)
                {
                    viewModel.OpenColumnMappingDialog(sheet);
                }
            }
        }
    }
}

