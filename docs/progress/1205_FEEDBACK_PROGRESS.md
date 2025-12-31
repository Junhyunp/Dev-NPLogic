# 1205 미팅 피드백 개선 진행 상황

> **생성일**: 2024-12-16  
> **마지막 업데이트**: 2024-12-31  
> **전체 진행률**: 35/35 (100%)

---

## 📊 Phase 요약

| Phase | 설명 | 상태 | 진행률 |
|-------|------|------|--------|
| Phase 1 | UI/디자인 변경 | ✅ 완료 | 4/4 |
| Phase 2 | 대시보드 구조 변경 | ✅ 완료 | 6/6 |
| Phase 3 | 비핵심 내부 구조 변경 | ✅ 완료 | 4/4 |
| Phase 4 | 네비게이션 개선 | ✅ 완료 | 5/5 |
| Phase 5 | 기능 제거 및 업로드 개선 | ✅ 완료 | 4/4 |
| Phase 6 | 로직/DB 변경 | ✅ 완료 | 5/5 |
| Phase 7 | 후순위 작업 | ✅ 완료 | 4/4 |
| 기타 | 데이터 관련 | ⬜ 대기 | 0/3 |

---

## Phase 1: UI/디자인 변경 ✅

> **담당 파일**: `Colors.xaml`, `LoginWindow.xaml`, `MainWindow.xaml`

### 피드백 항목

- [x] **1.1** 로그인 화면 톤 변경: 현재 너무 진함 → 기존 남색 톤으로 변경
  - 파일: `src/NPLogic.UI/Styles/Colors.xaml`
  - 파일: `src/NPLogic.App/Views/LoginWindow.xaml`
  - ✅ Primary 색상 `#4A90E2` → `#1E3A5F` (남색)로 변경

- [x] **1.2** Header UI 수정
  - 파일: `src/NPLogic.App/MainWindow.xaml`
  - 세부사항:
    - [x] 엠필로지 로고 워딩 사용 (white-logo.png)
    - [x] 헤더 남색 배경 (`#1A2F4A`)
    - [x] 알림/설정/나가기 아이콘 흰색 톤

- [x] **1.3** 화면 테두리를 비슷한 톤으로 둘러싸기 (엑셀 스타일처럼)
  - 파일: `src/NPLogic.App/MainWindow.xaml`
  - ✅ 남색 테두리 3px 적용

- [x] **1.4** 첫 번째 화면 ↔ 두 번째 화면 톤 통일 (같은 프로그램처럼 연결성 있게)
  - ✅ 로그인/메인 화면 모두 동일한 남색 테두리 적용

### Phase 1 완료 조건
- 로그인 화면과 메인 화면의 색상 톤이 통일됨
- Header가 남색 배경 + 흰색 아이콘으로 변경됨
- 엑셀 스타일 테두리 적용됨

---

## Phase 2: 대시보드 구조 변경 ✅

> **담당 파일**: `DashboardView.xaml`, `DashboardViewModel.cs`  
> **난이도**: 높음 (핵심 작업)

### 피드백 항목

- [x] **2.1** "메뉴창" 텍스트 제거
  - 파일: `src/NPLogic.App/Views/DashboardView.xaml`
  - ✅ 기존 코드에 해당 텍스트 없음 (이미 제거됨)

- [x] **2.2** 대시보드 구성: "진행 중인 프로그램" | "프로그램명" 두 가지 카테고리로 구분
  - 파일: `src/NPLogic.App/Views/DashboardView.xaml`
  - ✅ 좌측: 진행 중인 프로그램 목록 (ProgramSummaries)
  - ✅ 우측: 선택된 프로그램의 상세 데이터 (DashboardProperties)

- [x] **2.3** 두 영역 간 width 크기 조절 가능 (마우스 드래그로)
  - ✅ `GridSplitter` 컨트롤 추가 완료

- [x] **2.4** 두 영역 모두 닫기/열기 가능하도록
  - ✅ 토글 버튼 구현 (LeftPanelToggle, LeftPanelOpenButton)

- [x] **2.5** 상단 기능 메뉴(홈/비핵심/등기부/권리분석/기초데이터/마감) → 프로그램명 내부로 위치 변경
  - ✅ 우측 영역 내 RadioButton 탭으로 구현

