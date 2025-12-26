using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// PropertyDetailView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PropertyDetailView : UserControl
    {
        public PropertyDetailView()
        {
            InitializeComponent();
        }

        private async void PropertyDetailView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is PropertyDetailViewModel viewModel)
            {
                await viewModel.InitializeAsync();

                // 지도 준비 완료 이벤트 구독
                PropertyMapView.MapReady += async (s, args) =>
                {
                    await UpdateMapLocationAsync(viewModel);
                };

                // 평가 탭 DataContext 설정
                if (viewModel.EvaluationViewModel != null)
                {
                    EvaluationTabView.DataContext = viewModel.EvaluationViewModel;
                    await viewModel.EvaluationViewModel.LoadAsync();
                }

                // ViewModel의 Property 변경 감지
                viewModel.PropertyChanged += async (s, args) =>
                {
                    if (args.PropertyName == nameof(PropertyDetailViewModel.Property))
                    {
                        await UpdateMapLocationAsync(viewModel);
                    }
                };
            }
        }

        /// <summary>
        /// 지도에 물건 위치 업데이트
        /// </summary>
        private async System.Threading.Tasks.Task UpdateMapLocationAsync(PropertyDetailViewModel viewModel)
        {
            var property = viewModel.Property;
            if (property == null) return;

            var address = property.AddressFull ?? property.AddressRoad ?? property.AddressJibun;

            // 위도/경도가 있는 경우
            if (property.Latitude.HasValue && property.Longitude.HasValue)
            {
                await PropertyMapView.SetLocationAsync(
                    (double)property.Latitude.Value,
                    (double)property.Longitude.Value,
                    address
                );
            }
            // 위도/경도가 없고 주소가 있는 경우
            else if (!string.IsNullOrEmpty(address))
            {
                // 기본 좌표로 설정 (주소 기반 검색은 MapView 내부에서 처리)
                await PropertyMapView.SetLocationAsync(37.5665, 126.9780, address);
            }
        }
    }
}

