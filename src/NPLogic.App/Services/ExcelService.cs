using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using NPLogic.Core.Models;

namespace NPLogic.Services
{
    /// <summary>
    /// Excel 파일 처리 서비스
    /// </summary>
    public class ExcelService
    {
        public ExcelService()
        {
            // EPPlus 라이센스 설정 (비상업적 용도)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Excel 파일 읽기
        /// </summary>
        /// <returns>(컬럼 목록, 데이터)</returns>
        public async Task<(List<string> Columns, List<Dictionary<string, object>> Data)> ReadExcelFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var columns = new List<string>();
                var data = new List<Dictionary<string, object>>();

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // 첫 번째 시트
                    
                    if (worksheet.Dimension == null)
                    {
                        throw new Exception("Excel 파일이 비어있습니다.");
                    }

                    int colCount = worksheet.Dimension.End.Column;
                    int rowCount = worksheet.Dimension.End.Row;

                    // 첫 번째 행을 컬럼명으로 사용
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[1, col].Value;
                        var columnName = cellValue?.ToString() ?? $"Column{col}";
                        columns.Add(columnName);
                    }

                    // 데이터 읽기 (2행부터)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var rowData = new Dictionary<string, object>();
                        bool hasData = false;

                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            if (cellValue != null)
                            {
                                rowData[columns[col - 1]] = cellValue;
                                hasData = true;
                            }
                            else
                            {
                                rowData[columns[col - 1]] = "";
                            }
                        }

