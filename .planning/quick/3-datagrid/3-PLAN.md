---
phase: quick
plan: 3
type: execute
wave: 1
depends_on: []
files_modified:
  - src/NPLogic.App/Views/CollateralPropertyView.xaml
  - src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs
autonomous: true
requirements: [QUICK-3]
must_haves:
  truths:
    - "등기부등본 DataGrid 셀 값들이 좌우 여유가 있어 답답하지 않다"
    - "갑구/을구 행을 위/아래로 이동할 수 있다"
    - "순위 재설정 버튼을 누르면 RankNo가 1,2,3... 순으로 갱신된다"
    - "을구 테이블의 담보종류 컬럼이 피담보채무로 표시된다"
  artifacts:
    - path: "src/NPLogic.App/Views/CollateralPropertyView.xaml"
      provides: "패딩 확대, 행이동 버튼, 순위재설정 버튼, 컬럼명 변경"
    - path: "src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs"
      provides: "행이동 Command, 순위재설정 Command"
  key_links:
    - from: "CollateralPropertyView.xaml 행이동 버튼"
      to: "PropertyDetailViewModel MoveGapguRowUp/Down, MoveEulguRowUp/Down"
      via: "Command binding"
    - from: "CollateralPropertyView.xaml 순위재설정 버튼"
      to: "PropertyDetailViewModel ResetGapguRankNumbers, ResetEulguRankNumbers"
      via: "Command binding"
---

<objective>
등기부등본 정보 섹션의 DataGrid UX를 개선한다: 셀 패딩 확대, 갑구/을구 행 이동 기능, 순위 재설정 버튼, 을구 컬럼명 변경.

Purpose: 데이터가 셀 안에서 답답하게 보이는 문제 해결 + 행 순서 조정 기능으로 사용자 편의 향상
Output: 패딩이 넓어진 DataGrid, 행 이동/순위재설정 버튼이 동작하는 등기부등본 탭
</objective>

<execution_context>
@C:/Users/kim/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/kim/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@src/NPLogic.App/Views/CollateralPropertyView.xaml
@src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs
@src/NPLogic.Core/Models/RegistryGapguRow.cs
@src/NPLogic.Core/Models/RegistryEulguRow.cs

<interfaces>
<!-- RegistryGapguRow (NPLogic.Core/Models/RegistryGapguRow.cs) -->
public class RegistryGapguRow {
    public Guid Id { get; set; }
    public string? RankNo { get; set; }      // 순위번호 (string, OCR 추출)
    public int? SortIndex { get; set; }       // 표 원본 순서
    // ... other fields
}

<!-- RegistryEulguRow (NPLogic.Core/Models/RegistryEulguRow.cs) -->
public class RegistryEulguRow {
    public Guid Id { get; set; }
    public string? RankNo { get; set; }
    public string? CollateralTypeUserInput { get; set; } // 담보종류 (binding target)
    public int? SortIndex { get; set; }
    // ... other fields
}

<!-- PropertyDetailViewModel (existing patterns) -->
[ObservableProperty]
private ObservableCollection<RegistryGapguRow> _registryGapguRows = new();
[ObservableProperty]
private ObservableCollection<RegistryEulguRow> _registryEulguRows = new();

// Existing commands use [RelayCommand] attribute + async Task pattern
// Save: _registryRepository.UpdateGapguRowAsync(row), _registryRepository.UpdateEulguRowAsync(row)
// Add row sets SortIndex = Max(existing) + 1

<!-- Styles (CollateralPropertyView.xaml, lines 39-83) -->
SectionDataGridColumnHeaderStyle: Padding="12,6"
SectionDataGridCellStyle: Padding="10,4"
SectionDataGridTextBlockStyle: no padding, FontSize/Alignment only

<!-- 갑구 서브섹션 헤더 (line 594-598) -->
<Border Background="{StaticResource BlueGray100Brush}" Padding="12,8">
    <Grid>
        <TextBlock Text="2. 갑구" FontWeight="SemiBold" VerticalAlignment="Center"/>
    </Grid>
</Border>

<!-- 을구 서브섹션 헤더 (line 677-681) -->
<Border Background="{StaticResource BlueGray100Brush}" Padding="12,8">
    <Grid>
        <TextBlock Text="3. 을구" FontWeight="SemiBold" VerticalAlignment="Center"/>
    </Grid>
</Border>

<!-- 을구 담보종류 컬럼 (line 722) -->
<DataGridTextColumn Header="담보종류" Binding="{Binding CollateralTypeUserInput, ...}" />

