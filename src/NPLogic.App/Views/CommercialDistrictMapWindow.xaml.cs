using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.Core;
using NPLogic.Core.Models;
using NPLogic.Data.Services;

namespace NPLogic.Views
{
    /// <summary>
    /// 상권 분석 지도 창
    /// Phase 7.1: 소상공인 상권 데이터 지도 연동
    /// </summary>
    public partial class CommercialDistrictMapWindow : Window
    {
        private readonly CommercialDistrictService _commercialService;
        private readonly string? _kakaoMapApiKey;
        
        private Property? _property;
        private int _currentRadius = 500; // 기본 반경 500m
        private bool _isMapReady = false;

        public CommercialDistrictMapWindow(IConfiguration? configuration = null)
        {
            InitializeComponent();

            // API 키 로드
            _kakaoMapApiKey = configuration?["KakaoMapApiKey"];
            var commercialApiKey = configuration?["CommercialDistrictApiKey"];
            
            _commercialService = new CommercialDistrictService(commercialApiKey);
        }

        /// <summary>
        /// 물건 정보로 창 열기
        /// </summary>
        public static async Task OpenAsync(Property property, Window? owner = null)
        {
            var config = App.ServiceProvider?.GetService(typeof(IConfiguration)) as IConfiguration;
            var window = new CommercialDistrictMapWindow(config)
            {
                Owner = owner,
                _property = property
            };
            
            window.Show();
            await window.InitializeMapAsync();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_property != null)
            {
                TxtPropertyInfo.Text = $"{_property.PropertyNumber} - {_property.GetShortAddress()}";
            }
            
            await InitializeMapAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // WebView2 리소스 정리
            if (MapWebView.CoreWebView2 != null)
            {
                MapWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }
        }

