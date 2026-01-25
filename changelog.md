# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2025-01-25

#### 물건정보 중복 키 에러 수정 (CollateralNumber 처리)

**문제점**
- IBK 엑셀에서 "물건번호" 컬럼이 `collateral_number`로 매핑됨 ("물건 일련번호"와 별도)
- `property.PropertyNumber`가 비어있고 `property.CollateralNumber`가 "2" 같은 숫자일 때
- 기존 로직이 CollateralNumber를 그대로 사용하여 여러 물건이 동일한 property_number "2"를 가짐
- 결과: `properties_project_id_property_number_key` 중복 키 에러

**해결 방법**
- CollateralNumber도 숫자만 있을 경우 차주번호와 조합하도록 로직 개선
- 예: 차주 R-007의 물건번호 2 → "R-007-2"로 생성

**수정된 물건번호 설정 로직**
1. PropertyNumber가 비숫자 문자 포함 → 그대로 사용 (예: "2025타경12345")
2. PropertyNumber가 숫자만 → 차주번호 + PropertyNumber 조합 (예: "R-007-2")
3. CollateralNumber가 비숫자 문자 포함 → 그대로 사용
4. CollateralNumber가 숫자만 → 차주번호 + CollateralNumber 조합 (예: "R-007-2")
5. 모두 없으면 → 자동생성 (예: "R-007-P1")

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs` - Property case 물건번호 설정 로직 개선

---

### 2025-01-24

#### 프로그램 삭제 기능 개선 - Cascade Delete 구현

프로그램 삭제 시 FK 제약조건 위반 문제를 해결하기 위해 Cascade Delete 기능 추가

**문제점**
- 프로그램에 연결된 데이터(차주, 대출, 물건 등)가 있을 경우 FK 제약조건으로 삭제 실패
- 삭제 실패 시 사용자에게 에러 메시지가 표시되지 않음

**해결 방법**
- 3버튼 다이얼로그 추가:
  - [예] - 연결된 모든 데이터와 함께 삭제 (Cascade Delete)
  - [아니오] - 프로그램만 삭제 시도 (연결 데이터 있으면 실패)
  - [취소] - 삭제 취소

**Cascade Delete 삭제 순서** (FK 제약조건 고려)
1. registry_sheet_data (property_id FK)
2. right_analysis (property_id FK)
3. credit_guarantees (borrower_id FK)
4. loans (borrower_id FK)
5. borrower_restructuring (borrower_id FK)
6. properties (program_id FK)
7. borrowers (program_id FK)
8. program_sheet_mappings (program_id FK)
9. programs

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`
  - `DeleteProgram` 메서드: 3버튼 다이얼로그 추가
  - `CascadeDeleteProgramAsync` 메서드 추가
- `src/NPLogic.Data/Repositories/PropertyRepository.cs` - `DeleteByProgramIdAsync()` 추가
- `src/NPLogic.Data/Repositories/BorrowerRepository.cs` - `DeleteByProgramIdAsync()` 추가
- `src/NPLogic.Data/Repositories/LoanRepository.cs` - `DeleteByBorrowerIdsAsync()` 추가
- `src/NPLogic.Data/Repositories/BorrowerRestructuringRepository.cs` - `DeleteByBorrowerIdsAsync()` 추가
- `src/NPLogic.Data/Repositories/CreditGuaranteeRepository.cs` - `DeleteByBorrowerIdsAsync()` 추가
- `src/NPLogic.Data/Repositories/RightAnalysisRepository.cs` - `DeleteByPropertyIdsAsync()` 추가
- `src/NPLogic.Data/Repositories/RegistrySheetDataRepository.cs` - `DeleteByPropertyIdsAsync()` 추가

---

#### 데이터디스크 업로드 - 등기부등본/신용보증서 처리 로직 구현

`ProgramManagementViewModel.ProcessSheetAsync`에 등기부등본정보와 신용보증서 시트 처리 로직 추가

**등기부등본정보 (RegistryDetail)**
- `MapRowToRegistrySheetData()` 메서드 추가
- 컬럼 매핑: 차주일련번호, 차주명, 물건번호, 지번번호, 담보소재지1~4

**신용보증서 (Guarantee)**
- `MapRowToCreditGuarantee()` 메서드 추가
- 컬럼 매핑: 자산유형, 차주일련번호, 차주명, 계좌일련번호, 보증기관, 보증종류, 보증서번호, 보증비율, 환산후 보증잔액, 관련 대출채권 계좌번호
- 차주ID 자동 연결 (borrower_id FK)

