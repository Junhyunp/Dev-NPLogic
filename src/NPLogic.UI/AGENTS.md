# NPLogic.UI - 재사용 가능 UI 컴포넌트

**Parent:** ../AGENTS.md
**Generated:** 2026-02-02

## 목적

NPLogic의 재사용 가능한 UI 컴포넌트 라이브러리입니다. MaterialDesign 기반의 커스텀 컨트롤과 스타일을 제공하여 일관된 사용자 경험을 구현합니다.

## 프로젝트 정보

- **타겟 프레임워크**: .NET 10.0
- **출력 타입**: WPF 클래스 라이브러리
- **디자인 시스템**: MaterialDesignInXamlToolkit 기반
- **의존성**: 없음 (완전히 독립적)

## 폴더 구조

| 폴더 | 파일 수 | 설명 |
|------|---------|------|
| **Controls/** | 9+ | 커스텀 컨트롤 |
| **Styles/** | 5+ | XAML 스타일 리소스 |
| **Themes/** | 3+ | 테마 정의 |
| **Converters/** | 8+ | 값 변환기 |
| **Behaviors/** | 6+ | 첨부 동작 (Attached Behaviors) |
| **Services/** | 1+ | UI 관련 서비스 |

## 주요 커스텀 컨트롤 (Controls/)

### 기본 컨트롤
| 컨트롤 | 설명 | 주요 속성 |
|--------|------|-----------|
| `NPButton` | 커스텀 버튼 | Text, Icon, Style (Primary/Secondary/Danger) |
| `NPCard` | 카드 컨테이너 | Title, Content, Header, Footer |
| `NPTextBox` | 향상된 텍스트박스 | Placeholder, Validation, Icon |
| `NPComboBox` | 향상된 콤보박스 | Placeholder, SearchEnabled |
| `NPDatePicker` | 날짜 선택기 | Format, MinDate, MaxDate |

### 데이터 표시 컨트롤
| 컨트롤 | 설명 | 주요 속성 |
|--------|------|-----------|
| `NPDataGrid` | 향상된 데이터그리드 | Columns, Pagination, Sorting, Filtering |
| `NPListView` | 향상된 리스트뷰 | ItemTemplate, GroupBy, VirtualizationEnabled |
| `NPTreeView` | 향상된 트리뷰 | HierarchicalTemplate, ExpandAll/CollapseAll |

### 피드백 컨트롤
| 컨트롤 | 설명 | 주요 속성 |
|--------|------|-----------|
| `NPToast` | 토스트 알림 | Message, Type (Info/Success/Warning/Error), Duration |
| `NPModal` | 모달 대화상자 | Title, Content, Buttons, Size |
| `NPProgressBar` | 진행률 표시 | Value, IsIndeterminate, ShowPercentage |
| `NPSpinner` | 로딩 스피너 | Size, Color, IsActive |

### 레이아웃 컨트롤
| 컨트롤 | 설명 | 주요 속성 |
|--------|------|-----------|
| `NPPanel` | 패널 컨테이너 | Orientation, Spacing, Padding |
| `NPExpander` | 확장/축소 패널 | Header, Content, IsExpanded |
| `NPTabControl` | 탭 컨트롤 | TabItems, SelectedIndex, TabPosition |

## NPButton 상세 구현

### XAML 정의
```xaml
<!-- Controls/NPButton.xaml -->
<UserControl x:Class="NPLogic.UI.Controls.NPButton"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <Button x:Name="ButtonControl"
            Style="{StaticResource NPButtonStyle}"
            Command="{Binding Command, RelativeSource={RelativeSource AncestorType=UserControl}}"
            CommandParameter="{Binding CommandParameter, RelativeSource={RelativeSource AncestorType=UserControl}}">
        <StackPanel Orientation="Horizontal">
            <md:PackIcon x:Name="IconElement"
                         Kind="{Binding Icon, RelativeSource={RelativeSource AncestorType=UserControl}}"
                         Width="18" Height="18"
                         Margin="0,0,8,0"
                         Visibility="{Binding Icon, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource NullToVisibilityConverter}}" />
            <TextBlock x:Name="TextElement"
                       Text="{Binding Text, RelativeSource={RelativeSource AncestorType=UserControl}}"
                       VerticalAlignment="Center" />
        </StackPanel>
    </Button>
</UserControl>
```

### Code-Behind
```csharp
// Controls/NPButton.xaml.cs
public partial class NPButton : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(NPButton));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(PackIconKind), typeof(NPButton));

    public static readonly DependencyProperty StyleTypeProperty =
        DependencyProperty.Register(nameof(StyleType), typeof(ButtonStyleType), typeof(NPButton),
            new PropertyMetadata(ButtonStyleType.Primary, OnStyleTypeChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(NPButton));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(NPButton));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public PackIconKind? Icon
    {
        get => (PackIconKind?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ButtonStyleType StyleType
    {
        get => (ButtonStyleType)GetValue(StyleTypeProperty);
        set => SetValue(StyleTypeProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public NPButton()
    {
        InitializeComponent();
    }

    private static void OnStyleTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NPButton button)
        {
            button.ApplyStyle();
        }
    }

    private void ApplyStyle()
    {
        var styleName = StyleType switch
        {
            ButtonStyleType.Primary => "NPButtonPrimaryStyle",
            ButtonStyleType.Secondary => "NPButtonSecondaryStyle",
            ButtonStyleType.Danger => "NPButtonDangerStyle",
            ButtonStyleType.Success => "NPButtonSuccessStyle",
            _ => "NPButtonStyle"
        };

        if (Resources[styleName] is Style style)
        {
            ButtonControl.Style = style;
        }
    }
}

public enum ButtonStyleType
{
    Primary,
    Secondary,
    Danger,
    Success,
    Link
}
```

### 사용 예시
```xaml
<!-- NPLogic.App에서 사용 -->
<ui:NPButton Text="저장"
             Icon="ContentSave"
             StyleType="Primary"
             Command="{Binding SaveCommand}" />

<ui:NPButton Text="삭제"
             Icon="Delete"
             StyleType="Danger"
             Command="{Binding DeleteCommand}" />
```

## NPDataGrid 상세 구현

### 주요 기능
- **페이징**: 대량 데이터 처리
- **정렬**: 컬럼별 오름차순/내림차순
- **필터링**: 컬럼별 검색 필터
- **선택**: 단일/다중 선택
- **가상화**: 성능 최적화

### 커스텀 속성
```csharp
public class NPDataGrid : DataGrid
{
    // 페이징
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    // 이벤트
    public event EventHandler<PageChangedEventArgs> PageChanged;
    public event EventHandler<SortChangedEventArgs> SortChanged;
    public event EventHandler<FilterChangedEventArgs> FilterChanged;

    // 메서드
    public void GoToPage(int page) { }
    public void NextPage() { }
    public void PreviousPage() { }
    public void ApplyFilter(string columnName, string filterText) { }
    public void ClearFilters() { }
}
```

## NPToast 상세 구현

### 토스트 서비스
```csharp
// Services/ToastService.cs
public class ToastService
{
    private static ToastService? _instance;
    public static ToastService Instance => _instance ??= new ToastService();

    public event EventHandler<ToastEventArgs>? ToastRequested;

    public void Show(string message, ToastType type = ToastType.Info, int duration = 3000)
    {
        ToastRequested?.Invoke(this, new ToastEventArgs
        {
            Message = message,
            Type = type,
            Duration = duration
        });
    }

    public void Success(string message) => Show(message, ToastType.Success);
    public void Error(string message) => Show(message, ToastType.Error);
    public void Warning(string message) => Show(message, ToastType.Warning);
    public void Info(string message) => Show(message, ToastType.Info);
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

public class ToastEventArgs : EventArgs
{
    public string Message { get; set; }
    public ToastType Type { get; set; }
    public int Duration { get; set; }
}
```

### 토스트 컨트롤
```csharp
// Controls/NPToast.xaml.cs
public partial class NPToast : UserControl
{
    private DispatcherTimer? _timer;

    public NPToast()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;

        ToastService.Instance.ToastRequested += OnToastRequested;
    }

    private void OnToastRequested(object? sender, ToastEventArgs e)
    {
        Message = e.Message;
        Type = e.Type;
        Duration = e.Duration;

        Show();
    }

    private void Show()
    {
        Visibility = Visibility.Visible;

        // 애니메이션
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        BeginAnimation(OpacityProperty, fadeIn);

        // 자동 숨김 타이머
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Duration) };
        _timer.Tick += (s, e) =>
        {
            Hide();
            _timer.Stop();
        };
        _timer.Start();
    }

    private void Hide()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        fadeOut.Completed += (s, e) => Visibility = Visibility.Collapsed;
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
```

### 사용 예시
```csharp
// NPLogic.App에서 사용
ToastService.Instance.Success("저장되었습니다.");
ToastService.Instance.Error("오류가 발생했습니다.");
ToastService.Instance.Warning("경고: 데이터가 변경되었습니다.");
ToastService.Instance.Info("정보: 처리 중입니다.");
```

## 스타일 정의 (Styles/)

### Colors.xaml
```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Primary Colors -->
    <Color x:Key="PrimaryColor">#1976D2</Color>
    <Color x:Key="PrimaryLightColor">#63A4FF</Color>
    <Color x:Key="PrimaryDarkColor">#004BA0</Color>

    <!-- Secondary Colors -->
    <Color x:Key="SecondaryColor">#424242</Color>
    <Color x:Key="SecondaryLightColor">#6D6D6D</Color>
    <Color x:Key="SecondaryDarkColor">#1B1B1B</Color>

    <!-- Accent Colors -->
    <Color x:Key="AccentColor">#FF4081</Color>
    <Color x:Key="SuccessColor">#4CAF50</Color>
    <Color x:Key="WarningColor">#FF9800</Color>
    <Color x:Key="DangerColor">#F44336</Color>
    <Color x:Key="InfoColor">#2196F3</Color>

    <!-- Neutral Colors -->
    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <Color x:Key="TextPrimaryColor">#212121</Color>
    <Color x:Key="TextSecondaryColor">#757575</Color>
    <Color x:Key="BorderColor">#E0E0E0</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}" />
    <SolidColorBrush x:Key="SecondaryBrush" Color="{StaticResource SecondaryColor}" />
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}" />
    <SolidColorBrush x:Key="WarningBrush" Color="{StaticResource WarningColor}" />
    <SolidColorBrush x:Key="DangerBrush" Color="{StaticResource DangerColor}" />
    <SolidColorBrush x:Key="InfoBrush" Color="{StaticResource InfoColor}" />
</ResourceDictionary>
```

### Typography.xaml
```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Font Family -->
    <FontFamily x:Key="PrimaryFont">Noto Sans KR</FontFamily>
    <FontFamily x:Key="MonoFont">Consolas</FontFamily>

    <!-- Font Sizes -->
    <system:Double x:Key="FontSizeH1">32</system:Double>
    <system:Double x:Key="FontSizeH2">24</system:Double>
    <system:Double x:Key="FontSizeH3">20</system:Double>
    <system:Double x:Key="FontSizeH4">16</system:Double>
    <system:Double x:Key="FontSizeBody">14</system:Double>
    <system:Double x:Key="FontSizeCaption">12</system:Double>

    <!-- Text Styles -->
    <Style x:Key="H1TextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{StaticResource FontSizeH1}" />
        <Setter Property="FontWeight" Value="Bold" />
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
    </Style>

    <Style x:Key="BodyTextStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{StaticResource FontSizeBody}" />
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
    </Style>
</ResourceDictionary>
```

### Buttons.xaml
```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Base Button Style -->
    <Style x:Key="NPButtonStyle" TargetType="Button">
        <Setter Property="FontFamily" Value="{StaticResource PrimaryFont}" />
        <Setter Property="FontSize" Value="{StaticResource FontSizeBody}" />
        <Setter Property="Padding" Value="16,8" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center"
                                          VerticalAlignment="Center" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Opacity" Value="0.8" />
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
                <Setter Property="Opacity" Value="0.5" />
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Primary Button -->
    <Style x:Key="NPButtonPrimaryStyle" TargetType="Button" BasedOn="{StaticResource NPButtonStyle}">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
        <Setter Property="Foreground" Value="White" />
    </Style>

    <!-- Secondary Button -->
    <Style x:Key="NPButtonSecondaryStyle" TargetType="Button" BasedOn="{StaticResource NPButtonStyle}">
        <Setter Property="Background" Value="{StaticResource SecondaryBrush}" />
        <Setter Property="Foreground" Value="White" />
    </Style>

    <!-- Danger Button -->
    <Style x:Key="NPButtonDangerStyle" TargetType="Button" BasedOn="{StaticResource NPButtonStyle}">
        <Setter Property="Background" Value="{StaticResource DangerBrush}" />
        <Setter Property="Foreground" Value="White" />
    </Style>
</ResourceDictionary>
```

## Converters (값 변환기)

### BoolToVisibilityConverter
```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            bool invert = parameter?.ToString() == "Invert";
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool invert = parameter?.ToString() == "Invert";
            return (visibility == Visibility.Visible) ^ invert;
        }
        return false;
    }
}
```

### CurrencyConverter
```csharp
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("N0") + "원";
        }
        if (value is int intValue)
        {
            return intValue.ToString("N0") + "원";
        }
        return "0원";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            var cleanValue = stringValue.Replace("원", "").Replace(",", "");
            if (decimal.TryParse(cleanValue, out decimal result))
            {
                return result;
            }
        }
        return 0m;
    }
}
```

### DateTimeConverter
```csharp
public class DateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            string format = parameter?.ToString() ?? "yyyy-MM-dd";
            return dateTime.ToString(format);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue && DateTime.TryParse(stringValue, out DateTime result))
        {
            return result;
        }
        return DateTime.MinValue;
    }
}
```

## Behaviors (첨부 동작)

### WatermarkBehavior
```csharp
public class WatermarkBehavior
{
    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.RegisterAttached("Watermark", typeof(string), typeof(WatermarkBehavior),
            new PropertyMetadata(string.Empty, OnWatermarkChanged));

    public static string GetWatermark(DependencyObject obj)
        => (string)obj.GetValue(WatermarkProperty);

    public static void SetWatermark(DependencyObject obj, string value)
        => obj.SetValue(WatermarkProperty, value);

    private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            // Watermark 표시 로직
        }
    }
}
```

### NumericOnlyBehavior
```csharp
public class NumericOnlyBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(NumericOnlyBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
        => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value)
        => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox && e.NewValue is bool isEnabled)
        {
            if (isEnabled)
            {
                textBox.PreviewTextInput += OnPreviewTextInput;
                DataObject.AddPastingHandler(textBox, OnPasting);
            }
            else
            {
                textBox.PreviewTextInput -= OnPreviewTextInput;
                DataObject.RemovePastingHandler(textBox, OnPasting);
            }
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsNumeric(e.Text);
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            string text = (string)e.DataObject.GetData(typeof(string));
            if (!IsNumeric(text))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private static bool IsNumeric(string text)
        => text.All(c => char.IsDigit(c) || c == '.' || c == ',');
}
```

## AI 에이전트 작업 지침

### 새 컨트롤 추가 순서

1. **UserControl 생성** (Controls/)
   ```xaml
   <!-- Controls/NPMyControl.xaml -->
   <UserControl x:Class="NPLogic.UI.Controls.NPMyControl">
       <!-- UI 정의 -->
   </UserControl>
   ```

2. **DependencyProperty 정의**
   ```csharp
   public partial class NPMyControl : UserControl
   {
       public static readonly DependencyProperty MyPropertyProperty =
           DependencyProperty.Register(nameof(MyProperty), typeof(string), typeof(NPMyControl));

       public string MyProperty
       {
           get => (string)GetValue(MyPropertyProperty);
           set => SetValue(MyPropertyProperty, value);
       }
   }
   ```

3. **스타일 추가** (Styles/)
   ```xaml
   <Style x:Key="NPMyControlStyle" TargetType="local:NPMyControl">
       <!-- 스타일 정의 -->
   </Style>
   ```

4. **사용 예시 문서화**
   ```xaml
   <!-- 사용 예시 -->
   <ui:NPMyControl MyProperty="Value" />
   ```

### 스타일 작성 원칙

1. **리소스 딕셔너리 분리**
   - Colors, Typography, Buttons, DataGrids 등 카테고리별 분리
   - Generic.xaml에서 MergedDictionaries로 통합

2. **네이밍 규칙**
   - 컨트롤 스타일: `NPButtonStyle`, `NPCardStyle`
   - 색상: `PrimaryColor`, `SuccessColor`
   - 브러시: `PrimaryBrush`, `SuccessBrush`

3. **BasedOn 상속 활용**
   ```xaml
   <Style x:Key="NPButtonPrimaryStyle" TargetType="Button" BasedOn="{StaticResource NPButtonStyle}">
       <!-- 추가 속성만 오버라이드 -->
   </Style>
   ```

### Converter 작성 원칙

1. **IValueConverter 구현**
   ```csharp
   public class MyConverter : IValueConverter
   {
       public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
       {
           // value를 UI 표시용으로 변환
       }

       public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
       {
           // UI 값을 모델로 역변환
       }
   }
   ```

2. **Null 처리**
   - 항상 null 값 처리
   - 기본값 반환

3. **재사용성**
   - 일반적인 변환 로직만 구현
   - 도메인 로직 포함 금지

### Behavior 작성 원칙

1. **Attached Property 사용**
   ```csharp
   public static readonly DependencyProperty MyBehaviorProperty =
       DependencyProperty.RegisterAttached("MyBehavior", typeof(bool), typeof(MyBehavior),
           new PropertyMetadata(false, OnMyBehaviorChanged));
   ```

2. **이벤트 핸들러 정리**
   - Attach 시 구독
   - Detach 시 구독 해제
   - 메모리 누수 방지

3. **재사용 가능한 동작**
   - 여러 컨트롤에서 사용 가능하도록 설계

### 테스트 요구사항

#### Visual 테스트
- 각 컨트롤의 시각적 외형 확인
- 다양한 테마 적용 테스트
- 반응형 레이아웃 테스트

#### 기능 테스트
```csharp
[WpfFact]
public void NPButton_Click_ShouldExecuteCommand()
{
    // Arrange
    var button = new NPButton();
    bool commandExecuted = false;
    button.Command = new RelayCommand(() => commandExecuted = true);

    // Act
    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    // Assert
    Assert.True(commandExecuted);
}
```

## 성능 최적화

### 가상화
```xaml
<!-- DataGrid 가상화 -->
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          VirtualizingPanel.ScrollUnit="Item" />
```

### 리소스 공유
```xaml
<!-- 자주 사용하는 브러시는 리소스로 -->
<SolidColorBrush x:Key="SharedBrush" Color="#1976D2" />
```

### Freezable 사용
```csharp
var brush = new SolidColorBrush(Colors.Blue);
brush.Freeze(); // 성능 향상
```

## 접근성 (Accessibility)

### 키보드 네비게이션
```xaml
<Button TabIndex="1" IsTabStop="True" />
```

### 스크린 리더 지원
```xaml
<Button AutomationProperties.Name="저장"
        AutomationProperties.HelpText="현재 데이터를 저장합니다" />
```

## 외부 의존성

| 패키지 | 용도 |
|--------|------|
| `MaterialDesignThemes` | MaterialDesign 아이콘 및 스타일 |
| `MaterialDesignColors` | 색상 팔레트 |

## 참고 사항

### 독립성 유지
- 이 프로젝트는 도메인 로직 참조 금지
- 순수 UI 컴포넌트만 포함
- NPLogic.Core, NPLogic.Data 참조 불가

### MaterialDesign 가이드라인 준수
- Material Design 원칙 따르기
- 일관된 간격, 색상, 타이포그래피 사용

### 테마 지원
- Light/Dark 테마 전환 가능하도록 설계
- 동적 색상 변경 지원

## 다음 단계

- 새 컨트롤 추가 시 위 가이드라인 준수
- 재사용 가능하도록 설계
- 접근성 고려
- Visual 테스트 작성
