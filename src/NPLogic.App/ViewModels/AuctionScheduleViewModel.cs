using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 경공매 일정 ViewModel
    /// </summary>
    public partial class AuctionScheduleViewModel : ObservableObject
    {
        private readonly AuctionScheduleRepository _scheduleRepository;
        private readonly SupabaseService _supabaseService;

        [ObservableProperty]
        private ObservableCollection<AuctionSchedule> _schedules = new();

        [ObservableProperty]
        private AuctionSchedule? _selectedSchedule;

        // ========== 필터 ==========
        [ObservableProperty]
        private ObservableCollection<string> _statuses = new() { "전체", "scheduled", "completed", "cancelled" };

        [ObservableProperty]
        private string _selectedStatus = "전체";

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        // ========== 경매/공매 필터 (A-001) ==========
        [ObservableProperty]
        private bool _showAuction = true;

        [ObservableProperty]
        private bool _showPublicSale = true;

        // ========== 상태 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _scheduledCount;

        [ObservableProperty]
        private int _completedCount;

        // ========== 편집 ==========
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isNewRecord;

        // 편집 필드
        [ObservableProperty]
        private string _editAuctionNumber = "";

        [ObservableProperty]
        private DateTime? _editAuctionDate;

        [ObservableProperty]
        private DateTime? _editBidDate;

        [ObservableProperty]
        private decimal _editMinimumBid;

        [ObservableProperty]
        private decimal _editSalePrice;

        [ObservableProperty]
        private string _editStatus = "scheduled";

        // 인터링/상계회수 편집 필드
        [ObservableProperty]
        private decimal _editInterimPrincipalOffset;

        [ObservableProperty]
        private decimal _editInterimPrincipalRecovery;

        [ObservableProperty]
        private decimal _editInterimInterestOffset;

        [ObservableProperty]
        private decimal _editInterimInterestRecovery;

        // ========== 경공매 유형 편집 (A-001) ==========
        [ObservableProperty]
        private string _editScheduleType = "auction";

        // ========== 대법원 캡처 편집 (A-002) ==========
        [ObservableProperty]
        private string? _editCaseCaptureUrl;

        [ObservableProperty]
        private string? _editScheduleCaptureUrl;

        [ObservableProperty]
        private string? _editDocumentCaptureUrl;

        // ========== 보기 모드 ==========
        [ObservableProperty]
        private bool _isCalendarView;

        public AuctionScheduleViewModel(AuctionScheduleRepository scheduleRepository, SupabaseService supabaseService)
        {
            _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));

            // 기본 날짜 범위: 이번 달
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
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

                await LoadSchedulesAsync();
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
        /// 일정 목록 로드
        /// </summary>
        private async Task LoadSchedulesAsync()
        {
            try
            {
                IsLoading = true;

                var schedules = await _scheduleRepository.GetAllAsync();

                // 경매/공매 필터 (A-001)
                schedules = schedules.Where(s =>
                    (ShowAuction && s.ScheduleType == "auction") ||
                    (ShowPublicSale && s.ScheduleType == "public_sale")
                ).ToList();

                // 상태 필터
                if (SelectedStatus != "전체")
                {
                    schedules = schedules.Where(s => s.Status == SelectedStatus).ToList();
                }

                // 날짜 필터
                if (StartDate.HasValue)
                {
                    schedules = schedules.Where(s => s.AuctionDate >= StartDate.Value).ToList();
                }
                if (EndDate.HasValue)
                {
                    schedules = schedules.Where(s => s.AuctionDate <= EndDate.Value).ToList();
                }

                Schedules.Clear();
                foreach (var schedule in schedules.OrderBy(s => s.AuctionDate))
                {
                    Schedules.Add(schedule);
                }

                TotalCount = Schedules.Count;
                ScheduledCount = Schedules.Count(s => s.Status == "scheduled");
                CompletedCount = Schedules.Count(s => s.Status == "completed");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"일정 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedStatusChanged(string value) => _ = LoadSchedulesAsync();
        partial void OnStartDateChanged(DateTime? value) => _ = LoadSchedulesAsync();
        partial void OnEndDateChanged(DateTime? value) => _ = LoadSchedulesAsync();
        partial void OnShowAuctionChanged(bool value) => _ = LoadSchedulesAsync();
        partial void OnShowPublicSaleChanged(bool value) => _ = LoadSchedulesAsync();

        /// <summary>
        /// 선택된 일정 변경 시
        /// </summary>
        partial void OnSelectedScheduleChanged(AuctionSchedule? value)
        {
            if (value != null)
            {
                EditScheduleType = value.ScheduleType;
                EditAuctionNumber = value.AuctionNumber ?? "";
                EditAuctionDate = value.AuctionDate;
                EditBidDate = value.BidDate;
                EditMinimumBid = value.MinimumBid ?? 0;
                EditSalePrice = value.SalePrice ?? 0;
                EditStatus = value.Status;
                EditInterimPrincipalOffset = value.InterimPrincipalOffset;
                EditInterimPrincipalRecovery = value.InterimPrincipalRecovery;
                EditInterimInterestOffset = value.InterimInterestOffset;
                EditInterimInterestRecovery = value.InterimInterestRecovery;
                EditCaseCaptureUrl = value.CaseCaptureUrl;
                EditScheduleCaptureUrl = value.ScheduleCaptureUrl;
                EditDocumentCaptureUrl = value.DocumentCaptureUrl;
                IsEditing = true;
                IsNewRecord = false;
            }
        }

        /// <summary>
        /// 새 일정 추가
        /// </summary>
        [RelayCommand]
        private void NewSchedule()
        {
            SelectedSchedule = null;
            EditScheduleType = "auction";
            EditAuctionNumber = "";
            EditAuctionDate = DateTime.Today.AddDays(7);
            EditBidDate = null;
            EditMinimumBid = 0;
            EditSalePrice = 0;
            EditStatus = "scheduled";
            EditInterimPrincipalOffset = 0;
            EditInterimPrincipalRecovery = 0;
            EditInterimInterestOffset = 0;
            EditInterimInterestRecovery = 0;
            EditCaseCaptureUrl = null;
            EditScheduleCaptureUrl = null;
            EditDocumentCaptureUrl = null;
            IsEditing = true;
            IsNewRecord = true;
        }

        /// <summary>
        /// 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!EditAuctionDate.HasValue)
            {
                ErrorMessage = "경공매일을 입력하세요.";
                return;
            }

            try
            {
                IsLoading = true;

                if (IsNewRecord)
                {
                    var newSchedule = new AuctionSchedule
                    {
                        Id = Guid.NewGuid(),
                        ScheduleType = EditScheduleType,
                        AuctionNumber = EditAuctionNumber,
                        AuctionDate = EditAuctionDate,
                        BidDate = EditBidDate,
                        MinimumBid = EditMinimumBid,
                        SalePrice = EditSalePrice,
                        Status = EditStatus,
                        InterimPrincipalOffset = EditInterimPrincipalOffset,
                        InterimPrincipalRecovery = EditInterimPrincipalRecovery,
                        InterimInterestOffset = EditInterimInterestOffset,
                        InterimInterestRecovery = EditInterimInterestRecovery,
                        CaseCaptureUrl = EditCaseCaptureUrl,
                        ScheduleCaptureUrl = EditScheduleCaptureUrl,
                        DocumentCaptureUrl = EditDocumentCaptureUrl
                    };

                    await _scheduleRepository.CreateAsync(newSchedule);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경공매 일정이 생성되었습니다.");
                }
                else if (SelectedSchedule != null)
                {
                    SelectedSchedule.ScheduleType = EditScheduleType;
                    SelectedSchedule.AuctionNumber = EditAuctionNumber;
                    SelectedSchedule.AuctionDate = EditAuctionDate;
                    SelectedSchedule.BidDate = EditBidDate;
                    SelectedSchedule.MinimumBid = EditMinimumBid;
                    SelectedSchedule.SalePrice = EditSalePrice;
                    SelectedSchedule.Status = EditStatus;
                    SelectedSchedule.InterimPrincipalOffset = EditInterimPrincipalOffset;
                    SelectedSchedule.InterimPrincipalRecovery = EditInterimPrincipalRecovery;
                    SelectedSchedule.InterimInterestOffset = EditInterimInterestOffset;
                    SelectedSchedule.InterimInterestRecovery = EditInterimInterestRecovery;
                    SelectedSchedule.CaseCaptureUrl = EditCaseCaptureUrl;
                    SelectedSchedule.ScheduleCaptureUrl = EditScheduleCaptureUrl;
                    SelectedSchedule.DocumentCaptureUrl = EditDocumentCaptureUrl;

                    await _scheduleRepository.UpdateAsync(SelectedSchedule);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경공매 일정이 수정되었습니다.");
                }

                await LoadSchedulesAsync();
                CancelEdit();
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
        /// 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedSchedule == null) return;

            try
            {
                IsLoading = true;
                await _scheduleRepository.DeleteAsync(SelectedSchedule.Id);
                await LoadSchedulesAsync();
                CancelEdit();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경공매 일정이 삭제되었습니다.");
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
        /// 편집 취소
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            SelectedSchedule = null;
            IsEditing = false;
            IsNewRecord = false;
        }

        /// <summary>
        /// 보기 모드 전환
        /// </summary>
        [RelayCommand]
        private void ToggleView()
        {
            IsCalendarView = !IsCalendarView;
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
        /// 이번 주 일정 보기
        /// </summary>
        [RelayCommand]
        private async Task ShowThisWeekAsync()
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            StartDate = today.AddDays(-dayOfWeek);
            EndDate = StartDate.Value.AddDays(6);
            await LoadSchedulesAsync();
        }

        /// <summary>
        /// 이번 달 일정 보기
        /// </summary>
        [RelayCommand]
        private async Task ShowThisMonthAsync()
        {
            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
            await LoadSchedulesAsync();
        }

        // ========== 대법원 캡처 이미지 업로드 (A-002) ==========

        /// <summary>
        /// 사건내역 캡처 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadCaseCaptureAsync()
        {
            var url = await UploadCaptureImageAsync("case");
            if (url != null)
            {
                EditCaseCaptureUrl = url;
            }
        }

        /// <summary>
        /// 기일내역 캡처 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadScheduleCaptureAsync()
        {
            var url = await UploadCaptureImageAsync("schedule");
            if (url != null)
            {
                EditScheduleCaptureUrl = url;
            }
        }

        /// <summary>
        /// 문건송달내역 캡처 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadDocumentCaptureAsync()
        {
            var url = await UploadCaptureImageAsync("document");
            if (url != null)
            {
                EditDocumentCaptureUrl = url;
            }
        }

        /// <summary>
        /// 캡처 이미지 업로드 (공통)
        /// </summary>
        private async Task<string?> UploadCaptureImageAsync(string captureType)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.gif;*.bmp|모든 파일|*.*",
                    Title = $"{GetCaptureTypeName(captureType)} 캡처 이미지 선택"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    
                    var fileName = System.IO.Path.GetFileName(dialog.FileName);
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
                    var storagePath = $"auction-captures/{captureType}/{Guid.NewGuid()}/{fileName}";

                    var client = await _supabaseService.GetClientAsync();
                    var result = await client.Storage
                        .From("captures")
                        .Upload(fileBytes, storagePath);

                    if (result != null)
                    {
                        var publicUrl = client.Storage
                            .From("captures")
                            .GetPublicUrl(storagePath);
                        
                        NPLogic.UI.Services.ToastService.Instance.ShowSuccess($"{GetCaptureTypeName(captureType)} 캡처가 업로드되었습니다.");
                        return publicUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"캡처 업로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
            
            return null;
        }

        private static string GetCaptureTypeName(string captureType) => captureType switch
        {
            "case" => "사건내역",
            "schedule" => "기일내역",
            "document" => "문건송달내역",
            _ => captureType
        };

        /// <summary>
        /// 캡처 이미지 팝업으로 확대 보기
        /// </summary>
        [RelayCommand]
        private void ShowCaptureImage(string? captureUrl)
        {
            if (string.IsNullOrEmpty(captureUrl)) return;

            var popup = new NPLogic.Views.ImagePopupWindow(
                "대법원 경매 캡처",
                captureUrl,
                NPLogic.Views.ImagePopupWindow.PopupType.Image);
            popup.Owner = System.Windows.Application.Current.MainWindow;
            popup.ShowDialog();
        }
    }
}

