using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 작업 이력 ViewModel
    /// </summary>
    public partial class AuditLogsViewModel : ObservableObject
    {
        private readonly AuditLogRepository _auditLogRepository;

        [ObservableProperty]
        private ObservableCollection<AuditLog> _auditLogs = new();

        [ObservableProperty]
        private AuditLog? _selectedLog;

        // ========== 필터 ==========
        [ObservableProperty]
        private ObservableCollection<string> _tableNames = new() { "전체" };

        [ObservableProperty]
        private string _selectedTableName = "전체";

        [ObservableProperty]
        private ObservableCollection<string> _actions = new()
        {
            "전체", "INSERT", "UPDATE", "DELETE"
        };

        [ObservableProperty]
        private string _selectedAction = "전체";

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private string _searchKeyword = "";

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private int _totalCount;

        // ========== 상세 보기 ==========
        [ObservableProperty]
        private bool _isDetailVisible;

        [ObservableProperty]
        private string _detailOldData = "";

        [ObservableProperty]
        private string _detailNewData = "";

        public AuditLogsViewModel(AuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            
            // 기본 날짜 범위: 최근 30일
            EndDate = DateTime.Today;
            StartDate = DateTime.Today.AddDays(-30);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await LoadTableNamesAsync();
                await LoadLogsAsync();
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
        /// 테이블 목록 로드
        /// </summary>
        private async Task LoadTableNamesAsync()
        {
            try
            {
                var tables = await _auditLogRepository.GetTableNamesAsync();
                TableNames.Clear();
                TableNames.Add("전체");
                foreach (var table in tables)
                {
                    TableNames.Add(table);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"테이블 목록 로드 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 이력 로드
        /// </summary>
        private async Task LoadLogsAsync()
        {
            try
            {
                IsLoading = true;

                var logs = await _auditLogRepository.GetFilteredAsync(
                    tableName: SelectedTableName == "전체" ? null : SelectedTableName,
                    action: SelectedAction == "전체" ? null : SelectedAction,
                    startDate: StartDate,
                    endDate: EndDate,
                    limit: 500
                );

                // 키워드 필터링 (클라이언트 사이드)
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    logs = logs.Where(l =>
                        (l.UserEmail?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (l.TableName?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (l.OldData?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (l.NewData?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                AuditLogs.Clear();
                foreach (var log in logs)
                {
                    AuditLogs.Add(log);
                }

                TotalCount = AuditLogs.Count;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"이력 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedTableNameChanged(string value) => _ = LoadLogsAsync();
        partial void OnSelectedActionChanged(string value) => _ = LoadLogsAsync();
        partial void OnStartDateChanged(DateTime? value) => _ = LoadLogsAsync();
        partial void OnEndDateChanged(DateTime? value) => _ = LoadLogsAsync();

        /// <summary>
        /// 선택된 로그 변경 시
        /// </summary>
        partial void OnSelectedLogChanged(AuditLog? value)
        {
            if (value != null)
            {
                IsDetailVisible = true;
                DetailOldData = FormatJson(value.OldData);
                DetailNewData = FormatJson(value.NewData);
            }
            else
            {
                IsDetailVisible = false;
                DetailOldData = "";
                DetailNewData = "";
            }
        }

        /// <summary>
        /// JSON 포맷팅
        /// </summary>
        private string FormatJson(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return "(데이터 없음)";

            try
            {
                var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
                return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// 검색
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadLogsAsync();
        }

        /// <summary>
        /// 필터 초기화
        /// </summary>
        [RelayCommand]
        private async Task ResetFilterAsync()
        {
            SelectedTableName = "전체";
            SelectedAction = "전체";
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            SearchKeyword = "";
            await LoadLogsAsync();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        /// <summary>
        /// 상세 닫기
        /// </summary>
        [RelayCommand]
        private void CloseDetail()
        {
            SelectedLog = null;
            IsDetailVisible = false;
        }
    }
}

