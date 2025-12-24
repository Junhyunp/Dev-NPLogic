# 엑셀 함수(수식) 기능 구현 계획서

## 📋 개요

### 배경
프로그램 내 테이블 형식 데이터에서 **사칙연산, SUM, SUMIF 등의 간단한 엑셀 함수**를 지원하여 사용자가 데이터를 실시간으로 계산할 수 있도록 개선 요청.

### 목표
- 엑셀과 유사한 수식 입력 경험 제공
- 사칙연산 (+, -, *, /)
- 기본 집계 함수 (SUM, SUMIF, AVERAGE, COUNT 등)
- 셀 참조 (A1, B2:B10 등)
- 실시간 계산 및 자동 업데이트

---

## 🎯 적용 대상 화면

### 1. 대시보드 (Dashboard)
**파일**: `src/NPLogic.App/Views/DashboardView.xaml`

**현재 상태**:
- DataGrid 기반 물건 목록 표시
- 15개 컬럼 (차주번호, 차주명, 담보번호, 물건종류 등)
- 진행률 체크박스

**수식 적용 가능 시나리오**:
```
예시 1: 진행률 자동 계산
- 완료된 체크박스 개수 / 전체 체크박스 개수 * 100
- 수식: =COUNT(약정서:권리분석, "완료") / 9 * 100

예시 2: 미완료 작업 개수
- 수식: =COUNT(약정서:권리분석, "미완료")
```

**구현 방향**:
- 진행률 컬럼에 자동 수식 적용
- 커스텀 컬럼 추가 기능 (사용자 정의 수식)
- 그리드 하단 합계/평균 행 (Footer Row)

---

### 2. 담보물건 요약 (CollateralSummaryViewModel)
**파일**: `src/NPLogic.App/ViewModels/CollateralSummaryViewModel.cs`

**현재 상태**:
- 차주별 담보물건 목록
- 대지면적, 건물면적, 감정평가액, 선순위, 배당가능재원 등

**수식 적용 가능 시나리오**:
```
예시 1: 배당가능재원 계산
- 현재: C# 코드로 계산 (line 210)
  item.RecoverableAmount = Math.Max(0, item.EstimatedValue - item.SeniorRights);
- 수식 버전: =MAX(0, 평가액 - 선순위)

예시 2: 합계 계산
- 현재: CalculateTotals() 메서드
- 수식 버전: 
  총평가액 = SUM(평가액 컬럼)
  총선순위 = SUM(선순위 컬럼)
  총배당가능재원 = SUM(배당가능재원 컬럼)
```

**구현 방향**:
- 그리드 Footer Row에 집계 수식 적용
- SUMIF로 조건부 합계 (예: 완료된 물건만)

---

### 3. 평가 탭 (EvaluationTab)
**파일**: `src/NPLogic.App/Views/EvaluationTab.xaml`

**현재 상태**:
- XNPV 계산기 (현금흐름 입력)
- 낙찰통계 분석
- 시세 조회

**수식 적용 가능 시나리오**:
```
예시 1: 현금흐름 테이블
- 순현금흐름 = 현금유입 - 현금유출
- 누적현금흐름 = SUM(순현금흐름[T0:현재행])

예시 2: 예상 낙찰가
- 수식: =감정평가액 * 평균낙찰률

예시 3: 투자회수기간 계산
- 조건부 수식 활용
```

**구현 방향**:
- 현금흐름 그리드에 수식 컬럼 추가
- 사용자가 직접 수식 편집 가능

---

### 4. 권리분석 탭 (RightsAnalysisTab)
**파일**: `src/NPLogic.App/ViewModels/RightsAnalysisTabViewModel.cs`

**현재 상태**:
- 선순위 분석 (근저당권, 소액보증금, 임금채권 등)
- 배당 시뮬레이션

**수식 적용 가능 시나리오**:
```
예시 1: 선순위 합계
- 수식: =SUM(선순위근저당권, 선순위소액보증금, 선순위임금채권, 당해세, 선순위조세채권)

예시 2: 배당가능재원
- 수식: =낙찰가 - 경매수수료 - 선순위합계

예시 3: 회수율
- 수식: =배당가능재원 / LoanCap * 100

예시 4: 조건부 합계 (SUMIF)
- 선순위 중 '반영' 상태만: =SUMIF(상태열, "반영", 금액열)
```

