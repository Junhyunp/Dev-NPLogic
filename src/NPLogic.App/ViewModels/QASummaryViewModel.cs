using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// QA 집계 ViewModel
    /// 피드백 섹션 11: QA 집계 메뉴 구현
    /// - 차주, 파트, 질문, 답변 형식으로 총괄 관리
    /// - 답변 엑셀 업로드 시 Q-A 매칭
    /// - 답변 도착 시 팝업 알람 기능
    /// </summary>
    public partial class QASummaryViewModel : ObservableObject
    {
        private readonly PropertyQaRepository _qaRepository;
        private readonly QaNotificationRepository _notificationRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly AuthService _authService;
        private readonly PermissionService _permissionService;

        // ========== 차주 목록 (좌측 패널) ==========
        [ObservableProperty]
        private ObservableCollection<BorrowerQaSummary> _borrowerSummaries = new();

        [ObservableProperty]
        private BorrowerQaSummary? _selectedBorrower;

        // ========== QA 목록 (우측 패널) ==========
        [ObservableProperty]
        private ObservableCollection<PropertyQa> _qaList = new();

        [ObservableProperty]
        private PropertyQa? _selectedQa;

        // ========== 필터 ==========
        [ObservableProperty]
        private string _selectedFilter = "all"; // all, unanswered, answered

        [ObservableProperty]
        private string _searchText = "";

        // ========== 통계 ==========
        [ObservableProperty]
        private int _totalQaCount;

        [ObservableProperty]
        private int _answeredCount;

        [ObservableProperty]
        private int _unansweredCount;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // ========== 현재 사용자 ==========
        [ObservableProperty]
        private User? _currentUser;

        // ========== QA 편집 모드 ==========
        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _editQuestion = "";

        [ObservableProperty]
        private string _editAnswer = "";

        [ObservableProperty]
        private string _editPart = "";

        // 파트 목록 (나중에 확정 시 수정)
        public ObservableCollection<string> PartOptions { get; } = new()
        {
            "",
            "등기부등본",
            "선순위",
            "평가",
            "경매일정",
            "권리분석",
            "기타"
        };

        public QASummaryViewModel(
            PropertyQaRepository qaRepository,
            QaNotificationRepository notificationRepository,
            PropertyRepository propertyRepository,
            AuthService authService,
            PermissionService permissionService)
        {
            _qaRepository = qaRepository ?? throw new ArgumentNullException(nameof(qaRepository));
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        }

        /// <summary>
        /// 초기화 (View Loaded 시 호출)
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 세션에서 사용자 ID 가져오기
                var session = _authService.GetSession();
                if (session?.User?.Id != null)
                {
                    var userId = Guid.Parse(session.User.Id);
                    CurrentUser = new User { Id = userId };
                }

                await LoadBorrowerSummariesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"초기화 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 차주별 QA 요약 로드
        /// </summary>
        private async Task LoadBorrowerSummariesAsync()
        {
            try
            {
                var summaries = await _qaRepository.GetBorrowerQaSummariesAsync();
                
                BorrowerSummaries.Clear();
                foreach (var summary in summaries)
                {
                    BorrowerSummaries.Add(summary);
                }

                // 통계 업데이트
                TotalQaCount = summaries.Sum(s => s.TotalCount);
                AnsweredCount = summaries.Sum(s => s.AnsweredCount);
                UnansweredCount = summaries.Sum(s => s.UnansweredCount);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"차주 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 선택된 차주의 QA 목록 로드
        /// </summary>
        private async Task LoadQaListAsync()
        {
            if (SelectedBorrower == null)
            {
                QaList.Clear();
                return;
            }

            try
            {
                IsLoading = true;
                var qaList = await _qaRepository.GetByBorrowerNumberAsync(SelectedBorrower.BorrowerNumber);

                // 필터 적용
                var filtered = ApplyFilter(qaList);

                QaList.Clear();
                foreach (var qa in filtered)
                {
                    QaList.Add(qa);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"QA 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 전체 QA 목록 로드 (차주 선택 없을 때)
        /// </summary>
        [RelayCommand]
        private async Task LoadAllQaAsync()
        {
            try
            {
                IsLoading = true;
                SelectedBorrower = null;
                
                var qaList = await _qaRepository.GetAllAsync();
                var filtered = ApplyFilter(qaList);

                QaList.Clear();
                foreach (var qa in filtered)
                {
                    QaList.Add(qa);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"전체 QA 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 필터 적용
        /// </summary>
        private List<PropertyQa> ApplyFilter(List<PropertyQa> qaList)
        {
            var filtered = qaList.AsEnumerable();

            // 상태 필터
            if (SelectedFilter == "unanswered")
            {
                filtered = filtered.Where(q => !q.IsAnswered);
            }
            else if (SelectedFilter == "answered")
            {
                filtered = filtered.Where(q => q.IsAnswered);
            }

            // 검색어 필터
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(q =>
                    (q.Question?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (q.Answer?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (q.BorrowerNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (q.BorrowerName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return filtered.ToList();
        }

        /// <summary>
        /// 차주 선택 변경 시
        /// </summary>
        partial void OnSelectedBorrowerChanged(BorrowerQaSummary? value)
        {
            _ = LoadQaListAsync();
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedFilterChanged(string value)
        {
            _ = LoadQaListAsync();
        }

        /// <summary>
        /// 검색어 변경 시
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            _ = LoadQaListAsync();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadBorrowerSummariesAsync();
            await LoadQaListAsync();
        }

        /// <summary>
        /// QA 추가
        /// </summary>
        [RelayCommand]
        private void AddQa()
        {
            if (SelectedBorrower == null)
            {
                MessageBox.Show("먼저 차주를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsEditMode = true;
            SelectedQa = null;
            EditQuestion = "";
            EditAnswer = "";
            EditPart = "";
        }

        /// <summary>
        /// QA 수정
        /// </summary>
        [RelayCommand]
        private void EditQa()
        {
            if (SelectedQa == null) return;

            IsEditMode = true;
            EditQuestion = SelectedQa.Question;
            EditAnswer = SelectedQa.Answer ?? "";
            EditPart = SelectedQa.Part ?? "";
        }

        /// <summary>
        /// QA 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveQaAsync()
        {
            if (string.IsNullOrWhiteSpace(EditQuestion))
            {
                MessageBox.Show("질문을 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                if (SelectedQa == null)
                {
                    // 새 QA 생성
                    var newQa = new PropertyQa
                    {
                        PropertyId = Guid.Empty, // TODO: 물건 선택 기능 추가 시 수정
                        Question = EditQuestion,
                        Answer = string.IsNullOrWhiteSpace(EditAnswer) ? null : EditAnswer,
                        Part = string.IsNullOrWhiteSpace(EditPart) ? null : EditPart,
                        BorrowerNumber = SelectedBorrower?.BorrowerNumber,
                        BorrowerName = SelectedBorrower?.BorrowerName,
                        CreatedBy = CurrentUser?.Id
                    };

                    await _qaRepository.CreateAsync(newQa);
                }
                else
                {
                    // 기존 QA 수정
                    SelectedQa.Question = EditQuestion;
                    SelectedQa.Answer = string.IsNullOrWhiteSpace(EditAnswer) ? null : EditAnswer;
                    SelectedQa.Part = string.IsNullOrWhiteSpace(EditPart) ? null : EditPart;

                    if (!string.IsNullOrWhiteSpace(EditAnswer) && !SelectedQa.IsAnswered)
                    {
                        SelectedQa.AnsweredBy = CurrentUser?.Id;
                        SelectedQa.AnsweredAt = DateTime.UtcNow;
                    }

                    await _qaRepository.UpdateAsync(SelectedQa);
                }

                IsEditMode = false;
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 편집 취소
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            IsEditMode = false;
            EditQuestion = "";
            EditAnswer = "";
            EditPart = "";
        }

        /// <summary>
        /// QA 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteQaAsync()
        {
            if (SelectedQa == null) return;

            try
            {
                // 권한 체크: Property를 통해 ProgramId 확인
                var property = await _propertyRepository.GetByIdAsync(SelectedQa.PropertyId);
                if (property?.ProgramId.HasValue == true)
                {
                    var canDelete = await _permissionService.CanDeleteAsync(property.ProgramId.Value, CurrentUser);
                    if (!canDelete)
                    {
                        ErrorMessage = PermissionService.GetNoPermissionMessage("delete");
                        return;
                    }
                }

                var result = MessageBox.Show(
                    "선택한 QA를 삭제하시겠습니까?",
                    "삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;
                await _qaRepository.DeleteAsync(SelectedQa.Id);
                await RefreshAsync();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("QA가 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 답변 엑셀 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadAnswerExcelAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "답변 엑셀 파일 선택"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                IsLoading = true;
                var filePath = dialog.FileName;

                // TODO: 엑셀 파일 파싱 및 답변 매칭 로직
                // 양식이 확정되면 구현 예정
                
                MessageBox.Show(
                    "엑셀 업로드 기능은 양식 확정 후 구현 예정입니다.\n" +
                    $"선택된 파일: {Path.GetFileName(filePath)}",
                    "안내",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"엑셀 업로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// QA 목록 엑셀 내보내기
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"QA집계_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "QA 목록 내보내기"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                IsLoading = true;
                
                // TODO: 엑셀 내보내기 구현
                MessageBox.Show(
                    "엑셀 내보내기 기능은 추후 구현 예정입니다.",
                    "안내",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"엑셀 내보내기 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 에러 메시지 클리어
        /// </summary>
        [RelayCommand]
        private void ClearError()
        {
            ErrorMessage = null;
        }

        /// <summary>
        /// 필터 버튼 클릭 (전체/미답변/답변완료)
        /// </summary>
        [RelayCommand]
        private void SetFilter(string filter)
        {
            SelectedFilter = filter;
        }
    }
}
