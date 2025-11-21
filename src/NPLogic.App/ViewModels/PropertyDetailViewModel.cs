using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 물건 상세 ViewModel
    /// </summary>
    public partial class PropertyDetailViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;

        [ObservableProperty]
        private Property _property = new();

        [ObservableProperty]
        private Property _originalProperty = new();

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        private Guid? _propertyId;
        private Action? _goBackAction;

        public PropertyDetailViewModel(PropertyRepository propertyRepository)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        }

        /// <summary>
        /// 물건 ID로 초기화
        /// </summary>
        public void SetPropertyId(Guid propertyId, Action? goBackAction = null)
        {
            _propertyId = propertyId;
            _goBackAction = goBackAction;
        }

        /// <summary>
        /// Property 객체로 직접 로드
        /// </summary>
        public void LoadProperty(Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _propertyId = property.Id;
            Property = property;

            // 원본 복사 (변경 감지용)
            OriginalProperty = new Property
            {
                Id = property.Id,
                ProjectId = property.ProjectId,
                PropertyNumber = property.PropertyNumber,
                PropertyType = property.PropertyType,
                AddressFull = property.AddressFull,
                AddressRoad = property.AddressRoad,
                AddressJibun = property.AddressJibun,
                AddressDetail = property.AddressDetail,
                LandArea = property.LandArea,
                BuildingArea = property.BuildingArea,
                Floors = property.Floors,
                CompletionDate = property.CompletionDate,
                AppraisalValue = property.AppraisalValue,
                MinimumBid = property.MinimumBid,
                SalePrice = property.SalePrice,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                Status = property.Status,
                AssignedTo = property.AssignedTo,
                CreatedBy = property.CreatedBy,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt
            };

            HasUnsavedChanges = false;
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_propertyId == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var property = await _propertyRepository.GetByIdAsync(_propertyId.Value);
                if (property != null)
                {
                    Property = property;
                    
                    // 원본 복사 (변경 감지용)
                    OriginalProperty = new Property
                    {
                        Id = property.Id,
                        ProjectId = property.ProjectId,
                        PropertyNumber = property.PropertyNumber,
                        PropertyType = property.PropertyType,
                        AddressFull = property.AddressFull,
                        AddressRoad = property.AddressRoad,
                        AddressJibun = property.AddressJibun,
                        AddressDetail = property.AddressDetail,
                        LandArea = property.LandArea,
                        BuildingArea = property.BuildingArea,
                        Floors = property.Floors,
                        CompletionDate = property.CompletionDate,
                        AppraisalValue = property.AppraisalValue,
                        MinimumBid = property.MinimumBid,
                        SalePrice = property.SalePrice,
                        Latitude = property.Latitude,
                        Longitude = property.Longitude,
                        Status = property.Status,
                        AssignedTo = property.AssignedTo,
                        CreatedBy = property.CreatedBy,
                        CreatedAt = property.CreatedAt,
                        UpdatedAt = property.UpdatedAt
                    };
                }
                else
                {
                    ErrorMessage = "물건을 찾을 수 없습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 저장 명령
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                await _propertyRepository.UpdateAsync(Property);
                
                // 성공 메시지
                SuccessMessage = "저장되었습니다.";
                HasUnsavedChanges = false;

                // 원본 업데이트
                await InitializeAsync();
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
        /// 새로고침 명령
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (HasUnsavedChanges)
            {
                // TODO: 확인 다이얼로그 표시
                // 지금은 그냥 새로고침
            }

            await InitializeAsync();
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// 뒤로가기 명령
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            if (HasUnsavedChanges)
            {
                // TODO: 확인 다이얼로그 표시
                // 지금은 그냥 뒤로가기
            }

            _goBackAction?.Invoke();
        }

        /// <summary>
        /// Property 변경 시
        /// </summary>
        partial void OnPropertyChanged(Property value)
        {
            // 변경 감지 (간단 버전)
            HasUnsavedChanges = true;
        }
    }
}

