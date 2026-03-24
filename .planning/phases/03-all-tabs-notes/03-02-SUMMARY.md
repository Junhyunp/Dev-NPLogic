---
phase: 03-all-tabs-notes
plan: 02
subsystem: ui
tags: [wpf, mvvm, note-panel, lost-focus, auto-save, code-behind]

# Dependency graph
requires:
  - phase: 01-db-data-foundation
    provides: PropertyNoteRepository, property_notes 테이블
  - phase: 03-all-tabs-notes (plan 01)
    provides: 4개 탭 비고 패턴 (BorrowerOverview, RightsAnalysis, BasicData, QASummary)
provides:
  - 6개 추가 탭(Loan, 경공매일정, 인터림, 현금흐름집계, NPV비교, 마감)에 비고 사이드 패널
  - LostFocus 자동 저장 패턴 (저장 버튼 없는 탭 전용)
  - ClosingTab code-behind 비고 관리 패턴 (익명 DataContext 대응)
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "LostFocus auto-save: TextBox LostFocus -> ViewModel.SaveNoteCommand.Execute(null)"
    - "code-behind note management: PropertyNoteRepository 직접 사용 (익명 DataContext 제약)"
    - "프로그램 레벨 비고: programId를 noteContextId로 활용"
    - "사용자 레벨 비고: AuthService 세션 userId를 noteOwnerId로 활용"

key-files:
  created: []
  modified:
    - src/NPLogic.App/ViewModels/LoanSheetViewModel.cs
    - src/NPLogic.App/ViewModels/AuctionScheduleDetailViewModel.cs
    - src/NPLogic.App/ViewModels/InterimTabViewModel.cs
    - src/NPLogic.App/ViewModels/CashFlowSummaryViewModel.cs
    - src/NPLogic.App/ViewModels/XnpvComparisonViewModel.cs
    - src/NPLogic.App/Views/Loan/LoanSheetView.xaml
    - src/NPLogic.App/Views/Loan/LoanSheetView.xaml.cs
    - src/NPLogic.App/Views/AuctionPublicSaleView.xaml
    - src/NPLogic.App/Views/AuctionPublicSaleView.xaml.cs
    - src/NPLogic.App/Views/InterimTab.xaml
    - src/NPLogic.App/Views/InterimTab.xaml.cs
    - src/NPLogic.App/Views/CashFlowSummaryView.xaml
    - src/NPLogic.App/Views/CashFlowSummaryView.xaml.cs
    - src/NPLogic.App/Views/XnpvComparisonView.xaml
    - src/NPLogic.App/Views/XnpvComparisonView.xaml.cs
    - src/NPLogic.App/Views/ClosingTab.xaml
    - src/NPLogic.App/Views/ClosingTab.xaml.cs

key-decisions:
  - "ClosingTab은 code-behind 방식 사용 (익명 DataContext 제약으로 ViewModel 바인딩 불가)"
  - "InterimTab 비고는 programId를 property_id로 활용 (물건 컨텍스트 없는 프로그램 레벨 탭)"
  - "CashFlowSummary/XnpvComparison 비고는 userId를 property_id로 활용 (사용자 레벨 비고)"
  - "AuctionPublicSaleView NotePanelColumn DataContext를 code-behind에서 auctionVm으로 설정"

patterns-established:
  - "LostFocus auto-save: 저장 버튼 없는 탭에서 TextBox LostFocus 시 자동 DB 저장"
  - "code-behind note: 익명 DataContext 탭에서 Reflection으로 Property 추출 후 직접 Repository 호출"

requirements-completed: [SCOPE-01, SCOPE-02]

# Metrics
duration: 17min
completed: 2026-03-24
---

# Phase 3 Plan 2: 저장 버튼 없는 6개 탭 비고 사이드 패널 Summary

**6개 탭(Loan/경공매/인터림/현금흐름/NPV비교/마감)에 LostFocus 자동 저장 비고 패널 추가, ClosingTab은 익명 DataContext 대응 code-behind 방식**

## Performance

- **Duration:** 17 min
- **Started:** 2026-03-24T14:12:16Z
- **Completed:** 2026-03-24T14:29:xx Z
- **Tasks:** 2 of 2 auto tasks completed (Task 3 = checkpoint:human-verify)
- **Files modified:** 17

## Accomplishments
- 5개 ViewModel(Loan, AuctionSchedule, Interim, CashFlowSummary, XnpvComparison)에 비고 속성/명령/로드/저장 로직 추가
- 6개 View에 250px 사이드 패널 XAML + 토글 버튼 + LostFocus 자동 저장 구현
- ClosingTab은 code-behind에서 PropertyNoteRepository 직접 사용 (익명 DataContext 제약 해결)
- 프로그램 레벨(Interim) 및 사용자 레벨(CashFlow/Xnpv) 비고 컨텍스트 설계

## Task Commits

Each task was committed atomically:

