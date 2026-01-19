using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views.Loan
{
    /// <summary>
    /// LoanSheetView.xaml - Loan(Ⅰ) 시트 메인 뷰
    /// 4개 탭: 일반, 일반보증, 해지부보증, 일반+해지부보증
    /// </summary>
    public partial class LoanSheetView : UserControl
    {
        private LoanSheetViewModel? _viewModel;
        private string _currentSheet = "Basic";
        private bool _isInitialized;

        public LoanSheetView()
        {
            InitializeComponent();
        }

        private async void LoanSheetView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as LoanSheetViewModel;
            Debug.WriteLine($"[LoanSheetView] Loaded - ViewModel: {(_viewModel != null ? "OK" : "NULL")}");
            
            if (_viewModel != null && !_isInitialized)
            {
                _isInitialized = true;
                
                // ViewModel이 이미 데이터를 가지고 있는지 확인
                if (_viewModel.Loans.Count == 0 && _viewModel.Borrowers.Count == 0)
                {
                    Debug.WriteLine("[LoanSheetView] No data, initializing...");
                    await _viewModel.InitializeAsync();
                }
                else
                {
                    Debug.WriteLine($"[LoanSheetView] Already has data - Loans: {_viewModel.Loans.Count}, Borrowers: {_viewModel.Borrowers.Count}");
                }
                
                LoadSheetContent("Basic");
            }
        }

        /// <summary>
        /// 시트 탭 변경 이벤트
        /// </summary>
        private void SheetTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string sheetType)
            {
                LoadSheetContent(sheetType);
            }
        }

        /// <summary>
        /// 시트 컨텐츠 로드
        /// </summary>
        private void LoadSheetContent(string sheetType)
        {
            if (_viewModel == null) return;
            
            _currentSheet = sheetType;
            SheetContentGrid.Children.Clear();

            UserControl? sheetContent = sheetType switch
            {
                "Basic" => new Sheets.BasicSheet { DataContext = _viewModel },
                "Guarantee" => new Sheets.GuaranteeSheet { DataContext = _viewModel },
                "Terminated" => new Sheets.TerminatedGuaranteeSheet { DataContext = _viewModel },
                "Combined" => new Sheets.CombinedSheet { DataContext = _viewModel },
                _ => null
            };

            if (sheetContent != null)
            {
                SheetContentGrid.Children.Add(sheetContent);
            }
        }

        /// <summary>
        /// 현재 시트 새로고침
        /// </summary>
        public void RefreshCurrentSheet()
        {
            LoadSheetContent(_currentSheet);
        }
    }
}
