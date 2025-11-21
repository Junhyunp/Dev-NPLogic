using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// DataUploadView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DataUploadView : UserControl
    {
        public DataUploadView()
        {
            InitializeComponent();
        }

        private void DataUploadView_Drop(object sender, DragEventArgs e)
        {
            // 전체 뷰에서의 드롭 이벤트는 무시
        }

        private void DataUploadView_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void ExcelDropZone_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is DataUploadViewModel viewModel)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        viewModel.HandleExcelFileDrop(files[0]);
                    }
                }
            }
            e.Handled = true;
        }

        private void PdfDropZone_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is DataUploadViewModel viewModel)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    viewModel.HandlePdfFilesDrop(files);
                }
            }
            e.Handled = true;
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }
}

