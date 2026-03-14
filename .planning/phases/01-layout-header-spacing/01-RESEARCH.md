# Phase 1: 레이아웃/헤더/여백 기반 통일 - Research

**Researched:** 2026-03-14
**Domain:** WPF XAML 스타일 통일 (EvaluationTab.xaml)
**Confidence:** HIGH

## Summary

EvaluationTab.xaml은 3577줄의 단일 모놀리식 파일로, 5개 평가 유형(아파트, 연립다세대, 공장/창고, 상가/아파트형공장, 주택/근린시설/토지/기타)의 모든 UI를 인라인으로 포함한다. 현재 21개 섹션이 PrimaryBrush 배경 + PackIcon 헤더 패턴을 사용하며, 이를 SectionHeader 이모지+텍스트 패턴으로 통일해야 한다.

수정 대상 파일은 `src/NPLogic.App/Views/EvaluationTab.xaml` 단 하나이며, Typography.xaml의 CardBorder Style 기본 Margin은 변경하지 않는 것을 권장한다(다른 탭에 영향 가능). 대신 EvaluationTab.xaml 내 로컬 `CardStyle`의 Margin 기본값을 `0,0,0,16`으로 조정하거나, 각 섹션의 인라인 Border에 `Margin="0,0,0,16"`을 직접 지정한다.

**Primary recommendation:** EvaluationTab.xaml의 21개 PrimaryBrush 섹션 헤더를 SectionHeader 스타일 이모지+텍스트로 교체하고, 모든 섹션 외곽 Border에 Padding="16"과 Margin="0,0,0,16"을 일관 적용한다. DesignHeight는 이미 900이므로 확인만 필요하다.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- DesignHeight를 900으로 통일 (현재 아파트/연립다세대 개별 View가 800이지만, 실제 사용되는 EvaluationTab.xaml은 이미 900)
- DesignWidth 1400 유지
- 좌우 패널 비율(*,16,*) -- 해당 없음. EvaluationTab.xaml은 단일 열 레이아웃(ScrollViewer 하나)
- EvaluationTab.xaml이 모든 콘텐츠를 인라인으로 포함하는 현재 구조 유지
- Views/Evaluation/ 폴더의 개별 View 파일들(EvaluationApartmentView.xaml 등)은 무시 (현재 미사용)
- 섹션 순서는 현재 상태 유지 -- 변경하지 않음
- 모든 섹션 헤더를 이모지+텍스트 SectionHeader 스타일로 통일
- PrimaryBrush 배경 + PackIcon 헤더(평가결과, 지번별 평가 등)를 제거하고 일반 SectionHeader로 교체
- 이모지 유지 -- 각 섹션의 기존 이모지 그대로 사용
- CardBorder 아래 Margin: 0,0,0,16으로 전 섹션 통일
- CardBorder 내부 Padding: 16px 유지

### Claude's Discretion
- CardBorder Style 기본 Margin을 12->16으로 변경할지, 각 섹션에서 개별 오버라이드할지
- PrimaryBrush 헤더 제거 후 평가결과 섹션의 이모지 선택
- 헤더 간 Margin/Padding 미세 조정

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| LAYOUT-01 | 전체 5개 View의 DesignHeight를 동일하게 통일 | EvaluationTab.xaml은 이미 d:DesignHeight="900". 개별 View 파일은 미사용이므로 확인만 필요 |
| LAYOUT-02 | 좌우 패널 내 섹션 순서를 일관된 원칙으로 정리 | 단일 열 레이아웃이므로 해당 없음. 섹션 순서 유지 결정됨 |
| LAYOUT-03 | CardBorder Margin을 모든 섹션에서 동일하게 적용 (0,0,0,16) | 22개 외곽 Border 중 21개가 이미 Margin="0,0,0,16", 1개(상가구분)만 Margin="0,0,0,8" -- 통일 필요 |
| HDR-01 | 섹션 헤더 스타일을 SectionHeader 기준으로 통일 | 21개 PrimaryBrush+PackIcon 헤더를 SectionHeader 이모지+텍스트로 교체. 이모지 매핑 테이블 제공 |
| HDR-02 | 평가결과 헤더와 기타 헤더 간 일관성 확보 | 4개 평가결과 섹션 헤더를 동일 패턴으로 교체. 이모지: 기존 ClipboardCheckOutline -> 적절한 이모지 선택 |
| SPC-01 | Border 내부 Padding을 모든 섹션에서 동일하게 (16px) | 현재 외곽 Border에 Padding 없음(PrimaryBrush 헤더가 대신). 교체 후 Padding="16" 추가 필요 |
| SPC-02 | 섹션 간 간격(Margin)을 전체 View에서 일관되게 적용 | Margin="0,0,0,16" 통일. 상가구분 Border의 0,0,0,8을 16으로 변경 |
</phase_requirements>

