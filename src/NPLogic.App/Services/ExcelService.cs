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
    }
}

