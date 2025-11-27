# NPLogic 개발 진행 체크리스트

## 📋 프로젝트 현황

**프로젝트명**: NPLogic (등기부등본 자동화 시스템)
**기술 스택**: WPF .NET 8, Supabase, Python
**개발 기간**: 7주
**목표**: 업무 자동화 데스크톱 애플리케이션

---

## 🗺️ 화면별 빠른 참조 가이드

> **💡 사용법**: 화면 구현할 때 이 테이블만 보고 필요한 파일들을 찾으면 됩니다!

| 화면명 | 기획서 파일 | DB 테이블 | 상세 가이드 | 비고 |
|--------|------------|-----------|------------|------|
| **1. 로그인** | `Login.md` | `users` (Supabase Auth) | `SCREEN_GUIDE.md` §1 | Auth 구현 완료 |
| **2-1. PM 대시보드** | `PM.md` | `properties`, `borrowers`, `programs` | `SCREEN_GUIDE.md` §2 | 15개 컬럼 그리드 |
| **2-2. 평가자 대시보드** | `평가자.md` | 동일 (role 필터) | `SCREEN_GUIDE.md` §2 | 진행률 추적 |
| **2-3. 관리자 대시보드** | `관리자모드.md` | `programs`, `users` | `SCREEN_GUIDE.md` §2 | 프로그램 관리 |
| **3. 물건 목록** | - | `properties` | `SCREEN_GUIDE.md` §3 | 검색/필터 |
| **4. 기초데이터 탭** | `기초테이터.md`, `담보물건(Ⅳ).md` | `properties`, `documents` | `SCREEN_GUIDE.md` §4 | 지도, KB시세, QA |
| **5. 등기부 탭** | `등기부(Ⅲ).md` | `registry_documents`, `registry_rights` | `SCREEN_GUIDE.md` §5 | DD 크로스체크, OCR |
| **6. 데이터 업로드** | - | 전체 | `SCREEN_GUIDE.md` §6 | Excel, PDF 업로드 |
| **7. 권리분석 탭** | `선순위(Ⅴ).md` | `senior_rights`, `auction_info` | `SCREEN_GUIDE.md` §7 | ⭐ 40+ 케이스 로직 |
| **8. 평가 탭** | `평가(Ⅵ)_1. 아파트.md` | `evaluations`, `auction_statistics` | `SCREEN_GUIDE.md` §8 | 낙찰통계, XNPV |
| **9. 지도 탭** | - | `properties` | `SCREEN_GUIDE.md` §9 | 카카오맵/네이버맵 |
| **10. 선순위 관리** | - | `senior_rights` | `SCREEN_GUIDE.md` §10 | CRUD |
| **11. 경매일정** | `경매일정(Ⅶ).md` | `auction_schedules` | `SCREEN_GUIDE_ADDITIONS.md` §1 | 비용 시뮬레이션 |
| **12. 공매일정** | - | `public_sale_schedules` | `SCREEN_GUIDE.md` §11-12 | 경매와 유사 |
| **13. 통계 대시보드** | - | 전체 (집계) | `SCREEN_GUIDE.md` §13 | 차트 |
| **14. 사용자 관리** | - | `users` (Supabase Auth) | `SCREEN_GUIDE.md` §14 | 관리자 전용 |
| **15. 설정 관리** | - | `settings` | `SCREEN_GUIDE.md` §15 | 수식, 매핑 |
| **16. 작업 이력** | - | `audit_logs` | `SCREEN_GUIDE.md` §16 | 변경 추적 |
| **17. 차주개요** | - | `borrowers`, `programs` | `SCREEN_GUIDE_ADDITIONS.md` §2 | 차주 정보 |
| **18. 담보총괄** | `담보총괄(Ⅱ).md` | `properties` (집계) | `SCREEN_GUIDE_ADDITIONS.md` §3 | Loan Cap |
| **19. Loan 상세** | - | `loans` | `SCREEN_GUIDE_ADDITIONS.md` §4 | 대출 관리 |
| **20. 현금흐름 집계** | - | `cash_flows` | `SCREEN_GUIDE_ADDITIONS.md` §5 | XNPV |
| **21. XNPV 비교** | - | `evaluations`, `cash_flows` | `SCREEN_GUIDE_ADDITIONS.md` §6 | 할인율 시나리오 |
| **22. 회생개요** | - | `restructuring` | `SCREEN_GUIDE_ADDITIONS.md` §7 | 변제 계획 |
| **23. Tool Box** | - | `reference_data` | `SCREEN_GUIDE_ADDITIONS.md` §8 | 참조 데이터 |

### 📚 문서 파일 위치
- **기획서**: `docs/converted_sheets/markdown/*.md` 또는 `docs/converted_sheets/csv/*.csv`
- **상세 가이드**: `docs/screens/SCREEN_GUIDE.md` (주요 화면), `docs/screens/SCREEN_GUIDE_ADDITIONS.md` (추가 화면)
- **DB 스키마**: `docs/database/SCHEMA.md`
- **디자인 시스템**: `docs/design/DESIGN_SYSTEM.md`

