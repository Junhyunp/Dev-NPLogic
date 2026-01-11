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
    /// 프로그램 기초데이터 설정 ViewModel (C-001 ~ C-005)
    /// </summary>
    public partial class ProgramSettingsViewModel : ObservableObject
    {
        private readonly ReferenceDataRepository _referenceDataRepository;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        // ========== 1. 법원별 정보 (C-001) ==========
        [ObservableProperty]
        private ObservableCollection<Court> _courts = new();

        [ObservableProperty]
        private Court? _selectedCourt;

        // ========== 2. 법률적용률 (C-002) ==========
        [ObservableProperty]
        private ObservableCollection<LegalApplicationRate> _legalRates = new();

        [ObservableProperty]
        private LegalApplicationRate? _selectedLegalRate;

        // ========== 3 & 4. 임대차 기준 (C-003, C-004) ==========
        [ObservableProperty]
        private ObservableCollection<LeaseStandard> _leaseStandards = new();

        [ObservableProperty]
        private LeaseStandard? _selectedLeaseStandard;

        // ========== 5. 경매비용 산정 데이터 (C-005) ==========
        [ObservableProperty]
        private ObservableCollection<AuctionCostStandard> _auctionCostStandards = new();

        [ObservableProperty]
        private AuctionCostStandard? _selectedAuctionCostStandard;

        public ProgramSettingsViewModel(ReferenceDataRepository referenceDataRepository)
        {
            _referenceDataRepository = referenceDataRepository ?? throw new ArgumentNullException(nameof(referenceDataRepository));
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await Task.WhenAll(
                    LoadCourtsAsync(),
                    LoadLegalRatesAsync(),
                    LoadLeaseStandardsAsync(),
                    LoadAuctionCostStandardsAsync()
                );
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

        // ========== 로드 메서드 ==========

        private async Task LoadCourtsAsync()
        {
            var data = await _referenceDataRepository.GetCourtsAsync();
            Courts.Clear();
            foreach (var item in data) Courts.Add(item);
        }

        private async Task LoadLegalRatesAsync()
        {
            var data = await _referenceDataRepository.GetLegalApplicationRatesAsync();
            LegalRates.Clear();
            foreach (var item in data) LegalRates.Add(item);
        }

        private async Task LoadLeaseStandardsAsync()
        {
            var data = await _referenceDataRepository.GetLeaseStandardsAsync();
            LeaseStandards.Clear();
            foreach (var item in data) LeaseStandards.Add(item);
        }

        private async Task LoadAuctionCostStandardsAsync()
        {
            var data = await _referenceDataRepository.GetAuctionCostStandardsAsync();
            AuctionCostStandards.Clear();
            foreach (var item in data) AuctionCostStandards.Add(item);
        }

        // ========== 명령 (추가/저장/삭제) ==========

        [RelayCommand]
        private async Task SaveCourtAsync()
        {
            if (SelectedCourt == null) return;
            try { await _referenceDataRepository.UpdateCourtAsync(SelectedCourt); }
            catch (Exception ex) { ErrorMessage = $"법원 저장 실패: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task SaveLegalRateAsync()
        {
            if (SelectedLegalRate == null) return;
            try { await _referenceDataRepository.UpdateLegalApplicationRateAsync(SelectedLegalRate); }
            catch (Exception ex) { ErrorMessage = $"법률적용률 저장 실패: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task SaveLeaseStandardAsync()
        {
            if (SelectedLeaseStandard == null) return;
            try { await _referenceDataRepository.UpdateLeaseStandardAsync(SelectedLeaseStandard); }
            catch (Exception ex) { ErrorMessage = $"임대차 기준 저장 실패: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task SaveAuctionCostStandardAsync()
        {
            if (SelectedAuctionCostStandard == null) return;
            try { await _referenceDataRepository.UpdateAuctionCostStandardAsync(SelectedAuctionCostStandard); }
            catch (Exception ex) { ErrorMessage = $"경매비용 기준 저장 실패: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task RefreshAsync() => await InitializeAsync();
    }
}
