using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;

namespace NPLogic.ViewModels
{
    /// <summary>
    /// 기초데이터 탭 ViewModel
    /// - 프로그램정보관리, Data Disk 정보관리, 기초정보관리
    /// - QA, 고유정보관리, 사용인 설정
    /// </summary>
    public partial class BasicDataTabViewModel : ObservableObject
    {
        private readonly ProgramRepository _programRepository;
        private Guid _programId;

        // ========== 상태 ==========

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        // ========== 프로그램 정보 ==========

        [ObservableProperty]
        private Program? _program;

        // ========== 기초정보관리 ==========

        [ObservableProperty]
        private decimal _irr = 0.15m;

        [ObservableProperty]
        private decimal _creditGuaranteeRecoveryRate = 0.80m;

        [ObservableProperty]
        private decimal _interimRecoveryRate = 0.80m;

        [ObservableProperty]
        private int _auctionFirstLeadTimeMonths = 6;

        [ObservableProperty]
        private int _roundLeadTime = 2;

        [ObservableProperty]
        private int _distributionLeadTime = 1;

        [ObservableProperty]
        private int _openingToAuctionLeadTime = 6;

        [ObservableProperty]
        private int _subrogationToAuctionLeadTime = 8;

        [ObservableProperty]
        private int _combinedLeadTime = 10;

        // ========== 사용인 설정 ==========

        [ObservableProperty]
        private string? _agentAffiliation;

        [ObservableProperty]
        private string? _agentName;

        [ObservableProperty]
        private string _agentGrade = "A";

        [ObservableProperty]
        private string? _agentContact;

        [ObservableProperty]
        private string? _agentKakaoId;

        [ObservableProperty]
        private string? _goodAuctionId;

        [ObservableProperty]
        private string? _goodAuctionPw;

        // ========== 업데이트 정보 ==========

        [ObservableProperty]
        private DateTime? _programInfoUpdatedAt;

        [ObservableProperty]
        private string? _programInfoUpdatedBy;

        [ObservableProperty]
        private DateTime? _basicInfoUpdatedAt;

        [ObservableProperty]
        private string? _basicInfoUpdatedBy;

        // ========== Data Disk 정보 ==========

        [ObservableProperty]
        private DateTime? _dataDiskUpdatedAt;

        [ObservableProperty]
        private string? _dataDiskUpdatedBy;

        // ========== QA 정보 ==========

        [ObservableProperty]
        private int _qaTotalCount;

        [ObservableProperty]
        private int _qaUnansweredCount;

        // ========== 고유정보관리 ==========

        [ObservableProperty]
        private int _courtReductionRateCount;

        [ObservableProperty]
        private string? _housingLeaseProtectionStatus;

        [ObservableProperty]
        private string? _commercialLeaseProtectionStatus;

        public BasicDataTabViewModel(ProgramRepository programRepository)
        {
            _programRepository = programRepository ?? throw new ArgumentNullException(nameof(programRepository));
        }

        /// <summary>
        /// 프로그램 ID로 초기화
        /// </summary>
        public async Task InitializeAsync(Guid programId)
        {
            _programId = programId;
            await LoadProgramDataAsync();
        }

