# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-11

#### 탭 이동/물건 선택 시 로딩 성능 최적화

- **N+1 문제 해결**: `LoadCollateralStatisticsAsync`에서 물건마다 개별 DB 호출 → `GetByPropertyIdsAsync` 배치 조회 (50개 물건 5초 → 0.1초)
- **RightAnalysisRepository에 배치 조회 추가**: `GetByPropertyIdsAsync(List<Guid>)` — Filter("property_id", Operator.In) 사용
- **PropertyDetailViewModel.InitializeAsync 병렬화**: `LoadJibunAppraisalsAsync`, `LoadMachineryAppraisalsAsync`를 순차 실행에서 `Task.WhenAll` 배치로 이동 (10개 병렬 작업)
- **SeniorRightsViewModel.InitializeAsync 병렬화**: LoadProperties/LoadRights/LoadRightAnalysis 3개 순차 → `Task.WhenAll` 병렬
- **RegistryTabViewModel.LoadAllDataForPropertyAsync 병렬화**: BasicInfo/Gapgu/Eulgu 3개 순차 → `Task.WhenAll` 병렬

**변경된 파일**
- `src/NPLogic.Data/Repositories/RightAnalysisRepository.cs`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`

#### DD 업로드 성능 최적화 (배치 INSERT)

- **배치 INSERT 메서드 추가**: 7개 Repository에 `CreateBatchAsync` 메서드 추가
  - `BorrowerRepository`, `LoanRepository`, `PropertyRepository`, `RightAnalysisRepository`
  - `BorrowerRestructuringRepository`, `RegistrySheetDataRepository`, `CreditGuaranteeRepository`
  - 청크 단위 배치 INSERT + 실패 시 개별 삽입 폴백 (InterimRepository 패턴)
- **ProgramManagementViewModel 배치 처리 리팩터링**:
  - BorrowerGeneral 시트 우선 처리 → 차주 캐시(`borrowerIdCache`) 구축
  - 나머지 시트에서 차주 조회 시 캐시 사용 (DB 호출 제거)
  - 각 시트별 행을 메모리에서 매핑 후 배치 INSERT로 DB 호출 최소화
  - Property + RightAnalysis 배치 INSERT로 물건당 4회 → 청크당 2회로 감소
- **헬퍼 메서드 추출**: `ExtractBorrowerNumber()`, `BuildRightAnalysis()`
- **예상 효과**: DB 호출 ~2,260회 → ~30-40회, 업로드 시간 40-60초 → 8-15초

**변경된 파일**
- `src/NPLogic.Data/Repositories/BorrowerRepository.cs`
- `src/NPLogic.Data/Repositories/LoanRepository.cs`
- `src/NPLogic.Data/Repositories/PropertyRepository.cs`
- `src/NPLogic.Data/Repositories/RightAnalysisRepository.cs`
- `src/NPLogic.Data/Repositories/BorrowerRestructuringRepository.cs`
- `src/NPLogic.Data/Repositories/RegistrySheetDataRepository.cs`
- `src/NPLogic.Data/Repositories/CreditGuaranteeRepository.cs`
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`

#### registry_rights 테이블 C# 코드 동기화

- **RegistryRight 모델 업데이트**: Supabase에 재생성한 `registry_rights` 테이블 스키마에 맞게 동기화
  - `RegistryDocumentId`, `Debtor`, `CollateralType`, `IsFactoryMortgage`, `IsWageClaimEstimate` 필드 제거
  - `Section`("갑구"/"을구"), `TargetOwner`(대상소유자), `JibeonNumber`(지번번호) 필드 추가
- **Postgrest 매핑 업데이트**: `RegistryRightTable` 모델 및 매핑 메서드 동기화
- **ViewModel 필터 변경**: `RightType == "gap"/"eul"` → `Section == "갑구"/"을구"` (SeniorRightsViewModel, RegistryTabViewModel)

#### 선순위 탭 경매사건 테이블 레이아웃 변경

- **통합 8×5 테이블**: 기존 선행/후행 분리 Grid → 통합 테이블로 재구성
  - 왼쪽: 경매신청기관, 경매개시일자, 경매사건번호, 배당요구종기일 (선행/후행 각각)
  - 오른쪽: 경매개시여부, 관할법원, 최초법사가, 최초경매기일, 최종경매회차, 최종경매결과, 낙찰금액, 차후최저입찰금액
