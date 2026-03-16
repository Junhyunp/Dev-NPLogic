# Roadmap: NPLogic 평가 탭 UI/UX 통일

## Overview

5개 평가 유형(아파트, 연립다세대, 상가/아파트형공장, 공장/창고, 주택/근린시설/토지/기타)의 XAML 스타일을 3단계로 통일한다. 먼저 레이아웃/여백/헤더 등 구조적 기반을 잡고, 그 위에 DataGrid 스타일을 표준화한 뒤, 마지막으로 유형별 평가결과 섹션의 패턴을 통일한다.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: 레이아웃/헤더/여백 기반 통일** - 5개 View의 구조적 골격(DesignHeight, 패널 비율, CardBorder, 헤더, 여백)을 일관되게 정리
- [ ] **Phase 2: DataGrid 스타일 표준화** - 용도별 DataGrid 속성(MaxHeight, FontSize, RowHeight 등)을 표준 기준으로 통일
- [x] **Phase 3: 평가결과 섹션 패턴 통일** - 아파트/연립다세대 포함 전 유형의 평가결과 섹션을 동일한 시나리오 헤더+DataGrid 패턴으로 정리 (completed 2026-03-16)

## Phase Details

### Phase 1: 레이아웃/헤더/여백 기반 통일
**Goal**: 어떤 평가 유형을 열어도 전체 레이아웃 구조, 섹션 헤더 모양, 여백 간격이 동일하게 느껴진다
**Depends on**: Nothing (first phase)
**Requirements**: LAYOUT-01, LAYOUT-02, LAYOUT-03, HDR-01, HDR-02, SPC-01, SPC-02
**Success Criteria** (what must be TRUE):
  1. 5개 View 모두 동일한 DesignHeight 값을 가지며, 좌우 패널 비율이 일관된다
  2. 공통 섹션(낙찰통계, 사례평가, 탐문내역 등)이 모든 유형에서 같은 패널/순서에 배치되어 있다
  3. 모든 CardBorder의 Margin이 0,0,0,16으로 통일되고, 내부 Padding이 16px로 일관된다
  4. 섹션 헤더가 SectionHeader 스타일 기준으로 통일되며, 이모지/아이콘 패턴이 일관적이다
  5. 평가결과 헤더(PrimaryBrush+PackIcon)와 개별 View 헤더 간 시각적 일관성이 확보된다
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md -- CardStyle Margin 업데이트 + 공통 섹션 6개 PrimaryBrush 헤더 변환
- [ ] 01-02-PLAN.md -- 유형별 전용 섹션 16개 헤더 변환 + 전체 시각적 검증

### Phase 2: DataGrid 스타일 표준화
**Goal**: 전 유형의 DataGrid가 용도별로 일관된 크기, 폰트, 행 높이, 테두리를 갖는다
**Depends on**: Phase 1
**Requirements**: GRID-01, GRID-02, GRID-03
**Success Criteria** (what must be TRUE):
  1. 데이터 조회용 DataGrid와 편집용 DataGrid의 MaxHeight가 각각 표준 값으로 통일되어 있다
  2. DataGrid FontSize가 용도에 따라 FontSizeBody 또는 FontSizeSmall로 일관되게 적용된다
  3. RowHeight, BorderBrush, AlternatingRowBackground 등 공통 속성이 전 유형에서 동일하다
**Plans**: 1 plan

Plans:
- [ ] 02-01-PLAN.md -- 15개 DataGrid 공통 속성 통일 (AlternatingRowBackground, BorderThickness, BorderBrush) + 시각적 검증

### Phase 3: 평가결과 섹션 패턴 통일
**Goal**: 5개 유형 모두 평가결과 섹션이 동일한 시각적 패턴(시나리오 헤더+DataGrid)을 따르며, 유형별 내용 차이만 존재한다
**Depends on**: Phase 2
**Requirements**: EVAL-01, EVAL-02
**Success Criteria** (what must be TRUE):
  1. 아파트/연립다세대 평가결과가 다른 유형과 같은 시나리오 헤더+DataGrid 패턴을 사용한다
  2. 4개 유형별 평가결과 섹션의 컬럼 정렬, 셀 스타일, 헤더 스타일이 시각적으로 일관된다
  3. 평가 유형을 전환할 때 평가결과 섹션의 레이아웃 패턴이 자연스럽게 연속되어 보인다
**Plans**: TBD

Plans:
- [ ] 03-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. 레이아웃/헤더/여백 기반 통일 | 1/2 | In Progress | - |
| 2. DataGrid 스타일 표준화 | 0/1 | Not started | - |
| 3. 평가결과 섹션 패턴 통일 | 0/? | Complete    | 2026-03-16 |
