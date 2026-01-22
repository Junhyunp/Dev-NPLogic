# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

NPLogic은 .NET 10.0 기반 WPF 데스크톱 애플리케이션입니다. 부동산 금융 업무(부채정리, 대출, 부동산 평가, 경매)를 관리하는 통합 금융 시스템입니다.

## 빌드 및 실행

```bash
# 빌드
dotnet build

# 실행
dotnet run --project src/NPLogic.App

# 특정 프로젝트만 빌드
dotnet build src/NPLogic.Core
dotnet build src/NPLogic.Data
dotnet build src/NPLogic.App
```

## Python 백엔드 (OCR/추천)

```bash
cd python
pip install -r requirements.txt
python ocr_processor.py <pdf_file_path>
```

## 아키텍처

### 프로젝트 구조

```
src/
├── NPLogic.App     # 메인 WPF 애플리케이션 (Views, ViewModels, Services)
├── NPLogic.Core    # 핵심 비즈니스 로직 및 도메인 모델
├── NPLogic.Data    # 데이터 접근 계층 (Repositories, Supabase 연동)
└── NPLogic.UI      # 재사용 가능 UI 컴포넌트

python/             # Python 보조 서버 (OCR, 유사물건 추천)
```

### 의존성 방향

```
NPLogic.App → NPLogic.Core, NPLogic.Data, NPLogic.UI
NPLogic.Data → NPLogic.Core
NPLogic.UI → (독립적)
NPLogic.Core → (독립적)
```

### MVVM 패턴

- **Views**: `src/NPLogic.App/Views/` - XAML UI 파일들
- **ViewModels**: `src/NPLogic.App/ViewModels/` - CommunityToolkit.Mvvm 사용
- **Models**: `src/NPLogic.Core/Models/` - 도메인 모델

### 의존성 주입

`App.xaml.cs`에서 Microsoft.Extensions.DependencyInjection으로 모든 서비스 등록:
- Services: Singleton
- Repositories: Singleton
- ViewModels: Transient
- Views: Transient

전역 서비스 접근: `App.ServiceProvider.GetRequiredService<T>()`

### 데이터 접근

- **백엔드**: Supabase (PostgreSQL + PostgREST API)
- **Repository 패턴**: `src/NPLogic.Data/Repositories/`
- **인증**: `AuthService` - 자동 로그인 및 세션 관리 지원

### 핵심 서비스

- `SupabaseService`: DB 연결 및 인증
- `AuthService`: 사용자 인증
- `PermissionService`: 권한 관리
- `PythonBackendService`: Python OCR/추천 서버 통신 (Singleton.Instance 패턴)
- `RightAnalysisRuleEngine`: 권리 분석 규칙 엔진
- `XnpvCalculator`: 순현재가 계산

## 주요 기술 스택

- **UI**: WPF + MaterialDesignThemes
- **MVVM**: CommunityToolkit.Mvvm
- **차트**: LiveChartsCore (SkiaSharp)
- **Excel**: ClosedXML, EPPlus
- **DB**: Supabase (supabase-csharp, postgrest-csharp)
- **로깅**: Serilog

## 주요 기능 영역

- 부동산 관리 (PropertyListView, PropertyDetailView)
- 부채정리 (NonCoreView)
- 대출 관리 (LoanDetailView, LoanSheetView)
- 평가 (EvaluationTab - 주택, 상업시설, 공장)
- 경매/공매 (AuctionPublicSaleView)
- 등기부등본 OCR (RegistryOcrService)