**구현 방향**:
- 선순위 분석 그리드에 수식 자동 적용
- 사용자가 DD금액 대신 수동 수식 입력 가능

---

### 5. 설정 관리 (SettingsView)
**파일**: `src/NPLogic.App/Views/SettingsView.xaml`

**현재 상태**:
- 계산 수식 설정 섹션 (line 188-199)
- 수식 도움말 표시

**수식 적용 가능 시나리오**:
```
예시: 전역 수식 정의
- 수식명: "회수율"
- 표현식: "({낙찰가} - {경매비용} - {선순위합계}) / {LoanCap} * 100"
- 적용대상: 모든 권리분석 탭

사용자가 정의한 수식을 다른 화면에서 재사용
```

**구현 방향**:
- 수식 템플릿 관리
- 변수 치환 엔진
- 수식 검증 기능

---

## 🔧 구현 방법 (3가지 옵션)

### 옵션 1: WPF DataGrid 자체 확장 ⭐ 추천
**장점**:
- 기존 DataGrid와 자연스럽게 통합
- WPF MVVM 패턴 유지
- 경량, 빠른 성능

**단점**:
- 직접 구현 필요 (수식 파서, 계산 엔진)
- 고급 기능 제한적

**구현 방식**:
```csharp
// 1. 수식 파서 구현
public class FormulaParser
{
    public object Evaluate(string formula, Dictionary<string, object> context)
    {
        // "=SUM(A1:A10)" 파싱
        // 셀 참조 해석
        // 함수 실행
    }
}

// 2. DataGrid 컬럼에 수식 바인딩
public class FormulaColumn : DataGridTextColumn
{
    public string Formula { get; set; }
    
    protected override object GetCellContent(DataGridRow row)
    {
        var result = _parser.Evaluate(Formula, GetRowContext(row));
        return new TextBlock { Text = result.ToString() };
    }
}

// 3. 사용
<DataGrid>
    <DataGrid.Columns>
        <local:FormulaColumn Header="합계" Formula="=SUM(B:B)"/>
    </DataGrid.Columns>
</DataGrid>
```

**필요한 라이브러리**:
- **NCalc**: 수식 파싱 및 평가 라이브러리 (MIT 라이선스)
  ```bash
  Install-Package NCalc
  ```
  
  ```csharp
  using NCalc;
  
  var expression = new Expression("2 + 3 * 5");
  var result = expression.Evaluate(); // 17
  
  // 변수 사용
  expression = new Expression("price * quantity");
  expression.Parameters["price"] = 100;
  expression.Parameters["quantity"] = 5;
  result = expression.Evaluate(); // 500
  ```

---

### 옵션 2: Syncfusion DataGrid (Commercial)
**장점**:
- 엑셀과 거의 동일한 기능
- 수식 엔진 내장
- 셀 스타일, 필터 등 풍부한 기능

**단점**:
- 상용 라이선스 필요 ($995~)
- 무거움 (패키지 크기 큰)

**구현 방식**:
```xml
<syncfusion:SfDataGrid ItemsSource="{Binding Properties}">
    <syncfusion:SfDataGrid.Columns>
        <syncfusion:GridNumericColumn 
            MappingName="Total" 
            Formula="=A1+B1"/>
    </syncfusion:SfDataGrid.Columns>
</syncfusion:SfDataGrid>
```

---

### 옵션 3: ClosedXML 기반 (Excel 엔진 활용)
**장점**:
- 이미 프로젝트에서 사용 중 (`ExcelService.cs`)
- Excel 파일로 내보내기 시 수식 유지

**단점**:
- UI와 분리됨 (백엔드 계산만)
- 실시간 업데이트 불가

**구현 방식**:
```csharp
// Excel 내보낼 때만 수식 적용
using (var workbook = new XLWorkbook())
{
    var worksheet = workbook.Worksheets.Add("담보물건");
    
    worksheet.Cell("D1").FormulaA1 = "=SUM(B:B)"; // 합계
    worksheet.Cell("E1").FormulaA1 = "=AVERAGE(C:C)"; // 평균
    
    workbook.SaveAs("output.xlsx");
}
```