**변경된 파일**
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`
  - `RegistrySheetDataRepository`, `CreditGuaranteeRepository` 필드 및 DI 주입 추가
  - `ProcessSheetAsync`에 `SheetType.RegistryDetail`, `SheetType.Guarantee` case 추가
  - `MapRowToRegistrySheetData()`, `MapRowToCreditGuarantee()` 매핑 메서드 추가
- `src/NPLogic.App/App.xaml.cs` - ProgramManagementViewModel DI 등록에 Repository 추가

---

#### 데이터디스크 업로드 버그 수정

**회생차주정보 FK 제약조건 위반 수정**
- 원인: `GetByBorrowerNumberAsync`가 program_id 없이 조회하여 다른 프로그램의 차주와 혼동
- 해결: `GetByProgramIdAndBorrowerNumberAsync` 메서드 추가 및 사용

**물건정보 중복 키 위반 수정**
- 원인: "번호" 컬럼 매칭이 "일련번호"도 매칭하여 물건번호가 1, 2, 3... 으로 잘못 설정됨
- 해결: 차주번호 + 물건번호 조합으로 유니크한 property_number 생성 (예: "R-007-2")
- 수정 위치: `ProcessSheetAsync`의 Property case에서 물건번호 설정 로직 개선
  - 경매사건번호(비숫자 포함) → 그대로 사용
  - 숫자만 있는 물건번호 → 차주번호와 조합 (예: "R-007-2")
  - CollateralNumber만 있으면 → 그대로 사용
  - 없으면 → 자동생성

**변경된 파일**
- `src/NPLogic.Data/Repositories/BorrowerRepository.cs` - `GetByProgramIdAndBorrowerNumberAsync()` 추가
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs`
  - 회생/대출 시트 처리 시 program_id로 차주 조회
  - Property 시트 물건번호 설정 로직 개선 (숫자만 있는 경우 차주번호와 조합)

---

### 2025-01-22

#### 데이터디스크 업로드 - 4개 시트 대표컬럼 구현

Excel 데이터디스크에서 Supabase로 데이터 업로드 시 사용되는 4개 시트의 대표컬럼 매핑 체계 구현

**1. 차주일반정보 (BorrowerGeneral) - 9개 대표컬럼**
- 자산유형, 차주일련번호, 차주명, 관련차주, 차주형태, 미상환원금잔액, 미수이자, 근저당권설정액, 비고
- DB 테이블: borrowers

**2. 회생차주정보 (BorrowerRestructuring) - 14개 대표컬럼**
- 자산유형, 차주일련번호, 차주명, 세부진행단계, 관할법원, 회생사건번호, 보전처분일, 개시결정일, 채권신고일, 인가/폐지결정일, 업종, 상장/비상장, 종업원수, 설립일
- DB 테이블: borrower_restructurings

**3. 채권일반정보 (Loan) - 14개 대표컬럼**
- 차주일련번호, 차주명, 대출일련번호, 대출과목, 계좌번호, 정상이자율, 연체이자율, 최초대출일, 최초대출원금, 환산된 대출잔액, 가지급금, 미상환원금잔액, 미수이자, 채권액 합계
- DB 테이블: loans
- 자동 계산 규칙:
  - 대출일련번호가 숫자만 있으면 "차주번호-대출일련번호" 형식으로 변환
  - 정상이자율만 있으면 연체이자율 = 정상이자율 + 3%
  - 연체이자율만 있으면 정상이자율 = 연체이자율 - 3%
  - 채권액 합계 없으면 = 대출잔액 + 가지급금 + 미수이자

**4. 물건정보 (Property) - 56개 대표컬럼**
- DB 테이블: properties
- Supabase migration 적용: add_property_representative_columns, add_properties_column_comments
- 컬럼 카테고리:
  - 기본 정보 (6개): 자산유형, 차주일련번호, 차주명, 물건일련번호, 물건종류, 물건번호
  - 주소 (4개): 담보소재지 1~4
  - 면적/금액 (4개): 대지면적, 건물면적, 기계기구, 공담물건금액
  - 선순위 정보 (12개): 선순위설정액, 주택/상가 소액보증금, 소액보증금, 주택/상가 임차보증금, 임차보증금, 임금채권, 당해세, 조세채권, 기타, 합계
  - 감정평가 (9개): 구분, 일자, 기관, 토지/건물/기계 평가액, 제시외, 합계, KB시세
  - 경매 기본 (2개): 개시여부, 관할법원
  - 경매 선행 (5개): 신청기관, 개시일자, 사건번호, 배당요구종기일, 청구금액
  - 경매 후행 (5개): 신청기관, 개시일자, 사건번호, 배당요구종기일, 청구금액
  - 경매 기일/결과 (9개): 최초법사가, 최초/최종/차기 경매기일, 최종회차/결과, 낙찰금액, 최저입찰금액들, 비고

**변경된 파일**
- `src/NPLogic.Core/Models/Property.cs` - 56개 대표컬럼 속성 추가
- `src/NPLogic.Core/Models/BorrowerRestructuring.cs` - 회생차주 속성
- `src/NPLogic.Data/Repositories/PropertyRepository.cs` - 테이블 매핑 업데이트
- `src/NPLogic.Data/Repositories/LoanRepository.cs` - 채권 매핑
- `src/NPLogic.App/Services/SheetMappingConfig.cs` - 4개 시트 매핑 규칙
- `src/NPLogic.App/Services/DataDiskUploadService.cs` - GetRepresentativeColumns, GetDbColumnDisplayName, MapRowTo* 메서드

