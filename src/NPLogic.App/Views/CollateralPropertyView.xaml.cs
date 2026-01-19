using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 담보물건 탭 - 물건 기본 정보, 등기부등본 정보, 감정평가 정보를 테이블 형태로 표시
    /// 피드백 반영: 위성도/지적도/로드뷰/토지이용계획/건축물대장 버튼을 한 행에 배치
    /// </summary>
    public partial class CollateralPropertyView : UserControl
    {
        public CollateralPropertyView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 위성도 열기 (카카오맵 위성지도)
        /// </summary>
        private void OpenSatelliteMap_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 카카오맵 위성지도 URL (좌표 기반 또는 주소 기반)
                // DisplayAddress는 지번주소를 우선 사용하여 깔끔한 검색 가능
                string url;
                if (vm.Property.Latitude.HasValue && vm.Property.Longitude.HasValue)
                {
                    url = $"https://map.kakao.com/?map_type=TYPE_SKYVIEW&q={vm.Property.Latitude},{vm.Property.Longitude}";
                }
                else
                {
                    var address = Uri.EscapeDataString(vm.Property.DisplayAddress ?? "");
                    url = $"https://map.kakao.com/?map_type=TYPE_SKYVIEW&q={address}";
                }
                
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"위성도를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 지적도 열기 (일사편리)
        /// </summary>
        private void OpenCadastralMap_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 일사편리 토지정보 (지적도) - 지번주소 사용
                var address = Uri.EscapeDataString(vm.Property.DisplayAddress ?? "");
                var url = $"https://www.eum.go.kr/web/ar/lu/luLandDet.jsp?is498&selAdd={address}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"지적도를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 로드뷰 열기 (카카오맵 로드뷰)
        /// </summary>
        private void OpenRoadView_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 카카오맵 로드뷰 URL
                string url;
                if (vm.Property.Latitude.HasValue && vm.Property.Longitude.HasValue)
                {
                    url = $"https://map.kakao.com/?map_type=TYPE_MAP&panoid=1&rv=on&rvlat={vm.Property.Latitude}&rvlng={vm.Property.Longitude}";
                }
                else
                {
                    var address = Uri.EscapeDataString(vm.Property.DisplayAddress ?? "");
                    url = $"https://map.kakao.com/?q={address}";
                }
                
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로드뷰를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 토지이용계획 열기 (토지이음)
        /// </summary>
        private void OpenLandUsePlan_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 토지이음 URL - 지번주소 사용
                var address = Uri.EscapeDataString(vm.Property.DisplayAddress ?? "");
                var url = $"https://www.eum.go.kr/web/ar/lu/luLandDet.jsp?isLu498&selAdd={address}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"토지이용계획을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 건축물대장 열기 (세움터)
        /// </summary>
        private void OpenBuildingRegister_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PropertyDetailViewModel;
            if (vm?.Property == null)
            {
                MessageBox.Show("물건 정보가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 세움터 건축물대장 URL
                var url = "https://cloud.eais.go.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"건축물대장을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
