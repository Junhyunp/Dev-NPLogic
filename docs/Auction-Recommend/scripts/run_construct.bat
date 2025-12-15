@echo off
setlocal

REM total/merge_progress.yaml을 참고하여 미처리 폴더만 델타 생성
REM 출력: results/auction_construct_delta.xlsx

set PY=python

REM run_update.bat과 동일한 자격증명 사용
set AUCT_ID=k.symoon
set AUCT_PW=595986

%PY% ..\data_construct.py ^
  --input_root ..\data\4_202501_2 ^
  --output ..\results\auction_construct_delta.xlsx ^
  --use_progress ^
  --update_progress ^
  --enrich_site ^
  --login_id "%AUCT_ID%" --login_pw "%AUCT_PW%" ^
  --selenium_wait 45 ^
  --sleep 0.2

endlocal

