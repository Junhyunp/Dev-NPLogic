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
- **Supabase Project ID**: `nlddampvgxamaukflqhd` (MCP 도구 사용 시 이 ID 사용)
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

## 데이터디스크 시트 대표컬럼

엑셀 데이터디스크 업로드 시 각 시트의 대표컬럼 매핑 정보입니다.

### 1. 차주일반정보 (BorrowerGeneral) - Sheet A
- **테이블**: `borrowers`
- **대표컬럼** (9개): 자산유형, 차주일련번호, 차주명, 관련차주, 차주형태, 미상환원금잔액, 미수이자, 근저당권설정액, 비고

### 2. 회생차주정보 (BorrowerRestructuring) - Sheet A-1, Sheet F
- **테이블**: `borrower_restructurings`
- **대표컬럼** (14개): 자산유형, 차주일련번호, 차주명, 세부 진행단계, 관할법원, 회생사건번호, 보전처분일, 개시결정일, 채권신고일, 인가/폐지결정일, 업종, 상장/비상장, 종업원수, 설립일

### 3. 채권일반정보 (Loan) - Sheet B, Sheet B-1
- **테이블**: `loans`
- **대표컬럼** (14개): 차주일련번호, 차주명, 대출일련번호, 대출과목, 계좌번호, 정상이자율, 연체이자율, 최초대출일, 최초대출원금, 환산된 대출잔액, 가지급금, 미상환원금잔액, 미수이자, 채권액 합계

### 4. 물건정보 (Property) - Sheet C-1
- **테이블**: `properties`
- **대표컬럼** (56개):
  - 기본: 자산유형, 차주일련번호, 차주명, 물건 일련번호, 물건 종류, 비고
  - 주소: 담보소재지 1, 담보소재지 2, 담보소재지 3, 담보소재지 4
  - 면적/금액: 물건 대지면적, 물건 건물면적, 물건 기타 (기계기구 등), 공담 물건 금액
  - 선순위: 물건별 선순위 설정액, 선순위 주택 소액보증금, 선순위 상가 소액보증금, 선순위 소액보증금, 선순위 주택 임차보증금, 선순위 상가 임차보증금, 선순위 임차보증금, 선순위 임금채권, 선순위 당해세, 선순위 조세채권, 선순위 기타, 선순위 합계
  - 감정평가: 감정평가구분, 감정평가일자, 감정평가기관, 토지감정평가액, 건물감정평가액, 기계평가액, 제시외, 감정평가액합계, KB아파트시세
  - 경매기본: 경매개시여부, 경매 관할법원
  - 경매(선행): 경매신청기관(선행), 경매개시일자(선행), 경매사건번호(선행), 배당요구종기일(선행), 청구금액(선행)
  - 경매(후행): 경매신청기관(후행), 경매개시일자(후행), 경매사건번호(후행), 배당요구종기일(후행), 청구금액(후행)
  - 경매기일/결과: 최초법사가, 최초경매기일, 최종경매회차, 최종경매결과, 최종경매기일, 차기경매기일, 낙찰금액, 최종경매일의 최저입찰금액, 차후최종경매일의 최저입찰금액

### 5. 등기부등본정보 (RegistryDetail) - Sheet C-2
- **테이블**: `registry_sheet_data`
- **대표컬럼** (8개): 차주일련번호, 차주명, 물건번호, 지번번호, 담보소재지1, 담보소재지2, 담보소재지3, 담보소재지4
- **참고**: OCR 코드의 basic_info 결과와 비교

### 6. 신용보증서 (Guarantee) - Sheet D
- **테이블**: `credit_guarantees`
- **대표컬럼** (10개): 자산유형, 차주일련번호, 차주명, 계좌일련번호, 보증기관, 보증종류, 보증서번호, 보증비율, 환산후 보증잔액, 관련 대출채권 계좌번호