### 🔥 복잡도 레벨
- 🟢 **낮음**: 로그인, 물건 목록, 지도, 경매/공매일정
- 🟡 **중간**: 기초데이터, 등기부, 평가, 차주개요, 담보총괄
- 🔴 **높음**: 대시보드 (15+ 컬럼), 권리분석 (40+ 케이스), XNPV 계산

---

## ✅ Phase 0: 프로젝트 설정 (현재)

### 문서화
- [x] 요구사항 문서 분석
- [x] 기술 아키텍처 문서 작성 (`docs/architecture/TECH_STACK.md`)
- [x] UI/UX 디자인 시스템 명세 (`docs/design/DESIGN_SYSTEM.md`)
- [x] 데이터베이스 스키마 설계 (`docs/database/SCHEMA.md`)
- [x] 화면별 구현 가이드 (`docs/screens/SCREEN_GUIDE.md`)
- [x] 개발 진행 체크리스트 (`docs/progress/CHECKLIST.md`)

### 프로젝트 구조
- [x] 프로젝트 폴더 구조 재구성
  - [x] src/ 폴더 생성
  - [x] NPLogic.App/ 프로젝트 생성 (WPF)
  - [x] NPLogic.Core/ 프로젝트 생성 (비즈니스 로직)
  - [x] NPLogic.Data/ 프로젝트 생성 (데이터 액세스)
  - [x] NPLogic.UI/ 프로젝트 생성 (공통 UI)
  - [x] python/ 폴더 생성
  - [x] tests/ 폴더 생성
  - [x] NPLogic.sln 솔루션 파일 생성
  - [x] 기존 파일들 적절한 프로젝트로 이동

### 개발 환경
- [x] NuGet 패키지 설치
  - [x] CommunityToolkit.Mvvm (8.2.2) → NPLogic.App
  - [x] MaterialDesignThemes (4.9.0) → NPLogic.App, NPLogic.UI
  - [x] supabase-csharp (0.16.2) → NPLogic.Data
  - [x] EPPlus (7.0.5) → NPLogic.Core
  - [x] Microsoft.Web.WebView2 → NPLogic.App
  - [x] Serilog (3.1.1) → NPLogic.App
  - [x] Serilog.Sinks.File (5.0.0) → NPLogic.App
  - [x] Microsoft.Extensions.DependencyInjection (8.0.0) → NPLogic.App
- [ ] Python 환경 설정
  - [ ] Python 3.x 설치 확인
  - [x] requirements.txt 작성
  - [ ] OCR 라이브러리 설치 (Tesseract)
- [x] Supabase 프로젝트 생성
  - [x] 프로젝트 생성
  - [x] API 키 확보
  - [x] 환경 변수 설정 (appsettings.json 템플릿)
  - [x] .gitignore 업데이트
  - **💡 참고**: Supabase MCP 도구로 DB 직접 제어 가능
    - `mcp_supabase_apply_migration` - 마이그레이션 적용
    - `mcp_supabase_execute_sql` - SQL 쿼리 실행
    - `mcp_supabase_list_tables` - 테이블 목록 조회
    - `mcp_supabase_get_logs` - 로그 확인

---

## 🎨 Phase 1: 디자인 시스템 구축 (1주차)

### XAML 리소스
- [x] `Styles/Colors.xaml` 생성
  - [x] Deep Navy 색상 정의
  - [x] Blue Gray 색상 정의
  - [x] Semantic 색상 정의
- [x] `Styles/Typography.xaml` 생성
  - [x] 폰트 패밀리 정의
  - [x] 폰트 크기 스타일
  - [x] 폰트 굵기 스타일
- [x] `Styles/Controls.xaml` 생성
  - [x] Button 기본 스타일
  - [x] TextBox 스타일
  - [x] ComboBox 스타일
  - [x] CheckBox/RadioButton 스타일
- [x] `Styles/Buttons.xaml` 생성
  - [x] PrimaryButton 스타일
  - [x] SecondaryButton 스타일
  - [x] DangerButton 스타일
  - [x] IconButton 스타일
- [x] `Styles/DataGrid.xaml` 생성
  - [x] DataGrid 헤더 스타일 (Blue Gray)
  - [x] DataGrid 행 스타일
  - [x] Hover 효과
  - [x] 선택 효과

### 공통 컴포넌트
- [x] `Controls/NPButton.xaml` - 커스텀 버튼
- [x] `Controls/NPTextBox.xaml` - 커스텀 입력 필드
- [x] `Controls/NPDataGrid.xaml` - 커스텀 그리드
- [x] `Controls/NPCard.xaml` - 카드 컨테이너
- [x] `Controls/NPModal.xaml` - 모달 다이얼로그
- [x] `Controls/NPProgressBar.xaml` - 프로그레스바
- [x] `Controls/NPBadge.xaml` - 뱃지/태그
- [x] `Controls/NPToast.xaml` - 토스트 알림
- [x] `Services/ToastService.cs` - 토스트 서비스

