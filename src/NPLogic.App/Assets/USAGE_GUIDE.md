# 로고 이미지 사용 가이드

## 📁 파일 위치
**이미지 파일을 여기에 넣으세요**: `src/NPLogic.App/Assets/`

예시:
- `src/NPLogic.App/Assets/logo.png`
- `src/NPLogic.App/Assets/icon.ico`

---

## 🖼️ MainWindow에서 로고 사용하기

### 현재 코드 (아이콘)
```xml
<materialDesign:PackIcon Kind="FileDocument" 
                       Width="32" Height="32"
                       Foreground="White"
                       Margin="0,0,12,0"/>
```

### 이미지로 교체
```xml
<!-- 방법 1: Image 컨트롤 사용 -->
<Image Source="/Assets/logo.png" 
       Width="32" Height="32"
       Margin="0,0,12,0"/>

<!-- 방법 2: 둥근 이미지 (CircleImage) -->
<Ellipse Width="32" Height="32" Margin="0,0,12,0">
    <Ellipse.Fill>
        <ImageBrush ImageSource="/Assets/logo.png" Stretch="UniformToFill"/>
    </Ellipse.Fill>
</Ellipse>

<!-- 방법 3: Border로 감싸서 테두리 추가 -->
<Border Width="32" Height="32" 
        CornerRadius="4" 
        Margin="0,0,12,0"
        BorderBrush="White" 
        BorderThickness="1">
    <Image Source="/Assets/logo.png" Stretch="UniformToFill"/>
</Border>
```

---

## 🎯 사용 예시

### 1. 헤더 로고 (현재 위치)
파일: `MainWindow.xaml` (약 37번째 줄)

```xml
<!-- Logo & Title -->
<StackPanel Grid.Column="0" 
           Orientation="Horizontal" 
           VerticalAlignment="Center">
    <!-- 아이콘을 이미지로 교체 -->
    <Image Source="/Assets/logo.png" 
           Width="32" Height="32"
           Margin="0,0,12,0"/>
    <TextBlock Text="NPLogic" 
              Style="{StaticResource H3TextStyle}"
              Foreground="White"
              VerticalAlignment="Center"/>
</StackPanel>
```

### 2. 로그인 화면 로고
파일: `Views/LoginWindow.xaml` (약 130번째 줄)

```xml
<!-- Logo -->
<Border Background="#1A237E"
        CornerRadius="60"
        Width="120"
        Height="120"
        Margin="0,0,0,20">
    <!-- 기존 아이콘을 이미지로 교체 -->
    <Image Source="/Assets/logo.png" 
           Width="80" Height="80"/>
</Border>
```

### 3. 윈도우 아이콘 (타이틀바 왼쪽 작은 아이콘)
파일: `MainWindow.xaml` (1번째 줄)

```xml
<Window x:Class="NPLogic.MainWindow"
        ...
        Icon="/Assets/icon.ico"
        Title="NPLogic - 등기부등본 자동화 시스템">
```

---

## 💡 추천 이미지 사양

### 로고 이미지
- **형식**: PNG (투명 배경)
- **크기**: 512x512px (큰 크기로 만들어두면 자동으로 축소됨)
- **배경**: 투명
- **여백**: 이미지 주변에 약간의 여백 권장

### 아이콘 파일
- **형식**: ICO
- **크기**: 16x16, 32x32, 48x48 (다중 해상도 포함)

---

## 🔧 파일 추가 후 할 일

1. ✅ `src/NPLogic.App/Assets/` 폴더에 이미지 복사
2. ✅ 프로젝트가 자동으로 인식 (`.csproj`에 이미 설정됨)
3. ✅ XAML 파일에서 경로 지정: `/Assets/파일명.png`
4. ✅ 앱 재빌드 및 실행

---

## 📝 참고사항

### 경로 작성 방법
```xml
<!-- ✅ 올바른 경로 (루트부터 시작) -->
<Image Source="/Assets/logo.png"/>

<!-- ❌ 잘못된 경로 -->
<Image Source="Assets/logo.png"/>
<Image Source="./Assets/logo.png"/>
```

### 이미지가 안 보일 때
1. 파일이 `Assets` 폴더에 있는지 확인
2. 파일 이름과 확장자가 정확한지 확인
3. 프로젝트 다시 빌드 (`Ctrl+Shift+B`)
4. 경로가 `/Assets/파일명.png` 형식인지 확인

---

## 🎨 추가 스타일링

### 그림자 효과
```xml
<Image Source="/Assets/logo.png" Width="32" Height="32">
    <Image.Effect>
        <DropShadowEffect Color="Black" 
                         BlurRadius="10" 
                         ShadowDepth="3" 
                         Opacity="0.3"/>
    </Image.Effect>
</Image>
```

### 호버 효과
```xml
<Image Source="/Assets/logo.png" Width="32" Height="32">
    <Image.Style>
        <Style TargetType="Image">
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Opacity" Value="0.8"/>
                </Trigger>
            </Style.Triggers>
        </Style>
    </Image.Style>
</Image>
```

