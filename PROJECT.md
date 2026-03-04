# PROJECT.md

NPLogic 프로젝트 문서 SSOT이다. 2026-03-03 기준으로 이 저장소에 흩어져 있던 `README.md`, `CLAUDE.md`, `docs/*.md`, `python/AGENTS.md`, `reference/AGENTS.md`, `reference/Auction-Certificate/README.md`, `changelog.md`의 핵심 내용을 현재 코드베이스 기준으로 재정리했다.

추가로 2026-03-03에 Supabase MCP로 실제 프로젝트(`nlddampvgxamaukflqhd`)를 조회해 DB/OCR 관련 핵심 사실을 재검증했다. 이 문서에서 DB, 마이그레이션, Edge Function 관련 서술은 그 검증 결과를 반영한다.

검증 기준 표기:

- `[코드]`: 현재 저장소 코드에서 직접 확인한 내용
- `[MCP]`: Supabase MCP로 실DB/실프로젝트를 조회해 확인한 내용
- `[문서]`: 레거시 문서나 changelog를 바탕으로 정리했으며 코드나 MCP로 전부 재확인하지는 않은 내용
- 복합 표기는 해당 근거를 함께 사용했다는 뜻이다.

## 1. 문서 운영 원칙

- 이 문서는 프로젝트 문서의 단일 기준 문서다.
- 기존 개별 문서들은 호환용 엔트리 문서만 유지하고, 실질 내용은 여기서 관리한다.
- 문서와 코드가 충돌하면 먼저 코드를 확인하고, 확인 결과를 이 문서에 반영한다.
- 런타임의 최종 진실은 코드와 실제 DB 상태지만, 사람이 참조하는 프로젝트 설명과 운영 지식은 이 문서가 기준이다.
- 실제 DB 스키마는 Supabase 실DB가 최종 상태다.
- 현재 저장소에는 `supabase/migrations/` 디렉터리가 없지만, Supabase 프로젝트 내부에는 마이그레이션 이력이 존재한다.
- 따라서 DB 관련 설명은 코드와 Supabase MCP 결과를 우선하고, 저장소 내 파일만 보고 스키마를 단정하지 않는다.

## 2. 프로젝트 개요

NPLogic은 .NET 10 기반 WPF 데스크톱 애플리케이션으로, 부실채권(NPL)과 담보부동산 평가 업무를 통합 관리한다. 핵심 업무 범위는 다음과 같다.

- 프로그램/프로젝트 단위 관리
- 차주, 대출, 담보물건 관리
- 데이터디스크(엑셀) 업로드 및 매핑
- 등기부등본 OCR 및 정제 데이터 관리
- 선순위/권리분석
- 평가, 실거래가, 유사사례 추천
- 경매/공매 시나리오 및 회수 분석
- 인터림, 현금흐름, XNPV 비교

## 3. 현재 저장소 구조

```text
.
├─ PROJECT.md
├─ NPLogic.sln
├─ src/
│  ├─ NPLogic.App    # WPF 앱
│  ├─ NPLogic.Core   # 도메인 모델, 핵심 계산/룰
│  ├─ NPLogic.Data   # Supabase 연동, Repository
│  └─ NPLogic.UI     # 공통 UI 컴포넌트/스타일
├─ python/           # FastAPI 기반 보조 서버 (OCR/추천)
├─ registry_ocr/     # 레거시 OCR 스크립트/의존성 일부
└─ reference/        # 원청 참고자료, 샘플, 레거시 OCR 파이프라인
```

## 4. 기술 스택

### 4.1 애플리케이션

- .NET 10
- WPF
- CommunityToolkit.Mvvm
- MaterialDesignThemes
- LiveChartsCore + SkiaSharp
- WebView2
- ClosedXML
- EPPlus
- Serilog

### 4.2 데이터/백엔드

- Supabase
- PostgreSQL / PostgREST
- `supabase-csharp`, `postgrest-csharp`

### 4.3 Python 보조 서비스

- FastAPI
- Uvicorn
- pandas / numpy
- pytesseract / pdf2image / Pillow / PyPDF2
- PyYAML
- Supabase Python SDK

## 5. 프로젝트 구조와 의존성

### 5.1 솔루션 의존성 방향

```text
NPLogic.App -> NPLogic.Core, NPLogic.Data, NPLogic.UI
NPLogic.Data -> NPLogic.Core
NPLogic.Core -> 독립
NPLogic.UI -> 독립
```

### 5.2 각 프로젝트 역할

#### `src/NPLogic.App`