- [x] **2.6** 대시보드 컬럼별 진행율 퍼센테이지로 표시
  - ✅ ColumnProgressInfo 모델 추가
  - ✅ "약정서 45%, 보증서 60%" 형태로 표시

### Phase 2 완료 조건
- ✅ 2영역 레이아웃 구현 (진행 중인 프로그램 | 프로그램명)
- ✅ GridSplitter로 영역 크기 조절 가능
- ✅ 각 영역 열기/닫기 가능
- ✅ 진행률 퍼센테이지 표시

---

## Phase 3: 비핵심 내부 구조 변경 ✅

> **담당 파일**: `NonCoreView.xaml`, `NonCoreViewModel.cs`

### 피드백 항목

- [x] **3.1** 비핵심 내부 탭 구성 변경
  - 새로운 탭 구성:
    1. 차주 개요
    2. 론
    3. 담보 총괄
    4. 담보 물건
    5. 선순위 평가
    6. 경공매
    7. 회생개요
    8. 현금 흐름 집계
    9. NPB
  - ✅ NonCoreView.xaml에 9개 RadioButton 탭으로 구현
  - ✅ NonCoreTabStyle 스타일 추가 (Controls.xaml)

- [x] **3.2** 물건 번호 탭처럼 나열 (PDF 여러 개 띄워놓은 것처럼)
  - ✅ PropertyTabItem 모델로 물건 탭 관리
  - ✅ PDF 스타일 탭 UI 구현 (상단 가로 스크롤)
  - ✅ 탭 선택/닫기 기능

- [x] **3.3** 툴박스는 옆(사이드)으로 배치
  - ✅ 우측 사이드 패널로 이동 (Width="300")
  - ✅ 열기/닫기 토글 기능
  - ✅ 법원 정보, 빠른 계산, 참조 링크 섹션

- [x] **3.4** 선순위만, 평가만 등 기능별로 먼저 쭉 작업할 수 있어야 함
  - ✅ "기능별 일괄작업" 드롭다운 버튼 추가
  - ✅ 선순위만/평가만/경공매만/전체 일괄작업 메뉴
  - ✅ BatchWorkType으로 일괄 작업 모드 관리

### Phase 3 완료 조건
- ✅ 비핵심 탭이 9개로 재구성됨
- ✅ 물건 번호가 탭으로 나열됨
- ✅ 툴박스가 사이드에 배치됨

---

## Phase 4: 네비게이션 개선 ✅

> **담당 파일**: 각종 View 및 ViewModel

### 피드백 항목

- [x] **4.1** 진행 중인 프로그램 클릭 → 오른쪽 프로그램명에 해당 프로그램 데이터 출력
  - ✅ ProgramListBox_SelectionChanged 이벤트로 구현됨

- [x] **4.2** 프로그램명(대시보드)의 차주번호 클릭 → 해당 차주번호의 '비핵심' 대시보드로 이동
  - ✅ PropertyNumber_Click 이벤트로 구현됨

- [x] **4.3** 페이지/탭 이동: 버튼 또는 단축키로 다음 페이지(탭) 이동
  - 단축키 구현:
    - [x] `Shift+Tab`: 이전 탭
    - [x] `Tab`: 다음 탭
    - [x] `Alt+방향키`: 페이지/물건/프로그램 이동
    - [x] `Enter`: 선택된 물건 비핵심 화면으로 이동
    - [x] `Ctrl+D`: 대시보드로 돌아가기
    - [x] `Ctrl+R`: 새로고침
  - ✅ DashboardView_PreviewKeyDown, NonCoreView_PreviewKeyDown 추가

- [x] **4.4** 비핵심 대시보드 ↔ 프로그램명 대시보드 간 상하좌우 이동 자유롭게
  - ✅ Alt+방향키로 프로그램/물건/탭 간 이동 가능

- [x] **4.5** 롤업 기능
  - [x] 롤업 버튼 클릭 → 전체 화면(비핵심 전체)으로 이어짐
  - [x] 각 탭에서 수정 시 전체 화면에 즉각 반영 (5초 자동 새로고침)
  - [x] 롤업 창은 수정 중에도 닫히면 안 됨 (닫기 시 확인 메시지)
  - ✅ RollupWindow.xaml/cs 생성