---

## 🎨 구현 계획 (옵션 1 기준)

### Phase 1: 기본 인프라 구축 (1주)

#### 1.1 수식 파서 구현
**파일**: `src/NPLogic.Core/Services/FormulaParser.cs`

```csharp
using NCalc;

namespace NPLogic.Core.Services
{
    public class FormulaParser
    {
        /// <summary>
        /// 수식 평가
        /// </summary>
        /// <param name="formula">수식 문자열 (예: "=SUM(A1:A10)")</param>
        /// <param name="context">셀 데이터 컨텍스트</param>
        /// <returns>계산 결과</returns>
        public object Evaluate(string formula, Dictionary<string, object> context)
        {
            if (string.IsNullOrEmpty(formula) || !formula.StartsWith("="))
                return formula;

            formula = formula.Substring(1); // "=" 제거

            // 엑셀 함수 → NCalc 문법 변환
            formula = ConvertExcelToNCalc(formula, context);

            var expression = new Expression(formula);
            
            // 컨텍스트 변수 주입
            foreach (var kvp in context)
            {
                expression.Parameters[kvp.Key] = kvp.Value;
            }

            return expression.Evaluate();
        }

        /// <summary>
        /// 엑셀 함수를 NCalc 문법으로 변환
        /// </summary>
        private string ConvertExcelToNCalc(string formula, Dictionary<string, object> context)
        {
            // SUM(A1:A10) → Sum([A1, A2, ..., A10])
            formula = Regex.Replace(formula, @"SUM\(([A-Z]+\d+):([A-Z]+\d+)\)", 
                match => ConvertSumRange(match, context));

            // AVERAGE(B1:B10) → Average([B1, B2, ...])
            formula = Regex.Replace(formula, @"AVERAGE\(([A-Z]+\d+):([A-Z]+\d+)\)", 
                match => ConvertAverageRange(match, context));

            // SUMIF(A1:A10, ">100", B1:B10) → 조건부 합계
            // TODO: 구현

            return formula;
        }

        /// <summary>
        /// 셀 범위 해석 (A1:A10 → [A1, A2, ..., A10])
        /// </summary>
        private string ConvertSumRange(Match match, Dictionary<string, object> context)
        {
            var startCell = match.Groups[1].Value;
            var endCell = match.Groups[2].Value;
            
            var cells = GetCellRange(startCell, endCell, context);
            var sum = cells.Sum(c => Convert.ToDecimal(c));
            
            return sum.ToString();
        }

        /// <summary>
        /// 셀 범위 값 가져오기
        /// </summary>
        private List<object> GetCellRange(string startCell, string endCell, Dictionary<string, object> context)
        {
            // A1 → (컬럼: A, 행: 1)
            var startCol = Regex.Match(startCell, @"[A-Z]+").Value;
            var startRow = int.Parse(Regex.Match(startCell, @"\d+").Value);
            
            var endCol = Regex.Match(endCell, @"[A-Z]+").Value;
            var endRow = int.Parse(Regex.Match(endCell, @"\d+").Value);

            var result = new List<object>();
            
            // 같은 컬럼 범위만 지원 (A1:A10)
            if (startCol == endCol)
            {
                for (int row = startRow; row <= endRow; row++)
                {
                    var cellKey = $"{startCol}{row}";
                    if (context.ContainsKey(cellKey))
                    {
                        result.Add(context[cellKey]);
                    }
                }
            }

            return result;
        }
    }
}
```

#### 1.2 수식 컬럼 구현
**파일**: `src/NPLogic.UI/Controls/FormulaColumn.cs`

