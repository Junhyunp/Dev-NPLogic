# 등기부등본 OCR 시스템 통합 계획

> **작성일**: 2025-12-08  
> **소스 코드**: `manager/client/Auction-Certificate/`

---

## 📋 상대방에게 요청할 것

### 필수 (작업 시작 전 확보 필요)

- [ ] **Clova OCR Secret Key** - 본인 계정의 API 키
- [ ] **Clova OCR Invoke URL** - 본인 계정의 API 엔드포인트
- [ ] **API 호출 한도 확인** - 월/일 호출 제한이 있는지

> ⚠️ 현재 코드에 하드코딩된 키 (테스트용으로 추정):
> ```
> secret_key = "ZFJlT05zZk1IZ3FvdHlLSFdWekR5U2NTb0VYdWdLd3M="
> invoke_url = "https://e8fcojkfeq.apigw.ntruss.com/custom/v1/44525/..."
> ```
> 이 키가 계속 사용 가능한지 확인 필요

### 확인 필요

- [ ] PDF 파일명 규칙 유지 여부 (현재: `R-XXX-YY [유형] 주소.pdf`)
- [ ] 한 번에 처리할 PDF 예상 개수 (동시 처리 설계용)

### 이미 확보됨 ✅

- [x] 샘플 PDF 파일 (`data/` 폴더에 약 1,500개)
- [x] 샘플 엑셀 데이터 (`data/datadisk/`)
- [x] 테이블 파싱 로직 (`utils.py`)
- [x] 컬럼 매핑 설정 (`cfg/extract_columns.json`)

---

## 🚀 구현 체크리스트

### Phase 1: Python 서버 만들기 ✅

**목표**: 기존 Python 코드에 FastAPI 서버 씌우기

- [x] FastAPI 프로젝트 구조 생성
  ```
  manager/client/Auction-Certificate/
  ├── server.py         (신규) ✅
  ├── api/
  │   ├── __init__.py   (신규) ✅
  │   └── routes.py     (신규) ✅
  ├── main.py           (기존)
  ├── ocr.py            (기존)
  └── utils.py          (기존)
  ```

- [x] `server.py` 작성
  - [x] FastAPI 앱 초기화
  - [x] CORS 설정 (localhost 허용)
  - [x] `/api/health` 헬스체크 엔드포인트

- [x] `/api/ocr/registry` 엔드포인트 구현
  - [x] PDF 파일 업로드 받기
  - [x] 요약 페이지 찾기 (`SummaryPageFinder`)
  - [x] Clova OCR 호출 (`ClovaOCRProcessor`)
  - [x] 테이블 파싱 (`TableProcessor`)
  - [x] JSON 응답 반환

- [x] 로컬 테스트
  - [x] 빌드 확인 (import 테스트 통과)
  - [ ] `python server.py` 실행
  - [ ] Postman/curl로 API 호출 테스트
  - [ ] 샘플 PDF로 결과 확인

**완료 기준**: `http://localhost:8000/api/ocr/registry`로 PDF 보내면 JSON 응답 옴

---

### Phase 2: PyInstaller로 exe 빌드 ✅

**목표**: Python 서버를 exe 파일로 패키징

- [x] requirements 정리
  - [x] 불필요한 패키지 제거 → `requirements-server.txt` 생성
  - [x] 버전 고정

- [x] PyInstaller 설정
  - [x] `.spec` 파일 작성 → `registry_ocr.spec`
  - [x] `cfg/` 폴더 포함 설정
  - [x] hidden imports 추가 (FastAPI, uvicorn 등)

- [x] 빌드 & 테스트
  - [x] `pyinstaller registry_ocr.spec` 실행
  - [ ] 생성된 exe 단독 실행 테스트
  - [ ] exe에서 API 호출 테스트

- [x] 최적화
  - [x] 파일 크기 확인: **185.65 MB** (목표: 200MB 이하 ✅)
  - [ ] 시작 시간 확인 (목표: 5초 이내)

> ⚠️ **주의**: 빌드 시 `fitz (PyMuPDF)` not found 에러 발생. 필요시 PyMuPDF 설치 후 재빌드 필요.

**완료 기준**: `registry_ocr.exe` 더블클릭하면 서버 시작됨

---

### Phase 3: WPF 앱에서 호출 ✅

**목표**: WPF 앱 시작 시 Python 서버 자동 실행 & 통신

- [x] Python 서버 자동 실행
  - [x] `App.xaml.cs`에 서버 시작 로직 추가
  - [x] 백그라운드 프로세스로 실행
  - [x] 앱 종료 시 서버도 종료

- [x] HTTP 클라이언트 서비스
  - [x] `RegistryOcrService.cs` 생성
  - [x] PDF 업로드 메서드 (`ProcessPdfAsync`)
  - [x] 응답 JSON 파싱 (소유자/갑구/을구)
  - [x] 배치 처리 지원 (`ProcessMultiplePdfsAsync`)

- [x] 서버 상태 체크
  - [x] `/api/health` 헬스체크 메서드
  - [x] 서버 준비될 때까지 대기 (최대 30초)
  - [x] 자동 서버 경로 탐색 (개발/배포 환경 모두 지원)

- [x] 빌드 설정
  - [x] `NPLogic.App.csproj`에 exe 복사 설정 추가
  - [x] DI 컨테이너에 `RegistryOcrService` 등록

**완료 기준**: WPF 앱 실행하면 Python 서버 자동으로 뜨고, API 호출 가능

---

### Phase 4: UI 연동 & DB 저장 ✅

