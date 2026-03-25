# Requirements: v3.0 QA 질의/답변 테이블 시스템

**Defined:** 2026-03-25
**Core Value:** 원청에 대한 질의/답변을 물건별로 체계적으로 관리하고, 과거 이력을 포함해 한눈에 확인할 수 있다

## v1 Requirements

### UI — QA 팝업

- [ ] **QA-POP-01**: QA 팝업 내용을 n×4 DataGrid 표로 교체 (질의일자/질의내용/회신일자/답변내용)
- [ ] **QA-POP-02**: 과거 질의/답변 이력 누적 표시 (스크롤로 전체 확인)
- [ ] **QA-POP-03**: 신규 질의 입력 기능 (행 추가 + 질의일자 자동 설정)
- [ ] **QA-POP-04**: 답변 내용 입력/수정 기능 (회신일자 + 답변내용)

### UI — 전체 탭 QA 카드

- [ ] **QA-HOME-01**: 전체 탭 QA 요약 카드를 동일한 n×4 표 형식으로 교체
- [ ] **QA-HOME-02**: 현재 물건에 대한 QA만 필터하여 표시

### UI — QA집계 탭

- [ ] **QA-AGG-01**: QA집계 탭을 n×6 표로 교체 (차주번호/차주명/질의일자/질의내용/회신일자/답변내용)
- [ ] **QA-AGG-02**: 전체 차주 대상 QA 조회 (물건 필터 없이 전체)

## Out of Scope

| Feature | Reason |
|---------|--------|
| QA 알림/노티피케이션 | 별도 기능으로 분리 |
| QA 상태 관리 (진행중/완료) | v3에서는 단순 입력/조회 |
| QA 첨부파일 | 텍스트 기반만 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| QA-POP-01 | Phase 1 | Pending |
| QA-POP-02 | Phase 1 | Pending |
| QA-POP-03 | Phase 1 | Pending |
| QA-POP-04 | Phase 1 | Pending |
| QA-HOME-01 | Phase 2 | Pending |
| QA-HOME-02 | Phase 2 | Pending |
| QA-AGG-01 | Phase 2 | Pending |
| QA-AGG-02 | Phase 2 | Pending |

**Coverage:**
- v1 requirements: 8 total
- Mapped to phases: 8
- Unmapped: 0

---
*Requirements defined: 2026-03-25*
