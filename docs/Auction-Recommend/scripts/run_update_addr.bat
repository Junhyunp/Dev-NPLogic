@echo off
setlocal

REM 의심 주소만 사이트에서 재조회하여 address / address_road 갱신

set PY=python
set AUCT_ID=zerochord
set AUCT_PW=zc2010!@

%PY% ..\data_update.py ^
  --type address ^
  --excel ..\results\auction_constructed.xlsx ^
  --login_id %AUCT_ID% --login_pw %AUCT_PW% ^
  --address_scope invalid_only ^
  --updated_csv ..\results\tmp_address_updated.csv

endlocal


