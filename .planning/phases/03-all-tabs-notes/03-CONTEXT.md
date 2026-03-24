# Phase 3: 전체 탭 비고란 적용 - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 2에서 확립한 사이드 패널 비고란 패턴을 나머지 10개 탭에 적용한다. 콘텐츠가 있는 탭 전부에 적용.

</domain>

<decisions>
## Implementation Decisions

### 대상 탭 (10개, 이미 완료된 3개 제외)

**저장 버튼 있는 탭 (4개) — 기존 저장에 통합:**
1. 차주개요 (BorrowerOverviewView) — SaveBorrowerCommand, tab_name: borrower_overview
2. 권리분석 (RightsAnalysisTab) — SaveCommand, tab_name: rights_analysis
3. 기초데이터 (BasicDataTab) — SaveBasicInfoCommand, tab_name: basic_data
4. QA집계 (QASummaryTab) — SaveQaCommand, tab_name: qa_summary

**저장 버튼 없는 탭 (6개) — 포커스 잃을 때 자동 저장:**
5. Loan (LoanSheetView) — tab_name: loan
6. 경(공)매일정 (AuctionScheduleContentControl) — tab_name: auction_schedule
7. 인터림 (InterimTab) — tab_name: interim
8. 현금흐름집계 (CashFlowSummaryView) — tab_name: cashflow_summary
9. NPV비교 (XnpvComparisonView) — tab_name: npv_comparison
10. 마감 (ClosingTab) — tab_name: closing

### 저장 방식
- 저장 버튼 있는 탭: Phase 2와 동일하게 기존 SaveAll/Save에 통합
- 저장 버튼 없는 탭: TextBox의 LostFocus 이벤트로 자동 저장 (포커스 잃을 때)

### UI 패턴
- Phase 2에서 확립한 패턴 그대로 적용:
  - 사이드 패널 250px, 오른쪽, 즉시 토글
  - PackIcon NoteEdit 토글 버튼 (탭 상단 오른쪽)
  - TextBox 여러 줄 입력 (TextWrapping, AcceptsReturn)

### Claude's Discretion
- 자동 저장 구현 방식 (LostFocus vs PropertyChanged + 디바운스)
- 각 탭의 토글 버튼 정확한 배치 위치 (기존 UI 구조에 맞게)
- DI 등록 누락 방지 (App.xaml.cs에서 모든 ViewModel에 PropertyNoteRepository 주입 확인)

</decisions>

<code_context>
## Existing Code Insights

### Phase 2에서 확립한 패턴
- ViewModel: NoteText, IsNotePanelVisible, ToggleNotePanelCommand, LoadNoteAsync, SaveNoteAsync
- View: Grid 2열 구조 (메인 + 사이드 패널 Auto), PackIcon NoteEdit 토글
- DI: App.xaml.cs에서 PropertyNoteRepository 주입 필수 (2곳 누락으로 버그 경험)

### 수정 대상 파일 (10개 탭)
**Views:**
- BorrowerOverviewView.xaml, LoanSheetView.xaml, AuctionScheduleContentControl.xaml
- InterimTab.xaml, RightsAnalysisTab.xaml, BasicDataTab.xaml
- QASummaryTab.xaml, CashFlowSummaryView.xaml, XnpvComparisonView.xaml, ClosingTab.xaml

**ViewModels:**
- 각 View에 대응하는 ViewModel (또는 공유 ViewModel)

**DI:**
- App.xaml.cs — 모든 ViewModel DI 등록에 PropertyNoteRepository 주입 확인

</code_context>

<specifics>
## Specific Ideas

- DI 등록 누락 주의: Phase 2에서 SeniorRightsViewModel DI 등록 누락으로 비고 저장 안 되는 버그가 있었음. 모든 ViewModel의 DI 등록을 꼼꼼히 확인해야 함.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 03-all-tabs-notes*
*Context gathered: 2026-03-24*
