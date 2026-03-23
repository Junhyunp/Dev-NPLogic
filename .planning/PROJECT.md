# NPLogic 프로젝트 관리

## Milestone History

### v1.0 — 평가 탭 UI/UX 통일 (완료)
평가/선순위/담보물건 탭 간 UI/UX 통일. PrimaryBrush 헤더, DataGrid 스타일, 여백/간격, 저장 버튼 통합, +/- 토글 등.

## Current Milestone

### v2.0 — 탭별 비고란 추가 및 종합 표시

모든 탭에 비고란(특이사항 메모)을 추가하고, 비핵심 > 전체 탭에서 탭별 비고를 종합 표시한다.

## Core Value

담당자가 각 탭에서 특이사항을 바로 메모하고, 전체 탭에서 한눈에 확인할 수 있다.

## Context

- 원청 피드백: "각 카테고리별 비고란이 있으면 좋겠다. 모든 탭에 비고가 있고, 있는 경우만 맨 앞(전체 탭)으로 끌고 와주면 된다."
- 대상: 비핵심 하위 8개 탭 + 등기부등본/권리분석/기초데이터/QA집계/현금흐름집계/NPV비교/마감 (총 15개+)
- DB: Supabase에 `property_notes` 테이블 신규 생성
- UI: 오른쪽 사이드 패널, +/- 토글로 접기/펼치기
- 저장: 기존 탭별 저장 버튼과 함께 저장

## Constraints

- **Tech stack**: WPF + XAML, Supabase PostgreSQL
- **UI 패턴**: 기존 확립된 PrimaryBrush 헤더, 사이드 패널 패턴 활용
- **저장 방식**: 기존 SaveAll 커맨드에 통합

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 새 테이블 property_notes | 물건별+탭별 유연한 구조, 기존 테이블 변경 불필요 | Decided |
| 사이드 패널 + 토글 | 메인 콘텐츠 방해 없이 필요할 때만 사용 | Decided |
| 전체 탭에 종합 표시 | 담당자가 한곳에서 모든 비고 확인 | Decided |
| 기존 저장 버튼과 통합 | 별도 저장 UX 불필요, 자연스러운 플로우 | Decided |

---
*Last updated: 2026-03-23 — v2.0 milestone initialized*
