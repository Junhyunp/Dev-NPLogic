@echo off
setlocal ENABLEDELAYEDEXPANSION

REM UTF-8 코드페이지로 변경 (한글 경로/출력 안전)
chcp 65001 > nul

REM 실행 위치를 스크립트 파일 기준으로 이동 후 ver6 루트로 이동
cd /d %~dp0
cd ..

REM 기본 경로 설정 (필요시 아래 값을 수정하세요)
set "EXCEL=.\data\datadisk\IBK_2025_3_test11.xlsx"
set "CFG=.\cfg\extract_columns.json"
set "PDFDIR=.\data\[IBK 2025-3] 등기부등본\1.등기부등본"
set "JSON_OUT=.\results\IBK_2025_3\ocr_json"
set "ITEMS_OUT=.\results\IBK_2025_3\items"
REM CLOVA 자격 정보 (환경변수로도 덮어쓰기 가능)
if not defined CLOVA_SECRET set "CLOVA_SECRET=%CLOVA_SECRET_KEY%"
if not defined CLOVA_URL set "CLOVA_URL=%CLOVA_INVOKE_URL%"
REM 요약 탐색 디버그 출력 토글 (1로 설정시 상세 로그) - 기본값 1로 활성화
if not defined DEBUG_SUMMARY set "DEBUG_SUMMARY=1"
set "PYTHONUNBUFFERED=1"
REM merge_split_rows 식별자 병합 디버그 토글 (1로 설정 시 식별자 병합 전/후 로그 출력)
set "DBG_MERGE_IDS=0"
set "DBG_MATCH_KEYS=0"
set "DBG_MERGE_TABLE=0"
echo        DBG_MERGE_IDS=%DBG_MERGE_IDS%
echo        DBG_MATCH_KEYS=%DBG_MATCH_KEYS%
echo        DBG_MERGE_TABLE=%DBG_MERGE_TABLE%

echo [INFO] Running Clova OCR pipeline...
echo        EXCEL=%EXCEL%
echo        CFG=%CFG%
echo        PDFDIR=%PDFDIR%
echo        JSON_OUT=%JSON_OUT%
echo        ITEMS_OUT=%ITEMS_OUT%
echo        DEBUG_SUMMARY=%DEBUG_SUMMARY%

REM 추가 인자(%*)를 통해 기본값을 덮어쓸 수 있습니다.
REM 예) run_clova_pipeline.bat --pdfdir "D:\pdfs" --enable_table

python main.py --clova --enable_table --apply_sharpening --max_workers 5 ^
  --excel "%EXCEL%" ^
  --cfg "%CFG%" ^
  --pdfdir "%PDFDIR%" ^
  --json_out "%JSON_OUT%" ^
  --items_out "%ITEMS_OUT%" ^
  --secret_key "%CLOVA_SECRET%" ^
  --invoke_url "%CLOVA_URL%" ^
  %*

set EXITCODE=%ERRORLEVEL%
if not "%EXITCODE%"=="0" (
  echo [ERROR] Pipeline failed with exit code %EXITCODE%.
  exit /b %EXITCODE%
)

echo [INFO] Pipeline completed successfully.
exit /b 0


