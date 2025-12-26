using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 문서 파일 아이템 (토지이용계획확인원, 건축물대장 등)
    /// </summary>
    public class DocumentFileItem : ObservableObject
    {
        private string _fileName = "";
        private string _localPath = "";
        private string _storagePath = "";
        private byte[]? _fileBytes;
        private bool _isUploaded;

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);
        }

        public string StoragePath
        {
            get => _storagePath;
            set => SetProperty(ref _storagePath, value);
        }

        public byte[]? FileBytes
        {
            get => _fileBytes;
            set => SetProperty(ref _fileBytes, value);
        }

        public bool IsUploaded
        {
            get => _isUploaded;
            set => SetProperty(ref _isUploaded, value);
        }
    }

    /// <summary>
    /// 지도 탭 ViewModel - Export 기능 포함
    /// </summary>
    public partial class MapTabViewModel : ObservableObject
    {
        private readonly StorageService? _storageService;
        private readonly ExcelService _excelService;

        private Guid? _propertyId;
        private Property? _property;
        
        // WebView2 캡처용 참조
        private Microsoft.Web.WebView2.Wpf.WebView2? _mapWebView;
        private Microsoft.Web.WebView2.Wpf.WebView2? _roadViewWebView;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // 지적도/위치도 이미지
        [ObservableProperty]
        private BitmapImage? _cadastralMapImage;

        [ObservableProperty]
        private bool _hasCadastralMap;

        // 로드뷰 이미지
        [ObservableProperty]
        private BitmapImage? _roadViewImage;

        [ObservableProperty]
        private bool _hasRoadView;

        // 토지이용계획확인원
        [ObservableProperty]
        private DocumentFileItem? _landUsePlanDocument;

        [ObservableProperty]
        private bool _hasLandUsePlanDocument;

        // 요약건축물대장
        [ObservableProperty]
        private DocumentFileItem? _buildingRegisterDocument;

        [ObservableProperty]
        private bool _hasBuildingRegisterDocument;

        // Export 가능 여부
        [ObservableProperty]
        private bool _canExport;

        public MapTabViewModel(
            StorageService? storageService = null,
            ExcelService? excelService = null)
        {
            _storageService = storageService;
            _excelService = excelService ?? new ExcelService();
        }

        /// <summary>
        /// WebView2 참조 설정 (MapExportTab에서 호출)
        /// </summary>
        public void SetWebViews(
            Microsoft.Web.WebView2.Wpf.WebView2? mapWebView,
            Microsoft.Web.WebView2.Wpf.WebView2? roadViewWebView = null)
        {
            _mapWebView = mapWebView;
            _roadViewWebView = roadViewWebView;
        }

        /// <summary>
        /// 물건 정보 설정
        /// </summary>
        public void SetProperty(Property property)
        {
            _property = property;
            _propertyId = property.Id;
            
            // 기존 업로드된 문서 로드
            _ = LoadExistingDocumentsAsync();
        }

        /// <summary>
        /// 기존 업로드된 문서 로드
        /// </summary>
        private async Task LoadExistingDocumentsAsync()
        {
            if (_storageService == null || _propertyId == null) return;

            try
            {
                var files = await _storageService.ListFilesAsync("attachments", $"properties/{_propertyId}/documents");
                
                foreach (var file in files ?? new System.Collections.Generic.List<Supabase.Storage.FileObject>())
                {
                    if (file.Name.StartsWith("land_use_plan"))
                    {
                        LandUsePlanDocument = new DocumentFileItem
                        {
                            FileName = file.Name,
                            StoragePath = $"properties/{_propertyId}/documents/{file.Name}",
                            IsUploaded = true
                        };
                        HasLandUsePlanDocument = true;
                    }
                    else if (file.Name.StartsWith("building_register"))
                    {
                        BuildingRegisterDocument = new DocumentFileItem
                        {
                            FileName = file.Name,
                            StoragePath = $"properties/{_propertyId}/documents/{file.Name}",
                            IsUploaded = true
                        };
                        HasBuildingRegisterDocument = true;
                    }
                }
                
                UpdateCanExport();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"문서 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// Export 가능 여부 업데이트
        /// </summary>
        private void UpdateCanExport()
        {
            // 최소한 하나의 이미지나 문서가 있으면 Export 가능
            CanExport = HasCadastralMap || HasRoadView || HasLandUsePlanDocument || HasBuildingRegisterDocument;
        }

        #region 지도 이미지 캡처 (WebView2 스크린샷)

        /// <summary>
        /// 지적도/위치도 이미지 캡처 (WebView2 스크린샷)
        /// </summary>
        [RelayCommand]
        private async Task CaptureCadastralMapAsync()
        {
            if (_mapWebView == null || _mapWebView.CoreWebView2 == null)
            {
                ErrorMessage = "지도가 로드되지 않았습니다. 먼저 '지도' 탭에서 지도를 확인해주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // WebView2 스크린샷 캡처
                using var ms = new MemoryStream();
                await _mapWebView.CoreWebView2.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, 
                    ms);
                
                var imageBytes = ms.ToArray();
                
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    CadastralMapImage = LoadBitmapFromBytes(imageBytes);
                    HasCadastralMap = true;
                    SuccessMessage = "지도 이미지가 캡처되었습니다.";
                }
                else
                {
                    ErrorMessage = "지도 이미지를 캡처할 수 없습니다.";
                }

                UpdateCanExport();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"지도 캡처 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 로드뷰 이미지 캡처 (WebView2 스크린샷)
        /// </summary>
        [RelayCommand]
        private async Task CaptureRoadViewAsync()
        {
            if (_roadViewWebView == null || _roadViewWebView.CoreWebView2 == null)
            {
                ErrorMessage = "로드뷰가 로드되지 않았습니다. 먼저 '지도' 탭에서 로드뷰를 켜주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // WebView2 스크린샷 캡처
                using var ms = new MemoryStream();
                await _roadViewWebView.CoreWebView2.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, 
                    ms);
                
                var imageBytes = ms.ToArray();
                
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    RoadViewImage = LoadBitmapFromBytes(imageBytes);
                    HasRoadView = true;
                    SuccessMessage = "로드뷰 이미지가 캡처되었습니다.";
                }
                else
                {
                    ErrorMessage = "로드뷰 이미지를 캡처할 수 없습니다.";
                }

                UpdateCanExport();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"로드뷰 캡처 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 문서 업로드

        /// <summary>
        /// 토지이용계획확인원 파일 선택
        /// </summary>
        [RelayCommand]
        private async Task SelectLandUsePlanDocumentAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "토지이용계획확인원 파일 선택",
                Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.gif;*.bmp|PDF 파일|*.pdf|모든 파일|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    var fileName = Path.GetFileName(dialog.FileName);
                    var extension = Path.GetExtension(dialog.FileName);
                    var fileBytes = await File.ReadAllBytesAsync(dialog.FileName);

                    LandUsePlanDocument = new DocumentFileItem
                    {
                        FileName = fileName,
                        LocalPath = dialog.FileName,
                        FileBytes = fileBytes,
                        IsUploaded = false
                    };
                    HasLandUsePlanDocument = true;

                    // Supabase에 업로드
                    if (_storageService != null && _propertyId != null)
                    {
                        var storagePath = $"properties/{_propertyId}/documents/land_use_plan{extension}";
                        await _storageService.UploadFileAsync("attachments", storagePath, fileBytes);
                        LandUsePlanDocument.StoragePath = storagePath;
                        LandUsePlanDocument.IsUploaded = true;
                    }

                    SuccessMessage = "토지이용계획확인원이 업로드되었습니다.";
                    UpdateCanExport();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"파일 업로드 실패: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 요약건축물대장 파일 선택
        /// </summary>
        [RelayCommand]
        private async Task SelectBuildingRegisterDocumentAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "요약건축물대장 파일 선택",
                Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.gif;*.bmp|PDF 파일|*.pdf|모든 파일|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    var fileName = Path.GetFileName(dialog.FileName);
                    var extension = Path.GetExtension(dialog.FileName);
                    var fileBytes = await File.ReadAllBytesAsync(dialog.FileName);

                    BuildingRegisterDocument = new DocumentFileItem
                    {
                        FileName = fileName,
                        LocalPath = dialog.FileName,
                        FileBytes = fileBytes,
                        IsUploaded = false
                    };
                    HasBuildingRegisterDocument = true;

                    // Supabase에 업로드
                    if (_storageService != null && _propertyId != null)
                    {
                        var storagePath = $"properties/{_propertyId}/documents/building_register{extension}";
                        await _storageService.UploadFileAsync("attachments", storagePath, fileBytes);
                        BuildingRegisterDocument.StoragePath = storagePath;
                        BuildingRegisterDocument.IsUploaded = true;
                    }

                    SuccessMessage = "요약건축물대장이 업로드되었습니다.";
                    UpdateCanExport();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"파일 업로드 실패: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 토지이용계획확인원 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteLandUsePlanDocumentAsync()
        {
            if (LandUsePlanDocument == null) return;

            try
            {
                IsLoading = true;

                if (_storageService != null && LandUsePlanDocument.IsUploaded && !string.IsNullOrEmpty(LandUsePlanDocument.StoragePath))
                {
                    await _storageService.DeleteFileAsync("attachments", LandUsePlanDocument.StoragePath);
                }

                LandUsePlanDocument = null;
                HasLandUsePlanDocument = false;
                UpdateCanExport();
                SuccessMessage = "토지이용계획확인원이 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 요약건축물대장 삭제
        /// </summary>
        [RelayCommand]
        private async Task DeleteBuildingRegisterDocumentAsync()
        {
            if (BuildingRegisterDocument == null) return;

            try
            {
                IsLoading = true;

                if (_storageService != null && BuildingRegisterDocument.IsUploaded && !string.IsNullOrEmpty(BuildingRegisterDocument.StoragePath))
                {
                    await _storageService.DeleteFileAsync("attachments", BuildingRegisterDocument.StoragePath);
                }

                BuildingRegisterDocument = null;
                HasBuildingRegisterDocument = false;
                UpdateCanExport();
                SuccessMessage = "요약건축물대장이 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Excel Export

        /// <summary>
        /// 담보물건 Excel Export
        /// </summary>
        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (_property == null)
            {
                ErrorMessage = "물건 정보가 없습니다.";
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "담보물건 Export",
                Filter = "Excel 파일|*.xlsx",
                FileName = $"담보물건_{_property.PropertyNumber}_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    // 이미지 바이트 배열 준비
                    byte[]? cadastralImageBytes = null;
                    byte[]? roadViewImageBytes = null;
                    byte[]? landUsePlanBytes = null;
                    byte[]? buildingRegisterBytes = null;

                    // 지적도 이미지
                    if (CadastralMapImage != null)
                    {
                        cadastralImageBytes = BitmapImageToBytes(CadastralMapImage);
                    }

                    // 로드뷰 이미지
                    if (RoadViewImage != null)
                    {
                        roadViewImageBytes = BitmapImageToBytes(RoadViewImage);
                    }

                    // 토지이용계획확인원
                    if (LandUsePlanDocument != null)
                    {
                        if (LandUsePlanDocument.FileBytes != null)
                        {
                            landUsePlanBytes = LandUsePlanDocument.FileBytes;
                        }
                        else if (_storageService != null && !string.IsNullOrEmpty(LandUsePlanDocument.StoragePath))
                        {
                            landUsePlanBytes = await _storageService.DownloadFileAsync("attachments", LandUsePlanDocument.StoragePath);
                        }
                    }

                    // 요약건축물대장
                    if (BuildingRegisterDocument != null)
                    {
                        if (BuildingRegisterDocument.FileBytes != null)
                        {
                            buildingRegisterBytes = BuildingRegisterDocument.FileBytes;
                        }
                        else if (_storageService != null && !string.IsNullOrEmpty(BuildingRegisterDocument.StoragePath))
                        {
                            buildingRegisterBytes = await _storageService.DownloadFileAsync("attachments", BuildingRegisterDocument.StoragePath);
                        }
                    }

                    // Excel 생성
                    await ExportPropertyDocumentsToExcelAsync(
                        saveDialog.FileName,
                        _property,
                        cadastralImageBytes,
                        roadViewImageBytes,
                        landUsePlanBytes,
                        buildingRegisterBytes);

                    SuccessMessage = $"Excel 파일이 저장되었습니다.\n{saveDialog.FileName}";

                    // 파일 열기
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Export 실패: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 담보물건 문서를 Excel로 Export
        /// </summary>
        private async Task ExportPropertyDocumentsToExcelAsync(
            string outputPath,
            Property property,
            byte[]? cadastralImageBytes,
            byte[]? roadViewImageBytes,
            byte[]? landUsePlanBytes,
            byte[]? buildingRegisterBytes)
        {
            await Task.Run(() =>
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("담보물건");

                // 기본 정보 헤더 (1-2행)
                worksheet.Cell("A1").Value = "물건번호";
                worksheet.Cell("B1").Value = property.PropertyNumber ?? "";
                worksheet.Cell("C1").Value = "주소";
                worksheet.Cell("D1").Value = property.AddressFull ?? "";
                worksheet.Range("A1:D1").Style.Font.Bold = true;
                worksheet.Range("A1:D1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1E3A5F");
                worksheet.Range("A1:D1").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

                // 4분할 헤더 (3행)
                worksheet.Cell("A3").Value = "지적도 & 위치도";
                worksheet.Cell("C3").Value = "로드뷰";
                worksheet.Range("A3:B3").Merge();
                worksheet.Range("C3:D3").Merge();
                worksheet.Range("A3:D3").Style.Font.Bold = true;
                worksheet.Range("A3:D3").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#3B82F6");
                worksheet.Range("A3:D3").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                worksheet.Range("A3:D3").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // 이미지 영역 (4-18행, 약 300px 높이)
                int imageRowStart = 4;
                int imageRowEnd = 18;
                
                // 행 높이 설정
                for (int r = imageRowStart; r <= imageRowEnd; r++)
                {
                    worksheet.Row(r).Height = 20;
                }

                // 열 너비 설정
                worksheet.Column("A").Width = 30;
                worksheet.Column("B").Width = 30;
                worksheet.Column("C").Width = 30;
                worksheet.Column("D").Width = 30;

                // 지적도 이미지 삽입
                if (cadastralImageBytes != null && cadastralImageBytes.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(cadastralImageBytes);
                        var picture = worksheet.AddPicture(ms)
                            .MoveTo(worksheet.Cell("A4"))
                            .WithSize(400, 280);
                    }
                    catch { /* 이미지 삽입 실패 시 무시 */ }
                }

                // 로드뷰 이미지 삽입
                if (roadViewImageBytes != null && roadViewImageBytes.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(roadViewImageBytes);
                        var picture = worksheet.AddPicture(ms)
                            .MoveTo(worksheet.Cell("C4"))
                            .WithSize(400, 280);
                    }
                    catch { /* 이미지 삽입 실패 시 무시 */ }
                }

                // 하단 헤더 (19행)
                worksheet.Cell("A19").Value = "토지이용계획확인원";
                worksheet.Cell("C19").Value = "요약건축물대장";
                worksheet.Range("A19:B19").Merge();
                worksheet.Range("C19:D19").Merge();
                worksheet.Range("A19:D19").Style.Font.Bold = true;
                worksheet.Range("A19:D19").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#10B981");
                worksheet.Range("A19:D19").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                worksheet.Range("A19:D19").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // 하단 이미지 영역 (20-34행)
                for (int r = 20; r <= 34; r++)
                {
                    worksheet.Row(r).Height = 20;
                }

                // 토지이용계획확인원 이미지 삽입
                if (landUsePlanBytes != null && landUsePlanBytes.Length > 0)
                {
                    try
                    {
                        // PDF인 경우 표시 불가 메시지
                        if (IsPdfBytes(landUsePlanBytes))
                        {
                            worksheet.Cell("A20").Value = "[PDF 파일 - Excel에서 직접 표시 불가]";
                            worksheet.Cell("A21").Value = "별도 첨부 파일 참조";
                        }
                        else
                        {
                            using var ms = new MemoryStream(landUsePlanBytes);
                            var picture = worksheet.AddPicture(ms)
                                .MoveTo(worksheet.Cell("A20"))
                                .WithSize(400, 280);
                        }
                    }
                    catch { /* 이미지 삽입 실패 시 무시 */ }
                }

                // 요약건축물대장 이미지 삽입
                if (buildingRegisterBytes != null && buildingRegisterBytes.Length > 0)
                {
                    try
                    {
                        // PDF인 경우 표시 불가 메시지
                        if (IsPdfBytes(buildingRegisterBytes))
                        {
                            worksheet.Cell("C20").Value = "[PDF 파일 - Excel에서 직접 표시 불가]";
                            worksheet.Cell("C21").Value = "별도 첨부 파일 참조";
                        }
                        else
                        {
                            using var ms = new MemoryStream(buildingRegisterBytes);
                            var picture = worksheet.AddPicture(ms)
                                .MoveTo(worksheet.Cell("C20"))
                                .WithSize(400, 280);
                        }
                    }
                    catch { /* 이미지 삽입 실패 시 무시 */ }
                }

                // 테두리 추가
                worksheet.Range("A3:D34").Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                worksheet.Range("A3:B18").Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                worksheet.Range("C3:D18").Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                worksheet.Range("A19:B34").Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                worksheet.Range("C19:D34").Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                workbook.SaveAs(outputPath);
            });
        }

        /// <summary>
        /// PDF 파일인지 확인
        /// </summary>
        private bool IsPdfBytes(byte[] bytes)
        {
            if (bytes.Length < 4) return false;
            // PDF 시그니처: %PDF
            return bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 바이트 배열에서 BitmapImage 로드
        /// </summary>
        private BitmapImage LoadBitmapFromBytes(byte[] imageData)
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(imageData))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }

        /// <summary>
        /// BitmapImage를 바이트 배열로 변환
        /// </summary>
        private byte[] BitmapImageToBytes(BitmapImage bitmapImage)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }

        #endregion

        #region 외부 사이트 열기

        /// <summary>
        /// 정부24 열기 (토지이용계획확인원 발급)
        /// </summary>
        [RelayCommand]
        private void OpenGov24LandUsePlan()
        {
            try
            {
                var url = "https://www.gov.kr/mw/AA020InfoCappView.do?HighCtgCD=A01010&CappBizCD=13100000015";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 정부24 열기 (건축물대장 발급)
        /// </summary>
        [RelayCommand]
        private void OpenGov24BuildingRegister()
        {
            try
            {
                var url = "https://www.gov.kr/mw/AA020InfoCappView.do?HighCtgCD=A01010&CappBizCD=12100000041";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// LURIS 열기
        /// </summary>
        [RelayCommand]
        private void OpenLuris()
        {
            try
            {
                var url = "https://www.eum.go.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        #endregion
    }
}

