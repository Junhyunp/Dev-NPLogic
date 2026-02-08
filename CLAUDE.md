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

EC2 서버(3.34.10.57)에서 Docker로 실행 중:
```bash
cd python
docker compose up -d --build
# 포트: 8000
# OCR 엔드포인트: POST /api/ocr/registry
```

로컬 개발 시:
```bash
cd python
pip install -r requirements.txt
python server.py
```

## 아키텍처

### 프로젝트 구조

```
src/
├── NPLogic.App     # 메인 WPF 애플리케이션 (Views, ViewModels, Services)
├── NPLogic.Core    # 핵심 비즈니스 로직 및 도메인 모델
├── NPLogic.Data    # 데이터 접근 계층 (Repositories, Supabase 연동)
└── NPLogic.UI      # 재사용 가능 UI 컴포넌트

python/             # Python 보조 서버 (OCR, 유사물건 추천) - EC2에서 Docker로 운영
reference/          # 원청 참고자료 (Auction-Certificate 등)
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
- **세션 관리**: `SupabaseService.EnsureValidSessionAsync()` - JWT 만료 전 자동 갱신 (임계값 10분)

### 핵심 서비스

- `SupabaseService`: DB 연결 및 인증 (JWT UTC/Local 시간대 보정 포함)
- `AuthService`: 사용자 인증
- `PermissionService`: 권한 관리
- `PythonBackendService`: Python OCR/추천 서버 통신 (Singleton.Instance 패턴)
- `RightAnalysisRuleEngine`: 권리 분석 규칙 엔진
- `XnpvCalculator`: 순현재가 계산

### UI 네비게이션 구조

```
MainWindow
└── DashboardView (ContentControl)
    ├── 목록 모드: DataGrid (물건 목록, 서버 사이드 페이지네이션)
    └── 상세 모드: 내부 탭 (RadioButton 기반)
        ├── 비핵심 (NonCoreView) → 11개 기능 탭
        │   ├── 전체 (Home)
        │   ├── 차주개요
        │   ├── Loan
        │   ├── 담보물건 (CollateralPropertyView) ★ 등기부등본 정보 표시
        │   ├── 선순위
        │   ├── 회생개요
        │   ├── 평가
        │   ├── 경공매일정
        │   ├── 인터림
        │   ├── 현금흐름
        │   └── XNPV비교
        ├── 등기부등본 (RegistryTab) ★ PDF 업로드 + OCR 처리
        ├── 권리분석
        └── 기타 탭들...
```

**주의**: NonCoreView의 각 기능 탭은 `_tabViewCache`/`_tabViewModelCache`로 **캐시**됩니다.
같은 물건에서 탭 전환 시 캐시된 ViewModel이 재사용되므로, 데이터 갱신이 필요한 경우 명시적으로 새로고침해야 합니다.

## 등기부등본 OCR 파이프라인 (현재 아키텍처)

```
WPF App (RegistryTabViewModel)
    → PDF 파일 선택 + 물건 자동 매칭
    → Edge Function "ocr-registry-save" (v12) 호출
        → EC2 Python OCR 서버 (/api/ocr/registry) 호출
        → OCR 결과 + DD 데이터 병합
        → registry_runs / basic_info / gapgu_rows / eulgu_rows 저장
        → summary_images를 registry_runs.summary_images_base64에 저장
    ← 응답: run 정보, 정제 데이터, 요약 이미지
    
담보물건 탭 (PropertyDetailViewModel)
    → DB에서 property 기준 합산 조회
    → 갑구/을구 중복 행 병합 (접수정보+대상소유자 기준)
    → DataGrid 표시
    → "등기부등본" 버튼 → 이미지 뷰어 (DB에서 Base64 이미지 로드)
```

### Edge Function `ocr-registry-save` (v12)

- **인증**: `verify_jwt: false` + `supabase.auth.getUser()` (ES256/HS256 모두 지원)
- **파일명**: `source_pdf_name` form-data 필드로 별도 전달 (.NET 한글 인코딩 문제 우회)
- **deed_seq**: PDF 파일명에서 추출 (R-0003-1-05 → 5)
- **덮어쓰기**: "물건+PDF파일명" 단위 DELETE → INSERT
- **DD 주소 매칭**: `registry_sheet_data`에서 지번별 주소 조회 후 PDF 파일명 주소와 비교
- **이미지 저장**: `summary_images_base64` jsonb 컬럼에 Base64 이미지 배열 저장

### 등기부 관련 DB 테이블 (현재)

| 테이블 | 용도 |
|--------|------|
| `registry_runs` | 물건별 OCR 결과 세트 (PDF 1개 = 1 run), summary_images_base64 포함 |
| `registry_basic_info` | 소유지분현황 (run당 1행) |
| `registry_gapgu_rows` | 갑구 행 (run당 N행) |
| `registry_eulgu_rows` | 을구 행 (run당 N행) |
| `registry_sheet_data` | DD 엑셀 Sheet C-2 원본 (OCR과 무관) |

**삭제된 레거시 테이블**: `registry_documents`, `registry_owners`, `registry_rights`

## 주요 기술 스택

- **UI**: WPF + MaterialDesignThemes
- **MVVM**: CommunityToolkit.Mvvm
- **차트**: LiveChartsCore (SkiaSharp)
- **Excel**: ClosedXML, EPPlus
- **DB**: Supabase (supabase-csharp, postgrest-csharp)
- **로깅**: Serilog
- **지도**: 카카오 지도 API (WebView2), OpenStreetMap

## 주요 기능 영역

- 부동산 관리 (PropertyListView, PropertyDetailView)
- 부채정리 (NonCoreView - 11개 기능 탭)
- 대출 관리 (LoanDetailView, LoanSheetView)
- 평가 (EvaluationTab - 주택, 상업시설, 공장)
- 경매/공매 (AuctionPublicSaleView)
- 등기부등본 OCR (RegistryTab → Edge Function → Python OCR)
- 권리분석 (RightAnalysisTab, RightAnalysisRuleEngine)

## 데이터디스크 시트 대표컬럼

엑셀 데이터디스크 업로드 시 각 시트의 대표컬럼 매핑 정보입니다.

### 1. 차주일반정보 (BorrowerGeneral) - Sheet A
- **테이블**: `borrowers`
- **대표컬럼** (9개): 자산유형, 차주일련번호, 차주명, 관련차주, 차주형태, 미상환원금잔액, 미수이자, 근저당권설정액, 비고

### 2. 회생차주정보 (BorrowerRestructuring) - Sheet A-1, Sheet F
- **테이블**: `borrower_restructuring`
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

## 알려진 주의사항

- **NonCoreView 탭 캐시**: 같은 물건에서 다른 탭으로 갔다 돌아오면 `propertyChanged=false`로 캐시 재사용됨. "CollateralProperty" 탭은 등기부 데이터 자동 새로고침 처리됨.
- **DashboardView 탭 이벤트**: RadioButton.IsChecked 프로그래밍 변경 시 `_suppressInnerTabChecked`/`_suppressFunctionTabChecked` 플래그로 이중 로드 방지 필요
- **JWT 토큰**: `supabase-csharp`가 ES256 알고리즘 사용 → Edge Function은 `verify_jwt: false` + `getUser()` 수동 검증
- **파일명 인코딩**: .NET HttpClient의 한글 파일명 인코딩 문제 → `ContentDispositionHeaderValue` 직접 설정 또는 `source_pdf_name` 별도 전달
