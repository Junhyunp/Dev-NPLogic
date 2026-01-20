using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 차주별 인터림 요약 아이템
    /// </summary>
    public partial class BorrowerInterimSummary : ObservableObject
    {
        [ObservableProperty]
        private string _borrowerNumber = "";

        [ObservableProperty]
        private string _borrowerName = "";

        [ObservableProperty]
        private decimal _advanceTotal;

        [ObservableProperty]
        private decimal _collectionTotal;

        [ObservableProperty]
        private int _advanceCount;

        [ObservableProperty]
        private int _collectionCount;

        /// <summary>순수익 (회수총액 - 가지급금)</summary>
        public decimal NetAmount => CollectionTotal - AdvanceTotal;

        /// <summary>총계 표시</summary>
        public string TotalDisplay => $"{NetAmount:N0}원";

        /// <summary>가지급금 표시</summary>
        public string AdvanceTotalDisplay => $"{AdvanceTotal:N0}원";

        /// <summary>회수총액 표시</summary>
        public string CollectionTotalDisplay => $"{CollectionTotal:N0}원";
    }

    /// <summary>
    /// 인터림 탭 ViewModel
    /// 차주별 인터림 데이터(가지급금, 회수정보) 관리
    /// </summary>
    public partial class InterimTabViewModel : ObservableObject
    {
        private readonly InterimRepository _interimRepository;
        private readonly PropertyRepository _propertyRepository;
        private readonly ExcelService _excelService;

        private Guid _programId;
        private string? _currentBorrowerNumber;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "";

        // 차주 목록
        [ObservableProperty]
        private ObservableCollection<BorrowerInterimSummary> _borrowerSummaries = new();

        [ObservableProperty]
        private BorrowerInterimSummary? _selectedBorrower;

        // 선택된 차주의 가지급금
        [ObservableProperty]
        private ObservableCollection<InterimAdvance> _advances = new();

        // 선택된 차주의 회수정보
        [ObservableProperty]
        private ObservableCollection<InterimCollection> _collections = new();

        // 통계
        [ObservableProperty]
        private decimal _totalAdvance;

        [ObservableProperty]
        private decimal _totalCollection;

        [ObservableProperty]
        private decimal _totalNet;

        [ObservableProperty]
        private int _totalBorrowerCount;

        public InterimTabViewModel(
            InterimRepository interimRepository,
            PropertyRepository propertyRepository,
            ExcelService excelService)
        {
            _interimRepository = interimRepository ?? throw new ArgumentNullException(nameof(interimRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        }

        /// <summary>
        /// 프로그램 ID 설정 및 데이터 로드
        /// </summary>
        public async Task InitializeAsync(Guid programId, string? borrowerNumber = null)
        {
            _programId = programId;
            _currentBorrowerNumber = borrowerNumber;

            await LoadDataAsync();

            // 특정 차주 선택
            if (!string.IsNullOrEmpty(borrowerNumber))
            {
                var target = BorrowerSummaries.FirstOrDefault(b => b.BorrowerNumber == borrowerNumber);
                if (target != null)
                {
                    SelectedBorrower = target;
                }
            }
        }

        /// <summary>
        /// 전체 인터림 데이터 로드
        /// </summary>
        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (_programId == Guid.Empty) return;

            try
            {
                IsLoading = true;
                StatusMessage = "인터림 데이터 로딩 중...";

                // 가지급금, 회수정보 조회
                var advances = await _interimRepository.GetAdvancesByProgramAsync(_programId);
                var collections = await _interimRepository.GetCollectionsByProgramAsync(_programId);

                // 차주별 그룹핑
                var borrowerNumbers = advances.Select(a => a.BorrowerNumber)
                    .Union(collections.Select(c => c.BorrowerNumber))
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct()
                    .ToList();

                var summaries = new List<BorrowerInterimSummary>();
                foreach (var borrowerNum in borrowerNumbers)
                {
                    var borrowerAdvances = advances.Where(a => a.BorrowerNumber == borrowerNum).ToList();
                    var borrowerCollections = collections.Where(c => c.BorrowerNumber == borrowerNum).ToList();

                    var summary = new BorrowerInterimSummary
                    {
                        BorrowerNumber = borrowerNum!,
                        BorrowerName = borrowerAdvances.FirstOrDefault()?.BorrowerName 
                                       ?? borrowerCollections.FirstOrDefault()?.BorrowerName 
                                       ?? "",
                        AdvanceTotal = borrowerAdvances.Sum(a => a.Amount ?? 0),
                        CollectionTotal = borrowerCollections.Sum(c => c.TotalAmount ?? 0),
                        AdvanceCount = borrowerAdvances.Count,
                        CollectionCount = borrowerCollections.Count
                    };
                    summaries.Add(summary);
                }

                // 차주번호 순으로 정렬
                BorrowerSummaries = new ObservableCollection<BorrowerInterimSummary>(
                    summaries.OrderBy(s => s.BorrowerNumber));

                // 전체 통계
                TotalAdvance = advances.Sum(a => a.Amount ?? 0);
                TotalCollection = collections.Sum(c => c.TotalAmount ?? 0);
                TotalNet = TotalCollection - TotalAdvance;
                TotalBorrowerCount = BorrowerSummaries.Count;

                StatusMessage = $"{TotalBorrowerCount}개 차주의 인터림 데이터 로드 완료";
            }
            catch (Exception ex)
            {
                StatusMessage = $"데이터 로드 실패: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"InterimTabViewModel.LoadDataAsync 오류: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 선택된 차주의 상세 데이터 로드
        /// </summary>
        partial void OnSelectedBorrowerChanged(BorrowerInterimSummary? value)
        {
            if (value == null)
            {
                Advances.Clear();
                Collections.Clear();
                return;
            }

            _ = LoadBorrowerDetailAsync(value.BorrowerNumber);
        }

        private async Task LoadBorrowerDetailAsync(string borrowerNumber)
        {
            if (_programId == Guid.Empty || string.IsNullOrEmpty(borrowerNumber)) return;

            try
            {
                IsLoading = true;

                var advances = await _interimRepository.GetAdvancesByBorrowerAsync(_programId, borrowerNumber);
                var collections = await _interimRepository.GetCollectionsByBorrowerAsync(_programId, borrowerNumber);

                Advances = new ObservableCollection<InterimAdvance>(advances);
                Collections = new ObservableCollection<InterimCollection>(collections);

                StatusMessage = $"{borrowerNumber} 차주: 가지급금 {advances.Count}건, 회수 {collections.Count}건";
            }
            catch (Exception ex)
            {
                StatusMessage = $"상세 데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 인터림 파일 업로드
        /// </summary>
        [RelayCommand]
        private async Task UploadInterimFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls|모든 파일 (*.*)|*.*",
                Title = "인터림 파일 선택"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                IsLoading = true;
                StatusMessage = "인터림 파일 분석 중...";

                var filePath = dialog.FileName;
                var result = await ProcessInterimFileAsync(filePath);

                if (result.Success)
                {
                    StatusMessage = $"업로드 완료: 가지급금 {result.AdvanceCount}건, 회수 {result.CollectionCount}건";
                    MessageBox.Show($"인터림 데이터 업로드 완료\n\n" +
                                    $"• 가지급금: {result.AdvanceCount}건\n" +
                                    $"• 회수정보: {result.CollectionCount}건",
                        "업로드 완료", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 데이터 새로고침
                    await LoadDataAsync();
                }
                else
                {
                    StatusMessage = $"업로드 실패: {result.ErrorMessage}";
                    MessageBox.Show($"인터림 파일 업로드 실패:\n{result.ErrorMessage}",
                        "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"업로드 오류: {ex.Message}";
                MessageBox.Show($"파일 처리 중 오류 발생:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 인터림 파일 처리
        /// </summary>
        private async Task<InterimUploadResult> ProcessInterimFileAsync(string filePath)
        {
            var result = new InterimUploadResult();

            try
            {
                // EPPlus를 사용하여 파일 파싱
                using var package = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(filePath));
                var workbook = package.Workbook;
                if (workbook == null || workbook.Worksheets.Count == 0)
                {
                    result.ErrorMessage = "Excel 파일을 열 수 없습니다.";
                    return result;
                }

                var sheet = workbook.Worksheets.FirstOrDefault();
                if (sheet == null)
                {
                    result.ErrorMessage = "시트를 찾을 수 없습니다.";
                    return result;
                }

                // 헤더 행 분석 (첫 번째 행)
                var headerRow = 1;
                var columnMap = new Dictionary<string, int>();

                var lastCol = sheet.Dimension?.End.Column ?? 20;
                for (int col = 1; col <= lastCol; col++)
                {
                    var headerValue = sheet.Cells[headerRow, col].Text?.Trim().ToLower() ?? "";
                    
                    if (headerValue.Contains("차주") && headerValue.Contains("번호"))
                        columnMap["BorrowerNumber"] = col;
                    else if (headerValue.Contains("차주") && headerValue.Contains("명"))
                        columnMap["BorrowerName"] = col;
                    else if (headerValue.Contains("채권") && headerValue.Contains("번호"))
                        columnMap["AccountNumber"] = col;
                    else if (headerValue.Contains("발생") || headerValue.Contains("가지급"))
                        columnMap["AdvanceAmount"] = col;
                    else if (headerValue.Contains("회수") && headerValue.Contains("원금"))
                        columnMap["PrincipalAmount"] = col;
                    else if (headerValue.Contains("회수") && headerValue.Contains("이자"))
                        columnMap["InterestAmount"] = col;
                    else if (headerValue.Contains("회수") && (headerValue.Contains("총") || headerValue.Contains("합계")))
                        columnMap["TotalAmount"] = col;
                    else if (headerValue.Contains("비고"))
                        columnMap["Notes"] = col;
                    else if (headerValue.Contains("일자") || headerValue.Contains("날짜"))
                        columnMap["Date"] = col;
                }

                if (!columnMap.ContainsKey("BorrowerNumber"))
                {
                    result.ErrorMessage = "차주번호 컬럼을 찾을 수 없습니다.";
                    return result;
                }

                // 기존 데이터 삭제 (재업로드)
                await _interimRepository.DeleteAdvancesByProgramAsync(_programId);
                await _interimRepository.DeleteCollectionsByProgramAsync(_programId);

                // 데이터 행 처리
                var advances = new List<InterimAdvance>();
                var collections = new List<InterimCollection>();

                var lastRow = sheet.Dimension?.End.Row ?? 1;
                for (int row = headerRow + 1; row <= lastRow; row++)
                {
                    var borrowerNumber = GetCellValue(sheet, row, columnMap, "BorrowerNumber");
                    if (string.IsNullOrEmpty(borrowerNumber)) continue;

                    var borrowerName = GetCellValue(sheet, row, columnMap, "BorrowerName") ?? "";
                    var accountNumber = GetCellValue(sheet, row, columnMap, "AccountNumber") ?? "";
                    var notes = GetCellValue(sheet, row, columnMap, "Notes") ?? "";
                    var dateStr = GetCellValue(sheet, row, columnMap, "Date");
                    DateTime? date = null;
                    if (DateTime.TryParse(dateStr, out var parsedDate))
                        date = parsedDate;

                    // 가지급금 (발생액)
                    var advanceAmountStr = GetCellValue(sheet, row, columnMap, "AdvanceAmount");
                    if (!string.IsNullOrEmpty(advanceAmountStr) && decimal.TryParse(advanceAmountStr.Replace(",", ""), out var advanceAmount) && advanceAmount != 0)
                    {
                        advances.Add(new InterimAdvance
                        {
                            ProgramId = _programId,
                            BorrowerNumber = borrowerNumber,
                            BorrowerName = borrowerName,
                            AccountNumber = accountNumber,
                            Amount = Math.Abs(advanceAmount),
                            TransactionDate = date,
                            Notes = notes
                        });
                    }

                    // 회수정보
                    var principalStr = GetCellValue(sheet, row, columnMap, "PrincipalAmount");
                    var interestStr = GetCellValue(sheet, row, columnMap, "InterestAmount");
                    var totalStr = GetCellValue(sheet, row, columnMap, "TotalAmount");

                    decimal? principal = null, interest = null, total = null;
                    if (decimal.TryParse(principalStr?.Replace(",", ""), out var p)) principal = p;
                    if (decimal.TryParse(interestStr?.Replace(",", ""), out var i)) interest = i;
                    if (decimal.TryParse(totalStr?.Replace(",", ""), out var t)) total = t;

                    if (principal.HasValue || interest.HasValue || total.HasValue)
                    {
                        // 총액이 없으면 원금+이자로 계산
                        if (!total.HasValue && (principal.HasValue || interest.HasValue))
                            total = (principal ?? 0) + (interest ?? 0);

                        if (total > 0)
                        {
                            collections.Add(new InterimCollection
                            {
                                ProgramId = _programId,
                                BorrowerNumber = borrowerNumber,
                                BorrowerName = borrowerName,
                                AccountNumber = accountNumber,
                                PrincipalAmount = principal,
                                InterestAmount = interest,
                                TotalAmount = total,
                                CollectionDate = date,
                                Notes = notes
                            });
                        }
                    }
                }

                // 배치 저장
                if (advances.Count > 0)
                {
                    var (created, failed) = await _interimRepository.CreateAdvancesBatchAsync(advances);
                    result.AdvanceCount = created;
                }

                if (collections.Count > 0)
                {
                    var (created, failed) = await _interimRepository.CreateCollectionsBatchAsync(collections);
                    result.CollectionCount = created;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string? GetCellValue(OfficeOpenXml.ExcelWorksheet sheet, int row, Dictionary<string, int> columnMap, string key)
        {
            if (!columnMap.TryGetValue(key, out var col)) return null;
            return sheet.Cells[row, col].Text?.Trim();
        }

        /// <summary>
        /// 엑셀 다운로드
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel 파일 (*.xlsx)|*.xlsx",
                FileName = $"인터림_데이터_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "인터림 데이터 저장"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Excel 파일 생성 중...";

                var advances = await _interimRepository.GetAdvancesByProgramAsync(_programId);
                var collections = await _interimRepository.GetCollectionsByProgramAsync(_programId);

                using var package = new OfficeOpenXml.ExcelPackage();

                // 가지급금 시트
                var advanceSheet = package.Workbook.Worksheets.Add("가지급금");
                advanceSheet.Cells[1, 1].Value = "차주번호";
                advanceSheet.Cells[1, 2].Value = "차주명";
                advanceSheet.Cells[1, 3].Value = "계좌번호";
                advanceSheet.Cells[1, 4].Value = "비용종류";
                advanceSheet.Cells[1, 5].Value = "거래일자";
                advanceSheet.Cells[1, 6].Value = "금액";
                advanceSheet.Cells[1, 7].Value = "비고";

                for (int i = 0; i < advances.Count; i++)
                {
                    var row = i + 2;
                    var item = advances[i];
                    advanceSheet.Cells[row, 1].Value = item.BorrowerNumber;
                    advanceSheet.Cells[row, 2].Value = item.BorrowerName;
                    advanceSheet.Cells[row, 3].Value = item.AccountNumber;
                    advanceSheet.Cells[row, 4].Value = item.ExpenseType;
                    advanceSheet.Cells[row, 5].Value = item.TransactionDate;
                    advanceSheet.Cells[row, 6].Value = item.Amount;
                    advanceSheet.Cells[row, 7].Value = item.Notes;
                }

                // 회수정보 시트
                var collectionSheet = package.Workbook.Worksheets.Add("회수정보");
                collectionSheet.Cells[1, 1].Value = "차주번호";
                collectionSheet.Cells[1, 2].Value = "차주명";
                collectionSheet.Cells[1, 3].Value = "계좌번호";
                collectionSheet.Cells[1, 4].Value = "회수일자";
                collectionSheet.Cells[1, 5].Value = "회수원금";
                collectionSheet.Cells[1, 6].Value = "회수이자";
                collectionSheet.Cells[1, 7].Value = "총회수액";
                collectionSheet.Cells[1, 8].Value = "비고";

                for (int i = 0; i < collections.Count; i++)
                {
                    var row = i + 2;
                    var item = collections[i];
                    collectionSheet.Cells[row, 1].Value = item.BorrowerNumber;
                    collectionSheet.Cells[row, 2].Value = item.BorrowerName;
                    collectionSheet.Cells[row, 3].Value = item.AccountNumber;
                    collectionSheet.Cells[row, 4].Value = item.CollectionDate;
                    collectionSheet.Cells[row, 5].Value = item.PrincipalAmount;
                    collectionSheet.Cells[row, 6].Value = item.InterestAmount;
                    collectionSheet.Cells[row, 7].Value = item.TotalAmount;
                    collectionSheet.Cells[row, 8].Value = item.Notes;
                }

                // 요약 시트
                var summarySheet = package.Workbook.Worksheets.Add("요약");
                summarySheet.Cells[1, 1].Value = "차주번호";
                summarySheet.Cells[1, 2].Value = "차주명";
                summarySheet.Cells[1, 3].Value = "가지급금 합계";
                summarySheet.Cells[1, 4].Value = "회수 합계";
                summarySheet.Cells[1, 5].Value = "순수익";

                for (int i = 0; i < BorrowerSummaries.Count; i++)
                {
                    var row = i + 2;
                    var item = BorrowerSummaries[i];
                    summarySheet.Cells[row, 1].Value = item.BorrowerNumber;
                    summarySheet.Cells[row, 2].Value = item.BorrowerName;
                    summarySheet.Cells[row, 3].Value = item.AdvanceTotal;
                    summarySheet.Cells[row, 4].Value = item.CollectionTotal;
                    summarySheet.Cells[row, 5].Value = item.NetAmount;
                }

                await package.SaveAsAsync(new System.IO.FileInfo(dialog.FileName));

                StatusMessage = "Excel 파일 저장 완료";
                MessageBox.Show("Excel 파일이 저장되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Excel 저장 실패: {ex.Message}";
                MessageBox.Show($"Excel 파일 저장 실패:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 차주별 인터림 데이터 조회 (외부 호출용)
        /// </summary>
        public async Task<InterimRecoveryData> GetBorrowerInterimDataAsync(string borrowerNumber)
        {
            if (_programId == Guid.Empty || string.IsNullOrEmpty(borrowerNumber))
                return new InterimRecoveryData();

            try
            {
                var advances = await _interimRepository.GetAdvancesByBorrowerAsync(_programId, borrowerNumber);
                var collections = await _interimRepository.GetCollectionsByBorrowerAsync(_programId, borrowerNumber);

                return new InterimRecoveryData
                {
                    PrincipalRecovery = collections.Sum(c => c.PrincipalAmount ?? 0),
                    InterestRecovery = collections.Sum(c => c.InterestAmount ?? 0),
                    OtherRecovery = 0,
                    AuctionCost = advances.Where(a => a.ExpenseType?.Contains("경매") == true).Sum(a => a.Amount ?? 0),
                    PrincipalOffset = 0,
                    InterestOffset = 0,
                    Subrogation = advances.Where(a => a.ExpenseType?.Contains("대위") == true).Sum(a => a.Amount ?? 0)
                };
            }
            catch
            {
                return new InterimRecoveryData();
            }
        }
    }

    /// <summary>
    /// 인터림 파일 업로드 결과
    /// </summary>
    public class InterimUploadResult
    {
        public bool Success { get; set; }
        public int AdvanceCount { get; set; }
        public int CollectionCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
