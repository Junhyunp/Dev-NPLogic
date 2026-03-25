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

