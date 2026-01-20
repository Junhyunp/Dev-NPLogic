using System;
using System.Windows;
using System.Windows.Controls;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 경(공)매 일정 통합 뷰 - 탭으로 경매/공매 전환
    /// </summary>
    public partial class AuctionPublicSaleView : UserControl
    {
        private AuctionScheduleDetailViewModel? _auctionViewModel;
        private PublicSaleScheduleViewModel? _publicSaleViewModel;
        private Guid? _currentPropertyId;
        
        public AuctionPublicSaleView()
        {
            InitializeComponent();
        }
        
        private async void AuctionPublicSaleView_Loaded(object sender, RoutedEventArgs e)
        {
            // ViewModel이 설정되어 있으면 초기화
            if (_auctionViewModel != null)
            {
                AuctionContent.DataContext = _auctionViewModel;
                await _auctionViewModel.InitializeAsync();
            }
            
            if (_publicSaleViewModel != null)
            {
                PublicSaleContent.DataContext = _publicSaleViewModel;
                // 공매 탭은 선택될 때 초기화
            }
        }
        
        private async void ScheduleTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != ScheduleTabControl) return;
            
            var selectedIndex = ScheduleTabControl.SelectedIndex;
            
            if (selectedIndex == 1 && _publicSaleViewModel != null)
            {
                // 공매 탭 선택 시 초기화
                await _publicSaleViewModel.InitializeAsync();
            }
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = ScheduleTabControl.SelectedIndex;
            
            if (selectedIndex == 0 && _auctionViewModel != null)
            {
                await _auctionViewModel.SaveAsync();
            }
            else if (selectedIndex == 1 && _publicSaleViewModel != null)
            {
                await _publicSaleViewModel.SaveAsync();
            }
        }
        
        /// <summary>
        /// 물건 ID 설정 및 ViewModel 초기화
        /// </summary>
        public void SetPropertyId(Guid propertyId)
        {
            _currentPropertyId = propertyId;
            
            if (_auctionViewModel != null)
            {
                _auctionViewModel.SetPropertyId(propertyId);
            }
            
            if (_publicSaleViewModel != null)
            {
                _publicSaleViewModel.SetPropertyId(propertyId);
            }
        }
        
        /// <summary>
        /// ViewModel 설정
        /// </summary>
        public void SetViewModels(AuctionScheduleDetailViewModel auctionVm, PublicSaleScheduleViewModel publicSaleVm)
        {
            _auctionViewModel = auctionVm;
            _publicSaleViewModel = publicSaleVm;
            
            AuctionContent.DataContext = _auctionViewModel;
            PublicSaleContent.DataContext = _publicSaleViewModel;
            
            if (_currentPropertyId.HasValue)
            {
                _auctionViewModel.SetPropertyId(_currentPropertyId.Value);
                _publicSaleViewModel.SetPropertyId(_currentPropertyId.Value);
            }
        }
    }
}