        /// <summary>
        /// 프로그램 데이터 로드
        /// </summary>
        private async Task LoadProgramDataAsync()
        {
            if (_programId == Guid.Empty)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var program = await _programRepository.GetByIdAsync(_programId);
                if (program == null)
                {
                    ErrorMessage = "프로그램을 찾을 수 없습니다.";
                    return;
                }

                Program = program;

                // 기초정보관리 필드 로드
                Irr = program.Irr;
                CreditGuaranteeRecoveryRate = program.CreditGuaranteeRecoveryRate;
                InterimRecoveryRate = program.InterimRecoveryRate;
                AuctionFirstLeadTimeMonths = program.AuctionFirstLeadTimeMonths;
                RoundLeadTime = program.RoundLeadTime;
                DistributionLeadTime = program.DistributionLeadTime;
                OpeningToAuctionLeadTime = program.OpeningToAuctionLeadTime;
                SubrogationToAuctionLeadTime = program.SubrogationToAuctionLeadTime;
                CombinedLeadTime = program.CombinedLeadTime;

                // 사용인 설정 필드 로드
                AgentAffiliation = program.AgentAffiliation;
                AgentName = program.AgentName;
                AgentGrade = program.AgentGrade;
                AgentContact = program.AgentContact;
                AgentKakaoId = program.AgentKakaoId;
                GoodAuctionId = program.GoodAuctionId;
                GoodAuctionPw = program.GoodAuctionPw;

                // 업데이트 정보
                ProgramInfoUpdatedAt = program.UpdatedAt;
                BasicInfoUpdatedAt = program.UpdatedAt;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"데이터 로드 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 기초정보관리 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveBasicInfoAsync()
        {
            if (Program == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // 기초정보관리 필드 업데이트
                Program.Irr = Irr;
                Program.CreditGuaranteeRecoveryRate = CreditGuaranteeRecoveryRate;
                Program.InterimRecoveryRate = InterimRecoveryRate;
                Program.AuctionFirstLeadTimeMonths = AuctionFirstLeadTimeMonths;
                Program.RoundLeadTime = RoundLeadTime;
                Program.DistributionLeadTime = DistributionLeadTime;
                Program.OpeningToAuctionLeadTime = OpeningToAuctionLeadTime;
                Program.SubrogationToAuctionLeadTime = SubrogationToAuctionLeadTime;
                Program.CombinedLeadTime = CombinedLeadTime;

                await _programRepository.UpdateAsync(Program);

                BasicInfoUpdatedAt = DateTime.Now;
                SuccessMessage = "기초정보가 저장되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 사용인 설정 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAgentSettingsAsync()
        {
            if (Program == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // 사용인 설정 필드 업데이트
                Program.AgentAffiliation = AgentAffiliation;
                Program.AgentName = AgentName;
                Program.AgentGrade = AgentGrade;
                Program.AgentContact = AgentContact;
                Program.AgentKakaoId = AgentKakaoId;
                Program.GoodAuctionId = GoodAuctionId;
                Program.GoodAuctionPw = GoodAuctionPw;

                await _programRepository.UpdateAsync(Program);

                SuccessMessage = "사용인 설정이 저장되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 전체 저장
        /// </summary>
        [RelayCommand]
        private async Task SaveAllAsync()
        {
            if (Program == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // 기초정보관리 필드 업데이트
                Program.Irr = Irr;
                Program.CreditGuaranteeRecoveryRate = CreditGuaranteeRecoveryRate;
                Program.InterimRecoveryRate = InterimRecoveryRate;
                Program.AuctionFirstLeadTimeMonths = AuctionFirstLeadTimeMonths;
                Program.RoundLeadTime = RoundLeadTime;
                Program.DistributionLeadTime = DistributionLeadTime;
                Program.OpeningToAuctionLeadTime = OpeningToAuctionLeadTime;
                Program.SubrogationToAuctionLeadTime = SubrogationToAuctionLeadTime;
                Program.CombinedLeadTime = CombinedLeadTime;

                // 사용인 설정 필드 업데이트
                Program.AgentAffiliation = AgentAffiliation;
                Program.AgentName = AgentName;
                Program.AgentGrade = AgentGrade;
                Program.AgentContact = AgentContact;
                Program.AgentKakaoId = AgentKakaoId;
                Program.GoodAuctionId = GoodAuctionId;
                Program.GoodAuctionPw = GoodAuctionPw;

                await _programRepository.UpdateAsync(Program);

                BasicInfoUpdatedAt = DateTime.Now;
                SuccessMessage = "모든 설정이 저장되었습니다.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"저장 실패: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 새로고침
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadProgramDataAsync();
            SuccessMessage = "데이터를 새로고침했습니다.";
        }
    }
}
