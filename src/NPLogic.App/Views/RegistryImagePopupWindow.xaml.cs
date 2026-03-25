using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NPLogic.Core.Models;

namespace NPLogic.Views
{
    /// <summary>
    /// 등기부등본 이미지 팝업 윈도우 - 물건지별 등기부등본 이미지를 별도 창으로 표시
    /// </summary>
    public partial class RegistryImagePopupWindow : Window
    {
        public RegistryImagePopupWindow(IList<RegistryRun> runs)
        {
            InitializeComponent();

            RunComboBox.ItemsSource = runs;
            if (runs.Count > 0)
            {
                RunComboBox.SelectedIndex = 0;
            }
        }

        private void RunComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RunComboBox.SelectedItem is not RegistryRun selectedRun)
            {
                ImageList.ItemsSource = null;
                NoImageText.Visibility = Visibility.Visible;
                return;
            }

            var images = new ObservableCollection<BitmapImage>();

            if (selectedRun.SummaryImagesBase64 != null && selectedRun.SummaryImagesBase64.Count > 0)
            {
                Debug.WriteLine($"[RegistryImagePopup] 이미지 로드: {selectedRun.SourcePdfName}, {selectedRun.SummaryImagesBase64.Count}개");
                foreach (var base64 in selectedRun.SummaryImagesBase64)
                {
                    var bitmap = ConvertBase64ToBitmapImage(base64);
                    if (bitmap != null)
                    {
                        images.Add(bitmap);
                    }
                }
            }

            ImageList.ItemsSource = images;
            NoImageText.Visibility = images.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static BitmapImage? ConvertBase64ToBitmapImage(string base64String)
        {
            try
            {
                var base64Data = base64String;
                if (base64String.Contains(","))
                {
                    base64Data = base64String.Substring(base64String.IndexOf(",") + 1);
                }

                var imageBytes = Convert.FromBase64String(base64Data);
                var bitmap = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RegistryImagePopup] Base64->BitmapImage 변환 실패: {ex.Message}");
                return null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