- 메인 WPF 애플리케이션
- Views, ViewModels, 앱 서비스, 설정, 실행 진입점 포함
- `App.xaml.cs`에서 DI 컨테이너를 직접 구성

#### `src/NPLogic.Core`

- 도메인 모델
- 권리분석 룰 엔진
- XNPV/XIRR 계산
- UI/DB 의존성이 없는 핵심 계산 로직

#### `src/NPLogic.Data`

- Supabase 연결
- 인증/세션 관리
- Repository 패턴
- PostgREST 모델 매핑

#### `src/NPLogic.UI`

- 공통 컨트롤
- 공통 스타일
- 재사용 가능한 UI 리소스

## 6. 아키텍처 개요

### 6.1 패턴

- 기본 구조는 MVVM이다.
- View는 XAML 중심이다.
- ViewModel은 CommunityToolkit.Mvvm 기반이다.
- 데이터 접근은 Repository 패턴이다.
- 서비스 등록은 `Microsoft.Extensions.DependencyInjection` 기반 수동 등록이다.

### 6.2 DI 구성 현황

2026-03-03 기준 저장소에서 확인한 대략적 규모:

- `src/NPLogic.App/Services`: 19개 C# 파일
- `src/NPLogic.Data/Repositories`: 30개 C# 파일
- `src/NPLogic.App/ViewModels`: 32개 C# 파일

`App.xaml.cs`에서 다음이 등록된다.

- Singleton: `SupabaseService`, `AuthService`, `ExcelService`, `StorageService`, 지도/외부연동 서비스, 각 Repository, 업로드 서비스, 권한 서비스
- Transient: 대부분의 ViewModel, View, Window
- `PythonBackendService`는 DI 등록 대신 자체 Singleton 패턴(`Instance`)을 사용한다

### 6.3 전역 구성상 중요한 사실

- `App.xaml.cs`에 Supabase URL과 anon key가 하드코딩되어 있다.
- `src/NPLogic.App/appsettings.json.template`는 존재하지만, 현재 시작 경로는 설정 파일이 아니라 상수 기반으로 Supabase에 연결한다.
- 이는 문서화와 보안 관점에서 중요한 리스크다. 설정 일원화가 필요하다.

## 7. UI/탭 구조

메인 네비게이션은 다음 흐름으로 동작한다.

```text
MainWindow
└─ DashboardView
   ├─ 목록 모드: 물건 리스트
   └─ 상세 모드
      ├─ 비핵심(NonCoreView)
      │  ├─ 전체(Home)
      │  ├─ 차주개요
      │  ├─ Loan
      │  ├─ 담보물건
      │  ├─ 선순위
      │  ├─ 회생개요
      │  ├─ 평가
      │  ├─ 경공매일정
      │  ├─ 인터림
      │  ├─ 현금흐름
      │  └─ XNPV비교
      ├─ 등기부등본
      ├─ 권리분석
      └─ 기타 관리 탭
```

주의사항:

- `NonCoreView`는 내부 탭 캐시를 사용한다.
- 같은 물건에서 탭 전환 시 ViewModel이 재사용될 수 있으므로, 데이터 강제 새로고침이 필요한 화면은 명시적으로 처리해야 한다.

## 8. 개발 환경

### 8.1 필수 소프트웨어

- .NET 10 SDK
- Visual Studio 2022 17.12+ 또는 VS Code
- Python 3.10+
- Git
- WebView2 Runtime

### 8.2 Python/OCR 추가 요구사항

- Tesseract OCR
- Poppler 또는 `pdf2image`가 요구하는 PDF 변환 환경

### 8.3 설정 파일

`src/NPLogic.App/appsettings.json.template`에는 다음 범주의 설정이 정의되어 있다.

- Supabase
- MapServer
- KakaoMap
- RealEstateAPI
- Logging
- Python

다만 현재 앱의 Supabase 연결은 이 파일이 아니라 `App.xaml.cs` 상수값을 사용한다.

## 9. 빌드, 실행, 배포

### 9.1 빌드/실행

```bash
dotnet restore NPLogic.sln
dotnet build NPLogic.sln
dotnet run --project src/NPLogic.App/NPLogic.App.csproj
```

### 9.2 배포

`src/NPLogic.App/NPLogic.App.csproj` 기준:

- TargetFramework: `net10.0-windows`
- RuntimeIdentifier: `win-x64`
- SelfContained: `true`
- PublishReadyToRun: `true`
- PublishTrimmed: `false`

예시:

```bash
dotnet publish src/NPLogic.App/NPLogic.App.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish
```

배포 시 같이 복사되도록 설정된 주요 항목:

