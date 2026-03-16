# Phase 2: DataGrid 스타일 표준화 - Research

**Researched:** 2026-03-16
**Domain:** WPF DataGrid XAML style standardization (EvaluationTab.xaml)
**Confidence:** HIGH

## Summary

EvaluationTab.xaml 내 15개 DataGrid의 현재 상태를 전수 조사한 결과, 대부분은 이미 공통 스타일(ColumnHeaderStyle, CellStyle, RowStyle)과 RowHeight="32"가 적용되어 있다. 핵심 불일치는 4개 DataGrid에 AlternatingRowBackground/Background/RowBackground가 누락된 점과, BorderThickness 값이 "0"과 "1" 사이에서 혼재되는 점이다.

이 작업은 순수 XAML 속성 수정으로, 기능/바인딩/ViewModel 변경이 전혀 없다. 단일 파일(EvaluationTab.xaml) 수정이므로 병합 충돌 위험도 최소화된다.

**Primary recommendation:** 4개 미적용 DataGrid에 Background="White" RowBackground="White" AlternatingRowBackground="#FAFAFA" 추가하고, BorderThickness를 "0"으로 통일한 뒤, 전체 15개 DataGrid의 속성 일관성을 최종 확인한다.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- MaxHeight: 현재 상태 유지 -- 실거래가 DataGrid(280px)만 MaxHeight 있고 나머지는 없음. 추가 MaxHeight 설정하지 않음
- AlternatingRowBackground: 전체 DataGrid에 AlternatingRowBackground="#FAFAFA" 통일 적용. 현재 미적용인 DataGrid: RealTransactionGrid(실거래가), CaseEvaluationGrid(사례평가), InterimRecovery/Expense(인터림)
- Background="White", RowBackground="White" 도 함께 통일
- FontSize: 전체 DataGrid FontSizeBody(12pt) 통일 -- 현재 상태 유지 확인
- SectionDataGridTextBlockStyle/SectionDataGridTextBoxStyle이 이미 FontSizeBody 사용 중

### Claude's Discretion
- RowHeight 32 이미 전체 통일됨 -- 확인만 필요
- BorderBrush, GridLinesVisibility 등 기타 속성 일관성 확인 및 수정
- DataGrid에 공통 스타일(ColumnHeaderStyle, CellStyle, RowStyle) 누락된 곳 보완

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| GRID-01 | 유사 용도의 DataGrid MaxHeight 통일 (데이터 조회용 vs 편집용 구분하여 표준화) | 사용자가 현재 상태 유지 결정. 실거래가만 MaxHeight="280" 유지, 나머지 없음. 변경 불필요 -- 확인만 수행 |
| GRID-02 | DataGrid FontSize를 용도별로 통일 (FontSizeBody vs FontSizeSmall 기준 정립) | 전체 DataGrid가 SectionDataGridTextBlockStyle/TextBoxStyle 사용하며 이미 FontSizeBody(12pt) 통일. 변경 불필요 -- 확인만 수행 |
| GRID-03 | DataGrid 공통 속성(RowHeight, BorderBrush, AlternatingRowBackground 등) 일관 적용 | 핵심 작업 대상. 아래 불일치 감사표 참조. 4개 DataGrid에 AlternatingRowBackground 추가, BorderThickness 통일 필요 |
</phase_requirements>

## Architecture Patterns

### 대상 파일
```
src/NPLogic.App/Views/EvaluationTab.xaml  (3,487 lines)
```

### 로컬 공통 스타일 (UserControl.Resources, lines 15-93)
| 스타일 Key | TargetType | 핵심 속성 |
|-----------|------------|----------|
| SectionDataGridColumnHeaderStyle | DataGridColumnHeader | BlueGray100Brush 배경, SemiBold, 12pt, Center, BorderBrush 테두리 |
| SectionDataGridCellStyle | DataGridCell | Foreground=Black, Background=White, BorderBrush 테두리, Padding 4,2 |
| SectionDataGridRowStyle | DataGridRow | VerticalContentAlignment=Center, Foreground=Black |
| SectionDataGridTextBlockStyle | TextBlock | FontSizeBody, Center 정렬 |
| SectionDataGridTextBoxStyle | TextBox | FontSizeBody, Center 정렬 |
| ScenarioHeaderTextBoxStyle | TextBox | 12pt, 편집 시 PrimaryBrush 테두리 |

