# NPLogic.App - 메인 WPF 애플리케이션

**Parent:** ../AGENTS.md
**Generated:** 2026-02-02

## 목적

NPLogic의 사용자 인터페이스 및 애플리케이션 로직을 담당하는 메인 WPF 프로젝트입니다. MVVM 패턴을 사용하여 부동산 금융 업무를 위한 통합 데스크톱 환경을 제공합니다.

## 프로젝트 정보

- **타겟 프레임워크**: .NET 10.0
- **출력 타입**: WPF 애플리케이션
- **MVVM 프레임워크**: CommunityToolkit.Mvvm
- **UI 라이브러리**: MaterialDesignThemes
- **의존성**: NPLogic.Core, NPLogic.Data, NPLogic.UI

## 폴더 구조

| 폴더 | 파일 수 | 설명 |
|------|---------|------|
| **Views/** | 70+ | XAML 뷰 파일 (화면 UI) |
| **ViewModels/** | 35+ | ViewModel 클래스 (UI 로직) |
| **Services/** | 18+ | 애플리케이션 서비스 |
| **Converters/** | 12+ | XAML 값 변환기 |
| **Helpers/** | 8+ | 유틸리티 헬퍼 클래스 |
| **Resources/** | - | XAML 리소스 딕셔너리 |

## 주요 View 및 ViewModel

### 대시보드 및 네비게이션
| View | ViewModel | 기능 |
|------|-----------|------|
| `MainWindow.xaml` | `MainWindowViewModel` | 메인 윈도우, 네비게이션 |
| `DashboardView.xaml` | `DashboardViewModel` | 대시보드 (통계, 차트) |
| `LeftSidebarView.xaml` | - | 좌측 메뉴바 |

### 부동산 관리 (70+ 뷰 중 핵심)
| View | ViewModel | 기능 |
|------|-----------|------|
| `PropertyListView.xaml` | `PropertyListViewModel` | 물건 목록 조회 |
| `PropertyDetailView.xaml` | `PropertyDetailViewModel` | 물건 상세 정보 |
| `EvaluationTab.xaml` | `EvaluationTabViewModel` | 평가 (주택/상업/공장) |
| `RightAnalysisView.xaml` | `RightAnalysisViewModel` | 권리 분석 (40+ 케이스) |
| `AuctionPublicSaleView.xaml` | `AuctionPublicSaleViewModel` | 경매/공매 관리 |

### 차주 및 대출 관리
| View | ViewModel | 기능 |
|------|-----------|------|
| `BorrowerListView.xaml` | `BorrowerListViewModel` | 차주 목록 |
| `BorrowerDetailView.xaml` | `BorrowerDetailViewModel` | 차주 상세 |
| `LoanDetailView.xaml` | `LoanDetailViewModel` | 대출 상세 |
| `LoanSheetView.xaml` | `LoanSheetViewModel` | 대출 시트 |
| `NonCoreView.xaml` | `NonCoreViewModel` | 부채정리 (비핵심자산) |

### 데이터 관리
| View | ViewModel | 기능 |
|------|-----------|------|
| `DataDiskUploadView.xaml` | `DataDiskUploadViewModel` | 엑셀 데이터디스크 업로드 |
| `RegistryOcrView.xaml` | `RegistryOcrViewModel` | 등기부등본 OCR |
| `ExcelImportView.xaml` | `ExcelImportViewModel` | Excel 가져오기 |

### 설정 및 관리
| View | ViewModel | 기능 |
|------|-----------|------|
| `LoginView.xaml` | `LoginViewModel` | 로그인 |
| `UserManagementView.xaml` | `UserManagementViewModel` | 사용자 관리 |
| `PermissionManagementView.xaml` | `PermissionManagementViewModel` | 권한 관리 |
| `SettingsView.xaml` | `SettingsViewModel` | 설정 |

## 주요 서비스 (Services/)

### 데이터 처리 서비스
| 서비스 | 기능 | 비고 |
|--------|------|------|
| `DataDiskUploadService` | 데이터디스크 엑셀 업로드 (6개 시트) | 대표컬럼 매핑 |
| `ExcelService` | Excel 파일 읽기/쓰기 | ClosedXML 사용 |
| `RegistryOcrService` | 등기부등본 OCR 처리 | Python 백엔드 연동 |

### 외부 연동 서비스
| 서비스 | 기능 | 비고 |
|--------|------|------|
| `PythonBackendService` | Python OCR/추천 서버 통신 | Singleton.Instance |
| `MapService` | 지도 API 연동 | 네이버/카카오 지도 |
| `KbAptPriceService` | KB 아파트 시세 조회 | 외부 API |

### UI/UX 서비스
| 서비스 | 기능 | 비고 |
|--------|------|------|
| `NavigationService` | 화면 네비게이션 | MVVM 패턴 |
| `DialogService` | 모달 대화상자 | MaterialDesign |
| `NotificationService` | 알림/토스트 메시지 | NPToast 사용 |

### 비즈니스 서비스
| 서비스 | 기능 | 비고 |
|--------|------|------|
| `ReportService` | 보고서 생성 (PDF, Excel) | 각종 서식 출력 |
| `ChartService` | 차트 데이터 준비 | LiveChartsCore |
| `ValidationService` | 입력 검증 | FluentValidation |

## 의존성 주입 (App.xaml.cs)

### 서비스 등록
```csharp
// Singleton Services
services.AddSingleton<SupabaseService>();
services.AddSingleton<AuthService>();
services.AddSingleton<NavigationService>();
services.AddSingleton<ToastService>();
// ... 18개 서비스

// Singleton Repositories
services.AddSingleton<PropertyRepository>();
services.AddSingleton<BorrowerRepository>();
// ... 27개 리포지토리

// Transient ViewModels
services.AddTransient<MainWindowViewModel>();
services.AddTransient<PropertyListViewModel>();
// ... 35개 뷰모델

// Transient Views
services.AddTransient<MainWindow>();
services.AddTransient<PropertyListView>();
// ... 70개 뷰
```

### 전역 서비스 접근
```csharp
var service = App.ServiceProvider.GetRequiredService<T>();
```

## 데이터디스크 6개 시트 대표컬럼

### Sheet A: 차주일반정보 (BorrowerGeneral)
- **테이블**: `borrowers`
- **대표컬럼** (9개): 자산유형, 차주일련번호, 차주명, 관련차주, 차주형태, 미상환원금잔액, 미수이자, 근저당권설정액, 비고

### Sheet A-1/F: 회생차주정보 (BorrowerRestructuring)
- **테이블**: `borrower_restructurings`
- **대표컬럼** (14개): 세부 진행단계, 관할법원, 회생사건번호, 보전처분일, 개시결정일 등

### Sheet B/B-1: 채권일반정보 (Loan)
- **테이블**: `loans`
- **대표컬럼** (14개): 대출일련번호, 대출과목, 계좌번호, 정상이자율, 최초대출일, 미상환원금잔액 등

### Sheet C-1: 물건정보 (Property)
- **테이블**: `properties`
- **대표컬럼** (56개):
  - 기본: 물건 일련번호, 물건 종류
  - 주소: 담보소재지 1~4
  - 면적: 대지면적, 건물면적
  - 선순위: 설정액, 임차보증금, 조세채권 등
  - 감정평가: 평가액, KB시세
  - 경매: 개시일자, 사건번호, 낙찰금액 등

### Sheet C-2: 등기부등본정보 (RegistryDetail)
- **테이블**: `registry_sheet_data`
- **대표컬럼** (8개): 차주일련번호, 물건번호, 지번번호, 담보소재지1~4

### Sheet D: 신용보증서 (Guarantee)
- **테이블**: `credit_guarantees`
- **대표컬럼** (10개): 보증기관, 보증종류, 보증서번호, 보증비율, 환산후 보증잔액 등

## 주요 Converters (XAML 값 변환기)

| Converter | 기능 |
|-----------|------|
| `BoolToVisibilityConverter` | bool → Visibility 변환 |
| `NullToVisibilityConverter` | null → Visibility 변환 |
| `DateTimeConverter` | DateTime 포맷 변환 |
| `CurrencyConverter` | 통화 포맷 변환 (₩ 1,000,000) |
| `PercentConverter` | 퍼센트 포맷 변환 (12.34%) |
| `ColorToBrushConverter` | Color → Brush 변환 |
| `InverseBoolConverter` | bool 반전 |
| `EmptyStringConverter` | 빈 문자열 처리 |

## AI 에이전트 작업 지침

### View/ViewModel 작업 시

1. **MVVM 패턴 준수**
   - View는 코드비하인드 최소화 (생성자만)
   - ViewModel에 모든 UI 로직 작성
   - Model은 NPLogic.Core에서 참조

2. **데이터 바인딩**
   - OneWay: 읽기 전용 데이터
   - TwoWay: 입력 필드
   - ObservableCollection: 컬렉션
   - RelayCommand: 버튼 클릭

3. **CommunityToolkit.Mvvm 사용**
   ```csharp
   [ObservableProperty]
   private string _name;

   [RelayCommand]
   private async Task SaveAsync() { }
   ```

4. **MaterialDesign 컴포넌트 활용**
   - Card, Button, TextField
   - DataGrid, ComboBox
   - Dialog, Snackbar

### Service 작업 시

1. **생성자 주입**
   ```csharp
   public MyService(IRepository repo, ILogger logger)
   {
       _repo = repo;
       _logger = logger;
   }
   ```

2. **비동기 작업**
   - async/await 사용
   - CancellationToken 전달
   - 예외 처리 필수

3. **로깅**
   ```csharp
   _logger.LogInformation("작업 시작");
   _logger.LogError(ex, "오류 발생");
   ```

### 새 화면 추가 순서

1. **Model 정의** (NPLogic.Core/Models)
2. **Repository 생성** (NPLogic.Data/Repositories)
3. **Service 구현** (Services/ 또는 ViewModel에 직접)
4. **ViewModel 작성** (ViewModels/)
   - ObservableProperty, RelayCommand
   - 생성자 주입
5. **View 디자인** (Views/)
   - XAML 바인딩
   - MaterialDesign 컴포넌트
6. **DI 등록** (App.xaml.cs)
   - ViewModel: Transient
   - View: Transient
   - Service: Singleton (필요시)
7. **네비게이션 연결** (NavigationService)

### 테스트 시나리오

- **UI 테스트**: 주요 화면 시나리오 (로그인, 물건 등록, 대출 조회)
- **통합 테스트**: ViewModel + Repository + Supabase
- **수동 테스트**: 실제 사용자 플로우

## 성능 최적화

### 대량 데이터 처리
- **가상화**: VirtualizingStackPanel 사용 (DataGrid, ListBox)
- **페이징**: 1000건 이상 데이터는 페이징 처리
- **비동기 로딩**: await Task.Run으로 UI 블로킹 방지

### 메모리 관리
- **이벤트 구독 해제**: Dispose 패턴
- **WeakReference**: 순환 참조 방지
- **이미지 최적화**: BitmapImage 캐싱

## 에러 처리

### 전역 예외 처리
```csharp
App.xaml.cs:
- DispatcherUnhandledException
- AppDomain.UnhandledException
- TaskScheduler.UnobservedTaskException
```

### 사용자 친화적 메시지
```csharp
try { }
catch (Exception ex)
{
    _logger.LogError(ex, "작업 실패");
    await _dialogService.ShowErrorAsync("저장 실패", ex.Message);
}
```

## 주요 데이터 흐름 예시

### 물건 등록 흐름
```
PropertyDetailView (XAML)
    → PropertyDetailViewModel (바인딩)
        → PropertyRepository.SaveAsync()
            → SupabaseService (Supabase 연동)
                → properties 테이블 저장
```

### 데이터디스크 업로드 흐름
```
DataDiskUploadView
    → DataDiskUploadViewModel
        → DataDiskUploadService
            → ExcelService (6개 시트 파싱)
                → 대표컬럼 추출
                    → 각 Repository 일괄 저장
                        → Supabase 42개 테이블
```

### 권리 분석 흐름
```
RightAnalysisView
    → RightAnalysisViewModel
        → RightAnalysisRuleEngine (NPLogic.Core)
            → Registry + Property 데이터 분석
                → 40+ 케이스 규칙 매칭
                    → 분석 결과 반환
```

## 외부 의존성

| 패키지 | 용도 |
|--------|------|
| `MaterialDesignThemes` | UI 디자인 시스템 |
| `CommunityToolkit.Mvvm` | MVVM 헬퍼 |
| `LiveChartsCore.SkiaSharpView.WPF` | 차트 |
| `ClosedXML` | Excel 읽기/쓰기 |
| `EPPlus` | Excel 고급 기능 |
| `Serilog` | 로깅 |
| `Newtonsoft.Json` | JSON 처리 |
| `Microsoft.Extensions.DependencyInjection` | DI 컨테이너 |

## 참고 사항

### Python 백엔드 연동
- **PythonBackendService**: Singleton 인스턴스
- **OCR 엔드포인트**: `POST /ocr/process`
- **추천 엔드포인트**: `POST /recommend/similar`

### Supabase 프로젝트
- **Project ID**: `nlddampvgxamaukflqhd`
- **42개 테이블**: borrowers, properties, loans, evaluations 등

### 자동 로그인
- **AuthService**: 세션 저장 및 자동 복원 지원
- **SessionStorageService**: 암호화된 토큰 저장

## 다음 단계

- View/ViewModel 추가 시 위 가이드라인 준수
- MaterialDesign 컴포넌트 우선 사용
- 성능 최적화 (가상화, 페이징) 적용
- 에러 처리 및 로깅 필수
