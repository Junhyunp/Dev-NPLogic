using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views
{
    /// <summary>
    /// 제목행(헤더행) 미리보기 및 선택 다이얼로그
    /// </summary>
    public partial class HeaderRowPreviewDialog : Window, INotifyPropertyChanged
    {
        private string _sheetName = "";
        private int _selectedHeaderRow = 1;
        private int _totalRows = 0;
        private DataTable? _previewData;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 시트 이름
        /// </summary>
        public string SheetName
        {
            get => _sheetName;
            set
            {
                _sheetName = value;
                OnPropertyChanged(nameof(SheetName));
            }
        }

        /// <summary>
        /// 선택된 제목행 번호 (1-based)
        /// </summary>
        public int SelectedHeaderRow
        {
            get => _selectedHeaderRow;
            set
            {
                _selectedHeaderRow = value;
                OnPropertyChanged(nameof(SelectedHeaderRow));
                UpdateHeaderRowText();
            }
        }

        /// <summary>
        /// 전체 행 수 텍스트
        /// </summary>
        public string TotalRowsText => $"전체 {_totalRows}행";

        public HeaderRowPreviewDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 시트 미리보기 데이터 설정
        /// </summary>
        /// <param name="sheetName">시트 이름</param>
        /// <param name="previewRows">미리보기 데이터 (처음 N행)</param>
        /// <param name="currentHeaderRow">현재 설정된 헤더행 (1-based)</param>
        /// <param name="totalRows">전체 행 수</param>
        public void SetPreviewData(string sheetName, List<List<object?>> previewRows, int currentHeaderRow, int totalRows)
        {
            SheetName = sheetName;
            _selectedHeaderRow = currentHeaderRow;
            _totalRows = totalRows;
            OnPropertyChanged(nameof(TotalRowsText));
            UpdateHeaderRowText();

            // DataTable 생성
            _previewData = new DataTable();

            if (previewRows == null || previewRows.Count == 0)
            {
                PreviewDataGrid.ItemsSource = null;
                return;
            }

            // 최대 컬럼 수 계산
            int maxCols = previewRows.Max(r => r?.Count ?? 0);
            
            // 행 번호 컬럼 추가
            _previewData.Columns.Add("행", typeof(string));
            
            // 컬럼 생성 (A, B, C, ... 형식)
            for (int i = 0; i < maxCols; i++)
            {
                string colName = GetExcelColumnName(i);
                _previewData.Columns.Add(colName, typeof(string));
            }

            // 데이터 추가
            for (int rowIdx = 0; rowIdx < previewRows.Count; rowIdx++)
            {
                var row = previewRows[rowIdx];
                var dataRow = _previewData.NewRow();
                
                // 행 번호 (1-based)
                dataRow["행"] = $"{rowIdx + 1}행";
                
                if (row != null)
                {
                    for (int colIdx = 0; colIdx < row.Count && colIdx < maxCols; colIdx++)
                    {
                        var value = row[colIdx];
                        dataRow[colIdx + 1] = value?.ToString() ?? "";
                    }
                }
                
                _previewData.Rows.Add(dataRow);
            }

            PreviewDataGrid.ItemsSource = _previewData.DefaultView;

            // 현재 헤더행 선택
            if (currentHeaderRow > 0 && currentHeaderRow <= previewRows.Count)
            {
                PreviewDataGrid.SelectedIndex = currentHeaderRow - 1;
            }
        }

        /// <summary>
        /// Excel 컬럼 이름 생성 (A, B, ..., Z, AA, AB, ...)
        /// </summary>
        private string GetExcelColumnName(int columnIndex)
        {
            string columnName = "";
            while (columnIndex >= 0)
            {
                columnName = (char)('A' + columnIndex % 26) + columnName;
                columnIndex = columnIndex / 26 - 1;
            }
            return columnName;
        }

        private void UpdateHeaderRowText()
        {
            if (CurrentHeaderRowText != null)
            {
                CurrentHeaderRowText.Text = $"{_selectedHeaderRow}행";
            }
        }

        private void PreviewDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PreviewDataGrid.SelectedIndex >= 0)
            {
                SelectedHeaderRow = PreviewDataGrid.SelectedIndex + 1;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
