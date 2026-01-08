using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
    /// 비핵심 탭 상태 저장 클래스 (파일 저장용)
    /// </summary>
    public class NonCoreTabState
    {
        /// <summary>
        /// 프로그램별 닫은 탭 ID 목록 (Key: ProgramId, Value: 닫은 PropertyId 목록)
        /// </summary>
        public Dictionary<string, List<string>> ClosedTabsByProgram { get; set; } = new();
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

        // ========== 비핵심 탭 상태 관리 (파일 저장) ==========

        private NonCoreTabState _nonCoreTabState = new();
        private static readonly string TabStateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NPLogic",
            "noncore_tab_state.json");

        /// <summary>
        /// 탭 상태 파일에서 로드
        /// </summary>
        public void LoadNonCoreTabState()
        {
            try
            {
                if (File.Exists(TabStateFilePath))
                {
                    var json = File.ReadAllText(TabStateFilePath);
                    _nonCoreTabState = JsonSerializer.Deserialize<NonCoreTabState>(json) ?? new NonCoreTabState();
                }
            }
            catch
            {
                _nonCoreTabState = new NonCoreTabState();
            }
        }

        /// <summary>
        /// 탭 상태 파일에 저장
        /// </summary>
        private void SaveNonCoreTabState()
        {
            try
            {
                var directory = Path.GetDirectoryName(TabStateFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_nonCoreTabState, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TabStateFilePath, json);
            }
            catch
            {
                // 저장 실패 시 무시 (다음 번에 다시 시도)
            }
        }

        /// <summary>
        /// 특정 프로그램의 닫은 탭 ID 목록 가져오기
        /// </summary>
        public HashSet<Guid> GetClosedTabs(Guid programId)
        {
            var key = programId.ToString();
            if (_nonCoreTabState.ClosedTabsByProgram.TryGetValue(key, out var closedIds))
            {
                var result = new HashSet<Guid>();
                foreach (var id in closedIds)
                {
                    if (Guid.TryParse(id, out var guid))
                    {
                        result.Add(guid);
                    }
                }
                return result;
            }
            return new HashSet<Guid>();
        }

        /// <summary>
        /// 탭 닫기 기록 추가
        /// </summary>
        public void AddClosedTab(Guid programId, Guid propertyId)
        {
            var key = programId.ToString();
            if (!_nonCoreTabState.ClosedTabsByProgram.ContainsKey(key))
            {
                _nonCoreTabState.ClosedTabsByProgram[key] = new List<string>();
            }

            var propertyIdStr = propertyId.ToString();
            if (!_nonCoreTabState.ClosedTabsByProgram[key].Contains(propertyIdStr))
            {
                _nonCoreTabState.ClosedTabsByProgram[key].Add(propertyIdStr);
                SaveNonCoreTabState();
            }
        }

        /// <summary>
        /// 탭 열기 기록 (닫은 목록에서 제거)
        /// </summary>
        public void RemoveClosedTab(Guid programId, Guid propertyId)
        {
            var key = programId.ToString();
            if (_nonCoreTabState.ClosedTabsByProgram.ContainsKey(key))
            {
                _nonCoreTabState.ClosedTabsByProgram[key].Remove(propertyId.ToString());
                SaveNonCoreTabState();
            }
        }

        /// <summary>
        /// 특정 프로그램의 닫은 탭 기록 모두 초기화
        /// </summary>
        public void ClearClosedTabs(Guid programId)
        {
            var key = programId.ToString();
            if (_nonCoreTabState.ClosedTabsByProgram.ContainsKey(key))
            {
                _nonCoreTabState.ClosedTabsByProgram.Remove(key);
                SaveNonCoreTabState();
            }
        }

        /// <summary>
        /// 모든 닫은 탭 기록 초기화
        /// </summary>
        public void ClearAllClosedTabs()
        {
            _nonCoreTabState.ClosedTabsByProgram.Clear();
            SaveNonCoreTabState();
        }
    }
}