### Phase 4 완료 조건
- ✅ 클릭으로 데이터 연동됨
- ✅ 단축키 동작함
- ✅ 롤업 기능 구현됨

---

## Phase 5: 기능 제거 및 업로드 개선 ✅

> **담당 파일**: `DataUploadView.xaml`, 관련 View 파일들

### 피드백 항목

- [x] **5.1** 물건 목록 기능 필요 없음
  - ✅ MainWindow.xaml: PropertyListButton Visibility="Collapsed" 설정

- [x] **5.2** 통제 기능 필요 없음
  - ✅ 해당 기능 없음 (확인 완료)

- [x] **5.3** 기존 데이터 업로드 화면 삭제
  - ✅ MainWindow.xaml: DataUploadButton Visibility="Collapsed" 설정

- [x] **5.4** 비핵심 기초데이터 화면에서 데이터 디스크 업로드
  - ✅ BasicDataTab.xaml: 데이터 업로드 섹션 추가
  - ✅ PropertyDetailViewModel.cs: UploadDataFileCommand, DownloadTemplateCommand 추가
  - ✅ 마지막 업로드 일자 표시 (LastDataUploadDate)

### Phase 5 완료 조건
- ✅ 불필요한 기능이 제거됨 (물건 목록, 데이터 업로드 버튼 숨김)
- ✅ 데이터 업로드가 비핵심 기초데이터 화면에서 가능함

---

## Phase 6: 로직/DB 변경 ✅

> **담당 파일**: DB 스키마, ViewModel, Model 파일들  
> **주의**: DB 마이그레이션 필요할 수 있음

### 피드백 항목 - 권한별 화면

- [x] **6.1** 관리자/유안코: 전체 풀 다 보임
  - ✅ DashboardViewModel에서 Admin 권한 전체 데이터 조회

- [x] **6.2** 회계법인 PM: 담당 전체 보임
  - ✅ ProgramUserRepository로 PM 담당 프로그램 ID 조회
  - ✅ DashboardViewModel에서 PM 권한 필터링 로직 추가

- [x] **6.3** 평가자: 자기가 assign된 것만 보임
  - ✅ DashboardViewModel에서 Evaluator AssignedTo 필터링 (기존 로직 유지)

### 피드백 항목 - 설계 파일 업데이트 (버전 7.1)

- [x] **6.4** 물건에 OPB 컬럼 추가
  - ✅ Property.cs에 Opb 필드 추가
  - ✅ PropertyRepository에 컬럼 매핑 추가
  - ✅ DB 마이그레이션 스크립트 생성 (docs/database/migration_phase6.sql)

- [x] **6.5** 자본소득률 → 상가/아파트형공장 상권 데이터 추가
  - ✅ Property.cs에 HasCommercialDistrictData 필드 추가
  - ✅ PropertyRepository에 컬럼 매핑 추가
  - ✅ 상가/아파트형공장 유형 물건에 대한 체크박스

### Phase 6 완료 조건
- ✅ 권한별 데이터 필터링 동작함
- ✅ OPB 컬럼 추가됨
- ✅ 상권 데이터 체크박스 추가됨

---

## Phase 7: 후순위 작업 ✅

> **담당**: 시간 여유 있을 때 진행

### 피드백 항목

- [x] **7.1** 소상공인 상권 데이터 지도 연동 검토
  - ✅ 물건 위치 자동 표시 (카카오맵 연동)
  - ✅ 주변 상권 정보 표시 (업종별 점포 현황, 점포 목록)
  - ✅ 반경 설정 기능 (200m/500m/1km/2km)
  - ✅ Excel 내보내기 기능
  - **필요 조건**: data.go.kr 소상공인 상권정보 API 키 발급 후 appsettings.json에 설정

- [x] **7.2** 인터링, 상계회수 항목 추가 (경매 일정 맨 밑에)
  - ✅ AuctionSchedule.cs: InterimPrincipalOffset, InterimPrincipalRecovery, InterimInterestOffset, InterimInterestRecovery 필드 추가
  - ✅ AuctionScheduleRepository.cs: 테이블 클래스 및 매퍼에 컬럼 추가
  - ✅ AuctionScheduleView.xaml: 편집 패널 하단에 인터링/상계회수 섹션 추가
  - ✅ AuctionScheduleViewModel.cs: 편집 필드 및 저장 로직 추가
  - ✅ migration_phase7.sql: DB 마이그레이션 스크립트 생성

