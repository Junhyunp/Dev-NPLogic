using System;
using System.Windows;
using System.Windows.Controls;
using NPLogic.Data.Repositories;
using NPLogic.Data.Services;
using NPLogic.ViewModels;

namespace NPLogic.Views
{
    /// <summary>
    /// 프로그램(Pool) 수준 기초데이터 화면
    /// - 프로그램정보관리, Data Disk 정보관리, 기초정보관리
    /// - QA, 고유정보관리, 사용인 설정, Assign
    /// </summary>
    public partial class BasicDataTab : UserControl
    {
        private BasicDataTabViewModel? _viewModel;
        private Guid _programId;

        public BasicDataTab()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 프로그램 ID 설정 및 초기화
        /// </summary>
        public async void Initialize(Guid programId, SupabaseService supabaseService)
        {
            _programId = programId;

            var programRepository = new ProgramRepository(supabaseService);
            _viewModel = new BasicDataTabViewModel(programRepository);
            DataContext = _viewModel;

            await _viewModel.InitializeAsync(programId);
        }

        private async void BasicDataTab_Loaded(object sender, RoutedEventArgs e)
        {
            // 이미 초기화된 경우 스킵
            if (_viewModel != null && _programId != Guid.Empty)
            {
                await _viewModel.InitializeAsync(_programId);
            }
        }
    }
}