- `appsettings.json`
- `Assets/Maps/*.html`
- `Resources/mapping_template.json`
- 존재할 경우 `python/dist/nplogic_backend.exe`
- 존재할 경우 레거시 OCR 실행 파일

## 10. 테스트 현황

- 저장소 내 별도 테스트 프로젝트는 확인되지 않았다.
- 문서상 `dotnet test` 예시는 있었지만, 현재 리포지토리 구조상 테스트 코드가 갖춰져 있지 않다.
- 즉, 현재 품질 검증은 수동 검증과 실제 동작 확인에 크게 의존하는 상태다.

## 11. 외부 시스템 연동

### 11.1 Supabase

검증 기준: `[코드]` + `[MCP]`

- 프로젝트 ID: `nlddampvgxamaukflqhd`
- 프로젝트 URL: `https://nlddampvgxamaukflqhd.supabase.co`
- 역할:
  - 인증
  - 프로그램/차주/대출/물건 데이터 저장
  - 등기/OCR 정제 결과 저장
  - 평가/경매/공매/QA/설정 관리

중요:

- Supabase MCP 기준 `public` 스키마의 base table 수는 `53`개다.
- `public`의 53개 테이블 모두 RLS가 활성화되어 있다.
- 현재 저장소에는 `supabase/migrations/` 폴더가 없지만, Supabase 프로젝트 내부 마이그레이션 이력은 존재한다.
- MCP 기준 최신 마이그레이션은 `20260227154544 create_property_trade_applied`다.
- 따라서 “저장소에 마이그레이션 파일이 없다”와 “DB에 마이그레이션 이력이 없다”는 전혀 다른 이야기다.

핵심 테이블 현재 row count(MCP 조회 시점):

- `users`: 1
- `programs`: 1
- `borrowers`: 493
- `properties`: 555
- `loans`: 1503
- `property_trade_applied`: 1

### 11.2 Python FastAPI 백엔드

검증 기준: `[코드]`

`python/server.py`에서 확인되는 엔드포인트:

- `GET /api/health`
- `POST /api/ocr/registry`
- `POST /api/recommend/similar`

역할:

- 등기부등본 OCR 처리
- 유사 물건 추천

개발 실행:

```bash
cd python
pip install -r requirements.txt
python server.py
```

배포 관련 파일:

- `python/Dockerfile`
- `python/docker-compose.yml`
- `python/deploy.sh`
- `python/nplogic_backend.spec`

### 11.3 지도/부동산 외부 연동

검증 기준: `[코드]`

문서와 코드상 확인되는 연동:

- Kakao Map
- Vworld
- 국토부 실거래가 API(Data.go.kr)
- 외부 Supabase 기반 거래 데이터 RPC(`TradeService`)

### 11.4 Supabase Edge Functions

검증 기준: `[MCP]` + `[코드]`

Supabase MCP 기준 활성 Edge Function:

- `get-map-config` v3 (`verify_jwt = false`)
- `ocr-registry-save` v12 (`verify_jwt = false`)

즉, 기존 문서에 있던 `ocr-registry-save v12` 서술은 실DB 기준으로도 맞다. 코드 기준으로도 등기 OCR 저장 경로는 `RegistryRepository.OcrRegistrySaveViaEdgeFunctionAsync(...)`를 통해 `ocr-registry-save`를 호출한다.

## 12. 핵심 도메인과 데이터 모델

### 12.1 주요 엔티티

검증 기준: `[코드]` + `[MCP]`

- 프로그램 `programs`
- 사용자 `users`
- 프로그램-사용자 매핑 `program_users`
- 차주 `borrowers`
- 대출 `loans`
- 담보물건 `properties`
- 권리분석 `right_analysis`
- 평가 `evaluations` 및 하위 테이블
- 경매/공매 `auction_schedules`, `public_sale_schedules`, `auction_cases`
- 등기 OCR `registry_runs`, `registry_basic_info`, `registry_gapgu_rows`, `registry_eulgu_rows`
- 등기 시트 원본 `registry_sheet_data`
- 선순위 관련 `lease_items`, `wage_claim_items`, `registry_rights`
- 신용보증 `credit_guarantees`
- 실거래가 적용 저장 `property_trade_applied`

### 12.2 현재 문서상 DB 규모

검증 기준: `[MCP]`

실DB 기준 `public` base table은 총 `53`개다.

기존 ERD 문서의 `52개`는 현재 기준으로는 오래된 정보다.

실DB 도메인 분포를 크게 나누면 다음과 같다.