### 메인 레이아웃
- [x] `MainWindow.xaml` 재구성
  - [x] Header 영역 구현
  - [x] Sidebar 네비게이션
  - [x] Main Content Area
  - [x] Status Bar
- [x] Sidebar 접기/펴기 기능
- [x] 메뉴 아이콘 및 라벨

---

## 🔐 Phase 2: 인증 및 기본 기능 (1-2주차) ✅ 완료

### Supabase 연동
- [x] Supabase 클라이언트 설정
  - [x] `SupabaseService.cs` 생성
  - [x] 초기화 코드
  - [x] 에러 핸들링
- [x] 데이터베이스 설정
  - [x] SQL 스크립트 실행
  - [x] 테이블 생성 (14개 테이블)
  - [x] RLS 정책 적용
  - [ ] Storage 버킷 생성

### 로그인 화면
- [x] `Views/LoginWindow.xaml` 생성
- [x] `ViewModels/LoginViewModel.cs` 생성
- [x] Supabase Auth 로그인 구현
- [x] 입력 검증
- [x] 에러 메시지 표시
- [x] 자동 로그인 기능

### 사용자 관리
- [x] User 모델 클래스
- [x] 역할 기반 권한 확인
- [x] 세션 관리

---

## 📊 Phase 3: 대시보드 및 물건 목록 (2-3주차) ✅ 완료

### 대시보드
- [x] `Views/DashboardView.xaml` 생성
- [x] `ViewModels/DashboardViewModel.cs` 생성
- [x] PM 대시보드 레이아웃 완성
- [x] 평가자 대시보드 레이아웃 완성
- [x] 관리자 대시보드 레이아웃 완성
- [x] 통계 카드 데이터 바인딩
- [x] 프로그램 선택기
- [x] 프로그램별 데이터 필터링

### 물건 목록
- [x] `Views/PropertyListView.xaml` 생성
- [x] `ViewModels/PropertyListViewModel.cs` 생성
- [x] 데이터 그리드 Supabase 바인딩
- [x] 검색 기능 (실시간)
- [x] 필터 패널
  - [x] 물건 유형 필터
  - [x] 상태 필터
  - [x] 프로젝트 필터
- [x] 정렬 기능 (Repository 레벨)
- [x] 페이지네이션 (50개씩)
- [x] 물건 추가 모달
- [x] 물건 수정 모달

### Property 모델
- [x] Property 모델 클래스
- [x] PropertyRepository 생성
- [x] CRUD 메서드 구현 완성
  - [x] GetAllAsync
  - [x] GetByIdAsync
  - [x] GetPagedAsync (검색, 필터, 페이지네이션)
  - [x] CreateAsync
  - [x] UpdateAsync
  - [x] DeleteAsync
  - [x] GetStatisticsAsync

---

## 📝 Phase 4: 물건 상세 - 기초데이터 (3주차) ✅ 완료

### 물건 상세 화면
- [x] `Views/PropertyDetailView.xaml` 생성
- [x] `ViewModels/PropertyDetailViewModel.cs` 생성
- [x] 탭 컨트롤 구조
- [x] 기초데이터 탭 레이아웃
  - [x] 기본 정보 섹션
  - [x] 주소 정보 섹션
  - [x] 면적 정보 섹션
  - [x] 가격 정보 섹션
- [x] 폼 데이터 바인딩
- [x] 자동 입력 필드 (읽기 전용)
- [x] 수동 입력 필드 (편집 가능)
- [x] 저장 기능
- [x] 취소/새로고침 기능

---

## 📄 Phase 5: 데이터 업로드 (3-4주차) ✅ 완료

### 엑셀 업로드
- [x] `Views/DataUploadView.xaml` 생성
- [x] `ViewModels/DataUploadViewModel.cs` 생성
- [x] 파일 선택 (드래그앤드롭)
- [x] Excel 파싱 (EPPlus)
- [x] ExcelService 구현
- [x] 컬럼 매핑 화면
  - [x] 자동 매핑 로직
  - [x] 수동 매핑 UI
- [x] 데이터 검증
- [x] Supabase 일괄 INSERT
- [x] 프로그레스바
- [x] 결과 요약

### PDF 업로드
- [x] PDF 다중 파일 선택
- [x] Supabase Storage 업로드
- [x] StorageService 구현
- [x] 파일 목록 표시
- [x] 업로드 진행상황 표시
- [x] 개별 파일 프로그레스
- [x] 전체 프로그레스
- [x] 실패 파일 표시

---

## 🔍 Phase 6: OCR 통합 (4주차) 🔄 부분 완료

### 등기부 탭 UI ✅ 완료
- [x] 등기부 탭 레이아웃 (`Views/RegistryTab.xaml`)
- [x] 물건 기본 정보 섹션 (지번번호, 담보물형태, 면적, 주소 일치여부)
- [x] 소유자 정보 그리드 (추가/삭제 기능)
- [x] 갑구 (소유권) 권리 그리드 (추가/삭제, 합계 표시)
- [x] 을구 (근저당/전세권) 권리 그리드 (추가/삭제, 합계 표시)
- [x] 데이터 저장/새로고침 기능