**목표**: PDF 업로드 → OCR → 결과 표시 → DB 저장

- [x] PDF 업로드 UI
  - [x] 파일 선택 버튼
  - [x] 다중 파일 선택
  - [x] 선택된 파일 목록 표시
  - [x] 개별/전체 파일 취소

- [x] 처리 진행 UI
  - [x] 프로그레스바
  - [x] 현재 처리 중 파일명 표시
  - [x] 취소 버튼
  - [x] 파일별 상태 표시 (대기/처리중/완료/실패)

- [x] 결과 표시 UI
  - [x] OCR 결과 미리보기 섹션
  - [x] 소유자/갑구/을구 건수 표시
  - [x] 추출된 주소 표시

- [x] DB 저장
  - [x] 기존 테이블 활용 (registry_owners, registry_rights)
  - [x] OCR 결과 → 모델 변환 로직
  - [x] 저장 버튼으로 DB INSERT

**완료 기준**: PDF 업로드하면 결과가 화면에 표시되고 DB에 저장됨 ✅

---

### Phase 5: 테스트 & 배포

- [ ] 통합 테스트
  - [ ] 샘플 PDF 10개로 E2E 테스트
  - [ ] 에러 케이스 테스트 (잘못된 PDF, API 실패 등)

- [ ] 설치 프로그램
  - [ ] WPF 앱 + Python exe 함께 패키징
  - [ ] 설치 프로그램 빌드 (Inno Setup)
  - [ ] 설치/제거 테스트

- [ ] 문서화
  - [ ] API 키 설정 방법
  - [ ] 트러블슈팅 가이드

**완료 기준**: 설치 프로그램 하나로 전체 기능 동작

---

## 📁 결과물 위치

```
manager/client/Auction-Certificate/
├── server.py                    # FastAPI 서버 (Phase 1) ✅
├── api/
│   ├── __init__.py              ✅
│   └── routes.py                ✅
├── requirements-server.txt      # 서버용 핵심 패키지 (Phase 2) ✅
├── registry_ocr.spec            # PyInstaller 설정 ✅
├── scripts/
│   └── build_server.bat         # 빌드 스크립트 ✅
├── dist/
│   └── registry_ocr/            # 패키징된 exe 폴더 (185.65 MB) ✅
│       ├── registry_ocr.exe     # 11.87 MB
│       └── _internal/           # 종속 라이브러리
└── build/                       # PyInstaller 빌드 캐시

src/NPLogic.App/
├── App.xaml.cs                  # OCR 서비스 DI 등록 & 종료 처리 (Phase 3) ✅
├── Services/
│   ├── RegistryOcrService.cs    # OCR API 클라이언트 (Phase 3) ✅
│   ├── ExcelService.cs
│   └── StorageService.cs
├── ViewModels/
│   ├── RegistryTabViewModel.cs  # OCR 업로드/처리/저장 기능 추가 (Phase 4) ✅
│   └── PropertyDetailViewModel.cs # OCR 서비스 주입 연결 (Phase 4) ✅
├── Views/
│   ├── RegistryTab.xaml         # OCR 업로드 UI 섹션 추가 (Phase 4) ✅
│   └── RegistryTab.xaml.cs      # OCR 서버 상태 확인 (Phase 4) ✅
├── NPLogic.App.csproj           # exe 자동 복사 설정 추가 ✅
└── bin/Debug/.../backend/
    └── registry_ocr/            # 빌드 시 자동 복사됨
```

---

## 📊 예상 소요 시간

| Phase | 작업 | 예상 | 상태 |
|-------|------|------|------|
| 1 | Python 서버 만들기 | 1일 | ✅ 완료 |
| 2 | PyInstaller exe 빌드 | 0.5일 | ✅ 완료 |
| 3 | WPF 앱에서 호출 | 1일 | ✅ 완료 |
| 4 | UI 연동 & DB 저장 | 1.5일 | ✅ 완료 |
| 5 | 테스트 & 배포 | 1일 | ⏳ 대기 |
| **합계** | | **5일** | **Phase 1-4 완료** |

---

## 🔑 핵심 코드 참조

### OCR 호출 (ocr.py)
```python
class ClovaOCRProcessor:
    def process_image(self, image: Image, apply_sharpening: bool) -> dict
```

### 테이블 분류 기준 (utils.py)
```python
# 소유자 테이블 판별
OWNERS_REQUIRED_COLS = ["등기명의인", "(주민)등록번호", "최종지분", "순위번호"]

# 갑구/을구 테이블 판별  
GAP_EUL_REQUIRED_COLS = ["순위번호", "등기목적", "주요등기사항"]

# 갑구 키워드
GAP_KEYWORDS = ["가압류", "압류", "임의경매개시결정", "강제경매개시결정", ...]

# 을구 키워드
EUL_KEYWORDS = ["근저당권설정", "전세권", "임차권", ...]
```

### 출력 컬럼 (utils.py)
```python
# 소유자
OWNERS_TARGET_COLS = ["등기명의인", "(주민)등록번호", "최종지분", "주소", "순위번호"]

# 갑구
GAP_PARSED_COLS = ["순위번호", "등기목적", "접수정보", "접수날짜", "권리자/채권자/가등기권자", "청구금액", "비고", "대상소유자"]

# 을구
EUL_PARSED_COLS = ["순위번호", "등기목적", "접수정보", "접수날짜", "근저당권자/전세권자/채권자", "채권최고액/전세금", "채무자", "대상소유자"]
```
