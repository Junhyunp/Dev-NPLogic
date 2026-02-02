# OCR 등기부등본 처리 시스템 (v1.0)

등기부등본 PDF 파일에서 Clova OCR을 사용하여 소유자, 갑구, 을구 정보를 추출하고 데이터를 정리하는 시스템입니다.

## 주요 기능

- **PDF 요약 페이지 자동 추출**: 등기부등본에서 요약 페이지를 자동으로 찾아 추출
- **Clova OCR 표 인식**: 소유자, 갑구, 을구 테이블을 자동으로 인식하고 구조화된 데이터로 변환
- **주소 매칭**: PDF 파일명의 주소와 데이터베이스의 주소를 비교하여 물건번호 매칭
- **데이터 정리 및 변환**: 면적 단위 변환, 컬럼 정리, 조건부 데이터 처리
- **멀티프로세싱**: 대량의 PDF 파일을 병렬로 처리하여 성능 향상
- **진행률 표시**: tqdm을 사용한 실시간 진행률 표시

## 시스템 요구사항

- Python 3.8+
- Windows 10/11 (배치 스크립트 포함)
- Clova OCR API 키 (Naver Cloud Platform)

## 설치

### 1. 의존성 패키지 설치

```bash
pip install -r requirements.txt
```

### 2. Clova OCR API 설정

환경변수로 설정하거나 명령행 인자로 전달:

```bash
# 환경변수 설정
set CLOVA_SECRET=your_secret_key
set CLOVA_URL=your_invoke_url

# 또는 명령행에서 직접 지정
python main.py --secret_key your_secret_key --invoke_url your_invoke_url
```

## 사용 방법

### 1. 기본 실행 (배치 스크립트 사용)

가장 간단한 방법은 배치 스크립트를 사용하는 것입니다:

```bash
# 기본 설정으로 실행
scripts\run_clova_pipeline.bat

# 커스텀 경로로 실행
scripts\run_clova_pipeline.bat --pdfdir "D:\custom\pdf\path"
```

### 2. Python 직접 실행

```bash
# 예시: 입력/설정/PDF 경로 + Clova 키 + 결과 저장 디렉터리 + 요약(및 이후) 페이지 이미지 저장
python main.py \
  --excel "./data/datadisk/KB_2024_5.xlsx" \
  --cfg "./cfg/extract_columns.json" \
  --pdfdir "./data/KB 2024-5 Program_등기부등본/1. 등본" \
  --secret_key "your_secret_key" \
  --invoke_url "your_invoke_url" \
  --items_out "./results/items" \
  --json_out "./results/ocr_json" \
  --save_summary_images
```

## 명령행 옵션

| 옵션 | 기본값 | 설명 |
|------|--------|------|
| `--excel`, `-e` | `./data/datadisk/KB_2024_5.xlsx` | 입력 엑셀 파일 경로 |
| `--cfg`, `-c` | `./cfg/extract_columns.json` | 컬럼 추출 설정 JSON 파일 |
| `--pdfdir` | `./data/KB 2024-5 Program_등기부등본/1. 등본` | PDF 파일이 있는 디렉토리 |
| `--secret_key` | 환경변수 `CLOVA_SECRET` | Clova OCR 시크릿 키 |
| `--invoke_url` | 환경변수 `CLOVA_URL` | Clova OCR Invoke URL |
| `--items_out` | `./results/items` | 물건번호별 산출물 저장 루트 디렉토리 |
| `--save_summary_images` | `False` | 요약(및 이후) 페이지 이미지를 `items_out/<itemId>/images`에 JPG로 저장 |

## 입력 파일 구조

### 1. 엑셀 파일 (KB_2024_5.xlsx)
데이터베이스 파일로 다음 컬럼들이 포함되어야 합니다:
- `차주일련번호`
- `Property 일련번호`
- `등기부등본 일련번호`
- `담보소재지1-4` (주소 관련)
- `등기부등본- 담보물형태`
- `Property- 대지면적`
- `Property- 건물면적`

### 2. PDF 파일
- 파일명 형식: `R-XXX-YY [건물/토지/집합건물] 주소.pdf`
- 예: `R-001-01 [건물] 서울특별시 강남구 신사동 510.pdf`

### 3. 설정 파일 (extract_columns.json)
컬럼 매핑 설정을 정의합니다.

## 출력 파일 구조

### 1. 물건번호별 폴더 구조
```
results/items/
├── R-001_01/
│   ├── basic_info.csv      # 기본 정보 (물건번호 제외)
│   ├── owners.csv          # 소유자 정보
│   ├── gapgu.csv           # 갑구 정보
│   ├── eulgu.csv           # 을구 정보
│   └── images/             # (--save_summary_images 사용 시) 요약(및 이후) 페이지 이미지
└── R-002_01/
    ├── basic_info.csv
    ├── owners.csv
    ├── gapgu.csv
    └── eulgu.csv
```

### 2. basic_info.csv 컬럼 순서
1. 지번일련번호
2. 물건지 (등기부등본)
3. 물건지 (DD)
4. 일치여부
5. 담보물형태
6. 대지면적 (평)
7. 건물면적 (평)
8. 소유자
9. 등록번호
10. 최종지분
11. 소유자 주소

### 3. 특별 처리 규칙
- **담보물형태가 "동산담보"인 경우**: 물건지 (DD), 물건지 (등기부등본), 일치여부 컬럼이 빈칸으로 설정
- **면적 변환**: 제곱미터를 평으로 변환 (3.305785로 나누기)

## 문제 해결

### 1. Clova API 오류
```
CR API 오류: 400 {"code":"0025","message":"Calls to this api have exceeded the rate limit"}
```
- 동시 요청 수가 과도할 때 발생할 수 있습니다. 처리 작업을 나누어 실행하거나(입력 PDF를 배치로 분리) 시간 간격을 두고 재시도하세요.

### 2. 한글 경로 문제
- 배치 스크립트에서 `chcp 65001`로 UTF-8 코드페이지 설정
- 파일 경로에 한글이 포함된 경우 주의

### 3. 메모리 부족
- PDF 파일을 배치로 나누어 처리하거나, 동시에 실행되는 작업을 줄이세요.

## 개발자 정보

- **버전**: 1.0
- **개발 환경**: Python 3.8+, Windows 10/11
- **주요 라이브러리**: pandas, tqdm, multiprocessing, Clova OCR API

## 라이선스

이 프로젝트는 내부 사용을 위한 것입니다.
