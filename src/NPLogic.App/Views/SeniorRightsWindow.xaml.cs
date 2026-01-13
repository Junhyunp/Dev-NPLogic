using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NPLogic.Data.Repositories;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 선순위 분석 팝업 창
    /// </summary>
    public partial class SeniorRightsWindow : Window
    {
        private readonly Guid _propertyId;

        public SeniorRightsWindow(Guid propertyId, string propertyInfo)
        {
            InitializeComponent();
            _propertyId = propertyId;
            Title = $"선순위 분석 - {propertyInfo}";
            
            Loaded += SeniorRightsWindow_Loaded;
        }

        private async void SeniorRightsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ViewModel 가져오기
                if (SeniorRightsContent.DataContext is SeniorRightsViewModel vm)
                {
                    // 해당 물건 선택
                    foreach (var prop in vm.Properties)
                    {
                        if (prop.Id == _propertyId)
                        {
                            vm.SelectedProperty = prop;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"선순위 데이터 로드 실패: {ex.Message}", "오류", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
