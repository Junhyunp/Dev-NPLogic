---
phase: 01-qa-popup-table
plan: 01
subsystem: ui
tags: [wpf, datagrid, popup, qa, xaml]

# Dependency graph
requires: []
provides:
  - "QAPopupWindow with DataGrid 4-column read-only QA history table"
  - "NonCoreView QAButton_Click wired to QAPopupWindow"
affects: [01-02-PLAN]

# Tech tracking
tech-stack:
  added: []
  patterns: [window-popup-with-datagrid, code-behind-repository-load]

key-files:
  created:
    - src/NPLogic.App/Views/QAPopupWindow.xaml
    - src/NPLogic.App/Views/QAPopupWindow.xaml.cs
  modified:
    - src/NPLogic.App/Views/NonCoreView.xaml.cs

key-decisions:
  - "DataGrid styles duplicated in Window.Resources (not shared resource dictionary) to keep popup self-contained"
  - "QA history sorted CreatedAt ascending (oldest first, newest at bottom) for chronological reading"
  - "QAInputDialog files preserved (not deleted) for safety"

patterns-established:
  - "QAPopupWindow pattern: constructor(propertyId, borrowerNumber, borrowerName) + Loaded event async data load"
  - "DataGrid 4-column layout: 질의일자(100px)/질의내용(3*)/회신일자(100px)/답변내용(3*)"

requirements-completed: [QA-POP-01, QA-POP-02]

# Metrics
duration: 5min
completed: 2026-03-25
---

# Phase 1 Plan 01: QA Popup DataGrid Table Summary

**QAPopupWindow with 4-column DataGrid (질의일자/질의내용/회신일자/답변내용) replacing QAInputDialog, loading QA history via PropertyQaRepository**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-25T08:57:58Z
- **Completed:** 2026-03-25T09:03:38Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- QAPopupWindow.xaml with 4-column DataGrid matching project's SectionDataGrid style (BlueGray100 headers, 30px rows)
- Code-behind loads QA history from PropertyQaRepository on Loaded event, sorted chronologically
- NonCoreView QAButton_Click replaced to open QAPopupWindow with SelectedPropertyTab data
- Guard added for unselected property with user-friendly message

## Task Commits

Each task was committed atomically:

1. **Task 1: QAPopupWindow XAML + code-behind creation** - `6c5a1cd` (feat)
2. **Task 2: NonCoreView QAButton_Click replacement** - `69ae064` (feat)

## Files Created/Modified
- `src/NPLogic.App/Views/QAPopupWindow.xaml` - DataGrid 4-column popup window with styles
- `src/NPLogic.App/Views/QAPopupWindow.xaml.cs` - Code-behind with PropertyQaRepository data loading
- `src/NPLogic.App/Views/NonCoreView.xaml.cs` - QAButton_Click updated to open QAPopupWindow

## Decisions Made
- DataGrid styles duplicated in Window.Resources rather than referencing shared dictionary, keeping the popup self-contained and avoiding cross-file dependencies
- QA history sorted CreatedAt ascending (oldest first at top, newest at bottom) for natural chronological reading flow
- QAInputDialog.xaml/cs files preserved (not deleted) since they may be referenced elsewhere

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build reported 12 MSB3027/MSB3021 file lock errors due to Visual Studio (PID 5832) and running NPLogic.App (PID 25772) locking output DLLs/PDBs. These are environment-related, not code errors. C# compilation (CS errors) was zero throughout.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- QAPopupWindow ready for Plan 02 to add row insertion (new QA question) and answer editing/saving
- ObservableCollection _qaItems field already prepared for Plan 02 data manipulation
- Footer StackPanel has space reserved for "행 추가" and "저장" buttons

## Self-Check: PASSED

All files exist, all commits verified.

---
*Phase: 01-qa-popup-table*
*Completed: 2026-03-25*