### Registry 모델 ✅ 완료
- [x] RegistryDocument 모델 (`Core/Models/RegistryDocument.cs`)
- [x] RegistryOwner 모델 (`Core/Models/RegistryOwner.cs`)
- [x] RegistryRight 모델 (`Core/Models/RegistryRight.cs`)
- [x] RegistryRepository 구현 (`Data/Repositories/RegistryRepository.cs`)
- [x] RegistryTabViewModel 구현 (`ViewModels/RegistryTabViewModel.cs`)

### Python OCR 설정 ⏳ 보류 (추후 구현)
- [ ] `python/ocr_processor.py` 작성
- [ ] Python 의존성 설치
- [ ] OCR 테스트 스크립트

### C# OCR 서비스 ⏳ 보류 (추후 구현)
- [ ] `Services/OcrService.cs` 생성
- [ ] Python 프로세스 실행
- [ ] JSON 결과 파싱
- [ ] 배치 처리 (50-100개씩)
- [ ] 병렬 처리
- [ ] 프로그레스 추적
- [ ] 오류 처리 및 재처리

### OCR 기반 기능 ⏳ 보류 (추후 구현)
- [ ] PDF 목록 표시
- [ ] OCR 실행 버튼
- [ ] OCR 상태 표시
- [ ] 추출 데이터 자동 매핑
- [ ] DD 데이터 자동 비교

---

## ⚖️ Phase 7: 권리 분석 및 평가 (4-5주차) ✅ 권리분석 완료

### 권리 분석 탭 ✅ 완료
- [x] 권리 분석 탭 레이아웃 (`Views/RightsAnalysisTab.xaml`)
- [x] RightAnalysis 모델 클래스 (`Core/Models/RightAnalysis.cs`)
- [x] RightAnalysisRepository 구현 (`Data/Repositories/RightAnalysisRepository.cs`)
- [x] RightsAnalysisTabViewModel 구현
- [x] 선순위 권리 그리드 (DD금액, 반영금액, 상세추정 근거)
- [x] 선순위 합계 자동 계산
- [x] 배당 시뮬레이션 (예상낙찰가, 경매수수료, 배당가능재원, Loan Cap)
- [x] 위험도 평가 (🔴 높음 / 🟡 중간 / 🟢 낮음)
- [x] 40+ 케이스 룰 엔진 (`Core/Services/RightAnalysisRuleEngine.cs`)
  - 주택 케이스 (CASE_R1~R18)
  - 토지 케이스 (CASE_L1~L4)
  - 상가/공장 케이스 (CASE_C1~C15)
  - 임금채권 케이스
  - 당해세/선순위조세 케이스

### 평가 탭
- [ ] 평가 유형 선택
- [ ] 아파트 평가 폼
- [ ] 상가 평가 폼
- [ ] 토지 평가 폼
- [ ] 시세 API 연동 (선택적)
- [ ] 자동 계산 (수식 적용)
- [ ] 평가액 확정

### 계산 수식 엔진
- [ ] 수식 파서 구현
- [ ] 수식 실행 엔진
- [ ] 설정에서 수식 관리

---

## 📤 Phase 8: Excel 출력 (5주차)

### Excel 서비스
- [ ] `Services/ExcelService.cs` 생성
- [ ] EPPlus 설정
- [ ] 여러 시트 통합 로직
- [ ] 데이터 쿼리 및 가공
- [ ] 수식 적용
- [ ] 스타일 및 포맷 적용
- [ ] 파일 생성 및 저장

### 출력 화면
- [ ] 출력 설정 모달
- [ ] 출력 대상 선택 (전체/선택/필터)
- [ ] 시트 구성 선택
- [ ] 시트 미리보기
- [ ] 프로그레스 표시
- [ ] 완료 후 파일 열기

---

## 🗺️ Phase 9: 지도 연동 (5-6주차) 🔄 진행 중

### 지도 통합
- [x] CefSharp 또는 WebView2 설치
- [x] HTML 지도 코드 준비 (카카오맵)
- [x] 지도 탭 레이아웃
- [x] WebView 컨트롤 추가 (MapView UserControl)
- [x] C# ↔ JavaScript 통신
- [x] 물건 데이터 전달
- [x] 마커 표시
- [ ] 레이어 선택 UI
- [ ] 레이어 토글 기능
- [x] 마커 클릭 이벤트
- [x] 정보 팝업 표시

---

## 📈 Phase 10: 통계 및 유사 물건 추천 (6주차) 🔄 진행 중

### 통계 화면 ✅ 완료
- [x] `Views/StatisticsView.xaml` 생성
- [x] `ViewModels/StatisticsViewModel.cs` 생성
- [x] 필터 패널 (프로젝트 선택)
- [x] 통계 카드 (총 물건 수, 평균 감정가, 평균 회수율, 완료율)
- [x] LiveCharts2 설치 (LiveChartsCore.SkiaSharpView.WPF)
- [x] 차트 구현
  - [x] 막대 차트 (상태별, 지역별)
  - [x] 선 차트 (월별 등록 추이)
  - [x] 파이 차트 (물건 유형별 분포)