## Architecture Patterns

### 현재 파일 구조 (수정 대상)
```
src/NPLogic.App/Views/EvaluationTab.xaml          -- 3577줄, 모든 평가 UI 포함 (수정 대상)
src/NPLogic.UI/Styles/Typography.xaml              -- 스타일 정의 (참조만, 변경 불필요 권장)
src/NPLogic.App/Views/Evaluation/*.xaml            -- 5개 개별 View (미사용, 무시)
src/NPLogic.App/Views/Controls/*.xaml              -- 재사용 컨트롤 (참고용 패턴)
```

### Pattern 1: 현재 섹션 구조 (Before -- 제거 대상)
**What:** PrimaryBrush 배경 + PackIcon + 흰색 텍스트 헤더
**구조:**
```xml
<!-- 외곽 Border: Padding 없음, CornerRadius=8 -->
<Border Background="{StaticResource SurfaceColor}"
        BorderBrush="{StaticResource BorderBrush}"
        BorderThickness="1"
        CornerRadius="8"
        Margin="0,0,0,16">
    <StackPanel>
        <!-- PrimaryBrush 헤더: 상단 모서리만 둥글게 -->
        <Border Background="{StaticResource PrimaryBrush}"
                CornerRadius="8,8,0,0"
                Padding="16,10">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <materialDesign:PackIcon Kind="ChartBox" Width="18" Height="18"
                                         Margin="0,0,6,0" VerticalAlignment="Center" Foreground="White"/>
                <TextBlock Text="사례평가"
                           Foreground="White"
                           FontWeight="SemiBold"
                           FontSize="{StaticResource FontSizeH4}"/>
            </StackPanel>
        </Border>
        <!-- 콘텐츠 (DataGrid 등) -->
    </StackPanel>
</Border>
```

### Pattern 2: 목표 섹션 구조 (After -- 적용 대상)
**What:** CardBorder/CardStyle 패턴 + SectionHeader 이모지+텍스트
**When to use:** 모든 섹션에 동일하게 적용
**Example:**
```xml
<!-- Source: BidStatisticsControl.xaml (기존 Controls에서 사용 중인 패턴) -->
<Border Style="{StaticResource CardStyle}" Margin="0,0,0,16">
    <StackPanel>
        <TextBlock Text="📋 사례평가"
                   Style="{StaticResource SectionHeader}"
                   Margin="0,0,0,12"/>
        <!-- 콘텐츠 (DataGrid 등) -->
    </StackPanel>
</Border>
```

