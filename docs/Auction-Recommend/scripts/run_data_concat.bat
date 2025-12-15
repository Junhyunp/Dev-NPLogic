@echo off
setlocal

REM 기존 data/total + data/3_202509 를 합쳐 data/total 재생성
REM 필요시 아래 경로를 수정해서 사용하세요.

set PY=python

%PY% ..\data_concat.py ^
  --prev_dir ..\data\total ^
  --new_dir ..\data\3_202509 ^
  --out_dir ..\data\total

endlocal