- [x] StatisticsRepository 생성 (통계 쿼리 메서드)
- [x] MainWindow 네비게이션 연결
- [ ] 통계 Excel 출력 (추후 구현)

### 유사 물건 추천 ⏸️ 외부 디펜던시 대기

> **⚠️ 주의: 외부 디펜던시 있음 - 먼저 진행하지 마세요!**
> 
> **대기 사유**: 박광호님(@채널코드) 추천 코드 수령 후 진행
> 
> **선행 조건**:
> 1. 박광호님 추천 코드 공유 받기 (마할라노비스 거리 기반)
> 2. NPLogic에서 원하는 출력 형태 정의서 전달
> 3. 실제 구현 시 방식 협의 (일괄 행렬연산 vs 개별 처리)
> 
> **참고**: `manager/client/1124_미팅.txt` (2024-11-24 미팅 내용)

- [ ] 추천 알고리즘 통합
- [ ] 과거 경매 데이터 조회
- [ ] 유사도 계산 (마할라노비스 거리)
- [ ] 추천 결과 표시
- [ ] 사례 지도 기능

---

## 🔧 Phase 11: 관리자 기능 (6주차)

### 사용자 관리
- [ ] `Views/UserManagementView.xaml` 생성
- [ ] `ViewModels/UserManagementViewModel.cs` 생성
- [ ] 사용자 목록 표시
- [ ] 사용자 추가 모달
- [ ] 사용자 수정 모달
- [ ] 사용자 삭제
- [ ] 역할 할당
- [ ] 상태 변경

### 설정 관리
- [ ] `Views/SettingsView.xaml` 생성
- [ ] 계산 수식 설정 탭
- [ ] 데이터 매핑 설정 탭
- [ ] 시스템 환경 설정 탭
- [ ] 설정 저장/로드

### 작업 이력
- [ ] `Views/AuditLogsView.xaml` 생성
- [ ] 이력 목록 표시
- [ ] 필터링 (사용자, 날짜, 액션)
- [ ] 변경 전/후 비교
- [ ] 되돌리기 기능 (선택적)

---

## 🎬 Phase 12: 추가 화면 (6-7주차)

### 경매/공매 일정
- [ ] `Views/AuctionScheduleView.xaml` 생성
- [ ] `Views/PublicSaleScheduleView.xaml` 생성
- [ ] 달력 보기
- [ ] 리스트 보기
- [ ] 일정 추가/수정/삭제

### 선순위 관리
- [ ] `Views/SeniorRightsView.xaml` 생성
- [ ] 권리 목록 표시
- [ ] 권리 추가/수정/삭제
- [ ] 순위 정렬
- [ ] 합계 계산

---

## 🧪 Phase 13: QA 및 테스트 (7주차)

### 기능 테스트
- [ ] 로그인/로그아웃 테스트
- [ ] 역할별 권한 테스트
- [ ] 데이터 업로드 테스트 (소량)
- [ ] 데이터 업로드 테스트 (대량 500+)
- [ ] OCR 처리 테스트
- [ ] Excel 출력 테스트
- [ ] 지도 기능 테스트
- [ ] 통계 기능 테스트

### 성능 테스트
- [ ] 3,000개 PDF OCR 처리 테스트
- [ ] 대량 데이터 그리드 로딩 테스트
- [ ] Excel 출력 성능 테스트
- [ ] 메모리 사용량 모니터링

### UI/UX 테스트
- [ ] 화면 레이아웃 검증
- [ ] 색상 및 스타일 일관성
- [ ] 반응성 테스트
- [ ] 사용자 시나리오 테스트

### 버그 수정
- [ ] 버그 목록 작성
- [ ] 우선순위별 수정
- [ ] 재테스트

---

## 🚀 Phase 14: 배포 및 문서화 (7주차)

### 배포 준비
- [ ] Release 모드 빌드
- [ ] 종속성 확인
- [ ] 설치 프로그램 생성 (ClickOnce 또는 Inno Setup)
- [ ] Python 런타임 포함
- [ ] 자동 업데이트 설정 (선택적)

### 문서화
- [ ] 설치 매뉴얼 작성
- [ ] 사용자 가이드 작성
- [ ] 트러블슈팅 가이드
- [ ] 관리자 매뉴얼

### 최종 확인
- [ ] 클라이언트 데모
- [ ] 피드백 수집
- [ ] 최종 수정
- [ ] 배포

---

## 📌 지속적인 작업

### 코드 품질
- [ ] 코드 리뷰
- [ ] 리팩토링
- [ ] 주석 작성
- [ ] 네이밍 컨벤션 통일

### 로깅 및 모니터링
- [ ] Serilog 설정
- [ ] 로그 레벨 설정
- [ ] 에러 로그 수집
- [ ] 성능 로그

