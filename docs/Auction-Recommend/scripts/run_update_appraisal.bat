@echo off
setlocal

REM appraisal_price가 비어있는 행들을 대상으로 popup_url을 열어
REM 옥션원 입찰내역에서 최저매각가격(appraisal_price)을 추출/갱신

set PY=python
set AUCT_ID=zerochord
set AUCT_PW=zc2010!@

%PY% ..\data_update.py ^
  --type appraisal ^
  --excel ..\results\auction_constructed.xlsx ^
  --login_id %AUCT_ID% --login_pw %AUCT_PW% ^
  --max_rows 0 ^
  --sleep 0.1 ^
  --failed_csv ..\results\appraisal_update_failed.csv ^
  --appraisal_csv ..\results\appraisal_missing.csv

endlocal