- 사용자/프로그램: `users`, `programs`, `program_users`
- 차주/대출/자금: `borrowers`, `borrower_restructuring`, `loans`, `loan_info`, `credit_guarantees`, `interim_advances`, `interim_collections`
- 물건/QA: `properties`, `property_qa`
- 등기/OCR: `registry_runs`, `registry_basic_info`, `registry_gapgu_rows`, `registry_eulgu_rows`, `registry_rights`, `registry_sheet_data`, `registry_gapgu_ownership_shares`, `registry_gapgu_rights_summary`, `registry_eulgu_rights_summary`
- 권리분석/선순위 산정: `right_analysis`, `lease_items`, `wage_claim_items`, `lease_standards`, `legal_application_rates`
- 평가/실거래/감정: `evaluations`, `evaluation_cases`, `evaluation_land_parcels`, `evaluation_machinery`, `evaluation_rental_analysis`, `evaluation_management_fees`, `evaluation_commercial_data`, `property_jibun_appraisals`, `property_machinery_appraisals`, `property_trade_applied`, `appraisal_firms`
- 경매/공매: `auction_cases`, `auction_schedules`, `auction_cost_standards`, `public_sale_schedules`, `public_sale_cost_standards`
- 참조/설정/운영: `courts`, `common_codes`, `calculation_formulas`, `financial_institutions`, `data_disks`, `program_sheet_mappings`, `app_config`, `settings`, `system_settings`, `audit_logs`, `qa_notifications`

### 12.3 물건 중심 구조

검증 기준: `[코드]` + `[MCP]`

이 시스템은 사실상 `properties`를 중심으로 대부분의 정보가 연결된다.

- `properties` -> `borrowers`
- `properties` -> `right_analysis`
- `properties` -> `evaluations`
- `properties` -> `auction_schedules`
- `properties` -> `public_sale_schedules`
- `properties` -> `registry_runs`
- `properties` -> `registry_rights`
- `properties` -> `lease_items`
- `properties` -> `wage_claim_items`
- `properties` -> `property_jibun_appraisals`
- `properties` -> `property_machinery_appraisals`
- `properties` -> `property_trade_applied`
- `properties` -> `registry_basic_info`, `registry_gapgu_rows`, `registry_eulgu_rows`

## 13. 등기부등본 OCR 파이프라인

검증 기준: `[코드]` + `[MCP]` + `[문서]`

현재 문서와 코드에서 파악되는 구조:

```text
WPF App
-> RegistryTabViewModel / RegistryRepository
-> Python OCR 또는 Supabase Edge Function 경유 처리
-> registry_runs / registry_basic_info / registry_gapgu_rows / registry_eulgu_rows 저장
-> 담보물건/선순위 화면에서 정제 결과 조회
```

현재 코드 기준 주 저장 경로는 `RegistryTabViewModel`이 `RegistryRepository.OcrRegistrySaveViaEdgeFunctionAsync(...)`를 호출해 `ocr-registry-save` Edge Function으로 PDF와 `source_pdf_name`을 전달하는 방식이다. `summary_images_base64` 컬럼 매핑도 코드에 존재한다.

핵심 저장 테이블:

- `registry_runs`
- `registry_basic_info`
- `registry_gapgu_rows`
- `registry_eulgu_rows`
- `registry_gapgu_ownership_shares`
- `registry_gapgu_rights_summary`
- `registry_eulgu_rights_summary`

관련 구현 단서:

- `summary_images_base64` 사용
- 물건 단위 OCR 결과 집계
- 갑구/을구 중복 병합 표시
- 주소 일치 여부 비교

레거시 참고자료:

- `reference/Auction-Certificate/`는 Clova OCR 기반의 별도 파이프라인 문서를 담고 있다.
- 현재 앱의 주 실행 경로와 완전히 동일하다고 보긴 어렵지만, OCR 정제/배치 처리 참고용으로 유효하다.

실DB 재검증 결과:

- `registry_documents`: 없음
- `registry_owners`: 없음
- `registry_runs`: 존재
- `registry_basic_info`: 존재
- `registry_gapgu_rows`: 존재
- `registry_eulgu_rows`: 존재
- `registry_gapgu_ownership_shares`: 존재
- `registry_gapgu_rights_summary`: 존재
- `registry_eulgu_rights_summary`: 존재
- 현재 이 프로젝트의 OCR 정제 테이블 행 수는 모두 `0`이다 (`registry_runs`, `registry_basic_info`, `registry_gapgu_rows`, `registry_eulgu_rows`).

즉, 스키마는 배포돼 있지만 현재 프로젝트 DB에는 OCR 결과 데이터가 아직 적재되지 않은 상태다.