### 보안
- [ ] API 키 환경 변수화
- [ ] RLS 정책 검증
- [ ] 입력 검증 강화
- [ ] SQL Injection 방지

---

## 🎯 마일스톤

### Milestone 1 (2주차 말) ✅ 완료
- [x] 프로젝트 설정 완료
- [x] 디자인 시스템 구축
- [x] 로그인 및 대시보드

### Milestone 2 (4주차 말) ✅ 완료
- [x] 물건 목록 및 상세 (완료)
- [x] 데이터 업로드 (완료)
- [ ] OCR 통합 (Python 제외)

### Milestone 3 (5주차 말) - 중도금 지급
- [ ] 권리 분석
- [ ] 평가 기능
- [ ] Excel 출력
- [ ] 핵심 기능 완료

### Milestone 4 (7주차 말) - 최종 완료
- [ ] 지도 연동
- [ ] 통계 및 추천
- [ ] 관리자 기능
- [ ] QA 및 배포

---

## 💡 다음 작업 시작 방법

새로운 채팅 세션에서 다음 명령으로 계속:

```
"NPLogic 프로젝트 계속 진행. docs/progress/CHECKLIST.md를 보고 
다음 체크되지 않은 항목부터 작업 시작해줘."
```

또는 특정 Phase부터:

```
"NPLogic Phase 1 (디자인 시스템 구축) 작업 시작해줘."
```

---

## 📝 작업 현황 요약

**완료됨**:
- ✅ 요구사항 분석
- ✅ 기술 아키텍처 문서
- ✅ 디자인 시스템 명세
- ✅ 데이터베이스 스키마
- ✅ 화면별 구현 가이드
- ✅ 개발 진행 체크리스트
- ✅ 멀티 프로젝트 구조 구축 (NPLogic.App, Core, Data, UI)
- ✅ NuGet 패키지 설치 완료
- ✅ 프로젝트 간 참조 설정
- ✅ WebView2 설치
- ✅ appsettings.json 템플릿 생성
- ✅ 프로젝트 구조 문서 작성
- ✅ 환경 설정 가이드 작성
- ✅ **Phase 1: 디자인 시스템 구축 완료**
  - ✅ 모든 XAML 리소스 스타일 (Colors, Typography, Controls, Buttons, DataGrid)
  - ✅ 8개 공통 컴포넌트 (NPButton, NPTextBox, NPDataGrid, NPCard, NPModal, NPProgressBar, NPBadge, NPToast)
  - ✅ ToastService 구현
  - ✅ MainWindow 레이아웃 완성 (Header, Sidebar, Content, StatusBar)
  - ✅ Sidebar 접기/펴기 애니메이션 기능
- ✅ **Phase 2: 인증 및 기본 기능 완료**
  - ✅ Supabase 클라이언트 설정 (SupabaseService, AuthService)
  - ✅ 데이터베이스 14개 테이블 생성 및 RLS 정책 적용
  - ✅ 로그인 화면 구현 (LoginWindow, LoginViewModel)
  - ✅ 사용자 인증 및 세션 관리
  - ✅ User 모델 및 Repository
- ✅ **Phase 3: 대시보드 및 물건 목록 완료**
  - ✅ DashboardView, DashboardViewModel 구현
  - ✅ PropertyListView, PropertyListViewModel 구현
  - ✅ Property 모델 및 Repository (완전한 CRUD)
  - ✅ 통계 카드 데이터 바인딩
  - ✅ 프로그램 선택기 및 필터링
  - ✅ 검색/필터/정렬/페이지네이션
  - ✅ 물건 추가/수정 모달 (PropertyFormModal)
- ✅ **Phase 4: 물건 상세 - 기초데이터 완료**
  - ✅ PropertyDetailView, PropertyDetailViewModel 구현
  - ✅ 탭 컨트롤 구조 (6개 탭)
  - ✅ 기초데이터 탭 (기본 정보, 주소, 면적, 가격)
  - ✅ 폼 데이터 바인딩 및 저장 기능
- ✅ **Phase 5: 데이터 업로드 완료**
  - ✅ DataUploadView, DataUploadViewModel 구현
  - ✅ ExcelService (EPPlus 기반 파싱 및 출력)
  - ✅ StorageService (Supabase Storage 연동)
  - ✅ Excel 업로드 (컬럼 매핑, 자동 매핑, 일괄 INSERT)
  - ✅ PDF 업로드 (다중 파일, 프로그레스 추적)

**진행 중**:
- 🔄 **Phase 9: 지도 연동 (부분 완료)**
  - ✅ WebView2 + 카카오맵 연동
  - ✅ MapView UserControl 생성 (`Views/MapView.xaml`)
  - ✅ 물건 상세 지도 탭 구현
  - ✅ 마커 표시 및 인포윈도우
  - ✅ C# ↔ JavaScript 통신
  - ⏳ 레이어 선택/토글 기능 (추후)
