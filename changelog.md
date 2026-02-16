# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-16

#### MCI 산식 구현 + 사이드바 무한스크롤 + Loan 탭 버튼 정리

- **MCI 컬럼 축소 (23→18열)**: 채권잔액, 인수대상원금-MCI가입잔액, (미수이자+연체이자), 유효담보가의 20%, 유효담보가+이자(Max 한도) 5개 중간계산 컬럼 제거
- **MCI 데이터 연동**: credit_guarantees 테이블에서 MCI 보증서 데이터(MCI최초가입금액, MCI가입잔액, 채권번호) 로드, loans 테이블에서 인수대상원금/정상이자율 로드
- **MCI 산식 구현**: 일수(예상배당일-최종이수일), 유효담보가(인수대상원금-MCI가입잔액), 유효담보가의 이자, 이자한도(20%), 유효담보가 배당액, MCI 정상이자(유효담보가×일수×정상이자율/365), 배당으로 충당되지 않은 MCI 잔액, 청구가능금액, 배당후 손실액(예상배당금-Loan Cap), MCI 청구액
- **사이드바 무한스크롤**: PropertySideListBox에 ScrollViewer.ScrollChangedEvent 핸들러 추가, 90% 스크롤 도달 시 LoadMorePropertiesAsync() 호출
- **Loan 탭 우측 상단 버튼 제거**: 전체 재계산, Excel, 저장 버튼 3개 제거 (섹션별 저장 + 상위 탭 Excel로 대체)

**변경된 파일**
- `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs` — CreditGuaranteeRepository 주입, UpdateMciDataAsync, MCI 전체 산식 구현
- `src/NPLogic.App/Views/Loan/Sections/MciSection.xaml` — 23→18열 축소
- `src/NPLogic.App/Views/DashboardView.xaml.cs` — 사이드바 무한스크롤 핸들러 추가
- `src/NPLogic.App/Views/Loan/LoanSheetView.xaml` — 우측 상단 버튼 3개 제거

### 2026-02-15

#### Loan 탭 UI 정리: MCI 섹션 리뉴얼 + 이미지 영역 이동 + 데이터 셀 배경색 제거

