using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.Views
{
    /// <summary>
    /// QA 질의/답변 팝업 - DataGrid 4열 표로 이력 표시 + CRUD
    /// Plan 01: 읽기 전용 이력 표시
    /// Plan 02: 행 추가 + 답변 수정/저장 기능
    /// </summary>
    public partial class QAPopupWindow : Window
    {
        private readonly Guid _propertyId;
        private readonly string _borrowerNumber;
        private readonly string _borrowerName;
        private readonly PropertyQaRepository _qaRepository;
        private ObservableCollection<PropertyQa> _qaItems = new();

        /// <summary>
        /// 원본 답변 스냅샷 - 변경 감지용
        /// Key: QA Id, Value: (Answer, AnsweredAt)
        /// </summary>
        private Dictionary<Guid, (string? Answer, DateTime? AnsweredAt)> _originalAnswers = new();

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

                // 원본 답변 스냅샷 저장
                SaveOriginalAnswersSnapshot();
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

        /// <summary>
        /// 현재 _qaItems의 답변 정보를 스냅샷으로 저장 (변경 감지용)
        /// </summary>
        private void SaveOriginalAnswersSnapshot()
        {
            _originalAnswers.Clear();
            foreach (var qa in _qaItems)
            {
                if (qa.Id != Guid.Empty)
                {
                    _originalAnswers[qa.Id] = (qa.Answer, qa.AnsweredAt);
                }
            }
        }

        /// <summary>
        /// 행 추가 버튼 클릭 - 새 QA 행 추가
        /// </summary>
        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            // DataGrid 현재 편집 모드 종료
            QaDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var newQa = new PropertyQa
            {
                Id = Guid.Empty, // DB 미저장 표시
                PropertyId = _propertyId,
                CreatedAt = DateTime.Now,
                BorrowerNumber = _borrowerNumber,
                BorrowerName = _borrowerName,
                Question = "",
                Answer = null,
                AnsweredAt = null
            };

            _qaItems.Add(newQa);

            // 스크롤을 맨 아래로 이동
            QaDataGrid.ScrollIntoView(newQa);

            // 추가된 행의 "질의내용" 셀에 포커스 + 편집 모드
            QaDataGrid.UpdateLayout();
            QaDataGrid.CurrentCell = new DataGridCellInfo(newQa, QaDataGrid.Columns[1]);
            QaDataGrid.BeginEdit();
        }

        /// <summary>
        /// 저장 버튼 클릭
        /// </summary>
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // DataGrid 현재 편집 모드 종료
            QaDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            await SaveAsync();
        }

        /// <summary>
        /// 신규 행 생성 + 변경된 행 업데이트를 DB에 저장
        /// </summary>
        private async System.Threading.Tasks.Task SaveAsync()
        {
            try
            {
                // 현재 사용자 ID 가져오기
                Guid? currentUserId = null;
                try
                {
                    var authService = App.ServiceProvider.GetRequiredService<AuthService>();
                    var session = authService.GetSession();
                    if (session?.User != null && Guid.TryParse(session.User.Id, out var userId))
                    {
                        currentUserId = userId;
                    }
                }
                catch
                {
                    // 인증 정보 없으면 무시
                }

                var itemsList = _qaItems.ToList();

                // 1. 신규 행 저장 (Id == Guid.Empty)
                foreach (var qa in itemsList.Where(q => q.Id == Guid.Empty))
                {
                    // 빈 질의내용은 건너뛰기
                    if (string.IsNullOrWhiteSpace(qa.Question))
                        continue;

                    // CreatedBy는 DB FK 제약(users 테이블)으로 인해 설정하지 않음
                    var created = await _qaRepository.CreateAsync(qa);

                    // 반환된 Id로 업데이트
                    qa.Id = created.Id;
                    qa.CreatedAt = created.CreatedAt;
                    qa.UpdatedAt = created.UpdatedAt;
                }

                // 2. 기존 행 중 변경된 항목 업데이트 (Id != Guid.Empty)
                foreach (var qa in itemsList.Where(q => q.Id != Guid.Empty))
                {
                    if (!_originalAnswers.TryGetValue(qa.Id, out var original))
                        continue;

                    bool answerChanged = qa.Answer != original.Answer;
                    bool answeredAtChanged = qa.AnsweredAt != original.AnsweredAt;

                    if (!answerChanged && !answeredAtChanged)
                        continue;

                    // Answer가 입력되었는데 AnsweredAt이 아직 null이면 자동 설정
                    if (!string.IsNullOrWhiteSpace(qa.Answer) && qa.AnsweredAt == null)
                    {
                        qa.AnsweredAt = DateTime.Now;
                    }

                    qa.AnsweredBy = currentUserId;
                    await _qaRepository.UpdateAsync(qa);
                }

                // 빈 신규 행 제거 (저장되지 않은 빈 행)
                var emptyNewRows = _qaItems.Where(q => q.Id == Guid.Empty && string.IsNullOrWhiteSpace(q.Question)).ToList();
                foreach (var empty in emptyNewRows)
                {
                    _qaItems.Remove(empty);
                }

                // 스냅샷 갱신
                SaveOriginalAnswersSnapshot();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"저장 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// DataGrid 셀 편집 시작 시 - 편집 제어
        /// 기존 행: 질의일자/질의내용 편집 불가
        /// 신규 행: 질의일자 편집 불가 (자동 설정)
        /// 회신일자/답변내용: 모든 행에서 편집 허용
        /// </summary>
        private void QaDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is not PropertyQa qa)
                return;

            int columnIndex = e.Column.DisplayIndex;

            // 질의일자(0): 모든 행에서 편집 불가
            if (columnIndex == 0)
            {
                e.Cancel = true;
                return;
            }

            // 질의내용(1): 기존 행(Id != Guid.Empty)에서 편집 불가
            if (columnIndex == 1 && qa.Id != Guid.Empty)
            {
                e.Cancel = true;
                return;
            }

            // 회신일자(2), 답변내용(3): 모든 행에서 편집 허용
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// DateTime? 바인딩용 ValueConverter
    /// 표시: yyyy-MM-dd 형식, null이면 빈 문자열
    /// 입력: 빈 문자열 → null, 유효한 날짜 → DateTime, 무효 → 기존값 유지(UnsetValue)
    /// </summary>
    public class NullableDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd");

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;

            if (string.IsNullOrWhiteSpace(str))
                return null!;

            if (DateTime.TryParseExact(str, new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            if (DateTime.TryParse(str, culture, DateTimeStyles.None, out var result2))
            {
                return result2;
            }

            // 파싱 실패 시 기존값 유지
            return DependencyProperty.UnsetValue;
        }
    }
}