- ✅ **Phase 10: 통계 대시보드 (완료)**
  - ✅ LiveCharts2 설치 및 차트 구현 (파이, 막대, 선)
  - ✅ StatisticsView, StatisticsViewModel 구현
  - ✅ StatisticsRepository 구현 (유형별/상태별/지역별/월별 통계)
  - ✅ 통계 카드 (총 물건 수, 평균 감정가, 회수율, 완료율)
  - ✅ Supabase 테스트 데이터 30건 삽입
  - ⏳ Excel 출력 (추후)
- 🔄 **Phase 6: OCR 통합 (UI 완료, OCR 보류)**
  - ✅ 등기부 탭 UI 구현 (`Views/RegistryTab.xaml`)
  - ✅ Registry 모델 및 Repository 구현
  - ✅ 소유자/갑구/을구 그리드 CRUD
  - ⏳ Python OCR 프로세서 (보류)
  - ⏳ C# OCR 서비스 (보류)
  - ⏳ PDF 자동 파싱 (보류)

**다음 작업**:
1. 평가 탭 (Phase 7) - 유형별 평가 폼 (아파트/상가/토지)
2. 경매/공매 일정 (Phase 12) - 비용 계산 시뮬레이션
3. 사용자 관리 / 설정 관리 / 작업 이력 (Phase 11)
4. 선순위 관리 화면 (Phase 12)

**예상 완료일**: 7주 후 (요구사항 문서 기준)

---
# NPLogic 화면별 개발 현황

> **전체 화면**: 23개 | **완료**: 18개 (78%) | **남은**: 5개 (22%)

---

## ✅ 완료된 화면 (1-6주차)

| 화면 | 화면명 | 주차 | 상태 | 완성도 |
|------|--------|------|------|--------|
| 1 | 로그인 | 1-2주차 | ✅ 완료 | 100% (비밀번호 찾기 추가) |
| 2-1 | PM 대시보드 | 2-3주차 | ✅ 완료 | 100% (15컬럼 진행관리, 담당자 배정) |
| 2-2 | 평가자 대시보드 | 2-3주차 | ✅ 완료 | 100% (본인 물건 필터링, 진행률 표시) |
| 2-3 | 관리자 대시보드 | 2-3주차 | ✅ 완료 | 100% (전체 현황, 시스템 관리) |
| 3 | 물건 목록 | 2-3주차 | ✅ 완료 | 100% |
| 4 | 기초데이터 탭 | 3주차 | ✅ 완료 | 100% (지도, KB시세, QA, 첨부파일) |
| 5 | 등기부 탭 | 4주차 | ✅ UI 완료 | 80% (OCR 보류, 수동 입력 가능) |
| 6 | 데이터 업로드 | 3-4주차 | ✅ 완료 | 100% |
| 9 | 지도 탭 | 5-6주차 | ✅ 완료 | 100% (레이어 토글, 로드뷰) |
| 13 | 통계 대시보드 | 6주차 | ✅ 완료 | 100% |
| 17 | 차주개요 | 6주차 | ✅ 완료 | 100% (차주 정보, 담보물건, XNPV) |
| 18 | 담보총괄 | 6주차 | ✅ 완료 | 100% (집계, Loan Cap 검토) |
| 19 | Loan 상세 | 6주차 | ✅ 완료 | 100% (채권정보, Loan Cap 계산) |
| 20 | 현금흐름 집계 | 6주차 | ✅ 완료 | 100% (XNPV/IRR 계산, 민감도 분석) |
| 21 | XNPV 비교 | 6주차 | ✅ 완료 | 100% (시나리오 비교, 투자 추천) |
| 22 | 회생개요 | 6주차 | ✅ 완료 | 100% (회생 차주 현황) |
| 23 | Tool Box | 6주차 | ✅ 완료 | 100% (법원, 금융기관, 공통코드) |

---

## 🔄 남은 화면 (4-7주차)

| 화면 | 화면명 | 주차 | 상태 |
|------|--------|------|------|
| 5 | 등기부 탭 | 4주차 | ✅ UI 완료 (OCR 보류) |
| 7 | 권리분석 탭 | 4-5주차 | ✅ 완료 |
| 8 | 평가 탭 | 4-5주차 | ⬜ 대기 |
| 17 | 차주개요 | 4-5주차 | ✅ 완료 |
| 18 | 담보총괄 | 4-5주차 | ✅ 완료 |
| 19 | Loan 상세 | 4-5주차 | ✅ 완료 |
| 20 | 현금흐름 집계 | 4-5주차 | ✅ 완료 |
| 21 | XNPV 비교 | 4-5주차 | ✅ 완료 |
| 14 | 사용자 관리 | 6주차 | ⬜ 대기 |
| 15 | 설정 관리 | 6주차 | ⬜ 대기 |
| 16 | 작업 이력 | 6주차 | ⬜ 대기 |
| 10 | 선순위 관리 | 6-7주차 | ⬜ 대기 |
| 11 | 경매일정 | 6-7주차 | ⬜ 대기 |
| 12 | 공매일정 | 6-7주차 | ⬜ 대기 |
| 22 | 회생개요 | 6-7주차 | ✅ 완료 |
| 23 | Tool Box | 6-7주차 | ✅ 완료 |

