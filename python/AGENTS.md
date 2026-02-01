# python/ - Python 백엔드 서버

NPLogic C# WPF 애플리케이션을 지원하는 Python 백엔드 서버입니다.

**상위 문서**: [../AGENTS.md](../AGENTS.md)

---

## 개요

Python 기반 보조 서버로 다음 기능을 제공합니다:

- **OCR 처리**: 등기부등본 PDF 파일에서 데이터 추출
- **유사물건 추천**: 규칙 기반 경매 물건 추천 엔진

C# 애플리케이션은 HTTP API를 통해 이 서버와 통신합니다.

---

## 프로젝트 구조

```
python/
├── server.py                    # FastAPI 메인 서버
├── ocr_processor.py             # 등기부등본 OCR 프로세서
├── recommend_processor.py       # 유사물건 추천 프로세서
├── requirements.txt             # Python 패키지 의존성
├── nplogic_backend.spec         # PyInstaller 설정 (exe 빌드용)
├── README.md                    # 사용법 가이드
└── recommend/                   # 추천 엔진 모듈
    ├── __init__.py
    ├── recommend.py             # 추천 로직 (규칙 기반 필터링)
    ├── utils.py                 # 유틸리티 (날짜, 거리 계산, 파생값)
    └── config.yaml              # 추천 규칙 설정
```

---

## 핵심 컴포넌트

### 1. server.py - FastAPI 서버

**역할**: HTTP API 엔드포인트 제공

**주요 엔드포인트**:

| 엔드포인트 | 메서드 | 설명 |
|-----------|--------|------|
| `/api/health` | GET | 서버 상태 확인 |
| `/api/ocr/registry` | POST | 등기부등본 PDF OCR 처리 |
| `/api/recommend/similar` | POST | 유사 물건 추천 |

**기술 스택**:
- FastAPI: 웹 프레임워크
- Uvicorn: ASGI 서버
- CORS 미들웨어: C# 클라이언트 통신 허용

**C# 연동**:
- `PythonBackendService.cs`에서 HTTP 요청

### 2. ocr_processor.py - OCR 프로세서

**역할**: 등기부등본 PDF에서 구조화된 데이터 추출

**입력**: PDF 파일 경로 또는 업로드 파일

**출력** (JSON):
```json
{
  "success": true,
  "registry_type": "토지",
  "registry_number": "1234-5678-901234",
  "owners": [...],
  "rights": [...],
  "land_info": {...}
}
```

**기술**:
- pdf2image: PDF → 이미지 변환
- pytesseract: OCR (Tesseract 엔진)
- Pillow: 이미지 전처리

**중요**: Tesseract OCR 설치 필요 (Windows: `C:\Program Files\Tesseract-OCR`)

**현재 상태**: 샘플 구현 (실제 클라이언트 OCR 코드로 대체 예정)

### 3. recommend_processor.py - 추천 프로세서

**역할**: 대상 물건과 유사한 경매 물건 추천

**입력** (JSON):
```json
{
  "subject": {
    "property_id": "...",
    "address": "...",
    "usage": "아파트",
    "building_area": 85.5,
    "latitude": 37.5,
    "longitude": 127.0,
    ...
  },
  "candidates_source": "supabase",
  "rule_index": 1,
  "similar_land": false,
  "region_scope": "big",
  "topk": 10
}
```

**출력**:
```json
{
  "success": true,
  "subject": {...},
  "rule_results": {
    "1": [추천물건1, 추천물건2, ...]
  },
  "total_count": 10,
  "config": {...}
}
```

**데이터 소스**:
- `supabase`: Supabase `auction_cases` 테이블
- `json`: JSON 파일 (테스트용)
- `excel`: Excel 파일 (백업용)

**환경변수**:
- `SUPABASE_URL`: Supabase 프로젝트 URL
- `SUPABASE_KEY`: Supabase API 키

---

## recommend/ 모듈

### recommend.py - 규칙 기반 추천 엔진

**핵심 로직**: 카테고리별 규칙에 따라 후보군 필터링 및 정렬

**카테고리**:
1. `APT_OFFICETEL`: 아파트/오피스텔
2. `ROWHOUSE_MULTI`: 연립/다세대
3. `RETAIL_OFFICE_APT_FACTORY`: 근린상가/사무실/아파트형공장
4. `PLANT_WAREHOUSE_ETC`: 공장/창고/농가 등
5. `OTHER_BIG`: 기타 대형 시설

