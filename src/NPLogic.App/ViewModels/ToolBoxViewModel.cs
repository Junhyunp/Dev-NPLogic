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
    /// Tool Box ViewModel
    /// </summary>
    public partial class ToolBoxViewModel : ObservableObject
    {
        private readonly ReferenceDataRepository _referenceDataRepository;

        [ObservableProperty]
        private int _selectedTabIndex;

        // 법원 데이터
        [ObservableProperty]
        private ObservableCollection<Court> _courts = new();

        [ObservableProperty]
        private Court? _selectedCourt;

        // 금융기관 데이터
        [ObservableProperty]
        private ObservableCollection<FinancialInstitution> _financialInstitutions = new();

        [ObservableProperty]
        private FinancialInstitution? _selectedFinancialInstitution;

        // 감정평가기관 데이터
        [ObservableProperty]
        private ObservableCollection<AppraisalFirm> _appraisalFirms = new();

        [ObservableProperty]
        private AppraisalFirm? _selectedAppraisalFirm;

        // 공통 코드 데이터
        [ObservableProperty]
        private ObservableCollection<CommonCode> _commonCodes = new();

        [ObservableProperty]
        private ObservableCollection<string> _codeGroups = new();

        [ObservableProperty]
        private string? _selectedCodeGroup;

        private bool _isLoadingCodes;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public ToolBoxViewModel(ReferenceDataRepository referenceDataRepository)
        {
            _referenceDataRepository = referenceDataRepository ?? throw new ArgumentNullException(nameof(referenceDataRepository));
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

                await LoadCourtsAsync();
                await LoadFinancialInstitutionsAsync();
                await LoadAppraisalFirmsAsync();
                await LoadCommonCodesAsync();
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

        // ========== 법원 ==========

        private async Task LoadCourtsAsync()
        {
            try
            {
                var courts = await _referenceDataRepository.GetCourtsAsync();
                Courts.Clear();
                foreach (var court in courts)
                {
                    Courts.Add(court);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"법원 목록 로드 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddCourtAsync()
        {
            try
            {
                var newCourt = new Court
                {
                    Id = Guid.NewGuid(),
                    CourtCode = $"C-{DateTime.Now:yyyyMMddHHmmss}",
                    CourtName = "새 법원",
                    IsActive = true
                };

                await _referenceDataRepository.CreateCourtAsync(newCourt);
                await LoadCourtsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"법원 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveCourtAsync()
        {
            if (SelectedCourt == null) return;

            try
            {
                await _referenceDataRepository.UpdateCourtAsync(SelectedCourt);
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("저장되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"법원 저장 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteCourtAsync(Court court)
        {
            if (court == null) return;

            try
            {
                await _referenceDataRepository.DeleteCourtAsync(court.Id);
                await LoadCourtsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"법원 삭제 실패: {ex.Message}";
            }
        }

        // ========== 금융기관 ==========

        private async Task LoadFinancialInstitutionsAsync()
        {
            try
            {
                var institutions = await _referenceDataRepository.GetFinancialInstitutionsAsync();
                FinancialInstitutions.Clear();
                foreach (var fi in institutions)
                {
                    FinancialInstitutions.Add(fi);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"금융기관 목록 로드 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddFinancialInstitutionAsync()
        {
            try
            {
                var newFi = new FinancialInstitution
                {
                    Id = Guid.NewGuid(),
                    InstitutionCode = $"FI-{DateTime.Now:yyyyMMddHHmmss}",
                    InstitutionName = "새 금융기관",
                    InstitutionType = "은행",
                    IsActive = true
                };

                await _referenceDataRepository.CreateFinancialInstitutionAsync(newFi);
                await LoadFinancialInstitutionsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"금융기관 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteFinancialInstitutionAsync(FinancialInstitution fi)
        {
            if (fi == null) return;

            try
            {
                await _referenceDataRepository.DeleteFinancialInstitutionAsync(fi.Id);
                await LoadFinancialInstitutionsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"금융기관 삭제 실패: {ex.Message}";
            }
        }

        // ========== 감정평가기관 ==========

        private async Task LoadAppraisalFirmsAsync()
        {
            try
            {
                var firms = await _referenceDataRepository.GetAppraisalFirmsAsync();
                AppraisalFirms.Clear();
                foreach (var firm in firms)
                {
                    AppraisalFirms.Add(firm);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"감정평가기관 목록 로드 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddAppraisalFirmAsync()
        {
            try
            {
                var newFirm = new AppraisalFirm
                {
                    Id = Guid.NewGuid(),
                    FirmCode = $"AF-{DateTime.Now:yyyyMMddHHmmss}",
                    FirmName = "새 감정평가기관",
                    IsActive = true
                };

                await _referenceDataRepository.CreateAppraisalFirmAsync(newFirm);
                await LoadAppraisalFirmsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"감정평가기관 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteAppraisalFirmAsync(AppraisalFirm firm)
        {
            if (firm == null) return;

            try
            {
                await _referenceDataRepository.DeleteAppraisalFirmAsync(firm.Id);
                await LoadAppraisalFirmsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"감정평가기관 삭제 실패: {ex.Message}";
            }
        }

        // ========== 공통 코드 ==========

        private async Task LoadCommonCodesAsync()
        {
            if (_isLoadingCodes) return;
            _isLoadingCodes = true;

            try
            {
                var codes = await _referenceDataRepository.GetCommonCodesAsync();
                CommonCodes.Clear();
                foreach (var code in codes)
                {
                    CommonCodes.Add(code);
                }

                // 코드 그룹 추출
                var groups = codes.Select(c => c.CodeGroup).Distinct().OrderBy(g => g).ToList();
                CodeGroups.Clear();
                CodeGroups.Add("전체");
                foreach (var group in groups)
                {
                    CodeGroups.Add(group);
                }
                SelectedCodeGroup = CodeGroups.FirstOrDefault();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"공통 코드 목록 로드 실패: {ex.Message}";
            }
            finally
            {
                _isLoadingCodes = false;
            }
        }

        partial void OnSelectedCodeGroupChanged(string? value)
        {
            if (_isLoadingCodes) return;
            _ = FilterCommonCodesAsync();
        }

        private async Task FilterCommonCodesAsync()
        {
            if (string.IsNullOrEmpty(SelectedCodeGroup) || SelectedCodeGroup == "전체")
            {
                await LoadCommonCodesAsync();
            }
            else
            {
                try
                {
                    var codes = await _referenceDataRepository.GetCommonCodesByGroupAsync(SelectedCodeGroup);
                    CommonCodes.Clear();
                    foreach (var code in codes)
                    {
                        CommonCodes.Add(code);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"공통 코드 필터링 실패: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task AddCommonCodeAsync()
        {
            try
            {
                var newCode = new CommonCode
                {
                    Id = Guid.NewGuid(),
                    CodeGroup = SelectedCodeGroup == "전체" ? "NEW_GROUP" : SelectedCodeGroup ?? "NEW_GROUP",
                    CodeValue = $"CODE_{DateTime.Now:HHmmss}",
                    CodeName = "새 코드",
                    SortOrder = CommonCodes.Count + 1,
                    IsActive = true
                };

                await _referenceDataRepository.CreateCommonCodeAsync(newCode);
                await LoadCommonCodesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"공통 코드 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteCommonCodeAsync(CommonCode code)
        {
            if (code == null) return;

            try
            {
                await _referenceDataRepository.DeleteCommonCodeAsync(code.Id);
                await LoadCommonCodesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"공통 코드 삭제 실패: {ex.Message}";
            }
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