### Pattern 3: 헤더에 버튼이 포함된 섹션 (After)
**What:** 테이블 생성/삭제 버튼이 있는 섹션 (탐문 결과, 임대호가분석, 무상임대분석, 탐문 수익가치, 현재 임대 수익가치 등)
**When to use:** 6개 섹션에 +/- 버튼 존재
**Example:**
```xml
<Border Style="{StaticResource CardStyle}" Margin="0,0,0,16">
    <StackPanel>
        <Grid Margin="0,0,0,12">
            <TextBlock Text="📝 탐문 결과"
                       Style="{StaticResource SectionHeader}"
                       Margin="0"/>
            <!-- 테이블 생성/제거 버튼 -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="+" FontSize="16" FontWeight="Bold"
                        HorizontalAlignment="Right" VerticalAlignment="Center"
                        Command="{Binding CreateInquiryResultTableCommand}"
                        Visibility="{Binding HasInquiryResultTable, Converter={StaticResource InverseBooleanToVisibilityConverter}}"
                        Padding="8,0" MinHeight="0" Height="20" Cursor="Hand"
                        ToolTip="탐문 결과 테이블 생성"/>
                <Button Content="-" FontSize="16" FontWeight="Bold"
                        HorizontalAlignment="Right" VerticalAlignment="Center"
                        Command="{Binding DestroyInquiryResultTableCommand}"
                        Visibility="{Binding HasInquiryResultTable, Converter={StaticResource BoolToVisibilityConverter}}"
                        Padding="8,0" MinHeight="0" Height="20" Cursor="Hand"
                        ToolTip="탐문 결과 테이블 제거"/>
            </StackPanel>
        </Grid>
        <!-- 콘텐츠 -->
    </StackPanel>
</Border>
```

### Pattern 4: 헤더에 가져오기 버튼이 포함된 섹션 (After)
**What:** 사례지도 섹션 -- 가져오기 버튼 + Binding 제목
**Example:**
```xml
<Border Style="{StaticResource CardStyle}" Margin="0,0,0,16">
    <StackPanel>
        <Grid Margin="0,0,0,12">
            <TextBlock Text="{Binding CaseMapSectionTitle}"
                       Style="{StaticResource SectionHeader}"
                       Margin="0"/>
            <Button Content="가져오기"
                    Command="{Binding LoadCaseMapCommand}"
                    Padding="12,4"
                    FontSize="{StaticResource FontSizeBody}"
                    HorizontalAlignment="Right"
                    VerticalAlignment="Center"/>
        </Grid>
        <!-- 콘텐츠 -->
    </StackPanel>
</Border>
```
**Note:** 사례지도 섹션은 제목이 `{Binding CaseMapSectionTitle}`이므로 이모지를 Binding에 포함시킬 수 없다. ViewModel 측에서 이모지를 포함하거나, 별도 이모지 TextBlock을 앞에 배치해야 한다.

### Anti-Patterns to Avoid
- **Typography.xaml의 CardBorder 기본 Margin 변경:** Margin="0,0,0,12"를 16으로 변경하면 InterimTab, Controls 등 다른 곳에 영향. EvaluationTab 로컬 CardStyle에서만 오버라이드할 것.
- **StackPanel 제거 후 Grid 직접 사용:** 현재 StackPanel > PrimaryBrush Header + Content 구조에서 PrimaryBrush Header를 제거할 때, StackPanel 자체를 유지하고 그 안에 SectionHeader TextBlock을 배치. 구조 변경 최소화.
- **한 번에 모든 섹션 변경:** 3577줄 파일을 한 번에 수정하면 실수 가능성 높음. 섹션 그룹별(공통/유형별)로 나누어 작업.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| 섹션 헤더 스타일 | 인라인 속성 직접 지정 | `Style="{StaticResource SectionHeader}"` | Typography.xaml에 이미 정의됨 (FontSizeH4, SemiBold, Margin 0,0,0,12) |
| 카드 외곽 Border | 인라인 속성 5개 반복 | `Style="{StaticResource CardStyle}"` | EvaluationTab.xaml 로컬 리소스에 이미 CardBorder 기반 CardStyle 존재 (line 26-28) |
| 버튼 Foreground 색상 변경 | 흰색에서 검정으로 수동 변경 | 기본 Button 스타일 유지 | PrimaryBrush 배경 제거 시 Foreground="White" 속성도 함께 제거하면 기본 색상 적용됨 |

