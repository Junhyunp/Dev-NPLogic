using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using NPLogic.Data.Repositories;
using SkiaSharp;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 통계 대시보드 ViewModel
    /// </summary>
    public partial class StatisticsViewModel : ObservableObject
    {
        private readonly StatisticsRepository _statisticsRepository;

        #region Observable Properties

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string _selectedProjectId = "전체";

        [ObservableProperty]
        private ObservableCollection<string> _projects = new();

        // 주요 지표
        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _averageAppraisalValue = "0";

        [ObservableProperty]
        private string _averageRecoveryRate = "0%";

        [ObservableProperty]
        private string _completionRate = "0%";

        // 차트 데이터
        [ObservableProperty]
        private ISeries[] _propertyTypeSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] _statusSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _statusXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ISeries[] _monthlyTrendSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _monthlyTrendXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ISeries[] _regionSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _regionXAxes = Array.Empty<Axis>();

        #endregion

        // 색상 팔레트 (디자인 시스템 기반)
        private static readonly SKColor[] ChartColors = new[]
        {
            SKColor.Parse("#1A2332"),  // Deep Navy
            SKColor.Parse("#3B82F6"),  // Info Blue
            SKColor.Parse("#10B981"),  // Success Green
            SKColor.Parse("#F59E0B"),  // Warning Yellow
            SKColor.Parse("#EF4444"),  // Error Red
            SKColor.Parse("#8B5CF6"),  // Purple
            SKColor.Parse("#EC4899"),  // Pink
            SKColor.Parse("#06B6D4"),  // Cyan
        };

        public StatisticsViewModel(StatisticsRepository statisticsRepository)
        {
            _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
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

                await LoadProjectsAsync();
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 프로젝트 목록 로드
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            var projectList = await _statisticsRepository.GetProjectListAsync();
            
            Projects.Clear();
            Projects.Add("전체");
            foreach (var project in projectList)
            {
                Projects.Add(project);
            }
        }

        /// <summary>
        /// 모든 데이터 로드
        /// </summary>
        private async Task LoadAllDataAsync()
        {
            var projectId = SelectedProjectId == "전체" ? null : SelectedProjectId;

            await Task.WhenAll(
                LoadMetricsAsync(projectId),
                LoadPropertyTypeChartAsync(projectId),
                LoadStatusChartAsync(projectId),
                LoadMonthlyTrendChartAsync(projectId),
                LoadRegionChartAsync(projectId)
            );
        }

        /// <summary>
        /// 주요 지표 로드
        /// </summary>
        private async Task LoadMetricsAsync(string? projectId)
        {
            var metrics = await _statisticsRepository.GetAverageMetricsAsync(projectId);

            TotalCount = metrics.TotalCount;
            AverageAppraisalValue = FormatCurrency(metrics.AverageAppraisalValue);
            AverageRecoveryRate = $"{metrics.AverageRecoveryRate:F1}%";
            CompletionRate = $"{metrics.CompletionRate:F1}%";
        }

        /// <summary>
        /// 물건 유형별 파이 차트 로드
        /// </summary>
        private async Task LoadPropertyTypeChartAsync(string? projectId)
        {
            var data = await _statisticsRepository.GetPropertyTypeDistributionAsync(projectId);

            PropertyTypeSeries = data.Select((item, index) => new PieSeries<int>
            {
                Name = item.PropertyType,
                Values = new[] { item.Count },
                Fill = new SolidColorPaint(ChartColors[index % ChartColors.Length]),
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{item.PropertyType}\n{item.Count}건"
            } as ISeries).ToArray();
        }

        /// <summary>
        /// 상태별 막대 차트 로드
        /// </summary>
        private async Task LoadStatusChartAsync(string? projectId)
        {
            var data = await _statisticsRepository.GetStatusDistributionAsync(projectId);

            var statusColors = new SKColor[]
            {
                SKColor.Parse("#F59E0B"),  // 대기 - Warning
                SKColor.Parse("#3B82F6"),  // 진행중 - Info
                SKColor.Parse("#10B981"),  // 완료 - Success
            };

            StatusSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "물건 수",
                    Values = data.Select(x => x.Count).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#1A2332")),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                    MaxBarWidth = 60
                }
            };

            StatusXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = data.Select(x => x.StatusLabel).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1D23")),
                    TextSize = 14
                }
            };
        }

        /// <summary>
        /// 월별 추이 선 차트 로드
        /// </summary>
        private async Task LoadMonthlyTrendChartAsync(string? projectId)
        {
            var data = await _statisticsRepository.GetMonthlyTrendAsync(projectId, 6);

            MonthlyTrendSeries = new ISeries[]
            {
                new LineSeries<int>
                {
                    Name = "등록 물건 수",
                    Values = data.Select(x => x.Count).ToArray(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6"), 3),
                    Fill = new SolidColorPaint(SKColor.Parse("#3B82F6").WithAlpha(50)),
                    GeometrySize = 10,
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#3B82F6"), 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1D23")),
                    DataLabelsSize = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };

            MonthlyTrendXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = data.Select(x => FormatMonth(x.Month)).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1D23")),
                    TextSize = 12
                }
            };
        }

        /// <summary>
        /// 지역별 막대 차트 로드
        /// </summary>
        private async Task LoadRegionChartAsync(string? projectId)
        {
            var data = await _statisticsRepository.GetRegionDistributionAsync(projectId);

            RegionSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "물건 수",
                    Values = data.Select(x => x.Count).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#10B981")),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                    MaxBarWidth = 50
                }
            };

            RegionXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = data.Select(x => x.Region).ToArray(),
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#1A1D23")),
                    TextSize = 12,
                    LabelsRotation = 0
                }
            };
        }

        /// <summary>
        /// 프로젝트 선택 변경 시
        /// </summary>
        partial void OnSelectedProjectIdChanged(string value)
        {
            _ = RefreshDataAsync();
        }

        /// <summary>
        /// 데이터 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"새로고침 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Excel 출력 (추후 구현)
        /// </summary>
        [RelayCommand]
        private void ExportToExcel()
        {
            // TODO: Excel 출력 기능 구현
            System.Windows.MessageBox.Show("Excel 출력 기능은 추후 구현 예정입니다.", "알림", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        #region Helper Methods

        private static string FormatCurrency(decimal value)
        {
            if (value >= 100000000) // 1억 이상
            {
                return $"{value / 100000000:F1}억";
            }
            else if (value >= 10000) // 1만 이상
            {
                return $"{value / 10000:F0}만";
            }
            return $"{value:N0}";
        }

        private static string FormatMonth(int month)
        {
            return $"{month}월";
        }

        #endregion
    }
}