<!-- 갑구/을구 행추가/행삭제 버튼 영역 (lines 651-670, 737-756) -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="12,0,12,8">
    <!-- 행추가, 행삭제 buttons -->
</StackPanel>
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: 셀 패딩 확대 + 을구 컬럼명 변경 (XAML)</name>
  <files>src/NPLogic.App/Views/CollateralPropertyView.xaml</files>
  <action>
CollateralPropertyView.xaml의 UserControl.Resources 내 스타일 수정:

1. **SectionDataGridColumnHeaderStyle** (line 41): Padding을 "12,6" -> "16,8"로 변경
2. **SectionDataGridCellStyle** (line 54): Padding을 "10,4" -> "16,6"로 변경
3. **SectionDataGridTextBlockStyle** (line 69): Padding="4,0" 추가 (좌우 4px 여유)

4. **을구 컬럼명 변경** (line 722): Header="담보종류" -> Header="피담보채무"

5. **갑구 서브섹션 헤더** (lines 594-598): Grid에 Grid.ColumnDefinitions 추가하여 왼쪽에 TextBlock, 오른쪽에 "순위 재설정" 버튼 배치.
   헤더 Grid 구조:
   ```xml
   <Grid>
       <Grid.ColumnDefinitions>
           <ColumnDefinition Width="*"/>
           <ColumnDefinition Width="Auto"/>
       </Grid.ColumnDefinitions>
       <TextBlock Grid.Column="0" Text="2. 갑구" FontWeight="SemiBold" VerticalAlignment="Center"/>
       <Button Grid.Column="1"
               Command="{Binding ResetGapguRankNumbersCommand}"
               Style="{StaticResource MaterialDesignFlatButton}"
               Padding="8,4" Height="26"
               ToolTip="현재 순서대로 순위번호를 1,2,3... 재설정">
           <StackPanel Orientation="Horizontal">
               <materialDesign:PackIcon Kind="FormatListNumbered" Width="14" Height="14" VerticalAlignment="Center"/>
               <TextBlock Text="순위 재설정" Margin="4,0,0,0" FontSize="{StaticResource FontSizeSmall}" VerticalAlignment="Center"/>
           </StackPanel>
       </Button>
   </Grid>
   ```

6. **을구 서브섹션 헤더** (lines 677-681): 동일 구조로 "순위 재설정" 버튼 추가. Command는 ResetEulguRankNumbersCommand.

7. **갑구 행추가/행삭제 버튼 영역** (lines 651-670): 기존 StackPanel에 행이동 버튼 2개를 행추가 버튼 앞에 추가.
   SelectedItem을 CommandParameter로 전달하기 위해 GapguDataGrid의 SelectedItem 사용.
   ```xml
   <Button Command="{Binding MoveGapguRowUpCommand}"
           CommandParameter="{Binding SelectedItem, ElementName=GapguDataGrid}"
           Style="{StaticResource MaterialDesignFlatButton}"
           Padding="8,4" Height="28" ToolTip="선택 행 위로 이동">
       <materialDesign:PackIcon Kind="ArrowUp" Width="16" Height="16"/>
   </Button>
   <Button Command="{Binding MoveGapguRowDownCommand}"
           CommandParameter="{Binding SelectedItem, ElementName=GapguDataGrid}"
           Style="{StaticResource MaterialDesignFlatButton}"
           Padding="8,4" Height="28" Margin="0,0,8,0" ToolTip="선택 행 아래로 이동">
       <materialDesign:PackIcon Kind="ArrowDown" Width="16" Height="16"/>
   </Button>
   ```
   그 뒤에 기존 행추가/행삭제 버튼이 이어진다.

8. **을구 행추가/행삭제 버튼 영역** (lines 737-756): 동일 패턴. ElementName=EulguDataGrid, Command는 MoveEulguRowUpCommand/MoveEulguRowDownCommand.
  </action>
  <verify>
    <automated>cd C:/gits/Dev-NPLogic && rtk dotnet build NPLogic.sln</automated>
  </verify>
  <done>
- 3개 DataGrid 셀 좌우 패딩이 넓어짐 (ColumnHeader 16,8 / Cell 16,6 / TextBlock 4,0)
- 을구 테이블 헤더가 "피담보채무"로 표시
- 갑구/을구 서브섹션 헤더 오른쪽에 "순위 재설정" 버튼 노출
- 갑구/을구 행추가/삭제 영역에 위/아래 화살표 버튼 노출
  </done>
