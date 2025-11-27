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
    /// 등기부 탭 ViewModel
    /// </summary>
    public partial class RegistryTabViewModel : ObservableObject
    {
        private readonly RegistryRepository _registryRepository;
        private Guid? _propertyId;

        #region Observable Properties

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // ========== 기본 정보 ==========
        
        /// <summary>
        /// 물건번호 (지번일련번호)
        /// </summary>
        [ObservableProperty]
        private string? _propertyNumber;

        /// <summary>
        /// 물건지 (등기부 기준)
        /// </summary>
        [ObservableProperty]
        private string? _registryAddress;

        /// <summary>
        /// 물건지 (DD 기준)
        /// </summary>
        [ObservableProperty]
        private string? _ddAddress;

        /// <summary>
        /// 주소 일치 여부
        /// </summary>
        [ObservableProperty]
        private bool _isAddressMatch = true;

        /// <summary>
        /// 담보물 형태
        /// </summary>
        [ObservableProperty]
        private string? _collateralType;

        /// <summary>
        /// 대지면적 (평)
        /// </summary>
        [ObservableProperty]
        private decimal? _landAreaPyeong;

        /// <summary>
        /// 건물면적 (평)
        /// </summary>
        [ObservableProperty]
        private decimal? _buildingAreaPyeong;

        // ========== 소유자 정보 ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryOwner> _owners = new();

        [ObservableProperty]
        private RegistryOwner? _selectedOwner;

        [ObservableProperty]
        private bool _hasNoOwners = true;

        // ========== 갑구 (소유권) ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryRight> _gapguRights = new();

        [ObservableProperty]
        private RegistryRight? _selectedGapguRight;

        [ObservableProperty]
        private bool _hasNoGapguRights = true;

        /// <summary>
        /// 갑구 청구금액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _gapguTotalAmount;

        // ========== 을구 (근저당/전세권) ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryRight> _eulguRights = new();

        [ObservableProperty]
        private RegistryRight? _selectedEulguRight;

        [ObservableProperty]
        private bool _hasNoEulguRights = true;

        /// <summary>
        /// 을구 채권최고액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _eulguTotalAmount;

        // ========== 등기부 문서 ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryDocument> _documents = new();

        [ObservableProperty]
        private bool _hasNoDocuments = true;

        #endregion

        public RegistryTabViewModel(RegistryRepository registryRepository)
        {
            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
        }

        /// <summary>
        /// 물건 ID 설정 및 초기화
        /// </summary>
        public void SetPropertyId(Guid propertyId)
        {
            _propertyId = propertyId;
        }

        /// <summary>
        /// 물건 정보로 기본 정보 설정
        /// </summary>
        public void SetPropertyInfo(Property property)
        {
            if (property == null) return;

            PropertyNumber = property.PropertyNumber;
            RegistryAddress = property.AddressFull;
            DdAddress = property.AddressFull; // DD와 비교할 주소 (추후 DD 데이터에서 가져옴)
            IsAddressMatch = true; // 추후 비교 로직 구현
            CollateralType = property.PropertyType;

            // 면적 평 환산 (1평 = 3.3058㎡)
            const decimal pyeongConverter = 3.3058m;
            LandAreaPyeong = property.LandArea.HasValue ? Math.Round(property.LandArea.Value / pyeongConverter, 2) : null;
            BuildingAreaPyeong = property.BuildingArea.HasValue ? Math.Round(property.BuildingArea.Value / pyeongConverter, 2) : null;
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (_propertyId == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 소유자 정보 로드
                var owners = await _registryRepository.GetOwnersByPropertyIdAsync(_propertyId.Value);
                Owners = new ObservableCollection<RegistryOwner>(owners);
                HasNoOwners = Owners.Count == 0;

                // 갑구 권리 로드
                var gapguRights = await _registryRepository.GetGapguRightsAsync(_propertyId.Value);
                GapguRights = new ObservableCollection<RegistryRight>(gapguRights);
                HasNoGapguRights = GapguRights.Count == 0;
                GapguTotalAmount = GapguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);

                // 을구 권리 로드
                var eulguRights = await _registryRepository.GetEulguRightsAsync(_propertyId.Value);
                EulguRights = new ObservableCollection<RegistryRight>(eulguRights);
                HasNoEulguRights = EulguRights.Count == 0;
                EulguTotalAmount = EulguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);

                // 등기부 문서 로드
                var documents = await _registryRepository.GetDocumentsByPropertyIdAsync(_propertyId.Value);
                Documents = new ObservableCollection<RegistryDocument>(documents);
                HasNoDocuments = Documents.Count == 0;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 소유자 관련 Commands

        [RelayCommand]
        private async Task AddOwnerAsync()
        {
            if (_propertyId == null) return;

            try
            {
                var newOwner = new RegistryOwner
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId,
                    OwnerName = "새 소유자",
                    ShareRatio = "100%",
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _registryRepository.CreateOwnerAsync(newOwner);
                Owners.Add(created);
                HasNoOwners = false;
                SelectedOwner = created;
                SuccessMessage = "소유자가 추가되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"소유자 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteOwnerAsync()
        {
            if (SelectedOwner == null) return;

            try
            {
                await _registryRepository.DeleteOwnerAsync(SelectedOwner.Id);
                Owners.Remove(SelectedOwner);
                HasNoOwners = Owners.Count == 0;
                SelectedOwner = null;
                SuccessMessage = "소유자가 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"소유자 삭제 실패: {ex.Message}";
            }
        }

        #endregion

        #region 갑구 관련 Commands

        [RelayCommand]
        private async Task AddGapguRightAsync()
        {
            if (_propertyId == null) return;

            try
            {
                var maxOrder = GapguRights.Any() ? GapguRights.Max(r => r.RightOrder ?? 0) : 0;
                var newRight = new RegistryRight
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId,
                    RightType = "gap",
                    RightOrder = maxOrder + 1,
                    RegistrationCause = "가압류",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _registryRepository.CreateRightAsync(newRight);
                GapguRights.Add(created);
                HasNoGapguRights = false;
                SelectedGapguRight = created;
                UpdateGapguTotal();
                SuccessMessage = "갑구 권리가 추가되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"갑구 권리 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteGapguRightAsync()
        {
            if (SelectedGapguRight == null) return;

            try
            {
                await _registryRepository.DeleteRightAsync(SelectedGapguRight.Id);
                GapguRights.Remove(SelectedGapguRight);
                HasNoGapguRights = GapguRights.Count == 0;
                SelectedGapguRight = null;
                UpdateGapguTotal();
                SuccessMessage = "갑구 권리가 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"갑구 권리 삭제 실패: {ex.Message}";
            }
        }

        private void UpdateGapguTotal()
        {
            GapguTotalAmount = GapguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);
        }

        #endregion

        #region 을구 관련 Commands

        [RelayCommand]
        private async Task AddEulguRightAsync()
        {
            if (_propertyId == null) return;

            try
            {
                var maxOrder = EulguRights.Any() ? EulguRights.Max(r => r.RightOrder ?? 0) : 0;
                var newRight = new RegistryRight
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId,
                    RightType = "eul",
                    RightOrder = maxOrder + 1,
                    RegistrationCause = "근저당권",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _registryRepository.CreateRightAsync(newRight);
                EulguRights.Add(created);
                HasNoEulguRights = false;
                SelectedEulguRight = created;
                UpdateEulguTotal();
                SuccessMessage = "을구 권리가 추가되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"을구 권리 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteEulguRightAsync()
        {
            if (SelectedEulguRight == null) return;

            try
            {
                await _registryRepository.DeleteRightAsync(SelectedEulguRight.Id);
                EulguRights.Remove(SelectedEulguRight);
                HasNoEulguRights = EulguRights.Count == 0;
                SelectedEulguRight = null;
                UpdateEulguTotal();
                SuccessMessage = "을구 권리가 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"을구 권리 삭제 실패: {ex.Message}";
            }
        }

        private void UpdateEulguTotal()
        {
            EulguTotalAmount = EulguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);
        }

        #endregion

        #region 권리 저장 Command

        [RelayCommand]
        private async Task SaveAllRightsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 소유자 저장
                foreach (var owner in Owners)
                {
                    await _registryRepository.UpdateOwnerAsync(owner);
                }

                // 갑구 저장
                foreach (var right in GapguRights)
                {
                    await _registryRepository.UpdateRightAsync(right);
                }

                // 을구 저장
                foreach (var right in EulguRights)
                {
                    await _registryRepository.UpdateRightAsync(right);
                }

                SuccessMessage = "모든 등기부 정보가 저장되었습니다.";
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

        #endregion

        #region 새로고침 Command

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        #endregion
    }
}