## Common Pitfalls

### Pitfall 1: PrimaryBrush 제거 시 버튼 Foreground 누락
**What goes wrong:** PrimaryBrush 헤더 내부의 +/- 버튼이 `Foreground="White"`로 설정되어 있음. 헤더 제거 후 흰색 버튼이 흰색 배경에 보이지 않게 됨.
**Why it happens:** 기존 버튼들은 PrimaryBrush(파란색) 배경 위에 흰색 텍스트로 표시됨. 배경 제거 시 색상 누락.
**How to avoid:** 헤더 변환 시 내부 Button의 `Foreground="White"` 및 `Background="Transparent"` 속성을 제거하거나 적절한 색상으로 변경.
**Warning signs:** 버튼이 보이지 않거나 클릭 영역만 존재하는 상태.

### Pitfall 2: 사례지도 섹션의 Binding 제목
**What goes wrong:** 사례지도 섹션의 제목이 `{Binding CaseMapSectionTitle}`로 동적 바인딩됨. 이모지를 XAML에 하드코딩하면 바인딩 값과 이모지가 분리됨.
**Why it happens:** 다른 섹션은 고정 텍스트지만 이 섹션만 ViewModel에서 제목 제공.
**How to avoid:** 두 가지 방안 중 선택:
  1. ViewModel의 CaseMapSectionTitle getter에 이모지 포함 (예: "🗺 사례지도 및 실거래가")
  2. XAML에서 이모지 TextBlock + Binding TextBlock을 나란히 배치
  방안 2를 권장 (ViewModel 수정 없이 UI만 변경하는 원칙 유지).
**Warning signs:** 이모지 없이 텍스트만 표시되거나, ViewModel과 XAML 간 이모지 중복.

### Pitfall 3: DataGrid 상단 여백 변경
**What goes wrong:** 기존 PrimaryBrush 헤더는 Padding="16,10"으로 DataGrid 위에 16px 좌우 패딩 제공. 헤더 제거 + 외곽 Padding="16" 추가 시 DataGrid 좌우에 16px 패딩이 추가되어 DataGrid 폭이 줄어듦.
**Why it happens:** 기존 구조는 외곽 Border에 Padding 없이 PrimaryBrush 헤더만 패딩. DataGrid는 외곽 Border에 edge-to-edge 배치.
**How to avoid:** 이것은 의도된 변경임. CardBorder 패턴에서는 콘텐츠가 패딩 안에 들어가는 것이 표준. DataGrid가 약간 좁아지는 것은 시각적 일관성을 위한 트레이드오프.
**Warning signs:** 없음. 이는 정상적인 결과.

### Pitfall 4: 상가구분 Border의 특수 Margin
**What goes wrong:** 상가구분 Border (line 170-208)는 Margin="0,0,0,8"로 다른 섹션보다 좁은 간격. 이것도 0,0,0,16으로 변경해야 함.
**Why it happens:** 상가구분은 평가 유형 선택 바로 아래에 위치하는 보조 선택 UI로, 의도적으로 좁은 간격 적용됨.
**How to avoid:** LAYOUT-03 요구사항에 따라 0,0,0,16으로 통일. 상가구분 Border도 예외 없이 적용.

### Pitfall 5: PrimaryBrush 내부의 내부 테이블 셀 헤더
**What goes wrong:** 낙찰통계 섹션(line 1009)과 회수 전략 요약의 시나리오 라벨(line 3257)에도 PrimaryBrush가 사용됨. 이들은 섹션 헤더가 아닌 테이블 내부 셀 헤더이므로 제거 대상이 아님.
**Why it happens:** PrimaryBrush가 섹션 헤더와 테이블 셀 모두에 사용됨.
**How to avoid:** `CornerRadius="8,8,0,0"` 패턴으로 섹션 헤더만 식별. 내부 셀의 PrimaryBrush는 그대로 유지.

## Code Examples

