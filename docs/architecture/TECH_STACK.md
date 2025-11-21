# NPLogic 기술 아키텍처

## 기술 스택 개요

### 프론트엔드
- **Framework**: WPF (Windows Presentation Foundation)
- **Runtime**: .NET 8.0 (최신 LTS)
- **UI Pattern**: MVVM (Model-View-ViewModel)
- **UI Library**: MaterialDesignThemes + Custom Components

### 백엔드/클라우드
- **BaaS**: Supabase
  - Authentication (사용자 인증)
  - PostgreSQL Database
  - Storage (PDF 파일 저장)
  - Realtime (선택적)
  - **개발 도구**: Supabase MCP를 통한 직접 DB 제어 가능
    - 테이블 생성/수정
    - SQL 쿼리 실행
    - 마이그레이션 적용
    - 데이터 조회/수정
    - 로그 확인

### 외부 통합
- **OCR**: Python 스크립트 (별도 프로세스)
- **Excel 처리**: EPPlus
- **지도**: CefSharp (Chromium 임베디드 브라우저)

---

## MVVM 아키텍처 패턴

### 구조
```
View (XAML)
    ↕ DataBinding
ViewModel (로직 + 상태)
    ↕
Model (데이터)
    ↕
Service (Supabase 통신)
```

### 주요 컴포넌트

#### 1. Views (화면)
- `LoginView.xaml` - 로그인 화면
- `DashboardView.xaml` - 대시보드
- `PropertyListView.xaml` - 물건 목록
- `PropertyDetailView.xaml` - 물건 상세 (탭 구조)
- 각 화면은 Code-behind 최소화

#### 2. ViewModels
- `CommunityToolkit.Mvvm` 사용
- `ObservableObject` 상속
- `RelayCommand` 사용
- INotifyPropertyChanged 자동 구현

예시:
```csharp
public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string email;
    
    [RelayCommand]
    private async Task LoginAsync()
    {
        // 로그인 로직
    }
}
```

#### 3. Models
- 데이터 구조 정의
- Supabase 테이블과 매핑
- 비즈니스 로직 포함

#### 4. Services
- `ISupabaseService` - Supabase 통신
- `IOcrService` - Python OCR 실행
- `IExcelService` - Excel 입출력
- `IMapService` - 지도 기능

---

## Supabase 연동 구조

### 인증 흐름
```
1. 사용자 로그인 → Supabase Auth
2. JWT 토큰 발급
3. 토큰을 모든 API 요청에 포함
4. RLS(Row Level Security)로 권한 확인
```

### 데이터 접근
```csharp
// Supabase 클라이언트 초기화
var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
};
var supabase = new Supabase.Client(SUPABASE_URL, SUPABASE_KEY, options);

// 로그인
await supabase.Auth.SignIn(email, password);

// 데이터 조회
var response = await supabase
    .From<Property>()
    .Select("*")
    .Where(x => x.Status == "active")
    .Get();

// 데이터 삽입
var newProperty = new Property { Name = "물건1" };
await supabase.From<Property>().Insert(newProperty);
```

### Storage 사용
```csharp
// PDF 업로드
await supabase.Storage
    .From("registry-pdfs")
    .Upload(fileBytes, filePath);

// PDF 다운로드
var fileBytes = await supabase.Storage
    .From("registry-pdfs")
    .Download(filePath);
```

---

## Python OCR 통합 방식

### 선택된 방법: 별도 프로세스 실행

**장점**:
- Python 환경 독립적 관리
- C# 애플리케이션 안정성
- 쉬운 디버깅 및 유지보수

### 구현 방식

#### 1. Python 스크립트 구조
```
python/
├── ocr_processor.py      # 메인 OCR 스크립트
├── requirements.txt      # 의존성
└── utils/
    ├── pdf_handler.py
    └── text_extractor.py
```

#### 2. C#에서 Python 실행
```csharp
public class OcrService : IOcrService
{
    public async Task<OcrResult> ProcessPdfAsync(string pdfPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"python/ocr_processor.py \"{pdfPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"OCR failed: {error}");

        return JsonConvert.DeserializeObject<OcrResult>(output);
    }
}
```

#### 3. 대량 처리 최적화
- 배치 처리: 50-100개씩 묶어서 처리
- 병렬 처리: Task.WhenAll로 여러 Python 프로세스 동시 실행
- 프로그레스 추적: IProgress<T> 사용

```csharp
public async Task<List<OcrResult>> ProcessBatchAsync(
    List<string> pdfPaths, 
    IProgress<int> progress)
{
    var results = new List<OcrResult>();
    var batches = pdfPaths.Chunk(50); // 50개씩 배치
    
    int processed = 0;
    foreach (var batch in batches)
    {
        var tasks = batch.Select(path => ProcessPdfAsync(path));
        var batchResults = await Task.WhenAll(tasks);
        results.AddRange(batchResults);
        
        processed += batch.Count();
        progress?.Report(processed);
    }
    
    return results;
}
```