                        // 빈 행은 제외
                        if (hasData)
                        {
                            data.Add(rowData);
                        }
                    }
                }

                return (columns, data);
            });
        }

        /// <summary>
        /// Excel 파일 생성 (물건 목록 출력)
        /// </summary>
        public async Task<string> ExportPropertiesToExcelAsync(
            IEnumerable<Core.Models.Property> properties,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("물건 목록");

                    // 헤더
                    var headers = new[]
                    {
                        "프로젝트 ID", "물건번호", "물건유형", "전체주소", "도로명주소", "지번주소",
                        "토지면적", "건물면적", "층수", "감정가", "최저입찰가", "매각가", "상태"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    }

                    // 데이터
                    int row = 2;
                    foreach (var property in properties)
                    {
                        worksheet.Cells[row, 1].Value = property.ProjectId;
                        worksheet.Cells[row, 2].Value = property.PropertyNumber;
                        worksheet.Cells[row, 3].Value = property.PropertyType;
                        worksheet.Cells[row, 4].Value = property.AddressFull;
                        worksheet.Cells[row, 5].Value = property.AddressRoad;
                        worksheet.Cells[row, 6].Value = property.AddressJibun;
                        worksheet.Cells[row, 7].Value = property.LandArea;
                        worksheet.Cells[row, 8].Value = property.BuildingArea;
                        worksheet.Cells[row, 9].Value = property.Floors;
                        worksheet.Cells[row, 10].Value = property.AppraisalValue;
                        worksheet.Cells[row, 11].Value = property.MinimumBid;
                        worksheet.Cells[row, 12].Value = property.SalePrice;
                        worksheet.Cells[row, 13].Value = property.Status;
                        row++;
                    }

                    // 데이터 너비 자동 조절
                    worksheet.Cells.AutoFitColumns();

                    // 파일 저장
                    var file = new FileInfo(outputPath);
                    package.SaveAs(file);
                    
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 생성 (Loan 상세 출력)
        /// </summary>
        public async Task<string> ExportLoansToExcelAsync(
            IEnumerable<Core.Models.Loan> loans,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Loan 상세");

                    // 헤더
                    var headers = new[]
                    {
                        "계좌일련번호(A)", "대출과목(B)", "대출종류(C)", "계좌번호(D)", "최초대출원금(F)",
                        "대출원금잔액(G)", "가지급금(H)", "미수이자(I)", "채권액 합계(J)",
                        "최초대출일(E)", "최종이수일(K)", "정상이자율(L)", "연체이자율(M)",
                        "Loan Cap(1안)", "예상배당일(1안)", "연체이자(1안)",
                        "Loan Cap(2안)", "예상배당일(2안)", "연체이자(2안)"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cells[1, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 데이터
                    int row = 2;
                    foreach (var loan in loans)
                    {
                        worksheet.Cells[row, 1].Value = loan.AccountSerial;
                        worksheet.Cells[row, 2].Value = loan.LoanType;
                        worksheet.Cells[row, 3].Value = loan.LoanCategory;
                        worksheet.Cells[row, 4].Value = loan.AccountNumber;
                        worksheet.Cells[row, 5].Value = loan.InitialLoanAmount;
                        worksheet.Cells[row, 6].Value = loan.LoanPrincipalBalance;
                        worksheet.Cells[row, 7].Value = loan.AdvancePayment;
                        worksheet.Cells[row, 8].Value = loan.AccruedInterest;
                        worksheet.Cells[row, 9].Value = loan.TotalClaimAmount ?? (loan.LoanPrincipalBalance + loan.AdvancePayment + loan.AccruedInterest);
                        worksheet.Cells[row, 10].Value = loan.InitialLoanDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 11].Value = loan.LastInterestDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 12].Value = loan.NormalInterestRate;
                        worksheet.Cells[row, 13].Value = loan.OverdueInterestRate;
                        worksheet.Cells[row, 14].Value = loan.LoanCap1;
                        worksheet.Cells[row, 15].Value = loan.ExpectedDividendDate1?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 16].Value = loan.OverdueInterest1;
                        worksheet.Cells[row, 17].Value = loan.LoanCap2;
                        worksheet.Cells[row, 18].Value = loan.ExpectedDividendDate2?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 19].Value = loan.OverdueInterest2;

                        // 숫자 포맷
                        worksheet.Cells[row, 5, row, 9].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 12, row, 13].Style.Numberformat.Format = "0.00%";
                        worksheet.Cells[row, 14].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 16].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 17].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 19].Style.Numberformat.Format = "#,##0";
                        
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 생성 (담보총괄 출력)
        /// </summary>
        public async Task<string> ExportCollateralSummaryToExcelAsync(
            IEnumerable<ViewModels.CollateralItem> items,
            string borrowerName,
            decimal loanCap,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("담보총괄");

                    // 정보 영역
                    worksheet.Cells["A1"].Value = "차주명:";
                    worksheet.Cells["B1"].Value = borrowerName;
                    worksheet.Cells["A2"].Value = "Loan Cap (OPB):";
                    worksheet.Cells["B2"].Value = loanCap;
                    worksheet.Cells["B2"].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells["A1:A2"].Style.Font.Bold = true;

                    // 헤더 (4행부터)
                    var headers = new[]
                    {
                        "물건번호", "주소", "물건유형", "토지면적", "건물면적", "합계면적",
                        "감정가", "추정매각가", "선순위권리", "배당가능재원", "진행상태", "진행률"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cells[4, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // 데이터
                    int row = 5;
                    foreach (var item in items)
                    {
                        worksheet.Cells[row, 1].Value = item.PropertyNumber;
                        worksheet.Cells[row, 2].Value = item.Address;
                        worksheet.Cells[row, 3].Value = item.CollateralType;
                        worksheet.Cells[row, 4].Value = item.LandArea;
                        worksheet.Cells[row, 5].Value = item.BuildingArea;
                        worksheet.Cells[row, 6].Value = item.TotalArea;
                        worksheet.Cells[row, 7].Value = item.AppraisalValue;
                        worksheet.Cells[row, 8].Value = item.EstimatedValue;
                        worksheet.Cells[row, 9].Value = item.SeniorRights;
                        worksheet.Cells[row, 10].Value = item.RecoverableAmount;
                        worksheet.Cells[row, 11].Value = item.Status;
                        worksheet.Cells[row, 12].Value = item.ProgressDisplay;

                        // 포맷팅
                        worksheet.Cells[row, 4, row, 6].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, 7, row, 10].Style.Numberformat.Format = "#,##0";
                        row++;
                    }

                    // 합계 행
                    worksheet.Cells[row, 1].Value = "합계";
                    worksheet.Cells[row, 1, row, 3].Merge = true;
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 7].Formula = $"SUM(G5:G{row - 1})";
                    worksheet.Cells[row, 8].Formula = $"SUM(H5:H{row - 1})";
                    worksheet.Cells[row, 9].Formula = $"SUM(I5:I{row - 1})";
                    worksheet.Cells[row, 10].Formula = $"SUM(J5:J{row - 1})";
                    worksheet.Cells[row, 1, row, 12].Style.Font.Bold = true;
                    worksheet.Cells[row, 7, row, 10].Style.Numberformat.Format = "#,##0";

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 생성 (통계 데이터 출력)
        /// </summary>
        public async Task<string> ExportStatisticsToExcelAsync(
            string projectId,
            int totalCount,
            string avgAppraisal,
            string avgRecovery,
            string completionRate,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("통계 대시보드");

                    // 정보 영역
                    worksheet.Cells["A1"].Value = "대상 프로젝트:";
                    worksheet.Cells["B1"].Value = projectId;
                    worksheet.Cells["A3"].Value = "전체 물건 수";
                    worksheet.Cells["B3"].Value = totalCount;
                    worksheet.Cells["A4"].Value = "평균 감정가";
                    worksheet.Cells["B4"].Value = avgAppraisal;
                    worksheet.Cells["A5"].Value = "평균 회수율";
                    worksheet.Cells["B5"].Value = avgRecovery;
                    worksheet.Cells["A6"].Value = "진행 완료율";
                    worksheet.Cells["B6"].Value = completionRate;

                    worksheet.Cells["A1:A6"].Style.Font.Bold = true;
                    
                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 생성 (차주 목록 출력)
        /// </summary>
        public async Task<string> ExportBorrowersToExcelAsync(
            IEnumerable<Core.Models.Borrower> borrowers,
            string title,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(title);

                    // 헤더
                    var headers = new[]
                    {
                        "차주번호", "차주명", "차주유형", "사업자번호", "대표자",
                        "연락처", "이메일", "주소", "OPB", "근저당설정액",
                        "회생여부", "개회여부", "사망여부"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cells[1, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 데이터
                    int row = 2;
                    foreach (var b in borrowers)
                    {
                        worksheet.Cells[row, 1].Value = b.BorrowerNumber;
                        worksheet.Cells[row, 2].Value = b.BorrowerName;
                        worksheet.Cells[row, 3].Value = b.BorrowerType;
                        worksheet.Cells[row, 4].Value = b.BusinessNumber;
                        worksheet.Cells[row, 5].Value = b.Representative;
                        worksheet.Cells[row, 6].Value = b.Phone;
                        worksheet.Cells[row, 7].Value = b.Email;
                        worksheet.Cells[row, 8].Value = b.Address;
                        worksheet.Cells[row, 9].Value = b.Opb;
                        worksheet.Cells[row, 10].Value = b.MortgageAmount;
                        worksheet.Cells[row, 11].Value = b.IsRestructuring ? "Y" : "N";
                        worksheet.Cells[row, 12].Value = b.IsOpened ? "Y" : "N";
                        worksheet.Cells[row, 13].Value = b.IsDeceased ? "Y" : "N";

                        // 포맷
                        worksheet.Cells[row, 9, row, 10].Style.Numberformat.Format = "#,##0";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 생성 (XNPV 비교 출력)
        /// </summary>
        public async Task<string> ExportXnpvToExcelAsync(
            IEnumerable<ViewModels.XnpvComparisonItem> items,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("XNPV 비교");

                    // 헤더
                    var headers = new[]
                    {
                        "차주번호", "차주명", "물건수", "총 OPB", 
                        "Loan Cap(1안)", "Loan Cap(2안)", 
                        "XNPV(1안)", "XNPV(2안)",
                        "Ratio(1안)", "Ratio(2안)", "차이(1-2)", "우위안"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cells[1, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 데이터
                    int row = 2;
                    foreach (var item in items)
                    {
                        worksheet.Cells[row, 1].Value = item.BorrowerNumber;
                        worksheet.Cells[row, 2].Value = item.BorrowerName;
                        worksheet.Cells[row, 3].Value = item.PropertyCount;
                        worksheet.Cells[row, 4].Value = item.TotalOpb;
                        worksheet.Cells[row, 5].Value = item.LoanCap1;
                        worksheet.Cells[row, 6].Value = item.LoanCap2;
                        worksheet.Cells[row, 7].Value = item.Xnpv1;
                        worksheet.Cells[row, 8].Value = item.Xnpv2;
                        worksheet.Cells[row, 9].Value = item.Ratio1;
                        worksheet.Cells[row, 10].Value = item.Ratio2;
                        worksheet.Cells[row, 11].Value = item.Difference;
                        worksheet.Cells[row, 12].Value = item.BetterScenario;

                        // 포맷
                        worksheet.Cells[row, 4, row, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 9, row, 10].Style.Numberformat.Format = "0.00%";
                        worksheet.Cells[row, 11].Style.Numberformat.Format = "#,##0";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 생성 (현금흐름 집계 출력)
        /// </summary>
        public async Task<string> ExportCashFlowToExcelAsync(
            IEnumerable<CashFlowSummaryItem> items,
            string borrowerName,
            decimal discountRate,
            decimal xnpv,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("현금흐름 집계");

                    // 정보 영역
                    worksheet.Cells["A1"].Value = "차주명:";
                    worksheet.Cells["B1"].Value = borrowerName;
                    worksheet.Cells["A2"].Value = "적용 할인율:";
                    worksheet.Cells["B2"].Value = discountRate;
                    worksheet.Cells["B2"].Style.Numberformat.Format = "0.00%";
                    worksheet.Cells["A3"].Value = "최종 XNPV:";
                    worksheet.Cells["B3"].Value = xnpv;
                    worksheet.Cells["B3"].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells["A1:A3"].Style.Font.Bold = true;

                    // 헤더 (5행부터)
                    var headers = new[]
                    {
                        "기수", "날짜", "현금유입", "현금유출", "순현금흐름", "누적현금흐름", "비고"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cells[5, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }

                    // 데이터
                    int row = 6;
                    foreach (var item in items)
                    {
                        worksheet.Cells[row, 1].Value = item.Period;
                        worksheet.Cells[row, 2].Value = item.FlowDate.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 3].Value = item.CashInflow;
                        worksheet.Cells[row, 4].Value = item.CashOutflow;
                        worksheet.Cells[row, 5].Value = item.NetCashFlow;
                        worksheet.Cells[row, 6].Value = item.CumulativeCashFlow;
                        worksheet.Cells[row, 7].Value = item.Description;

                        // 포맷
                        worksheet.Cells[row, 3, row, 6].Style.Numberformat.Format = "#,##0";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// Excel 파일 쓰기 (템플릿용)
        /// </summary>
        public async Task WriteExcelFileAsync(
            string filePath,
            List<string> columns,
            List<Dictionary<string, object>> data)
        {
            await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    // 헤더 작성
                    for (int i = 0; i < columns.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = columns[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // 데이터 작성
                    for (int row = 0; row < data.Count; row++)
                    {
                        var rowData = data[row];
                        for (int col = 0; col < columns.Count; col++)
                        {
                            var columnName = columns[col];
                            if (rowData.ContainsKey(columnName))
                            {
                                worksheet.Cells[row + 2, col + 1].Value = rowData[columnName];
                            }
                        }
                    }

                    // 자동 너비 조정
                    worksheet.Cells.AutoFitColumns();

                    // 파일 저장
                    var file = new FileInfo(filePath);
                    package.SaveAs(file);
                }
            });
        }

        /// <summary>
        /// 비핵심 전체 데이터 Excel 내보내기
        /// </summary>
        public async Task<string> ExportNonCoreAllToExcelAsync(
            IEnumerable<Core.Models.Property> properties,
            string programName,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("비핵심 전체");

                    // 프로그램 정보
                    worksheet.Cells["A1"].Value = "프로그램:";
                    worksheet.Cells["B1"].Value = programName;
                    worksheet.Cells["A2"].Value = "출력일시:";
                    worksheet.Cells["B2"].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells["A1:A2"].Style.Font.Bold = true;

                    // 헤더 (4행부터)
                    var headers = new[]
                    {
                        "차주명", "물건번호", "물건유형", "주소", "토지면적", "건물면적",
                        "감정가", "최저입찰가", "OPB", "진행상태", "경매일정", "진행률"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cells[4, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 58, 95)); // 남색
                        cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    }

                    // 데이터
                    int row = 5;
                    foreach (var p in properties)
                    {
                        worksheet.Cells[row, 1].Value = p.DebtorName;
                        worksheet.Cells[row, 2].Value = p.PropertyNumber;
                        worksheet.Cells[row, 3].Value = p.PropertyType;
                        worksheet.Cells[row, 4].Value = p.AddressFull ?? p.AddressRoad ?? p.AddressJibun;
                        worksheet.Cells[row, 5].Value = p.LandArea;
                        worksheet.Cells[row, 6].Value = p.BuildingArea;
                        worksheet.Cells[row, 7].Value = p.AppraisalValue;
                        worksheet.Cells[row, 8].Value = p.MinimumBid;
                        worksheet.Cells[row, 9].Value = p.Opb;
                        worksheet.Cells[row, 10].Value = p.Status;
                        worksheet.Cells[row, 11].Value = p.AuctionScheduleDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 12].Value = $"{p.GetProgressPercent()}%";

                        // 포맷
                        worksheet.Cells[row, 5, row, 6].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[row, 7, row, 9].Style.Numberformat.Format = "#,##0";
                        row++;
                    }

                    // 합계
                    worksheet.Cells[row, 1].Value = "합계";
                    worksheet.Cells[row, 7].Formula = $"SUM(G5:G{row - 1})";
                    worksheet.Cells[row, 8].Formula = $"SUM(H5:H{row - 1})";
                    worksheet.Cells[row, 9].Formula = $"SUM(I5:I{row - 1})";
                    worksheet.Cells[row, 1, row, 12].Style.Font.Bold = true;
                    worksheet.Cells[row, 7, row, 9].Style.Numberformat.Format = "#,##0";

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// 비핵심 차주별 데이터 Excel 내보내기 (각 차주별 시트 분리)
        /// </summary>
        public async Task<string> ExportNonCoreByBorrowerToExcelAsync(
            Dictionary<string, List<Core.Models.Property>> propertiesByBorrower,
            string programName,
            string outputPath)
        {
            return await Task.Run(() =>
            {
                using (var package = new ExcelPackage())
                {
                    // 요약 시트
                    var summarySheet = package.Workbook.Worksheets.Add("요약");
                    summarySheet.Cells["A1"].Value = "프로그램:";
                    summarySheet.Cells["B1"].Value = programName;
                    summarySheet.Cells["A2"].Value = "출력일시:";
                    summarySheet.Cells["B2"].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    summarySheet.Cells["A3"].Value = "차주 수:";
                    summarySheet.Cells["B3"].Value = propertiesByBorrower.Count;
                    summarySheet.Cells["A1:A3"].Style.Font.Bold = true;

                    // 차주별 요약 테이블
                    var summaryHeaders = new[] { "차주명", "물건수", "총 감정가", "총 OPB", "평균 진행률" };
                    for (int i = 0; i < summaryHeaders.Length; i++)
                    {
                        var cell = summarySheet.Cells[5, i + 1];
                        cell.Value = summaryHeaders[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 58, 95));
                        cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    }

                    int summaryRow = 6;
                    foreach (var kvp in propertiesByBorrower.OrderBy(x => x.Key))
                    {
                        var borrowerName = kvp.Key;
                        var props = kvp.Value;

                        summarySheet.Cells[summaryRow, 1].Value = borrowerName;
                        summarySheet.Cells[summaryRow, 2].Value = props.Count;
                        summarySheet.Cells[summaryRow, 3].Value = props.Sum(p => p.AppraisalValue ?? 0);
                        summarySheet.Cells[summaryRow, 4].Value = props.Sum(p => p.Opb ?? 0);
                        summarySheet.Cells[summaryRow, 5].Value = props.Average(p => p.GetProgressPercent());

                        summarySheet.Cells[summaryRow, 3, summaryRow, 4].Style.Numberformat.Format = "#,##0";
                        summarySheet.Cells[summaryRow, 5].Style.Numberformat.Format = "0.0%";
                        summaryRow++;
                    }

                    summarySheet.Cells.AutoFitColumns();

                    // 각 차주별 시트 생성
                    int sheetIndex = 1;
                    foreach (var kvp in propertiesByBorrower.OrderBy(x => x.Key))
                    {
                        var borrowerName = kvp.Key;
                        var props = kvp.Value;

                        // 시트명 제한 (31자) 및 특수문자 제거
                        var sheetName = $"{sheetIndex}_{SanitizeSheetName(borrowerName)}";
                        if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);

                        var worksheet = package.Workbook.Worksheets.Add(sheetName);

                        // 차주 정보
                        worksheet.Cells["A1"].Value = "차주명:";
                        worksheet.Cells["B1"].Value = borrowerName;
                        worksheet.Cells["A2"].Value = "물건 수:";
                        worksheet.Cells["B2"].Value = props.Count;
                        worksheet.Cells["A1:A2"].Style.Font.Bold = true;

                        // 헤더
                        var headers = new[]
                        {
                            "물건번호", "물건유형", "주소", "토지면적", "건물면적",
                            "감정가", "최저입찰가", "OPB", "진행상태", "경매일정", "진행률"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cells[4, i + 1];
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        }

                        // 데이터
                        int row = 5;
                        foreach (var p in props)
                        {
                            worksheet.Cells[row, 1].Value = p.PropertyNumber;
                            worksheet.Cells[row, 2].Value = p.PropertyType;
                            worksheet.Cells[row, 3].Value = p.AddressFull ?? p.AddressRoad ?? p.AddressJibun;
                            worksheet.Cells[row, 4].Value = p.LandArea;
                            worksheet.Cells[row, 5].Value = p.BuildingArea;
                            worksheet.Cells[row, 6].Value = p.AppraisalValue;
                            worksheet.Cells[row, 7].Value = p.MinimumBid;
                            worksheet.Cells[row, 8].Value = p.Opb;
                            worksheet.Cells[row, 9].Value = p.Status;
                            worksheet.Cells[row, 10].Value = p.AuctionScheduleDate?.ToString("yyyy-MM-dd");
                            worksheet.Cells[row, 11].Value = $"{p.GetProgressPercent()}%";

                            worksheet.Cells[row, 4, row, 5].Style.Numberformat.Format = "#,##0.00";
                            worksheet.Cells[row, 6, row, 8].Style.Numberformat.Format = "#,##0";
                            row++;
                        }

                        // 합계
                        worksheet.Cells[row, 1].Value = "합계";
                        worksheet.Cells[row, 6].Formula = $"SUM(F5:F{row - 1})";
                        worksheet.Cells[row, 7].Formula = $"SUM(G5:G{row - 1})";
                        worksheet.Cells[row, 8].Formula = $"SUM(H5:H{row - 1})";
                        worksheet.Cells[row, 1, row, 11].Style.Font.Bold = true;
                        worksheet.Cells[row, 6, row, 8].Style.Numberformat.Format = "#,##0";

                        worksheet.Cells.AutoFitColumns();
                        sheetIndex++;
                    }

                    package.SaveAs(new FileInfo(outputPath));
                    return outputPath;
                }
            });
        }

        /// <summary>
        /// 시트명 정리 (특수문자 제거)
        /// </summary>
        private string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Unknown";

            // Excel 시트명에 사용 불가능한 문자 제거
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        // ==================== IBK Multi-Sheet Upload 지원 ====================

        /// <summary>
        /// Excel 파일의 시트 목록 조회
        /// </summary>
        public List<ExcelSheetInfo> GetSheetNames(string filePath)
        {
            var sheets = new List<ExcelSheetInfo>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var headers = new List<string>();
                    int detectedHeaderRow = 1;
                    
                    if (worksheet.Dimension != null)
                    {
                        int colCount = worksheet.Dimension.End.Column;
                        int rowCount = worksheet.Dimension.End.Row;
                        
                        // 헤더 행 자동 감지 (상위 20행에서 검색)
                        detectedHeaderRow = DetectHeaderRowInWorksheet(worksheet, Math.Min(20, rowCount), Math.Min(30, colCount));
                        
                        // 감지된 헤더 행에서 컬럼명 읽기
                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[detectedHeaderRow, col].Value;
                            if (cellValue != null)
                            {
                                var headerText = cellValue.ToString() ?? "";
                                // 빈 문자열이 아닌 경우에만 추가
                                if (!string.IsNullOrWhiteSpace(headerText))
                                {
                                    headers.Add(headerText);
                                }
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[GetSheetNames] '{worksheet.Name}': 헤더행={detectedHeaderRow}, 컬럼수={headers.Count}");
                    }

                    var sheetInfo = new ExcelSheetInfo
                    {
                        Name = worksheet.Name,
                        Index = worksheet.Index,
                        RowCount = worksheet.Dimension?.End.Row ?? 0,
                        Headers = headers,
                        SheetType = DetectSheetType(worksheet.Name, headers),
                        HeaderRow = detectedHeaderRow  // 감지된 헤더 행 저장
                    };

                    sheets.Add(sheetInfo);
                }
            }

            return sheets;
        }

        /// <summary>
        /// 워크시트 내에서 헤더 행 감지 (이미 열린 워크시트 사용)
        /// </summary>
        private int DetectHeaderRowInWorksheet(ExcelWorksheet worksheet, int maxRows, int maxCols)
        {
            for (int row = 1; row <= maxRows; row++)
            {
                // Field 패턴 찾기
                bool hasFieldPattern = false;
                for (int col = 1; col <= maxCols; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                    if (cellValue.Contains("Field"))
                    {
                        hasFieldPattern = true;
                        break;
                    }
                }

                if (hasFieldPattern)
                {
                    // Field 패턴 발견 - 다음 행이 실제 헤더
                    return row + 1;
                }

                // 헤더 키워드 카운트
                int headerKeywordCount = 0;
                for (int col = 1; col <= maxCols; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                    if (IsHeaderKeyword(cellValue))
                    {
                        headerKeywordCount++;
                    }
                }

                // 헤더 키워드가 3개 이상이면 헤더 행으로 인식
                if (headerKeywordCount >= 3)
                {
                    return row;
                }
            }

            return 1; // 기본값: 첫 번째 행
        }

        /// <summary>
        /// 시트 유형 감지 (시트명 우선, 헤더 패턴 fallback)
        /// </summary>
        public SheetType DetectSheetType(string sheetName, List<string> headers)
        {
            // 1. 시트명 기반 감지 (더 구체적인 매칭 먼저)
            
            // Sheet A-1: 회생차주정보 (A-1이 A보다 먼저 매칭되어야 함)
            if (sheetName.Contains("회생") || sheetName.Contains("Sheet A-1") || sheetName.Contains("A-1"))
                return SheetType.BorrowerRestructuring;
            
            // Sheet A: 차주일반정보
            if (sheetName.Contains("차주일반") || sheetName.Contains("Sheet A"))
                return SheetType.BorrowerGeneral;
            
            // Sheet B: 채권정보
            if (sheetName.Contains("채권") || sheetName.Contains("Sheet B"))
                return SheetType.Loan;
            
            // Sheet C-2: 등기부등본정보 (C-2가 C보다 먼저 매칭되어야 함)
            if (sheetName.Contains("등기부등본") || sheetName.Contains("Sheet C-2") || sheetName.Contains("C-2"))
                return SheetType.RegistryDetail;
            
            // Sheet C-3: 담보설정정보 (C-3이 C보다 먼저 매칭되어야 함)
            if (sheetName.Contains("담보설정") || sheetName.Contains("Sheet C-3") || sheetName.Contains("C-3"))
                return SheetType.CollateralSetting;
            
            // Sheet C-1: 담보물건정보 (C-1만, C 전체가 아님)
            if (sheetName.Contains("물건정보") || sheetName.Contains("Sheet C-1") || sheetName.Contains("C-1"))
                return SheetType.Property;
            
            // Sheet C (번호 없음): 담보물건정보로 간주 (하위호환)
            if ((sheetName.Contains("담보") || sheetName.Contains("물건")) && 
                !sheetName.Contains("C-2") && !sheetName.Contains("C-3"))
                return SheetType.Property;
            
            // Sheet D: 보증정보
            if (sheetName.Contains("보증") || sheetName.Contains("Sheet D"))
                return SheetType.Guarantee;

            // 2. 헤더 패턴 fallback
            if (headers.Any(h => h.Contains("회생사건번호") || h.Contains("관할법원")))
                return SheetType.BorrowerRestructuring;
            
            if (headers.Any(h => h.Contains("대출일련번호") || h.Contains("대출과목")))
                return SheetType.Loan;
            
            if (headers.Any(h => h.Contains("등기부등본") || h.Contains("등기사항")))
                return SheetType.RegistryDetail;
            
            if (headers.Any(h => h.Contains("근저당설정") || h.Contains("담보권설정")))
                return SheetType.CollateralSetting;
            
            if (headers.Any(h => h.Contains("Property") || h.Contains("담보소재지") || h.Contains("경매사건번호")))
                return SheetType.Property;
            
            if (headers.Any(h => h.Contains("보증인") || h.Contains("보증금액")))
                return SheetType.Guarantee;
            
            if (headers.Any(h => h.Contains("차주일련번호") || h.Contains("차주명")))
                return SheetType.BorrowerGeneral;

            return SheetType.Unknown;
        }

        /// <summary>
        /// 특정 시트의 데이터 읽기
        /// </summary>
        public async Task<(List<string> Columns, List<Dictionary<string, object>> Data)> ReadExcelSheetAsync(
            string filePath, 
            string sheetName,
            int headerRow = 1)
        {
            // 시작 컬럼 자동 감지
            var (_, startColumn) = DetectHeaderRowAndColumn(filePath, sheetName);
            return await ReadExcelSheetAsync(filePath, sheetName, headerRow, startColumn);
        }

        /// <summary>
        /// 특정 시트의 데이터 읽기 (시작 컬럼 지정)
        /// 상대방 요청: "일련번호" 기준 좌측 열 제거
        /// </summary>
        public async Task<(List<string> Columns, List<Dictionary<string, object>> Data)> ReadExcelSheetAsync(
            string filePath, 
            string sheetName,
            int headerRow,
            int startColumn)
        {
            return await Task.Run(() =>
            {
                var columns = new List<string>();
                var data = new List<Dictionary<string, object>>();

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];
                    
                    if (worksheet == null)
                    {
                        throw new Exception($"시트를 찾을 수 없습니다: {sheetName}");
                    }

                    if (worksheet.Dimension == null)
                    {
                        throw new Exception($"시트가 비어있습니다: {sheetName}");
                    }

                    int colCount = worksheet.Dimension.End.Column;
                    int rowCount = worksheet.Dimension.End.Row;

                    System.Diagnostics.Debug.WriteLine($"[ReadExcelSheetAsync] '{sheetName}': 헤더행={headerRow}, 시작컬럼={startColumn}, 총컬럼={colCount}, 총행={rowCount}");

                    // 헤더 행에서 컬럼명 읽기 (시작 컬럼부터)
                    for (int col = startColumn; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[headerRow, col].Value;
                        var columnName = cellValue?.ToString()?.Trim() ?? $"Column{col}";
                        
                        // 빈 컬럼명은 건너뛰기
                        if (string.IsNullOrWhiteSpace(columnName))
                        {
                            columnName = $"Column{col}";
                        }
                        
                        // 컬럼명 공백 제거 (상대방 요청)
                        var normalizedColumnName = columnName
                            .Replace("\r\n", " ")
                            .Replace("\n", " ")
                            .Replace("\r", " ")
                            .Trim();
                        
                        // 중복 컬럼명 처리
                        var finalColumnName = normalizedColumnName;
                        if (columns.Contains(finalColumnName))
                        {
                            finalColumnName = $"{finalColumnName}_{col}";
                        }
                        columns.Add(finalColumnName);
                    }

                    // 데이터 읽기 (헤더 행 다음부터, 시작 컬럼부터)
                    for (int row = headerRow + 1; row <= rowCount; row++)
                    {
                        var rowData = new Dictionary<string, object>();
                        bool hasData = false;
                        int columnIndex = 0;

                        for (int col = startColumn; col <= colCount; col++)
                        {
                            if (columnIndex >= columns.Count) break;
                            
                            var cellValue = worksheet.Cells[row, col].Value;
                            var columnName = columns[columnIndex];
                            
                            if (cellValue != null)
                            {
                                rowData[columnName] = cellValue;
                                hasData = true;
                            }
                            else
                            {
                                rowData[columnName] = "";
                            }
                            columnIndex++;
                        }

                        // 빈 행은 제외
                        if (hasData)
                        {
                            data.Add(rowData);
                        }
                    }
                }

                return (columns, data);
            });
        }

        /// <summary>
        /// 헤더 행 자동 감지 (일련번호 기준)
        /// 상대방 요청: 시트별로 "일련번호" 셀을 기준으로 좌측 열, 위측 행들 모두 제거
        /// </summary>
        public int DetectHeaderRow(string filePath, string sheetName)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet?.Dimension == null) return 1;

                int rowCount = Math.Min(30, worksheet.Dimension.End.Row); // 상위 30행에서 찾기
                int colCount = Math.Min(50, worksheet.Dimension.End.Column); // 최대 50개 컬럼 확인

                // 1. "일련번호" 또는 "차주일련번호" 셀 찾기 (최우선)
                for (int row = 1; row <= rowCount; row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
                        var normalizedValue = cellValue.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                        
                        // "일련번호"를 포함하는 셀 찾기 (차주일련번호 제외하고 정확한 "일련번호" 우선)
                        if (normalizedValue == "일련번호" || 
                            (normalizedValue.Contains("일련번호") && !normalizedValue.Contains("차주") && !normalizedValue.Contains("대출") && !normalizedValue.Contains("계좌")))
                        {
                            System.Diagnostics.Debug.WriteLine($"[DetectHeaderRow] '{sheetName}': '일련번호' 발견 (row {row}, col {col})");
                            return row;
                        }
                    }
                }

                // 2. Field 패턴 찾기 (fallback)
                for (int row = 1; row <= rowCount; row++)
                {
                    bool hasFieldPattern = false;
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                        if (cellValue.Contains("Field"))
                        {
                            hasFieldPattern = true;
                            break;
                        }
                    }

                    if (hasFieldPattern)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DetectHeaderRow] '{sheetName}': Field 패턴 발견 (row {row}), 헤더는 row {row + 1}");
                        return row + 1;
                    }
                }

                // 3. 헤더 키워드 기반 감지 (최종 fallback)
                for (int row = 1; row <= rowCount; row++)
                {
                    int headerKeywordCount = 0;
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                        if (IsHeaderKeyword(cellValue))
                        {
                            headerKeywordCount++;
                        }
                    }

                    if (headerKeywordCount >= 3)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DetectHeaderRow] '{sheetName}': 헤더 키워드 {headerKeywordCount}개 발견 (row {row})");
                        return row;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DetectHeaderRow] '{sheetName}': 헤더 감지 실패, 기본값 1");
                return 1;
            }
        }

        /// <summary>
        /// 헤더 행과 시작 컬럼 감지 (일련번호 기준)
        /// </summary>
        /// <returns>(헤더 행 번호, 시작 컬럼 번호)</returns>
        public (int HeaderRow, int StartColumn) DetectHeaderRowAndColumn(string filePath, string sheetName)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet?.Dimension == null) return (1, 1);

                int rowCount = Math.Min(30, worksheet.Dimension.End.Row);
                int colCount = Math.Min(50, worksheet.Dimension.End.Column);

                // "일련번호" 셀 찾기
                for (int row = 1; row <= rowCount; row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
                        var normalizedValue = cellValue.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                        
                        if (normalizedValue == "일련번호" || 
                            (normalizedValue.Contains("일련번호") && !normalizedValue.Contains("차주") && !normalizedValue.Contains("대출") && !normalizedValue.Contains("계좌")))
                        {
                            System.Diagnostics.Debug.WriteLine($"[DetectHeaderRowAndColumn] '{sheetName}': '일련번호' 발견 (row {row}, col {col})");
                            return (row, col);
                        }
                    }
                }

                // 기본값
                return (DetectHeaderRow(filePath, sheetName), 1);
            }
        }

        /// <summary>
        /// 헤더 키워드 판별
        /// </summary>
        private bool IsHeaderKeyword(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            // 데이터디스크에서 사용되는 헤더 키워드
            string[] keywords = new[]
            {
                "일련번호", "차주번호", "차주명", "물건번호", "지번번호",
                "담보", "소재지", "대지면적", "건물면적", "감정평가",
                "Pool", "대출", "계좌", "이자율", "원금", "잔액",
                "채권", "근저당", "설정액", "등기", "Property"
            };

            foreach (var keyword in keywords)
            {
                if (value.Contains(keyword))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 시트의 처음 N행 미리보기 데이터 가져오기 (제목행 확인용)
        /// </summary>
        /// <param name="filePath">엑셀 파일 경로</param>
        /// <param name="sheetName">시트 이름</param>
        /// <param name="previewRowCount">미리보기할 행 수 (기본 10행)</param>
        /// <returns>행 데이터 리스트 (각 행은 셀 값 리스트)</returns>
        public List<List<object?>> GetSheetPreviewRows(string filePath, string sheetName, int previewRowCount = 10)
        {
            var result = new List<List<object?>>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];
                    if (worksheet?.Dimension == null) return result;

                    int rowCount = Math.Min(previewRowCount, worksheet.Dimension.End.Row);
                    int colCount = Math.Min(30, worksheet.Dimension.End.Column); // 최대 30열까지만

                    for (int row = 1; row <= rowCount; row++)
                    {
                        var rowData = new List<object?>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            rowData.Add(cellValue);
                        }
                        result.Add(rowData);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetSheetPreviewRows] Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 시트의 전체 행 수 가져오기
        /// </summary>
        public int GetSheetTotalRowCount(string filePath, string sheetName)
        {
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];
                    if (worksheet?.Dimension == null) return 0;
                    return worksheet.Dimension.End.Row;
                }
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Excel 시트 정보
    /// </summary>
    public class ExcelSheetInfo
    {
        public string Name { get; set; } = "";
        public int Index { get; set; }
        public int RowCount { get; set; }
        public List<string> Headers { get; set; } = new();
        public SheetType SheetType { get; set; }
        public bool IsSelected { get; set; }
        
        /// <summary>
        /// 감지된 헤더 행 번호 (1부터 시작)
        /// </summary>
        public int HeaderRow { get; set; } = 1;

        /// <summary>
        /// 시트 유형 표시명
        /// </summary>
        public string SheetTypeDisplay => SheetType switch
        {
            SheetType.BorrowerGeneral => "차주일반정보",
            SheetType.BorrowerRestructuring => "회생차주정보",
            SheetType.Loan => "채권정보",
            SheetType.Property => "담보물건정보",
            SheetType.RegistryDetail => "등기부등본정보",
            SheetType.CollateralSetting => "담보설정정보",
            SheetType.Guarantee => "보증정보",
            _ => "알 수 없음"
        };

        /// <summary>
        /// 데이터 행 수 (헤더 제외)
        /// </summary>
        public int DataRowCount => Math.Max(0, RowCount - 1);
    }

    /// <summary>
    /// 시트 유형
    /// </summary>
    public enum SheetType
    {
        Unknown,
        BorrowerGeneral,       // Sheet A: 차주일반정보
        BorrowerRestructuring, // Sheet A-1: 회생차주정보
        Loan,                  // Sheet B: 채권일반정보
        Property,              // Sheet C-1: 담보물건정보
        RegistryDetail,        // Sheet C-2: 등기부등본정보
        CollateralSetting,     // Sheet C-3: 담보설정정보
        Guarantee              // Sheet D: 보증정보
    }
}