        /// <summary>
        /// 지도 초기화
        /// </summary>
        private async Task InitializeMapAsync()
        {
            try
            {
                // WebView2 환경 초기화
                var env = await CoreWebView2Environment.CreateAsync();
                await MapWebView.EnsureCoreWebView2Async(env);

                // 메시지 핸들러 등록
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // HTML 파일 로드
                var htmlPath = GetHtmlFilePath();

                if (File.Exists(htmlPath))
                {
                    MapWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);

                    MapWebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                    {
                        if (args.IsSuccess)
                        {
                            // 카카오맵 초기화
                            var apiKey = _kakaoMapApiKey ?? "";
                            var script = $"initializeMap('{EscapeJsString(apiKey)}');";
                            await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                        }
                        else
                        {
                            ShowNoApiKeyOverlay();
                        }
                    };
                }
                else
                {
                    ShowNoApiKeyOverlay();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"지도 초기화 실패: {ex.Message}");
                ShowNoApiKeyOverlay();
            }
        }

        /// <summary>
        /// HTML 파일 경로
        /// </summary>
        private string GetHtmlFilePath()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(basePath, "Assets", "Maps", "commercial_map.html");
        }

        /// <summary>
        /// WebView2 메시지 수신 핸들러
        /// </summary>
        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = JsonSerializer.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (message == null) return;

                var type = message.ContainsKey("type") ? message["type"]?.ToString() : null;

                switch (type)
                {
                    case "mapReady":
                        _isMapReady = true;
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        await LoadPropertyOnMapAsync();
                        break;

                    case "storeClick":
                        // 점포 클릭 시 상세 정보 표시
                        if (message.TryGetValue("storeName", out var storeName))
                        {
                            Debug.WriteLine($"점포 클릭: {storeName}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"메시지 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 물건을 지도에 표시하고 상권 데이터 로드
        /// </summary>
        private async Task LoadPropertyOnMapAsync()
        {
            if (_property == null || !_isMapReady) return;

            var lat = (double)(_property.Latitude ?? 37.5665m);
            var lng = (double)(_property.Longitude ?? 126.9780m);

            // 물건 위치에 마커 표시
            var script = $"setPropertyMarker({lat}, {lng}, '{EscapeJsString(_property.AddressFull ?? "")}', '{EscapeJsString(_property.PropertyNumber ?? "")}');";
            await MapWebView.CoreWebView2.ExecuteScriptAsync(script);

            // 반경 표시
            var radiusScript = $"setRadius({_currentRadius});";
            await MapWebView.CoreWebView2.ExecuteScriptAsync(radiusScript);

            // 상권 데이터 로드
            await LoadCommercialDataAsync(lat, lng);
        }

        /// <summary>
        /// 상권 데이터 로드
        /// </summary>
        private async Task LoadCommercialDataAsync(double lat, double lng)
        {
            TxtNoData.Text = "상권 데이터를 불러오는 중...";

            // 업종별 통계 조회
            var statsResult = await _commercialService.GetIndustryStatsAsync(lat, lng, _currentRadius);

            if (statsResult.Success)
            {
                // 요약 정보 업데이트
                TxtTotalStores.Text = statsResult.TotalStores.ToString("N0");
                TxtIndustryCount.Text = statsResult.Stats.Count.ToString();

                // 업종별 통계 표시
                var maxCount = statsResult.Stats.FirstOrDefault()?.StoreCount ?? 1;
                var displayStats = statsResult.Stats.Take(10).Select(s => new
                {
                    s.IndustryName,
                    s.StoreCount,
                    Percentage = (double)s.StoreCount / maxCount * 100
                }).ToList();

                IndustryStatsList.ItemsSource = displayStats;
                TxtNoData.Visibility = displayStats.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                // 지도에 점포 마커 표시
                await DisplayStoresOnMapAsync(lat, lng);
            }
            else
            {
                TxtNoData.Text = statsResult.ErrorMessage ?? "데이터를 불러올 수 없습니다.";
                TxtNoData.Visibility = Visibility.Visible;
                
                // API 키 안내 표시
                if (statsResult.ErrorMessage?.Contains("API 키") == true)
                {
                    ApiGuidePanel.Visibility = Visibility.Visible;
                }
            }

            // 상권 분석 정보 조회
            var analysisResult = await _commercialService.GetDistrictAnalysisAsync(lat, lng);
            if (analysisResult.Success)
            {
                TxtDistrictName.Text = analysisResult.DistrictName ?? "-";
            }
            else
            {
                TxtDistrictName.Text = "해당 위치 상권 정보 없음";
            }
        }

        /// <summary>
        /// 지도에 점포 마커 표시
        /// </summary>
        private async Task DisplayStoresOnMapAsync(double lat, double lng)
        {
            var result = await _commercialService.GetNearbyDistrictsAsync(lat, lng, _currentRadius);

            if (result.Success && result.Districts.Count > 0)
            {
                // 점포 목록 그리드에 표시
                StoreListGrid.ItemsSource = result.Districts.Take(100);
                TxtStoreListCount.Text = $"(총 {result.TotalCount}개)";

                // 지도에 점포 마커 표시
                var storesJson = JsonSerializer.Serialize(result.Districts.Take(200).Select(d => new
                {
                    name = d.StoreName,
                    industry = d.IndustryName,
                    lat = d.Latitude,
                    lng = d.Longitude
                }));

                var script = $"displayStores({storesJson});";
                await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else
            {
                StoreListGrid.ItemsSource = null;
                TxtStoreListCount.Text = "(0개)";
            }
        }

        /// <summary>
        /// 반경 변경
        /// </summary>
        private async void CmbRadius_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isMapReady || _property == null) return;

            _currentRadius = CmbRadius.SelectedIndex switch
            {
                0 => 200,
                1 => 500,
                2 => 1000,
                3 => 2000,
                _ => 500
            };

            await LoadPropertyOnMapAsync();
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_property == null) return;

            TxtNoData.Text = "새로고침 중...";
            TxtNoData.Visibility = Visibility.Visible;
            
            await LoadPropertyOnMapAsync();
        }

        /// <summary>
        /// Excel 내보내기
        /// </summary>
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stores = StoreListGrid.ItemsSource as IEnumerable<CommercialDistrict>;
                if (stores == null || !stores.Any())
                {
                    MessageBox.Show("내보낼 데이터가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // CSV 파일로 내보내기
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV 파일 (*.csv)|*.csv",
                    FileName = $"상권분석_{_property?.PropertyNumber}_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
                    writer.WriteLine("점포명,업종,세부업종,주소,도로명주소,위도,경도");

                    foreach (var store in stores)
                    {
                        writer.WriteLine($"\"{store.StoreName}\",\"{store.IndustryName}\",\"{store.IndustryDetail}\",\"{store.Address}\",\"{store.RoadAddress}\",{store.Latitude},{store.Longitude}");
                    }

                    MessageBox.Show($"파일이 저장되었습니다.\n{dialog.FileName}", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// API 신청 페이지 열기
        /// </summary>
        private void BtnOpenApiPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.data.go.kr/data/15012005/openapi.do") 
                { 
                    UseShellExecute = true 
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"페이지를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// API 키 없음 오버레이 표시
        /// </summary>
        private void ShowNoApiKeyOverlay()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            NoApiKeyOverlay.Visibility = Visibility.Visible;
            ApiGuidePanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// JS 문자열 이스케이프
        /// </summary>
        private string EscapeJsString(string str)
        {
            return str.Replace("\\", "\\\\")
                      .Replace("'", "\\'")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r");
        }
    }
}






