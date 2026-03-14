---
phase: 01-layout-header-spacing
plan: 01
subsystem: ui
tags: [xaml, wpf, card-style, section-header, emoji, evaluation-tab]

# Dependency graph
requires: []
provides:
  - "CardStyle에 Margin=0,0,0,16 Setter 추가 (전체 섹션 간격 기반)"
  - "공통 섹션 6개(상가구분 + 사례지도/사례평가/사례로드뷰/경매사건검색/낙찰통계) CardStyle+SectionHeader 변환"
affects: [01-02-PLAN, phase-2]

# Tech tracking
tech-stack:
  added: []
  patterns: ["CardStyle로 외곽 Border 통일", "SectionHeader + 이모지 TextBlock 헤더 패턴", "XML entity로 이모지 삽입 (&#x1F4CB;)"]

key-files:
  created: []
  modified:
    - src/NPLogic.App/Views/EvaluationTab.xaml

key-decisions:
  - "CardBorder 기본 Margin(0,0,0,12)은 변경하지 않고 로컬 CardStyle에서만 0,0,0,16으로 오버라이드"
  - "이모지를 별도 TextBlock으로 분리하여 ViewModel Binding 텍스트와 독립 배치 (사례지도 패턴)"
  - "낙찰통계 내부 테이블 셀의 PrimaryBrush는 섹션 헤더가 아니므로 유지"

patterns-established:
  - "CardStyle 패턴: 외곽 Border에 Style={StaticResource CardStyle} 적용으로 Padding=16, Margin=0,0,0,16 자동 부여"
  - "SectionHeader 패턴: 이모지 TextBlock + 텍스트 TextBlock (Style=SectionHeader, Margin=0,0,0,12)"
  - "버튼 포함 헤더: Grid 안에 StackPanel(이모지+텍스트) + Button(HorizontalAlignment=Right)"

requirements-completed: [LAYOUT-01, LAYOUT-03, SPC-01, SPC-02, HDR-01]

# Metrics
duration: 3min
completed: 2026-03-14
---

# Phase 1 Plan 1: CardStyle Margin + 공통 섹션 SectionHeader 변환 Summary

**EvaluationTab.xaml 로컬 CardStyle에 Margin=0,0,0,16 추가 후, 공통 섹션 6개(상가구분+사례지도/사례평가/사례로드뷰/경매사건검색/낙찰통계)의 PrimaryBrush 헤더를 이모지+SectionHeader 패턴으로 변환**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-14T08:19:39Z
- **Completed:** 2026-03-14T08:23:10Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- CardStyle에 Margin=0,0,0,16 Setter 추가로 전체 섹션 간격 기반 확립
- 상가구분 Border를 인라인 속성에서 CardStyle로 변환 (Margin 0,0,0,8 -> 0,0,0,16 통일)
- 5개 공통 섹션의 PrimaryBrush+PackIcon 헤더를 이모지+SectionHeader 텍스트로 교체
- 5개 공통 섹션 외곽 Border를 CardStyle로 변환 (인라인 속성 5개 제거)
- 낙찰통계 내부 테이블 셀의 PrimaryBrush 정상 유지 확인

## Task Commits

Each task was committed atomically:

1. **Task 1: CardStyle Margin 업데이트 + 상가구분 Border 통일 + DesignHeight 확인** - `b5aea7f` (feat)
2. **Task 2: 공통 섹션 5개 PrimaryBrush 헤더를 SectionHeader로 변환** - `cb92e90` (feat)

## Files Created/Modified
- `src/NPLogic.App/Views/EvaluationTab.xaml` - CardStyle Margin 추가, 상가구분+공통5개 섹션 헤더 변환

## Decisions Made
- CardBorder 기본 Margin(0,0,0,12)은 변경하지 않고 로컬 CardStyle에서만 0,0,0,16으로 오버라이드 (다른 탭에 영향 방지)
- 사례지도 섹션의 CaseMapSectionTitle Binding을 유지하기 위해 이모지를 별도 TextBlock으로 분리
- 낙찰통계 내부 테이블 셀의 PrimaryBrush(적용낙찰가율 헤더)는 섹션 헤더가 아니므로 변환하지 않음

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- 공통 섹션 6개 변환 완료, 01-02-PLAN.md의 유형별 전용 섹션 16개 헤더 변환 준비 완료
- CardStyle + SectionHeader 패턴이 확립되어 나머지 섹션에 동일 패턴 적용 가능

## Self-Check: PASSED

- FOUND: src/NPLogic.App/Views/EvaluationTab.xaml
- FOUND: .planning/phases/01-layout-header-spacing/01-01-SUMMARY.md
- FOUND: b5aea7f (Task 1 commit)
- FOUND: cb92e90 (Task 2 commit)

---
*Phase: 01-layout-header-spacing*
*Completed: 2026-03-14*
