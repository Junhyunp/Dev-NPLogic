# Phase 2: 사이드 패널 비고란 프로토타입 - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

담보물건, 선순위, 평가 3개 탭에 오른쪽 사이드 패널 비고란을 추가한다. Phase 1에서 만든 property_notes DB/Repository를 연결하여 저장/로드까지 구현한다.

</domain>

<decisions>
## Implementation Decisions

### 프로토타입 대상 탭
- 담보물건 (CollateralPropertyView), 선순위 (SeniorRightsView), 평가 (EvaluationTab) 3개 탭
- 3개 탭 모두 SaveAll 커맨드가 이미 있어 비고 저장 통합 용이

### 사이드 패널 디자인
- 너비: 250px
- 열림/닫힘: 즉시 표시/숨김 (애니메이션 없음)
- 위치: 탭 콘텐츠 오른쪽
- 패널 내부: 📝 비고 헤더 + TextBox (여러 줄, TextWrapping, AcceptsReturn)
- 패널 스타일: 기존 디자인(담보 목록 사이드바)과 유사한 톤

### 토글 버튼
- 위치: 탭 제일 상단 오른쪽 (저장 버튼 옆)
- 디자인: MaterialDesign PackIcon (NoteEdit 또는 NotePlus) — 클릭하면 사이드 패널 토글
- 패널 열린 상태에서는 아이콘 변경 또는 하이라이트로 상태 표시

### 저장 연동
- 기존 SaveAll 커맨드에 비고 저장(PropertyNoteRepository.UpsertAsync) 통합
- 탭 로드 시 PropertyNoteRepository.GetByPropertyAndTabAsync로 비고 로드
- tab_name: collateral_property, senior_rights, evaluation

### Claude's Discretion
- 패널 배경색, 테두리 스타일
- TextBox 높이 (최소/최대)
- 비고 입력 placeholder 텍스트
- 패널 열림 상태의 아이콘 표시 방식

</decisions>

<code_context>
## Existing Code Insights

### Phase 1에서 생성된 기반
- `src/NPLogic.Core/Models/PropertyNote.cs` — 모델
- `src/NPLogic.Data/Repositories/PropertyNoteRepository.cs` — CRUD
- App.xaml.cs에 DI Singleton 등록 완료

### 기존 패턴 참고
- 사이드바: DashboardView의 담보 목록 사이드바 (HeaderNavyBrush 배경, 토글 버튼)
- SaveAll: EvaluationTabViewModel.SaveCommand, SeniorRightsViewModel.SaveAllCommand, PropertyDetailViewModel.SaveAllCollateralCommand
- +/- 토글: EvaluationTab의 섹션 +/- 패턴 (Visibility 바인딩)

### 수정 대상 파일
- Views: EvaluationTab.xaml, SeniorRightsView.xaml, CollateralPropertyView.xaml
- ViewModels: EvaluationTabViewModel.cs, SeniorRightsViewModel.cs, PropertyDetailViewModel.cs

</code_context>

<specifics>
## Specific Ideas

- 사이드 패널은 메인 콘텐츠와 분리 배치 (Grid Column)
- 닫힌 상태에서는 Column Width="0"으로 완전 숨김

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-note-sidebar-prototype*
*Context gathered: 2026-03-23*
