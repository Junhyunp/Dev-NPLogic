# Requirements: NPLogic 평가 탭 UI/UX 통일

**Defined:** 2026-03-14
**Core Value:** 평가 유형 간 전환 시 사용자가 혼란 없이 자연스럽게 느끼는 일관된 UI/UX

## v1 Requirements

### 레이아웃

- [x] **LAYOUT-01**: 전체 5개 View의 DesignHeight를 동일하게 통일 (현재 800/900 혼재)
- [ ] **LAYOUT-02**: 좌우 패널 내 섹션 순서를 일관된 원칙으로 정리 (공통 섹션은 같은 위치)
- [x] **LAYOUT-03**: CardBorder Margin을 모든 섹션에서 동일하게 적용 (0,0,0,16)

### 섹션 헤더

- [x] **HDR-01**: 유형별 View 내 섹션 헤더 스타일을 SectionHeader 기준으로 통일 (이모지/아이콘 패턴 일관화)
- [ ] **HDR-02**: EvaluationTab.xaml의 평가결과 헤더(PrimaryBrush + PackIcon)와 개별 View 헤더 스타일 간 일관성 확보

### DataGrid

- [ ] **GRID-01**: 유사 용도의 DataGrid MaxHeight 통일 (데이터 조회용 vs 편집용 구분하여 표준화)
- [ ] **GRID-02**: DataGrid FontSize를 용도별로 통일 (FontSizeBody vs FontSizeSmall 기준 정립)
- [ ] **GRID-03**: DataGrid 공통 속성(RowHeight, BorderBrush, AlternatingRowBackground 등) 일관 적용

### 평가결과

- [ ] **EVAL-01**: 아파트/연립다세대 평가결과를 다른 유형과 같은 시나리오 헤더+DataGrid 패턴으로 통일
- [ ] **EVAL-02**: 4개 유형별 평가결과 섹션의 컬럼 구조/스타일 일관성 확보

### 여백/간격

- [x] **SPC-01**: Border 내부 Padding을 모든 섹션에서 동일하게 (16px)
- [x] **SPC-02**: 섹션 간 간격(Margin)을 전체 View에서 일관되게 적용

## v2 Requirements

### 리팩토링

- **REFACT-01**: 공통 섹션(낙찰통계, 사례평가, 탐문내역)을 재사용 가능한 UserControl로 추출
- **REFACT-02**: 평가결과 섹션을 템플릿화하여 유형별 차이만 ViewModel로 주입

## Out of Scope

| Feature | Reason |
|---------|--------|
| 새로운 기능 추가 | UI/UX 다듬기만 집중 |
| 비즈니스 로직 변경 | ViewModel/계산 로직은 그대로 유지 |
| 평가 탭 외 다른 탭 변경 | 범위 밖 (등기부등본, 권리분석 등) |
| 성능 최적화 | 이번 작업의 목표가 아님 |
| UserControl 추출 리팩토링 | v2로 이관 -- 현재는 스타일 통일만 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LAYOUT-01 | Phase 1 | Complete (01-01) |
| LAYOUT-02 | Phase 1 | Pending |
| LAYOUT-03 | Phase 1 | Complete (01-01) |
| HDR-01 | Phase 1 | Complete (01-01) |
| HDR-02 | Phase 1 | Pending |
| GRID-01 | Phase 2 | Pending |
| GRID-02 | Phase 2 | Pending |
| GRID-03 | Phase 2 | Pending |
| EVAL-01 | Phase 3 | Pending |
| EVAL-02 | Phase 3 | Pending |
| SPC-01 | Phase 1 | Complete (01-01) |
| SPC-02 | Phase 1 | Complete (01-01) |

**Coverage:**
- v1 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0

---
*Requirements defined: 2026-03-14*
*Last updated: 2026-03-14 after roadmap creation*
