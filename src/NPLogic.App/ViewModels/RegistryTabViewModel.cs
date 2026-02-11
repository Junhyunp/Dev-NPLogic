using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
    /// OCR PDF 파일 정보 + 물건 매칭 정보
    /// </summary>
    public partial class OcrPdfFileWithMatch : OcrPdfFile
    {
        /// <summary>
        /// 매칭된 물건
        /// </summary>
        [ObservableProperty]
        private Property? _matchedProperty;

        /// <summary>
        /// OCR에서 추출된 주소
        /// </summary>
        [ObservableProperty]
        private string? _extractedAddress;

        /// <summary>
        /// 자동 매칭 여부
        /// </summary>
        [ObservableProperty]
        private bool _isAutoMatched;

        /// <summary>
        /// 매칭 신뢰도 (0~1)
        /// </summary>
        [ObservableProperty]
        private double _matchConfidence;

        /// <summary>
        /// OCR 결과 데이터 (저장용)
        /// </summary>
        public OcrResultData? OcrResultData { get; set; }

        /// <summary>
        /// 매칭 상태 텍스트
        /// </summary>
        public string MatchStatusText => MatchedProperty != null
            ? (IsAutoMatched ? $"자동매칭 ({MatchConfidence:P0})" : "수동선택")
            : "미매칭";

        partial void OnMatchedPropertyChanged(Property? value)
        {
            // 처리 중에는 변경 무시 (UI에서도 비활성화)
            if (Status == "처리 중")
                return;

            // 사용자가 바꾸면 수동선택으로 간주
            IsAutoMatched = false;
            MatchConfidence = 0;

            // 이미 완료된 파일의 매칭을 바꾸면 재처리 대상으로 되돌림
            if (Status == "완료")
            {
                Status = "대기";
                Progress = 0;
                ErrorMessage = null;
            }

            // MatchStatusText는 computed property이므로 수동 알림 필요
            OnPropertyChanged(nameof(MatchStatusText));
        }

        partial void OnIsAutoMatchedChanged(bool value)
        {
            OnPropertyChanged(nameof(MatchStatusText));
        }

        partial void OnMatchConfidenceChanged(double value)
        {
            OnPropertyChanged(nameof(MatchStatusText));
        }
    }

    /// <summary>
    /// 등기부 탭 ViewModel
    /// </summary>
    public partial class RegistryTabViewModel : ObservableObject
    {
        private readonly RegistryRepository _registryRepository;
        private readonly RegistryOcrService? _ocrService;
        private readonly PropertyRepository? _propertyRepository;
        private Guid? _propertyId;
        private Guid? _programId;
        private CancellationTokenSource? _ocrCancellationTokenSource;
        private bool _suppressSelectedResultPropertyChanged;
        private bool _suppressSelectedRegistryRunChanged;

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

        /// <summary>
        /// 등기부등본 요약 페이지 이미지들 (Base64 → BitmapImage 변환)
        /// 주요 등기사항 요약이 여러 페이지일 수 있음
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BitmapImage> _summaryImages = new();

        /// <summary>
        /// 요약 이미지가 있는지 여부
        /// </summary>
        [ObservableProperty]
        private bool _hasSummaryImage;

        /// <summary>
        /// 현재 요약 이미지 페이지 수
        /// </summary>
        public int SummaryImageCount => SummaryImages.Count;

        // ========== 물건 매칭 관련 ==========

        /// <summary>
        /// 전체 물건 목록 (매칭용)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Property> _availableProperties = new();

        /// <summary>
        /// 매칭된 PDF 파일 목록 (OcrPdfFileWithMatch)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<OcrPdfFileWithMatch> _ocrPdfFilesWithMatch = new();

        /// <summary>
        /// 매칭용 PDF 파일 존재 여부
        /// </summary>
        [ObservableProperty]
        private bool _hasOcrPdfFilesWithMatch;

        /// <summary>
        /// 단일 물건 모드 여부 (SetPropertyId로 설정된 경우)
        /// </summary>
        public bool IsSinglePropertyMode => _propertyId.HasValue;

        /// <summary>
        /// 파일이 있는지 여부 (단일 모드 또는 매칭 모드)
        /// </summary>
        public bool HasAnyOcrPdfFiles => HasOcrPdfFiles || HasOcrPdfFilesWithMatch;

        // ========== 저장된 정제 결과(run/basic_info/gapgu/eulgu) ==========

        /// <summary>
        /// (프로그램 레벨) 저장된 결과 확인용 선택 물건
        /// </summary>
        [ObservableProperty]
        private Property? _selectedResultProperty;

        [ObservableProperty]
        private ObservableCollection<RegistryRun> _registryRuns = new();

        [ObservableProperty]
        private RegistryRun? _selectedRegistryRun;

        [ObservableProperty]
        private ObservableCollection<RegistryBasicInfo> _registryBasicInfoList = new();

        [ObservableProperty]
        private ObservableCollection<RegistryGapguRow> _registryGapguRows = new();

        [ObservableProperty]
        private ObservableCollection<RegistryEulguRow> _registryEulguRows = new();

        [ObservableProperty]
        private bool _hasSavedRegistryRuns;

        [ObservableProperty]
        private bool _hasNoSavedRegistryRuns = true;

        [ObservableProperty]
        private int _savedBasicInfoCount;

        [ObservableProperty]
        private int _savedGapguRowCount;

        [ObservableProperty]
        private int _savedEulguRowCount;

        #endregion

        public RegistryTabViewModel(RegistryRepository registryRepository, RegistryOcrService? ocrService = null, PropertyRepository? propertyRepository = null)
        {
            _registryRepository = registryRepository ?? throw new ArgumentNullException(nameof(registryRepository));
            _ocrService = ocrService;
            _propertyRepository = propertyRepository;
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

            // 프로그램 ID 저장 (물건 매칭용)
            if (property.ProgramId.HasValue)
            {
                _programId = property.ProgramId;
            }
        }

        /// <summary>
        /// 프로그램 ID 설정 (물건 목록 로드용)
        /// </summary>
        public void SetProgramId(Guid programId)
        {
            _programId = programId;
        }

        /// <summary>
        /// 물건 목록 로드 (프로그램 ID로)
        /// </summary>
        public async Task LoadAvailablePropertiesAsync()
        {
            if (_propertyRepository == null || !_programId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine("[RegistryTabViewModel] LoadAvailablePropertiesAsync: PropertyRepository or ProgramId is null");
                return;
            }

            try
            {
                var properties = await _propertyRepository.GetByProgramIdAsync(_programId.Value);
                // PropertyNumber 기준 자연 정렬 (R-0001_1, R-0002_1, ... R-0010_1 순서)
                var sortedProperties = properties
                    .OrderBy(p => ExtractPropertySortKey(p.PropertyNumber).major)
                    .ThenBy(p => ExtractPropertySortKey(p.PropertyNumber).minor)
                    .ToList();
                AvailableProperties = new ObservableCollection<Property>(sortedProperties);
                System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] Loaded {properties.Count} properties for matching (natural sorted by PropertyNumber)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] LoadAvailablePropertiesAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// PropertyNumber에서 정렬용 키 추출 (자연 정렬용)
        /// </summary>
        private static (int major, int minor) ExtractPropertySortKey(string? propertyNumber)
        {
            if (string.IsNullOrEmpty(propertyNumber))
                return (int.MaxValue, int.MaxValue);

            // 패턴: R-XXX_Y 또는 R-XXXX_Y (숫자 부분 추출)
            var match = Regex.Match(propertyNumber, @"R-?(\d+)[_-](\d+)");
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out int major);
                int.TryParse(match.Groups[2].Value, out int minor);
                return (major, minor);
            }

            // 패턴이 맞지 않으면 문자열 전체를 기준으로
            return (int.MaxValue, int.MaxValue);
        }

        /// <summary>
        /// 파일명에서 물건번호 추출 (예: "R-001-01", "R-0001-01", "R-001_01", "R001-01")
        /// </summary>
        private string? ExtractPropertyNumberFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            // 다양한 물건번호 패턴 매칭 (3~4자리 숫자 지원)
            // 패턴: R-XXX-XX, R-XXXX-XX, R-XXX_XX, R_XXX_XX, RXXX-XX, RXXXX_XX 등
            var patterns = new[]
            {
                @"R[-_]?(\d{3,4})[-_](\d{1,2})",  // R-001-01, R-0001-01, R_001_01, R001-01, R0001_1
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // DB 형식으로 변환: "R-001-01" → "R-0001_1" (4자리 + underscore + 숫자)
                    var num1 = match.Groups[1].Value;  // "001" or "0001"
                    var num2 = match.Groups[2].Value.TrimStart('0');  // "01" → "1"
                    if (string.IsNullOrEmpty(num2)) num2 = "1";  // "00" 케이스 처리

                    // 첫 번째 숫자를 4자리로 패딩 (001 → 0001)
                    if (int.TryParse(num1, out int majorNum))
                    {
                        num1 = majorNum.ToString("D4");  // 4자리로 패딩
                    }

                    var propertyNumber = $"R-{num1}_{num2}";  // "R-0001_1" 형식
                    System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] Extracted PropertyNumber from '{fileName}': {propertyNumber}");
                    return propertyNumber;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] No PropertyNumber pattern found in '{fileName}'");
            return null;
        }

        /// <summary>
        /// 주소 매칭 (물건번호 우선, 주소 기반 fallback)
        /// </summary>
        private (Property? property, double confidence) FindMatchingProperty(string? extractedAddress, string? fileName = null)
        {
            // 1. 파일명에서 물건번호로 매칭 시도 (100% 신뢰도)
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var propertyNumber = ExtractPropertyNumberFromFileName(fileName);
                if (!string.IsNullOrEmpty(propertyNumber))
                {
                    // 1차: 정확히 일치 (R-001_1)
                    var matchedByNumber = AvailableProperties.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(p.PropertyNumber) &&
                        p.PropertyNumber.Equals(propertyNumber, StringComparison.OrdinalIgnoreCase));

                    if (matchedByNumber != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] Matched by PropertyNumber: {propertyNumber} -> {matchedByNumber.DisplayAddress}");
                        return (matchedByNumber, 1.0);
                    }

                    // 2차: 숫자 부분만 비교하여 매칭 (형식 차이 무시)
                    var extractedKey = ExtractPropertySortKey(propertyNumber);
                    if (extractedKey.major != int.MaxValue)
                    {
                        matchedByNumber = AvailableProperties.FirstOrDefault(p =>
                        {
                            if (string.IsNullOrEmpty(p.PropertyNumber)) return false;
                            var dbKey = ExtractPropertySortKey(p.PropertyNumber);
                            return dbKey.major == extractedKey.major && dbKey.minor == extractedKey.minor;
                        });

                        if (matchedByNumber != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] Matched by numeric key ({extractedKey.major}, {extractedKey.minor}): {matchedByNumber.PropertyNumber} -> {matchedByNumber.DisplayAddress}");
                            return (matchedByNumber, 1.0);
                        }
                    }

                    // 3차: 밑줄을 대시로 바꾼 형식도 확인 (R-0001_1 → R-0001-1)
                    var alternateFormat1 = propertyNumber.Replace('_', '-');
                    matchedByNumber = AvailableProperties.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(p.PropertyNumber) &&
                        p.PropertyNumber.Equals(alternateFormat1, StringComparison.OrdinalIgnoreCase));

                    if (matchedByNumber != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] Matched by alternate format (dash): {alternateFormat1} -> {matchedByNumber.DisplayAddress}");
                        return (matchedByNumber, 1.0);
                    }

                    System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] No property found with PropertyNumber: {propertyNumber} (tried numeric key and alternate formats)");
                }
            }

            // 2. 주소 기반 매칭 (기존 로직)
            if (string.IsNullOrWhiteSpace(extractedAddress) || AvailableProperties.Count == 0)
                return (null, 0);

            var normalizedExtracted = NormalizeAddress(extractedAddress);

            // 2-1. 정확히 일치하는 경우
            foreach (var property in AvailableProperties)
            {
                var propAddress = property.AddressFull ?? property.AddressJibun ?? property.AddressRoad ?? "";
                if (!string.IsNullOrEmpty(propAddress))
                {
                    var normalizedProp = NormalizeAddress(propAddress);
                    if (normalizedProp == normalizedExtracted ||
                        normalizedProp.Contains(normalizedExtracted) ||
                        normalizedExtracted.Contains(normalizedProp))
                    {
                        return (property, 1.0);
                    }
                }
            }

            // 2-2. 부분 일치 (핵심 키워드 비교)
            var keywords = ExtractAddressKeywords(extractedAddress);
            Property? bestMatch = null;
            double bestScore = 0;

            foreach (var property in AvailableProperties)
            {
                var propAddress = property.AddressFull ?? property.AddressJibun ?? property.AddressRoad ?? "";
                if (string.IsNullOrEmpty(propAddress)) continue;

                var propKeywords = ExtractAddressKeywords(propAddress);
                var matchScore = CalculateMatchScore(keywords, propKeywords);

                if (matchScore > bestScore)
                {
                    bestScore = matchScore;
                    bestMatch = property;
                }
            }

            // 70% 이상 일치하면 매칭
            if (bestScore >= 0.7 && bestMatch != null)
            {
                return (bestMatch, bestScore);
            }

            return (null, 0);
        }

        /// <summary>
        /// 주소 정규화 (공백, 특수문자 제거)
        /// </summary>
        private static string NormalizeAddress(string address)
        {
            return Regex.Replace(address.Trim(), @"\s+", " ").ToLower();
        }

        /// <summary>
        /// 주소에서 핵심 키워드 추출 (시/구/동/번지)
        /// </summary>
        private static List<string> ExtractAddressKeywords(string address)
        {
            var keywords = new List<string>();

            // 공백으로 분리
            var parts = address.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 숫자 포함된 부분 (번지) 추가
                if (Regex.IsMatch(trimmed, @"\d"))
                {
                    keywords.Add(trimmed.ToLower());
                }
                // 동/읍/면/리/로/길 포함된 부분 추가
                else if (trimmed.EndsWith("동") || trimmed.EndsWith("읍") ||
                         trimmed.EndsWith("면") || trimmed.EndsWith("리") ||
                         trimmed.EndsWith("로") || trimmed.EndsWith("길") ||
                         trimmed.EndsWith("구") || trimmed.EndsWith("시"))
                {
                    keywords.Add(trimmed.ToLower());
                }
            }

            return keywords;
        }

        /// <summary>
        /// 키워드 매칭 점수 계산 (Jaccard 유사도)
        /// </summary>
        private static double CalculateMatchScore(List<string> keywords1, List<string> keywords2)
        {
            if (keywords1.Count == 0 || keywords2.Count == 0)
                return 0;

            var set1 = new HashSet<string>(keywords1);
            var set2 = new HashSet<string>(keywords2);

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return union > 0 ? (double)intersection / union : 0;
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        public async Task LoadDataAsync()
        {
            // 단일 물건 모드일 때만 자동 로드 (프로그램 레벨에서는 사용자가 물건 선택)
            if (_propertyId == null) return;
            await LoadRegistryRunsForPropertyAsync(_propertyId.Value);
        }

        partial void OnSelectedResultPropertyChanged(Property? value)
        {
            if (_suppressSelectedResultPropertyChanged) return;

            if (value == null)
            {
                ClearSavedRegistryData();
                return;
            }

            _ = LoadRegistryRunsForPropertyAsync(value.Id);
        }

        partial void OnSelectedRegistryRunChanged(RegistryRun? value)
        {
            if (_suppressSelectedRegistryRunChanged) return;
            _ = LoadSelectedRegistryRunAsync(value);
        }

        private async Task LoadRegistryRunsForPropertyAsync(Guid propertyId, Guid? selectRunId = null)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var runs = await _registryRepository.GetRunsByPropertyIdAsync(propertyId);
                RegistryRuns = new ObservableCollection<RegistryRun>(runs);

                HasSavedRegistryRuns = runs.Count > 0;
                HasNoSavedRegistryRuns = runs.Count == 0;

                _suppressSelectedRegistryRunChanged = true;
                SelectedRegistryRun = selectRunId.HasValue
                    ? runs.FirstOrDefault(r => r.Id == selectRunId.Value)
                    : runs.FirstOrDefault();
                _suppressSelectedRegistryRunChanged = false;

                // property 기준으로 모든 run의 데이터를 합산 조회
                await LoadAllDataForPropertyAsync(propertyId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"등기부 세트 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _suppressSelectedRegistryRunChanged = false;
            }
        }

        /// <summary>
        /// 물건 ID 기준으로 모든 run의 basic_info/gapgu/eulgu를 합산 로드
        /// </summary>
        private async Task LoadAllDataForPropertyAsync(Guid propertyId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                RegistryBasicInfoList = new ObservableCollection<RegistryBasicInfo>(
                    await _registryRepository.GetBasicInfoListByPropertyIdAsync(propertyId));
                RegistryGapguRows = new ObservableCollection<RegistryGapguRow>(
                    await _registryRepository.GetGapguRowsByPropertyIdAsync(propertyId));
                RegistryEulguRows = new ObservableCollection<RegistryEulguRow>(
                    await _registryRepository.GetEulguRowsByPropertyIdAsync(propertyId));

                SavedBasicInfoCount = RegistryBasicInfoList.Count;
                SavedGapguRowCount = RegistryGapguRows.Count;
                SavedEulguRowCount = RegistryEulguRows.Count;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"등기부 데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSelectedRegistryRunAsync(RegistryRun? run)
        {
            if (run == null)
            {
                ClearSavedRegistryData();
                return;
            }
            // run 변경 시에도 property 기준으로 전체 로드 (여러 run 합산)
            await LoadAllDataForPropertyAsync(run.PropertyId);
        }

        private void ClearSavedRegistryData()
        {
            RegistryRuns = new ObservableCollection<RegistryRun>();
            SelectedRegistryRun = null;
            RegistryBasicInfoList = new ObservableCollection<RegistryBasicInfo>();
            RegistryGapguRows = new ObservableCollection<RegistryGapguRow>();
            RegistryEulguRows = new ObservableCollection<RegistryEulguRow>();
            HasSavedRegistryRuns = false;
            HasNoSavedRegistryRuns = true;
            SavedBasicInfoCount = 0;
            SavedGapguRowCount = 0;
            SavedEulguRowCount = 0;
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
                    Section = "갑구",
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
                    Section = "을구",
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

        #region 정제 결과(user input) 저장 Command

        [RelayCommand]
        private async Task SaveRegistryUserInputsAsync()
        {
            if (SelectedRegistryRun == null)
            {
                ErrorMessage = "저장할 등기부 세트를 선택해 주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                foreach (var row in RegistryGapguRows)
                {
                    await _registryRepository.UpdateGapguRowAsync(row);
                }

                foreach (var row in RegistryEulguRows)
                {
                    await _registryRepository.UpdateEulguRowAsync(row);
                }

                SuccessMessage = "등기부 사용자 입력이 저장되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사용자 입력 저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region OCR 관련 Commands

        /// <summary>
        /// OCR 서버 상태 확인
        /// </summary>
        public async Task CheckOcrServerStatusAsync()
        {
            // 2026-02: OCR은 Supabase Edge Function(ocr-registry-save)을 통해 수행/저장한다.
            // 따라서 로컬/원격 Python 서버 헬스체크로 UI를 막지 않고, 처리 시점에 Edge Function 호출로 검증한다.
            IsOcrServerReady = true;
            OcrServerStatus = "Edge Function 준비됨 ✓";
            await Task.CompletedTask;
        }

        /// <summary>
        /// PDF 파일 선택
        /// </summary>
        [RelayCommand]
        private async Task SelectOcrPdfFilesAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "등기부등본 PDF 선택",
                Filter = "PDF 파일|*.pdf",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                // 물건 목록 로드 (아직 로드되지 않은 경우)
                if (AvailableProperties.Count == 0 && _programId.HasValue)
                {
                    await LoadAvailablePropertiesAsync();
                }

                // 물건 목록 로드 후 로그 추가
                System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] AvailableProperties count: {AvailableProperties.Count}");
                if (AvailableProperties.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] First property: {AvailableProperties[0].PropertyNumber}");
                }

                foreach (var filePath in dialog.FileNames)
                {
                    // 중복 체크 (기존 목록) - 완료/실패 파일은 재처리 대상으로 리셋
                    var existingFile = OcrPdfFiles.FirstOrDefault(f => f.FilePath == filePath);
                    if (existingFile != null)
                    {
                        if (existingFile.Status == "완료" || existingFile.Status == "실패")
                        {
                            existingFile.Status = "대기";
                            existingFile.Progress = 0;
                            existingFile.ErrorMessage = null;
                            System.Diagnostics.Debug.WriteLine($"[SelectPdf] 기존 파일 리셋: {existingFile.FileName} → 대기");
                        }
                        continue;
                    }

                    // 중복 체크 (매칭 목록) - 완료/실패 파일은 재처리 대상으로 리셋 + 자동매칭 재실행
                    var existingMatchFile = OcrPdfFilesWithMatch.FirstOrDefault(f => f.FilePath == filePath);
                    if (existingMatchFile != null)
                    {
                        if (existingMatchFile.Status == "완료" || existingMatchFile.Status == "실패")
                        {
                            existingMatchFile.Status = "대기";
                            existingMatchFile.Progress = 0;
                            existingMatchFile.ErrorMessage = null;

                            // 자동 매칭 재실행
                            var (reMatchedProperty, reConfidence) = FindMatchingProperty(null, existingMatchFile.FileName);
                            if (reMatchedProperty != null)
                            {
                                existingMatchFile.MatchedProperty = reMatchedProperty;
                                existingMatchFile.MatchConfidence = reConfidence;
                                existingMatchFile.IsAutoMatched = true;
                            }
                            System.Diagnostics.Debug.WriteLine($"[SelectPdf] 기존 매칭 파일 리셋: {existingMatchFile.FileName} → 대기, 매칭={reMatchedProperty?.PropertyNumber ?? "없음"}");
                        }
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);

                    // 단일 물건 모드가 아니면 OcrPdfFileWithMatch 사용
                    if (!IsSinglePropertyMode && _programId.HasValue)
                    {
                        var matchFile = new OcrPdfFileWithMatch
                        {
                            FilePath = filePath,
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            Status = "대기"
                        };

                        // 파일명 기반 사전 자동매칭 (OCR 전)
                        var (matchedProperty, confidence) = FindMatchingProperty(null, matchFile.FileName);
                        if (matchedProperty != null)
                        {
                            matchFile.MatchedProperty = matchedProperty;
                            matchFile.MatchConfidence = confidence;
                            matchFile.IsAutoMatched = true;
                        }

                        OcrPdfFilesWithMatch.Add(matchFile);
                    }
                    else
                    {
                        // 기존 단일 물건 모드
                        OcrPdfFiles.Add(new OcrPdfFile
                        {
                            FilePath = filePath,
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            Status = "대기"
                        });
                    }
                }

                HasOcrPdfFiles = OcrPdfFiles.Count > 0;
                HasOcrPdfFilesWithMatch = OcrPdfFilesWithMatch.Count > 0;
                OnPropertyChanged(nameof(HasAnyOcrPdfFiles));
            }
        }

        /// <summary>
        /// PDF 파일 제거
        /// </summary>
        [RelayCommand]
        private void RemoveOcrPdfFile(OcrPdfFile? file)
        {
            if (file == null) return;

            // OcrPdfFileWithMatch인 경우 해당 목록에서도 제거
            if (file is OcrPdfFileWithMatch matchFile)
            {
                OcrPdfFilesWithMatch.Remove(matchFile);
                HasOcrPdfFilesWithMatch = OcrPdfFilesWithMatch.Count > 0;
            }

            OcrPdfFiles.Remove(file);
            HasOcrPdfFiles = OcrPdfFiles.Count > 0;
            OnPropertyChanged(nameof(HasAnyOcrPdfFiles));
        }

        /// <summary>
        /// 모든 PDF 파일 취소
        /// </summary>
        [RelayCommand]
        private void CancelAllOcrPdfFiles()
        {
            OcrPdfFiles.Clear();
            OcrPdfFilesWithMatch.Clear();
            HasOcrPdfFiles = false;
            HasOcrPdfFilesWithMatch = false;
            OnPropertyChanged(nameof(HasAnyOcrPdfFiles));
            HasOcrResults = false;
            OcrPreviewOwners.Clear();
            OcrPreviewGapgu.Clear();
            OcrPreviewEulgu.Clear();
            OcrExtractedAddress = null;
            SummaryImages.Clear();
            HasSummaryImage = false;
            OnPropertyChanged(nameof(SummaryImageCount));
        }

        /// <summary>
        /// OCR 처리 시작
        /// </summary>
        [RelayCommand]
        private async Task StartOcrProcessingAsync()
        {
            // 매칭 모드 또는 단일 모드 확인
            var useMatchMode = HasOcrPdfFilesWithMatch && OcrPdfFilesWithMatch.Count > 0;
            var filesToProcess = useMatchMode
                ? OcrPdfFilesWithMatch.Where(f => f.Status != "완료").Cast<OcrPdfFile>().ToList()
                : OcrPdfFiles.Where(f => f.Status != "완료").ToList();

            if (filesToProcess.Count == 0)
            {
                SuccessMessage = "처리할 파일이 없습니다. (모든 파일이 완료 상태입니다.)";
                return;
            }

            // 매칭 모드에서는 property_id가 반드시 필요 (Edge Function 저장)
            if (useMatchMode)
            {
                var notMatched = OcrPdfFilesWithMatch
                    .Where(f => f.Status != "완료" && f.MatchedProperty == null)
                    .ToList();
                if (notMatched.Count > 0)
                {
                    var names = string.Join(", ", notMatched.Select(f => f.FileName));
                    ErrorMessage = $"매칭되지 않은 파일이 있습니다: {names}\n물건을 수동으로 선택해 주세요.";
                    return;
                }
            }
            else
            {
                if (_propertyId == null)
                {
                    ErrorMessage = "물건 ID가 설정되지 않았습니다.";
                    return;
                }
            }

            try
            {
                IsOcrProcessing = true;
                ErrorMessage = null;
                SuccessMessage = null;
                System.Diagnostics.Debug.WriteLine($"[OCR] StartOcrProcessingAsync 시작 - useMatchMode={useMatchMode}, filesToProcess={filesToProcess.Count}");

                _ocrCancellationTokenSource = new CancellationTokenSource();
                var token = _ocrCancellationTokenSource.Token;

                OcrTotalCount = filesToProcess.Count;
                OcrCompletedCount = 0;
                OcrProgressPercent = 0;

                // 결과 초기화
                OcrPreviewOwners.Clear();
                OcrPreviewGapgu.Clear();
                OcrPreviewEulgu.Clear();

                foreach (var pdfFile in filesToProcess)
                {
                    if (token.IsCancellationRequested)
                        break;

                    pdfFile.Status = "처리 중";
                    pdfFile.Progress = 0;
                    OcrStatusMessage = $"처리 중: {pdfFile.FileName}";

                    try
                    {
                        pdfFile.Progress = 10;

                        var propertyId = useMatchMode
                            ? ((OcrPdfFileWithMatch)pdfFile).MatchedProperty!.Id
                            : _propertyId!.Value;

                        System.Diagnostics.Debug.WriteLine($"[OCR] Edge Function 호출 시작 - propertyId={propertyId}, file={pdfFile.FileName}");

                        var (run, biSaved, gapSaved, eulSaved, _, registryAddress, summaryImages) =
                            await _registryRepository.OcrRegistrySaveViaEdgeFunctionAsync(
                                propertyId,
                                pdfFile.FilePath,
                                includeSummaryImages: true,
                                summaryImageMaxPages: 3,
                                cancellationToken: token);

                        pdfFile.Progress = 80;

                        // 요약 페이지 이미지 표시 (응답에 포함된 것만, 최대 N페이지)
                        SummaryImages.Clear();
                        if (summaryImages != null && summaryImages.Count > 0)
                        {
                            foreach (var base64Image in summaryImages)
                            {
                                var bitmap = ConvertBase64ToBitmapImage(base64Image);
                                if (bitmap != null)
                                {
                                    SummaryImages.Add(bitmap);
                                }
                            }
                            HasSummaryImage = SummaryImages.Count > 0;
                            OnPropertyChanged(nameof(SummaryImageCount));
                        }
                        else
                        {
                            HasSummaryImage = false;
                            OnPropertyChanged(nameof(SummaryImageCount));
                        }

                        // 주소 저장 (있는 경우)
                        if (!string.IsNullOrWhiteSpace(registryAddress))
                        {
                            OcrExtractedAddress = registryAddress;
                        }

                        // 매칭 모드: 추출 주소 업데이트 (표시용)
                        if (pdfFile is OcrPdfFileWithMatch matchFile)
                        {
                            matchFile.ExtractedAddress = registryAddress;
                            matchFile.OcrResultData = null;
                        }

                        // 저장된 정제 결과 섹션 자동 갱신 (마지막 처리된 물건 기준)
                        if (!useMatchMode)
                        {
                            await LoadRegistryRunsForPropertyAsync(propertyId, run.Id);
                        }
                        else
                        {
                            var prop = ((OcrPdfFileWithMatch)pdfFile).MatchedProperty!;
                            _suppressSelectedResultPropertyChanged = true;
                            SelectedResultProperty = prop;
                            _suppressSelectedResultPropertyChanged = false;
                            await LoadRegistryRunsForPropertyAsync(prop.Id, run.Id);
                        }

                        // 저장 카운트(표시용)
                        SavedBasicInfoCount = biSaved;
                        SavedGapguRowCount = gapSaved;
                        SavedEulguRowCount = eulSaved;

                        pdfFile.Status = "완료";
                        pdfFile.Progress = 100;
                    }
                    catch (TaskCanceledException)
                    {
                        pdfFile.Status = "취소됨";
                        System.Diagnostics.Debug.WriteLine($"[OCR] 취소됨: {pdfFile.FileName}");
                    }
                    catch (Exception ex)
                    {
                        pdfFile.Status = "실패";
                        pdfFile.ErrorMessage = ex.Message;
                        System.Diagnostics.Debug.WriteLine($"[OCR] 실패: {pdfFile.FileName} - {ex.Message}");
                        if (ex.InnerException != null)
                            System.Diagnostics.Debug.WriteLine($"[OCR]   └─ Inner: {ex.InnerException.Message}");
                    }

                    OcrCompletedCount++;
                    OcrProgressPercent = (OcrCompletedCount * 100) / OcrTotalCount;
                }

                // 결과 메시지 생성
                var successCount = filesToProcess.Count(f => f.Status == "완료");
                var failCount = filesToProcess.Count(f => f.Status == "실패");

                if (useMatchMode)
                {
                    var matchedCount = OcrPdfFilesWithMatch.Count(f => f.MatchedProperty != null);
                    var unmatchedCount = successCount - matchedCount;
                    OcrStatusMessage = $"완료: {successCount}개 성공, {matchedCount}개 매칭, {unmatchedCount}개 미매칭";
                    HasOcrResults = successCount > 0;

                    if (successCount > 0)
                    {
                        SuccessMessage = $"OCR+저장 완료! {successCount}개 파일 처리됨";
                    }
                }
                else
                {
                    HasOcrResults = successCount > 0;
                    OcrStatusMessage = $"완료: {successCount}개 성공, {failCount}개 실패";

                    if (successCount > 0)
                    {
                        SuccessMessage = $"OCR+저장 완료! {successCount}개 파일 처리됨";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"OCR 처리 중 오류: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[OCR] 전체 처리 오류: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"[OCR]   └─ Inner: {ex.InnerException.Message}");
            }
            finally
            {
                IsOcrProcessing = false;
                _ocrCancellationTokenSource?.Dispose();
                _ocrCancellationTokenSource = null;
                _suppressSelectedResultPropertyChanged = false;
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
                        Section = "갑구",
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
                        Section = "을구",
                        RightOrder = ParseInt(GetStringValue(eulDict, "순위번호")),
                        RegistrationCause = GetStringValue(eulDict, "등기목적"),
                        RegistrationNumber = GetStringValue(eulDict, "접수정보"),
                        RegistrationDate = ParseDate(GetStringValue(eulDict, "접수날짜")),
                        RightHolder = GetStringValue(eulDict, "근저당권자/전세권자/채권자"),
                        ClaimAmount = ParseDecimal(GetStringValue(eulDict, "채권최고액/전세금")),
                        Notes = GetStringValue(eulDict, "채무자"),
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
            // 2026-02: OCR 처리 시작 시 Edge Function을 통해 "OCR+저장"까지 완료됨.
            // 여기서는 중복 저장을 방지하고, 저장된 결과 섹션을 새로고침만 수행한다.
            try
            {
                ErrorMessage = null;

                if (IsSinglePropertyMode && _propertyId.HasValue)
                {
                    await LoadRegistryRunsForPropertyAsync(_propertyId.Value);
                    SuccessMessage = "저장된 등기부 세트를 새로고침했습니다.";
                    return;
                }

                if (!IsSinglePropertyMode && SelectedResultProperty != null)
                {
                    await LoadRegistryRunsForPropertyAsync(SelectedResultProperty.Id);
                    SuccessMessage = "저장된 등기부 세트를 새로고침했습니다.";
                    return;
                }

                ErrorMessage = "새로고침할 물건을 선택해 주세요.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"새로고침 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 매칭 모드에서 OCR 결과를 DB에 저장 (각 PDF별 매칭된 물건에 저장)
        /// </summary>
        private async Task SaveOcrResultsWithMatchAsync()
        {
            var completedFiles = OcrPdfFilesWithMatch.Where(f => f.Status == "완료").ToList();

            if (completedFiles.Count == 0)
            {
                ErrorMessage = "저장할 OCR 결과가 없습니다.";
                return;
            }

            // 매칭되지 않은 파일 확인
            var unmatchedFiles = completedFiles.Where(f => f.MatchedProperty == null).ToList();
            if (unmatchedFiles.Count > 0)
            {
                var unmatchedNames = string.Join(", ", unmatchedFiles.Select(f => f.FileName));
                ErrorMessage = $"매칭되지 않은 파일이 있습니다: {unmatchedNames}\n물건을 수동으로 선택해 주세요.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                int savedFileCount = 0;
                int savedRuns = 0;
                int savedBasicInfo = 0;
                int savedGapgu = 0;
                int savedEulgu = 0;

                RegistryRun? lastRun = null;
                Property? lastProperty = null;

                foreach (var pdfFile in completedFiles)
                {
                    var prop = pdfFile.MatchedProperty!;

                    var (run, bi, gap, eul, _, _, _) =
                        await _registryRepository.OcrRegistrySaveViaEdgeFunctionAsync(prop.Id, pdfFile.FilePath);

                    savedFileCount++;
                    savedRuns++;
                    savedBasicInfo += bi;
                    savedGapgu += gap;
                    savedEulgu += eul;

                    lastRun = run;
                    lastProperty = prop;
                }

                SuccessMessage =
                    $"저장 완료: {savedFileCount}개 PDF → 세트 {savedRuns}개 생성 (basic_info {savedBasicInfo}행, 갑구 {savedGapgu}행, 을구 {savedEulgu}행)";

                // 저장된 결과 섹션 자동 갱신 (마지막 저장된 물건 기준)
                if (lastProperty != null && lastRun != null)
                {
                    _suppressSelectedResultPropertyChanged = true;
                    SelectedResultProperty = lastProperty;
                    _suppressSelectedResultPropertyChanged = false;

                    await LoadRegistryRunsForPropertyAsync(lastProperty.Id, lastRun.Id);
                }

                CancelAllOcrPdfFiles();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                _suppressSelectedResultPropertyChanged = false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Base64 문자열을 BitmapImage로 변환
        /// </summary>
        private static BitmapImage? ConvertBase64ToBitmapImage(string base64String)
        {
            try
            {
                // data:image/png;base64, 형식에서 Base64 부분만 추출
                var base64Data = base64String;
                if (base64String.Contains(","))
                {
                    base64Data = base64String.Substring(base64String.IndexOf(",") + 1);
                }

                var imageBytes = Convert.FromBase64String(base64Data);

                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // UI 스레드에서 사용 가능하도록
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegistryTabViewModel] Base64 to BitmapImage 변환 실패: {ex.Message}");
                return null;
            }
        }

        private static string? GetStringValue(Dictionary<string, object?>? dict, string key)
        {
            if (dict == null || !dict.TryGetValue(key, out var value))
                return null;
            
            return value?.ToString();
        }

        private static string? GetFirstStringValue(Dictionary<string, object?>? dict, params string[] keys)
        {
            if (dict == null || keys == null || keys.Length == 0) return null;
            foreach (var key in keys)
            {
                var v = GetStringValue(dict, key);
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return null;
        }

        private static bool IsEmptyRow(Dictionary<string, object?>? dict)
        {
            if (dict == null || dict.Count == 0) return true;
            foreach (var kv in dict)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value?.ToString()))
                    return false;
            }
            return true;
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