### 섹션별 이모지 매핑 (전체 21개 섹션)

기존 개별 View 파일(EvaluationApartmentView.xaml 등)에서 사용된 이모지를 기준으로 매핑:

| # | 섹션명 | PackIcon Kind | 권장 이모지 | 출처 | 버튼 유무 |
|---|--------|--------------|------------|------|-----------|
| 1 | 사례지도 및 실거래가 | MapMarkerRadius | (Binding 제목) | ApartmentView line 23 | 가져오기 버튼 |
| 2 | 사례평가 | ChartBox | 📋 | ApartmentView line 179 | 없음 |
| 3 | 사례 로드뷰 | ImageMultipleOutline | 🏠 | ApartmentView line 240 | 없음 |
| 4 | 경매사건검색 | Gavel | 🔍 | ApartmentView line 276 | 없음 |
| 5 | 낙찰통계 | ChartBar | 📈 | ApartmentView line 306 | 없음 |
| 6 | 탐문 내역 (공장/상가/주택) | PhoneInTalk | 📞 | FactoryView line 414 | 없음 |
| 7 | 탐문 결과 (공장/상가/주택) | ClipboardTextOutline | 📝 | -- | +/- 버튼 |
| 8 | 임대호가분석 | TableLarge | 📊 | CommercialView line 229 (📝) | +/- 버튼 |
| 9 | 무상임대분석 | CalendarBlank | 🆓 | CommercialView line 348 | +/- 버튼 |
| 10 | 탐문 수익가치 | CurrencyUsd | 💰 | CommercialView line 269 | +/- 버튼 |
| 11 | 현재 임대 수익가치 | HomeCity | 🏢 | HouseLandView line 193 (💰) | +/- 버튼 |
| 12 | 탐문 내역 (연립다세대) | PhoneInTalk | 📞 | MultiFamilyView line 327 | +/- 버튼 |
| 13 | 탐문 결과 (연립다세대) | ClipboardTextOutline | 📝 | -- | +/- 버튼 |
| 14 | 평가결과 (아파트/연립) | ClipboardCheckOutline | ✅ | EvaluationResultControl line 11 | 없음 |
| 15 | 평가결과 (공장/창고) | ClipboardCheckOutline | ✅ | Claude 재량 | 없음 |
| 16 | 평가결과 (상가/아파트형공장) | ClipboardCheckOutline | ✅ | Claude 재량 | 없음 |
| 17 | 평가결과 (주택/근린시설/토지/기타) | ClipboardCheckOutline | ✅ | Claude 재량 | 없음 |
| 18 | 지번별 평가 | MapMarkerMultipleOutline | 📍 | FactoryView line 61 | 없음 |
| 19 | 회수 전략 요약 | ShieldCheckOutline | 💰 | RecoveryStrategySummaryControl line 12 | 없음 |
| 20 | 인터림 상계/회수 | SwapHorizontal | 🔄 | -- (Claude 선택) | 없음 |
| 21 | 인터림 지출 | CashMinus | 💸 | -- (Claude 선택) | 없음 |

**Note:** 평가결과 이모지는 EvaluationResultControl.xaml에서 ✅ 사용 확인. 4개 유형 모두 동일하게 적용.

### 기본 변환 패턴 (버튼 없는 섹션)

**Before:**
```xml
<Border Background="{StaticResource SurfaceColor}"
        BorderBrush="{StaticResource BorderBrush}"
        BorderThickness="1"
        CornerRadius="8"
        Margin="0,0,0,16">
    <StackPanel>
        <Border Background="{StaticResource PrimaryBrush}"
                CornerRadius="8,8,0,0"
                Padding="16,10">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <materialDesign:PackIcon Kind="ChartBox" Width="18" Height="18"
                                         Margin="0,0,6,0" VerticalAlignment="Center" Foreground="White"/>
                <TextBlock Text="사례평가"
                           Foreground="White"
                           FontWeight="SemiBold"
                           FontSize="{StaticResource FontSizeH4}"/>
            </StackPanel>
        </Border>
        <!-- content -->
    </StackPanel>
</Border>
```

