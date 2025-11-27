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
    /// 설정 관리 ViewModel
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsRepository _settingsRepository;

        [ObservableProperty]
        private int _selectedTabIndex;

        // ========== 계산 수식 ==========
        [ObservableProperty]
        private ObservableCollection<CalculationFormula> _formulas = new();

        [ObservableProperty]
        private CalculationFormula? _selectedFormula;

        [ObservableProperty]
        private ObservableCollection<string> _formulaCategories = new()
        {
            "전체", "권리분석", "평가", "XNPV", "경매", "기타"
        };

        [ObservableProperty]
        private string? _selectedFormulaCategory = "전체";

        // ========== 데이터 매핑 ==========
        [ObservableProperty]
        private ObservableCollection<DataMapping> _dataMappings = new();

        [ObservableProperty]
        private DataMapping? _selectedDataMapping;

        // ========== 시스템 설정 ==========
        [ObservableProperty]
        private ObservableCollection<SystemSetting> _systemSettings = new();

        [ObservableProperty]
        private SystemSetting? _selectedSystemSetting;

        // ========== 공통 ==========
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // ========== 수식 편집용 ==========
        [ObservableProperty]
        private bool _isFormulaEditing;

        [ObservableProperty]
        private string _editFormulaName = "";

        [ObservableProperty]
        private string _editFormulaExpression = "";

        [ObservableProperty]
        private string _editFormulaDescription = "";

        [ObservableProperty]
        private string _editFormulaAppliesTo = "";

        [ObservableProperty]
        private bool _editFormulaIsActive = true;

        public SettingsViewModel(SettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
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

                await LoadFormulasAsync();
                await LoadDataMappingsAsync();
                await LoadSystemSettingsAsync();
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

        // ========== 계산 수식 ==========

        private async Task LoadFormulasAsync()
        {
            try
            {
                var formulas = await _settingsRepository.GetFormulasAsync();
                Formulas.Clear();
                foreach (var formula in formulas)
                {
                    Formulas.Add(formula);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"수식 목록 로드 실패: {ex.Message}";
            }
        }

        partial void OnSelectedFormulaCategoryChanged(string? value)
        {
            _ = FilterFormulasAsync();
        }

        private async Task FilterFormulasAsync()
        {
            if (string.IsNullOrEmpty(SelectedFormulaCategory) || SelectedFormulaCategory == "전체")
            {
                await LoadFormulasAsync();
            }
            else
            {
                try
                {
                    var formulas = await _settingsRepository.GetFormulasByApplicabilityAsync(SelectedFormulaCategory);
                    Formulas.Clear();
                    foreach (var formula in formulas)
                    {
                        Formulas.Add(formula);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"수식 필터링 실패: {ex.Message}";
                }
            }
        }

        partial void OnSelectedFormulaChanged(CalculationFormula? value)
        {
            if (value != null)
            {
                EditFormulaName = value.FormulaName;
                EditFormulaExpression = value.FormulaExpression;
                EditFormulaDescription = value.FormulaDescription ?? "";
                EditFormulaAppliesTo = value.AppliesTo ?? "";
                EditFormulaIsActive = value.IsActive;
                IsFormulaEditing = true;
            }
            else
            {
                ClearFormulaForm();
            }
        }

        private void ClearFormulaForm()
        {
            EditFormulaName = "";
            EditFormulaExpression = "";
            EditFormulaDescription = "";
            EditFormulaAppliesTo = "";
            EditFormulaIsActive = true;
            IsFormulaEditing = false;
        }

        [RelayCommand]
        private void NewFormula()
        {
            SelectedFormula = null;
            ClearFormulaForm();
            EditFormulaName = "새 수식";
            IsFormulaEditing = true;
        }

        [RelayCommand]
        private async Task SaveFormulaAsync()
        {
            try
            {
                IsLoading = true;

                if (SelectedFormula != null)
                {
                    // 수정
                    SelectedFormula.FormulaName = EditFormulaName;
                    SelectedFormula.FormulaExpression = EditFormulaExpression;
                    SelectedFormula.FormulaDescription = EditFormulaDescription;
                    SelectedFormula.AppliesTo = EditFormulaAppliesTo;
                    SelectedFormula.IsActive = EditFormulaIsActive;

                    await _settingsRepository.UpdateFormulaAsync(SelectedFormula);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("수식이 저장되었습니다.");
                }
                else
                {
                    // 신규
                    var newFormula = new CalculationFormula
                    {
                        Id = Guid.NewGuid(),
                        FormulaName = EditFormulaName,
                        FormulaExpression = EditFormulaExpression,
                        FormulaDescription = EditFormulaDescription,
                        AppliesTo = EditFormulaAppliesTo,
                        IsActive = EditFormulaIsActive
                    };

                    await _settingsRepository.CreateFormulaAsync(newFormula);
                    NPLogic.UI.Services.ToastService.Instance.ShowSuccess("수식이 생성되었습니다.");
                }

                await LoadFormulasAsync();
                ClearFormulaForm();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"수식 저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DeleteFormulaAsync()
        {
            if (SelectedFormula == null) return;

            try
            {
                await _settingsRepository.DeleteFormulaAsync(SelectedFormula.Id);
                await LoadFormulasAsync();
                ClearFormulaForm();
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("수식이 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"수식 삭제 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CancelFormulaEdit()
        {
            SelectedFormula = null;
            ClearFormulaForm();
        }

        // ========== 데이터 매핑 ==========

        private async Task LoadDataMappingsAsync()
        {
            try
            {
                var mappings = await _settingsRepository.GetDataMappingsAsync();
                DataMappings.Clear();
                foreach (var mapping in mappings)
                {
                    DataMappings.Add(mapping);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 매핑 로드 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddDataMappingAsync()
        {
            try
            {
                var newMapping = new DataMapping
                {
                    Id = Guid.NewGuid(),
                    SettingKey = $"MAPPING_{DateTime.Now:yyyyMMddHHmmss}",
                    SettingValue = "",
                    SettingType = "mapping",
                    Description = "새 매핑"
                };

                await _settingsRepository.CreateDataMappingAsync(newMapping);
                await LoadDataMappingsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 매핑 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveDataMappingAsync()
        {
            if (SelectedDataMapping == null) return;

            try
            {
                await _settingsRepository.UpdateDataMappingAsync(SelectedDataMapping);
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("매핑이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 매핑 저장 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteDataMappingAsync(DataMapping mapping)
        {
            if (mapping == null) return;

            try
            {
                await _settingsRepository.DeleteDataMappingAsync(mapping.Id);
                await LoadDataMappingsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 매핑 삭제 실패: {ex.Message}";
            }
        }

        // ========== 시스템 설정 ==========

        private async Task LoadSystemSettingsAsync()
        {
            try
            {
                var settings = await _settingsRepository.GetSystemSettingsAsync();
                SystemSettings.Clear();
                foreach (var setting in settings)
                {
                    SystemSettings.Add(setting);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"시스템 설정 로드 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddSystemSettingAsync()
        {
            try
            {
                var newSetting = new SystemSetting
                {
                    Id = Guid.NewGuid(),
                    SettingKey = $"SETTING_{DateTime.Now:yyyyMMddHHmmss}",
                    SettingValue = "",
                    SettingType = "STRING",
                    Description = "새 설정"
                };

                await _settingsRepository.CreateSystemSettingAsync(newSetting);
                await LoadSystemSettingsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"시스템 설정 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveSystemSettingAsync()
        {
            if (SelectedSystemSetting == null) return;

            try
            {
                await _settingsRepository.UpdateSystemSettingAsync(SelectedSystemSetting);
                NPLogic.UI.Services.ToastService.Instance.ShowSuccess("설정이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"시스템 설정 저장 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteSystemSettingAsync(SystemSetting setting)
        {
            if (setting == null) return;

            try
            {
                await _settingsRepository.DeleteSystemSettingAsync(setting.Id);
                await LoadSystemSettingsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"시스템 설정 삭제 실패: {ex.Message}";
            }
        }

        // ========== 공통 ==========

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }
    }
}

