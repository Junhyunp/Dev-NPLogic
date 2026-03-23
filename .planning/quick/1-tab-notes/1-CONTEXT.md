# Quick Task 1: 각 탭마다 비고란 추가 + 전체 탭에서 종합 표시 - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Task Boundary

모든 탭에 비고란(특이사항 메모) 추가. 각 탭에서 입력한 비고를 비핵심 > 전체 탭에서 종합 표시.

</domain>

<decisions>
## Implementation Decisions

### 대상 탭 범위
- 전체 탭에 비고란 추가: 비핵심 하위 8개(전체/차주개요/Loan/담보물건/선순위/평가/경공매일정/인터림) + 등기부등본/권리분석/기초데이터/QA집계/현금흐름집계/NPV비교/마감
- 종합 표시 위치: 비핵심 > 전체 탭(HomeTab)

### 비고란 UI 위치/방식
- 위치: 오른쪽 사이드 패널 (접기/펼치기 가능)
- 표시 방식: +/- 토글로 접기/펼치기
- TextBox로 자유 입력, 여러 줄 가능 (TextWrapping, AcceptsReturn)

### DB 저장 구조
- 새 테이블: `property_notes` (property_id UUID, tab_name TEXT, note_text TEXT, updated_at TIMESTAMPTZ)
- property_id + tab_name으로 유니크 (물건별+탭별 1개 비고)

### 저장 타이밍
- 기존 탭별 저장 버튼과 함께 저장 (SaveAll에 비고 저장 포함)

### Claude's Discretion
- 사이드 패널 너비, 토글 버튼 위치
- 전체 탭 종합 표시의 레이아웃 디자인
- tab_name 값 규칙 (영문 소문자 snake_case 등)

</decisions>

<specifics>
## Specific Ideas

- 전체 탭 종합: 비고가 있는 탭만 표시, 없는 탭은 생략 또는 "(비고 없음)"
- 사이드 패널은 기존 디자인(담보 목록 사이드바)과 유사한 톤

</specifics>
