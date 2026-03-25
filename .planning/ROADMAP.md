# Roadmap: v3.0 QA 질의/답변 테이블 시스템

## Overview

QA 팝업의 표 형식 교체와 CRUD 기능을 먼저 구현하여 핵심 상호작용 패턴을 확립한 뒤, 동일한 표 패턴을 전체 탭 QA 카드와 QA집계 탭에 적용하여 3곳의 QA UI를 통일한다.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: QA 팝업 표 형식 교체 + CRUD** - QA 팝업을 DataGrid 표로 교체하고 질의/답변 입력/수정 기능 구현
- [ ] **Phase 2: 전체 탭 QA 카드 + QA집계 탭** - HomeTab QA 카드와 QASummaryTab을 동일한 표 형식으로 통일

## Phase Details

### Phase 1: QA 팝업 표 형식 교체 + CRUD
**Goal**: 사용자가 QA 팝업에서 질의/답변을 표 형식으로 확인하고, 신규 질의 입력 및 답변 수정을 할 수 있다
**Depends on**: Nothing (first phase)
**Requirements**: QA-POP-01, QA-POP-02, QA-POP-03, QA-POP-04
**Success Criteria** (what must be TRUE):
  1. QA 팝업을 열면 질의일자/질의내용/회신일자/답변내용 4열 DataGrid 표가 보인다
  2. 해당 물건의 과거 질의/답변 이력이 모두 표에 누적 표시되고, 스크롤로 전체 확인할 수 있다
  3. 신규 질의 행을 추가하면 질의일자가 자동 설정되고, 질의내용을 입력하여 저장할 수 있다
  4. 기존 행의 회신일자와 답변내용을 입력/수정하여 저장할 수 있다
**Plans**: 2 plans

Plans:
- [ ] 01-01-PLAN.md — QA 팝업 DataGrid 4열 표 생성 + 과거 이력 누적 표시
- [ ] 01-02-PLAN.md — 신규 질의 입력(행 추가) + 답변 수정/저장 기능

### Phase 2: 전체 탭 QA 카드 + QA집계 탭
**Goal**: 사용자가 전체 탭과 QA집계 탭에서 동일한 표 형식으로 QA 데이터를 조회할 수 있다
**Depends on**: Phase 1
**Requirements**: QA-HOME-01, QA-HOME-02, QA-AGG-01, QA-AGG-02
**Success Criteria** (what must be TRUE):
  1. 전체 탭 QA 카드가 Phase 1과 동일한 n x 4 표 형식으로 표시된다
  2. 전체 탭 QA 카드에는 현재 선택된 물건의 QA만 필터되어 표시된다
  3. QA집계 탭이 차주번호/차주명/질의일자/질의내용/회신일자/답변내용 6열 표로 표시된다
  4. QA집계 탭에서는 물건 필터 없이 전체 차주의 QA를 조회할 수 있다
**Plans**: TBD

Plans:
- [ ] 02-01: TBD
- [ ] 02-02: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. QA 팝업 표 형식 교체 + CRUD | 0/2 | Planned | - |
| 2. 전체 탭 QA 카드 + QA집계 탭 | 0/? | Not started | - |
