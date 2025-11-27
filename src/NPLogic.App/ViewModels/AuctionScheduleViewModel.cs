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
    /// 경매 일정 ViewModel
    /// </summary>
    public partial class AuctionScheduleViewModel : ObservableObject
    {
        private readonly AuctionScheduleRepository _scheduleRepository;
        private readonly PropertyRepository _propertyRepository;

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

        // ========== 물건 선택 ==========
        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        [ObservableProperty]
        private Property? _selectedProperty;

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
        private Guid? _editPropertyId;

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

        // ========== 보기 모드 ==========
        [ObservableProperty]
        private bool _isCalendarView;

        public AuctionScheduleViewModel(AuctionScheduleRepository scheduleRepository, PropertyRepository propertyRepository)
        {
            _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));

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

                await LoadPropertiesAsync();
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
        /// 물건 목록 로드
        /// </summary>
        private async Task LoadPropertiesAsync()
        {
            try
            {
                var properties = await _propertyRepository.GetAllAsync();
                Properties.Clear();
                foreach (var prop in properties)
                {
                    Properties.Add(prop);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 목록 로드 실패: {ex.Message}";
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

                // 물건 필터
                if (SelectedProperty != null)
                {
                    schedules = schedules.Where(s => s.PropertyId == SelectedProperty.Id).ToList();
                }

                Schedules.Clear();
                foreach (var schedule in schedules.OrderBy(s => s.AuctionDate))
                {
                    // 물건 정보 연결
                    schedule.Property = Properties.FirstOrDefault(p => p.Id == schedule.PropertyId);
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
        partial void OnSelectedPropertyChanged(Property? value) => _ = LoadSchedulesAsync();

        /// <summary>
        /// 선택된 일정 변경 시
        /// </summary>
        partial void OnSelectedScheduleChanged(AuctionSchedule? value)
        {
            if (value != null)
            {
                EditPropertyId = value.PropertyId;
                EditAuctionNumber = value.AuctionNumber ?? "";
                EditAuctionDate = value.AuctionDate;
                EditBidDate = value.BidDate;
                EditMinimumBid = value.MinimumBid ?? 0;
                EditSalePrice = value.SalePrice ?? 0;
                EditStatus = value.Status;
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
            EditPropertyId = SelectedProperty?.Id;
            EditAuctionNumber = "";
            EditAuctionDate = DateTime.Today.AddDays(7);
            EditBidDate = null;
            EditMinimumBid = 0;
            EditSalePrice = 0;
            EditStatus = "scheduled";
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
                ErrorMessage = "경매일을 입력하세요.";
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
                        PropertyId = EditPropertyId,
                        AuctionNumber = EditAuctionNumber,
                        AuctionDate = EditAuctionDate,
                        BidDate = EditBidDate,
                        MinimumBid = EditMinimumBid,
                        SalePrice = EditSalePrice,
                        Status = EditStatus
                    };

                    await _scheduleRepository.CreateAsync(newSchedule);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경매 일정이 생성되었습니다.");
                }
                else if (SelectedSchedule != null)
                {
                    SelectedSchedule.PropertyId = EditPropertyId;
                    SelectedSchedule.AuctionNumber = EditAuctionNumber;
                    SelectedSchedule.AuctionDate = EditAuctionDate;
                    SelectedSchedule.BidDate = EditBidDate;
                    SelectedSchedule.MinimumBid = EditMinimumBid;
                    SelectedSchedule.SalePrice = EditSalePrice;
                    SelectedSchedule.Status = EditStatus;

                    await _scheduleRepository.UpdateAsync(SelectedSchedule);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경매 일정이 수정되었습니다.");
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
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("경매 일정이 삭제되었습니다.");
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
    }
}

