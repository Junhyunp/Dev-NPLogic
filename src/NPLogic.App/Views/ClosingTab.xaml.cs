using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.Views
{
    /// <summary>
    /// 마감 탭 - 마감 체크리스트, 권리분석 업데이트 알림, 마감 이력 표시
    /// 비고는 code-behind에서 직접 관리 (익명 DataContext 제약)
    /// </summary>
    public partial class ClosingTab : UserControl
    {
        private PropertyNoteRepository? _noteRepository;
        private Guid? _notePropertyId;

        public ClosingTab()
        {
            InitializeComponent();
        }

        private async void ClosingTab_Loaded(object sender, RoutedEventArgs e)
        {
            _noteRepository = App.ServiceProvider?
                .GetService(typeof(PropertyNoteRepository)) as PropertyNoteRepository;

            // 익명 DataContext에서 Property 추출
            if (DataContext != null)
            {
                var propertyInfo = DataContext.GetType().GetProperty("Property");
                if (propertyInfo?.GetValue(DataContext) is Property property)
                {
                    _notePropertyId = property.Id;
                    await LoadNoteAsync();
                }
            }
        }

        private async Task LoadNoteAsync()
        {
            if (_noteRepository == null || _notePropertyId == null) return;
            try
            {
                var note = await _noteRepository.GetByPropertyAndTabAsync(_notePropertyId.Value, "closing");
                NoteTextBox.Text = note?.NoteText ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Closing] 비고 로드 실패: {ex.Message}");
            }
        }

        private async void NoteTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_noteRepository == null || _notePropertyId == null) return;
            try
            {
                await _noteRepository.UpsertAsync(new PropertyNote
                {
                    PropertyId = _notePropertyId.Value,
                    TabName = "closing",
                    NoteText = NoteTextBox.Text ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Closing] 비고 저장 실패: {ex.Message}");
            }
        }

        private void ToggleNotePanel_Click(object sender, RoutedEventArgs e)
        {
            NotePanelColumn.Visibility = NotePanelColumn.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
