---
phase: 03-all-tabs-notes
plan: 01
subsystem: ui
tags: [wpf, mvvm, xaml, side-panel, notes, community-toolkit]

# Dependency graph
requires:
  - phase: 01-db-data-foundation
    provides: PropertyNote model + PropertyNoteRepository (Supabase CRUD)
provides:
  - 차주개요/권리분석/기초데이터/QA집계 탭 비고 사이드 패널 (ViewModel + View)
  - 기존 저장 커맨드에 비고 저장 통합
  - 기존 데이터 로드 흐름에 비고 로드 통합
affects: [03-02, 04-notes-overview]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - App.ServiceProvider 패턴으로 PropertyNoteRepository 주입 (생성자 변경 불필요)
    - 프로그램 레벨 탭은 programId를 property_id로 사용
    - 사용자 레벨 탭은 userId를 property_id로 사용

key-files:
  created: []
  modified:
    - src/NPLogic.App/ViewModels/BorrowerOverviewViewModel.cs
    - src/NPLogic.App/ViewModels/RightsAnalysisTabViewModel.cs
    - src/NPLogic.App/ViewModels/BasicDataTabViewModel.cs
    - src/NPLogic.App/ViewModels/QASummaryViewModel.cs
    - src/NPLogic.App/Views/BorrowerOverviewView.xaml
    - src/NPLogic.App/Views/RightsAnalysisTab.xaml
    - src/NPLogic.App/Views/BasicDataTab.xaml
    - src/NPLogic.App/Views/QASummaryTab.xaml

key-decisions:
  - "App.ServiceProvider 패턴 사용 (4개 ViewModel 모두) - 생성자 파라미터 변경 없이 Repository 접근"
  - "BasicDataTab: programId를 property_id로 사용 (프로그램 레벨 비고)"
  - "QASummaryTab: userId를 property_id로 사용 (사용자별 QA 비고)"

patterns-established:
  - "비고 패널 패턴: NoteText/IsNotePanelVisible + ToggleNotePanel + LoadNoteAsync/SaveNoteAsync"
  - "2열 Grid 래퍼 패턴: 기존 콘텐츠(Column 0) + 비고 사이드 패널(Column 1, Width=250, Auto)"

requirements-completed: [SCOPE-01, SCOPE-02]

# Metrics
duration: 12min
completed: 2026-03-24
---

# Phase 3 Plan 01: 저장 탭 비고 사이드 패널 Summary

**차주개요/권리분석/기초데이터/QA집계 4개 탭에 250px 비고 사이드 패널 + NoteEdit 토글 버튼 추가, 기존 Save/Load 흐름에 비고 저장/로드 통합**

## Performance

- **Duration:** 12 min
- **Started:** 2026-03-24T14:11:58Z
- **Completed:** 2026-03-24T14:24:26Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- 4개 ViewModel에 비고 속성/명령/로드/저장 로직 추가 (PropertyNoteRepository 통합)
- 4개 View에 비고 사이드 패널 XAML + materialDesign 토글 버튼 추가
- 기존 저장 명령(SaveBorrower/Save/SaveBasicInfo/SaveQa)에 비고 저장 자동 통합
- 기존 데이터 로드 흐름(SetSelectedProperty/LoadData/Initialize)에 비고 로드 자동 통합

## Task Commits

Each task was committed atomically:

1. **Task 1: 4개 ViewModel에 비고 속성/명령/로드/저장 로직 추가** - `dd7c653` (feat)
2. **Task 2: 4개 View에 사이드 패널 XAML + 토글 버튼 추가** - `9c804dd` (feat)

## Files Created/Modified
- `src/NPLogic.App/ViewModels/BorrowerOverviewViewModel.cs` - NoteText/IsNotePanelVisible + LoadNoteAsync/SaveNoteAsync (SelectedProperty.Id 기반)
- `src/NPLogic.App/ViewModels/RightsAnalysisTabViewModel.cs` - 비고 로직 (property_id 기반, rights_analysis 탭)
- `src/NPLogic.App/ViewModels/BasicDataTabViewModel.cs` - 비고 로직 (programId 기반, basic_data 탭)
- `src/NPLogic.App/ViewModels/QASummaryViewModel.cs` - 비고 로직 (userId 기반, qa_summary 탭)
- `src/NPLogic.App/Views/BorrowerOverviewView.xaml` - 2열 Grid 래퍼 + 사이드 패널 + 토글 버튼
- `src/NPLogic.App/Views/RightsAnalysisTab.xaml` - 2열 Grid 래퍼 + 사이드 패널 + 토글 버튼
- `src/NPLogic.App/Views/BasicDataTab.xaml` - 루트 Grid 컬럼 확장 + 사이드 패널 + 토글 버튼
- `src/NPLogic.App/Views/QASummaryTab.xaml` - 외부 Grid 래퍼 + 사이드 패널 + 토글 버튼

## Decisions Made
- App.ServiceProvider 패턴 사용: 4개 ViewModel 모두 생성자 파라미터 변경 없이 `App.ServiceProvider?.GetService(typeof(PropertyNoteRepository))` 패턴으로 Repository 접근. Phase 2 EvaluationTabViewModel 선례 따름.
- BasicDataTab 프로그램 레벨 비고: programId를 property_id로 사용하여 프로그램 단위 비고 저장. Phase 4 종합 표시 시 특수 케이스 고려 필요.
- QASummaryTab 사용자 레벨 비고: userId를 property_id로 사용하여 사용자별 QA 비고 저장. Session에서 추출한 userId를 _noteOwnerId로 보관.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- 빌드 시 LoanSheetViewModel.cs와 LoanSheetView.xaml에서 pre-existing 에러 발생 (본 Plan 변경사항과 무관). 4개 대상 파일의 XML 유효성 및 C# 컴파일은 정상 확인됨.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- 저장 버튼이 있는 4개 탭 비고 기능 완료
- Phase 3 Plan 02 (저장 버튼 없는 탭들) 진행 준비 완료
- Phase 4 (종합 비고 표시) 진행 시 BasicDataTab/QASummaryTab의 programId/userId 기반 비고 특수 처리 필요

## Self-Check: PASSED

- All 8 modified files confirmed present on disk
- Commits dd7c653 (Task 1) and 9c804dd (Task 2) verified in git log
- All 4 ViewModels contain NoteText/IsNotePanelVisible/ToggleNotePanel/LoadNoteAsync/SaveNoteAsync
- All 4 Views contain NotePanelColumn and ToggleNotePanelCommand
- SaveNoteAsync integrated into all 4 save commands
- LoadNoteAsync integrated into all 4 load flows

---
*Phase: 03-all-tabs-notes*
*Completed: 2026-03-24*
