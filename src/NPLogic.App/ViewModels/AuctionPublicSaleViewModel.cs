using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 경(공)매 일정 통합 ViewModel - 탭으로 경매/공매 전환 지원
    /// </summary>
    public partial class AuctionPublicSaleViewModel : ObservableObject
    {
        private readonly SupabaseService? _supabaseService;
        
        [ObservableProperty]
        private Guid? _propertyId;
        
        [ObservableProperty]
        private int _selectedTabIndex;
        
        /// <summary>
        /// 경매일정(Ⅶ) ViewModel
        /// </summary>
        public AuctionScheduleDetailViewModel AuctionViewModel { get; }
        
        /// <summary>
        /// 공매일정(Ⅷ) ViewModel
        /// </summary>
        public PublicSaleScheduleViewModel PublicSaleViewModel { get; }
        
        public AuctionPublicSaleViewModel()
        {
            AuctionViewModel = new AuctionScheduleDetailViewModel();
            PublicSaleViewModel = new PublicSaleScheduleViewModel();
        }
        
        public AuctionPublicSaleViewModel(
            SupabaseService? supabaseService,
            AuctionScheduleRepository? auctionScheduleRepository,
            PublicSaleScheduleRepository? publicSaleScheduleRepository,
            PropertyRepository? propertyRepository = null,
            EvaluationRepository? evaluationRepository = null,
            RightAnalysisRepository? rightAnalysisRepository = null)
        {
            _supabaseService = supabaseService;
            
            AuctionViewModel = new AuctionScheduleDetailViewModel(
                supabaseService,
                auctionScheduleRepository,
                propertyRepository,
                evaluationRepository,
                rightAnalysisRepository);
            
            PublicSaleViewModel = new PublicSaleScheduleViewModel(
                supabaseService,
                publicSaleScheduleRepository,
                propertyRepository,
                evaluationRepository,
                rightAnalysisRepository);
        }
        
        /// <summary>
        /// 물건 ID 설정
        /// </summary>
        public void SetPropertyId(Guid propertyId)
        {
            PropertyId = propertyId;
            AuctionViewModel.SetPropertyId(propertyId);
            PublicSaleViewModel.SetPropertyId(propertyId);
        }
        
        /// <summary>
        /// 초기화 (경매 탭 먼저)
        /// </summary>
        public async Task InitializeAsync()
        {
            await AuctionViewModel.InitializeAsync();
        }
        
        /// <summary>
        /// 공매 탭 초기화
        /// </summary>
        public async Task InitializePublicSaleAsync()
        {
            await PublicSaleViewModel.InitializeAsync();
        }
        
        /// <summary>
        /// 현재 탭에 따라 저장
        /// </summary>
        public async Task SaveCurrentAsync()
        {
            if (SelectedTabIndex == 0)
            {
                await AuctionViewModel.SaveAsync();
            }
            else
            {
                await PublicSaleViewModel.SaveAsync();
            }
        }
        
        partial void OnPropertyIdChanged(Guid? value)
        {
            if (value.HasValue)
            {
                AuctionViewModel.SetPropertyId(value.Value);
                PublicSaleViewModel.SetPropertyId(value.Value);
            }
        }
    }
}
