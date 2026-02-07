# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-07

#### 등기부등본 OCR 전체 파이프라인 개편

**Edge Function `ocr-registry-save` v11**
- **인증**: `verify_jwt: false` + `supabase.auth.getUser()` 수동 검증 (ES256/HS256 모두 지원)
- **파일명 전달**: .NET의 한글 파일명 인코딩 문제 해결 → `source_pdf_name` 별도 form-data 필드로 전달
- **덮어쓰기**: "물건+PDF파일명" 단위 DELETE → INSERT (같은 파일 재업로드 시 교체, 다른 파일은 유지)
- **deed_seq**: 트리거 auto-increment 대신 PDF 파일명에서 직접 추출 (R-0003-1-05 → deed_seq=5)
- **DD 주소 (물건지 DD)**: `registry_sheet_data` 테이블에서 지번별 개별 주소 조회 후 PDF 파일명 주소와 매칭
- **등기 주소 (물건지 등기부등본)**: PDF 파일명에서 주소 추출 (reference/Auction-Certificate 기존 로직과 동일)
- **지번번호**: deed_seq를 2자리 문자열(01, 02...)로 갑구/을구 행에 자동 채움

**WPF 앱 변경**
- basic_info: 단일 객체 → `ObservableCollection<RegistryBasicInfo>` (DataGrid N행)
- 갑구/을구: run 단위 → property 단위 합산 조회 (여러 PDF 결과 통합)
- 갑구/을구 DataGrid에 `지번번호` 컬럼 추가
- 등기부등본 탭: PDF 업로드 + OCR 처리 전용으로 단순화 (결과 조회 UI 제거)
- 결과 조회: 비핵심 → 담보물건 탭의 "등기부등본 정보" 패널에서만 확인

**변경된 파일/서비스**
- Supabase Edge Function `ocr-registry-save` v11
- `src/NPLogic.Data/Repositories/RegistryRepository.cs`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/Views/RegistryTab.xaml`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`

#### PDF 재업로드 시 기존 완료 파일 매칭/재처리 문제 수정

**문제점**
- 이미 OCR 완료된 PDF를 다시 선택하면 중복 체크로 건너뛰어, 재처리 불가
- 이전 매칭 정보(다른 물건에 연결된 상태)가 그대로 유지되어 잘못된 물건에 데이터 저장
- `MatchStatusText` (computed property)가 `IsAutoMatched`/`MatchConfidence` 변경 시 UI에 알림되지 않음

**해결 방법**
- 파일 선택 시 이미 "완료"/"실패" 상태인 파일은 **"대기"로 리셋**하고 자동 매칭을 재실행
- `OnMatchedPropertyChanged`, `OnIsAutoMatchedChanged`, `OnMatchConfidenceChanged`에서 `MatchStatusText` 변경 알림 추가

**변경된 파일**
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`

#### OCR 완료 후 담보물건 탭 데이터 갱신 문제 수정

**문제점**
- 탭 전환 시 `LoadRegistrySummaryAsync`가 fire-and-forget으로 호출되어 에러가 무시됨
- `ObservableCollection` 교체가 비-UI 스레드에서 일어날 수 있어 바인딩 업데이트 누락 가능

**해결 방법**
- 탭 전환 핸들러에 `RefreshRegistryDataAsync` 래퍼 추가 (에러 로깅 포함)
- `LoadRegistrySummaryAsync` 내 컬렉션 교체 및 속성 업데이트를 `Dispatcher.Invoke`로 UI 스레드 보장

**변경된 파일**
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`

#### DD 업로드 시 합계/요약 행 실패 카운트 개선

**문제점**
- 일부 은행(예: SHB) 데이터디스크 엑셀은 시트 하단에 합계/요약 행(키 컬럼 공란)이 포함됨
- 기존 로직은 키가 비어있으면 "실패"로 집계하여 실제 데이터 오류가 아닌데도 실패 건수 발생