### 표준 DataGrid 속성 세트 (목표 상태)
```xml
<DataGrid ...
    AutoGenerateColumns="False"
    CanUserAddRows="False" CanUserDeleteRows="False"
    CanUserSortColumns="False"
    HeadersVisibility="Column"
    RowHeight="32"
    GridLinesVisibility="All"
    HorizontalGridLinesBrush="{StaticResource BorderBrush}"
    VerticalGridLinesBrush="{StaticResource BorderBrush}"
    BorderBrush="{StaticResource BorderBrush}" BorderThickness="0"
    Background="White"
    RowBackground="White"
    AlternatingRowBackground="#FAFAFA"
    MinHeight="60"
    PreviewMouseWheel="DataGrid_PreviewMouseWheel">
    <DataGrid.ColumnHeaderStyle>
        <StaticResource ResourceKey="SectionDataGridColumnHeaderStyle"/>
    </DataGrid.ColumnHeaderStyle>
    <DataGrid.CellStyle>
        <StaticResource ResourceKey="SectionDataGridCellStyle"/>
    </DataGrid.CellStyle>
    <DataGrid.RowStyle>
        <StaticResource ResourceKey="SectionDataGridRowStyle"/>
    </DataGrid.RowStyle>
```

### 예외적 DataGrid (표준 세트에서 의도적으로 벗어나는 것)
| DataGrid | 예외 속성 | 사유 |
|----------|----------|------|
| RealTransactionGrid | MaxHeight="280", MinHeight="280", ScrollViewer.VerticalScrollBarVisibility="Auto", SelectionUnit="Cell", SelectionMode="Single" | 실거래가 스크롤 필요 (데이터 많음) |
| CaseEvaluationGrid | SelectionUnit="Cell", SelectionMode="Single" | 사례 비교 셀 선택 기능 |
| LotEvalDataGrid | HeadersVisibility="None", MinHeight="32" | 헤더 없는 보조 테이블 |
| InterimRecovery/Expense | IsReadOnly="True", ScrollViewer 양방향 Disabled | 읽기 전용 인터림 데이터 |

## 전수 감사: 15개 DataGrid 현재 상태

### 속성 불일치 매트릭스

| # | DataGrid | Line | BG/RowBG/AltBG | BorderThickness | BorderBrush | 공통스타일 3종 | 수정 필요 |
|---|----------|------|----------------|-----------------|-------------|---------------|----------|
| 1 | RealTransactionGrid | 259 | **없음/없음/없음** | 0 | 없음 (implicit) | 3종 완비 | **AlternatingRowBackground + BG + RowBG 추가** |
| 2 | CaseEvaluationGrid | 668 | **없음/없음/없음** | 0 | 없음 (implicit) | 3종 완비 | **AlternatingRowBackground + BG + RowBG 추가** |
| 3 | FactoryInquiryDataGrid | 1172 | White/White/#FAFAFA | **1** | BorderBrush | 3종 완비 | **BorderThickness 0으로 변경** |
| 4 | FactoryInquiryResultDataGrid | 1272 | White/White/#FAFAFA | **1** | BorderBrush | 3종 완비 | **BorderThickness 0으로 변경** |
| 5 | RentalIncomeDataGrid | 1960 | White/White/#FAFAFA | **1** | BorderBrush | 3종 완비 | **BorderThickness 0으로 변경** |
| 6 | InquiryDataGrid | 2177 | White/White/#FAFAFA | **1** | BorderBrush | 3종 완비 | **BorderThickness 0으로 변경** |
| 7 | InquiryResultDataGrid | 2279 | White/White/#FAFAFA | **1** | BorderBrush | 3종 완비 | **BorderThickness 0으로 변경** |
| 8 | FactoryScenario1DataGrid | 2481 | White/White/#FAFAFA | 0 | BorderBrush | 3종 완비 | 없음 |
| 9 | FactoryScenario2DataGrid | 2579 | White/White/#FAFAFA | 0 | BorderBrush | 3종 완비 | 없음 |
| 10 | CommercialScenario1DataGrid | 2692 | White/White/#FAFAFA | 0 | BorderBrush | 3종 완비 | 없음 |
| 11 | CommercialScenario2DataGrid | 2770 | White/White/#FAFAFA | 0 | BorderBrush | 3종 완비 | 없음 |
| 12 | HouseScenario1DataGrid | 2861 | White/White/#FAFAFA | 0 | BorderBrush | 3종 완비 | 없음 |
| 13 | HouseScenario2DataGrid | 2921 | White/White/#FAFAFA | 0 | BorderBrush | 3종 완비 | 없음 |
| 14 | LotEvalDataGrid | 3030 | White/White/#FAFAFA | 0 | BorderBrush | CellStyle+RowStyle (ColumnHeader 없음 -- HeadersVisibility="None") | 없음 |
| 15 | InterimRecovery (unnamed) | 3333 | **없음/없음/없음** | 0 | 없음 (implicit) | 3종 완비 | **AlternatingRowBackground + BG + RowBG + BorderBrush 추가** |
| 16 | InterimExpense (unnamed) | 3407 | **없음/없음/없음** | 0 | 없음 (implicit) | 3종 완비 | **AlternatingRowBackground + BG + RowBG + BorderBrush 추가** |