---

## 📊 최근 업데이트 내역 (2025-11-27)

### 누락 화면 7개 구현 완료 (신규)
- ✅ **차주개요 (BorrowerOverview)**
  - Borrower 모델, BorrowerRepository 구현
  - DB 테이블 생성 (borrowers)
  - 차주 목록/상세, 통계, XNPV 결과, 담보물건 목록
- ✅ **담보총괄 (CollateralSummary)**
  - 차주별 담보 물건 요약 그리드
  - 합계 및 Loan Cap 검토 섹션
- ✅ **Loan 상세 (LoanDetail)**
  - Loan 모델, LoanRepository 구현
  - DB 테이블 생성 (loans)
  - 채권정보, Loan Cap 계산 (시나리오 1/2)
- ✅ **Tool Box**
  - ReferenceData 모델 (Court, FinancialInstitution, AppraisalFirm, CommonCode)
  - DB 테이블 생성 (courts, financial_institutions, appraisal_firms, common_codes)
  - 탭 구조 CRUD UI
- ✅ **현금흐름 집계 (CashFlowSummary)**
  - CashFlow 모델, XnpvCalculator 서비스
  - XNPV/IRR 계산, 민감도 분석
- ✅ **XNPV 비교 (XnpvComparison)**
  - 시나리오별 비교 그리드
  - 투자 우선순위 추천
- ✅ **회생개요 (RestructuringOverview)**
  - 회생 차주 현황, 채권 정보
- ✅ **MainWindow 네비게이션 업데이트**
  - 차주/대출 섹션 추가 (7개 메뉴)

### 등기부 탭 UI 구현 (기존)
- ✅ RegistryTab.xaml UI 구현
  - 물건 기본 정보 섹션 (지번번호, 담보물형태, 면적, 주소 일치여부)
  - 소유자 정보 그리드 (추가/삭제 기능)
  - 갑구 (소유권) 권리 그리드 (합계 표시)
  - 을구 (근저당/전세권) 권리 그리드 (합계 표시)
- ✅ Registry 모델 생성 (RegistryDocument, RegistryOwner, RegistryRight)
- ✅ RegistryRepository 생성 (CRUD 기능)
- ✅ RegistryTabViewModel 생성
- ⏳ OCR 기능은 추후 구현 예정

### 대시보드 보강 (40% → 100%)
- ✅ 15컬럼 진행관리 그리드 (약정서, 보증서, 경개개시 등)
- ✅ 역할별 UI 분기 (PM/평가자/관리자)
- ✅ 담당자 배정 드롭다운
- ✅ Excel 내보내기 기능
- ✅ 체크박스 실시간 저장

### 기초데이터 탭 보강 (50% → 100%)
- ✅ 좌측 지도 영역 (카카오맵 WebView2 연동)
- ✅ KB시세 조회 섹션 (수동 조회 + 외부 링크)
- ✅ QA 관리 섹션 (질문/답변 상태 관리)
- ✅ 첨부파일 섹션 (Supabase Storage 연동)
- ✅ 감정평가 상세 정보 (토지/건물/기계 분리)

### 지도 탭 보강 (80% → 100%)
- ✅ 레이어 선택 체크박스 (기본/위성/지적도)
- ✅ 로드뷰 오버레이 표시
- ✅ 줌 컨트롤 버튼
- ✅ 좌표 정보 표시
- ✅ 외부에서 열기 버튼

### 로그인 보강 (95% → 100%)
- ✅ 비밀번호 찾기 링크
- ✅ 비밀번호 재설정 이메일 전송
- ✅ 재설정 모드 UI

---

## 🔑 필요한 API 키

| API | 용도 | 발급처 |
|-----|------|--------|
| 카카오맵 JavaScript | 지도 표시 | [developers.kakao.com](https://developers.kakao.com/) |
| 국토교통부 실거래가 | 시세 조회 | [data.go.kr](https://www.data.go.kr/data/15057511/openapi.do) |

> API 키 설정 방법: [docs/setup/API_KEYS_SETUP.md](../setup/API_KEYS_SETUP.md) 참고

---

## 📊 주차별 요약

| 주차 | 완료 화면 수 | 누적 진행률 |
|------|-------------|------------|
| 1-2주차 | 1개 | 4% |
| 3주차 | 5개 | 22% |
| 4주차 | 8개 | 35% |
| 5주차 | 15개 | 65% |
| 6주차 | 20개 | 87% |
| 7주차 | 23개 | 100% |

---

## 📞 중요 참고사항

- **화면 기획서**: `docs/converted_sheets/` (40+ 시트)
- **요구사항**: `docs/ours/NPlogic_1_요구사항정리.md`
- **디자인 가이드**: `docs/design/DESIGN_SYSTEM.md`
- **DB 스키마**: `docs/database/SCHEMA.md`
- **화면 가이드**: `docs/screens/SCREEN_GUIDE.md`

**이 파일을 매일 업데이트하여 진행 상황을 추적하세요!** ✨

