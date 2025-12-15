@echo off
setlocal

REM 지오코딩 보강 (longitude / latitude / coord_crs 재계산 및 덮어쓰기)

set PY=python

%PY% ..\data_update.py ^
  --type geo ^
  --excel ..\results\auction_constructed.xlsx ^
  --max_rows 0 ^
  --sleep 0.05 ^
  --failed_csv ..\results\update_failed.csv

endlocal