**After:**
```xml
<Border Style="{StaticResource CardStyle}" Margin="0,0,0,16">
    <StackPanel>
        <TextBlock Text="📋 사례평가"
                   Style="{StaticResource SectionHeader}"
                   Margin="0,0,0,12"/>
        <!-- content -->
    </StackPanel>
</Border>
```

### 변환 시 로컬 CardStyle 활용

EvaluationTab.xaml의 로컬 리소스에 이미 CardStyle이 정의되어 있음 (line 26-28):
```xml
<Style x:Key="CardStyle" TargetType="Border" BasedOn="{StaticResource CardBorder}">
    <Setter Property="Padding" Value="16"/>
</Style>
```

CardBorder 기본 Margin은 `0,0,0,12`이므로, 각 섹션에서 인라인으로 `Margin="0,0,0,16"`을 오버라이드하거나, 로컬 CardStyle에 Margin Setter를 추가:
```xml
<Style x:Key="CardStyle" TargetType="Border" BasedOn="{StaticResource CardBorder}">
    <Setter Property="Padding" Value="16"/>
    <Setter Property="Margin" Value="0,0,0,16"/>
</Style>
```

**권장:** 로컬 CardStyle에 Margin Setter 추가. 이렇게 하면 22개 섹션 모두 `Style="{StaticResource CardStyle}"`만으로 Padding 16 + Margin 0,0,0,16 적용.

## 섹션 인벤토리 및 구조 분석

### 외곽 Border 인벤토리 (총 22개)

현재 EvaluationTab.xaml에서 `Background="{StaticResource SurfaceColor}"` + `CornerRadius="8"` 패턴의 외곽 Border:

| Line | 섹션 | Visibility 조건 | 현재 Margin | Padding |
|------|------|-----------------|-------------|---------|
| 170 | 상가구분 | IsCommercialType | 0,0,0,8 | 16,10 |
| 211 | 사례지도 및 실거래가 | 없음 (공통) | 0,0,0,16 | 없음 |
| 661 | 사례평가 | 없음 (공통) | 0,0,0,16 | 없음 |
| 729 | 사례 로드뷰 | IsFactoryOrCommercialOrHouseType | 0,0,0,16 | 없음 |
| 834 | 경매사건검색 | 없음 (공통) | 0,0,0,16 | 없음 |
| 917 | 낙찰통계 | 없음 (공통) | 0,0,0,16 | 없음 |
| 1187 | 탐문 내역 (공장/상가/주택) | IsFactoryOrCommercialOrHouseType | 0,0,0,16 | 없음 |
| 1272 | 탐문 결과 (공장/상가/주택) | IsFactoryOrCommercialOrHouseType | 0,0,0,16 | 없음 |
| 1359 | 임대호가분석 | IsCommercialType | 0,0,0,16 | 없음 |
| 1549 | 무상임대분석 | IsCommercialType | 0,0,0,16 | 없음 |
| 1788 | 탐문 수익가치 | IsFactoryOrCommercialOrHouseType | 0,0,0,16 | 없음 |
| 1968 | 현재 임대 수익가치 | IsFactoryOrCommercialOrHouseType | 0,0,0,16 | 없음 |
| 2186 | 탐문 내역 (연립다세대) | IsMultiFamilyType | 0,0,0,16 | 없음 |
| 2291 | 탐문 결과 (연립다세대) | IsMultiFamilyType | 0,0,0,16 | 없음 |
| 2378 | 평가결과 (아파트/연립) | !IsFactoryOrCommercialOrHouseType | 0,0,0,16 | 없음 |
| 2479 | 평가결과 (공장/창고) | IsFactoryType | 0,0,0,16 | 없음 |
| 2699 | 평가결과 (상가/아파트형공장) | IsCommercialType | 0,0,0,16 | 없음 |
| 2878 | 평가결과 (주택/근린시설/토지/기타) | IsHouseLandType | 0,0,0,16 | 없음 |
| 3021 | 지번별 평가 | IsFactoryType | 0,0,0,16 | 없음 |
| 3213 | 회수 전략 요약 | IsFirstPropertyInBorrower | 0,0,0,16 | 없음 |
| 3397 | 인터림 상계/회수 | 없음 | 0,0,0,16 | 없음 |
| 3476 | 인터림 지출 | 없음 | 0,0,0,16 | 없음 |

