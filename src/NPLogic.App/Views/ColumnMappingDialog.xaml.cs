using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using NPLogic.Core.Models;
using NPLogic.Services;

namespace NPLogic.Views
{
    /// <summary>
    /// 컬럼 매핑 다이얼로그
    /// Excel 컬럼과 DB 컬럼을 매핑합니다.
    /// </summary>
    public partial class ColumnMappingDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly DataDiskUploadService? _uploadService;
        private DataDiskSheetType _sheetType;
        private string _excelSheetName = "";

        /// <summary>
        /// 시트 타입 표시명
        /// </summary>
        public string SheetTypeDisplay => _sheetType switch
        {
            DataDiskSheetType.BorrowerGeneral => "차주일반정보",
            DataDiskSheetType.BorrowerRestructuring => "회생차주정보",
            DataDiskSheetType.Loan => "채권정보",
            DataDiskSheetType.Property => "담보물건정보",
            DataDiskSheetType.RegistryDetail => "등기부등본정보",
            DataDiskSheetType.CollateralSetting => "담보설정정보",
            DataDiskSheetType.Guarantee => "보증정보",
            DataDiskSheetType.InterimAdvance => "가지급금",
            DataDiskSheetType.InterimCollection => "회수정보",
            _ => "알 수 없음"
        };

