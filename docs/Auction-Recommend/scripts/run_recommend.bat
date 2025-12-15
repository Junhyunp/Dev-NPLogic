@echo off
setlocal
chcp 65001 >nul

REM 은행 엑셀 전체 행에 대해 추천 실행 후, 결과 폴더들을 통합 엑셀로 병합
REM BANK_EXCEL / SHEET / TOPK 등은 run_recommend.py 내부 상수 사용

python ..\run_recommend.py
if errorlevel 1 (
  echo run_recommend.py 실행 중 오류가 발생했습니다.
  goto :end
)

REM results 하위 폴더들의 순위 CSV를 통합 (프로젝트 루트의 results 사용)
python ..\result_combine.py --results_dir ..\results

:end
endlocal