**Key findings:**
- 21/22 섹션이 이미 Margin="0,0,0,16" -- 거의 통일됨
- 상가구분 Border만 Margin="0,0,0,8" -- 16으로 변경 필요
- 모든 외곽 Border에 Padding이 없음 (PrimaryBrush 헤더가 대신 제공)
- 상가구분 Border는 PrimaryBrush 헤더 없이 인라인 스타일 사용 (별도 처리)

### 버튼이 있는 섹션 (6개)

| 섹션 | 버튼 종류 | Command |
|------|-----------|---------|
| 사례지도 | 가져오기 | LoadCaseMapCommand |
| 탐문 결과 (공장/상가/주택) | +/- | Create/DestroyInquiryResultTableCommand |
| 임대호가분석 | +/- | Create/DestroyRentalQuoteTableCommand (추정) |
| 무상임대분석 | +/- | Create/DestroyFreeRentTableCommand (추정) |
| 탐문 수익가치 | +/- | Create/DestroyInquiryProfitTableCommand (추정) |
| 현재 임대 수익가치 | +/- | Create/DestroyRentalProfitTableCommand (추정) |
| 탐문 내역 (연립다세대) | +/- | Create/DestroyInquiryTableCommand (추정) |
| 탐문 결과 (연립다세대) | +/- | Create/DestroyInquiryResultTableCommand |

## 스타일 의존성 분석

### CardBorder (Typography.xaml, line 341-349)
```
Background: SurfaceBrush
BorderBrush: BorderBrush
BorderThickness: 1
CornerRadius: 8
Padding: 16
Margin: 0,0,0,12
Effect: CardShadow
```

### CardStyle (EvaluationTab.xaml 로컬, line 26-28)
```
BasedOn: CardBorder
Padding: 16 (중복 설정, 동일 값)
```

### SectionHeader (Typography.xaml, line 92-98)
```
FontFamily: FontPrimary
FontSize: FontSizeH4 (14pt)
FontWeight: SemiBold
Foreground: TextPrimaryBrush
Margin: 0,0,0,12
```

### SectionHeaderStyle (EvaluationTab.xaml 로컬, line 20-22)
```
BasedOn: SectionHeader
FontSize: FontSizeH4 (중복 설정, 동일 값)
```

**Note:** `SectionHeaderStyle` 로컬 별칭은 `SectionHeader`와 동일하므로, 직접 `SectionHeader`를 사용해도 됨.

### SurfaceColor vs SurfaceBrush

Colors.xaml (line 48-49)에서 확인:
- `SurfaceBrush` = `SolidColorBrush` with Color=Surface
- `SurfaceColor` = `SolidColorBrush` with Color=Surface (동일)

두 리소스는 동일한 값. CardBorder는 `SurfaceBrush`를 사용하고, 현재 EvaluationTab.xaml은 `SurfaceColor`를 사용. CardStyle로 교체하면 자동으로 `SurfaceBrush` 사용.

## Open Questions

1. **사례지도 섹션의 이모지 처리**
   - What we know: CaseMapSectionTitle은 ViewModel에서 동적 생성됨
   - What's unclear: ViewModel이 어떤 텍스트를 반환하는지 (이모지 포함 여부)
   - Recommendation: XAML에서 별도 이모지 TextBlock 배치 또는 StackPanel에 "🗺" + Binding 조합 사용. ViewModel 변경 최소화 원칙에 따라 XAML 방식 권장.

