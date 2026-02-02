@echo off
setlocal ENABLEDELAYEDEXPANSION

rem UTF-8 코드 페이지로 변경 (한글 경로/출력 안전)
chcp 65001 > nul

rem 스크립트 위치 -> ver6 루트로 이동
cd /d %~dp0
cd ..

echo ======================================
echo [1] KB_2024_5.xlsx 파일 검색
echo ======================================
set "FOUND_XLSX="
for /r . %%F in (KB_2024_5.xlsx) do (
  if not defined FOUND_XLSX set "FOUND_XLSX=%%~fF"
)
if defined FOUND_XLSX (
  echo  - Found: !FOUND_XLSX!
) else (
  echo  - [WARN] KB_2024_5.xlsx 파일을 찾지 못했습니다.
)

echo.
echo ======================================
echo [2] extract_columns(.json) 파일 검색
echo ======================================
set "FOUND_CFG="
for /r . %%F in (extract_columns*.json) do (
  if not defined FOUND_CFG set "FOUND_CFG=%%~fF"
)
if defined FOUND_CFG (
  echo  - Found: !FOUND_CFG!
) else (
  echo  - [WARN] extract_columns.json 파일을 찾지 못했습니다.
)

echo.
echo ======================================
echo [3] 등기부등본 폴더 PDF 목록 출력 (고정 경로)
echo ======================================
set "REG_DIR=.\data\KB 2024-5 Program_등기부등본\1. 등본"

if exist "%REG_DIR%" (
  echo  - 등기부등본 폴더: %REG_DIR%
  pushd "%REG_DIR%"
  echo.
  echo  - 현재 폴더의 PDF 목록:
  dir /b *.pdf 2>nul
  popd
) else (
  echo  - [WARN] 경로가 존재하지 않습니다: %REG_DIR%
)

echo.
echo [DONE] 경로 점검을 완료했습니다.
exit /b 0


