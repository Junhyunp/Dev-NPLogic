# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-08

#### 등기부등본 이미지 뷰어 기능 추가

**구현 내용**
- `registry_runs` 테이블에 `summary_images_base64` (jsonb) 컬럼 추가
- Edge Function `ocr-registry-save` v12: OCR 시 주요 등기사항 요약 이미지를 DB에 저장
- 담보물건 탭 "등기부등본 정보" 패널 헤더에 "등기부등본" 버튼 추가
- 클릭 시 물건지(PDF) 선택 드롭다운 + 이미지 뷰어 패널 펼침/접기
- `PropertyDetailViewModel`: `ToggleRegistryImagePanelCommand`, `SelectedImageRun`, `RegistryDocumentImages`

**변경된 파일/서비스**
- Supabase Edge Function `ocr-registry-save` v12
- `src/NPLogic.Core/Models/RegistryRun.cs` (SummaryImagesBase64 프로퍼티)
- `src/NPLogic.Data/Repositories/RegistryRepository.cs`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`

#### 담보물건 탭 UI 개선

- 토지이용계획 버튼: 상단 버튼 바 → "물건 기본 정보" 패널 헤더 오른쪽으로 이동
- 건축물대장 버튼: 상단 버튼 바에서 제거 (등기부등본 버튼으로 대체하여 "등기부등본 정보" 헤더로 이동)
- 상단 버튼 바 제거

#### 갑구/을구 중복 행 병합

- 같은 접수정보 + 대상소유자인 행은 하나로 합치고 지번번호를 쉼표로 결합 (예: "01, 02, 03, 04")
- `reference/Auction-Certificate`의 `_dedup_with_remark` 로직 참고
- DB 원본은 유지, 표시 시점에만 ViewModel에서 병합

#### 비핵심 "전체" 화면 간헐적 빈 화면 수정

**문제점**
- 대시보드에서 물건 클릭 후 "비핵심" - "전체" 화면에 정보가 안 뜨는 경우가 간헐적으로 발생
- 로그에 `탭 로드 취소됨: Home` 확인 → RadioButton 프로그래밍 변경 시 이벤트가 이중 발생하여 첫 번째 로드가 취소됨

**해결 방법**
- `DashboardView`: `_suppressInnerTabChecked` 플래그 추가 → `SwitchToDetailMode` 시 이중 로드 방지
- `NonCoreView`: `_suppressFunctionTabChecked` 플래그 추가 → `ResetToHomeTabAsync` 시 이중 로드 방지

#### 탭 전환 시 이전 컨텐츠 잔상 수정

**문제점**
- NonCoreView 내부 탭 전환 시 이전 탭 화면이 남아있는 경우 발생 (창 최대화 시 정상 표시)

**해결 방법**
- `LoadFunctionContentAsync` 진입 시 `ContentArea.Content = null` 로 즉시 클리어

#### 등기부등본 탭 진입 시 PDF 파일 목록 초기화

- 등기부등본 탭으로 전환할 때마다 이전 OCR 완료된 PDF 파일 목록 자동 클리어
- `DashboardView.LoadTabViewAsync("registry")` 에서 `CancelAllOcrPdfFilesCommand` 호출

**변경된 파일**
- `src/NPLogic.App/Views/DashboardView.xaml.cs`
- `src/NPLogic.App/Views/NonCoreView.xaml.cs`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml.cs`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`

### 2026-02-07

#### 등기부등본 OCR 전체 파이프라인 개편

**Edge Function `ocr-registry-save` v11**
- **인증**: `verify_jwt: false` + `supabase.auth.getUser()` 수동 검증 (ES256/HS256 모두 지원)
- **파일명 전달**: .NET의 한글 파일명 인코딩 문제 해결 → `source_pdf_name` 별도 form-data 필드로 전달
- **덮어쓰기**: "물건+PDF파일명" 단위 DELETE → INSERT (같은 파일 재업로드 시 교체, 다른 파일은 유지)
- **deed_seq**: 트리거 auto-increment 대신 PDF 파일명에서 직접 추출 (R-0003-1-05 → deed_seq=5)
- **DD 주소 (물건지 DD)**: `registry_sheet_data` 테이블에서 지번별 개별 주소 조회 후 PDF 파일명 주소와 매칭
- **등기 주소 (물건지 등기부등본)**: PDF 파일명에서 주소 추출
- **지번번호**: deed_seq를 2자리 문자열(01, 02...)로 갑구/을구 행에 자동 채움

**WPF 앱 변경**
- basic_info: 단일 객체 → `ObservableCollection<RegistryBasicInfo>` (DataGrid N행)
- 갑구/을구: run 단위 → property 단위 합산 조회 (여러 PDF 결과 통합)
- 갑구/을구 DataGrid에 `지번번호` 컬럼 추가
- 등기부등본 탭: PDF 업로드 + OCR 처리 전용으로 단순화 (결과 조회 UI 제거)
- 결과 조회: 비핵심 → 담보물건 탭의 "등기부등본 정보" 패널에서만 확인

**PDF 재업로드 시 기존 완료 파일 매칭/재처리 문제 수정**
- 파일 선택 시 이미 "완료"/"실패" 상태인 파일은 "대기"로 리셋하고 자동 매칭을 재실행

**OCR 완료 후 담보물건 탭 데이터 갱신 문제 수정**
- `LoadRegistrySummaryAsync` 내 컬렉션 교체를 `Dispatcher.Invoke`로 UI 스레드 보장
- `NonCoreView` 캐시 시스템에서 "CollateralProperty" 탭 복귀 시 등기부 데이터 선택적 새로고침

#### DD 업로드 시 합계/요약 행 실패 카운트 개선

- 합계/요약/빈 행을 실패가 아닌 **스킵**으로 처리
- 디버그 로그에 실제 엑셀 행번호(`ExcelRow`) 함께 출력

### 2026-02-05

#### DD 업로드 중 `JWT expired (PGRST303)` 오류 대응

- `SupabaseService.EnsureValidSessionAsync()`에서 `DateTime.Kind` 고려한 UTC/Local 보정
- 갱신 임계값을 10분으로 상향

#### 대시보드 물건 누락(초기 페이지 스킵) 문제 해결

- `SelectedProjectId` 변경 시 자동 새로고침 일시 차단하여 서버 페이지네이션 결과 보호

### 2026-02-04

#### 등기부등본 OCR 스키마 개편 및 정제 파이프라인 도입

- Supabase 신규 테이블: `registry_runs`, `registry_basic_info`, `registry_gapgu_rows`, `registry_eulgu_rows`
- 레거시 테이블 삭제: `registry_documents`, `registry_owners`, `registry_rights`
- Python OCR 서버에 `refined` 필드 추가
- Supabase Edge Function `ocr-registry-save` 배포
- WPF 담보물건 탭 및 등기부등본 탭을 정제 스키마 기반으로 전환
