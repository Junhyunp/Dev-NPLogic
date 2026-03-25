---
phase: quick-2-pdf
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/NPLogic.App/Views/RegistryImagePopupWindow.xaml
  - src/NPLogic.App/Views/RegistryImagePopupWindow.xaml.cs
  - src/NPLogic.App/Views/CollateralPropertyView.xaml
  - src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs
autonomous: true
requirements: [PDF-POPUP-01]

must_haves:
  truths:
    - "등기부등본 버튼 클릭 시 별도 팝업 창이 열린다"
    - "팝업 내에서 물건지(RegistryRun)를 ComboBox로 선택할 수 있다"
    - "선택한 물건지의 등기부등본 이미지들이 팝업 내 스크롤 가능 영역에 표시된다"
    - "팝업 닫기(X) 버튼으로 창을 닫을 수 있다"
  artifacts:
    - path: "src/NPLogic.App/Views/RegistryImagePopupWindow.xaml"
      provides: "등기부등본 이미지 팝업 Window XAML"
    - path: "src/NPLogic.App/Views/RegistryImagePopupWindow.xaml.cs"
      provides: "팝업 code-behind (run 선택 + 이미지 로드 로직)"
  key_links:
    - from: "PropertyDetailViewModel.ToggleRegistryImagePanel"
      to: "RegistryImagePopupWindow"
      via: "new RegistryImagePopupWindow(RegistryRuns).Show()"
      pattern: "RegistryImagePopupWindow"
---

<objective>
등기부등본 PDF 이미지 뷰어를 CollateralPropertyView 인라인 패널에서 독립 팝업 창으로 변경한다.

Purpose: 인라인 패널은 화면 공간을 과도하게 차지하고 스크롤 충돌이 발생한다. 팝업으로 분리하면 물건 정보를 보면서 동시에 등기부등본 이미지를 참조할 수 있다.
Output: RegistryImagePopupWindow.xaml/.xaml.cs 생성, CollateralPropertyView.xaml에서 인라인 뷰어 제거, ViewModel 커맨드 수정
</objective>

<execution_context>
@C:/Users/kim/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/kim/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@src/NPLogic.App/Views/MapPopupWindow.xaml (팝업 창 패턴 참조)
@src/NPLogic.App/Views/MapPopupWindow.xaml.cs (code-behind 패턴 참조)
@src/NPLogic.App/Views/CollateralPropertyView.xaml (인라인 뷰어 제거 대상: lines 761-811)
@src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs (ToggleRegistryImagePanel 커맨드: line 4571, OnSelectedImageRunChanged: line 4584, ConvertBase64ToBitmapImage: line 4607)

<interfaces>
<!-- PropertyDetailViewModel.cs 관련 속성 (CommunityToolkit.Mvvm [ObservableProperty]) -->
From src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs:
```csharp
[ObservableProperty]
private ObservableCollection<RegistryRun> _registryRuns = new();

[ObservableProperty]
private bool _isRegistryImageExpanded;

[ObservableProperty]
private RegistryRun? _selectedImageRun;

[ObservableProperty]
private ObservableCollection<BitmapImage> _registryDocumentImages = new();

[RelayCommand]
private void ToggleRegistryImagePanel() { ... }

partial void OnSelectedImageRunChanged(RegistryRun? value) { ... }

private static BitmapImage? ConvertBase64ToBitmapImage(string base64String) { ... }
```

From RegistryRun model (used in ComboBox):
- `SourcePdfName` (string) — ComboBox DisplayMemberPath
- `SummaryImagesBase64` (List<string>) — base64 이미지 데이터
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: RegistryImagePopupWindow 생성 + 인라인 뷰어 제거</name>
  <files>
    src/NPLogic.App/Views/RegistryImagePopupWindow.xaml,
    src/NPLogic.App/Views/RegistryImagePopupWindow.xaml.cs,
    src/NPLogic.App/Views/CollateralPropertyView.xaml
  </files>
  <action>
1. **RegistryImagePopupWindow.xaml 생성** — MapPopupWindow.xaml 패턴을 따른다:
   - `x:Class="NPLogic.Views.RegistryImagePopupWindow"`
   - Window 속성: Width="900", Height="700", WindowStartupLocation="CenterOwner", ResizeMode="CanResize", Background="{StaticResource BackgroundBrush}"
   - Row 0: PrimaryBrush 배경 헤더 Border — 왼쪽에 PackIcon(Kind="FileDocument") + TextBlock "등기부등본 이미지", 오른쪽에 닫기 버튼 (CloseButton_Click)
   - Row 1: 콘텐츠 영역 Grid
     - 상단: BlueGray100Brush 배경 Border, "물건지 선택:" 라벨 + ComboBox (x:Name="RunComboBox", DisplayMemberPath="SourcePdfName", SelectionChanged="RunComboBox_SelectionChanged")
     - 하단: ScrollViewer(VerticalScrollBarVisibility="Auto") 안에 ItemsControl(x:Name="ImageList") — DataTemplate에 Border(BorderBrush, BorderThickness="1", Margin="0,0,0,8", CornerRadius="4") 안에 Image(Stretch="Uniform")
     - 이미지 없을 때 안내 TextBlock (x:Name="NoImageText"): "선택한 물건지에 대한 등기부등본 이미지가 없습니다." — 초기 Collapsed, 이미지 로드 후 Count==0일 때 Visible로 전환

