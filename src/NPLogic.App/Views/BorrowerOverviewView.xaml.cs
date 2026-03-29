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

        private async void BusinessStatusQuery_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BorrowerOverviewViewModel vm || vm.SelectedBorrower == null)
                return;

            var bizNumber = vm.SelectedBorrower.BusinessNumber;
            if (string.IsNullOrWhiteSpace(bizNumber))
            {
                NPLogic.UI.Services.ToastService.Instance.ShowWarning("사업자번호를 먼저 입력해주세요.");
                return;
            }

            try
            {
                var service = App.ServiceProvider?.GetService(typeof(NPLogic.Services.BusinessRegistrationService)) as NPLogic.Services.BusinessRegistrationService;
                if (service == null || !service.HasApiKey)
                {
                    NPLogic.UI.Services.ToastService.Instance.ShowError("사업자 상태조회 API 키가 설정되지 않았습니다.");
                    return;
                }

                vm.BusinessStatusDisplay = "조회 중...";
                var result = await service.GetStatusAsync(bizNumber);
                if (result != null)
                {
                    vm.BusinessStatusDisplay = result.DisplaySummary;
                }
                else
                {
                    vm.BusinessStatusDisplay = "조회 실패";
                }
            }
            catch (Exception ex)
            {
                vm.BusinessStatusDisplay = $"오류: {ex.Message}";
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

