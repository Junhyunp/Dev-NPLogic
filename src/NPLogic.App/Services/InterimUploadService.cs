using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using OfficeOpenXml;

namespace NPLogic.Services
{
    /// <summary>
    /// 인터림 파일 업로드 서비스
    /// 엑셀 파일에서 가지급금/회수정보를 파싱하여 DB에 저장
    /// </summary>
    public class InterimUploadService
    {
        private readonly InterimRepository _interimRepository;
        private readonly ExcelService _excelService;

        public InterimUploadService(
            InterimRepository interimRepository,
            ExcelService excelService)
        {
            _interimRepository = interimRepository ?? throw new ArgumentNullException(nameof(interimRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        }

        /// <summary>
        /// 인터림 파일 업로드 및 처리
        /// </summary>
        /// <param name="filePath">엑셀 파일 경로</param>
        /// <param name="programId">프로그램 ID</param>
        /// <param name="deleteExisting">기존 데이터 삭제 여부</param>
        /// <returns>업로드 결과</returns>
        public async Task<InterimUploadResult> UploadAsync(string filePath, Guid programId, bool deleteExisting = true)
        {
            var result = new InterimUploadResult();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                result.ErrorMessage = "파일을 찾을 수 없습니다.";
                return result;
            }

            if (programId == Guid.Empty)
            {
                result.ErrorMessage = "프로그램 ID가 유효하지 않습니다.";
                return result;
            }

            try
            {
                // 엑셀 파일 열기
                using var package = new ExcelPackage(new FileInfo(filePath));
                var workbook = package.Workbook;
                if (workbook == null || workbook.Worksheets.Count == 0)
                {
                    result.ErrorMessage = "Excel 파일을 열 수 없습니다.";
                    return result;
                }

                // 기존 데이터 삭제
                if (deleteExisting)
                {
                    await _interimRepository.DeleteAdvancesByProgramAsync(programId);
                    await _interimRepository.DeleteCollectionsByProgramAsync(programId);
                }

                // 시트별 처리
                foreach (var sheet in workbook.Worksheets)
                {
                    var sheetName = sheet.Name?.ToLower() ?? "";
                    
                    // 시트명으로 타입 구분
                    if (sheetName.Contains("가지급") || sheetName.Contains("발생") || sheetName.Contains("advance"))
                    {
                        var advances = ParseAdvanceSheet(sheet, programId);
                        if (advances.Count > 0)
                        {
                            var (created, failed) = await _interimRepository.CreateAdvancesBatchAsync(advances);
                            result.AdvanceCount += created;
                        }
                    }
                    else if (sheetName.Contains("회수") || sheetName.Contains("collection"))
                    {
                        var collections = ParseCollectionSheet(sheet, programId);
                        if (collections.Count > 0)
                        {
                            var (created, failed) = await _interimRepository.CreateCollectionsBatchAsync(collections);
                            result.CollectionCount += created;
                        }
                    }
                    else
                    {
                        // 시트명으로 구분 안되면 컬럼으로 자동 감지
                        var (advances, collections) = ParseMixedSheet(sheet, programId);
                        
                        if (advances.Count > 0)
                        {
                            var (created, failed) = await _interimRepository.CreateAdvancesBatchAsync(advances);
                            result.AdvanceCount += created;
                        }
                        
                        if (collections.Count > 0)
                        {
                            var (created, failed) = await _interimRepository.CreateCollectionsBatchAsync(collections);
                            result.CollectionCount += created;
                        }
                    }
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"InterimUploadService.UploadAsync 오류: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 가지급금 시트 파싱
        /// </summary>
        private List<InterimAdvance> ParseAdvanceSheet(ExcelWorksheet sheet, Guid programId)
        {
            var advances = new List<InterimAdvance>();
            var columnMap = DetectColumns(sheet);

            if (!columnMap.ContainsKey("BorrowerNumber"))
                return advances;

            var lastRow = sheet.Dimension?.End.Row ?? 1;
            var headerRow = FindHeaderRow(sheet);

            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                var borrowerNumber = GetCellValue(sheet, row, columnMap, "BorrowerNumber");
                if (string.IsNullOrEmpty(borrowerNumber)) continue;

                var amountStr = GetCellValue(sheet, row, columnMap, "AdvanceAmount") 
                                ?? GetCellValue(sheet, row, columnMap, "Amount");
                
                if (string.IsNullOrEmpty(amountStr)) continue;
                if (!decimal.TryParse(amountStr.Replace(",", ""), out var amount) || amount == 0) continue;

                var advance = new InterimAdvance
                {
                    ProgramId = programId,
                    BorrowerNumber = borrowerNumber,
                    BorrowerName = GetCellValue(sheet, row, columnMap, "BorrowerName") ?? "",
                    Pool = GetCellValue(sheet, row, columnMap, "Pool"),
                    LoanType = GetCellValue(sheet, row, columnMap, "LoanType"),
                    AccountSerial = GetCellValue(sheet, row, columnMap, "AccountSerial"),
                    AccountNumber = GetCellValue(sheet, row, columnMap, "AccountNumber"),
                    ExpenseType = GetCellValue(sheet, row, columnMap, "ExpenseType"),
                    Amount = Math.Abs(amount),
                    Description = GetCellValue(sheet, row, columnMap, "Description"),
                    Notes = GetCellValue(sheet, row, columnMap, "Notes")
                };

                // 거래일자 파싱
                var dateStr = GetCellValue(sheet, row, columnMap, "TransactionDate") 
                              ?? GetCellValue(sheet, row, columnMap, "Date");
                if (DateTime.TryParse(dateStr, out var date))
                    advance.TransactionDate = date;

                advances.Add(advance);
            }

            return advances;
        }

        /// <summary>
        /// 회수정보 시트 파싱
        /// </summary>
        private List<InterimCollection> ParseCollectionSheet(ExcelWorksheet sheet, Guid programId)
        {
            var collections = new List<InterimCollection>();
            var columnMap = DetectColumns(sheet);

            if (!columnMap.ContainsKey("BorrowerNumber"))
                return collections;

            var lastRow = sheet.Dimension?.End.Row ?? 1;
            var headerRow = FindHeaderRow(sheet);

            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                var borrowerNumber = GetCellValue(sheet, row, columnMap, "BorrowerNumber");
                if (string.IsNullOrEmpty(borrowerNumber)) continue;

                decimal? principal = null, interest = null, total = null;

                var principalStr = GetCellValue(sheet, row, columnMap, "PrincipalAmount");
                var interestStr = GetCellValue(sheet, row, columnMap, "InterestAmount");
                var totalStr = GetCellValue(sheet, row, columnMap, "TotalAmount") 
                               ?? GetCellValue(sheet, row, columnMap, "Amount");

                if (decimal.TryParse(principalStr?.Replace(",", ""), out var p)) principal = p;
                if (decimal.TryParse(interestStr?.Replace(",", ""), out var i)) interest = i;
                if (decimal.TryParse(totalStr?.Replace(",", ""), out var t)) total = t;

                // 총액이 없으면 원금+이자로 계산
                if (!total.HasValue && (principal.HasValue || interest.HasValue))
                    total = (principal ?? 0) + (interest ?? 0);

                if (!total.HasValue || total <= 0) continue;

                var collection = new InterimCollection
                {
                    ProgramId = programId,
                    BorrowerNumber = borrowerNumber,
                    BorrowerName = GetCellValue(sheet, row, columnMap, "BorrowerName") ?? "",
                    Pool = GetCellValue(sheet, row, columnMap, "Pool"),
                    LoanType = GetCellValue(sheet, row, columnMap, "LoanType"),
                    AccountSerial = GetCellValue(sheet, row, columnMap, "AccountSerial"),
                    AccountNumber = GetCellValue(sheet, row, columnMap, "AccountNumber"),
                    PrincipalAmount = principal,
                    InterestAmount = interest,
                    TotalAmount = total,
                    Notes = GetCellValue(sheet, row, columnMap, "Notes")
                };

                // 회수일자 파싱
                var dateStr = GetCellValue(sheet, row, columnMap, "CollectionDate") 
                              ?? GetCellValue(sheet, row, columnMap, "Date");
                if (DateTime.TryParse(dateStr, out var date))
                    collection.CollectionDate = date;

                collections.Add(collection);
            }

            return collections;
        }

        /// <summary>
        /// 혼합 시트 파싱 (가지급금/회수정보 자동 구분)
        /// </summary>
        private (List<InterimAdvance> advances, List<InterimCollection> collections) ParseMixedSheet(
            ExcelWorksheet sheet, Guid programId)
        {
            var advances = new List<InterimAdvance>();
            var collections = new List<InterimCollection>();
            var columnMap = DetectColumns(sheet);

            if (!columnMap.ContainsKey("BorrowerNumber"))
                return (advances, collections);

            var lastRow = sheet.Dimension?.End.Row ?? 1;
            var headerRow = FindHeaderRow(sheet);

            // 컬럼 존재 여부로 타입 결정
            bool hasAdvanceColumns = columnMap.ContainsKey("AdvanceAmount") || columnMap.ContainsKey("ExpenseType");
            bool hasCollectionColumns = columnMap.ContainsKey("PrincipalAmount") || columnMap.ContainsKey("InterestAmount") || columnMap.ContainsKey("TotalAmount");

            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                var borrowerNumber = GetCellValue(sheet, row, columnMap, "BorrowerNumber");
                if (string.IsNullOrEmpty(borrowerNumber)) continue;

                // 가지급금 처리
                if (hasAdvanceColumns)
                {
                    var advanceAmountStr = GetCellValue(sheet, row, columnMap, "AdvanceAmount");
                    if (!string.IsNullOrEmpty(advanceAmountStr) && 
                        decimal.TryParse(advanceAmountStr.Replace(",", ""), out var advanceAmount) && 
                        advanceAmount != 0)
                    {
                        var advance = new InterimAdvance
                        {
                            ProgramId = programId,
                            BorrowerNumber = borrowerNumber,
                            BorrowerName = GetCellValue(sheet, row, columnMap, "BorrowerName") ?? "",
                            Pool = GetCellValue(sheet, row, columnMap, "Pool"),
                            LoanType = GetCellValue(sheet, row, columnMap, "LoanType"),
                            AccountNumber = GetCellValue(sheet, row, columnMap, "AccountNumber"),
                            ExpenseType = GetCellValue(sheet, row, columnMap, "ExpenseType"),
                            Amount = Math.Abs(advanceAmount),
                            Notes = GetCellValue(sheet, row, columnMap, "Notes")
                        };

                        var dateStr = GetCellValue(sheet, row, columnMap, "Date");
                        if (DateTime.TryParse(dateStr, out var date))
                            advance.TransactionDate = date;

                        advances.Add(advance);
                    }
                }

                // 회수정보 처리
                if (hasCollectionColumns)
                {
                    decimal? principal = null, interest = null, total = null;

                    var principalStr = GetCellValue(sheet, row, columnMap, "PrincipalAmount");
                    var interestStr = GetCellValue(sheet, row, columnMap, "InterestAmount");
                    var totalStr = GetCellValue(sheet, row, columnMap, "TotalAmount");

                    if (decimal.TryParse(principalStr?.Replace(",", ""), out var p)) principal = p;
                    if (decimal.TryParse(interestStr?.Replace(",", ""), out var i)) interest = i;
                    if (decimal.TryParse(totalStr?.Replace(",", ""), out var t)) total = t;

                    if (!total.HasValue && (principal.HasValue || interest.HasValue))
                        total = (principal ?? 0) + (interest ?? 0);

                    if (total.HasValue && total > 0)
                    {
                        var collection = new InterimCollection
                        {
                            ProgramId = programId,
                            BorrowerNumber = borrowerNumber,
                            BorrowerName = GetCellValue(sheet, row, columnMap, "BorrowerName") ?? "",
                            Pool = GetCellValue(sheet, row, columnMap, "Pool"),
                            LoanType = GetCellValue(sheet, row, columnMap, "LoanType"),
                            AccountNumber = GetCellValue(sheet, row, columnMap, "AccountNumber"),
                            PrincipalAmount = principal,
                            InterestAmount = interest,
                            TotalAmount = total,
                            Notes = GetCellValue(sheet, row, columnMap, "Notes")
                        };

                        var dateStr = GetCellValue(sheet, row, columnMap, "Date");
                        if (DateTime.TryParse(dateStr, out var date))
                            collection.CollectionDate = date;

                        collections.Add(collection);
                    }
                }
            }

            return (advances, collections);
        }

        /// <summary>
        /// 헤더 행 찾기
        /// </summary>
        private int FindHeaderRow(ExcelWorksheet sheet)
        {
            var maxRow = Math.Min(10, sheet.Dimension?.End.Row ?? 1);
            var maxCol = Math.Min(20, sheet.Dimension?.End.Column ?? 1);

            for (int row = 1; row <= maxRow; row++)
            {
                for (int col = 1; col <= maxCol; col++)
                {
                    var value = sheet.Cells[row, col].Text?.ToLower() ?? "";
                    if (value.Contains("차주") && (value.Contains("번호") || value.Contains("일련")))
                        return row;
                }
            }
            return 1;
        }

        /// <summary>
        /// 컬럼 매핑 자동 감지
        /// </summary>
        private Dictionary<string, int> DetectColumns(ExcelWorksheet sheet)
        {
            var columnMap = new Dictionary<string, int>();
            var headerRow = FindHeaderRow(sheet);
            var lastCol = Math.Min(50, sheet.Dimension?.End.Column ?? 20);

            for (int col = 1; col <= lastCol; col++)
            {
                var headerValue = sheet.Cells[headerRow, col].Text?.Trim().ToLower() ?? "";
                if (string.IsNullOrEmpty(headerValue)) continue;

                // 차주 정보
                if ((headerValue.Contains("차주") && (headerValue.Contains("번호") || headerValue.Contains("일련"))) 
                    || headerValue == "borrower_number")
                    columnMap["BorrowerNumber"] = col;
                else if ((headerValue.Contains("차주") && headerValue.Contains("명")) 
                         || headerValue == "borrower_name")
                    columnMap["BorrowerName"] = col;

                // Pool / 채권구분
                else if (headerValue == "pool" || headerValue.Contains("풀"))
                    columnMap["Pool"] = col;
                else if (headerValue.Contains("채권") && headerValue.Contains("구분"))
                    columnMap["LoanType"] = col;

                // 계좌 정보
                else if (headerValue.Contains("계좌") && headerValue.Contains("일련"))
                    columnMap["AccountSerial"] = col;
                else if ((headerValue.Contains("계좌") && headerValue.Contains("번호")) 
                         || (headerValue.Contains("채권") && headerValue.Contains("번호")))
                    columnMap["AccountNumber"] = col;

                // 가지급금 관련
                else if (headerValue.Contains("발생") || (headerValue.Contains("가지급") && !headerValue.Contains("회수")))
                    columnMap["AdvanceAmount"] = col;
                else if (headerValue.Contains("비용") && headerValue.Contains("종류"))
                    columnMap["ExpenseType"] = col;
                else if (headerValue.Contains("거래") && headerValue.Contains("일"))
                    columnMap["TransactionDate"] = col;

                // 회수정보 관련
                else if (headerValue.Contains("회수") && headerValue.Contains("원금"))
                    columnMap["PrincipalAmount"] = col;
                else if (headerValue.Contains("회수") && headerValue.Contains("이자"))
                    columnMap["InterestAmount"] = col;
                else if ((headerValue.Contains("회수") && (headerValue.Contains("총") || headerValue.Contains("합계")))
                         || headerValue.Contains("총회수"))
                    columnMap["TotalAmount"] = col;
                else if (headerValue.Contains("회수") && headerValue.Contains("일"))
                    columnMap["CollectionDate"] = col;

                // 공통
                else if (headerValue.Contains("일자") || headerValue.Contains("날짜") || headerValue == "date")
                    if (!columnMap.ContainsKey("Date"))
                        columnMap["Date"] = col;
                else if (headerValue.Contains("금액") && !columnMap.ContainsKey("Amount"))
                    columnMap["Amount"] = col;
                else if (headerValue.Contains("비고") || headerValue.Contains("notes"))
                    columnMap["Notes"] = col;
                else if (headerValue.Contains("적요") || headerValue.Contains("설명"))
                    columnMap["Description"] = col;
            }

            return columnMap;
        }

        /// <summary>
        /// 셀 값 가져오기
        /// </summary>
        private string? GetCellValue(ExcelWorksheet sheet, int row, Dictionary<string, int> columnMap, string key)
        {
            if (!columnMap.TryGetValue(key, out var col)) return null;
            return sheet.Cells[row, col].Text?.Trim();
        }

        /// <summary>
        /// 차주별 인터림 통계 조회
        /// </summary>
        public async Task<Dictionary<string, InterimBorrowerSummary>> GetBorrowerSummariesAsync(Guid programId)
        {
            var summaries = new Dictionary<string, InterimBorrowerSummary>();

            try
            {
                var advances = await _interimRepository.GetAdvancesByProgramAsync(programId);
                var collections = await _interimRepository.GetCollectionsByProgramAsync(programId);

                // 차주번호 목록
                var borrowerNumbers = advances.Select(a => a.BorrowerNumber)
                    .Union(collections.Select(c => c.BorrowerNumber))
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct();

                foreach (var borrowerNum in borrowerNumbers)
                {
                    var borrowerAdvances = advances.Where(a => a.BorrowerNumber == borrowerNum).ToList();
                    var borrowerCollections = collections.Where(c => c.BorrowerNumber == borrowerNum).ToList();

                    summaries[borrowerNum!] = new InterimBorrowerSummary
                    {
                        BorrowerNumber = borrowerNum!,
                        BorrowerName = borrowerAdvances.FirstOrDefault()?.BorrowerName 
                                       ?? borrowerCollections.FirstOrDefault()?.BorrowerName 
                                       ?? "",
                        AdvanceTotal = borrowerAdvances.Sum(a => a.Amount ?? 0),
                        CollectionTotal = borrowerCollections.Sum(c => c.TotalAmount ?? 0),
                        PrincipalRecovery = borrowerCollections.Sum(c => c.PrincipalAmount ?? 0),
                        InterestRecovery = borrowerCollections.Sum(c => c.InterestAmount ?? 0),
                        AuctionCost = borrowerAdvances
                            .Where(a => a.ExpenseType?.Contains("경매") == true)
                            .Sum(a => a.Amount ?? 0)
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBorrowerSummariesAsync 오류: {ex}");
            }

            return summaries;
        }
    }

    /// <summary>
    /// 인터림 업로드 결과
    /// </summary>
    public class InterimUploadResult
    {
        public bool Success { get; set; }
        public int AdvanceCount { get; set; }
        public int CollectionCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 차주별 인터림 요약
    /// </summary>
    public class InterimBorrowerSummary
    {
        public string BorrowerNumber { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public decimal AdvanceTotal { get; set; }
        public decimal CollectionTotal { get; set; }
        public decimal PrincipalRecovery { get; set; }
        public decimal InterestRecovery { get; set; }
        public decimal AuctionCost { get; set; }
        
        public decimal NetAmount => CollectionTotal - AdvanceTotal;
    }
}
