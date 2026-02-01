# NPLogic 소스 디렉토리

**Parent:** ../AGENTS.md
**Generated:** 2026-02-02

## 목적

NPLogic의 핵심 소스 코드를 담고 있는 디렉토리입니다. 4개의 주요 프로젝트로 구성된 계층화된 아키텍처를 제공합니다.

## 프로젝트 구조

| 프로젝트 | 유형 | 설명 | 주요 의존성 |
|---------|------|------|-------------|
| **NPLogic.App** | WPF 애플리케이션 | 메인 UI 레이어 (Views, ViewModels, Services) | Core, Data, UI |
| **NPLogic.Core** | 클래스 라이브러리 | 비즈니스 로직 및 도메인 모델 | 없음 (독립적) |
| **NPLogic.Data** | 클래스 라이브러리 | 데이터 접근 계층 (Repositories, Supabase) | Core |
| **NPLogic.UI** | 클래스 라이브러리 | 재사용 가능 UI 컴포넌트 | 없음 (독립적) |

## 의존성 흐름

```
NPLogic.App
    ├── NPLogic.Core (비즈니스 로직)
    ├── NPLogic.Data (데이터 접근)
    └── NPLogic.UI (UI 컴포넌트)

NPLogic.Data
    └── NPLogic.Core (도메인 모델)

NPLogic.Core (독립적)

NPLogic.UI (독립적)
```

## 아키텍처 원칙

### MVVM 패턴
- **Views**: XAML UI 파일 (NPLogic.App/Views)
- **ViewModels**: CommunityToolkit.Mvvm 사용 (NPLogic.App/ViewModels)
- **Models**: 도메인 모델 (NPLogic.Core/Models)

### 계층 분리
- **UI 레이어**: App, UI 프로젝트
- **비즈니스 레이어**: Core 프로젝트
- **데이터 레이어**: Data 프로젝트

### 의존성 주입
- Microsoft.Extensions.DependencyInjection 사용
- App.xaml.cs에서 모든 서비스 등록
- Singleton: Services, Repositories
- Transient: ViewModels, Views

## 핵심 기능 영역

| 기능 영역 | 주요 프로젝트 | 설명 |
|----------|--------------|------|
| **부동산 관리** | App, Core, Data | 물건 등록/조회/수정, 평가 |
| **부채정리** | App, Core, Data | 비핵심자산 관리 |
| **대출 관리** | App, Core, Data | 대출 계좌 관리, 이자 계산 |
| **경매/공매** | App, Core, Data | 경매 진행 상황 추적 |
| **권리 분석** | Core | 40+ 케이스 규칙 엔진 |
| **OCR 처리** | App, Python | 등기부등본 자동 인식 |
| **데이터디스크** | App, Data | 엑셀 대량 업로드 (6개 시트) |
| **UI 컴포넌트** | UI | MaterialDesign 기반 재사용 컴포넌트 |

## AI 에이전트 작업 지침

### 코드 수정 시 확인사항

1. **의존성 방향 준수**
   - Core, UI는 다른 프로젝트 참조 금지
   - Data는 Core만 참조 가능
   - App은 모든 프로젝트 참조 가능

2. **MVVM 패턴 준수**
   - View는 코드비하인드 최소화
   - ViewModel에서 비즈니스 로직 호출
   - Model은 순수 데이터 클래스

3. **의존성 주입 사용**
   - 생성자 주입 사용
   - App.xaml.cs에서 서비스 등록
   - 전역 접근: `App.ServiceProvider.GetRequiredService<T>()`

4. **네임스페이스 규칙**
   - NPLogic.App.*
   - NPLogic.Core.*
   - NPLogic.Data.*
   - NPLogic.UI.*

### 새 기능 추가 순서

1. **모델 정의** (NPLogic.Core/Models)
2. **Repository 생성** (NPLogic.Data/Repositories)
3. **Service 구현** (NPLogic.App/Services 또는 Core/Services)
4. **ViewModel 작성** (NPLogic.App/ViewModels)
5. **View 디자인** (NPLogic.App/Views)
6. **DI 등록** (App.xaml.cs)

### 테스트 요구사항

- **단위 테스트**: Core, Data 프로젝트의 비즈니스 로직
- **통합 테스트**: Repository와 Supabase 연동
- **UI 테스트**: 주요 화면 시나리오

## 공통 유틸리티

### 로깅
- Serilog 사용
- 파일 로그: `Logs/log-.txt`
- 로그 레벨: Debug, Information, Warning, Error

### Excel 처리
- ClosedXML: 읽기/쓰기
- EPPlus: 고급 기능

### 차트
- LiveChartsCore (SkiaSharp)

## 주요 데이터 흐름

### 물건 등록 흐름
```
PropertyDetailView
    → PropertyDetailViewModel
        → PropertyRepository (Save)
            → Supabase (properties 테이블)
```

### 권리 분석 흐름
```
RightAnalysisView
    → RightAnalysisViewModel
        → RightAnalysisRuleEngine (Core)
            → Registry + Property 데이터 분석
                → 40+ 케이스 매칭
```

### 데이터디스크 업로드 흐름
```
DataDiskUploadService
    → Excel 파싱 (6개 시트)
        → 대표컬럼 추출
            → Repository 일괄 저장
                → Supabase 42개 테이블
```

## 기술 스택

| 카테고리 | 기술 |
|---------|------|
| **UI 프레임워크** | WPF (.NET 10.0) |
| **MVVM** | CommunityToolkit.Mvvm |
| **디자인** | MaterialDesignThemes |
| **DI** | Microsoft.Extensions.DependencyInjection |
| **DB** | Supabase (PostgreSQL) |
| **ORM/Client** | supabase-csharp, postgrest-csharp |
| **Excel** | ClosedXML, EPPlus |
| **차트** | LiveChartsCore (SkiaSharp) |
| **로깅** | Serilog |

## 다음 단계

각 프로젝트의 세부 정보는 하위 AGENTS.md 파일을 참조하세요:
- [NPLogic.App/AGENTS.md](./NPLogic.App/AGENTS.md)
- [NPLogic.Core/AGENTS.md](./NPLogic.Core/AGENTS.md)
- [NPLogic.Data/AGENTS.md](./NPLogic.Data/AGENTS.md)
- [NPLogic.UI/AGENTS.md](./NPLogic.UI/AGENTS.md)
