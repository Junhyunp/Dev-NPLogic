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
- 3단계: Supabase Edge Function(프록시/권한체크/저장) 설계 및 구현
- 4단계: WPF 담보물건 탭 “등기부등본 정보” 패널을 정제 표 기반으로 교체 + 사용자 입력 저장 연동

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