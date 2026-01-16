# -*- coding: utf-8 -*-
import pandas as pd

xl = pd.ExcelFile(r'C:\Users\pwm89\dev\nplogic\manager\client\IBK 2025-3 Program Data Disk I_Pool B.xlsx')

# Sheet C-1 데이터 확인
df = pd.read_excel(xl, sheet_name='Sheet C-1', header=9)

# 경매사건번호 컬럼 찾기
auction_col = None
for col in df.columns:
    if '경매사건번호' in col:
        auction_col = col
        print(f"경매사건번호 컬럼 발견: {repr(col)}")
        break

if auction_col:
    print(f"\n총 행 수: {len(df)}")
    
    # 경매사건번호가 있는 행 vs 없는 행
    has_number = df[auction_col].notna()
    print(f"경매사건번호가 있는 행: {has_number.sum()}")
    print(f"경매사건번호가 없는 행: {(~has_number).sum()}")
    
    print("\n=== 경매사건번호 샘플 (처음 20개) ===")
    for i, val in enumerate(df[auction_col].head(20)):
        print(f"Row {i+1}: {val}")
    
    # 차주명, 물건번호, Property Type 확인
    print("\n=== 관련 데이터 샘플 (처음 5행) ===")
    key_cols = ['차주일련번호', '차주명', '물건번호', 'Property Type', auction_col]
    actual_cols = [c for c in key_cols if c in df.columns]
    print(df[actual_cols].head(5).to_string())
else:
    print("경매사건번호 컬럼을 찾을 수 없습니다!")
    print("사용 가능한 컬럼:", list(df.columns))