- **MCI 보증 섹션 디자인 통일**: 6개 분리 Grid → 1안/2안 각각 단일 Grid(2행×23열)로 통합, 서브헤더 배너(#E8EAF6, 네이비 텍스트) 추가, 헤더 셀 BlueGray100 통일
- **MCI 섹션 정리**: 서브헤더 빨간 문구("N안 배당일에 MCI 회수 반영") 삭제, 산식 설명 행(DD SheetD 등) 삭제, Grid 3행→2행 축소
- **Loan Cap 1 이미지 영역 이동**: DataGrid 우측 DockPanel → DataGrid 아래 2열 Grid (선순위 탭 경매사건검색 패턴)
- **데이터 셀 배경색 전면 제거**: 채권정보(채권액합계 연두), Loan Cap 1(예상배당일 노랑, 연체이자 파랑, Loan Cap 초록), MCI 보증(예상배당일 노랑, 일수/유효담보가배당액/배당후 손실액 파랑, MCI 청구액 초록) — 모든 데이터 셀을 흰색 배경으로 통일

**변경된 파일**
- `src/NPLogic.App/Views/Loan/Sections/MciSection.xaml` — 서브헤더 + 단일 Grid 통합 + 산식 행 삭제 + 헤더/데이터 색상 정리
- `src/NPLogic.App/Views/Loan/Sections/LoanCapSection.xaml` — 이미지 영역 아래 이동 + 특수 배경색 스타일 제거
- `src/NPLogic.App/Views/Loan/Sections/BondInfoSection.xaml` — 채권액합계 인라인 스타일을 AmountHideOnSummary로 교체

#### 채권정보 저장 버튼 + DB 컬럼 추가 + 이미지 영속화

- **채권정보 저장 버튼**: BondInfoSection 우측 하단에 "저장" 버튼 추가 (SeniorRightsView 패턴, MaterialDesignRaisedButton)
- **Loan Cap 1 저장 버튼**: LoanCapSection 우측 하단에도 동일 저장 버튼 추가
- **DB 컬럼 5개 추가 (loans)**: `has_auction_application`, `has_subrogation_registration_cost`, `has_collateral_priority_1/2/3` — 체크박스 값 저장 가능
- **이미지 DB 저장**: "보증서 등" / "기타항목 및 전송" 이미지를 차주(borrowers) 테이블에 Base64로 저장/로드 (`guarantee_image_base64`, `other_items_image_base64`)
- **이미지 영역 디자인 통일**: 당사자내역 패턴 적용 (SecondaryButton 좌측 상단, GridSplitter 이미지 크기 조절)
- **이미지 영역 빈 공간 제거**: Grid → DockPanel 레이아웃 변경으로 LC2 열과 보증서 등 사이 빈 공간 제거

**변경된 파일**
- `src/NPLogic.App/Views/Loan/Sections/BondInfoSection.xaml` — 저장 버튼 추가
- `src/NPLogic.App/Views/Loan/Sections/LoanCapSection.xaml` — DockPanel 레이아웃, 당사자내역 스타일 이미지 패널, 저장 버튼
- `src/NPLogic.App/Views/Loan/Sheets/BasicSheet.xaml` — 이미지 영역을 LoanCapSection 내부로 이동
- `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs` — SaveAsync 이미지 저장, LoadLoansAsync 이미지 로드, ConvertImageToBase64/LoadImageFromBase64 유틸
- `src/NPLogic.Core/Models/Borrower.cs` — GuaranteeImageBase64, OtherItemsImageBase64 프로퍼티
- `src/NPLogic.Data/Repositories/BorrowerRepository.cs` — BorrowerTable + 매핑 2개 필드 추가
- `src/NPLogic.Data/Repositories/LoanRepository.cs` — LoanTable + 매핑 5개 필드 추가

#### 채권정보 체크박스 컬럼 동작 정의 + 보증서 연계

- **유효보증서여부(O) / MCI보증**: DD 신용보증서(Sheet D) 데이터와 연계하여 자동 판정, IsReadOnly 설정
- **기대위변제(P)**: 최초대출원금 ≠ 대출원금잔액 시 주황배경(#FFF3E0) 표시 (합계 행 제외, MultiDataTrigger)
- **체크박스 가운데 정렬**: ElementStyle + EditingElementStyle 모두에 HorizontalAlignment="Center" 적용
- **"보증서종류" 매칭 버그 수정**: SHB DD 컬럼명 "보증서종류"가 코드의 "보증종류" 매칭에 걸리지 않아 guarantee_type이 전부 null이던 문제 수정
- **MCI 판정 로직 수정**: 보증기관 기준 → guarantee_type(보증서종류) == "MCI" 기준으로 변경
- **UpdateLoanGuaranteeFlagsAsync 신규**: DD 전체 시트 처리 후 loans의 has_valid_guarantee/has_mci_guarantee 자동 갱신

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs` — 보증서종류 매칭 추가, UpdateLoanGuaranteeFlagsAsync, MCI 판정 로직 수정
- `src/NPLogic.App/Views/Loan/Sections/BondInfoSection.xaml` — 체크박스 정렬, 읽기전용 설정, 주황배경 MultiDataTrigger
- `src/NPLogic.Core/Models/Loan.cs` — HasDifferentPrincipal 계산 프로퍼티 추가

#### Loan Cap 1 섹션 구현 + 연체이자 실시간 계산

- **LoanCapSection 디자인 통일**: BondInfoSection과 동일한 스타일로 전면 리뉴얼 (네이비 헤더, BlueGray100 헤더 셀, CornerRadius 8, DataGrid 내부 합계 행)
- **필터링**: 유효보증서=N, 기대위변제=N인 대출만 표시 (`LoanCap1LoansWithSummary`)
- **컬럼 배경색**: 예상배당일(노랑), 연체이자(파랑), Loan Cap(초록), 전체 균등 너비(Width=*)
- **연체이자 자동 계산**: 최종이수일 null 시 기준일(CDate)을 fallback으로 사용하여 연체이자 계산
- **실시간 갱신**: 기준일/1안 배당일/2안 배당일 DatePicker 변경 시 자동 재계산 (OnCDateChanged, OnScenario1DateChanged, OnScenario2DateChanged)
- **이자회수(U) 컬럼 너비 수정**: Width="*" → Width="85"로 변경하여 컬럼 헤더가 잘리지 않도록 수정

**변경된 파일**
- `src/NPLogic.App/Views/Loan/Sections/LoanCapSection.xaml` — BondInfoSection 스타일로 전면 리뉴얼
- `src/NPLogic.App/Views/Loan/Sheets/BasicSheet.xaml` — LoanCapSection 독립 행 배치
- `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs` — LoanCap1LoansWithSummary, 날짜 변경 자동 재계산
- `src/NPLogic.Core/Models/Loan.cs` — CalculateOverdueInterest fallbackStartDate 파라미터 추가
- `src/NPLogic.App/Views/Loan/Sections/BondInfoSection.xaml` — 이자회수(U) 컬럼 너비 수정

#### DD 임포트 누락 필드 수정 (채권일반정보)

- **3개 코드 경로 필드 누락 수정**: DD 업로드 시 실제 사용되는 `ProgramManagementViewModel.MapRowToLoan`에 연체이자율, 채권액합계, 가지급금, 환산대출잔액, 미상환원금, 계좌일련번호 핸들러 추가. `DataDiskUploadService.MapRowToLoan`에 `loan_principal_balance` 추출 추가. `DataUploadViewModel`에 동일 필드 case 추가.
- **"최초 대출원금" 공백 매칭 버그 수정**: SHB DD Excel 헤더 "최초 대출원금"에 공백이 포함되어 매핑 실패하던 문제 수정 — `ProgramManagementViewModel`에 공백 포함 조건 추가, `SheetMappingConfig`에 "최초 대출원금" 매핑 규칙 추가
- **대출원금잔액 fallback**: `LoanPrincipalBalance`가 null일 때 `ConvertedLoanBalance` → `UnpaidPrincipal` 순서로 fallback (3개 코드 경로 모두)
- **이자율 fallback**: 정상이자율만 있으면 연체이자율 = 정상 + 3%, 채권액합계 미입력 시 잔액+가지급금+미수이자로 자동 계산

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs` — MapRowToLoan 필드 핸들러 대폭 추가
- `src/NPLogic.App/Services/DataDiskUploadService.cs` — loan_principal_balance 추출 및 fallback
- `src/NPLogic.App/ViewModels/DataUploadViewModel.cs` — 누락 필드 case 추가 및 fallback
- `src/NPLogic.App/Services/SheetMappingConfig.cs` — "최초 대출원금" 공백 변형 매핑 규칙 추가

#### 채권정보 테이블 합계 행 개선

- **합계 행을 DataGrid 내부로 이동**: 기존 DataGrid 아래 별도 패널 → DataGrid 마지막 행으로 변경하여 컬럼 너비 조정 시 자동 동기화
- **합계 행 표시 컬럼 제한**: 계좌일련번호(A)에 "합계" 텍스트, 최초대출원금(F)·대출원금잔액(G)에만 합산값 표시, 나머지 컬럼은 숨김 처리
- **합계 행 스타일**: BlueGray100 배경, Bold, 비활성(편집 불가). DataTrigger 기반 숨김 스타일 4종 정의

**변경된 파일**
- `src/NPLogic.App/ViewModels/LoanSheetViewModel.cs` — `LoansWithSummary` 프로퍼티 추가
- `src/NPLogic.App/Views/Loan/Sections/BondInfoSection.xaml` — 합계 행 DataGrid 내부 이동, 숨김 스타일 적용
- `src/NPLogic.Core/Models/Loan.cs` — `IsSummaryRow` 프로퍼티 추가

### 2026-02-14

#### 선순위 항목 산정표 자동 판단 기능 구현

- **자동 판단 버튼 추가**: 선순위 항목 산정표에 "자동 판단" 버튼 추가 — 클릭 시 `RightAnalysisRuleEngine`(60개 케이스)을 호출하여 반영금액 7개 + 상세추정 근거 7개를 조건에 맞게 자동 채움
- **상세추정 근거 편집 가능**: 산정표의 상세추정 근거 칼럼을 TextBlock→TextBox로 변경하여 자동 판단 후 유저가 직접 수정 가능
- **RuleEngine 상가 케이스 보완**: 원청 매트릭스 대비 누락된 상가 주소 분기 추가 (C12~C13 주소불일치/일치, C16~C17 주소불일치/일치) — 총 43개 케이스로 원청 매트릭스 완전 일치

**변경된 파일**
- `src/NPLogic.Core/Services/RightAnalysisRuleEngine.cs` — 상가 C12~C17 주소 분기 추가
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs` — AutoJudge() Command 추가
- `src/NPLogic.App/Views/SeniorRightsView.xaml` — 자동 판단 버튼 추가, 상세추정 근거 칼럼 편집 가능

#### 선순위 탭 UI 개선 및 비즈니스 로직 문서화

- **DataGrid 셀 편집 수정**: 주택임대차 DataGrid에서 일부 셀이 클릭 시 편집 모드에 진입하지 않는 문제 수정 — 3개 DataGrid(주택임대차, 상가임대차, 임금채권) 모두에 `PreviewMouseLeftButtonDown` 핸들러 추가하여 단일 클릭으로 즉시 편집 가능하도록 처리
- **불필요 UI 섹션 삭제**: 선순위 요약(배당 시뮬레이션) 및 선순위 참고사항 섹션 삭제 (~195줄) — 임금채권이 최하단 섹션
- **원청 선순위 근거 문구 전문 문서화**: `docs/5. BUSINESS_LOGIC.md` 1.5절에 원청 제공 60개 케이스별 상세추정 근거 문구 전문 기록 (주택류 R1~R18, 토지 L1~L4, 상가/공장 C1~C17, 임금채권 W1~W13, 당해세/조세 T1~T8)

**변경된 파일**
- `src/NPLogic.App/Views/SeniorRightsView.xaml` — 선순위 요약/참고사항 섹션 삭제
- `src/NPLogic.App/Views/SeniorRightsView.xaml.cs` — 3개 DataGrid PreviewMouseLeftButtonDown 핸들러 추가
- `docs/5. BUSINESS_LOGIC.md` — 케이스별 상세추정 근거 원문 추가, 업데이트일 갱신

### 2026-02-13

#### 주택임대차/상가임대차/임금채권 DB 영속화 및 저장 버튼 구현

- **DB 테이블 신규 생성**: `lease_items` (주택/상가 임대차, lease_type으로 구분), `wage_claim_items` (임금채권) 테이블 생성 (인덱스, RLS, 한국어 COMMENT 포함)
- **Repository 신규 생성**: `LeaseItemRepository`, `WageClaimItemRepository` — property_id 기반 조회/저장 (DELETE→INSERT 패턴)
- **저장 버튼 3개 추가**: 주택임대차, 상가임대차, 임금채권 각 DataGrid 아래에 기존 섹션과 동일한 디자인의 저장 버튼 추가
- **DB 로드/리셋 연동**: 물건 선택 시 DB에서 3개 컬렉션 자동 로드, 물건 전환 시 컬렉션 초기화
- **임금채권 합계 실시간 갱신**: `WageClaimItem`에 `INotifyPropertyChanged` 구현 — 임금/퇴직금/기타 입력 시 배당요구액 합계, 3개월임금/3년퇴직금/체당금 입력 시 반영금액 합계 자동 갱신

#### DataGrid 행 간격 및 스타일 수정

- **행 간격 문제 해결**: CellStyle의 `TextBlock.VerticalAlignment` attached property가 DataGrid 내부 레이아웃을 깨뜨리는 문제 수정 — CollateralPropertyView 패턴과 동일하게 각 컬럼에 ElementStyle/EditingElementStyle 직접 지정 방식으로 전환
- **DataGrid 불필요 속성 제거**: 3개 DataGrid에서 `CanUserResizeRows`, `CanUserResizeColumns`, `FontSize`, `HorizontalAlignment`, `ColumnHeaderHeight` 등 CollateralPropertyView에 없는 속성 제거

**변경된 파일**
- `src/NPLogic.App/Views/SeniorRightsView.xaml` — 저장 버튼 추가, DataGrid 스타일 수정
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs` — 저장/로드/리셋 로직 추가
- `src/NPLogic.App/App.xaml.cs` — DI 등록
- `src/NPLogic.Core/Models/WageClaimItem.cs` — INotifyPropertyChanged 구현
- `src/NPLogic.Data/Repositories/LeaseItemRepository.cs` (신규)
- `src/NPLogic.Data/Repositories/WageClaimItemRepository.cs` (신규)

### 2026-02-12 (7)

#### 선순위 탭 후속 구성 추가 (열람자료 2x4 + 신규 3개 테이블)

- **선순위 항목 산정표 하단 2x4 표 추가**: `전입세대열람`, `상가임대차열람`, `권리분석`, `임금자료` 구성으로 추가
- **이미지 입력 UX 통일**: 전입세대열람/상가임대차열람/임금자료 셀에 이미지 붙여넣기 + `GridSplitter` 높이 조절 적용
- **권리분석 셀 비사용 표시**: 권리분석 셀은 대각선 표시로 현재 미사용 상태를 명확히 표현
- **열람 이미지 영속화 연동**: `right_analysis`에 전입세대열람/상가임대차열람/권리분석/임금자료 이미지 저장·재로드 경로 연결
- **기존 중복 섹션 정리**: 하단의 기존 `열람 자료`, `선순위 구분` 섹션 제거

#### 선순위 탭 신규 입력 섹션 확장

- **신규 섹션 3종 추가**: `주택임대차`, `상가임대차`, `임금채권` 섹션을 + 버튼 생성형으로 추가
- **행 추가/삭제 동작 추가**: 세 섹션 모두 행 추가/행 삭제 지원
- **임금채권 2단 헤더 유지**: `배당요구액(체불내역)` / `반영금액` 그룹 헤더 + 하위 컬럼 구조 적용
- **초기값 표시 정리**: 금액 필드를 nullable로 전환하여 테이블 생성 시 `0` 대신 빈 값으로 시작

#### 표 스타일/동작 보정

- **행 버튼 스타일 통일**: 담보물건/선순위 탭 행 추가·행 삭제 버튼을 `등기부등본 정보` 스타일로 통일하고 위치를 각 표 우하단 기준으로 정리
- **입력 셀 색상 통일**: 선순위/담보물건 탭 표의 사용자 입력 셀 노란 배경 제거
- **정렬/가독성 보정**: 데이터 셀 폰트 크기, 가로/세로 중앙정렬, 구분선/셀 높이 보정
- **불필요 합계 텍스트 제거**: 주택임대차/상가임대차/임금채권 좌하단 `합계: 0원` 표시 삭제
- **행 추가 렌더링 보정**: `MaxHeight` 제거와 행 스타일 정리로 행 추가 시 표가 분리되어 보이지 않도록 수정
- **삭제 안내 메시지 처리 개선**: `삭제할 행을 선택하세요`를 하단 고정 에러 배너 대신 토스트 경고로 처리하고 잔여 에러 메시지 클리어

**변경된 파일**
- `src/NPLogic.App/Views/SeniorRightsView.xaml`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.Core/Models/LeaseItem.cs`
- `src/NPLogic.Core/Models/WageClaimItem.cs`
- `src/NPLogic.Core/Models/RightAnalysis.cs`
- `src/NPLogic.Data/Repositories/RightAnalysisRepository.cs`
- `src/NPLogic.App/Views/CollateralPropertyView.xaml`

### 2026-02-12 (6)

#### 선순위 DD 데이터 업로드→DB→화면 연동 보강

- **DD 매핑 확장**: `선순위근저당권(DD)` 및 `유치권 신고금액(DD)` 매핑 키를 업로드 파이프라인에 추가
- **저장 필드 확장**: `right_analysis` 저장 시 `senior_mortgage_dd`, `lien_dd` 포함 및 `SeniorTotalDd` 계산에 반영
- **업로드 경로 일관화**: `DataDiskUploadService`, `ProgramManagementViewModel`, `DataUploadViewModel` 경로 모두 동일 필드 저장 로직 적용

#### 물건 상세 선순위 탭 바인딩 경로 수정

- **DataContext 연결 수정**: `PropertyDetailView`의 선순위 탭이 `PropertyDetailViewModel` 전체가 아닌 `SeniorRightsViewModel`을 직접 바인딩하도록 수정
- **탭 ViewModel 초기화 보강**: `PropertyDetailViewModel`에서 `SeniorRightsViewModel` 생성 후 물건 로드 시 `SelectedProperty`를 전달하도록 수정

#### 선순위 항목 산정표 데이터 표시/입력 기능 완성

- **표 바인딩 완성**: `선순위 항목 산정표`의 DD/반영금액/근거 셀을 실제 필드(`SeniorMortgageDd`, `LienDd`, `SmallDepositDd`, `LeaseDepositDd`, `WageClaimDd`, `CurrentTaxDd`, `SeniorTaxClaimDd`)에 연결
- **반영금액 입력 가능화**: `평가자 반영 금액` 컬럼을 `TextBox`로 변경하고 `N0` 숫자 포맷/우측 정렬 적용
- **합계 자동 계산 표시**: `SeniorRightsDdTotal`, `SeniorRightsReflectedTotal` 계산 프로퍼티 추가 및 변경 알림 연동
- **합계 행 텍스트 정리**: 합계 행 우측의 `자동 합계` 문구 제거

#### 전입/임차 패널 툴팁 개선

- **건물기준시가 안내 툴팁 추가**: 헤더에 `(?)` 표시를 추가하고 마우스 오버 시 `건물감정가액의 70%로 계산됩니다.` 안내 표시

**변경된 파일**
- `src/NPLogic.App/Services/SheetMappingConfig.cs`
- `src/NPLogic.App/Services/DataDiskUploadService.cs`
- `src/NPLogic.App/ViewModels/DataUploadViewModel.cs`
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`
- `src/NPLogic.App/Views/PropertyDetailView.xaml`
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`
- `src/NPLogic.App/Views/SeniorRightsView.xaml`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`

### 2026-02-12 (5)

#### 선순위 탭 레이아웃 확장 - 선순위 항목 산정표 추가

- **새 패널 추가**: 전입/임차 현황 및 등기부 정보 패널 하단에 `선순위 항목 산정표` 섹션 추가
- **9×4 표 레이아웃 구성**: 헤더 1행(`선순위 구분`, `DD`, `평가자 반영 금액`, `상세추정 근거`) + 항목 8행
- **항목 행 구성**: 선순위근저당권, 유치권 신고금액, 선순위소액보증금, 선순위임차보증금, 선순위 임금채권, 당해세, 선순위 조세채권, 합계
- **열 너비 조정 지원**: 컬럼 사이 `GridSplitter` 추가로 마우스 드래그 너비 조정 가능
- **데이터 연동 전 준비 단계**: 셀 바인딩 없이 우선 표 구조/디자인만 배치

**변경된 파일**
- `src/NPLogic.App/Views/SeniorRightsView.xaml`

### 2026-02-12 (4)

#### 담보물건 탭 등기부 중복 병합 재적용 안정화

- **레이스 조건 수정**: `LoadRegistrySummaryAsync`에서 병합된 갑구/을구를 세팅한 뒤 `SelectedRegistryRun` 변경 콜백이 미병합 데이터로 덮어쓰던 문제 수정
- **콜백 억제 플래그 추가**: `SelectedRegistryRun`을 내부적으로 갱신할 때 `OnSelectedRegistryRunChanged` 자동 재로드를 막도록 `_suppressSelectedRegistryRunChanged` 적용
- **보조 로드 경로도 동일 병합 적용**: `LoadSelectedRegistryRunAsync` 경로에서도 `MergeGapguDuplicates`/`MergeEulguDuplicates`를 적용해 일관된 표시 보장

**변경된 파일**
- `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs`

### 2026-02-12 (3)

#### 선순위 탭 우측 등기부 정보 자동 연동

- **자동 바인딩 구현**: 전입/임차 현황 우측 3열(`접수정보`, `근저당권자`, `채권최고액`)을 `registry_eulgu_rows` 기반으로 표시
- **근저당 우선 표시**: `등기목적`에 `근저당`이 포함된 을구 행을 우선 사용, 없으면 을구 전체를 fallback으로 사용
- **중복 병합 적용**: 같은 `접수정보 + 대상소유자` 행은 1건으로 병합하여 표시 (지번 분할 OCR 중복 대응)
- **7행 고정 UI**: 우측 표는 7행 고정으로 유지하고 데이터가 부족한 행은 빈칸으로 표시

**변경된 파일**
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.App/Views/SeniorRightsView.xaml`

### 2026-02-12 (2)

#### 선순위 탭 - 전입/임차 현황 데이터 셀 바인딩 구현

- **Row 1 바인딩**: 임차인 CheckBox(`HasTenant`), 전입인 TextBox(`TenantName`), 전입일 DatePicker(`TenantMoveInDate`)
- **Row 3 바인딩**: 소유주 전입(`OwnerRegistered`), 경매열람자료(`HasAuctionDocs`), 전입세대/상가임대차 RadioButton(`HasTenantRegistry`/`HasCommercialLease`), 임차인 배당요구신청(`TenantClaimSubmitted`), 주택공시가격 TextBlock(`HousingOfficialPrice`)
- **Row 5 바인딩**: 임금채권(`HasWageClaim`), 임금채권 추정가압류 TextBox(`WageClaimEstimatedSeizure`), 임금채권 배당요구신청(`WageClaimSubmitted`), 당해세 교부청구(`HasTaxClaim`), 선순위조세 교부청구(`HasSeniorTaxClaim`)
- **Row 7 바인딩**: 공시지가 TextBlock(`OfficialLandPrice`, 자동산출), 건물기준시가 TextBox(`BuildingStandardPrice`, 수정가능)
- **저장 버튼 추가**: 전입/임차 현황 섹션 하단 오른쪽에 저장 버튼(`SaveTenantInfoCommand`)

#### 주택공시가격/공시지가/건물기준시가 자동 산출

- **주택공시가격**: VWORLD NED Data API로 PNU 기반 자동 조회 (공동주택/개별주택/개별공시지가 자동 분기)
- **PNU 자동 조회**: PNU 없는 물건은 주소로 VWORLD Search API 조회 → DB 저장(`PropertyRepository.UpdatePnuAsync`)
- **아파트 동/호 매칭**: 공동주택 API 응답에서 주소의 동/호 파싱하여 정확한 세대 가격 매칭
- **공시지가**: 개별공시지가(원/㎡) × 토지면적 자동 산출, 주택이 아닌 경우 별도 "토지" 타입으로 API 호출
- **건물기준시가**: 건물감정가액 × 70% 기본값, 사용자 수정 가능
- **VworldService API 키 로드**: Edge Function(`get-map-config`) 경유 비동기 로드(`EnsureApiKeyLoadedAsync`)

#### DB 마이그레이션

- `wage_claim_estimated_seizure`: `bool` → `numeric` (금액 입력용)
- `official_land_price`: 새 컬럼 추가 (공시지가)
- `building_standard_price`: 새 컬럼 추가 (건물기준시가)

#### 기타 수정

- **RightAnalysisRuleEngine**: `WageClaimEstimatedSeizure` bool→decimal 변경에 따른 비교 로직 수정 (`> 0`)
- **RadioButton 상호 배타**: `OnHasTenantRegistryChanged`/`OnHasCommercialLeaseChanged` partial 메서드 추가
- **폰트 크기 일치**: 건물기준시가/임금채권 추정가압류 TextBox에 `FontSize=FontSizeBody` 명시

**변경된 파일**
- `src/NPLogic.App/App.xaml.cs`
- `src/NPLogic.App/Services/VworldService.cs`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.App/Views/SeniorRightsView.xaml`
- `src/NPLogic.Core/Models/RightAnalysis.cs`
- `src/NPLogic.Core/Services/RightAnalysisRuleEngine.cs`
- `src/NPLogic.Data/Repositories/PropertyRepository.cs`
- `src/NPLogic.Data/Repositories/RightAnalysisRepository.cs`

### 2026-02-12

#### 선순위 탭 - 전입/임차 현황 외부 데이터 바인딩

- **차주구분 표시**: `borrowers.borrower_type`에서 읽기전용 TextBlock으로 표시 (DD 데이터)
- **주소지 일치여부 표시**: `registry_basic_info.is_address_matched`에서 읽기전용 CheckBox로 표시 (OCR 데이터)
- **(?) 툴팁 추가**: "물건지, 소유주 주소지 일치여부" 헤더에 (?) 아이콘, 마우스 호버 시 "등기부등본 OCR 업로드 필요" 안내 (InitialShowDelay=200ms)

#### 선순위 탭 - 전입/임차 현황 테이블 레이아웃 변경

- **8×8 테이블 재설계**: 전입/임차 현황 및 등기부 정보 영역을 8열×8행 Grid 테이블로 재구성
  - 왼쪽 5열: 주소지 일치여부, 차주구분, 임차인, 전입인, 전입일, 소유주 전입, 현황조사서 제출, 임대차 유무 등
  - 오른쪽 3열: 접수정보, 근저당권자, 채권최고액

#### NonCoreView 선순위 탭 DataContext 수정

- **DataContext 미설정 버그 수정**: `CreateAndCacheTabViewAsync`에서 SeniorRightsView 생성 시 DataContext를 명시적으로 설정하도록 변경 (기존: `content.DataContext is SeniorRightsViewModel` 체크가 항상 false)
- **Properties 1000개 제한 우회**: `GetAllAsync()` 기본 제한(1000행)으로 선택된 물건이 목록에 없는 경우 `GetByIdAsync`로 개별 조회 후 추가
- **SeniorRightsView DI 팩토리 정리**: App.xaml.cs에서 불필요한 DataContext 팩토리 제거 (NonCoreView가 직접 ViewModel 생성/설정)
- **Loaded 이벤트 핸들러 제거**: SeniorRightsView.xaml.cs의 `SeniorRightsView_Loaded` 제거 (이중 InitializeAsync 호출 방지)

#### BorrowerRepository 중복 행 에러 수정

- **`.Single()` → `.Limit(1).Get()`**: `GetByBorrowerNumberAsync`에서 같은 `borrower_number`에 중복 행이 있을 때 Supabase가 "multiple rows returned" 에러를 던지는 문제 수정

#### OCR 상태 조회 인프라 추가

- **RegistryRepository**: `GetAllOcrPropertyIdsAsync()` — OCR 데이터가 있는 모든 property_id를 HashSet으로 반환
- **DashboardViewModel**: `RegistryRepository` DI 추가, `ApplyOcrStatusAsync`로 물건별 OCR 상태 설정
- **Property 모델**: `HasOcrData` 런타임 전용 프로퍼티 추가 (DB 비저장)
- **NonCoreViewModel**: `RegistryRepository` DI 추가, `LoadOcrCountsAsync`로 차주별 OCR 카운트 로드, `BorrowerListItem.OcrPropertyCount/OcrDisplay/HasOcr` 추가

**변경된 파일**
- `src/NPLogic.App/App.xaml.cs`
- `src/NPLogic.App/Converters/VisibilityConverters.cs`
- `src/NPLogic.App/ViewModels/DashboardViewModel.cs`
- `src/NPLogic.App/ViewModels/NonCoreViewModel.cs`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.App/Views/NonCoreView.xaml`
- `src/NPLogic.App/Views/NonCoreView.xaml.cs`
- `src/NPLogic.App/Views/SeniorRightsView.xaml`
- `src/NPLogic.App/Views/SeniorRightsView.xaml.cs`
- `src/NPLogic.Core/Models/Property.cs`
- `src/NPLogic.Data/Repositories/BorrowerRepository.cs`
- `src/NPLogic.Data/Repositories/RegistryRepository.cs`

### 2026-02-11

#### 선순위 탭 - 경매사건 테이블 UI 개선 및 데이터 저장

- **컬럼 너비 드래그 조정**: 경매사건/선순위 구분/전입임차 테이블에 GridSplitter 추가, 컬럼 너비 마우스 드래그로 조절 가능
- **"당사자내역" 3열 추가**: 경매사건 테이블 오른쪽에 당사자내역 이미지 붙여넣기 영역 + 현황조사서/감정평가서 메모 + 배당요구종기일경과 체크박스
- **이미지 크기 조정**: 당사자내역 이미지 영역에 GridSplitter(ResizeDirection=Rows)로 높이 드래그 조정
- **배당요구종기일경과 자동 판단**: DD의 `precedent_claim_deadline` (선행, 없으면 후행) 날짜가 오늘 이전이면 자동 체크
- **경매사건 정보 DB 저장**: `right_analysis` 테이블에 `survey_report_note`, `appraisal_report_note`, `party_details_image_base64` 컬럼 추가, "저장" 버튼으로 DB 영구 저장/로드
- **대법원 경매 이미지 테이블 추가**: 경매사건검색/기일내역검색/문건송달내역 3열 이미지 붙여넣기 테이블 + DB 저장 (`court_case_search_image`, `court_date_search_image`, `court_document_delivery_image`)
- **"대법원 경매사건 검색" 버튼**: 대법원 경매사건 검색 사이트 바로가기 (courtauction.go.kr)

#### 지번별 감정평가/기계기구 감정가 조회 오류 수정

- **Postgrest `.Where()` → `.Filter()` 변경**: `PropertyRepository`에서 `.Where(x => x.PropertyId == propertyId.ToString())` 호출 시 Postgrest 표현식 파서가 `ToString()` 미지원 → `NotImplementedException` 발생. `.Filter("property_id", Operator.Equals, ...)` 방식으로 4곳 수정

**변경된 파일**
- `src/NPLogic.App/Views/SeniorRightsView.xaml`
- `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs`
- `src/NPLogic.Core/Models/RightAnalysis.cs`
- `src/NPLogic.Data/Repositories/RightAnalysisRepository.cs`
- `src/NPLogic.Data/Repositories/PropertyRepository.cs`

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
