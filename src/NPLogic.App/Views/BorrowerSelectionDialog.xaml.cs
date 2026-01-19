using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 차주 선택 다이얼로그 - 인쇄/엑셀용
    /// </summary>
    public partial class BorrowerSelectionDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _title = "차주 선택";
        private string _description = "선택한 차주의 데이터가 내보내집니다.";
        private string _actionButtonText = "확인";

        /// <summary>
        /// 다이얼로그 제목
        /// </summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 설명 텍스트
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 확인 버튼 텍스트
        /// </summary>
        public string ActionButtonText
        {
            get => _actionButtonText;
            set { _actionButtonText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 차주 목록
        /// </summary>
        public ObservableCollection<SelectableBorrower> Borrowers { get; } = new();

        /// <summary>
        /// 선택된 차주 수
        /// </summary>
        public int SelectedCount => Borrowers.Count(b => b.IsSelected);

        /// <summary>
        /// 전체 차주 수
        /// </summary>
        public int TotalCount => Borrowers.Count;

        /// <summary>
        /// 선택된 항목이 있는지 여부
        /// </summary>
        public bool HasSelection => SelectedCount > 0;

        /// <summary>
        /// 선택된 차주 목록 (결과)
        /// </summary>
        public List<SelectableBorrower> SelectedBorrowers => Borrowers.Where(b => b.IsSelected).ToList();

        public BorrowerSelectionDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 차주 목록 설정
        /// </summary>
        public void SetBorrowers(IEnumerable<BorrowerListItem> borrowers)
        {
            Borrowers.Clear();
            foreach (var borrower in borrowers)
            {
                Borrowers.Add(new SelectableBorrower
                {
                    BorrowerId = borrower.BorrowerId,
                    BorrowerNumber = borrower.BorrowerNumber,
                    BorrowerName = borrower.BorrowerName,
                    PropertyCount = borrower.PropertyCount,
                    IsSelected = true // 기본적으로 전체 선택
                });
            }
            UpdateCounts();
        }

        /// <summary>
        /// 인쇄용 다이얼로그 생성
        /// </summary>
        public static BorrowerSelectionDialog CreateForPrint(IEnumerable<BorrowerListItem> borrowers)
        {
            var dialog = new BorrowerSelectionDialog
            {
                Title = "인쇄할 차주 선택",
                Description = "선택한 차주의 데이터가 인쇄됩니다. 인쇄 후 Excel 파일이 열립니다.",
                ActionButtonText = "인쇄"
            };
            dialog.SetBorrowers(borrowers);
            return dialog;
        }

        /// <summary>
        /// 엑셀용 다이얼로그 생성
        /// </summary>
        public static BorrowerSelectionDialog CreateForExcel(IEnumerable<BorrowerListItem> borrowers)
        {
            var dialog = new BorrowerSelectionDialog
            {
                Title = "Excel로 내보낼 차주 선택",
                Description = "선택한 차주의 데이터가 Excel 파일로 저장됩니다.",
                ActionButtonText = "Excel 저장"
            };
            dialog.SetBorrowers(borrowers);
            return dialog;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var borrower in Borrowers)
            {
                borrower.IsSelected = true;
            }
            UpdateCounts();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var borrower in Borrowers)
            {
                borrower.IsSelected = false;
            }
            UpdateCounts();
        }

        private void BorrowerCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(HasSelection));
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelection)
            {
                MessageBox.Show("최소 1개 이상의 차주를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 선택 가능한 차주 모델
    /// </summary>
    public class SelectableBorrower : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid BorrowerId { get; set; }
        public string BorrowerNumber { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public int PropertyCount { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
    }
}
