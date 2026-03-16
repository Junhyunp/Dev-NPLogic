# Phase 2: DataGrid 스타일 표준화 - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

EvaluationTab.xaml 내 모든 DataGrid의 시각적 속성(MaxHeight, AlternatingRowBackground, FontSize, RowHeight, BorderBrush 등)을 일관되게 정리한다. 기능/바인딩 변경 없이 스타일 속성만 통일.

</domain>

<decisions>
## Implementation Decisions

### MaxHeight
- 현재 상태 유지 — 실거래가 DataGrid(280px)만 MaxHeight 있고 나머지는 없음
- 추가 MaxHeight 설정하지 않음

### AlternatingRowBackground
- 전체 DataGrid에 AlternatingRowBackground="#FAFAFA" 통일 적용
- 현재 미적용인 DataGrid: RealTransactionGrid(실거래가), CaseEvaluationGrid(사례평가), InterimRecovery/Expense(인터림)
- Background="White", RowBackground="White" 도 함께 통일

### FontSize
- 전체 DataGrid FontSizeBody(12pt) 통일 — 현재 상태 유지 확인
- SectionDataGridTextBlockStyle/SectionDataGridTextBoxStyle이 이미 FontSizeBody 사용 중

### Claude's Discretion
- RowHeight 32 이미 전체 통일됨 — 확인만 필요
- BorderBrush, GridLinesVisibility 등 기타 속성 일관성 확인 및 수정
- DataGrid에 공통 스타일(ColumnHeaderStyle, CellStyle, RowStyle) 누락된 곳 보완

</decisions>

<code_context>
## Existing Code Insights

### 핵심 파일
- `src/NPLogic.App/Views/EvaluationTab.xaml` — DataGrid 15개 포함
- 로컬 스타일: SectionDataGridColumnHeaderStyle, SectionDataGridCellStyle, SectionDataGridRowStyle, SectionDataGridTextBlockStyle, SectionDataGridTextBoxStyle

### DataGrid 목록 (15개)
- RealTransactionGrid (실거래가) — line ~262
- CaseEvaluationGrid (사례평가) — line ~668
- FactoryInquiryDataGrid (탐문 내역 공장) — line ~1172
- FactoryInquiryResultDataGrid (탐문 결과 공장) — line ~1272
- RentalIncomeDataGrid (임대수익) — line ~1960
- InquiryDataGrid (탐문 내역 연립다세대) — line ~2177
- InquiryResultDataGrid (탐문 결과 연립다세대) — line ~2279
- FactoryScenario1/2DataGrid (공장 평가결과) — line ~2481, ~2579
- CommercialScenario1/2DataGrid (상가 평가결과) — line ~2692, ~2770
- HouseScenario1/2DataGrid (주택 평가결과) — line ~2861, ~2921
- LotEvalDataGrid (지번별 평가) — line ~3030
- InterimRecovery/Expense (인터림) — line ~3333, ~3407

### Established Patterns
- RowHeight="32" — 전체 통일됨
- SectionDataGridColumnHeaderStyle — BlueGray100Brush 배경, SemiBold, 12pt
- SectionDataGridCellStyle — White 배경, BorderBrush 테두리
- GridLinesVisibility="All" — 대부분 적용

</code_context>

<specifics>
## Specific Ideas

No specific requirements — 일관성 확보가 핵심.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-datagrid-style*
*Context gathered: 2026-03-16*