**필터링 단계**:
1. 지역 필터 (시/도 또는 시/군/구)
2. 용도 필터 (동일 용도만)
3. 시간 윈도우 (낙찰일 기준 X일 이내)
4. 거리 반경 (하버사인 거리 계산)
5. 동일 아파트/건물 (선택)
6. 값 범위 필터 (면적, 단가, 감정가 ±%)

**정렬**:
- 최신순 (낙찰일 내림차순) → 가까운 순 (거리 오름차순)

**주요 함수**:
- `recommend_by_rule()`: 특정 규칙 적용
- `recommend_all_rules()`: 모든 규칙 적용
- `load_config()`: YAML 설정 로드
- `filter_by_*()`: 각종 필터링 함수

### utils.py - 유틸리티

**기능**:

1. **날짜/숫자 파싱**
   - `parse_date()`: 다양한 형식 → datetime
   - `to_days_from_epoch()`: 1970-01-01 기준 일수
   - `safe_float()`: 쉼표 포함 문자열 → float

2. **거리 계산**
   - `haversine_distance_m()`: 위경도 간 하버사인 거리 (미터)

3. **파생값 계산**
   - `derive_fields()`: 평당 단가, 총감정가 계산
   - 건물 단가 = 건물 감정가 / (건물 면적 / 3.305785)
   - 토지 단가 = 토지 감정가 / (토지 면적 / 3.305785)

4. **카테고리 판별**
   - `category_from_usage()`: 물건 용도 → 카테고리

5. **DataFrame 보강**
   - `ensure_derived_columns()`: 파생 컬럼 추가
   - `ensure_auction_days()`: auction_days 컬럼 추가

**상수**:
- `PYEONG = 3.305785`: 1평 = 3.305785㎡
- `EPOCH = datetime(1970, 1, 1)`: 에포크 기준일

### config.yaml - 추천 규칙 설정

**구조**:
```yaml
rules:
  APT_OFFICETEL:
    - name: "1순위"
      time_window_days: 365
      radius_m: 0
      require_same_apartment: true
      filters:
        building_area_pct: 0.20
    - name: "2순위"
      ...

  PLANT_WAREHOUSE_ETC:
    default:
      - name: "1순위"
        ...
    land_like:
      - name: "1순위"
        ...
```

**필터 파라미터**:
- `time_window_days`: 낙찰일 기준 ±X일
- `radius_m`: 거리 반경 (미터), 0이면 무제한
- `require_same_apartment`: 동일 아파트 단지 필수 여부
- `require_same_building`: 동일 건물 필수 여부
- `filters`: 값 범위 필터
  - `building_area_pct`: 건물 면적 ±%
  - `land_area_pct`: 토지 면적 ±%
  - `building_unit_price_pct`: 건물 평당 단가 ±%
  - `total_appraisal_price_pct`: 총 감정가 ±%

**카테고리별 규칙 수**:
- APT_OFFICETEL: 10개
- ROWHOUSE_MULTI: 12개
- RETAIL_OFFICE_APT_FACTORY: 9개
- PLANT_WAREHOUSE_ETC: 9개 (default/land_like 각각)
- OTHER_BIG: 9개 (default/land_like 각각)

---

## 실행 방법

### 개발 환경

1. **의존성 설치**:
   ```bash
   cd python
   pip install -r requirements.txt
   ```

