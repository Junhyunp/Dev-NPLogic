# Requirements: v2.0 탭별 비고란 추가 및 종합 표시

**Defined:** 2026-03-23
**Core Value:** 담당자가 각 탭에서 특이사항을 바로 메모하고, 전체 탭에서 한눈에 확인할 수 있다

## v1 Requirements

### DB/데이터

- [x] **DB-01**: Supabase에 `property_notes` 테이블 생성 (property_id, tab_name, note_text, updated_at)
- [x] **DB-02**: PropertyNote 모델 + PropertyNoteRepository (CRUD) 구현

### UI — 개별 탭 비고란

- [ ] **NOTE-01**: 각 탭 오른쪽에 사이드 패널 형태 비고란 추가 (+/- 토글로 접기/펼치기)
- [ ] **NOTE-02**: TextBox로 여러 줄 자유 입력 가능 (TextWrapping, AcceptsReturn)
- [ ] **NOTE-03**: 기존 저장 버튼(SaveAll 등)에 비고 저장 통합

### UI — 전체 탭 종합

- [ ] **SUMMARY-01**: 비핵심 > 전체 탭(HomeTab)에 탭별 비고 종합 섹션 추가
- [ ] **SUMMARY-02**: 비고가 있는 탭만 표시 (없는 탭은 생략 또는 "(비고 없음)")

### 대상 탭 범위

- [x] **SCOPE-01**: 비핵심 하위 8개 탭 (전체/차주개요/Loan/담보물건/선순위/평가/경공매일정/인터림)
- [x] **SCOPE-02**: 상위 탭 7개 (등기부등본/권리분석/기초데이터/QA집계/현금흐름집계/NPV비교/마감)

## Out of Scope

| Feature | Reason |
|---------|--------|
| 비고 히스토리/버전 관리 | v2에서는 최신 1개만 저장 |
| 비고 기반 알림/알려줌 | 단순 메모 기능으로 충분 |
| 비고 검색 기능 | 전체 탭 종합으로 대체 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DB-01 | Phase 1 | Complete |
| DB-02 | Phase 1 | Complete |
| NOTE-01 | Phase 2 | Pending |
| NOTE-02 | Phase 2 | Pending |
| NOTE-03 | Phase 2 | Pending |
| SCOPE-01 | Phase 3 | Complete |
| SCOPE-02 | Phase 3 | Complete |
| SUMMARY-01 | Phase 4 | Pending |
| SUMMARY-02 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 9 total
- Mapped to phases: 9
- Unmapped: 0

---
*Requirements defined: 2026-03-23*
