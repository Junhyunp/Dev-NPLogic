# -*- coding: utf-8 -*-
import pandas as pd
import warnings
import json

warnings.filterwarnings('ignore')

xl = pd.ExcelFile(r'C:\Users\pwm89\dev\nplogic\manager\client\IBK 2025-3 Program Data Disk I_Pool B.xlsx')

# Sheet C-1 실제 데이터 확인
df = pd.read_excel(xl, sheet_name='Sheet C-1', header=9)

print('=== Sheet C-1 컬럼 목록 ===')
for i, col in enumerate(df.columns):
    print(f'{i}: {repr(col)}')

print('\n=== 처음 5개 데이터 행 ===')
# 주요 컬럼 인덱스로 선택
key_cols = [1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 62]  # 일련번호, 차주구분, 차주일련번호, 차주명, 물건번호, Property일련번호, 담보소재지, Property Type, 경매사건번호
cols_to_show = [df.columns[i] for i in key_cols if i < len(df.columns)]

pd.set_option('display.max_columns', None)
pd.set_option('display.width', 300)
pd.set_option('display.max_colwidth', 25)

print(df[cols_to_show].head(10).to_string())

print('\n=== 데이터 샘플 (JSON) ===')
sample = df.head(3).to_dict(orient='records')
for i, row in enumerate(sample):
    print(f'\n--- Row {i+1} ---')
    for k, v in row.items():
        if pd.notna(v):
            print(f'  {k}: {v}')
