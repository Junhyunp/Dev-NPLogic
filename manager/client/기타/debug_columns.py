# -*- coding: utf-8 -*-
import pandas as pd
import json

xl = pd.ExcelFile(r'C:\Users\pwm89\dev\nplogic\manager\client\IBK 2025-3 Program Data Disk I_Pool B.xlsx')

# Sheet C-1 컬럼명과 첫 행 데이터를 JSON으로 출력
df = pd.read_excel(xl, sheet_name='Sheet C-1', header=9)

result = {
    "columns": [],
    "first_row": {}
}

for i, col in enumerate(df.columns):
    col_info = {
        "index": i,
        "name": col,
        "name_repr": repr(col),
        "has_newline": '\n' in str(col)
    }
    result["columns"].append(col_info)

# 첫 번째 데이터 행
first_row = df.iloc[0]
for col in df.columns:
    value = first_row[col]
    if pd.notna(value):
        result["first_row"][col] = str(value)

# JSON 파일로 저장
with open('debug_columns.json', 'w', encoding='utf-8') as f:
    json.dump(result, f, ensure_ascii=False, indent=2)

print("저장 완료: debug_columns.json")