        /// <summary>
        /// Excel 시트명
        /// </summary>
        public string ExcelSheetName
        {
            get => _excelSheetName;
            set { _excelSheetName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 컬럼 매핑 목록
        /// </summary>
        public ObservableCollection<EditableColumnMapping> ColumnMappings { get; } = new();

        /// <summary>
        /// 사용 가능한 DB 컬럼 목록 (드롭다운용)
        /// </summary>
        public ObservableCollection<DbColumnOption> AvailableDbColumns { get; } = new();

        /// <summary>
        /// 매핑된 컬럼 수
        /// </summary>
        public int MappedCount => ColumnMappings.Count(m => m.IsMapped);

        /// <summary>
        /// 전체 컬럼 수
        /// </summary>
        public int TotalColumns => ColumnMappings.Count;

        /// <summary>
        /// 결과 매핑 (확인 시)
        /// </summary>
        public List<ColumnMappingInfo> ResultMappings { get; private set; } = new();

        public ColumnMappingDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ColumnMappingDialog(DataDiskUploadService uploadService) : this()
        {
            _uploadService = uploadService;
        }

        /// <summary>
        /// 시트 정보 설정
        /// </summary>
        public void SetSheetInfo(SheetMappingInfo sheetInfo)
        {
            _sheetType = sheetInfo.SelectedType;
            ExcelSheetName = sheetInfo.ExcelSheetName;

            // 사용 가능한 DB 컬럼 목록 로드
            LoadAvailableDbColumns();

            // 기존 매핑이 있으면 사용, 없으면 기본 매핑 적용
            if (sheetInfo.ColumnMappings.Count > 0)
            {
                LoadExistingMappings(sheetInfo.Headers, sheetInfo.ColumnMappings);
            }
            else
            {
                LoadDefaultMappings(sheetInfo.Headers);
            }

            OnPropertyChanged(nameof(SheetTypeDisplay));
            UpdateCounts();
        }

        /// <summary>
        /// 사용 가능한 DB 컬럼 로드
        /// </summary>
        private void LoadAvailableDbColumns()
        {
            AvailableDbColumns.Clear();
            
            // 선택 안 함 옵션 추가
            AvailableDbColumns.Add(new DbColumnOption { DbColumn = "", DisplayName = "(선택안함)" });

            if (_uploadService != null)
            {
                var columns = _uploadService.GetAvailableDbColumns(_sheetType);
                foreach (var (dbColumn, displayName) in columns)
                {
                    AvailableDbColumns.Add(new DbColumnOption 
                    { 
                        DbColumn = dbColumn, 
                        DisplayName = displayName 
                    });
                }
            }
            else
            {
                // 서비스가 없으면 기본 컬럼 목록 사용
                var defaultColumns = GetDefaultDbColumns();
                foreach (var (dbColumn, displayName) in defaultColumns)
                {
                    AvailableDbColumns.Add(new DbColumnOption 
                    { 
                        DbColumn = dbColumn, 
                        DisplayName = displayName 
                    });
                }
            }
        }

        /// <summary>
        /// 기본 매핑 로드
        /// </summary>
        private void LoadDefaultMappings(List<string> headers)
        {
            ColumnMappings.Clear();

            if (_uploadService != null)
            {
                var defaultMappings = _uploadService.GetDefaultColumnMappings(_sheetType, headers);
                foreach (var mapping in defaultMappings)
                {
                    var editableMapping = new EditableColumnMapping
                    {
                        ExcelColumn = mapping.ExcelColumn,
                        DbColumn = mapping.DbColumn ?? "",
                        IsAutoMatched = mapping.IsAutoMatched,
                        IsRequired = mapping.IsRequired
                    };
                    editableMapping.PropertyChanged += Mapping_PropertyChanged;
                    ColumnMappings.Add(editableMapping);
                }
            }
            else
            {
                // 서비스가 없으면 빈 매핑
                foreach (var header in headers)
                {
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    var editableMapping = new EditableColumnMapping
                    {
                        ExcelColumn = header,
                        DbColumn = "",
                        IsAutoMatched = false,
                        IsRequired = false
                    };
                    editableMapping.PropertyChanged += Mapping_PropertyChanged;
                    ColumnMappings.Add(editableMapping);
                }
            }
        }

        /// <summary>
        /// 기존 매핑 로드
        /// </summary>
        private void LoadExistingMappings(List<string> headers, List<ColumnMappingInfo> existingMappings)
        {
            ColumnMappings.Clear();

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var existing = existingMappings.FirstOrDefault(m => m.ExcelColumn == header);
                var editableMapping = new EditableColumnMapping
                {
                    ExcelColumn = header,
                    DbColumn = existing?.DbColumn ?? "",
                    IsAutoMatched = existing?.IsAutoMatched ?? false,
                    IsRequired = existing?.IsRequired ?? false
                };
                editableMapping.PropertyChanged += Mapping_PropertyChanged;
                ColumnMappings.Add(editableMapping);
            }
        }

        private void Mapping_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditableColumnMapping.DbColumn))
            {
                UpdateCounts();
            }
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(MappedCount));
            OnPropertyChanged(nameof(TotalColumns));
        }

        /// <summary>
        /// 기본값 적용 버튼 클릭
        /// </summary>
        private void ApplyDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (_uploadService != null)
            {
                var headers = ColumnMappings.Select(m => m.ExcelColumn).ToList();
                var defaultMappings = _uploadService.GetDefaultColumnMappings(_sheetType, headers);

                foreach (var mapping in ColumnMappings)
                {
                    var defaultMapping = defaultMappings.FirstOrDefault(m => m.ExcelColumn == mapping.ExcelColumn);
                    if (defaultMapping != null)
                    {
                        mapping.DbColumn = defaultMapping.DbColumn ?? "";
                        mapping.IsAutoMatched = defaultMapping.IsAutoMatched;
                    }
                }
            }
            UpdateCounts();
        }

        /// <summary>
        /// 초기화 버튼 클릭
        /// </summary>
        private void ClearMappings_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mapping in ColumnMappings)
            {
                mapping.DbColumn = "";
                mapping.IsAutoMatched = false;
            }
            UpdateCounts();
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // 결과 매핑 생성
            ResultMappings = ColumnMappings.Select(m => new ColumnMappingInfo
            {
                ExcelColumn = m.ExcelColumn,
                DbColumn = m.DbColumn,
                IsAutoMatched = m.IsAutoMatched,
                IsRequired = m.IsRequired
            }).ToList();

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 기본 DB 컬럼 목록 (서비스 없을 때 fallback)
        /// </summary>
        private List<(string DbColumn, string DisplayName)> GetDefaultDbColumns()
        {
            return _sheetType switch
            {
                DataDiskSheetType.BorrowerGeneral => new List<(string, string)>
                {
                    ("borrower_number", "차주번호"),
                    ("borrower_name", "차주명"),
                    ("borrower_type", "차주형태"),
                    ("opb", "대출원금잔액"),
                    ("mortgage_amount", "근저당설정액")
                },
                DataDiskSheetType.Property => new List<(string, string)>
                {
                    ("borrower_number", "차주번호"),
                    ("borrower_name", "차주명"),
                    ("collateral_number", "담보번호"),
                    ("property_type", "물건유형"),
                    ("address_province", "담보소재지1"),
                    ("address_city", "담보소재지2"),
                    ("address_district", "담보소재지3"),
                    ("address_detail", "담보소재지4"),
                    ("land_area", "대지면적"),
                    ("building_area", "건물면적"),
                    ("appraisal_value", "감정평가액")
                },
                DataDiskSheetType.Loan => new List<(string, string)>
                {
                    ("borrower_number", "차주번호"),
                    ("account_serial", "대출일련번호"),
                    ("loan_type", "대출과목"),
                    ("account_number", "계좌번호"),
                    ("normal_interest_rate", "이자율"),
                    ("initial_loan_amount", "최초대출금액"),
                    ("loan_principal_balance", "대출금잔액")
                },
                _ => new List<(string, string)>()
            };
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 편집 가능한 컬럼 매핑
    /// </summary>
    public class EditableColumnMapping : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string ExcelColumn { get; set; } = "";
        
        private string _dbColumn = "";
        public string DbColumn
        {
            get => _dbColumn;
            set
            {
                if (_dbColumn != value)
                {
                    _dbColumn = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DbColumn)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMapped)));
                }
            }
        }

        private bool _isAutoMatched;
        public bool IsAutoMatched
        {
            get => _isAutoMatched;
            set
            {
                if (_isAutoMatched != value)
                {
                    _isAutoMatched = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAutoMatched)));
                }
            }
        }

        public bool IsRequired { get; set; }

        public bool IsMapped => !string.IsNullOrEmpty(DbColumn) && DbColumn != "(선택안함)";
    }

    /// <summary>
    /// DB 컬럼 옵션 (드롭다운용)
    /// </summary>
    public class DbColumnOption
    {
        public string DbColumn { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
