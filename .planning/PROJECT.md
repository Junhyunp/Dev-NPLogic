# NPLogic 프로젝트 관리

## Milestone History

### v1.0 — 평가 탭 UI/UX 통일 (완료)
평가/선순위/담보물건 탭 간 UI/UX 통일.

### v2.0 — 탭별 비고란 추가 및 종합 표시 (완료)
모든 탭에 비고 사이드 패널 + 전체 탭 종합 표시.

## Current Milestone

### v3.0 — QA 질의/답변 테이블 시스템

QA 팝업, 전체 탭 QA 카드, QA집계 탭을 표 형식(질의일자/질의내용/회신일자/답변내용)으로 통일하고, 과거 이력 누적 조회 + 신규 입력 기능을 제공한다.

## Core Value

원청에 대한 질의/답변을 물건별로 체계적으로 관리하고, 과거 이력을 포함해 한눈에 확인할 수 있다.

## Context

- 기존 PropertyQa 모델/Repository/DB 테이블이 이미 존재 (Question, Answer, CreatedAt, AnsweredAt, Part, BorrowerNumber, BorrowerName)
- QA 팝업: NonCoreView 우측 QA 버튼으로 열림
- 전체 탭 QA 카드: HomeTab 하단
- QA집계 탭: QASummaryTab — 전체 차주 대상 집계
- 원청 요구: 과거 질의도 누적 표시, 연장 질의 가능, 회신에 대한 재질의 가능

## Constraints

- **Tech stack**: WPF + XAML, Supabase PostgreSQL
- **기존 DB 활용**: property_qa 테이블 이미 존재
- **UI 패턴**: 기존 확립된 표 스타일(Height=30, BlueGray100 헤더 등) 활용

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 기존 property_qa 테이블 활용 | 이미 필요한 필드 대부분 존재 | Decided |
| 3곳 UI 통일 (팝업/전체탭/QA집계) | 일관된 표 형식 | Decided |
| QA집계에 차주번호/차주명 추가 열 | 전체 조회 시 차주 식별 필요 | Decided |

---
*Last updated: 2026-03-25 — v3.0 milestone initialized*
