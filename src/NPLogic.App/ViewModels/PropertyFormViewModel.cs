using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Views;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 물건 추가/수정 폼 ViewModel
    /// </summary>
    public partial class PropertyFormViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly AuthService _authService;
        private PropertyFormModal? _window;

        [ObservableProperty]
        private Property _property = new();

        [ObservableProperty]
        private string _title = "물건 추가";

        [ObservableProperty]
        private string _saveButtonText = "저장";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string _landAreaText = "";

        [ObservableProperty]
        private string _buildingAreaText = "";

        [ObservableProperty]
        private string _appraisalValueText = "";

        [ObservableProperty]
        private string _minimumBidText = "";

        [ObservableProperty]
        private string _salePriceText = "";

        private bool _isEditMode = false;

        public PropertyFormViewModel(
            PropertyRepository propertyRepository,
            AuthService authService)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // 기본값 설정
            Property.Id = Guid.NewGuid();
            Property.Status = "pending";
        }

        /// <summary>
        /// 창 참조 설정
        /// </summary>
        public void SetWindow(PropertyFormModal window)
        {
            _window = window;
        }

        /// <summary>
        /// 신규 물건 모드로 초기화
        /// </summary>
        public void InitializeForCreate()
        {
            _isEditMode = false;
            Title = "물건 추가";
            SaveButtonText = "추가";
            
            Property = new Property
            {
                Id = Guid.NewGuid(),
                Status = "pending"
            };

            // 현재 사용자를 생성자로 설정
            var session = _authService.GetSession();
            if (session?.User?.Id != null)
            {
                Property.CreatedBy = Guid.Parse(session.User.Id);
            }

            ClearTextFields();
        }

        /// <summary>
        /// 수정 모드로 초기화
        /// </summary>
        public void InitializeForEdit(Property property)
        {
            _isEditMode = true;
            Title = "물건 수정";
            SaveButtonText = "저장";

            Property = new Property
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

            // 텍스트 필드 초기화
            LandAreaText = property.LandArea?.ToString() ?? "";
            BuildingAreaText = property.BuildingArea?.ToString() ?? "";
            AppraisalValueText = property.AppraisalValue?.ToString("N0") ?? "";
            MinimumBidText = property.MinimumBid?.ToString("N0") ?? "";
            SalePriceText = property.SalePrice?.ToString("N0") ?? "";
        }

        /// <summary>
        /// 텍스트 필드 초기화
        /// </summary>
        private void ClearTextFields()
        {
            LandAreaText = "";
            BuildingAreaText = "";
            AppraisalValueText = "";
            MinimumBidText = "";
            SalePriceText = "";
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

                // 입력 검증
                if (!ValidateInput())
                    return;

                // 텍스트 필드를 숫자로 변환
                ParseNumericFields();

                // 저장
                if (_isEditMode)
                {
                    await _propertyRepository.UpdateAsync(Property);
                }
                else
                {
                    await _propertyRepository.CreateAsync(Property);
                }

                // 성공 시 창 닫기
                _window?.OnSaveSuccess();
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
        /// 입력 검증
        /// </summary>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Property.ProjectId))
            {
                ErrorMessage = "프로젝트 ID를 입력해주세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Property.PropertyType))
            {
                ErrorMessage = "물건 유형을 선택해주세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Property.AddressFull))
            {
                ErrorMessage = "전체 주소를 입력해주세요.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 텍스트 필드를 숫자로 변환
        /// </summary>
        private void ParseNumericFields()
        {
            // 토지 면적
            if (decimal.TryParse(LandAreaText, NumberStyles.Any, CultureInfo.InvariantCulture, out var landArea))
                Property.LandArea = landArea;
            else
                Property.LandArea = null;

            // 건물 면적
            if (decimal.TryParse(BuildingAreaText, NumberStyles.Any, CultureInfo.InvariantCulture, out var buildingArea))
                Property.BuildingArea = buildingArea;
            else
                Property.BuildingArea = null;

            // 감정가 (콤마 제거 후 파싱)
            var appraisalClean = AppraisalValueText.Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(appraisalClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var appraisalValue))
                Property.AppraisalValue = appraisalValue;
            else
                Property.AppraisalValue = null;

            // 최저입찰가 (콤마 제거 후 파싱)
            var minimumBidClean = MinimumBidText.Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(minimumBidClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var minimumBid))
                Property.MinimumBid = minimumBid;
            else
                Property.MinimumBid = null;

            // 매각가 (콤마 제거 후 파싱)
            var salePriceClean = SalePriceText.Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(salePriceClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var salePrice))
                Property.SalePrice = salePrice;
            else
                Property.SalePrice = null;
        }
    }
}

