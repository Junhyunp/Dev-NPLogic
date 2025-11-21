# NPLogic 프로젝트 구조

## 📁 전체 구조

```
NPLogic/
├── src/                        # 소스 코드
│   ├── NPLogic.App/           # WPF 애플리케이션
│   ├── NPLogic.Core/          # 비즈니스 로직, 모델, 인터페이스
│   ├── NPLogic.Data/          # 데이터 액세스, Supabase 연동
│   └── NPLogic.UI/            # 공통 UI 컴포넌트, 스타일, 컨버터
├── python/                     # Python OCR 스크립트
├── tests/                      # 테스트 프로젝트
├── docs/                       # 문서
└── NPLogic.sln                 # 솔루션 파일
```

## 🔹 NPLogic.App (메인 애플리케이션)

**역할**: WPF 애플리케이션의 진입점 및 UI 계층

**주요 내용**:
- `App.xaml` / `App.xaml.cs` - 애플리케이션 시작점
- `MainWindow.xaml` - 메인 윈도우
- `Views/` - 각 화면의 XAML 뷰
- `ViewModels/` - MVVM 패턴의 ViewModel
- `appsettings.json` - 설정 파일 (gitignore에 추가됨)

**NuGet 패키지**:
- CommunityToolkit.Mvvm (8.2.2)
- MaterialDesignThemes (4.9.0)
- Serilog (3.1.1)
- Serilog.Sinks.File (5.0.0)
- Microsoft.Extensions.DependencyInjection (8.0.0)
- Microsoft.Web.WebView2 (최신)

**프로젝트 참조**:
- NPLogic.Core
- NPLogic.Data
- NPLogic.UI

---

## 🔹 NPLogic.Core (비즈니스 로직)

**역할**: 비즈니스 로직, 도메인 모델, 인터페이스 정의

**주요 내용**:
- `Models/` - 도메인 모델 (Property, Owner, Registry 등)
- `Interfaces/` - 서비스 인터페이스
- `Services/` - 비즈니스 로직 서비스
  - `AuthService.cs` - 인증 관련 로직
  - `ExcelService.cs` - Excel 처리 (향후)
  - `OcrService.cs` - OCR 처리 (향후)
- `Enums/` - 열거형 타입
- `DTOs/` - 데이터 전송 객체

**NuGet 패키지**:
- EPPlus (7.0.5)

**프로젝트 참조**: 없음 (최상위 레이어)

---

## 🔹 NPLogic.Data (데이터 액세스)

**역할**: 데이터베이스 연동 및 데이터 액세스 계층

**주요 내용**:
- `Services/`
  - `SupabaseService.cs` - Supabase 클라이언트 초기화 및 연결
- `Repositories/` - 데이터 CRUD 작업 (향후)
  - `PropertyRepository.cs`
  - `UserRepository.cs`
  - `RegistryRepository.cs` 등
- `Migrations/` - 데이터베이스 마이그레이션 (향후)

**NuGet 패키지**:
- supabase-csharp (0.16.2)

**프로젝트 참조**:
- NPLogic.Core

---

## 🔹 NPLogic.UI (공통 UI 라이브러리)

**역할**: 재사용 가능한 UI 컴포넌트 및 스타일

**주요 내용**:
- `Controls/` - 커스텀 컨트롤
  - `NPCard.xaml` - 카드 컴포넌트
  - 기타 공통 컴포넌트 (향후)
- `Converters/` - XAML 값 변환기
  - `ValueConverters.cs`
- `Styles/` - XAML 리소스 딕셔너리
  - `Colors.xaml` - 색상 정의
  - `Typography.xaml` - 폰트 스타일
  - `Controls.xaml` - 컨트롤 기본 스타일
  - `Buttons.xaml` - 버튼 스타일
  - `DataGrid.xaml` - 그리드 스타일
- `Themes/` - 테마 관련

**NuGet 패키지**:
- MaterialDesignThemes (4.9.0)

**프로젝트 참조**: 없음 (독립적인 UI 라이브러리)

---

## 🐍 python/ (Python 스크립트)

**역할**: OCR 처리 및 PDF 분석

**주요 내용**:
- `ocr_processor.py` - OCR 메인 스크립트
- `requirements.txt` - Python 의존성
- `README.md` - Python 환경 설정 가이드

**의존성**:
- pytesseract
- Pillow
- pdf2image
- pandas
- PyPDF2

---

## 🧪 tests/ (테스트)

**역할**: 단위 테스트 및 통합 테스트 (향후)

**계획**:
- NPLogic.Core.Tests
- NPLogic.Data.Tests
- NPLogic.App.Tests

---

## 📚 docs/ (문서)

**주요 폴더**:
- `architecture/` - 아키텍처 문서
- `database/` - DB 스키마 및 설정
- `design/` - 디자인 시스템
- `screens/` - 화면별 가이드
- `setup/` - 설치 및 환경 설정
- `progress/` - 개발 진행 상황

---

## 🔗 프로젝트 의존성 다이어그램

```
NPLogic.App
    ├── NPLogic.Core
    ├── NPLogic.Data
    │       └── NPLogic.Core
    └── NPLogic.UI
```

**의존성 규칙**:
1. **App**: 모든 프로젝트를 참조 가능 (진입점)
2. **Core**: 다른 프로젝트를 참조하지 않음 (순수 비즈니스 로직)
3. **Data**: Core만 참조 (데이터 액세스 레이어)
4. **UI**: 독립적 (재사용 가능한 UI 라이브러리)

---

## 📦 빌드 순서

1. NPLogic.Core
2. NPLogic.UI
3. NPLogic.Data (Core 의존)
4. NPLogic.App (모든 프로젝트 의존)

---

## 🚀 실행 방법

### Visual Studio
1. `NPLogic.sln` 열기
2. NPLogic.App을 시작 프로젝트로 설정
3. F5 또는 Ctrl+F5로 실행

### 명령줄
```bash
dotnet build NPLogic.sln
dotnet run --project src/NPLogic.App/NPLogic.App.csproj
```

---

## 📝 주요 규칙

### 네이밍 컨벤션
- **네임스페이스**: `NPLogic.<ProjectName>.<Folder>`
- **클래스**: PascalCase
- **메서드**: PascalCase
- **변수**: camelCase
- **상수**: UPPER_SNAKE_CASE

### 파일 구조
- 각 클래스는 별도의 파일로 분리
- 파일명은 클래스명과 동일
- 인터페이스는 `I`로 시작 (예: `IPropertyRepository`)

### 코드 스타일
- C# 10+ 기능 활용
- `nullable` 활성화
- async/await 패턴 사용
- MVVM 패턴 준수

---

## 🔄 향후 계획

### 단기 (Phase 1-2)
- [ ] 공통 컴포넌트 확장
- [ ] Repository 패턴 구현
- [ ] 단위 테스트 추가

### 중기 (Phase 3-5)
- [ ] 캐싱 레이어 추가
- [ ] 이벤트 버스 구현
- [ ] 플러그인 시스템 (선택적)

### 장기
- [ ] 마이크로서비스 아키텍처 검토
- [ ] API 레이어 분리 (선택적)

---

**마지막 업데이트**: 2025-11-20


