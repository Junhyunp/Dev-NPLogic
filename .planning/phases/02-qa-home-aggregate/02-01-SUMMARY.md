---
phase: 02-qa-home-aggregate
plan: 01
subsystem: ui
tags: [wpf, datagrid, xaml, qa, mvvm]

# Dependency graph
requires:
  - phase: 01-qa-popup-table
    provides: DataGrid 4-column style pattern (BlueGray100Brush header, 30px row, Phase 1 styles)
provides:
  - HomeTab QA summary card replaced with 4-column DataGrid (질의일자/질의내용/회신일자/답변내용)
  - QASummaryTab right panel replaced with 6-column DataGrid (차주번호/차주명 + 4 QA columns)
  - Unified DataGrid style across all 3 QA views (popup/home/aggregate)
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Phase 1 DataGrid style applied to HomeTab and QASummaryTab for 3-view consistency"

key-files:
  created: []
  modified:
    - src/NPLogic.App/Views/HomeTab.xaml
    - src/NPLogic.App/Views/QASummaryTab.xaml

key-decisions:
  - "HomeTab MaxHeight=250 to prevent QA card from dominating HomeTab scroll area"
  - "QASummaryTab: 파트/상태 열 제거, 차주명 열 추가 (7열 -> 6열 간소화)"

patterns-established:
  - "3-view QA DataGrid consistency: popup(4열), home(4열), aggregate(6열=차주번호/차주명+4열)"

requirements-completed: [QA-HOME-01, QA-HOME-02, QA-AGG-01, QA-AGG-02]

# Metrics
duration: 5min
completed: 2026-03-25
---

# Phase 02 Plan 01: QA Home/Aggregate DataGrid Summary

**HomeTab QA 요약 카드를 4열 DataGrid 표로, QASummaryTab을 6열(차주번호/차주명 포함) DataGrid 표로 교체하여 3곳 QA UI 스타일 통일**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-25T09:37:10Z
- **Completed:** 2026-03-25T09:42:45Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- HomeTab Row 4 QA 요약: 6개 카테고리별 카운트 카드를 질의일자/질의내용/회신일자/답변내용 4열 DataGrid 표로 교체
- QASummaryTab 우측 패널: 7열(파트/상태 포함) DataGrid를 6열(차주번호/차주명/질의일자/질의내용/회신일자/답변내용) DataGrid 표로 교체
- 3곳 QA UI(팝업/전체탭/QA집계) 모두 Phase 1과 동일한 DataGrid 스타일 적용 (BlueGray100Brush 헤더, 30px row)
- ViewModel/code-behind 수정 불필요 - 기존 QaList 바인딩 활용

## Task Commits

Each task was committed atomically:

1. **Task 1: HomeTab QA 요약 카드를 4열 DataGrid로 교체** - `37b9951` (feat)
2. **Task 2: QASummaryTab 우측 패널을 6열 DataGrid로 교체** - `f7fa8ee` (feat)

**Plan metadata:** (pending) (docs: complete plan)

## Files Created/Modified
- `src/NPLogic.App/Views/HomeTab.xaml` - Row 4 QA 요약 섹션: 6개 카테고리 카운트 카드 -> 4열 DataGrid 표
- `src/NPLogic.App/Views/QASummaryTab.xaml` - 우측 패널 DataGrid: 7열(기존) -> 6열(차주번호/차주명 + 4 QA열)

## Decisions Made
- HomeTab DataGrid에 MaxHeight="250" 설정하여 HomeTab 전체 스크롤 내 QA 카드 크기 제한
- QASummaryTab에서 파트(Part) 열과 상태(IsAnswered) 열 제거 - 요구사항에 없으며 6열로 간소화
- QASummaryTab에 차주명(BorrowerName) 열 추가 - QA집계에서 차주 식별 요구사항 충족

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Visual Studio 프로세스(PID 5832)가 PDB/DLL 파일 잠금 → MSB3027/MSB3021 복사 오류 발생. 실제 XAML/C# 컴파일 오류는 0건으로 코드 변경은 정상.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- 3곳 QA UI(팝업/전체탭/QA집계) DataGrid 표 형식 통일 완료
- ViewModel/code-behind 변경 없이 XAML만 교체하여 기존 기능 완전 보존
- 추가 QA 관련 기능 확장 시 동일한 DataGrid 스타일 패턴 적용 가능

## Self-Check: PASSED

- FOUND: src/NPLogic.App/Views/HomeTab.xaml
- FOUND: src/NPLogic.App/Views/QASummaryTab.xaml
- FOUND: .planning/phases/02-qa-home-aggregate/02-01-SUMMARY.md
- FOUND: 37b9951 (Task 1 commit)
- FOUND: f7fa8ee (Task 2 commit)

---
*Phase: 02-qa-home-aggregate*
*Completed: 2026-03-25*