- **읽기 전용 표시**: TextBox → TextBlock 변경 (DD 데이터 표시 용도)
- **담보물건 탭 스타일 통일**: TableHeaderCell/TableDataCell 스타일 적용
- **데이터 바인딩 연결**: SelectedProperty의 경매 관련 Property 속성에 바인딩
- **AuctionStarted 표시**: bool 값을 DataTrigger로 "경매개시"/"경매미개시" 한국어 텍스트 변환

#### NonCoreView 선순위 탭 데이터 로딩 수정

- **LoadFunctionContentAsync**: SeniorRights 케이스 추가 — InitializeAsync + SelectedProperty 설정
- **RefreshTabDataAsync**: SeniorRights 케이스 추가 — 물건 전환 시 SelectedProperty 갱신

#### 물건번호 생성 로직 수정

- **ProgramManagementViewModel**: DD 업로드 시 물건번호를 카운터 기반 대신 엑셀 "물건 일련번호" 값 활용
  - 엑셀 일련번호가 있으면 `{차주번호}_{일련번호}` 형식으로 생성 (예: R-0035_2)
  - 없으면 기존 카운터 방식으로 폴백
  - R-0035처럼 비연속 일련번호(2번만 존재)에서 잘못된 번호(R-0035_1) 생성되던 버그 수정

**변경된 파일**
- `src/NPLogic.Core/Models/RegistryRight.cs`
- `src/NPLogic.Data/Repositories/RegistryRepository.cs`
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.App/Views/SeniorRightsView.xaml`
- `src/NPLogic.App/Views/NonCoreView.xaml.cs`
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`

### 2026-02-09

#### DataGrid UI 개선 및 버그 수정

- **흰색 폰트 수정**: 지번별 감정평가·기계기구 감정가 DataGrid에서 셀 선택/편집 시 글자가 흰색으로 바뀌는 문제 해결
  - CellStyle에 `Foreground="Black"` + `IsSelected`/`IsEditing` 트리거 추가
  - RowStyle에 `Foreground="Black"` 추가
- **합계 실시간 갱신**: 셀 값 편집 시 합계가 즉시 갱신되도록 행별 PropertyChanged 구독 추가
  - `SubscribeJibunRowChanged()`, `SubscribeMachineryRowChanged()` 메서드 추가
  - 행 생성/로드 6곳에서 구독 설정
- **평가액 합계 동적 위치**: 기계기구 감정가 합계 행의 평가액 합계가 동적 컬럼 추가/삭제에 따라 평가액 컬럼 위치에 자동 정렬
  - 고정 Grid → code-behind `RebuildMachineryTotalsRow()` 동적 생성으로 변경
- **물건 전환 시 이전 데이터 잔류 버그 수정 (탭 캐시 경로)**
  - `InitializeAsync()` 시작부에 await 전 동기적 Clear 추가 (LoadProperty 경로에 이어 추가)
  - NonCoreView 탭 캐시에서 RefreshTabDataAsync → InitializeAsync 호출 시에도 이전 데이터 즉시 클리어

#### 기계기구 감정가 패널 개선

- **공장저당n호 컬럼 타입 변경**: 체크박스(bool) → 텍스트(string) 입력으로 변경
  - `MortgageCheckItem` → `MortgageValueItem` (bool IsChecked → string Value)
  - code-behind: `DataGridCheckBoxColumn` → `DataGridTextColumn`
  - DB jsonb: `{"공장저당1호": true}` → `{"공장저당1호": "텍스트값"}`
- **합계 행 추가**: 지번별 감정평가(감정가 합), 기계기구 감정가(감정가 합 + 평가액 합)
  - DataGrid 하단 합계 Border 추가
  - computed properties: `JibunAppraisalTotalValue`, `MachineryAppraisalTotalValue`, `MachineryEvaluationTotalValue`
  - 행 추가/삭제/저장/로드 시 합계 자동 갱신
- **물건 전환 시 이전 데이터 잔류 버그 수정**: `LoadProperty()`에서 비동기 로드 전 즉시 동기 초기화 (Clear + HasTable=false) 추가

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml` (합계 행 XAML)
- `src/NPLogic.App/Views/CollateralPropertyView.xaml.cs` (동적 컬럼 CheckBox→Text)
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs` (MortgageValueItem, 합계 속성, 물건 전환 초기화)

#### 기계기구 감정가 패널 추가