2. **RegistryImagePopupWindow.xaml.cs 생성**:
   - 네임스페이스: `NPLogic.Views`
   - 생성자: `RegistryImagePopupWindow(IList<RegistryRun> runs)` — runs를 RunComboBox.ItemsSource에 할당, runs.Count > 0이면 RunComboBox.SelectedIndex = 0
   - `RunComboBox_SelectionChanged`: 선택된 RegistryRun의 SummaryImagesBase64를 순회하며 BitmapImage로 변환, ObservableCollection에 담아 ImageList.ItemsSource에 할당. 변환 로직은 PropertyDetailViewModel.ConvertBase64ToBitmapImage와 동일한 로직을 private 메서드로 복제 (static ConvertBase64ToBitmapImage). 이미지 0개면 NoImageText.Visibility = Visible, 아니면 Collapsed
   - `CloseButton_Click`: `Close()`
   - RegistryRun 타입 사용을 위해 `using NPLogic.Core.Models;` (또는 RegistryRun이 있는 네임스페이스) import. 파일 상단의 기존 RegistryRun import 경로는 PropertyDetailViewModel.cs를 참조하여 동일하게 맞출 것.

3. **CollateralPropertyView.xaml에서 인라인 뷰어 제거**:
   - lines 761-811 전체 삭제 (등기부등본 이미지 뷰어 토글 패널 Border와 그 안의 StackPanel, ScrollViewer, ItemsControl, 안내 TextBlock 전부)
   - 버튼(line 522-534)은 그대로 유지 — ToggleRegistryImagePanelCommand 바인딩 유지
  </action>
  <verify>
    <automated>cd C:/gits/Dev-NPLogic &amp;&amp; rtk dotnet build NPLogic.sln</automated>
  </verify>
  <done>RegistryImagePopupWindow.xaml/.xaml.cs가 생성되고, CollateralPropertyView.xaml에서 인라인 이미지 뷰어(lines 761-811)가 제거되었으며, 빌드가 성공한다.</done>
</task>

<task type="auto">
  <name>Task 2: ViewModel 커맨드를 팝업 열기로 변경</name>
  <files>
    src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs
  </files>
  <action>
1. **ToggleRegistryImagePanel() 메서드 수정** (line 4571):
   - 기존 IsRegistryImageExpanded 토글 로직을 제거
   - 대신 RegistryRuns가 비어있으면 return (이미지 없음)
   - `var popup = new NPLogic.Views.RegistryImagePopupWindow(RegistryRuns);`
   - `popup.Owner = Application.Current.MainWindow;`
   - `popup.Show();` (모달이 아닌 비모달 — 물건 정보와 동시 참조 가능)
   - 메서드 이름은 그대로 유지 (XAML 바인딩 ToggleRegistryImagePanelCommand 호환성)

2. **불필요 속성 정리**:
   - `_isRegistryImageExpanded` (line 602): [ObservableProperty] 어노테이션과 필드 제거. XAML에서 더 이상 바인딩하지 않으므로 불필요.
   - `_selectedImageRun` (line 605): 제거 — 팝업이 자체적으로 ComboBox 선택을 관리
   - `_registryDocumentImages` (line 608): 제거 — 팝업이 자체적으로 이미지 컬렉션 관리
   - `OnSelectedImageRunChanged` partial method (line 4584-4602): 제거 — 팝업 code-behind로 이동됨
   - `ConvertBase64ToBitmapImage` static method (line 4607~): 제거 — 팝업 code-behind에 복제됨

3. **using 추가**: 파일 상단에 `using NPLogic.Views;` 가 없으면 추가 (RegistryImagePopupWindow 참조를 위해). 또는 fully-qualified name 사용.

4. **주의**: `_registryRuns` (line 586)와 `_selectedRegistryRun` (line 589)는 유지 — 이들은 담보물건 탭의 DataGrid 드롭다운(등기부 run 선택)에서 사용됨. 삭제 대상은 이미지 뷰어 전용 속성들(`_selectedImageRun`, `_registryDocumentImages`, `_isRegistryImageExpanded`)만이다.
  </action>
  <verify>
    <automated>cd C:/gits/Dev-NPLogic &amp;&amp; rtk dotnet build NPLogic.sln</automated>
  </verify>
  <done>ToggleRegistryImagePanelCommand가 팝업 창을 열고, 인라인 뷰어 전용 속성(IsRegistryImageExpanded, SelectedImageRun, RegistryDocumentImages)이 제거되었으며, 빌드가 성공한다.</done>
</task>

</tasks>

<verification>
1. `dotnet build NPLogic.sln` 성공 (컴파일 에러 없음)
2. 앱 실행 후 담보물건 탭 > 등기부등본 정보 > "등기부등본" 버튼 클릭 시 팝업 창이 열림
3. 팝업 내 ComboBox에서 물건지 선택 시 해당 이미지들이 표시됨
4. 팝업 X 버튼으로 닫기 가능
5. 팝업을 열어둔 채 메인 창에서 다른 작업 가능 (비모달)
</verification>

<success_criteria>
- 등기부등본 버튼이 인라인 토글 대신 독립 팝업 창을 연다
- 팝업에서 물건지 선택 및 이미지 열람이 정상 동작한다
- CollateralPropertyView.xaml에 인라인 이미지 뷰어 코드가 없다
- 빌드 성공
</success_criteria>

<output>
완료 후 `.planning/quick/2-pdf/2-SUMMARY.md` 생성
</output>