1. **Task 1: 6개 ViewModel에 비고 속성/명령/로드/저장 로직 추가** - `6a7b47b` (feat)
2. **Task 2: 6개 View에 사이드 패널 XAML + LostFocus 자동 저장 추가** - `f89c048` (feat)

**Plan metadata:** (pending final docs commit)

## Files Created/Modified
- `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs` - NoteText, IsNotePanelVisible, ToggleNotePanel, SaveNote, LoadNoteAsync, SaveNoteAsync
- `src/NPLogic.App/ViewModels/AuctionScheduleDetailViewModel.cs` - 동일 패턴, PropertyId 기반, OnPropertyIdChanged에서 LoadNote
- `src/NPLogic.App/ViewModels/InterimTabViewModel.cs` - programId를 noteContextId로, "interim" tabName
- `src/NPLogic.App/ViewModels/CashFlowSummaryViewModel.cs` - AuthService userId를 noteOwnerId로, "cashflow_summary" tabName
- `src/NPLogic.App/ViewModels/XnpvComparisonViewModel.cs` - AuthService userId를 noteOwnerId로, "npv_comparison" tabName
- `src/NPLogic.App/Views/Loan/LoanSheetView.xaml` - 2열 Grid 래핑, 비고 패널, 토글 버튼
- `src/NPLogic.App/Views/Loan/LoanSheetView.xaml.cs` - NoteTextBox_LostFocus 핸들러
- `src/NPLogic.App/Views/AuctionPublicSaleView.xaml` - 2열 Grid 래핑, 비고 패널, 토글 Click 버튼
- `src/NPLogic.App/Views/AuctionPublicSaleView.xaml.cs` - SetViewModels에서 NotePanelColumn DataContext 설정, LostFocus/Toggle 핸들러
- `src/NPLogic.App/Views/InterimTab.xaml` - 2열 Grid 래핑, 비고 패널
- `src/NPLogic.App/Views/InterimTab.xaml.cs` - NoteTextBox_LostFocus 핸들러
- `src/NPLogic.App/Views/CashFlowSummaryView.xaml` - 2열 Grid 래핑, 비고 패널
- `src/NPLogic.App/Views/CashFlowSummaryView.xaml.cs` - NoteTextBox_LostFocus 핸들러
- `src/NPLogic.App/Views/XnpvComparisonView.xaml` - 2열 Grid 래핑, materialDesign xmlns 추가, 비고 패널
- `src/NPLogic.App/Views/XnpvComparisonView.xaml.cs` - NoteTextBox_LostFocus 핸들러
- `src/NPLogic.App/Views/ClosingTab.xaml` - 2열 Grid 래핑, code-behind 비고 패널, Loaded 이벤트
- `src/NPLogic.App/Views/ClosingTab.xaml.cs` - 전체 비고 관리 (Loaded/LoadNote/LostFocus/Toggle)

## Decisions Made
- ClosingTab은 DashboardView에서 `new { Property = property }` 익명 객체를 DataContext로 설정하므로, ViewModel 바인딩 불가. code-behind에서 Reflection으로 Property 추출 후 직접 Repository 사용
- InterimTab은 물건 ID가 없는 프로그램 레벨 탭이므로 programId를 비고 컨텍스트로 활용
- CashFlowSummary/XnpvComparison은 물건 컨텍스트 자체가 없으므로 AuthService 세션 userId를 비고 소유자 ID로 활용
- AuctionPublicSaleView는 두 개의 ViewModel을 사용하는 복합 뷰이므로, NotePanelColumn의 DataContext를 code-behind에서 명시적으로 auctionVm에 연결

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] LoanSheetViewModel 비고 코드가 잘못된 클래스(MciData)에 삽입됨**
- **Found during:** Task 1 (LoanSheetViewModel)
- **Issue:** 파일에 다수의 보조 클래스가 있어, 마지막 `}` 직전에 삽입 시 MciData 클래스 안에 들어감
- **Fix:** 올바른 위치(LoanSheetViewModel 클래스 본체 내)로 이동
- **Files modified:** src/NPLogic.App/ViewModels/LoanSheetViewModel.cs
- **Verification:** dotnet build 성공
- **Committed in:** 6a7b47b (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** 즉시 발견 및 수정. 스코프 변경 없음.

## Issues Encountered
- 빌드 시 MSB3027/MSB3021 파일 잠금 오류 발생 (vgc 프로세스) -- C# 컴파일 자체는 성공, exe 복사만 실패. 코드 오류 아님.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Task 3 (checkpoint:human-verify) 대기 중: 전체 10개 탭 비고 통합 검증 필요
- 빌드 성공 확인 완료
- Phase 2 기존 3개 탭 회귀 검증 필요 (담보물건/선순위/평가)

---
*Phase: 03-all-tabs-notes*
*Completed: 2026-03-24*

## Self-Check: PASSED

- All 17 modified files exist
- Commit 6a7b47b (Task 1) verified
- Commit f89c048 (Task 2) verified
- dotnet build: 0 CS errors
