using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// BorrowerOverviewView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BorrowerOverviewView : UserControl
    {
        public BorrowerOverviewView()
        {
            InitializeComponent();
        }

        private async void BorrowerOverviewView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BorrowerOverviewViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        private void BusinessNumberSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BorrowerOverviewViewModel vm || vm.SelectedBorrower == null)
                return;

            var bizNumber = vm.SelectedBorrower.BusinessNumber;
            if (string.IsNullOrWhiteSpace(bizNumber))
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("사업자번호가 비어있습니다. 먼저 사업자번호를 입력해주세요.");
                return;
            }

            // 사업자번호를 클립보드에 복사
            System.Windows.Clipboard.SetText(bizNumber.Trim());
            NPLogic.UI.Services.ToastService.Instance.ShowSuccess($"사업자번호 '{bizNumber.Trim()}'가 클립보드에 복사되었습니다. 홈택스에서 Ctrl+V로 붙여넣기 하세요.");

            // 홈택스 사업자등록상태조회 페이지 열기
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://hometax.go.kr/websquare/websquare.html?w2xPath=/ui/pp/index_pp.xml&tmIdx=43&tm2lIdx=4306000000&tm3lIdx=4306080000",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                NPLogic.UI.Services.ToastService.Instance.ShowError($"브라우저 열기 실패: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// bool? 값을 "Y"/"N"/"-" 문자열로 변환하는 컨버터
    /// </summary>
    public class BoolToYNConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? "Y" : "N";
            if (value is bool?)
            {
                var nullable = (bool?)value;
                return nullable.HasValue ? (nullable.Value ? "Y" : "N") : "-";
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
                return str == "Y";
            return false;
        }
    }
}

