using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.Views
{
    /// <summary>
    /// QA 질의/답변 팝업 - DataGrid 4열 표로 이력 표시
    /// Plan 01: 읽기 전용 이력 표시
    /// Plan 02: 행 추가 + 답변 수정/저장 기능 추가 예정
    /// </summary>
    public partial class QAPopupWindow : Window
    {
        private readonly Guid _propertyId;
        private readonly string _borrowerNumber;
        private readonly string _borrowerName;
        private readonly PropertyQaRepository _qaRepository;
        private ObservableCollection<PropertyQa> _qaItems = new();

        public QAPopupWindow(Guid propertyId, string borrowerNumber, string borrowerName)
        {
            InitializeComponent();

            _propertyId = propertyId;
            _borrowerNumber = borrowerNumber;
            _borrowerName = borrowerName;
            _qaRepository = App.ServiceProvider.GetRequiredService<PropertyQaRepository>();

            // Set header text
            HeaderText.Text = $"{_borrowerNumber} QA 질의/답변";

            Loaded += QAPopupWindow_Loaded;
        }

        private async void QAPopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadQaDataAsync();
        }

        /// <summary>
        /// DB에서 QA 이력을 로드하여 DataGrid에 표시
        /// </summary>
        private async System.Threading.Tasks.Task LoadQaDataAsync()
        {
            try
            {
                var qaList = await _qaRepository.GetByPropertyIdAsync(_propertyId);

                // Sort by CreatedAt ascending (oldest first, newest at bottom)
                var sorted = qaList.OrderBy(q => q.CreatedAt).ToList();

                _qaItems = new ObservableCollection<PropertyQa>(sorted);
                QaDataGrid.ItemsSource = _qaItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"QA 이력을 불러오는 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
