using System;

namespace NPLogic.Services
{
    /// <summary>
    /// 대시보드 네비게이션 상태 저장 클래스
    /// </summary>
    public class DashboardNavigationState
    {
        /// <summary>
        /// 선택된 프로그램 ID
        /// </summary>
        public string? SelectedProgramId { get; set; }

        /// <summary>
        /// 선택된 탭 이름 (home, noncore, registry, rights, basicdata, closing)
        /// </summary>
        public string SelectedTab { get; set; } = "home";

        /// <summary>
        /// 선택된 물건 ID
        /// </summary>
        public Guid? SelectedPropertyId { get; set; }

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 네비게이션 상태 관리 서비스 (싱글톤)
    /// 다른 화면으로 이동했다가 돌아와도 이전 상태를 복원하기 위한 서비스
    /// </summary>
    public class NavigationStateService
    {
        private static NavigationStateService? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static NavigationStateService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new NavigationStateService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 대시보드 상태
        /// </summary>
        public DashboardNavigationState DashboardState { get; } = new();

        private NavigationStateService()
        {
        }

        /// <summary>
        /// 대시보드 상태 저장
        /// </summary>
        public void SaveDashboardState(string? programId, string tabName, Guid? propertyId = null)
        {
            DashboardState.SelectedProgramId = programId;
            DashboardState.SelectedTab = tabName ?? "home";
            DashboardState.SelectedPropertyId = propertyId;
            DashboardState.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// 대시보드 상태 초기화
        /// </summary>
        public void ClearDashboardState()
        {
            DashboardState.SelectedProgramId = null;
            DashboardState.SelectedTab = "home";
            DashboardState.SelectedPropertyId = null;
            DashboardState.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// 저장된 상태가 있는지 확인
        /// </summary>
        public bool HasSavedDashboardState()
        {
            return !string.IsNullOrEmpty(DashboardState.SelectedProgramId);
        }
    }
}