- [x] **7.3** 계좌과 대출 과목에서 C 하나 삭제
  - ✅ LoanDetailView.xaml: 대출종류(C) 필드 제거
  - ✅ 컬럼 레이블 재정렬 (A: 계좌일련번호, B: 대출과목, C: 계좌번호, D: 최초대출일...)

- [x] **7.4** 주소 앞뒤 공란 제거 체크 기능
  - ✅ PropertyRepository.cs: TrimAddressFields() 헬퍼 메서드 추가
  - ✅ CreateAsync, UpdateAsync에서 저장 전 주소 필드 자동 Trim 처리

### Phase 7 완료 조건
- ✅ 상권 데이터 지도 연동 구현 완료
- ✅ 경매 일정에 인터링/상계회수 항목 추가
- ✅ 기타 소소한 수정 완료

---

## 기타: 데이터 관련 ⬜

> **담당**: 클라이언트로부터 데이터 수신 필요

- [ ] **기타.1** 경매 데이터 추출 완료되면 전달 예정
  - 클라이언트 전달 대기

- [ ] **기타.2** 일부 주소(금화 주차장 등) 좌표 변환 불가
  - 크리티컬하지 않음, 나중에 처리

- [ ] **기타.3** 주소 앞뒤 공란 제거 체크 필요

---

## 📅 다음 일정

- [ ] 다음 주 화요일 온라인 미팅
- [ ] 다다음 주 방문 (일정 재확인 필요)

---

## 📝 작업 로그

### 2024-12-31 (추가 UI 개선 피드백 반영)
- **폰트 크기 통일 (대시보드 기준)**
  - Typography.xaml: FontSizeBody 14→12, FontSizeH1~H4 크기 축소
  - NonCoreTabStyle: FontSize 13→12, Padding 16,12→12,8
  - InnerTabButton: FontSize 13→12, Padding 16,8→12,6

- **버튼 크기 축소**
  - Buttons.xaml: PrimaryButton, SecondaryButton, DangerButton
    - MinHeight 36→28, MinWidth 80→60, Padding 20,10→12,6
  - IconButton: Width/Height 36→28

- **물건 상세 화면 탭 구조 변경**
  - PropertyDetailView.xaml: 6탭→8탭 재구성
    - 이전: 홈/기초데이터/등기부/비핵심/권리분석/마감
    - 변경: 전체/차주개요/Loan/담보물건/선순위/평가/경(공)매/현금흐름

- **새로고침 버튼 개선**
  - DashboardView: 새로고침/롤업/Excel 버튼에 아이콘+ToolTip 추가
  - PropertyDetailView: 저장/새로고침 버튼에 아이콘+ToolTip 추가
  - ToolTip에 단축키 정보 포함 (Ctrl+R, Ctrl+S)

### 2024-12-31 (추가 피드백 구현)
- **인쇄/Excel 내보내기 개선**
  - NonCoreView.xaml: 인쇄, Excel 버튼 추가 (전체/차주별 선택 컨텍스트 메뉴)
  - ExcelService.cs: ExportNonCoreAllToExcelAsync(), ExportNonCoreByBorrowerToExcelAsync() 메서드 추가
  - 차주별 내보내기 시 각 차주별 시트 분리, 요약 시트 포함
  
- **Ctrl+F 검색 기능**
  - NonCoreView.xaml: 플로팅 검색 패널 UI 추가
  - NonCoreViewModel.cs: SearchText, IsSearchPanelVisible, FindNext(), FindPrevious() 추가
  - Ctrl+F로 검색창 토글, Enter/Shift+Enter로 이전/다음 결과 이동, Esc로 닫기
  
- **조건별 필터 검색창**
  - NonCoreView.xaml: 좌측 필터 패널 추가 (접기/펼치기 가능)
  - NonCoreViewModel.cs: PropertyTabItem에 필터용 속성 추가 (PropertyType, IsAuctionInProgress, IsRestructuring 등)
  - 필터 조건: 회생차주, 개인회생, 차주거주, 경매진행, 상가, 주택
  - ApplyFilters(), ClearFilters() 메서드로 실시간 필터링
  - 필터 결과 건수 표시 (검색결과: N/M건)