</task>

<task type="auto">
  <name>Task 2: 행 이동 + 순위 재설정 ViewModel 로직</name>
  <files>src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs</files>
  <action>
PropertyDetailViewModel.cs에 6개의 RelayCommand 메서드를 추가한다. 기존 AddGapguRowAsync/DeleteGapguRowAsync 메서드 근처 (line ~2320 부근)에 배치.

**1. MoveGapguRowUp(RegistryGapguRow? row)**
```csharp
[RelayCommand]
private void MoveGapguRowUp(RegistryGapguRow? row)
{
    if (row == null) return;
    var index = RegistryGapguRows.IndexOf(row);
    if (index <= 0) return;
    RegistryGapguRows.Move(index, index - 1);
    // SortIndex 재계산
    for (int i = 0; i < RegistryGapguRows.Count; i++)
        RegistryGapguRows[i].SortIndex = i + 1;
}
```

**2. MoveGapguRowDown(RegistryGapguRow? row)** - 동일 패턴, index >= Count-1 이면 return, Move(index, index+1)

**3. MoveEulguRowUp(RegistryEulguRow? row)** - 갑구와 동일 패턴, RegistryEulguRows 대상

**4. MoveEulguRowDown(RegistryEulguRow? row)** - 동일 패턴

**5. ResetGapguRankNumbers()**
```csharp
[RelayCommand]
private async Task ResetGapguRankNumbersAsync()
{
    if (_registryRepository == null || RegistryGapguRows.Count == 0) return;
    try
    {
        for (int i = 0; i < RegistryGapguRows.Count; i++)
        {
            RegistryGapguRows[i].RankNo = (i + 1).ToString();
            RegistryGapguRows[i].SortIndex = i + 1;
            await _registryRepository.UpdateGapguRowAsync(RegistryGapguRows[i]);
        }
        SuccessMessage = "갑구 순위번호가 재설정되었습니다.";
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"갑구 순위 재설정 실패: {ex.Message}");
        ErrorMessage = $"갑구 순위 재설정 실패: {ex.Message}";
    }
}
```

**6. ResetEulguRankNumbersAsync()** - 동일 패턴, RegistryEulguRows 대상, UpdateEulguRowAsync 호출.

주의사항:
- Move 메서드는 void (동기). ObservableCollection.Move()가 UI를 즉시 갱신한다.
- Move 후 SortIndex만 메모리에서 갱신하고 DB 저장은 하지 않는다. 기존 "저장" 버튼(SaveRegistryUserInputsCommand)이 일괄 저장하므로, 이동 후 자연스럽게 저장된다.
- ResetRankNumbers는 RankNo(표시값)와 SortIndex(정렬값) 모두 갱신하고 즉시 DB 저장한다. 순위번호는 중요 데이터이므로 즉시 저장이 적절.
- [RelayCommand]는 CommunityToolkit.Mvvm이 자동으로 MoveGapguRowUpCommand 등의 ICommand 프로퍼티를 생성한다.
  </action>
  <verify>
    <automated>cd C:/gits/Dev-NPLogic && rtk dotnet build NPLogic.sln</automated>
  </verify>
  <done>
- 갑구/을구 행 선택 후 위/아래 버튼 클릭 시 행 위치가 바뀌고 SortIndex가 재계산됨
- "순위 재설정" 클릭 시 현재 표시 순서대로 RankNo가 1,2,3... 으로 갱신되고 DB에 즉시 저장됨
- dotnet build 성공
  </done>
</task>

</tasks>

<verification>
1. `dotnet build NPLogic.sln` 성공
2. 앱 실행 후 등기부등본 탭 진입 시 DataGrid 셀 패딩이 이전보다 넓어 보임
3. 갑구/을구 행 선택 후 화살표 버튼으로 행 이동 가능
4. "순위 재설정" 버튼 클릭 시 순위번호가 1,2,3... 으로 변경됨
5. 을구 테이블 헤더에 "피담보채무"가 표시됨
</verification>

<success_criteria>
- 등기부등본 3개 DataGrid의 셀 패딩이 확대됨
- 갑구/을구 행 이동(위/아래) 동작
- 갑구/을구 "순위 재설정" 버튼이 RankNo를 1,2,3... 으로 갱신하고 DB 저장
- 을구 "담보종류" 컬럼이 "피담보채무"로 표시
- 빌드 에러 없음
</success_criteria>

<output>
After completion, create `.planning/quick/3-datagrid/3-SUMMARY.md`
</output>
