# NPLogic 평가 탭 UI/UX 통일

## What This Is

NPLogic WPF 애플리케이션의 평가(Evaluation) 탭에서 5개 평가 유형(아파트, 연립다세대, 상가/아파트형공장, 공장/창고, 주택/근린시설/토지/기타) 간 UI/UX를 통일하고 전반적인 마무리 품질을 높이는 프로젝트. 내부 평가사가 어떤 유형을 보든 동일한 품질감과 일관된 레이아웃을 경험하도록 한다.

## Core Value

평가 유형 간 전환 시 사용자가 혼란 없이 자연스럽게 느끼는 일관된 UI/UX.

## Requirements

### Validated

- ✓ 5개 평가 유형별 전용 View/ViewModel 분리 — existing
- ✓ 유형별 RadioButton 전환 메커니즘 — existing
- ✓ 아파트/연립다세대: 실거래가, 유사물건 추천, 사례평가, 낙찰통계, 탐문내역 — existing
- ✓ 상가/아파트형공장: 상가구분, 임대동향, 층별효용비율, 수익가치, 무상임대분석 — existing
- ✓ 공장/창고: 공시지가, 지번별 평가, 기계기구 목록 — existing
- ✓ 주택/근린시설/토지/기타: 탐문결과, 탐문수익가치, 수익가치 — existing
- ✓ 전 유형 평가결과 섹션 — existing
- ✓ CardBorder 기반 섹션 구분 — existing
- ✓ SectionHeader 스타일 — existing

### Active

- [ ] 공통 섹션(낙찰통계, 사례평가, 탐문내역 등) 레이아웃/스타일 일관성 확보
- [ ] 좌우 패널 비율 및 전체 레이아웃 구조 통일
- [ ] 섹션 헤더 스타일 통일 (이모지, 폰트, 크기, 여백)
- [ ] DataGrid 스타일 통일 (컬럼 포맷, 행 높이, 테두리, 정렬)
- [ ] 여백(Margin/Padding) 및 간격(Spacing) 일관성
- [ ] 평가결과 섹션 유형별 내용은 다르되 스타일/레이아웃 패턴 통일

### Out of Scope

- 새로운 기능 추가 — UI/UX 다듬기만 집중
- 비즈니스 로직 변경 — ViewModel/계산 로직은 그대로
- 평가 탭 외 다른 탭(등기부등본, 권리분석 등) — 범위 밖
- 성능 최적화 — 이번 작업의 목표가 아님

## Context

- 5개 평가 유형이 점진적으로 개발되어 각 유형의 스타일이 조금씩 다른 상태
- 최근 커밋(c179873, eb86462, 61ea626, ecd6753)에서 유형별 평가결과 섹션을 추가 중
- MaterialDesignThemes 4.9.0 기반 디자인 시스템 사용
- 각 View는 좌우 2패널 레이아웃이 기본이나 세부 구현이 다름
- 내부 평가사들이 실무에서 사용 중인 도구

## Constraints

- **Tech stack**: WPF + XAML, 기존 MaterialDesign 테마 유지
- **Timeline**: 당장 필요 — 빠르게 완료해야 함
- **Scope**: 기능 변경 없이 시각적 통일만 — 기존 동작 깨뜨리면 안 됨
- **Compatibility**: 현재 데이터 바인딩, 커맨드 구조 유지

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| UI/UX만 집중, 기능 추가 없음 | 빠른 마무리가 필요하고 기능은 이미 동작 중 | — Pending |
| 기존 MaterialDesign 테마 활용 | 새 디자인 시스템 도입은 범위 밖 | — Pending |

---
*Last updated: 2026-03-14 after initialization*
