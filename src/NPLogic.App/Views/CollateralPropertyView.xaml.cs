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
        /// 지적도 열기 (국가공간정보포털 지적도)
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
                // 국가공간정보포털 지적도 (지번주소 기반 검색)
                // 또는 네이버 지도 지적편집도 사용
                string url;
                if (vm.Property.Latitude.HasValue && vm.Property.Longitude.HasValue)
                {
                    // 좌표가 있으면 네이버 지도 지적편집도로 이동
                    url = $"https://map.naver.com/p/search/{Uri.EscapeDataString(vm.Property.DisplayAddress ?? "")}?c=15.00,0,0,2,dh&isCorrectAnswer=true";
                }
                else
                {
                    // 주소 기반 검색
                    var address = Uri.EscapeDataString(vm.Property.DisplayAddress ?? "");
                    url = $"https://map.naver.com/p/search/{address}?c=15.00,0,0,2,dh&isCorrectAnswer=true";
                }
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
                // 카카오맵 로드뷰 URL - 주소 검색 후 로드뷰 모드로 열기
                var address = Uri.EscapeDataString(vm.Property.DisplayAddress ?? "");
                string url;
                if (vm.Property.Latitude.HasValue && vm.Property.Longitude.HasValue)
                {
                    // 좌표가 있으면 직접 로드뷰 위치 지정
                    url = $"https://map.kakao.com/?urlLevel=3&urlX={vm.Property.Longitude}&urlY={vm.Property.Latitude}&rv=1";
                }
                else
                {
                    // 주소로 검색 후 로드뷰 모드
                    url = $"https://map.kakao.com/?q={address}&rv=1";
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
                // 토지이음 토지이용계획확인원 - 직접 검색 페이지로 이동
                // 주소를 복사해서 검색할 수 있도록 주소를 클립보드에 복사
                var address = vm.Property.DisplayAddress ?? "";
                if (!string.IsNullOrEmpty(address))
                {
                    System.Windows.Clipboard.SetText(address);
                }
                
                // 토지이음 토지이용계획 검색 페이지
                var url = "https://www.eum.go.kr/web/ar/lu/luLandSrch.jsp";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                
                if (!string.IsNullOrEmpty(address))
                {
                    MessageBox.Show($"주소가 클립보드에 복사되었습니다.\n검색창에 붙여넣기(Ctrl+V) 하세요.\n\n주소: {address}", 
                        "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"토지이용계획을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 건축물대장 열기 (정부24 건축물대장 열람)
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
                // 정부24 건축물대장 열람 페이지로 직접 이동
                // 주소를 클립보드에 복사하여 검색 편의 제공
                var address = vm.Property.DisplayAddress ?? "";
                if (!string.IsNullOrEmpty(address))
                {
                    System.Windows.Clipboard.SetText(address);
                }
                
                // 정부24 건축물대장 열람/발급 페이지 (로그인 불필요 열람 가능)
                var url = "https://www.gov.kr/mw/AA020InfoCappView.do?HighCtgCD=A09002&CappBizCD=13100000015";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                
                if (!string.IsNullOrEmpty(address))
                {
                    MessageBox.Show($"주소가 클립보드에 복사되었습니다.\n검색창에 붙여넣기(Ctrl+V) 하세요.\n\n주소: {address}", 
                        "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"건축물대장을 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