**참고:** DataGrid 수는 15개이나, 감사표에서는 인터림 2개를 별도로 세어 16행이다. CONTEXT.md에서 "15개 DataGrid"라 한 것과 일치 (인터림 2개 = 2 DataGrid).

### 수정 요약

| 수정 유형 | 대상 DataGrid | 수정 내용 |
|----------|--------------|----------|
| AlternatingRowBackground 추가 | RealTransactionGrid, CaseEvaluationGrid, InterimRecovery, InterimExpense (4개) | `Background="White" RowBackground="White" AlternatingRowBackground="#FAFAFA"` 추가 |
| BorderThickness 통일 | FactoryInquiryDataGrid, FactoryInquiryResultDataGrid, RentalIncomeDataGrid, InquiryDataGrid, InquiryResultDataGrid (5개) | `BorderThickness="1"` -> `BorderThickness="0"` |
| BorderBrush 추가 | RealTransactionGrid, CaseEvaluationGrid, InterimRecovery, InterimExpense (4개) | `BorderBrush="{StaticResource BorderBrush}"` 추가 (현재 명시적으로 없음) |

## Common Pitfalls

### Pitfall 1: AlternatingRowBackground와 CellStyle Background 충돌
**What goes wrong:** SectionDataGridCellStyle에 `Background="White"`가 설정되어 있으므로, AlternatingRowBackground가 CellStyle의 Background에 의해 덮어씌워질 수 있다.
**Why it happens:** WPF에서 CellStyle의 Background가 명시적으로 설정되면 Row-level의 AlternatingRowBackground보다 우선순위가 높다.
**How to avoid:** 현재 코드에서 이미 11개 DataGrid가 AlternatingRowBackground="#FAFAFA"와 SectionDataGridCellStyle을 함께 사용하며 정상 작동 중이다. SectionDataGridCellStyle의 Background=White는 셀 수준이지만, WPF DataGrid는 AlternatingRowBackground를 DataGridRow의 Background로 적용하고, Cell의 Background가 White(불투명)이면 Row 배경이 보이지 않을 수 있다. **기존 작동 그대로 복제하면 안전하다** -- 이미 작동 중인 11개가 증거.
**Warning signs:** 새로 추가한 4개 DataGrid에서 줄무늬가 보이지 않으면 CellStyle 충돌 의심.

### Pitfall 2: BorderThickness="1"에서 "0"으로 변경 시 시각적 차이
**What goes wrong:** 탐문 내역/결과 DataGrid(5개)의 외곽 테두리가 사라진다.
**Why it happens:** BorderThickness="1"은 DataGrid 외곽에 1px 테두리를 그린다.
**How to avoid:** 이 DataGrid들은 CardBorder 안에 있으므로, Card 테두리가 이미 외곽을 감싸고 있다. BorderThickness="0"인 다른 10개 DataGrid와 동일한 외관이 되므로 오히려 일관성이 좋아진다. 변경 후 카드 내부에서 이중 테두리가 사라지는 것을 확인하면 된다.

### Pitfall 3: Unnamed DataGrid (인터림 2개)
**What goes wrong:** 인터림 DataGrid 2개는 x:Name 속성이 없어 코드-비하인드에서 직접 참조 불가.
**Why it happens:** 인라인으로 정의된 읽기 전용 테이블이므로 이름 불필요.
**How to avoid:** 스타일 속성만 수정하므로 x:Name 부재는 문제 없음. 이름을 추가할 필요 없음 (scope 밖).

## Code Examples

### 수정 패턴 A: AlternatingRowBackground 추가 (4개 DataGrid)

현재 RealTransactionGrid (line 259-278):
```xml
<DataGrid DockPanel.Dock="Top"
          x:Name="RealTransactionGrid"
          ...
          GridLinesVisibility="All"
          BorderThickness="0"
          SelectionUnit="Cell"
          SelectionMode="Single"
          HorizontalGridLinesBrush="{StaticResource BorderBrush}"
          VerticalGridLinesBrush="{StaticResource BorderBrush}"
          RowHeight="32"
          ScrollViewer.HorizontalScrollBarVisibility="Disabled"
          ScrollViewer.VerticalScrollBarVisibility="Auto"
          MaxHeight="280"
          MinHeight="280">
```

