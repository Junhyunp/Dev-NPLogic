# Roadmap: NPLogic v2.0 탭별 비고란 추가 및 종합 표시

## Overview

모든 탭에 비고란(특이사항 메모) 사이드 패널을 추가하고, 전체 탭에서 종합 표시한다. 먼저 DB/모델 기반을 만들고, 1~2개 탭에 사이드 패널 프로토타입을 검증한 뒤, 나머지 탭 전체로 확산하고, 마지막으로 전체 탭에서 비고를 종합 표시한다.

## Milestones

- v1.0 평가 탭 UI/UX 통일 (shipped 2026-03-16)
- v2.0 탭별 비고란 추가 및 종합 표시 (in progress)

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: DB 및 데이터 기반** - property_notes 테이블, 모델, Repository CRUD 구현
- [ ] **Phase 2: 사이드 패널 비고란 프로토타입** - 1~2개 탭에서 사이드 패널 UI 패턴 검증 (토글, 입력, 저장)
- [ ] **Phase 3: 전체 탭 비고란 적용** - 비핵심 8개 + 상위 7개 전체 탭에 비고란 확산 적용
- [ ] **Phase 4: 전체 탭 종합 표시** - 비핵심 > 전체 탭(HomeTab)에서 탭별 비고 종합 표시

## Phase Details

### Phase 1: DB 및 데이터 기반
**Goal**: 비고 데이터를 저장하고 조회할 수 있는 기반이 존재한다
**Depends on**: Nothing (first phase)
**Requirements**: DB-01, DB-02
**Success Criteria** (what must be TRUE):
  1. Supabase에 property_notes 테이블이 존재하고, property_id + tab_name 유니크 제약이 동작한다
  2. PropertyNote 모델로 비고를 생성/조회/수정/삭제할 수 있고, Repository 메서드가 정상 동작한다
  3. 특정 물건(property_id)의 전체 비고를 한번에 조회할 수 있다
**Plans:** 1 plan

Plans:
- [ ] 01-01-PLAN.md — Supabase 테이블 생성 + PropertyNote 모델/Repository/DI 구현

### Phase 2: 사이드 패널 비고란 프로토타입
**Goal**: 담당자가 담보물건/선순위/평가 3개 탭에서 비고를 입력하고 저장할 수 있다
**Depends on**: Phase 1
**Requirements**: NOTE-01, NOTE-02, NOTE-03
**Success Criteria** (what must be TRUE):
  1. 탭 오른쪽에 +/- 토글 버튼이 있고, 클릭하면 사이드 패널이 열리고 닫힌다
  2. 사이드 패널 TextBox에 여러 줄 텍스트를 자유롭게 입력할 수 있다
  3. 기존 저장 버튼을 누르면 비고 내용이 DB에 저장되고, 탭 재진입 시 저장된 내용이 복원된다
  4. 사이드 패널이 닫힌 상태에서도 메인 콘텐츠 레이아웃이 정상적이다
**Plans:** 1 plan

Plans:
- [ ] 02-01-PLAN.md — 담보물건/선순위/평가 3개 탭에 사이드 패널 비고란 추가 (ViewModel + View + 저장/로드 통합)

### Phase 3: 전체 탭 비고란 적용
**Goal**: 비핵심 하위 8개 탭과 상위 7개 탭 모두에서 비고란을 사용할 수 있다
**Depends on**: Phase 2
**Requirements**: SCOPE-01, SCOPE-02
**Success Criteria** (what must be TRUE):
  1. 비핵심 하위 8개 탭(전체/차주개요/Loan/담보물건/선순위/평가/경공매일정/인터림) 모두에 사이드 패널 비고란이 동작한다
  2. 상위 7개 탭(등기부등본/권리분석/기초데이터/QA집계/현금흐름집계/NPV비교/마감) 모두에 사이드 패널 비고란이 동작한다
  3. 각 탭에서 입력한 비고가 탭별로 독립적으로 저장/조회된다 (탭 간 간섭 없음)
  4. 탭 전환 시 각 탭의 비고가 올바르게 로드된다
**Plans**: TBD

Plans:
- [ ] 03-01: TBD

### Phase 4: 전체 탭 종합 표시
**Goal**: 담당자가 비핵심 > 전체 탭 한곳에서 모든 탭의 비고를 한눈에 확인할 수 있다
**Depends on**: Phase 3
**Requirements**: SUMMARY-01, SUMMARY-02
**Success Criteria** (what must be TRUE):
  1. 비핵심 > 전체 탭(HomeTab)에 탭별 비고 종합 섹션이 표시된다
  2. 비고가 있는 탭만 표시되고, 비고가 없는 탭은 생략된다
  3. 물건을 전환하면 해당 물건의 비고 종합이 즉시 갱신된다
**Plans**: TBD

Plans:
- [ ] 04-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. DB 및 데이터 기반 | 1/1 | Complete | 2026-03-23 |
| 2. 사이드 패널 비고란 프로토타입 | 0/1 | Not started | - |
| 3. 전체 탭 비고란 적용 | 0/? | Not started | - |
| 4. 전체 탭 종합 표시 | 0/? | Not started | - |
