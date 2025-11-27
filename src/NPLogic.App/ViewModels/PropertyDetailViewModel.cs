using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using NPLogic.Services;
using NPLogic.ViewModels;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 첨부파일 모델
    /// </summary>
    public class AttachmentItem : ObservableObject
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string StoragePath { get; set; } = "";
        public DateTime CreatedAt { get; set; }

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
    /// QA 항목 모델
    /// </summary>
    public class QAItem : ObservableObject
    {
        public Guid Id { get; set; }
        public string Question { get; set; } = "";
        public string? Answer { get; set; }
        public bool IsAnswered => !string.IsNullOrWhiteSpace(Answer);
        public DateTime CreatedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
    }

    /// <summary>
    /// 물건 상세 ViewModel
    /// </summary>
    public partial class PropertyDetailViewModel : ObservableObject
    {
        private readonly PropertyRepository _propertyRepository;
        private readonly StorageService? _storageService;
        private readonly RegistryRepository? _registryRepository;
        private readonly RightAnalysisRepository? _rightAnalysisRepository;
        private readonly EvaluationRepository? _evaluationRepository;

        [ObservableProperty]
        private Property _property = new();

        [ObservableProperty]
        private Property _originalProperty = new();

        /// <summary>
        /// 등기부 탭 ViewModel
        /// </summary>
        [ObservableProperty]
        private RegistryTabViewModel? _registryViewModel;

        /// <summary>
        /// 권리분석 탭 ViewModel
        /// </summary>
        [ObservableProperty]
        private RightsAnalysisTabViewModel? _rightsAnalysisViewModel;

        /// <summary>
        /// 평가 탭 ViewModel
        /// </summary>
        [ObservableProperty]
        private EvaluationTabViewModel? _evaluationViewModel;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // 아파트 여부 (KB시세 표시 조건)
        [ObservableProperty]
        private bool _isApartment;

        // KB시세 정보
        [ObservableProperty]
        private decimal _kbPrice;

        [ObservableProperty]
        private decimal _kbJeonsePrice;

        [ObservableProperty]
        private decimal _kbPricePerPyeong;

        [ObservableProperty]
        private DateTime? _kbPriceDate;

        // 감정평가 상세 정보
        [ObservableProperty]
        private string? _appraisalType;

        [ObservableProperty]
        private string? _appraisalOrganization;

        [ObservableProperty]
        private DateTime? _appraisalDate;

        [ObservableProperty]
        private decimal _landAppraisalValue;

        [ObservableProperty]
        private decimal _buildingAppraisalValue;

        [ObservableProperty]
        private decimal _machineAppraisalValue;

        // 첨부파일 목록
        [ObservableProperty]
        private ObservableCollection<AttachmentItem> _attachments = new();

        [ObservableProperty]
        private bool _hasNoAttachments = true;

        // QA 목록
        [ObservableProperty]
        private ObservableCollection<QAItem> _qaList = new();

        [ObservableProperty]
        private bool _hasNoQA = true;

        private Guid? _propertyId;
        private Action? _goBackAction;

        public PropertyDetailViewModel(PropertyRepository propertyRepository, StorageService? storageService = null, RegistryRepository? registryRepository = null, RightAnalysisRepository? rightAnalysisRepository = null, EvaluationRepository? evaluationRepository = null)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _storageService = storageService;
            _registryRepository = registryRepository;
            _rightAnalysisRepository = rightAnalysisRepository;
            _evaluationRepository = evaluationRepository;

            // 등기부 탭 ViewModel 초기화
            if (_registryRepository != null)
            {
                RegistryViewModel = new RegistryTabViewModel(_registryRepository);
            }

            // 권리분석 탭 ViewModel 초기화
            if (_rightAnalysisRepository != null)
            {
                RightsAnalysisViewModel = new RightsAnalysisTabViewModel(_rightAnalysisRepository);
            }

            // 평가 탭 ViewModel 초기화
            if (_evaluationRepository != null)
            {
                EvaluationViewModel = new EvaluationTabViewModel(_evaluationRepository);
            }
        }

        /// <summary>
        /// 물건 ID로 초기화
        /// </summary>
        public void SetPropertyId(Guid propertyId, Action? goBackAction = null)
        {
            _propertyId = propertyId;
            _goBackAction = goBackAction;

            // 등기부 탭 ViewModel에 물건 ID 설정
            RegistryViewModel?.SetPropertyId(propertyId);

            // 권리분석 탭 ViewModel에 물건 ID 설정
            RightsAnalysisViewModel?.SetPropertyId(propertyId);

            // 평가 탭 ViewModel에 물건 ID 설정
            EvaluationViewModel?.SetPropertyId(propertyId);
        }

        /// <summary>
        /// Property 객체로 직접 로드
        /// </summary>
        public void LoadProperty(Property property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _propertyId = property.Id;
            Property = property;
            
            // 아파트 여부 확인
            IsApartment = property.PropertyType?.Contains("아파트") == true 
                       || property.PropertyType?.Contains("오피스텔") == true;

            // 원본 복사 (변경 감지용)
            CopyPropertyToOriginal(property);

            // 등기부 탭 ViewModel에 물건 정보 설정
            if (RegistryViewModel != null)
            {
                RegistryViewModel.SetPropertyId(property.Id);
                RegistryViewModel.SetPropertyInfo(property);
            }

            // 권리분석 탭 ViewModel에 물건 정보 설정
            if (RightsAnalysisViewModel != null)
            {
                RightsAnalysisViewModel.SetPropertyId(property.Id);
                RightsAnalysisViewModel.SetProperty(property);
            }

            // 평가 탭 ViewModel에 물건 정보 설정
            if (EvaluationViewModel != null)
            {
                EvaluationViewModel.SetPropertyId(property.Id);
                EvaluationViewModel.SetProperty(property);
            }

            HasUnsavedChanges = false;
        }

        private void CopyPropertyToOriginal(Property property)
        {
            OriginalProperty = new Property
            {
                Id = property.Id,
                ProjectId = property.ProjectId,
                PropertyNumber = property.PropertyNumber,
                PropertyType = property.PropertyType,
                AddressFull = property.AddressFull,
                AddressRoad = property.AddressRoad,
                AddressJibun = property.AddressJibun,
                AddressDetail = property.AddressDetail,
                LandArea = property.LandArea,
                BuildingArea = property.BuildingArea,
                Floors = property.Floors,
                CompletionDate = property.CompletionDate,
                AppraisalValue = property.AppraisalValue,
                MinimumBid = property.MinimumBid,
                SalePrice = property.SalePrice,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                Status = property.Status,
                AssignedTo = property.AssignedTo,
                CreatedBy = property.CreatedBy,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt
            };
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_propertyId == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var property = await _propertyRepository.GetByIdAsync(_propertyId.Value);
                if (property != null)
                {
                    Property = property;
                    IsApartment = property.PropertyType?.Contains("아파트") == true 
                               || property.PropertyType?.Contains("오피스텔") == true;
                    
                    CopyPropertyToOriginal(property);

                    // QA, 첨부파일 로드
                    await LoadAttachmentsAsync();
                    await LoadQAListAsync();
                }
                else
                {
                    ErrorMessage = "물건을 찾을 수 없습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"물건 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 첨부파일 관련

        private async Task LoadAttachmentsAsync()
        {
            // TODO: Supabase Storage에서 첨부파일 목록 조회
            // 현재는 빈 목록
            Attachments.Clear();
            HasNoAttachments = Attachments.Count == 0;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 파일 업로드 명령
        /// </summary>
        [RelayCommand]
        private async Task UploadFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "첨부파일 선택",
                Filter = "모든 파일|*.*|PDF 파일|*.pdf|이미지 파일|*.jpg;*.jpeg;*.png;*.gif|문서 파일|*.doc;*.docx;*.xls;*.xlsx",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    ErrorMessage = null;

                    if (_storageService == null)
                    {
                        ErrorMessage = "Storage 서비스가 초기화되지 않았습니다. appsettings.json을 확인해주세요.";
                        return;
                    }

                    var fileName = System.IO.Path.GetFileName(dialog.FileName);
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
                    var storagePath = $"properties/{_propertyId}/{fileName}";

                    var url = await _storageService.UploadFileAsync("attachments", storagePath, fileBytes);

                    if (!string.IsNullOrEmpty(url))
                    {
                        var attachment = new AttachmentItem
                        {
                            Id = Guid.NewGuid(),
                            FileName = fileName,
                            FileSize = fileBytes.Length,
                            StoragePath = storagePath,
                            CreatedAt = DateTime.Now
                        };
                        Attachments.Add(attachment);
                        HasNoAttachments = false;
                        SuccessMessage = "파일이 업로드되었습니다.";
                    }
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
        /// 파일 보기 명령
        /// </summary>
        [RelayCommand]
        private async Task ViewFileAsync(AttachmentItem? attachment)
        {
            if (attachment == null || _storageService == null) return;

            try
            {
                var url = await _storageService.GetPublicUrlAsync("attachments", attachment.StoragePath);
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"파일 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 파일 삭제 명령
        /// </summary>
        [RelayCommand]
        private async Task DeleteFileAsync(AttachmentItem? attachment)
        {
            if (attachment == null || _storageService == null) return;

            try
            {
                IsLoading = true;
                var success = await _storageService.DeleteFileAsync("attachments", attachment.StoragePath);
                if (success)
                {
                    Attachments.Remove(attachment);
                    HasNoAttachments = Attachments.Count == 0;
                    SuccessMessage = "파일이 삭제되었습니다.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"파일 삭제 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region QA 관련

        private async Task LoadQAListAsync()
        {
            // TODO: DB에서 QA 목록 조회
            // 현재는 빈 목록
            QaList.Clear();
            HasNoQA = QaList.Count == 0;
            await Task.CompletedTask;
        }

        /// <summary>
        /// QA 추가 명령
        /// </summary>
        [RelayCommand]
        private void AddQA()
        {
            // TODO: QA 입력 다이얼로그 표시
            // 현재는 테스트용 더미 QA 추가
            var newQA = new QAItem
            {
                Id = Guid.NewGuid(),
                Question = "새 질문입니다. (QA 입력 다이얼로그 구현 필요)",
                CreatedAt = DateTime.Now
            };
            QaList.Insert(0, newQA);
            HasNoQA = false;

            // QA 미회신 카운트 업데이트
            if (Property != null)
            {
                Property.QaUnansweredCount = QaList.Count(q => !q.IsAnswered);
            }
        }

        #endregion

        #region KB시세 관련

        /// <summary>
        /// KB시세 조회 명령
        /// </summary>
        [RelayCommand]
        private async Task FetchKBPriceAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // TODO: 실제 KB시세 API 연동 또는 국토교통부 실거래가 API 연동
                // 현재는 더미 데이터
                await Task.Delay(500); // 시뮬레이션

                // 더미 데이터 (실제로는 API 응답으로 대체)
                KbPrice = 450000000;
                KbJeonsePrice = 350000000;
                KbPricePerPyeong = 15000000;
                KbPriceDate = DateTime.Now;

                SuccessMessage = "KB시세가 조회되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"KB시세 조회 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// KB시세 사이트 열기
        /// </summary>
        [RelayCommand]
        private void OpenKBSite()
        {
            try
            {
                var url = "https://kbland.kr/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"사이트 열기 실패: {ex.Message}";
            }
        }

        /// <summary>
        /// 로드뷰 열기
        /// </summary>
        [RelayCommand]
        private void OpenRoadView()
        {
            if (Property?.Latitude != null && Property?.Longitude != null)
            {
                var url = $"https://map.kakao.com/?map_type=TYPE_MAP&sX={Property.Longitude}&sY={Property.Latitude}&sLevel=3";
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"로드뷰 열기 실패: {ex.Message}";
                }
            }
            else if (!string.IsNullOrWhiteSpace(Property?.AddressFull))
            {
                var encodedAddress = Uri.EscapeDataString(Property.AddressFull);
                var url = $"https://map.kakao.com/?q={encodedAddress}";
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"로드뷰 열기 실패: {ex.Message}";
                }
            }
        }

        #endregion

        /// <summary>
        /// 저장 명령
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                await _propertyRepository.UpdateAsync(Property);
                
                // 성공 메시지
                SuccessMessage = "저장되었습니다.";
                HasUnsavedChanges = false;

                // 원본 업데이트
                await InitializeAsync();
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

        /// <summary>
        /// 새로고침 명령
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (HasUnsavedChanges)
            {
                // TODO: 확인 다이얼로그 표시
                // 지금은 그냥 새로고침
            }

            await InitializeAsync();
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// 뒤로가기 명령
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            if (HasUnsavedChanges)
            {
                // TODO: 확인 다이얼로그 표시
                // 지금은 그냥 뒤로가기
            }

            _goBackAction?.Invoke();
        }

        /// <summary>
        /// Property 변경 시
        /// </summary>
        partial void OnPropertyChanged(Property value)
        {
            // 변경 감지 (간단 버전)
            HasUnsavedChanges = true;
            
            // 아파트 여부 업데이트
            IsApartment = value?.PropertyType?.Contains("아파트") == true 
                       || value?.PropertyType?.Contains("오피스텔") == true;
        }
    }
}
