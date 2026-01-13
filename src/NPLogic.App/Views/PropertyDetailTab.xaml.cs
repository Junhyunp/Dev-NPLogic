using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// PropertyDetailTab: 물건(Property) 수준 기초데이터 화면
    /// 기존 BasicDataTab의 백업본
    /// </summary>
    public partial class PropertyDetailTab : UserControl
    {
        public PropertyDetailTab()
        {
            InitializeComponent();
        }

        private async void PropertyDetailTab_Loaded(object sender, RoutedEventArgs e)
        {
            // 기존 BasicDataTab_Loaded 로직
            // 필요 시 WebView 초기화 등 추가
        }
    }
}
