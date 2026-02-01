# AGENTS.md

**Generated**: 2026-02-02

---

## 프로젝트 목적

NPLogic은 .NET 10.0 기반 WPF 데스크톱 애플리케이션으로, 부동산 금융 업무(부채정리, 대출, 부동산 평가, 경매/공매)를 관리하는 통합 금융 시스템입니다.

**핵심 기능:**
- 데이터디스크 업로드 및 파싱 (은행별 엑셀 템플릿 6개 시트)
- 등기부등본 OCR 자동 추출
- 권리분석 룰 엔진 (40+ 케이스 자동 분석)
- XNPV/XIRR 기반 순현재가 계산
- 경매/공매 시나리오 분석 및 회수전략 수립

**기술 스택:**
- Frontend: WPF + MaterialDesignThemes + LiveChartsCore
- Backend: Supabase (PostgreSQL + PostgREST API)
- Architecture: MVVM (CommunityToolkit.Mvvm)
- Auxiliary: Python 3.10+ (OCR, 유사물건 추천)

---

## 주요 파일

| 파일 | 목적 | 주요 내용 |
|------|------|----------|
| `NPLogic.sln` | 솔루션 파일 | 전체 프로젝트 구성 |
| `CLAUDE.md` | AI 작업 지침 | 프로젝트 개요, 빌드, 아키텍처 |
| `README.md` | 프로젝트 소개 | 기본 설명 및 사용법 |
| `changelog.md` | 변경 이력 | 버전별 주요 변경사항 |
| `MainWindow.xaml` | 메인 윈도우 레이아웃 | 상단 네비게이션, 탭 컨테이너 |
| `.mcp.json` | MCP 서버 설정 | Supabase 연동 구성 |

---

## 하위 디렉토리

| 디렉토리 | 목적 | 세부 문서 |
|----------|------|----------|
| `src/` | .NET 소스 코드 | [src/AGENTS.md](src/AGENTS.md) |
| `python/` | Python OCR/추천 서버 | [python/AGENTS.md](python/AGENTS.md) |
| `docs/` | 개발 문서 | [docs/AGENTS.md](docs/AGENTS.md) |
| `reference/` | 원청 참고자료 | [reference/AGENTS.md](reference/AGENTS.md) |
| `Controls/` | Legacy 공통 컨트롤 | (현재 미사용, 이전 버전 유물) |
| `Styles/` | Legacy 스타일 리소스 | (현재 미사용, App.xaml에 병합됨) |

---

## AI 에이전트 작업 지침

### 아키텍처 원칙

1. **의존성 방향**
   - `NPLogic.App` → `NPLogic.Core`, `NPLogic.Data`, `NPLogic.UI`
   - `NPLogic.Data` → `NPLogic.Core`
   - `NPLogic.Core`, `NPLogic.UI`는 독립적

2. **MVVM 패턴 준수**
   - Views: XAML + 코드비하인드 (최소한의 로직)
   - ViewModels: CommunityToolkit.Mvvm 사용 (`ObservableObject`, `RelayCommand`)
   - Models: 순수 데이터 클래스 (NPLogic.Core)

3. **의존성 주입**
   - `App.xaml.cs`에서 모든 서비스/리포지토리 등록
   - Services: Singleton
   - Repositories: Singleton
   - ViewModels/Views: Transient
   - 전역 접근: `App.ServiceProvider.GetRequiredService<T>()`

### 코드 작성 가이드

1. **Supabase 연동**
   - MCP Project ID: `nlddampvgxamaukflqhd`
   - 모든 DB 작업은 Repository 패턴 사용 (`src/NPLogic.Data/Repositories/`)
   - 인증: `AuthService` (자동 로그인, 세션 관리)

2. **데이터디스크 매핑**
   - 6개 시트별 대표컬럼 정의 준수 (CLAUDE.md 참조)
   - Sheet A: 차주일반정보 (9개 컬럼)
   - Sheet A-1/F: 회생차주정보 (14개 컬럼)
   - Sheet B/B-1: 채권일반정보 (14개 컬럼)
   - Sheet C-1: 물건정보 (56개 컬럼)
   - Sheet C-2: 등기부등본정보 (8개 컬럼)
   - Sheet D: 신용보증서 (10개 컬럼)

3. **권리분석 룰 엔진**
   - `RightAnalysisRuleEngine`: 40+ 케이스 자동 분류
   - 케이스별 설명, 위험도, 조치사항 자동 생성

4. **Python 백엔드 통신**
   - `PythonBackendService.Instance` (Singleton)
   - OCR: `POST /ocr` - 등기부등본 PDF → JSON
   - 추천: `POST /recommend` - 유사물건 분석

### 파일 수정 시 주의사항

- **ViewModel 수정**: 반드시 `INotifyPropertyChanged` 구현 (CommunityToolkit.Mvvm 사용)
- **Service 추가**: `App.xaml.cs`에 DI 등록 필수
- **DB 스키마 변경**: Supabase 마이그레이션 먼저 적용 후 모델 수정
- **Excel 파싱**: `ClosedXML` 사용, 헤더 행 자동 감지 로직 유지
- **차트**: LiveChartsCore 사용, SkiaSharp 렌더러

### 테스트 및 검증

- 빌드: `dotnet build`
- 실행: `dotnet run --project src/NPLogic.App`
- Python 서버: `cd python && python server.py` (포트 5000)

### 권장 작업 순서

1. 관련 AGENTS.md 읽기 (src/, docs/, python/ 등)
2. ERD 및 비즈니스 로직 문서 확인 (`docs/2. ERD.md`, `docs/5. BUSINESS_LOGIC.md`)
3. Repository 패턴으로 데이터 접근 계층 구현
4. ViewModel에 비즈니스 로직 연결
5. View (XAML) 바인딩 및 UI 구성
6. 빌드 및 실행 테스트

---

## 의존성 정보

### NuGet 패키지 (주요)

- **UI**: MaterialDesignThemes.Wpf, LiveChartsCore.SkiaSharpView.WPF
- **MVVM**: CommunityToolkit.Mvvm
- **DB**: supabase-csharp, postgrest-csharp
- **Excel**: ClosedXML, EPPlus
- **로깅**: Serilog, Serilog.Sinks.File
- **유틸리티**: Newtonsoft.Json, Microsoft.Extensions.DependencyInjection

### 외부 서비스

- **Supabase**: PostgreSQL 데이터베이스 + PostgREST API
- **Python Backend**: OCR (Tesseract) + 유사물건 추천 (sklearn)
- **카카오 지도 API**: 정적 지도 이미지 제공 (MapService)

---

## 관련 문서

- **개발 환경 설정**: [docs/1. ENVIRONMENT_SETUP.md](docs/1.%20ENVIRONMENT_SETUP.md)
- **ERD**: [docs/2. ERD.md](docs/2.%20ERD.md)
- **마이그레이션 가이드**: [docs/3. MIGRATION_GUIDE.md](docs/3.%20MIGRATION_GUIDE.md)
- **서비스 가이드**: [docs/4. SERVICE_GUIDE.md](docs/4.%20SERVICE_GUIDE.md)
- **비즈니스 로직**: [docs/5. BUSINESS_LOGIC.md](docs/5.%20BUSINESS_LOGIC.md)
- **배포 가이드**: [docs/6. DEPLOYMENT_GUIDE.md](docs/6.%20DEPLOYMENT_GUIDE.md)