- 기계기구 감정가(`MachineryAppraisalValue > 0`)가 있는 물건에서만 표시되는 패널 신규 추가
- (+) 버튼으로 테이블 생성, 행 추가/삭제(✕), 저장 버튼으로 DB 저장
- 고정 컬럼: 번호, 기계기구명, 제조사, 제작일자, 수량, 단가, 감정가(자동계산), 담보여부, 평가율, 평가액
- **동적 컬럼**: 공장저당n호 - 열 추가/삭제/이름 수정 가능
- 공장저당 열 관리 UI: 인라인 텍스트 편집 + 삭제(✕) + 열 추가 버튼
- 동적 컬럼은 code-behind에서 DataGrid 컬럼 동적 생성 (MortgageColumnNames CollectionChanged 구독)
- 감정가 자동 계산: 수량 × 단가

**DB 테이블 (Supabase migration)**
- `property_machinery_appraisals` 테이블 신규 생성 (properties FK, RLS 활성화)
- 고정 컬럼: `item_number`, `machinery_name`, `manufacturer`, `manufacture_date`, `quantity`, `unit_price`, `appraisal_value`, `is_collateral`, `evaluation_rate`, `evaluation_value`
- 동적 컬럼 저장: `factory_mortgages` (jsonb), `mortgage_column_names` (jsonb)

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml` (패널 XAML)
- `src/NPLogic.App/Views/CollateralPropertyView.xaml.cs` (동적 컬럼 code-behind)
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs` (MachineryAppraisalRow, MortgageValueItem, CRUD 커맨드)
- `src/NPLogic.Data/Repositories/PropertyRepository.cs` (MachineryAppraisalTable, Get/SaveMachineryAppraisalsAsync)

#### 보조패널 UI 개선 및 저장 방식 변경

- 지번별 감정평가 DataGrid: 모든 컬럼 값 수평·수직 중앙정렬 (ElementStyle + EditingElementStyle)
- KB시세 패널: 값 수평·수직 중앙정렬
- 분양가 패널: 값 수평·수직 중앙정렬 (Right → Center)
- KB시세/분양가 패널: 자동저장(On*Changed → HasUnsavedChanges) 제거 → 각 패널별 "저장" 버튼으로 수동 저장 방식으로 변경 (DB 통신 빈도 감소)
- `SaveKbPanelCommand`, `SaveSalePanelCommand` 신규 추가

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`

#### 지번별 감정평가 패널 추가

- 담보물건 탭 감정평가정보 패널 아래에 "지번별 감정평가" 패널 신규 추가
- 초기에는 빈 상태, 헤더의 (+) 버튼으로 테이블 생성
- 컬럼: 지번일련번호, 구분, 지번주소지, 면적(평), 평당감정가, 감정가 (모두 유저 입력)
- 행 추가/삭제(✕ 버튼) 기능, 저장 버튼으로 DB 저장
- 물건 로드 시 기존 데이터 자동 로드

**DB 테이블 (Supabase migration)**
- `property_jibun_appraisals` 테이블 신규 생성 (properties FK, RLS 활성화)
- 컬럼 prefix `ja_`: `ja_seq`, `ja_category`, `ja_address`, `ja_area_pyeong`, `ja_price_per_pyeong`, `ja_appraisal_value`
- 모든 컬럼에 COMMENT 설명 추가

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs` (JibunAppraisalRow 모델, CRUD 커맨드)
- `src/NPLogic.Data/Repositories/PropertyRepository.cs` (JibunAppraisalTable, GetJibunAppraisalsAsync, SaveJibunAppraisalsAsync)

#### 경(공)매일정 탭 디자인 개선

- 담보물건 탭의 디자인 패턴(PrimaryBrush 헤더, CornerRadius=8, 일관된 테두리/간격)을 경매일정·공매일정에 통일 적용
- 하드코딩 컬러(`#E8E8E8`, `#FFFDE7`, `#E3F2FD`, `#ABABAB`)를 리소스 브러시로 교체
- 각 섹션(평가결과, 경매일정, 공매일정, 공매비용, 회차별 공매일정, 타채권자 배분)에 PrimaryBrush 헤더 바 추가

**변경된 파일**
- `src/NPLogic.App/Views/AuctionPublicSaleView.xaml`
- `src/NPLogic.App/Views/AuctionScheduleContentControl.xaml`
- `src/NPLogic.App/Views/PublicSaleScheduleContentControl.xaml`

