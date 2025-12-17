using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// OCR PDF 파일 정보
    /// </summary>
    public partial class OcrPdfFile : ObservableObject
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        
        [ObservableProperty]
        private string _status = "대기";
        
        [ObservableProperty]
        private int _progress;
        
        [ObservableProperty]
        private string? _errorMessage;

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024:N0} KB";
                return $"{FileSize / (1024 * 1024):N1} MB";
            }
        }
    }

    /// <summary>
    /// 등기부 탭 ViewModel
    /// </summary>
    public partial class RegistryTabViewModel : ObservableObject
    {
        private readonly RegistryRepository _registryRepository;
        private readonly RegistryOcrService? _ocrService;
        private Guid? _propertyId;
        private CancellationTokenSource? _ocrCancellationTokenSource;

        #region Observable Properties

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // ========== 기본 정보 ==========
        
        /// <summary>
        /// 물건번호 (지번일련번호)
        /// </summary>
        [ObservableProperty]
        private string? _propertyNumber;

        /// <summary>
        /// 물건지 (등기부 기준)
        /// </summary>
        [ObservableProperty]
        private string? _registryAddress;

        /// <summary>
        /// 물건지 (DD 기준)
        /// </summary>
        [ObservableProperty]
        private string? _ddAddress;

        /// <summary>
        /// 주소 일치 여부
        /// </summary>
        [ObservableProperty]
        private bool _isAddressMatch = true;

        /// <summary>
        /// 담보물 형태
        /// </summary>
        [ObservableProperty]
        private string? _collateralType;

        /// <summary>
        /// 대지면적 (평)
        /// </summary>
        [ObservableProperty]
        private decimal? _landAreaPyeong;

        /// <summary>
        /// 건물면적 (평)
        /// </summary>
        [ObservableProperty]
        private decimal? _buildingAreaPyeong;

        // ========== 소유자 정보 ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryOwner> _owners = new();

        [ObservableProperty]
        private RegistryOwner? _selectedOwner;

        [ObservableProperty]
        private bool _hasNoOwners = true;

        // ========== 갑구 (소유권) ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryRight> _gapguRights = new();

        [ObservableProperty]
        private RegistryRight? _selectedGapguRight;

        [ObservableProperty]
        private bool _hasNoGapguRights = true;

        /// <summary>
        /// 갑구 청구금액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _gapguTotalAmount;

        // ========== 을구 (근저당/전세권) ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryRight> _eulguRights = new();

        [ObservableProperty]
        private RegistryRight? _selectedEulguRight;

        [ObservableProperty]
        private bool _hasNoEulguRights = true;

        /// <summary>
        /// 을구 채권최고액 합계
        /// </summary>
        [ObservableProperty]
        private decimal _eulguTotalAmount;

        // ========== 등기부 문서 ==========
        
        [ObservableProperty]
        private ObservableCollection<RegistryDocument> _documents = new();

        [ObservableProperty]
        private bool _hasNoDocuments = true;

        // ========== OCR 업로드 관련 ==========

        [ObservableProperty]
        private ObservableCollection<OcrPdfFile> _ocrPdfFiles = new();

        [ObservableProperty]
        private bool _hasOcrPdfFiles;

        [ObservableProperty]
        private bool _isOcrProcessing;

        [ObservableProperty]
        private string? _ocrStatusMessage;

        [ObservableProperty]
        private int _ocrProgressPercent;

        [ObservableProperty]
        private int _ocrCompletedCount;

        [ObservableProperty]
        private int _ocrTotalCount;

        [ObservableProperty]
        private bool _isOcrServerReady;

        [ObservableProperty]
        private string? _ocrServerStatus = "서버 확인 중...";

        // OCR 결과 미리보기
        [ObservableProperty]
        private ObservableCollection<RegistryOwner> _ocrPreviewOwners = new();

        [ObservableProperty]
        private ObservableCollection<RegistryRight> _ocrPreviewGapgu = new();

        [ObservableProperty]
        private ObservableCollection<RegistryRight> _ocrPreviewEulgu = new();

        [ObservableProperty]
        private bool _hasOcrResults;

        [ObservableProperty]
        private string? _ocrExtractedAddress;

        #endregion

        public RegistryTabViewModel(RegistryRepository registryRepository, RegistryOcrService? ocrService = null)
        {
            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
            _ocrService = ocrService;
        }

        /// <summary>
        /// 물건 ID 설정 및 초기화
        /// </summary>
        public void SetPropertyId(Guid propertyId)
        {
            _propertyId = propertyId;
        }

        /// <summary>
        /// 물건 정보로 기본 정보 설정
        /// </summary>
        public void SetPropertyInfo(Property property)
        {
            if (property == null) return;

            PropertyNumber = property.PropertyNumber;
            RegistryAddress = property.AddressFull;
            DdAddress = property.AddressFull; // DD와 비교할 주소 (추후 DD 데이터에서 가져옴)
            IsAddressMatch = true; // 추후 비교 로직 구현
            CollateralType = property.PropertyType;

            // 면적 평 환산 (1평 = 3.3058㎡)
            const decimal pyeongConverter = 3.3058m;
            LandAreaPyeong = property.LandArea.HasValue ? Math.Round(property.LandArea.Value / pyeongConverter, 2) : null;
            BuildingAreaPyeong = property.BuildingArea.HasValue ? Math.Round(property.BuildingArea.Value / pyeongConverter, 2) : null;
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (_propertyId == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 소유자 정보 로드
                var owners = await _registryRepository.GetOwnersByPropertyIdAsync(_propertyId.Value);
                Owners = new ObservableCollection<RegistryOwner>(owners);
                HasNoOwners = Owners.Count == 0;

                // 갑구 권리 로드
                var gapguRights = await _registryRepository.GetGapguRightsAsync(_propertyId.Value);
                GapguRights = new ObservableCollection<RegistryRight>(gapguRights);
                HasNoGapguRights = GapguRights.Count == 0;
                GapguTotalAmount = GapguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);

                // 을구 권리 로드
                var eulguRights = await _registryRepository.GetEulguRightsAsync(_propertyId.Value);
                EulguRights = new ObservableCollection<RegistryRight>(eulguRights);
                HasNoEulguRights = EulguRights.Count == 0;
                EulguTotalAmount = EulguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);

                // 등기부 문서 로드
                var documents = await _registryRepository.GetDocumentsByPropertyIdAsync(_propertyId.Value);
                Documents = new ObservableCollection<RegistryDocument>(documents);
                HasNoDocuments = Documents.Count == 0;
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

        #region 소유자 관련 Commands

        [RelayCommand]
        private async Task AddOwnerAsync()
        {
            if (_propertyId == null) return;

            try
            {
                var newOwner = new RegistryOwner
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId,
                    OwnerName = "새 소유자",
                    ShareRatio = "100%",
                    CreatedAt = DateTime.UtcNow
                };

                var created = await _registryRepository.CreateOwnerAsync(newOwner);
                Owners.Add(created);
                HasNoOwners = false;
                SelectedOwner = created;
                SuccessMessage = "소유자가 추가되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"소유자 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteOwnerAsync()
        {
            if (SelectedOwner == null) return;

            try
            {
                await _registryRepository.DeleteOwnerAsync(SelectedOwner.Id);
                Owners.Remove(SelectedOwner);
                HasNoOwners = Owners.Count == 0;
                SelectedOwner = null;
                SuccessMessage = "소유자가 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"소유자 삭제 실패: {ex.Message}";
            }
        }

        #endregion

        #region 갑구 관련 Commands

        [RelayCommand]
        private async Task AddGapguRightAsync()
        {
            if (_propertyId == null) return;

            try
            {
                var maxOrder = GapguRights.Any() ? GapguRights.Max(r => r.RightOrder ?? 0) : 0;
                var newRight = new RegistryRight
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId,
                    RightType = "gap",
                    RightOrder = maxOrder + 1,
                    RegistrationCause = "가압류",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _registryRepository.CreateRightAsync(newRight);
                GapguRights.Add(created);
                HasNoGapguRights = false;
                SelectedGapguRight = created;
                UpdateGapguTotal();
                SuccessMessage = "갑구 권리가 추가되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"갑구 권리 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteGapguRightAsync()
        {
            if (SelectedGapguRight == null) return;

            try
            {
                await _registryRepository.DeleteRightAsync(SelectedGapguRight.Id);
                GapguRights.Remove(SelectedGapguRight);
                HasNoGapguRights = GapguRights.Count == 0;
                SelectedGapguRight = null;
                UpdateGapguTotal();
                SuccessMessage = "갑구 권리가 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"갑구 권리 삭제 실패: {ex.Message}";
            }
        }

        private void UpdateGapguTotal()
        {
            GapguTotalAmount = GapguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);
        }

        #endregion

        #region 을구 관련 Commands

        [RelayCommand]
        private async Task AddEulguRightAsync()
        {
            if (_propertyId == null) return;

            try
            {
                var maxOrder = EulguRights.Any() ? EulguRights.Max(r => r.RightOrder ?? 0) : 0;
                var newRight = new RegistryRight
                {
                    Id = Guid.NewGuid(),
                    PropertyId = _propertyId,
                    RightType = "eul",
                    RightOrder = maxOrder + 1,
                    RegistrationCause = "근저당권",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _registryRepository.CreateRightAsync(newRight);
                EulguRights.Add(created);
                HasNoEulguRights = false;
                SelectedEulguRight = created;
                UpdateEulguTotal();
                SuccessMessage = "을구 권리가 추가되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"을구 권리 추가 실패: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteEulguRightAsync()
        {
            if (SelectedEulguRight == null) return;

            try
            {
                await _registryRepository.DeleteRightAsync(SelectedEulguRight.Id);
                EulguRights.Remove(SelectedEulguRight);
                HasNoEulguRights = EulguRights.Count == 0;
                SelectedEulguRight = null;
                UpdateEulguTotal();
                SuccessMessage = "을구 권리가 삭제되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"을구 권리 삭제 실패: {ex.Message}";
            }
        }

        private void UpdateEulguTotal()
        {
            EulguTotalAmount = EulguRights.Where(r => r.Status == "active").Sum(r => r.ClaimAmount ?? 0);
        }

        #endregion

        #region 권리 저장 Command

        [RelayCommand]
        private async Task SaveAllRightsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // 소유자 저장
                foreach (var owner in Owners)
                {
                    await _registryRepository.UpdateOwnerAsync(owner);
                }

                // 갑구 저장
                foreach (var right in GapguRights)
                {
                    await _registryRepository.UpdateRightAsync(right);
                }

                // 을구 저장
                foreach (var right in EulguRights)
                {
                    await _registryRepository.UpdateRightAsync(right);
                }

                SuccessMessage = "모든 등기부 정보가 저장되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 새로고침 Command

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        #endregion

        #region OCR 관련 Commands

        /// <summary>
        /// OCR 서버 상태 확인
        /// </summary>
        public async Task CheckOcrServerStatusAsync()
        {
            if (_ocrService == null)
            {
                OcrServerStatus = "OCR 서비스 비활성화";
                IsOcrServerReady = false;
                return;
            }

            try
            {
                OcrServerStatus = "서버 확인 중...";
                var isHealthy = await _ocrService.CheckHealthAsync();
                
                if (isHealthy)
                {
                    IsOcrServerReady = true;
                    OcrServerStatus = "서버 준비됨 ✓";
                }
                else
                {
                    // 서버 시작 시도
                    OcrServerStatus = "서버 시작 중...";
                    var started = await _ocrService.StartServerAsync();
                    
                    if (started)
                    {
                        IsOcrServerReady = true;
                        OcrServerStatus = "서버 준비됨 ✓";
                    }
                    else
                    {
                        IsOcrServerReady = false;
                        OcrServerStatus = "서버 시작 실패";
                    }
                }
            }
            catch (Exception ex)
            {
                IsOcrServerReady = false;
                OcrServerStatus = $"서버 오류: {ex.Message}";
            }
        }

        /// <summary>
        /// PDF 파일 선택
        /// </summary>
        [RelayCommand]
        private void SelectOcrPdfFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "등기부등본 PDF 선택",
                Filter = "PDF 파일|*.pdf",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    // 중복 체크
                    if (OcrPdfFiles.Any(f => f.FilePath == filePath))
                        continue;

                    var fileInfo = new FileInfo(filePath);
                    OcrPdfFiles.Add(new OcrPdfFile
                    {
                        FilePath = filePath,
                        FileName = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        Status = "대기"
                    });
                }

                HasOcrPdfFiles = OcrPdfFiles.Count > 0;
            }
        }

        /// <summary>
        /// PDF 파일 제거
        /// </summary>
        [RelayCommand]
        private void RemoveOcrPdfFile(OcrPdfFile? file)
        {
            if (file == null) return;
            
            OcrPdfFiles.Remove(file);
            HasOcrPdfFiles = OcrPdfFiles.Count > 0;
        }

        /// <summary>
        /// 모든 PDF 파일 취소
        /// </summary>
        [RelayCommand]
        private void CancelAllOcrPdfFiles()
        {
            OcrPdfFiles.Clear();
            HasOcrPdfFiles = false;
            HasOcrResults = false;
            OcrPreviewOwners.Clear();
            OcrPreviewGapgu.Clear();
            OcrPreviewEulgu.Clear();
            OcrExtractedAddress = null;
        }

        /// <summary>
        /// OCR 처리 시작
        /// </summary>
        [RelayCommand]
        private async Task StartOcrProcessingAsync()
        {
            if (_ocrService == null || OcrPdfFiles.Count == 0)
                return;

            if (!IsOcrServerReady)
            {
                ErrorMessage = "OCR 서버가 준비되지 않았습니다.";
                return;
            }

            try
            {
                IsOcrProcessing = true;
                ErrorMessage = null;
                SuccessMessage = null;

                _ocrCancellationTokenSource = new CancellationTokenSource();
                var token = _ocrCancellationTokenSource.Token;

                OcrTotalCount = OcrPdfFiles.Count;
                OcrCompletedCount = 0;
                OcrProgressPercent = 0;

                // 결과 초기화
                OcrPreviewOwners.Clear();
                OcrPreviewGapgu.Clear();
                OcrPreviewEulgu.Clear();

                foreach (var pdfFile in OcrPdfFiles)
                {
                    if (token.IsCancellationRequested)
                        break;

                    pdfFile.Status = "처리 중";
                    pdfFile.Progress = 0;
                    OcrStatusMessage = $"처리 중: {pdfFile.FileName}";

                    try
                    {
                        var progress = new Progress<OcrProgress>(p =>
                        {
                            pdfFile.Progress = p.ProgressPercent;
                        });

                        var result = await _ocrService.ProcessPdfAsync(pdfFile.FilePath, progress, token);

                        if (result.Success && result.Data != null)
                        {
                            pdfFile.Status = "완료";
                            pdfFile.Progress = 100;

                            // 주소 저장
                            if (!string.IsNullOrEmpty(result.Data.Address))
                            {
                                OcrExtractedAddress = result.Data.Address;
                            }

                            // 결과 파싱 및 미리보기에 추가
                            ParseOcrResultToPreview(result.Data, pdfFile.FileName);
                        }
                        else
                        {
                            pdfFile.Status = "실패";
                            pdfFile.ErrorMessage = result.Error ?? "알 수 없는 오류";
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        pdfFile.Status = "취소됨";
                    }
                    catch (Exception ex)
                    {
                        pdfFile.Status = "실패";
                        pdfFile.ErrorMessage = ex.Message;
                    }

                    OcrCompletedCount++;
                    OcrProgressPercent = (OcrCompletedCount * 100) / OcrTotalCount;
                }

                HasOcrResults = OcrPreviewOwners.Count > 0 || OcrPreviewGapgu.Count > 0 || OcrPreviewEulgu.Count > 0;
                
                var successCount = OcrPdfFiles.Count(f => f.Status == "완료");
                var failCount = OcrPdfFiles.Count(f => f.Status == "실패");
                
                OcrStatusMessage = $"완료: {successCount}개 성공, {failCount}개 실패";
                
                if (successCount > 0)
                {
                    SuccessMessage = $"OCR 처리 완료! {successCount}개 파일에서 데이터를 추출했습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"OCR 처리 중 오류: {ex.Message}";
            }
            finally
            {
                IsOcrProcessing = false;
                _ocrCancellationTokenSource?.Dispose();
                _ocrCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// OCR 처리 취소
        /// </summary>
        [RelayCommand]
        private void CancelOcrProcessing()
        {
            _ocrCancellationTokenSource?.Cancel();
            OcrStatusMessage = "취소 중...";
        }

        /// <summary>
        /// OCR 결과를 미리보기 컬렉션에 파싱
        /// </summary>
        private void ParseOcrResultToPreview(OcrResultData data, string sourceFileName)
        {
            // 소유자 파싱
            if (data.Owners != null)
            {
                foreach (var ownerDict in data.Owners)
                {
                    var owner = new RegistryOwner
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = _propertyId,
                        OwnerName = GetStringValue(ownerDict, "등기명의인"),
                        OwnerRegNo = GetStringValue(ownerDict, "(주민)등록번호"),
                        ShareRatio = GetStringValue(ownerDict, "최종지분"),
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    // 주소 파싱에서 등기원인 추출 시도
                    var address = GetStringValue(ownerDict, "주소");
                    if (!string.IsNullOrEmpty(address))
                    {
                        owner.RegistrationCause = address;
                    }
                    
                    OcrPreviewOwners.Add(owner);
                }
            }

            // 갑구 파싱
            if (data.Gapgu != null)
            {
                foreach (var gapDict in data.Gapgu)
                {
                    var right = new RegistryRight
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = _propertyId,
                        RightType = "gap",
                        RightOrder = ParseInt(GetStringValue(gapDict, "순위번호")),
                        RegistrationCause = GetStringValue(gapDict, "등기목적"),
                        RegistrationNumber = GetStringValue(gapDict, "접수정보"),
                        RegistrationDate = ParseDate(GetStringValue(gapDict, "접수날짜")),
                        RightHolder = GetStringValue(gapDict, "권리자/채권자/가등기권자"),
                        ClaimAmount = ParseDecimal(GetStringValue(gapDict, "청구금액")),
                        Notes = GetStringValue(gapDict, "비고"),
                        Status = "active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    OcrPreviewGapgu.Add(right);
                }
            }

            // 을구 파싱
            if (data.Eulgu != null)
            {
                foreach (var eulDict in data.Eulgu)
                {
                    var right = new RegistryRight
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = _propertyId,
                        RightType = "eul",
                        RightOrder = ParseInt(GetStringValue(eulDict, "순위번호")),
                        RegistrationCause = GetStringValue(eulDict, "등기목적"),
                        RegistrationNumber = GetStringValue(eulDict, "접수정보"),
                        RegistrationDate = ParseDate(GetStringValue(eulDict, "접수날짜")),
                        RightHolder = GetStringValue(eulDict, "근저당권자/전세권자/채권자"),
                        ClaimAmount = ParseDecimal(GetStringValue(eulDict, "채권최고액/전세금")),
                        Debtor = GetStringValue(eulDict, "채무자"),
                        Status = "active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    OcrPreviewEulgu.Add(right);
                }
            }
        }

        /// <summary>
        /// OCR 결과를 DB에 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveOcrResultsAsync()
        {
            if (_propertyId == null)
            {
                ErrorMessage = "물건 ID가 설정되지 않았습니다.";
                return;
            }

            if (!HasOcrResults)
            {
                ErrorMessage = "저장할 OCR 결과가 없습니다.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                int savedOwners = 0, savedGapgu = 0, savedEulgu = 0;

                // 소유자 저장
                foreach (var owner in OcrPreviewOwners)
                {
                    owner.PropertyId = _propertyId;
                    await _registryRepository.CreateOwnerAsync(owner);
                    savedOwners++;
                }

                // 갑구 저장
                foreach (var right in OcrPreviewGapgu)
                {
                    right.PropertyId = _propertyId;
                    await _registryRepository.CreateRightAsync(right);
                    savedGapgu++;
                }

                // 을구 저장
                foreach (var right in OcrPreviewEulgu)
                {
                    right.PropertyId = _propertyId;
                    await _registryRepository.CreateRightAsync(right);
                    savedEulgu++;
                }

                SuccessMessage = $"저장 완료: 소유자 {savedOwners}건, 갑구 {savedGapgu}건, 을구 {savedEulgu}건";

                // 미리보기 초기화 및 실제 데이터 새로고침
                CancelAllOcrPdfFiles();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region Helper Methods

        private static string? GetStringValue(Dictionary<string, object?>? dict, string key)
        {
            if (dict == null || !dict.TryGetValue(key, out var value))
                return null;
            
            return value?.ToString();
        }

        private static int? ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // 숫자만 추출
            var digits = Regex.Replace(value, @"[^\d]", "");
            if (int.TryParse(digits, out var result))
                return result;
            
            return null;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // 숫자와 소수점만 추출
            var cleaned = Regex.Replace(value, @"[^\d.]", "");
            if (decimal.TryParse(cleaned, out var result))
                return result;
            
            return null;
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // 다양한 날짜 형식 시도
            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy.MM.dd",
                "yyyy/MM/dd",
                "yyyyMMdd",
                "yyyy년 MM월 dd일",
                "yyyy년MM월dd일"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
            }

            // 정규식으로 날짜 추출 시도
            var dateMatch = Regex.Match(value, @"(\d{4})[-./년\s]*(\d{1,2})[-./월\s]*(\d{1,2})");
            if (dateMatch.Success)
            {
                var year = int.Parse(dateMatch.Groups[1].Value);
                var month = int.Parse(dateMatch.Groups[2].Value);
                var day = int.Parse(dateMatch.Groups[3].Value);
                
                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    // 유효하지 않은 날짜
                }
            }

            return null;
        }

        #endregion

        #endregion
    }
}

