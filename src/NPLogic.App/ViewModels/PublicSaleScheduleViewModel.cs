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
    /// 공매 일정 ViewModel
    /// </summary>
    public partial class PublicSaleScheduleViewModel : ObservableObject
    {
        private readonly PublicSaleScheduleRepository _scheduleRepository;
        private readonly PropertyRepository _propertyRepository;

        [ObservableProperty]
        private ObservableCollection<PublicSaleSchedule> _schedules = new();

        [ObservableProperty]
        private PublicSaleSchedule? _selectedSchedule;

        [ObservableProperty]
        private ObservableCollection<string> _statuses = new() { "전체", "scheduled", "completed", "cancelled" };

        [ObservableProperty]
        private string _selectedStatus = "전체";

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private ObservableCollection<Property> _properties = new();

        [ObservableProperty]
        private Property? _selectedProperty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isNewRecord;

        [ObservableProperty]
        private Guid? _editPropertyId;

        [ObservableProperty]
        private string _editSaleNumber = "";

        [ObservableProperty]
        private DateTime? _editSaleDate;

        [ObservableProperty]
        private decimal _editMinimumBid;

        [ObservableProperty]
        private decimal _editSalePrice;

        [ObservableProperty]
        private string _editStatus = "scheduled";

        public PublicSaleScheduleViewModel(PublicSaleScheduleRepository scheduleRepository, PropertyRepository propertyRepository)
        {
            _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));

            StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
        }

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

        private async Task LoadPropertiesAsync()
        {
            var properties = await _propertyRepository.GetAllAsync();
            Properties.Clear();
            foreach (var prop in properties)
            {
                Properties.Add(prop);
            }
        }

        private async Task LoadSchedulesAsync()
        {
            try
            {
                IsLoading = true;

                var schedules = await _scheduleRepository.GetAllAsync();

                if (SelectedStatus != "전체")
                    schedules = schedules.Where(s => s.Status == SelectedStatus).ToList();

                if (StartDate.HasValue)
                    schedules = schedules.Where(s => s.SaleDate >= StartDate.Value).ToList();

                if (EndDate.HasValue)
                    schedules = schedules.Where(s => s.SaleDate <= EndDate.Value).ToList();

                if (SelectedProperty != null)
                    schedules = schedules.Where(s => s.PropertyId == SelectedProperty.Id).ToList();

                Schedules.Clear();
                foreach (var schedule in schedules.OrderBy(s => s.SaleDate))
                {
                    schedule.Property = Properties.FirstOrDefault(p => p.Id == schedule.PropertyId);
                    Schedules.Add(schedule);
                }

                TotalCount = Schedules.Count;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"일정 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedStatusChanged(string value) => _ = LoadSchedulesAsync();
        partial void OnStartDateChanged(DateTime? value) => _ = LoadSchedulesAsync();
        partial void OnEndDateChanged(DateTime? value) => _ = LoadSchedulesAsync();
        partial void OnSelectedPropertyChanged(Property? value) => _ = LoadSchedulesAsync();

        partial void OnSelectedScheduleChanged(PublicSaleSchedule? value)
        {
            if (value != null)
            {
                EditPropertyId = value.PropertyId;
                EditSaleNumber = value.SaleNumber ?? "";
                EditSaleDate = value.SaleDate;
                EditMinimumBid = value.MinimumBid ?? 0;
                EditSalePrice = value.SalePrice ?? 0;
                EditStatus = value.Status;
                IsEditing = true;
                IsNewRecord = false;
            }
        }

        [RelayCommand]
        private void NewSchedule()
        {
            SelectedSchedule = null;
            EditPropertyId = SelectedProperty?.Id;
            EditSaleNumber = "";
            EditSaleDate = DateTime.Today.AddDays(7);
            EditMinimumBid = 0;
            EditSalePrice = 0;
            EditStatus = "scheduled";
            IsEditing = true;
            IsNewRecord = true;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!EditSaleDate.HasValue)
            {
                ErrorMessage = "공매일을 입력하세요.";
                return;
            }

            try
            {
                IsLoading = true;

                if (IsNewRecord)
                {
                    var newSchedule = new PublicSaleSchedule
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = EditPropertyId,
                        SaleNumber = EditSaleNumber,
                        SaleDate = EditSaleDate,
                        MinimumBid = EditMinimumBid,
                        SalePrice = EditSalePrice,
                        Status = EditStatus
                    };

                    await _scheduleRepository.CreateAsync(newSchedule);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("공매 일정이 생성되었습니다.");
                }
                else if (SelectedSchedule != null)
                {
                    SelectedSchedule.PropertyId = EditPropertyId;
                    SelectedSchedule.SaleNumber = EditSaleNumber;
                    SelectedSchedule.SaleDate = EditSaleDate;
                    SelectedSchedule.MinimumBid = EditMinimumBid;
                    SelectedSchedule.SalePrice = EditSalePrice;
                    SelectedSchedule.Status = EditStatus;

                    await _scheduleRepository.UpdateAsync(SelectedSchedule);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("공매 일정이 수정되었습니다.");
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
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("공매 일정이 삭제되었습니다.");
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

        [RelayCommand]
        private void CancelEdit()
        {
            SelectedSchedule = null;
            IsEditing = false;
            IsNewRecord = false;
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }
    }
}