```csharp
using System.Windows.Controls;

namespace NPLogic.UI.Controls
{
    /// <summary>
    /// 수식을 지원하는 DataGrid 컬럼
    /// </summary>
    public class FormulaColumn : DataGridTextColumn
    {
        private readonly FormulaParser _parser = new();

        public static readonly DependencyProperty FormulaProperty =
            DependencyProperty.Register(
                nameof(Formula),
                typeof(string),
                typeof(FormulaColumn));

        /// <summary>
        /// 수식 (예: "=SUM(A:A)")
        /// </summary>
        public string Formula
        {
            get => (string)GetValue(FormulaProperty);
            set => SetValue(FormulaProperty, value);
        }

        protected override FrameworkElement GenerateElement(
            DataGridCell cell, 
            object dataItem)
        {
            var textBlock = new TextBlock();
            
            if (!string.IsNullOrEmpty(Formula))
            {
                // 행 데이터를 컨텍스트로 변환
                var context = BuildContext(dataItem);
                
                try
                {
                    var result = _parser.Evaluate(Formula, context);
                    textBlock.Text = result?.ToString() ?? "";
                }
                catch (Exception ex)
                {
                    textBlock.Text = $"#ERROR: {ex.Message}";
                    textBlock.Foreground = Brushes.Red;
                }
            }
            else
            {
                // 일반 바인딩
                textBlock.SetBinding(TextBlock.TextProperty, Binding);
            }

            return textBlock;
        }

        /// <summary>
        /// 행 데이터 → 셀 컨텍스트 변환
        /// </summary>
        private Dictionary<string, object> BuildContext(object dataItem)
        {
            var context = new Dictionary<string, object>();
            
            if (dataItem == null) return context;

            var properties = dataItem.GetType().GetProperties();
            
            foreach (var prop in properties)
            {
                var value = prop.GetValue(dataItem);
                context[prop.Name] = value ?? 0;
            }

            return context;
        }
    }
}
```

---

### Phase 2: 대시보드 적용 (3일)

#### 2.1 진행률 자동 계산
**파일**: `src/NPLogic.App/Views/DashboardView.xaml`

**수정 전**:
```xml
<DataGridTextColumn Header="진행률" 
                    Binding="{Binding ProgressPercent}" 
                    Width="80"/>
```

**수정 후**:
```xml
<local:FormulaColumn Header="진행률" 
                     Formula="=COUNT(Status='완료') / 9 * 100"
                     Width="80">
    <DataGridTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="HorizontalAlignment" Value="Right"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
        </Style>
    </DataGridTextColumn.ElementStyle>
</local:FormulaColumn>
```

#### 2.2 그리드 Footer Row 추가
```xml
<DataGrid ItemsSource="{Binding Properties}">
    <!-- 컬럼 정의 -->
    
    <!-- Footer Row -->
    <DataGrid.RowStyle>
        <Style TargetType="DataGridRow">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsFooterRow}" Value="True">
                    <Setter Property="Background" Value="{StaticResource BlueGray100Brush}"/>
                    <Setter Property="FontWeight" Value="Bold"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </DataGrid.RowStyle>
</DataGrid>
```

#### 2.3 ViewModel 수정
**파일**: `src/NPLogic.App/ViewModels/DashboardViewModel.cs`

```csharp
// Footer Row 데이터 추가
public class DashboardRowViewModel
{
    public bool IsFooterRow { get; set; }
    public string 차주번호 { get; set; }
    // ... 기타 필드
}

private void LoadProperties()
{
    Properties.Clear();
    
    // 데이터 로드
    foreach (var property in _properties)
    {
        Properties.Add(new DashboardRowViewModel { /* ... */ });
    }
    
    // Footer Row 추가
    Properties.Add(new DashboardRowViewModel
    {
        IsFooterRow = true,
        차주번호 = "합계",
        // 수식은 FormulaColumn에서 자동 계산
    });
}
```

---

### Phase 3: 평가 탭 적용 (3일)

#### 3.1 현금흐름 테이블 수식 적용
**파일**: `src/NPLogic.App/Views/EvaluationTab.xaml`

```xml
<!-- 현금흐름 입력 그리드 (line 172-266) -->
<DataGrid ItemsSource="{Binding CashFlows}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="시점" Binding="{Binding Period}" Width="80"/>
        
        <DataGridTextColumn Header="현금유입" 
                            Binding="{Binding CashInflow, StringFormat=N0}" 
                            Width="120"/>
        
        <DataGridTextColumn Header="현금유출" 
                            Binding="{Binding CashOutflow, StringFormat=N0}" 
                            Width="120"/>
        
        <!-- 수식 컬럼 추가 -->
        <local:FormulaColumn Header="순현금흐름" 
                             Formula="=CashInflow - CashOutflow"
                             Width="120"/>
        
        <local:FormulaColumn Header="누적현금흐름" 
                             Formula="=SUM(NetCashFlow[0:현재행])"
                             Width="120"/>
    </DataGrid.Columns>
</DataGrid>
```

