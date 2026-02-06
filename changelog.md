# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-04

#### 등기부등본 OCR 요약 표 저장 스키마 개편 (진행중)

**목표**
- "주요 등기사항 요약"의 3개 표를 DB에 **표 형태 그대로** 저장/표시
- 물건별로 **최신 1회 결과만 유지(덮어쓰기)**

**진행 상황**
- [완료] Supabase에 신규 테이블 3개 추가 + RLS 정책 적용
  - `registry_gapgu_ownership_shares`
  - `registry_gapgu_rights_summary`
  - `registry_eulgu_rights_summary`
- [완료] 앱 저장/조회 경로를 신규 테이블로 전환(덮어쓰기)
  - OCR 저장: 기존 `registry_owners/registry_rights` 대신 신규 3개 테이블에 저장
  - 담보물건/권리분석시트 조회: 신규 3개 테이블에서 직접 조회
- [완료] 레거시 테이블 삭제 적용
  - 삭제: `registry_documents`, `registry_owners`, `registry_rights`
  - 유지: `registry_sheet_data` (엑셀 Sheet C-2)

#### 등기부 정제 산출물(basic_info/gapgu/eulgu) 스키마 도입 (1단계 완료)

**배경**
- 담보물건 탭에 표시할 최종 표는 “요약 원문표”가 아니라 `reference/Auction-Certificate`의 산출물 스키마(`basic_info.csv`, `gapgu.csv`, `eulgu.csv`) 기반
- 물건(`property_id`)당 등기부 결과 세트가 **여러 개** 저장될 수 있어 세트(run) 단위가 필요

**1단계(완료)**
- Supabase에 아래 신규 테이블/제약/RLS 정책을 추가
  - `registry_runs`: 물건별 등기부 결과 세트(여러 개) + `deed_seq` 자동 부여 + `jibeon_id` 자동 생성
  - `registry_basic_info`: 세트당 1행 요약(README의 11컬럼)
  - `registry_gapgu_rows`: 세트당 N행(사용자 입력: `note_user_input`, `wage_claim_estimate_user_input`)
  - `registry_eulgu_rows`: 세트당 N행(사용자 입력: `debtor_user_input`, `collateral_type_user_input`, `is_factory_mortgage_user_input`) ※ 을구 “비고” 컬럼 없음

**남은 작업**
- 2단계: EC2 OCR 서버 응답을 정제 스키마(basic_info/gapgu/eulgu) JSON으로 확장
- (완료) 3단계: Supabase Edge Function(프록시/권한체크/저장) 설계 및 구현
- (완료) 4단계: WPF 담보물건 탭 “등기부등본 정보” 패널을 정제 표 기반으로 교체 + 사용자 입력 저장 연동

**2단계 진행(코드 반영 완료, 배포 필요)**
- OCR 응답에 `refined` 필드를 추가하여 `basic_info/gapgu/eulgu` 정제 테이블을 JSON으로 제공
  - `python/ocr_processor.py`: `build_refined_registry_tables()` 추가
  - `python/server.py`: 응답 모델에 `refined`, `refined_version` 포함

**3단계 진행(완료)**
- Supabase Edge Function `ocr-registry-save` 배포
  - 역할: 앱 요청(인증 필요) → EC2 OCR 서버 호출 → Supabase에서 DD 관련 값 조회 → `registry_runs/basic_info/gapgu/eulgu` 저장
  - `deed_seq`, `jibeon_id`는 DB 트리거로 자동 부여

**4단계 진행(완료)**
- 담보물건 탭 “등기부등본 정보” 패널을 `registry_runs/basic_info/gapgu/eulgu` 기반으로 전환
  - `registry_runs`에서 물건별 세트 목록 로드 후, 기본으로 최신 세트 선택
  - 세트 선택(ComboBox) 시 해당 run의 `basic_info/gapgu/eulgu`를 다시 로드해 표로 표시
- 사용자 입력 저장 연동
  - 갑구: `note_user_input`, `wage_claim_estimate_user_input` 편집 가능 + 저장 버튼으로 DB 업데이트
  - 을구: `debtor_user_input`, `collateral_type_user_input`, `is_factory_mortgage_user_input` 편집 가능 + 저장 버튼으로 DB 업데이트
- 상위 메뉴 “등기부등본” 탭(RegistryTab)도 정제 스키마 기반으로 통일
  - “저장된 정제 결과” 섹션 추가: 물건 선택 → run 선택 → `basic_info/gapgu/eulgu` 표 표시
  - “OCR 처리 시작” 시 Supabase Edge Function `ocr-registry-save` 호출로 전환하여 `registry_runs/basic_info/gapgu/eulgu`에 저장 (중복 OCR 제거)
  - 일괄 업로드(매칭 모드)에서 **PDF별 물건 선택 콤보박스**를 OCR 전부터 표시(Edge Function 저장을 위해 `property_id` 사전 지정 필요)
  - 완료된 파일은 기본적으로 재처리 대상에서 제외(매칭 변경 시 “대기”로 되돌아가 재처리 가능)
  - 사용자 입력 저장 버튼으로 갑구/을구 user_input 업데이트
  - Edge Function 응답에 `summary_images`(최대 3페이지) + `refined`를 포함하도록 확장하여, UI에서 이미지/정제표를 즉시 확인 가능

**변경된 파일**
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`
- `src/NPLogic.App/Views/RegistryTab.xaml`
- `src/NPLogic.Data/Repositories/RegistryRepository.cs`

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

#### DD 업로드 시 합계/요약 행 실패 카운트 개선

**문제점**
- 일부 은행(예: SHB) 데이터디스크 엑셀은 시트 하단에 **합계/요약 행**(키 컬럼 공란)이 포함됨
- 기존 로직은 키(차주번호/보증서번호 등)가 비어있으면 “실패”로 집계하여, 실제 데이터 오류가 아닌데도 실패 건수가 발생
- 로그의 `(행 N)`은 엑셀 행번호가 아니라 **처리 순번**이라 사용자 입장에서 원인 파악이 어려움

**해결 방법**
- 키가 비어있는 행을 **합계/요약/빈 행**으로 판별하면 실패가 아닌 **스킵**으로 처리
- 디버그 로그에 처리 순번과 함께 **실제 엑셀 행번호(`ExcelRow`)** 를 함께 출력
- 완료 메시지에 `스킵 N건`을 함께 표시

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`
- `src/NPLogic.App/Services/DataDiskUploadService.cs`

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

#### 담보물건 탭 등기부등본 패널 레이아웃/표시 개선

**변경 내용**
- 3열(표제부/갑구/을구) 요약 텍스트 방식 → 3행(소유지분현황/갑구/을구) **표(DataGrid)** 방식으로 변경
- OCR 저장된 `registry_owners`, `registry_rights` 데이터를 그대로 표시하도록 바인딩 추가

**추가**
- OCR 원본 응답(JSON)을 임시 파일로 덤프하고, 출력창에 head/tail 일부를 함께 로깅

**변경된 파일**
- `src/NPLogic.App/ViewModels/DashboardViewModel.cs`