---

#### 시트 타입 관리 개선

**대표 시트 타입 표준화**
- 기존 7개에서 6개로 정리 (CollateralSetting/담보설정정보 제거)
- 표준 시트 타입:
  - 차주일반정보 (BorrowerGeneral) - Sheet A
  - 회생차주정보 (BorrowerRestructuring) - Sheet A-1, Sheet F
  - 채권일반정보 (Loan) - Sheet B, Sheet B-1
  - 물건정보 (Property) - Sheet C-1
  - 등기부등본정보 (RegistryDetail) - Sheet C-2
  - 신용보증서 (Guarantee) - Sheet D

**시트 자동 감지 로직 개선**
- 데이터 시트 제외: `_YYMMDD` 패턴으로 끝나는 시트 무시 (예: "신용보증서_220523")
- 은행별 시트명 패턴 지원:
  - KB: "Sheet A(차주일반정보)", "Sheet B(채권일반정보)" 등
  - IBK: "Sheet A", "Sheet A-1", "Sheet B" 등
  - NH: "Sheet A", "Sheet F" (회생), "Sheet B-1" 등
  - SHB: "1.차주일반", "2.매각대상채권", "3.담보물건" 등
- 너무 광범위한 키워드 매칭 제거 (예: "담보"만으로 매칭되는 문제 수정)

**수동 시트 타입 선택 기능 추가**
- 시트 목록에서 시트 타입을 드롭다운으로 변경 가능
- 자동 감지 실패 시 또는 사용자가 다른 타입으로 지정하고 싶을 때 사용
- 시트 타입 변경 시 체크박스 자동 선택/해제
- 동일 프로그램 내 중복 시트 타입 방지 (다른 시트에서 사용 중이면 기존 시트는 Unknown으로 변경)

#### 변경된 파일
- `src/NPLogic.App/Services/ExcelService.cs` - DetectSheetType 로직 개선
- `src/NPLogic.App/Services/SheetMappingConfig.cs` - CollateralSetting 제거
- `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs` - ChangeSheetType 메서드 개선
- `src/NPLogic.App/ViewModels/DataUploadViewModel.cs` - SelectableSheetInfo에 SelectedSheetType 추가
- `src/NPLogic.App/Views/ProgramManagementView.xaml` - 시트 타입 ComboBox 추가
- `src/NPLogic.App/Views/ProgramManagementView.xaml.cs` - ComboBox 이벤트 핸들러 추가
- `src/NPLogic.Core/Models/ProgramSheetMapping.cs` - DataDiskSheetType enum 정리
- `src/NPLogic.App/Views/ColumnMappingDialog.xaml.cs` - SheetTypeDisplay 업데이트
- `src/NPLogic.Services/DataDiskUploadService.cs` - 시트 타입 변환 메서드 정리

---

#### 등기부등본정보 (RegistryDetail) 시트 업로드 구현

데이터디스크 5번째 시트인 등기부등본정보 업로드 기능 구현

**배경**
- 기존 `registry_documents`, `registry_rights`, `registry_owners` 테이블은 OCR PDF 처리용
- 데이터디스크 Excel 업로드용 별도 테이블 필요

**등기부등본정보 (RegistryDetail) - 8개 대표컬럼**
- 차주일련번호, 차주명, 물건번호, 지번번호, 담보소재지1, 담보소재지2, 담보소재지3, 담보소재지4
- DB 테이블: `registry_sheet_data` (신규)
- 물건(properties)과 1:N 관계 (property_id FK, ON DELETE CASCADE)

**Supabase Migration**
- 테이블명: `registry_sheet_data`
- 컬럼: id, property_id, borrower_number, borrower_name, property_number, jibun_number, address_province, address_city, address_district, address_detail, created_at, updated_at
- 인덱스: property_id, borrower_number

**변경된 파일**
- `src/NPLogic.Core/Models/RegistrySheetData.cs` - 신규 모델 클래스
- `src/NPLogic.Data/Repositories/RegistrySheetDataRepository.cs` - 신규 Repository (CRUD)
- `src/NPLogic.Data/Repositories/PropertyRepository.cs` - `GetByBorrowerAndPropertyNumberAsync()` 메서드 추가
- `src/NPLogic.App/App.xaml.cs` - DI 등록 (RegistrySheetDataRepository)
- `src/NPLogic.App/Services/SheetMappingConfig.cs` - `GetRegistryDetailMappings()` 대표컬럼 매핑
- `src/NPLogic.App/Services/DataDiskUploadService.cs` - `ProcessRegistryDetailAsync()`, `MapRowToRegistrySheetData()` 추가
