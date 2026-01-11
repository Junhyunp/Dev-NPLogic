# -*- coding: utf-8 -*-
import pandas as pd
import json

xl = pd.ExcelFile(r'C:\Users\pwm89\dev\nplogic\manager\client\IBK 2025-3 Program Data Disk I_Pool B.xlsx')

# Sheet C-1 컬럼명 정확히 확인
df = pd.read_excel(xl, sheet_name='Sheet C-1', header=9)

print("=== Sheet C-1 컬럼명 (repr로 정확히) ===")
for i, col in enumerate(df.columns):
    # 줄바꿈 문자를 \\n으로 표시
    display_col = repr(col)
    print(f"{i}: {display_col}")

# 실제 데이터 샘플 (첫 행)
print("\n=== 첫 번째 데이터 행 ===")
first_row = df.iloc[0]
for col in df.columns:
    value = first_row[col]
    if pd.notna(value):
        print(f"  {repr(col)}: {value}")
