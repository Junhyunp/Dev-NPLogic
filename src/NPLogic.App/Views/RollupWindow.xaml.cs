using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.Views
{
    /// <summary>
    /// RollupWindow.xaml에 대한 상호 작용 논리
    /// 비핵심 전체 현황을 보여주는 롤업 창
    /// Phase 4.5: 롤업 기능 구현
    /// - 전체 화면으로 비핵심 데이터 표시
    /// - 수정 시 실시간 반영
    /// - 창 항상 열림 유지 가능
    /// </summary>
    public partial class RollupWindow : Window
    {
        private readonly PropertyRepository? _propertyRepository;
        private readonly DispatcherTimer _refreshTimer;
        private string? _currentProjectId;
        private ObservableCollection<Property> _properties = new();
        private bool _isClosingAllowed = false;
        
        // 싱글톤 인스턴스 (하나의 롤업 창만 유지)
        public static RollupWindow? Instance { get; private set; }

        public RollupWindow()
        {
            InitializeComponent();
            
            // Repository 주입
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                _propertyRepository = serviceProvider.GetService<PropertyRepository>();
            }
            
            // 자동 새로고침 타이머 설정 (5초)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshDataAsync();
            _refreshTimer.Start();
            
            Instance = this;
        }

        /// <summary>
        /// 프로젝트 ID 설정 및 데이터 로드
        /// </summary>
        public async Task SetProjectAsync(string projectId, string projectName)
        {
            _currentProjectId = projectId;
            ProgramNameText.Text = $"- {projectName}";
            await RefreshDataAsync();
        }

        /// <summary>
        /// 데이터 새로고침
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (_propertyRepository == null || string.IsNullOrEmpty(_currentProjectId))
                return;

            try
            {
                var (properties, _) = await _propertyRepository.GetPagedAsync(
                    page: 1,
                    pageSize: 500,  // 롤업에서는 더 많은 데이터 표시
                    projectId: _currentProjectId
                );

                _properties = new ObservableCollection<Property>(properties);
                RollupDataGrid.ItemsSource = _properties;

                // 통계 업데이트
                UpdateStatistics();
                
                // 마지막 업데이트 시간 표시
                LastUpdateText.Text = $"마지막 업데이트: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 통계 업데이트
        /// </summary>
        private void UpdateStatistics()
        {
            TotalCountText.Text = _properties.Count.ToString();
            CompletedCountText.Text = _properties.Count(p => p.Status == "completed").ToString();
            ProcessingCountText.Text = _properties.Count(p => p.Status == "processing").ToString();
        }

        /// <summary>
        /// 새로고침 버튼 클릭
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync();
        }

        /// <summary>
        /// Excel 내보내기 버튼 클릭
        /// </summary>
        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"롤업_{_currentProjectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("롤업");

                    // 헤더 (순서 변경: 차주번호, 물건번호 앞으로)
                    var headers = new[] { "차주번호", "물건번호", "차주명", "물건종류", "약정서", "보증서",
                                         "경매개시", "경매열람", "전입열람", "선순위", "평가확정",
                                         "경매일정", "QA", "권리분석", "경매사건번호", "상태" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1E3A5F");
                        worksheet.Cell(1, i + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                    }

                    // 데이터 (순서 변경: 차주번호, 물건번호 앞으로)
                    int row = 2;
                    foreach (var p in _properties)
                    {
                        worksheet.Cell(row, 1).Value = p.BorrowerNumber;
                        worksheet.Cell(row, 2).Value = p.CollateralNumber;
                        worksheet.Cell(row, 3).Value = p.DebtorName;
                        worksheet.Cell(row, 4).Value = p.PropertyType;
                        worksheet.Cell(row, 5).Value = p.AgreementDoc ? "O" : "";
                        worksheet.Cell(row, 6).Value = p.GuaranteeDoc ? "O" : "";
                        worksheet.Cell(row, 7).Value = p.AuctionStart1;
                        worksheet.Cell(row, 8).Value = p.AuctionStart2;
                        worksheet.Cell(row, 9).Value = p.AuctionDocs ? "O" : "";
                        worksheet.Cell(row, 10).Value = p.TenantDocs ? "O" : "";
                        worksheet.Cell(row, 11).Value = p.SeniorRightsReview ? "O" : "";
                        worksheet.Cell(row, 12).Value = p.AppraisalConfirmed ? "O" : "";
                        worksheet.Cell(row, 13).Value = p.AuctionScheduleDate?.ToString("yyyy-MM-dd");
                        worksheet.Cell(row, 14).Value = p.QaUnansweredCount;
                        worksheet.Cell(row, 15).Value = p.RightsAnalysisStatus;
                        worksheet.Cell(row, 16).Value = p.PropertyNumber;
                        worksheet.Cell(row, 17).Value = p.Status;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveFileDialog.FileName);

                    MessageBox.Show($"파일이 저장되었습니다.\n{saveFileDialog.FileName}",
                        "Excel 내보내기", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 자동 새로고침 토글
        /// </summary>
        private void AutoRefreshToggle_Click(object sender, RoutedEventArgs e)
        {
            if (AutoRefreshToggle.IsChecked == true)
            {
                _refreshTimer.Start();
                AutoRefreshIntervalText.Text = "5초";
            }
            else
            {
                _refreshTimer.Stop();
                AutoRefreshIntervalText.Text = "꺼짐";
            }
        }

        /// <summary>
        /// 창 닫기 처리 (확인 메시지)
        /// </summary>
        private void RollupWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosingAllowed)
            {
                var result = MessageBox.Show(
                    "롤업 창을 닫으시겠습니까?\n작업 중 실시간 반영이 중단됩니다.",
                    "롤업 창 닫기",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _refreshTimer.Stop();
            Instance = null;
        }

        /// <summary>
        /// 강제 닫기 (프로그램 종료 시)
        /// </summary>
        public void ForceClose()
        {
            _isClosingAllowed = true;
            Close();
        }

        /// <summary>
        /// 롤업 창 열기 (정적 메서드)
        /// </summary>
        public static async Task OpenRollupAsync(string projectId, string projectName)
        {
            if (Instance != null)
            {
                // 이미 열려있으면 포커스
                Instance.Activate();
                await Instance.SetProjectAsync(projectId, projectName);
            }
            else
            {
                var rollupWindow = new RollupWindow();
                rollupWindow.Show();
                await rollupWindow.SetProjectAsync(projectId, projectName);
            }
        }
    }
}

