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
    }
}