#### 3.2 낙찰가 예상 수식
```xml
<StackPanel Orientation="Horizontal">
    <TextBlock Text="예상 낙찰가: " Style="{StaticResource LabelStyle}"/>
    
    <!-- 수식 바인딩 TextBlock -->
    <TextBlock Text="{Binding EstimatedAuctionPrice, StringFormat=N0}" 
               Style="{StaticResource AmountStyle}"/>
    
    <!-- 수식 표시 -->
    <TextBlock Text="(= 감정평가액 × 평균낙찰률)" 
               FontSize="11" 
               Opacity="0.6"
               Margin="8,0,0,0"/>
</StackPanel>
```

ViewModel:
```csharp
public decimal EstimatedAuctionPrice => 
    _parser.Evaluate("=AppraisalValue * AverageAuctionRate", GetContext());
```

---

### Phase 4: 권리분석 탭 적용 (3일)

#### 4.1 선순위 합계 수식
**파일**: `src/NPLogic.App/Views/RightsAnalysisTab.xaml`

```xml
<DataGrid ItemsSource="{Binding SeniorRightItems}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="선순위 구분" 
                            Binding="{Binding Category}" 
                            Width="150"/>
        
        <DataGridTextColumn Header="DD 금액" 
                            Binding="{Binding DDAmount, StringFormat=N0}" 
                            Width="120"/>
        
        <DataGridTextColumn Header="평가자 반영금액" 
                            Binding="{Binding ReflectedAmount, StringFormat=N0}" 
                            Width="140"/>
        
        <DataGridTextColumn Header="상세추정 근거" 
                            Binding="{Binding Rationale}" 
                            Width="*"/>
    </DataGrid.Columns>
    
    <!-- Footer Row: 합계 -->
    <DataGrid.RowStyle>
        <Style TargetType="DataGridRow">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsTotal}" Value="True">
                    <Setter Property="Background" Value="#FFE3F2FD"/>
                    <Setter Property="FontWeight" Value="Bold"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </DataGrid.RowStyle>
</DataGrid>
```

ViewModel:
```csharp
public void CalculateTotals()
{
    // Footer Row 추가
    SeniorRightItems.Add(new SeniorRightItem
    {
        IsTotal = true,
        Category = "합계",
        DDAmount = SeniorRightItems.Sum(x => x.DDAmount),
        ReflectedAmount = SeniorRightItems.Sum(x => x.ReflectedAmount)
    });
}
```

#### 4.2 배당 시뮬레이션 수식
```csharp
// 기존 C# 계산 → 수식으로 표시
public string DistributionFormula { get; set; } = 
    "=낙찰가 - 경매수수료 - 선순위합계";

public decimal DistributionAmount
{
    get
    {
        var context = new Dictionary<string, object>
        {
            ["낙찰가"] = AuctionPrice,
            ["경매수수료"] = AuctionFee,
            ["선순위합계"] = TotalSeniorRights
        };
        
        return (decimal)_parser.Evaluate(DistributionFormula, context);
    }
}
```

---

### Phase 5: 설정 관리 - 사용자 정의 수식 (2일)

#### 5.1 전역 수식 정의
**파일**: `src/NPLogic.Core/Models/FormulaTemplate.cs`

```csharp
public class FormulaTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } // "회수율"
    public string Formula { get; set; } // "({낙찰가} - {경매비용} - {선순위합계}) / {LoanCap} * 100"
    public string Category { get; set; } // "권리분석", "평가" 등
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 5.2 설정 화면 UI
**파일**: `src/NPLogic.App/Views/SettingsView.xaml`

```xml
<!-- 수식 관리 섹션 (기존 코드 확장) -->
<DataGrid ItemsSource="{Binding FormulaTemplates}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="수식명" 
                            Binding="{Binding Name}" 
                            Width="120"/>
        
        <DataGridTextColumn Header="수식" 
                            Binding="{Binding Formula}" 
                            Width="*"/>
        
        <DataGridTextColumn Header="카테고리" 
                            Binding="{Binding Category}" 
                            Width="100"/>
        
        <DataGridTemplateColumn Header="액션" Width="150">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Button Content="편집" 
                                Command="{Binding EditFormulaCommand}"/>
                        <Button Content="삭제" 
                                Command="{Binding DeleteFormulaCommand}"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>