## 14. 데이터디스크 업로드 체계

검증 기준: `[문서]` + `[코드]`

원청 참고자료와 기존 문서 기준으로 업로드 핵심 시트는 다음과 같다.

### 14.1 대표 시트

1. 차주일반정보
2. 회생차주정보
3. 채권일반정보
4. 물건정보
5. 등기부등본정보
6. 신용보증서

### 14.2 관련 자원

- `reference/검증/01. 대표시트명, 대표컬럼명.txt`
- `reference/검증/02. 데이터디스크 업로드시 고려사항.txt`
- `reference/검증/03. mapping_template.json`
- `src/NPLogic.App/Resources/mapping_template.json`
- `src/NPLogic.App/Services/SheetMappingConfig.cs`
- `src/NPLogic.App/Services/BankMappingConfig.cs`
- `src/NPLogic.App/Services/DataDiskUploadService.cs`
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`

### 14.3 업로드 시 주의할 점

- 은행별 시트명과 컬럼명이 다르다.
- 병합 셀, 날짜 형식, 금액 표기, 공백 변형에 대한 방어 코드가 중요하다.
- 대표컬럼 기반 검증이 핵심이다.
- 신규 은행 추가 시 매핑 템플릿, 샘플, 파서, 테스트를 같이 갱신해야 한다.

## 15. 비핵심조서/산출물 체계

검증 기준: `[문서]`

원청 참고자료 기준 비핵심조서는 최종 산출물 역할을 한다.

주요 데이터 소스:

- 차주/회생: `borrowers`, `borrower_restructuring`
- 채권: `loans`, `credit_guarantees`
- 물건: `properties`
- 권리분석: `right_analysis`
- 평가: `evaluations` 및 하위 테이블
- 경매/공매: `auction_schedules`, `public_sale_schedules`

중요 참고자료:

- `reference/비핵심조서양식/`
- `reference/산출화면/`

문서상 핵심 포인트:

- 조서 출력은 템플릿 기반 Excel 산출물 성격이 강하다.
- XNPV/현금흐름/선순위/물건/채권 정보가 한데 모인다.
- 기존 샘플 엑셀은 화면 설계와 산출 구조를 이해하는 데 유용하지만, 실제 구현과 차이가 있을 수 있다.

## 16. 핵심 비즈니스 로직

### 16.1 권리분석 룰 엔진

검증 기준: `[코드]` 우선, `[문서]` 보조

위치:

- `src/NPLogic.Core/Services/RightAnalysisRuleEngine.cs`

이 엔진은 선순위 판단과 추정 근거 케이스를 계산한다. 코드에서 직접 확인된 것은 주택/토지/상가 계열의 케이스 코드와 임금채권/조세 계산 메서드 존재 여부다.

코드상 확인되는 케이스 군:

- 주택: `CASE_R1` ~ `CASE_R18`
- 토지: `CASE_L1` ~ `CASE_L4`
- 상가/공장: `CASE_C1` ~ `CASE_C17`
- 임금채권: `ApplyWageClaimRules(...)` 메서드로 임금채권 추정 로직 구현
- 당해세/선순위 조세: `ApplyTaxRules(...)` 메서드로 조세채권 추정 로직 구현

판단 축:

- 물건 유형
- 경매 개시 여부
- 배당요구종기 경과 여부
- 현황조사서/전입세대열람/상가임대차열람 존재 여부
- 임차인 존재 여부
- 근저당 설정일 대비 임차일 선후
- 주소 일치 여부
- 임금채권/당해세/조세 추정 정보

### 16.2 XNPV / XIRR

검증 기준: `[코드]`

위치:

- `src/NPLogic.Core/Services/XnpvCalculator.cs`

역할:

- 불규칙 현금흐름의 현재가치 계산
- 내부수익률 계산

활용 화면/업무:

- 현금흐름 정리
- XNPV 비교
- 비핵심조서 산출

### 16.3 경매/공매 시나리오

검증 기준: `[문서]`

문서상 기본 개념:

- 보수적 시나리오
- 낙관적 시나리오
- 회차별 저감율
- 경매 비용
- 선순위 공제 후 배당 가능액
- Loan Cap 적용
- 회수율 계산

## 17. 평가/실거래가/추천 기능

검증 기준: `[코드]` + `[MCP]` + `[문서]`

평가 영역에서 현재 코드와 changelog, 실DB로 확인되는 핵심:

- 평가 탭 저장 기능
- 실거래가 외부 연동
- PNU 자동 확보
- 사례지도와 실거래가 통합 UI
- 실거래가 적용 여부를 `property_trade_applied`로 저장
- 유사물건 추천은 Python 추천 엔진을 사용

코드 기준 실제 경로:

- `EvaluationTabViewModel.LoadRealTransactionsAsync()`가 실거래가 조회 진입점이다.
- PNU가 비어 있으면 `CleanAddressForPnuLookup()`로 주소를 정리한 뒤 `VworldService.SearchAddressAsync(...)`로 PNU를 보완한다.
- 실거래가 조회는 `TradeService.GetTradesByPnuAsync(...)`를 통해 Supabase RPC `get_trades`를 호출한다.
- 적용 체크 상태는 `PropertyTradeAppliedRepository`를 통해 `property_trade_applied` 테이블에 저장/복원된다.
- 추천은 `RecommendService`가 Python 백엔드의 `POST /api/recommend/similar`를 호출한다.

실DB 기준 현재 `property_trade_applied` row count는 `1`이다.

관련 코드:

- `src/NPLogic.App/ViewModels/EvaluationTabViewModel.cs`
- `src/NPLogic.App/Views/EvaluationTab.xaml`
- `src/NPLogic.App/Services/TradeService.cs`
- `src/NPLogic.App/Services/RecommendService.cs`
- `python/recommend/`

## 18. 선순위/Loan 관련 기능 메모

검증 기준: `[코드]` + `[문서]`

최근 문서와 변경 이력상 비중이 큰 영역:

- 선순위 항목 산정표 자동 판단
- 보증서요약/집계/배당가능재원/안분비율 DataGrid
- Loan Cap 1 계산
- MCI 산식 구현
- DD 업로드 필드 매핑 보강
- 채권정보/이미지 저장

관련 핵심 파일:

- `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs`
- `src/NPLogic.App/Views/Loan/Sections/*`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.Core/Models/Loan.cs`

## 19. Python 백엔드 상세

검증 기준: `[코드]`

### 19.1 역할

- OCR 처리
- 유사물건 추천

### 19.2 핵심 파일

- `python/server.py`
- `python/ocr_processor.py`
- `python/recommend_processor.py`
- `python/recommend/recommend.py`
- `python/recommend/utils.py`
- `python/recommend/config.yaml`

### 19.3 주요 실행 방식

- 로컬 실행
- Docker 기반 서버 실행
- PyInstaller 기반 실행 파일 생성

### 19.4 C# 연동

- `src/NPLogic.App/Services/PythonBackendService.cs`
- `src/NPLogic.App/Services/RegistryOcrService.cs`
- `src/NPLogic.App/Services/RecommendService.cs`

## 20. 레거시 OCR 참고자료

검증 기준: `[문서]`

`reference/Auction-Certificate/README.md`에 정리돼 있던 내용은 참고용 레거시 OCR 문서다.

핵심 내용:

- Clova OCR 기반 등기부등본 처리 파이프라인
- PDF 요약 페이지 자동 추출
- 표 인식, 주소 매칭, CSV 산출
- 대량 PDF 멀티프로세싱

현재 프로젝트와의 관계:

- 직접 운영 메인라인이라기보다 참고 구현/실험/레거시 자산에 가깝다.
- OCR 정제 구조나 파일 처리 방식 비교 시 참고 가능하다.

## 21. 알려진 주의사항과 리스크

검증 기준: `[코드]` + `[MCP]` + `[문서]`

### 21.1 문서/구조 불일치

- 저장소에는 `supabase/migrations/`가 없지만, Supabase DB 내부 마이그레이션은 존재한다.
- 일부 문서는 초기 설계 기준이라 현재 코드와 완전히 일치하지 않는다.
- 기존 ERD 문서의 테이블 수(`52`)는 실DB 기준 현재값(`53`)과 다르다.

### 21.2 설정 일원화 부족

- `appsettings.json.template`가 존재하지만 Supabase 접속 정보는 `App.xaml.cs`에 하드코딩되어 있다.
- 설정 문서와 실제 동작이 다르다.

### 21.3 NonCoreView 캐시

- 내부 탭 캐시 때문에 물건 전환 후 데이터가 오래 남는 버그가 반복적으로 발생해 왔다.
- 관련 수정 이력이 많으므로 신규 기능 추가 시 캐시 경로를 먼저 확인해야 한다.

### 21.4 세션/JWT

- Supabase 세션 자동 갱신 관련 이력이 많다.
- 절전/잠금 복귀, UTC/Local 시간 처리, 동시 갱신 방지 로직을 건드릴 때 회귀 위험이 높다.

### 21.5 테스트 부재

- 자동 테스트 부재로 인해 UI/업로드/배치 로직 변경 시 회귀 검증 비용이 크다.

## 22. 최근 변경 이력 요약

검증 기준: `[문서]` + `[코드]`

기존 `changelog.md`의 핵심만 현재 상태 이해에 필요한 수준으로 요약한다.

### 2026-03-04

- 실거래가 카테고리 매핑 수정: multiplex→villa, factory→apt_factory, 미존재 카테고리(house, land) 제거
- 연립다세대 사례지도 섹션 아파트와 동일하게 복원 (실거래가 표·추이 차트 표시)
- 연립다세대 전용 탐문 내역 섹션 신설 (부동산명/연락처/탐문내역 3열 편집 DataGrid, +/− 테이블 생성·제거, 행 추가·삭제)
- 연립다세대 전용 탐문 결과 섹션 신설 (구분/감정가/면적(평)/단가/평가액 5열, 고정 4행: 토지·건물·기계·합계, +/− 테이블 생성·제거)
- 연립다세대 유형 화면 분기: 사례지도 섹션 전체 너비 지도만 표시, 실거래가/추이 차트/회수 전략 요약/인터림 섹션 숨김
- 차주별 물건 수 동적 행 버그 수정 (EvaluationTabViewModel에서 PropertyRepository 직접 조회)
- 인터림 상계/회수 섹션 신설 (16열 DataGrid, 마지막 6열 체크박스, 모든 물건 공통 표시)
- 인터림 지출 섹션 신설 (10열 DataGrid, 마지막 2열 체크박스)
- 시나리오별 배당 요약 섹션 삭제
- 실거래가 DataGrid 스마트 스크롤 (내부 스크롤 끝 도달 시만 외부 전달)
- 선순위 탭 스크롤 수정 (수평 ScrollViewer 휠 관통 + DataGrid 3개 휠 전달)

### 2026-03-03

- 회수 전략 요약: 동적 행 생성 (차주별 물건 수 기반 Cdate 이후 경매비용/담보 N 배당회수)
- 회수 전략 요약: Cap 적용 여부 컬럼 정렬 + 셀 디자인 평가결과 헤더와 통일
- 회수 전략 요약: 구분 열/드롭다운 열 너비 확대, 1행-2행 구분선 강조
- 평가 탭 DataGrid 스크롤 관통 수정 (사례평가/실거래가/배당요약 3개 DataGrid)
- 회수 전략 요약 섹션 디자인 개편 (8열 좌우 병렬 테이블, 차주별 첫 물건만 표시)
- IsFirstPropertyInBorrower 판별 로직을 SetInterimData에서 SetProperty로 분리
- 평가결과 섹션 디자인 개편 (새 섹션 헤더 + 6열 테이블, 시나리오 1/2)
- 낙찰통계 섹션 디자인 전면 개편 (5x10 테이블, 시/구/동 그룹 색상 구분)
- 평가 탭 경매사건검색 섹션 신설 (사례1/사례2 플레이스홀더)
- 평가 탭 거래가격 추이/거래건수 추이 차트 플레이스홀더 추가
- 평가 탭 실거래가 거래면적 컬럼에 단위(㎡) 표기 추가
- 담보물건 탭 지도(위성도/지적도/로드뷰) 물건 전환 시 미갱신 버그 수정
- `PROJECT.md`를 루트 SSOT 문서로 신설하고 분산 문서 내용을 통합
- Supabase MCP 기준으로 DB/OCR 사실을 재검증하고 섹션별 `검증 기준` 표기 추가
- 루트 `README.md`를 저장소 진입용 포인터 문서로 축약
- 기존 분산 `.md` 문서를 삭제해 문서 진실 공급원을 `PROJECT.md` 하나로 정리
- `.gitignore`에 `.omc/`를 추가하고 기존 `.omc` 추적 파일을 Git 인덱스에서 제거

### 2026-02-28

- 평가 탭의 사례지도와 실거래가 섹션 통합
- 실거래가 정렬 개선
- `property_trade_applied` 도입으로 체크 상태 DB 저장
- 외부 Supabase RPC 기반 실거래가 연동
- PNU 자동 확보 및 주소 정제 로직 추가

### 2026-02-20

- 평가 탭 저장 버튼 추가
- 평가 유형 자동 선택 로직 업데이트
- 실거래가 섹션 UI 리뉴얼

### 2026-02-19

- 평가 탭 1칼럼 레이아웃 전환
- 추천 엔드포인트 수정

### 2026-02-17

- 일반보증 탭 섹션 통합
- 보증여부 요약/집계/배당가능재원/안분비율 DataGrid 추가

### 2026-02-16

- 보증서요약 디자인 통일
- JWT 갱신 안정화
- MCI 산식 구현
- 사이드바 무한스크롤 추가

### 2026-02-15

- Loan 탭 UI 정리
- 채권정보 저장 버튼 및 이미지 영속화
- Loan Cap 섹션 구현
- DD 임포트 누락 필드 보강

### 2026-02-14

- 선순위 항목 산정표 자동 판단 기능 구현
- 업로드 배치 처리 성능 리팩터링
- `registry_rights` 스키마에 맞춘 C# 코드 동기화
- 선순위 탭 경매사건 테이블 레이아웃 변경
- 물건번호 생성 로직 수정

### 2026-02-09

- 평가/감정 관련 DataGrid UI 버그 수정
- 지번별 감정평가, 기계기구 감정가 패널 보강
- NonCoreView 탭 캐시 버그 수정

### 2026-02-08

- 지적도 패널을 위성도 + 필지 경계 폴리곤으로 개선
- 등기부등본 이미지 뷰어 추가
- 비핵심 전체 화면 로드/잔상 문제 수정

### 2026-02-07

- 등기부등본 OCR 전체 파이프라인 개편
- OCR 결과 저장 구조 및 조회 경로 정리

### 2026-02-05

- `JWT expired` 오류 대응
- 대시보드 초기 페이지 스킵 문제 수정

### 2026-02-04

- 등기부등본 OCR 스키마 개편
- 신규 등기 테이블 구조 도입

## 23. 레거시 문서 맵

다음 문서들은 더 이상 개별 SSOT가 아니며, 2026-03-03 기준으로 이 문서에 통합된 뒤 저장소에서 삭제되었다.

- `CLAUDE.md`
- `changelog.md`
- `docs/AGENTS.md`
- `docs/1. ENVIRONMENT_SETUP.md`
- `docs/2. ERD.md`
- `docs/4. SERVICE_GUIDE.md`
- `docs/5. BUSINESS_LOGIC.md`
- `docs/6. DEPLOYMENT_GUIDE.md`
- `python/AGENTS.md`
- `reference/AGENTS.md`
- `reference/Auction-Certificate/README.md`

`README.md`는 삭제하지 않고 저장소 진입용 포인터 문서로만 유지한다.

## 24. PROJECT.md 업데이트 규칙

다음 변경이 발생하면 이 문서를 같이 갱신한다.

- 신규 프로젝트/폴더/주요 서비스 구조 변경
- DI 등록 구조 변경
- DB 주요 테이블 추가/삭제/개편
- OCR/추천/외부 API 연동 경로 변경
- 데이터디스크 업로드 규칙 변경
- 권리분석/평가/경매 핵심 로직 변경
- 배포/설정 방식 변경
- 장기적으로 참고해야 하는 중요한 회귀 이슈 또는 운영 리스크 발생

## 25. 빠른 참조

### 25.1 시작 명령

```bash
dotnet build NPLogic.sln
dotnet run --project src/NPLogic.App/NPLogic.App.csproj
```

### 25.2 Python 서버

```bash
cd python
pip install -r requirements.txt
python server.py
```

### 25.3 핵심 파일

- 앱 시작/DI: `src/NPLogic.App/App.xaml.cs`
- Supabase 연결: `src/NPLogic.Data/Services/SupabaseService.cs`
- 인증: `src/NPLogic.Data/Services/AuthService.cs`
- 권리분석 룰: `src/NPLogic.Core/Services/RightAnalysisRuleEngine.cs`
- XNPV: `src/NPLogic.Core/Services/XnpvCalculator.cs`
- OCR/등기 저장: `src/NPLogic.Data/Repositories/RegistryRepository.cs`
- 평가 탭: `src/NPLogic.App/ViewModels/EvaluationTabViewModel.cs`
- Loan 시트: `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs`
- 선순위 탭: `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- 데이터디스크 업로드: `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`

## 26. 남은 정리 과제

이 문서를 SSOT로 전환하면서 확인된 후속 과제:

1. `App.xaml.cs`의 Supabase 하드코딩 제거 및 설정 일원화
2. 실제 DB 스키마 추적 경로 복구 또는 저장소 내 마이그레이션 관리 체계 추가
3. 테스트 프로젝트 도입
4. changelog의 세부 이력을 구조화된 릴리스 노트 형식으로 재정비
5. 원청 참고자료와 현재 구현 간 차이 목록화
