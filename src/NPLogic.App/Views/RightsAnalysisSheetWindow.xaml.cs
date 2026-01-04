using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.Views
{
    /// <summary>
    /// 권리분석 시트 팝업 창
    /// 등기부등본에서 추출한 갑구/을구 데이터를 엑셀 형태로 표시
    /// </summary>
    public partial class RightsAnalysisSheetWindow : Window
    {
        private readonly RegistryRepository _registryRepository;
        private readonly Guid _propertyId;
        private readonly string _propertyInfo;

        private List<RegistryRight> _gapguRights = new();
        private List<RegistryRight> _eulguRights = new();
        private List<RegistryOwner> _owners = new();

        public RightsAnalysisSheetWindow(
            RegistryRepository registryRepository,
            Guid propertyId,
            string propertyInfo = "")
        {
            InitializeComponent();

            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
            _propertyId = propertyId;
            _propertyInfo = propertyInfo;

            PropertyInfoText.Text = string.IsNullOrEmpty(propertyInfo) 
                ? "물건 ID: " + propertyId.ToString().Substring(0, 8) + "..." 
                : propertyInfo;

            Loaded += async (s, e) => await LoadDataAsync();
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                // 갑구 데이터 로드
                _gapguRights = await _registryRepository.GetGapguRightsAsync(_propertyId);
                GapguDataGrid.ItemsSource = _gapguRights;
                GapguCountText.Text = $" | 총 {_gapguRights.Count}건";

                // 을구 데이터 로드
                _eulguRights = await _registryRepository.GetEulguRightsAsync(_propertyId);
                EulguDataGrid.ItemsSource = _eulguRights;
                EulguCountText.Text = $" | 총 {_eulguRights.Count}건";

                // 을구 합계 계산 (유효한 것만)
                var totalEulguAmount = _eulguRights
                    .Where(r => r.Status == "active")
                    .Sum(r => r.ClaimAmount ?? 0);
                EulguTotalText.Text = totalEulguAmount.ToString("N0") + "원";

                // 소유자 데이터 로드
                _owners = await _registryRepository.GetOwnersByPropertyIdAsync(_propertyId);
                OwnerDataGrid.ItemsSource = _owners;
                OwnerCountText.Text = $" | 총 {_owners.Count}명";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"데이터 로드 중 오류가 발생했습니다.\n\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// Excel 내보내기 버튼 클릭
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Excel 내보내기 기능 구현
                MessageBox.Show(
                    "Excel 내보내기 기능은 준비 중입니다.",
                    "알림",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Excel 내보내기 중 오류가 발생했습니다.\n\n{ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}