2. **Tesseract OCR 설치** (Windows):
   - [Tesseract 다운로드](https://github.com/UB-Mannheim/tesseract/wiki)
   - 경로: `C:\Program Files\Tesseract-OCR\tesseract.exe`

3. **환경변수 설정**:
   ```bash
   export SUPABASE_URL="https://nlddampvgxamaukflqhd.supabase.co"
   export SUPABASE_KEY="your-supabase-key"
   ```

4. **서버 실행**:
   ```bash
   python server.py
   # 기본 포트: 8000
   ```

### 단독 실행

**OCR 단독 실행**:
```bash
python ocr_processor.py <pdf_file_path>
```

**추천 단독 실행**:
```bash
python recommend_processor.py <subject_json_path>
# 또는
python recommend_processor.py --subject-json '{"property_id": "...", ...}'
```

### 배포 (PyInstaller)

```bash
pyinstaller nplogic_backend.spec
# dist/ 폴더에 실행 파일 생성
```

---

## C# 연동

### PythonBackendService.cs

**위치**: `src/NPLogic.App/Services/PythonBackendService.cs`

**패턴**: Singleton (`PythonBackendService.Instance`)

**주요 메서드**:
- `StartServerAsync()`: Python 서버 프로세스 시작
- `StopServer()`: 서버 종료
- `IsServerRunning()`: 서버 상태 확인
- `ProcessOcrAsync(string pdfPath)`: OCR 요청
- `GetSimilarPropertiesAsync(object subject, ...)`: 추천 요청

**통신 방식**:
- HTTP API (JSON)
- 기본 URL: `http://localhost:8000`

---

## AI 에이전트 가이드

### 코드 수정 시 주의사항

1. **OCR 정확도 개선**:
   - `ocr_processor.py` 수정
   - 전처리 로직 강화 (이미지 정규화, 노이즈 제거)
   - 후처리 정규화 (정규표현식 패턴 개선)

2. **추천 알고리즘 수정**:
   - `recommend/recommend.py`: 필터링 로직
   - `recommend/utils.py`: 거리/파생값 계산
   - `recommend/config.yaml`: 규칙 조정

3. **새 엔드포인트 추가**:
   - `server.py`에 라우트 추가
   - Pydantic 모델 정의 (Request/Response)
   - C# `PythonBackendService.cs`에 메서드 추가

### 테스트

**단위 테스트** (권장 추가):
```python
# tests/test_recommend.py
def test_category_from_usage():
    assert category_from_usage("아파트") == "APT_OFFICETEL"

def test_haversine_distance():
    dist = haversine_distance_m(37.5, 127.0, 37.51, 127.01)
    assert dist > 0
```

**통합 테스트**:
```bash
# 서버 실행 후
curl -X POST http://localhost:8000/api/health
curl -X POST http://localhost:8000/api/recommend/similar \
  -H "Content-Type: application/json" \
  -d @test_subject.json
```

### 성능 최적화

**병목 지점**:
1. Supabase 쿼리 (네트워크 I/O)
2. DataFrame 필터링 (대량 데이터)
3. OCR 처리 (CPU 집약)

**개선 방안**:
- Supabase 쿼리: 인덱스 활용, 쿼리 필터링 강화
- DataFrame: NumPy 벡터화, 멀티프로세싱
- OCR: 병렬 처리 (concurrent.futures)

---

## 의존성

### Python 패키지

```txt
fastapi==0.115.6          # 웹 프레임워크
uvicorn[standard]==0.34.0 # ASGI 서버
python-multipart==0.0.19  # 파일 업로드

pytesseract==0.3.10       # OCR
Pillow==10.1.0            # 이미지 처리
pdf2image==1.16.3         # PDF → 이미지
PyPDF2==3.0.1             # PDF 파싱

pandas==2.1.4             # 데이터 처리
numpy==1.26.2             # 수치 계산
PyYAML==6.0.1             # 설정 파일

supabase==2.3.4           # Supabase 클라이언트
```

### 외부 도구

- **Tesseract OCR**: 텍스트 인식 엔진
- **Poppler**: PDF → 이미지 변환 (pdf2image 의존성)

---

## 문제 해결

### OCR 오류

**증상**: `TesseractNotFoundError`

**해결**:
```python
# ocr_processor.py 상단에 추가
import pytesseract
pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'
```

### Supabase 연결 실패

**증상**: `후보군 데이터가 없습니다`

**확인 사항**:
1. 환경변수 설정: `SUPABASE_URL`, `SUPABASE_KEY`
2. 네트워크 연결
3. Supabase 테이블 존재: `auction_cases`

### 포트 충돌

**증상**: `Address already in use`

**해결**:
```bash
# 다른 포트 사용
PORT=8001 python server.py
```

---

## 향후 개선 사항

1. **OCR 고도화**:
   - 딥러닝 모델 적용 (EasyOCR, PaddleOCR)
   - 표 구조 인식 강화
   - 다양한 등기부 양식 지원

2. **추천 고도화**:
   - 머신러닝 기반 추천 (유사도 학습)
   - 사용자 피드백 반영
   - 실시간 경매 데이터 연동

3. **성능 개선**:
   - 캐싱 (Redis)
   - 비동기 처리 강화
   - 배치 처리 지원

4. **모니터링**:
   - 로깅 (Loguru)
   - 메트릭 수집 (Prometheus)
   - 에러 추적 (Sentry)

---

## 관련 문서

- [../AGENTS.md](../AGENTS.md) - 프로젝트 루트 가이드
- [../CLAUDE.md](../CLAUDE.md) - Claude AI 가이드
- [README.md](README.md) - Python 백엔드 사용법
- C# 연동: `../src/NPLogic.App/Services/PythonBackendService.cs`

---

**작성일**: 2026-02-02
**대상**: AI 에이전트 (Claude Code 등)
**목적**: Python 백엔드 코드베이스 이해 및 작업 가이드