**해결 방법**
- 키가 비어있는 행을 합계/요약/빈 행으로 판별하면 실패가 아닌 **스킵**으로 처리
- 디버그 로그에 실제 엑셀 행번호(`ExcelRow`) 함께 출력
- 완료 메시지에 `스킵 N건` 표시

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`
- `src/NPLogic.App/Services/DataDiskUploadService.cs`

### 2026-02-05

#### DD 업로드 중 `JWT expired (PGRST303)` 오류 대응

**문제점**
- DD 업로드(대량 Insert/Update) 도중 Supabase PostgREST가 `JWT expired`를 반환하며 저장이 연쇄 실패
- 내부적으로는 세션 만료 시점 계산에서 `ExpiresAt()`의 시간대(Local/UTC) 처리 차이로 인해
  만료를 제때 감지/갱신하지 못하는 케이스가 존재

**해결 방법**
- `SupabaseService.EnsureValidSessionAsync()`에서 `ExpiresAt()`의 `DateTime.Kind`를 고려해
  **UTC/Local 기준을 올바르게 선택**하도록 보정
- 장시간 작업 대비 **갱신 임계값을 10분**으로 상향하여 선제적으로 토큰을 Refresh

**변경된 파일**
- `src/NPLogic.Data/Services/SupabaseService.cs`

#### 대시보드 물건 누락(초기 페이지 스킵) 문제 해결

**문제점**
- 프로그램을 선택하면 서버 페이지네이션으로 **1페이지(1~50)**를 정상 로드함
- 그런데 **같은 순간** `SelectedProjectId` 변경이 자동 새로고침(`RefreshDataAsync`)을 다시 호출함
- 이 새로고침이 **구(legacy) 로딩 경로**(`LoadDashboardPropertiesAsync`)를 실행하면서
  방금 받은 **1페이지 데이터를 덮어씀**
- 결과: 화면에는 1페이지가 사라진 것처럼 보이고, 이후 페이지는 정상 정렬됨

**해결 방법**
- 프로그램 로딩 중에는 `SelectedProjectId` 변경으로 인한 **자동 새로고침을 일시 차단**
- 한 번의 로딩 경로만 실행되도록 만들어 **서버 페이지네이션 결과가 덮어쓰이지 않게 함**

### 2026-02-04

#### 등기부등본 OCR 스키마 개편 및 정제 파이프라인 도입

**배경**
- 담보물건 탭에 표시할 최종 표는 `reference/Auction-Certificate`의 산출물 스키마(`basic_info.csv`, `gapgu.csv`, `eulgu.csv`) 기반
- 물건(`property_id`)당 등기부 결과 세트가 여러 개 저장될 수 있어 세트(run) 단위가 필요

**완료된 작업**
- Supabase 신규 테이블 생성 + RLS 정책 적용
  - `registry_runs`: 물건별 등기부 결과 세트 + `deed_seq` 자동 부여
  - `registry_basic_info`: 세트당 1행 요약(11컬럼)
  - `registry_gapgu_rows`: 세트당 N행 (사용자 입력: `note_user_input`, `wage_claim_estimate_user_input`)
  - `registry_eulgu_rows`: 세트당 N행 (사용자 입력: `debtor_user_input`, `collateral_type_user_input`, `is_factory_mortgage_user_input`)
- 레거시 테이블 삭제: `registry_documents`, `registry_owners`, `registry_rights`
- OCR 응답에 `refined` 필드 추가 (`python/ocr_processor.py`, `python/server.py`)
- Supabase Edge Function `ocr-registry-save` 배포
- WPF 담보물건 탭 및 등기부등본 탭을 정제 스키마 기반으로 전환

**변경된 파일**
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`
- `src/NPLogic.App/Views/RegistryTab.xaml`
- `src/NPLogic.Data/Repositories/RegistryRepository.cs`
- `python/ocr_processor.py`
- `python/server.py`

#### 등기부등본 OCR 추출 결과 건수 표시 개선

**문제점**
- 매칭 모드에서 OCR 결과는 저장되지만, 미리보기 컬렉션이 채워지지 않아
  "OCR 추출 결과"에 소유자/갑구/을구 건수가 항상 0으로 표시됨

**해결 방법**
- 매칭 모드에서도 OCR 결과를 미리보기 컬렉션에 누적 파싱하도록 변경
- 결과 건수 로그를 추가해 추출 여부를 즉시 확인 가능

#### 담보물건 탭의 등기부 요약 정보 표시 개선

**문제점**
- OCR 저장으로 `registry_owners/registry_rights` 데이터는 쌓이지만
  `registry_documents`가 없으면 요약이 빈 문자열로 남아 화면이 비어 보임
- 탭 전환 시 요약 갱신이 없어 최신 OCR 데이터가 반영되지 않음

**해결 방법**
- 등기부 문서가 없어도 소유자/갑구/을구 데이터가 있으면 요약을 구성
- 담보물건 탭 진입 시 등기부 요약을 다시 로드하여 최신 상태 반영

**변경된 파일**
- `src/NPLogic.App/ViewModels/DashboardViewModel.cs`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