---

## 필수 NuGet 패키지

### UI/MVVM
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.*" />
<PackageReference Include="MaterialDesignThemes" Version="4.9.*" />
<PackageReference Include="MaterialDesignColors" Version="2.1.*" />
```

### Supabase
```xml
<PackageReference Include="supabase-csharp" Version="0.13.*" />
<PackageReference Include="Supabase.Gotrue" Version="4.0.*" />
<PackageReference Include="Supabase.Postgrest" Version="3.4.*" />
<PackageReference Include="Supabase.Storage" Version="1.5.*" />
```

### Excel 처리
```xml
<PackageReference Include="EPPlus" Version="7.0.*" />
<!-- EPPlus는 상용 라이선스 필요 (NonCommercial or Commercial) -->
```

### 지도 (HTML 렌더링)
```xml
<PackageReference Include="CefSharp.Wpf" Version="120.2.*" />
<!-- 또는 WebView2 -->
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.*" />
```

### JSON 처리
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.*" />
```

### 유틸리티
```xml
<PackageReference Include="Serilog" Version="3.1.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.*" />
```

---

## 프로젝트 구조

```
NPLogic/
├── src/
│   ├── NPLogic.App/              # WPF 메인 애플리케이션
│   │   ├── Views/                # XAML 뷰
│   │   ├── ViewModels/           # 뷰모델
│   │   ├── Styles/               # XAML 리소스
│   │   ├── App.xaml
│   │   └── MainWindow.xaml
│   │
│   ├── NPLogic.Core/             # 비즈니스 로직
│   │   ├── Models/               # 데이터 모델
│   │   ├── Services/             # 서비스 인터페이스
│   │   └── Helpers/              # 유틸리티
│   │
│   ├── NPLogic.Data/             # 데이터 액세스
│   │   ├── Supabase/             # Supabase 연동
│   │   ├── Services/             # 서비스 구현
│   │   └── Repositories/         # Repository 패턴
│   │
│   └── NPLogic.UI/               # 공통 UI 컴포넌트
│       ├── Controls/             # 커스텀 컨트롤
│       ├── Converters/           # Value Converter
│       └── Behaviors/            # Attached Behaviors
│
├── python/                        # Python OCR 스크립트
│   ├── ocr_processor.py
│   ├── requirements.txt
│   └── utils/
│
├── docs/                          # 문서
│   ├── architecture/
│   ├── design/
│   ├── database/
│   └── screens/
│
└── tests/                         # 테스트
    ├── NPLogic.Tests/
    └── NPLogic.IntegrationTests/
```

---

## 의존성 주입 (DI)

### Microsoft.Extensions.DependencyInjection 사용

```csharp
// App.xaml.cs
public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISupabaseService, SupabaseService>();
        services.AddSingleton<IOcrService, OcrService>();
        services.AddSingleton<IExcelService, ExcelService>();
        
        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PropertyListViewModel>();
        
        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginView>();
        services.AddTransient<DashboardView>();
    }
}
```

---

## 에러 처리 및 로깅

### Serilog 설정
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("logs/nplogic-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Application started");
```

### 전역 예외 처리
```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
    {
        Log.Fatal(ex.ExceptionObject as Exception, "Unhandled exception");
    };
    
    DispatcherUnhandledException += (s, ex) =>
    {
        Log.Error(ex.Exception, "Dispatcher unhandled exception");
        ex.Handled = true;
        MessageBox.Show("오류가 발생했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
    };
}
```

---

## 성능 최적화

### 대량 데이터 처리
1. **가상화**: DataGrid에 VirtualizingStackPanel 사용
2. **페이지네이션**: 한 번에 50-100개씩 로드
3. **비동기**: 모든 I/O 작업 async/await
4. **병렬 처리**: Task.WhenAll, Parallel.ForEach

### UI 반응성
1. **BackgroundWorker**: 긴 작업은 백그라운드 스레드
2. **Progress**: IProgress<T>로 진행 상황 표시
3. **CancellationToken**: 작업 취소 지원

---

## 보안

### Supabase 설정
1. **환경 변수**: API 키는 환경 변수로 관리
2. **RLS**: Row Level Security 정책 적용
3. **JWT**: 자동 토큰 갱신

### 로컬 저장
1. **비밀번호**: 저장하지 않음
2. **토큰**: Windows Credential Manager 사용 (선택적)

---

## 배포

### ClickOnce (권장)
- Visual Studio에서 쉬운 설정
- 자동 업데이트 지원
- 사용자별 설치

### 자체 설치 프로그램
- Inno Setup 사용
- 전체 제어 가능
- Python 런타임 포함

---

## 다음 단계

이 아키텍처 문서를 기반으로:
1. UI/UX 디자인 시스템 구축
2. 데이터베이스 스키마 설계
3. 공통 컴포넌트 개발
4. 화면별 구현 시작

