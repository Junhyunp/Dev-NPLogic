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
    /// 선순위 관리 ViewModel
    /// </summary>
    public partial class SeniorRightsViewModel : ObservableObject
    {
        private readonly RegistryRepository _registryRepository;
        private readonly PropertyRepository _propertyRepository;

        [ObservableProperty]
        private ObservableCollection<RegistryRight> _rights = new();

        [ObservableProperty]
        private RegistryRight? _selectedRight;

        // ========== 필터 ==========
        [ObservableProperty]
        private ObservableCollection<string> _rightTypes = new() { "전체", "갑구", "을구" };

        [ObservableProperty]
        private string _selectedRightType = "전체";

        [ObservableProperty]
        private ObservableCollection<string> _statuses = new() { "전체", "active", "cancelled" };

        [ObservableProperty]
        private string _selectedStatus = "전체";

        [ObservableProperty]
        private string _searchKeyword = "";

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
        private decimal _totalClaimAmount;

        // ========== 편집 ==========
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isNewRecord;

        // 편집 필드
        [ObservableProperty]
        private string _editRightType = "gap";

        [ObservableProperty]
        private int _editRightOrder = 1;

        [ObservableProperty]
        private string _editRightHolder = "";

        [ObservableProperty]
        private decimal _editClaimAmount;

        [ObservableProperty]
        private DateTime? _editRegistrationDate;

        [ObservableProperty]
        private string _editRegistrationNumber = "";

        [ObservableProperty]
        private string _editRegistrationCause = "";

        [ObservableProperty]
        private string _editStatus = "active";

        [ObservableProperty]
        private string _editNotes = "";

        public SeniorRightsViewModel(RegistryRepository registryRepository, PropertyRepository propertyRepository)
        {
            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
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
                await LoadRightsAsync();
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
        /// 권리 목록 로드
        /// </summary>
        private async Task LoadRightsAsync()
        {
            try
            {
                IsLoading = true;

                List<RegistryRight> rights;

                if (SelectedProperty != null)
                {
                    rights = await _registryRepository.GetRightsByPropertyIdAsync(SelectedProperty.Id);
                }
                else
                {
                    rights = await _registryRepository.GetAllRightsAsync();
                }

                // 필터 적용
                if (SelectedRightType != "전체")
                {
                    var typeFilter = SelectedRightType == "갑구" ? "gap" : "eul";
                    rights = rights.Where(r => r.RightType == typeFilter).ToList();
                }

                if (SelectedStatus != "전체")
                {
                    rights = rights.Where(r => r.Status == SelectedStatus).ToList();
                }

                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    rights = rights.Where(r =>
                        (r.RightHolder?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.RegistrationCause?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.RegistrationNumber?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                Rights.Clear();
                foreach (var right in rights.OrderBy(r => r.RightOrder))
                {
                    Rights.Add(right);
                }

                TotalCount = Rights.Count;
                TotalClaimAmount = Rights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"권리 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 필터 변경 시
        /// </summary>
        partial void OnSelectedRightTypeChanged(string value) => _ = LoadRightsAsync();
        partial void OnSelectedStatusChanged(string value) => _ = LoadRightsAsync();
        partial void OnSelectedPropertyChanged(Property? value) => _ = LoadRightsAsync();

        /// <summary>
        /// 선택된 권리 변경 시
        /// </summary>
        partial void OnSelectedRightChanged(RegistryRight? value)
        {
            if (value != null)
            {
                EditRightType = value.RightType;
                EditRightOrder = value.RightOrder ?? 1;
                EditRightHolder = value.RightHolder ?? "";
                EditClaimAmount = value.ClaimAmount ?? 0;
                EditRegistrationDate = value.RegistrationDate;
                EditRegistrationNumber = value.RegistrationNumber ?? "";
                EditRegistrationCause = value.RegistrationCause ?? "";
                EditStatus = value.Status;
                EditNotes = value.Notes ?? "";
                IsEditing = true;
                IsNewRecord = false;
            }
        }

        /// <summary>
        /// 새 권리 추가
        /// </summary>
        [RelayCommand]
        private void NewRight()
        {
            SelectedRight = null;
            EditRightType = "gap";
            EditRightOrder = Rights.Count > 0 ? Rights.Max(r => r.RightOrder ?? 0) + 1 : 1;
            EditRightHolder = "";
            EditClaimAmount = 0;
            EditRegistrationDate = DateTime.Today;
            EditRegistrationNumber = "";
            EditRegistrationCause = "";
            EditStatus = "active";
            EditNotes = "";
            IsEditing = true;
            IsNewRecord = true;
        }

        /// <summary>
        /// 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(EditRightHolder))
            {
                ErrorMessage = "권리자/채권자를 입력하세요.";
                return;
            }

            try
            {
                IsLoading = true;

                if (IsNewRecord)
                {
                    var newRight = new RegistryRight
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = SelectedProperty?.Id,
                        RightType = EditRightType,
                        RightOrder = EditRightOrder,
                        RightHolder = EditRightHolder,
                        ClaimAmount = EditClaimAmount,
                        RegistrationDate = EditRegistrationDate,
                        RegistrationNumber = EditRegistrationNumber,
                        RegistrationCause = EditRegistrationCause,
                        Status = EditStatus,
                        Notes = EditNotes
                    };

                    await _registryRepository.CreateRightAsync(newRight);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리가 생성되었습니다.");
                }
                else if (SelectedRight != null)
                {
                    SelectedRight.RightType = EditRightType;
                    SelectedRight.RightOrder = EditRightOrder;
                    SelectedRight.RightHolder = EditRightHolder;
                    SelectedRight.ClaimAmount = EditClaimAmount;
                    SelectedRight.RegistrationDate = EditRegistrationDate;
                    SelectedRight.RegistrationNumber = EditRegistrationNumber;
                    SelectedRight.RegistrationCause = EditRegistrationCause;
                    SelectedRight.Status = EditStatus;
                    SelectedRight.Notes = EditNotes;

                    await _registryRepository.UpdateRightAsync(SelectedRight);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리가 수정되었습니다.");
                }

                await LoadRightsAsync();
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
            if (SelectedRight == null) return;

            try
            {
                IsLoading = true;
                await _registryRepository.DeleteRightAsync(SelectedRight.Id);
                await LoadRightsAsync();
                CancelEdit();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("권리가 삭제되었습니다.");
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
            SelectedRight = null;
            IsEditing = false;
            IsNewRecord = false;
        }

        /// <summary>
        /// 검색
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadRightsAsync();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }
    }
}