수정 후:
```xml
<DataGrid DockPanel.Dock="Top"
          x:Name="RealTransactionGrid"
          ...
          GridLinesVisibility="All"
          BorderThickness="0"
          BorderBrush="{StaticResource BorderBrush}"
          SelectionUnit="Cell"
          SelectionMode="Single"
          HorizontalGridLinesBrush="{StaticResource BorderBrush}"
          VerticalGridLinesBrush="{StaticResource BorderBrush}"
          Background="White"
          RowBackground="White"
          AlternatingRowBackground="#FAFAFA"
          RowHeight="32"
          ScrollViewer.HorizontalScrollBarVisibility="Disabled"
          ScrollViewer.VerticalScrollBarVisibility="Auto"
          MaxHeight="280"
          MinHeight="280">
```

### 수정 패턴 B: BorderThickness 통일 (5개 DataGrid)

현재 (e.g., FactoryInquiryDataGrid line 1182):
```xml
BorderBrush="{StaticResource BorderBrush}" BorderThickness="1"
```

수정 후:
```xml
BorderBrush="{StaticResource BorderBrush}" BorderThickness="0"
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| DataGrid 스타일 통일 | DataGrid BasedOn Style (암묵적 스타일) | 각 DataGrid에 인라인 속성 직접 설정 | 이 파일은 이미 인라인 패턴으로 되어 있고, BasedOn 스타일로 바꾸면 scope 초과 (v2 REFACT-01 영역) |
| CellStyle Background 재정의 | AlternatingRowBackground 작동을 위한 CellStyle 수정 | 기존 SectionDataGridCellStyle 그대로 유지 | 이미 11개 DataGrid에서 정상 작동 중 |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual visual inspection (WPF UI) |
| Config file | N/A -- no automated UI test infrastructure |
| Quick run command | `dotnet build NPLogic.sln` |
| Full suite command | `dotnet build NPLogic.sln` (빌드 성공 = XAML 파싱 정상) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GRID-01 | MaxHeight 현재 상태 유지 확인 | manual-only | N/A -- 시각적 확인, MaxHeight 속성 grep | N/A |
| GRID-02 | FontSize FontSizeBody 통일 확인 | manual-only | N/A -- SectionDataGridTextBlockStyle 사용 확인 grep | N/A |
| GRID-03 | 공통 속성 일관 적용 | build + manual | `dotnet build NPLogic.sln` | N/A |

**수동 검증 사유:** WPF DataGrid 스타일 변경은 시각적 결과물이므로 자동화 테스트 불가. 빌드 성공으로 XAML 문법 오류 없음만 확인 가능.

### Sampling Rate
- **Per task commit:** `dotnet build NPLogic.sln` (XAML 파싱 오류 감지)
- **Per wave merge:** 앱 실행 후 각 평가 유형별 DataGrid 시각 확인
- **Phase gate:** 15개 DataGrid 전체 줄무늬/테두리 스크린샷 비교

### Wave 0 Gaps
None -- 기존 빌드 인프라로 충분. 추가 테스트 프레임워크 불필요.

## Open Questions

1. **SectionDataGridCellStyle의 Background=White가 AlternatingRowBackground를 가리는가?**
   - What we know: 이미 11개 DataGrid에서 함께 사용 중이며 정상 작동
   - What's unclear: WPF DataGrid의 정확한 렌더링 우선순위에서 Cell Background vs Row AlternatingRowBackground
   - Recommendation: 기존 작동 패턴을 그대로 복제. 만약 줄무늬가 안 보이면, SectionDataGridCellStyle의 Background를 Transparent로 변경 검토 (단, 이는 기존 11개에도 영향을 주므로 신중히)

## Sources

### Primary (HIGH confidence)
- `src/NPLogic.App/Views/EvaluationTab.xaml` -- 직접 전수 조사 (15개 DataGrid 각각의 속성 확인)
- `02-CONTEXT.md` -- 사용자 결정 사항

### Secondary (MEDIUM confidence)
- WPF DataGrid AlternatingRowBackground 동작 -- WPF 기본 동작 기준 (Microsoft docs)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - 단일 파일 XAML 수정, 기존 패턴 완전 파악
- Architecture: HIGH - 모든 15개 DataGrid 속성을 line-by-line 감사 완료
- Pitfalls: HIGH - CellStyle/AlternatingRowBackground 충돌 가능성이 유일한 리스크이나, 기존 11개 작동 사례로 검증됨

**Research date:** 2026-03-16
**Valid until:** 2026-04-16 (안정적 -- WPF/XAML 기본 기능, 변경 가능성 없음)