### 2024-12-16
- Progress 파일 생성
- 피드백 35개 항목 정리 완료
- **Phase 1 완료**
  - Colors.xaml: Primary 색상 남색(#1E3A5F)으로 변경, HeaderNavy 색상 추가
  - MainWindow.xaml: 헤더 남색 배경 + 흰색 아이콘, 엠필로지 로고, 남색 테두리 추가
  - LoginWindow.xaml: 남색 테두리 추가로 메인 화면과 톤 통일
- **Phase 2 완료**
  - DashboardView.xaml: 2영역 레이아웃 전면 재구성
    - 좌측: 진행 중인 프로그램 목록 (ProgramSummary 모델, 진행률 바 표시)
    - 우측: 프로그램명 상세 데이터 (데이터 그리드)
  - GridSplitter: 두 영역 간 너비 조절 가능
  - 토글 버튼: 좌측 패널 접기/펼치기 기능
  - 내부 탭: 홈/비핵심/등기부/권리분석/기초데이터/마감 탭 추가
  - DashboardViewModel.cs: 
    - ProgramSummary 모델 추가 (프로젝트별 요약)
    - ColumnProgressInfo 모델 추가 (컬럼별 진행률)
    - LoadProgramSummariesAsync(), RecalculateColumnProgress() 등 메서드 추가
  - DashboardView.xaml.cs: 패널 토글, 내부 탭 변경, 차주번호 클릭 이벤트 추가
  - MainWindow.xaml.cs: NavigateToNonCoreView() 메서드 추가
  - PropertyDetailViewModel.cs: SetActiveTab() 메서드 추가
- **Phase 3 완료**
  - NonCoreView.xaml: 비핵심 화면 신규 생성
    - 9개 기능 탭 (차주 개요, 론, 담보 총괄, 담보 물건, 선순위 평가, 경공매, 회생개요, 현금흐름 집계, NPB)
    - PDF 스타일 물건 번호 탭 바 (상단)
    - 툴박스 사이드 패널 (우측, 열기/닫기 토글)
    - 기능별 일괄작업 드롭다운 메뉴
  - NonCoreView.xaml.cs: 코드비하인드
    - 기능 탭 전환 로직
    - 물건 탭 선택/닫기 이벤트
    - 툴박스 열기/닫기, 빠른 계산, 참조 링크
  - NonCoreViewModel.cs: ViewModel
    - PropertyTabItem 모델 (물건 탭)
    - CourtInfo 모델 (법원 정보)
    - 일괄 작업 모드 (BatchWorkType)
  - Controls.xaml: NonCoreTabStyle 추가
  - App.xaml.cs: NonCoreView, NonCoreViewModel DI 등록

### 2024-12-17
- **Phase 4 완료**
  - 4.1, 4.2: 이미 구현되어 있음 (Phase 2에서)
  - 4.3: 단축키 네비게이션 구현
    - DashboardView: Shift+Tab, Tab, Alt+방향키, Enter, Ctrl+R
    - NonCoreView: Shift+Tab, Tab, Alt+방향키, Ctrl+D
  - 4.4: 대시보드 ↔ 비핵심 간 자유로운 이동
    - Alt+Left/Right: 프로그램/물건 이동
    - Alt+Up/Down: 탭 이동
  - 4.5: 롤업 기능 구현
    - RollupWindow.xaml/cs: 비핵심 전체 현황 창
    - 5초 자동 새로고침 (토글 가능)
    - 닫기 시 확인 메시지
    - Excel 내보내기 기능
    - BoolToCheckConverter 추가

- **Phase 5 완료**
  - 5.1: 물건 목록 버튼 숨김 처리 (MainWindow.xaml)
  - 5.2: 통제 기능 - 해당 없음 확인
  - 5.3: 데이터 업로드 버튼 숨김 처리 (MainWindow.xaml)
  - 5.4: 비핵심 기초데이터 화면에서 데이터 업로드
    - BasicDataTab.xaml: 데이터 업로드 섹션 추가
    - PropertyDetailViewModel.cs: UploadDataFileCommand, DownloadTemplateCommand
    - 마지막 업로드 일자 표시

- **Phase 6 완료**
  - 6.1~6.3: 권한별 화면 필터링 구현
    - DashboardViewModel.cs: ProgramUserRepository 의존성 추가
    - 관리자(Admin): 전체 데이터 조회
    - PM: 담당 프로그램만 조회 (ProgramUserRepository.GetUserPMProgramsAsync 활용)
    - 평가자(Evaluator): 자기에게 할당된 물건만 조회 (AssignedTo 필터)
    - LoadProgramSummariesAsync(), LoadDashboardPropertiesAsync(), LoadStatisticsAsync() 메서드에 권한별 필터링 로직 추가
  - 6.4: OPB 컬럼 추가
    - Property.cs: Opb (decimal?) 필드 추가
    - PropertyRepository.cs: PropertyTable에 opb 컬럼 매핑, MapToProperty/MapToPropertyTable 매핑 추가
  - 6.5: 상권 데이터 체크박스 추가
    - Property.cs: HasCommercialDistrictData (bool) 필드 추가
    - PropertyRepository.cs: has_commercial_district_data 컬럼 매핑
    - 상가/아파트형공장 유형 물건 전용
  - DB 마이그레이션 스크립트 생성: docs/database/migration_phase6.sql
  - App.xaml.cs: DashboardViewModel 생성자에 ProgramUserRepository 주입

- **Phase 7 부분 완료** (3/4)
  - 7.2: 경매 일정에 인터링/상계회수 항목 추가
    - AuctionSchedule.cs: InterimPrincipalOffset, InterimPrincipalRecovery, InterimInterestOffset, InterimInterestRecovery 필드 추가
    - AuctionScheduleRepository.cs: 테이블 클래스 및 매퍼 업데이트
    - AuctionScheduleView.xaml: 편집 패널에 인터링/상계회수 섹션 UI 추가
    - AuctionScheduleViewModel.cs: 편집 필드 추가, 저장 로직 업데이트
    - migration_phase7.sql: DB 마이그레이션 스크립트 생성
  - 7.3: 대출 화면에서 대출종류(C) 컬럼 제거
    - LoanDetailView.xaml: 채권정보 섹션 재구성 (A-B-C-D → A-B-C-D-E-F-G 순서 재정렬)
  - 7.4: 주소 앞뒤 공란 자동 제거 기능
    - PropertyRepository.cs: TrimAddressFields() 헬퍼 메서드 추가
    - CreateAsync, UpdateAsync에서 저장 전 주소 필드 자동 Trim
  - 7.1: 소상공인 상권 데이터 지도 연동 완료
    - CommercialDistrictService.cs: 소상공인 상권정보 API 연동 서비스
    - CommercialDistrictMapWindow.xaml/cs: 상권 분석 지도 창
    - commercial_map.html: 상권 지도 HTML (카카오맵 기반)
    - NonCoreView: 툴박스에 상권 지도 열기 버튼 추가
    - NonCoreViewModel: GetCurrentProperty() 메서드 추가
    - 기능: 물건 위치 표시, 반경 설정, 업종별 통계, 점포 목록, Excel 내보내기

<!-- 
작업 완료 시 아래 형식으로 로그 추가:

### YYYY-MM-DD
- Phase X 완료
- 주요 변경 사항: ...
- 이슈: ...
-->

---

## 🔗 관련 문서

- 원본 피드백: `manager/client/1205 피드백 요약.txt`
- 미팅 상세: `manager/client/1205 미팅 내용.txt`
- 화면 가이드: `docs/screens/SCREEN_GUIDE.md`
- 색상 정의: `src/NPLogic.UI/Styles/Colors.xaml`

---

## ⚠️ 주의사항

1. **Phase 2는 핵심 작업** - 가장 복잡하므로 충분한 시간 확보 필요
2. **Phase 6 DB 변경** - 마이그레이션 전 백업 필수
3. **Phase별 독립 작업 가능** - 단, Phase 1→2 순서 권장 (색상 변경 후 레이아웃)
4. **각 Phase 완료 후 이 파일 업데이트** - 체크박스 체크 및 작업 로그 추가
5. **빌드 불필요** - 각 Phase 작업 완료 후 빌드까지 할 필요 없음 (코드 변경만 확인)