2. **상가구분 Border의 CardStyle 적용 여부**
   - What we know: 상가구분은 PrimaryBrush 헤더 없이 인라인 Border 사용. Padding="16,10", 별도 스타일.
   - What's unclear: CardStyle 적용 시 시각적 차이 (Padding 16 vs 16,10, CardShadow 효과 추가됨)
   - Recommendation: 상가구분도 CardStyle 적용하여 일관성 확보. Padding 차이는 미미 (상하 6px 차이).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Visual inspection (WPF UI) |
| Config file | None -- WPF desktop app |
| Quick run command | `dotnet build NPLogic.sln` |
| Full suite command | `dotnet build NPLogic.sln` (빌드 성공 = XAML 파싱 성공) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| LAYOUT-01 | DesignHeight=900 | manual-only | N/A (XAML 속성 확인) | N/A |
| LAYOUT-02 | 섹션 순서 유지 | manual-only | N/A (변경하지 않음) | N/A |
| LAYOUT-03 | CardBorder Margin 통일 | manual-only | `dotnet build NPLogic.sln` (빌드 성공) | N/A |
| HDR-01 | SectionHeader 스타일 통일 | manual-only | `dotnet build NPLogic.sln` (빌드 성공) | N/A |
| HDR-02 | 평가결과 헤더 일관성 | manual-only | `dotnet build NPLogic.sln` (빌드 성공) | N/A |
| SPC-01 | Border Padding 16px | manual-only | `dotnet build NPLogic.sln` (빌드 성공) | N/A |
| SPC-02 | 섹션 간 Margin 통일 | manual-only | `dotnet build NPLogic.sln` (빌드 성공) | N/A |

**Justification for manual-only:** WPF XAML 스타일 변경은 시각적 결과를 런타임에서만 확인 가능. 자동화 테스트 불가능하나, `dotnet build` 성공으로 XAML 문법 오류를 검증할 수 있음.

### Sampling Rate
- **Per task commit:** `dotnet build NPLogic.sln`
- **Per wave merge:** `dotnet build NPLogic.sln` + 수동 UI 확인 (5개 유형 전환)
- **Phase gate:** 빌드 성공 + 5개 유형 각각 전환하여 헤더/여백/레이아웃 시각 확인

### Wave 0 Gaps
None -- 기존 빌드 인프라로 XAML 문법 검증 충분. 시각적 검증은 수동으로 수행.

## Sources

### Primary (HIGH confidence)
- `src/NPLogic.App/Views/EvaluationTab.xaml` -- 3577줄 전체 분석
- `src/NPLogic.UI/Styles/Typography.xaml` -- CardBorder, SectionHeader 스타일 정의 확인
- `src/NPLogic.UI/Styles/Colors.xaml` -- SurfaceColor/SurfaceBrush 동일성 확인
- `src/NPLogic.App/Views/Controls/*.xaml` -- SectionHeader 이모지+텍스트 패턴 참조
- `src/NPLogic.App/Views/Evaluation/*.xaml` -- 미사용 개별 View의 이모지 매핑 참조

### Secondary (MEDIUM confidence)
- 이모지 매핑 테이블 -- 기존 Controls/개별 View 파일에서 추출. 일부 섹션(인터림 상계/회수, 인터림 지출)은 기존 매핑 없음

### Tertiary (LOW confidence)
- 없음

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- WPF/XAML 직접 분석, 프로젝트 내 스타일 시스템 확인
- Architecture: HIGH -- 단일 파일 수정, 패턴은 기존 Controls에서 검증됨
- Pitfalls: HIGH -- 3577줄 XAML 코드 전체 분석으로 모든 엣지 케이스 식별

**Research date:** 2026-03-14
**Valid until:** 2026-04-14 (안정적 -- WPF/XAML은 변화 없음)