#### 감정평가정보 패널: KB시세 하위 패널 추가 (아파트)

- 물건종류가 "아파트"인 경우 감정평가정보 패널 내에 KB시세 테이블 표시
- 컬럼: KB시세 데이터(KB부동산 좌표 URL 링크), KB시세(DD kb_price 또는 감정평가구분 KB시세 시 감정평가액합계), 분양면적(유저 입력)
- KB부동산 URL: `kbland.kr/c/15307?xy={lat},{lng},16` 좌표 기반 생성
- 분양면적은 DB `properties.kb_supply_area` 컬럼에 저장 (유저 입력 → 탭 전환 시 자동저장)

**DB 컬럼 (Supabase migration)**
- `kb_supply_area` (numeric) - KB시세 패널 분양면적

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.Core/Models/Property.cs`
- `src/NPLogic.Data/Repositories/PropertyRepository.cs`

#### 감정평가정보 패널: 분양가 하위 패널 추가 (상가/아파트형공장)

- 물건종류가 정확히 "상가" 또는 "아파트형공장"인 경우 감정평가정보 패널 내에 분양가 테이블 표시
- 헤더 구조: 분양면적(RowSpan=2) | 분양가(ColSpan=4) → 토지, 건물, 합계, 부가세
- 모든 필드는 유저 입력 (ManualInputBgBrush 배경), 탭 전환 시 자동저장
- 현재 DB 물건종류: 상가(49건), 아파트형공장(26건) 대상. 추후 근린시설, 아파트형공장(상가) 등 추가 검토 가능

**DB 컬럼 (Supabase migration)**
- `sale_supply_area` (numeric) - 분양가 패널 분양면적
- `sale_price_land` (numeric) - 토지 분양가
- `sale_price_building` (numeric) - 건물 분양가
- `sale_price_total` (numeric) - 합계 분양가
- `sale_price_vat` (numeric) - 부가세

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.Core/Models/Property.cs`
- `src/NPLogic.Data/Repositories/PropertyRepository.cs`

#### NonCoreView 탭 캐시 버그 수정

- **버그**: 물건 변경 후 다른 탭(예: 담보물건)으로 이동 시 이전 물건 데이터가 그대로 표시됨
- **원인**: `_lastPropertyId`가 전역 1개로 관리되어, Home 탭 로드 시 갱신된 후 다른 탭에서는 `propertyChanged=false`로 판단
- **수정**: `_lastPropertyId` → `_tabLastPropertyId` (탭별 Dictionary)로 변경하여 각 탭이 독립적으로 물건 변경을 감지

**변경된 파일**
- `src/NPLogic.App/Views/NonCoreView.xaml.cs`

#### IsApartment 조건 수정

- 오피스텔을 KB시세 패널 표시 대상에서 제외 (아파트만 대상)
- 3개 로드 경로 모두 통일

**변경된 파일**
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`

### 2026-02-08

#### 지적도 패널: 필지 경계 폴리곤 표시 기능

**구현 내용**
- 지적도 패널을 기존 지적편집도(USE_DISTRICT) 오버레이에서 **위성도 + 해당 필지 경계선 폴리곤** 방식으로 변경
- VWORLD Data API (`/req/data`)로 PNU 기반 필지 경계 폴리곤(LP_PA_CBND_BUBUN) 조회
- 카카오 HYBRID 위성도 위에 빨간 경계선 폴리곤 오버레이 (strokeColor:#FF0000, fillOpacity:0.15)
- 폴리곤 영역 기준 자동 지도 범위 조정 (`map.setBounds`)
- 폴리곤 조회 실패 시 위성도+마커 fallback

**VWORLD API 키 이슈 해결**
- "웹사이트" 유형 키는 도메인 검증으로 WPF 데스크톱 앱에서 `INCORRECT_KEY` 에러 발생
- "APP(모바일, 솔루션 등)" 유형 키로 교체하여 해결
- DB `app_config.vworld_api_key` 업데이트

**변경된 파일**
- `src/NPLogic.App/Views/CollateralPropertyView.xaml.cs` (LoadCadastralBoundaryMapAsync, GenerateKakaoSatelliteWithBoundaryHtml 추가)
- `src/NPLogic.App/Services/VworldService.cs` (GetParcelBoundaryAsync - Data API 방식 추가)

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
