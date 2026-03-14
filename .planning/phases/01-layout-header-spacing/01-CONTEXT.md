# Phase 1: 레이아웃/헤더/여백 기반 통일 - Context

**Gathered:** 2026-03-14
**Status:** Ready for planning

<domain>
## Phase Boundary

EvaluationTab.xaml의 5개 평가 유형 간 구조적 골격(DesignHeight, 섹션 헤더 스타일, CardBorder 여백/간격)을 일관되게 정리한다. 섹션 순서와 콘텐츠 내용은 변경하지 않으며, 기능 추가 없이 스타일 통일만 수행한다.

</domain>

<decisions>
## Implementation Decisions

### DesignHeight/Width
- DesignHeight를 900으로 통일 (현재 아파트/연립다세대 개별 View가 800이지만, 실제 사용되는 EvaluationTab.xaml은 이미 900)
- DesignWidth 1400 유지
- 좌우 패널 비율(*,16,*) — 해당 없음. EvaluationTab.xaml은 단일 열 레이아웃(ScrollViewer 하나)

### 레이아웃 구조
- EvaluationTab.xaml이 모든 콘텐츠를 인라인으로 포함하는 현재 구조 유지
- Views/Evaluation/ 폴더의 개별 View 파일들(EvaluationApartmentView.xaml 등)은 무시 (현재 미사용)
- 섹션 순서는 현재 상태 유지 — 변경하지 않음

### 헤더 스타일
- 모든 섹션 헤더를 이모지+텍스트 SectionHeader 스타일로 통일
- PrimaryBrush 배경 + PackIcon 헤더(평가결과, 지번별 평가 등)를 제거하고 일반 SectionHeader로 교체
- 이모지 유지 — 각 섹션의 기존 이모지 그대로 사용
- 평가결과 섹션에 적합한 이모지 선택은 Claude 재량

### 여백/간격
- CardBorder 아래 Margin: 0,0,0,16으로 전 섹션 통일
- CardBorder 내부 Padding: 16px 유지
- CardBorder Style 기본값(Margin=0,0,0,12)과 다르므로, 오버라이드 방식(인라인 Margin 지정) 또는 Style 기본값 수정은 Claude 재량

### Claude's Discretion
- CardBorder Style 기본 Margin을 12→16으로 변경할지, 각 섹션에서 개별 오버라이드할지
- PrimaryBrush 헤더 제거 후 평가결과 섹션의 이모지 선택
- 헤더 간 Margin/Padding 미세 조정

</decisions>

<code_context>
## Existing Code Insights

### 핵심 파일
- `src/NPLogic.App/Views/EvaluationTab.xaml` — 모든 평가 탭 UI (3500+ lines). 수정 대상
- `src/NPLogic.UI/Styles/Typography.xaml` — SectionHeader, CardBorder 등 스타일 정의. 필요 시 수정

### Established Patterns
- **SectionHeader 스타일**: FontSizeH4 (14pt), SemiBold, Margin 0,0,0,12
- **CardBorder 스타일**: Background=SurfaceBrush, CornerRadius=8, Padding=16, Margin=0,0,0,12
- **Visibility 패턴**: IsApartmentType, IsCommercialType 등 bool 바인딩으로 유형별 표시/숨김

### PrimaryBrush 헤더 위치 (제거 대상)
- 평가결과 (아파트/연립다세대 전용) — line ~2386
- 평가결과 (공장/창고 전용) — line ~2487
- 평가결과 (상가/아파트형공장 전용) — line ~2706
- 평가결과 (주택/근린시설/토지/기타 전용) — line ~2885
- 지번별 평가 (공장/창고 전용) — line ~3029

### Integration Points
- EvaluationTabViewModel.cs — 바인딩 변경 없음 (UI만 수정)
- Typography.xaml — CardBorder 기본 Margin 변경 시 다른 탭에도 영향 가능 → 인라인 오버라이드 권장

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches. 핵심은 유형 전환 시 자연스러움.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-layout-header-spacing*
*Context gathered: 2026-03-14*
