using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

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

                    // 자동 너비 조정
                    worksheet.Cells.AutoFitColumns();

                    // 파일 저장
                    var file = new FileInfo(outputPath);
                    package.SaveAs(file);
                    
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