<Button Content="+ 수식 추가" 
        Command="{Binding AddFormulaCommand}"/>
```

#### 5.3 수식 편집 모달
```xml
<Window x:Class="NPLogic.Views.FormulaEditorDialog">
    <Grid>
        <StackPanel Margin="20">
            <TextBlock Text="수식 편집" FontSize="18" FontWeight="Bold"/>
            
            <TextBlock Text="수식명" Margin="0,16,0,4"/>
            <TextBox Text="{Binding FormulaName}"/>
            
            <TextBlock Text="수식" Margin="0,12,0,4"/>
            <TextBox Text="{Binding Formula}" 
                     Height="80" 
                     TextWrapping="Wrap"
                     AcceptsReturn="True"/>
            
            <TextBlock Text="사용 가능한 변수" Margin="0,12,0,4"/>
            <WrapPanel>
                <Button Content="{낙찰가}" Command="{Binding InsertVariableCommand}" CommandParameter="낙찰가"/>
                <Button Content="{경매비용}" Command="{Binding InsertVariableCommand}" CommandParameter="경매비용"/>
                <Button Content="{선순위합계}" Command="{Binding InsertVariableCommand}" CommandParameter="선순위합계"/>
                <!-- 더 많은 변수 버튼 -->
            </WrapPanel>
            
            <TextBlock Text="테스트" Margin="0,16,0,4"/>
            <StackPanel Orientation="Horizontal">
                <Button Content="수식 테스트" Command="{Binding TestFormulaCommand}"/>
                <TextBlock Text="{Binding TestResult}" 
                           Margin="12,0,0,0"
                           FontWeight="SemiBold"/>
            </StackPanel>
            
            <StackPanel Orientation="Horizontal" 
                        HorizontalAlignment="Right" 
                        Margin="0,24,0,0">
                <Button Content="저장" Command="{Binding SaveCommand}"/>
                <Button Content="취소" Command="{Binding CancelCommand}"/>
            </StackPanel>
        </StackPanel>
    </Grid>
</Window>
```

---

## 🧪 테스트 시나리오

### 1. 단위 테스트
**파일**: `tests/NPLogic.Tests/Services/FormulaParserTests.cs`

```csharp
[TestFixture]
public class FormulaParserTests
{
    private FormulaParser _parser;

    [SetUp]
    public void Setup()
    {
        _parser = new FormulaParser();
    }

    [Test]
    public void Evaluate_SimpleAddition_ReturnsCorrectResult()
    {
        // Arrange
        var formula = "=2 + 3";
        var context = new Dictionary<string, object>();

        // Act
        var result = _parser.Evaluate(formula, context);

        // Assert
        Assert.AreEqual(5, result);
    }

    [Test]
    public void Evaluate_SumFunction_ReturnsCorrectResult()
    {
        // Arrange
        var formula = "=SUM(A1:A3)";
        var context = new Dictionary<string, object>
        {
            ["A1"] = 10,
            ["A2"] = 20,
            ["A3"] = 30
        };

        // Act
        var result = _parser.Evaluate(formula, context);

        // Assert
        Assert.AreEqual(60, result);
    }

    [Test]
    public void Evaluate_ComplexFormula_ReturnsCorrectResult()
    {
        // Arrange
        var formula = "=(낙찰가 - 경매비용 - 선순위합계) / LoanCap * 100";
        var context = new Dictionary<string, object>
        {
            ["낙찰가"] = 200000000,
            ["경매비용"] = 5000000,
            ["선순위합계"] = 150000000,
            ["LoanCap"] = 120000000
        };

        // Act
        var result = _parser.Evaluate(formula, context);

        // Assert
        Assert.AreEqual(37.5, result); // (200M - 5M - 150M) / 120M * 100
    }
}
```

### 2. 통합 테스트
- 대시보드에서 진행률 자동 계산 확인
- 권리분석에서 선순위 합계 자동 업데이트 확인
- 평가 탭에서 현금흐름 수식 계산 확인

---

## 📦 필요한 NuGet 패키지

```xml
<!-- src/NPLogic.Core/NPLogic.Core.csproj -->
<ItemGroup>
    <!-- 수식 파싱 라이브러리 -->
    <PackageReference Include="NCalc" Version="1.12.0" />
    
    <!-- 정규식 유틸리티 (이미 .NET 기본 포함) -->
    <!-- System.Text.RegularExpressions -->
</ItemGroup>
```

설치 명령:
```powershell
cd src/NPLogic.Core
dotnet add package NCalc
```

---

## 📋 작업 일정

| Phase | 작업 내용 | 소요 시간 | 담당 |
|-------|----------|----------|------|
| Phase 1 | 기본 인프라 구축 (FormulaParser, FormulaColumn) | 1주 | 개발자 |
| Phase 2 | 대시보드 적용 (진행률, Footer Row) | 3일 | 개발자 |
| Phase 3 | 평가 탭 적용 (현금흐름, 낙찰가) | 3일 | 개발자 |
| Phase 4 | 권리분석 탭 적용 (선순위 합계, 배당) | 3일 | 개발자 |
| Phase 5 | 설정 관리 (사용자 정의 수식) | 2일 | 개발자 |
| 테스트 | 단위/통합 테스트, 버그 수정 | 3일 | QA + 개발자 |
| **합계** | | **약 3주** | |

---

## 🚀 향후 확장 가능성

### 고급 함수 추가
- **VLOOKUP**: 다른 테이블에서 값 찾기
- **IF**: 조건부 로직
- **MAX/MIN**: 최댓값/최솟값
- **ROUND**: 반올림
- **COUNTIF**: 조건부 개수
- **DATE**: 날짜 계산

### 엑셀과 동기화
- 수식 포함하여 Excel 내보내기
- Excel 파일에서 수식 가져오기
- ClosedXML과 통합

### 시각적 수식 편집기
- 드래그앤드롭으로 수식 작성
- 수식 자동 완성
- 실시간 미리보기

---

## 🔒 제약사항 및 주의사항

### 제약사항
1. **순환 참조 방지**: A1이 B1을 참조하고 B1이 A1을 참조하는 경우 무한루프
   - 해결: 의존성 그래프 분석, 순환 감지

2. **성능 최적화**: 수천 개 행에서 수식 재계산 시 느려질 수 있음
   - 해결: 캐싱, 변경된 셀만 재계산

3. **에러 처리**: 나누기 0, 형식 불일치 등
   - 해결: Try-Catch, 에러 메시지 표시

### 보안 고려사항
- **수식 주입 공격**: 악의적인 수식 실행 방지
  - 허용된 함수만 사용
  - 파일 접근, 네트워크 호출 차단

---

## 📚 참고 자료

### 라이브러리
- **NCalc**: https://github.com/ncalc/ncalc
- **NCalc Wiki**: https://github.com/ncalc/ncalc/wiki
- **ClosedXML**: https://github.com/ClosedXML/ClosedXML

### 엑셀 함수 레퍼런스
- Microsoft Excel 함수: https://support.microsoft.com/ko-kr/office/excel-함수-사전순-b3944572-255d-4efb-bb96-c6d90033e188

### WPF DataGrid 커스터마이징
- DataGrid 컬럼 확장: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid

---

## ✅ 체크리스트

- [ ] NCalc 패키지 설치
- [ ] FormulaParser 클래스 구현
- [ ] FormulaColumn 클래스 구현
- [ ] 단위 테스트 작성
- [ ] 대시보드 진행률 수식 적용
- [ ] 평가 탭 현금흐름 수식 적용
- [ ] 권리분석 선순위 합계 수식 적용
- [ ] 설정 관리 - 사용자 정의 수식 UI
- [ ] 통합 테스트
- [ ] 성능 테스트 (1000+ 행)
- [ ] 문서화 (사용자 매뉴얼)

---

**작성일**: 2025-12-03  
**작성자**: AI Assistant  
**버전**: 1.0










