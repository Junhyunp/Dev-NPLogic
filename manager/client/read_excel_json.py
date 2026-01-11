# -*- coding: utf-8 -*-
import pandas as pd
import warnings
import json

warnings.filterwarnings('ignore')

xl = pd.ExcelFile(r'C:\Users\pwm89\dev\nplogic\manager\client\IBK 2025-3 Program Data Disk I_Pool B.xlsx')

output = {}

for sheet in xl.sheet_names:
    df = pd.read_excel(xl, sheet_name=sheet, header=9)
    # NaN을 None으로 변환
    df = df.where(pd.notnull(df), None)
    # datetime을 string으로 변환
    for col in df.columns:
        if df[col].dtype == 'datetime64[ns]':
            df[col] = df[col].astype(str).replace('NaT', None)
    
    output[sheet] = {
        'columns': list(df.columns),
        'row_count': len(df),
        'data_sample': df.head(5).to_dict(orient='records')
    }

with open(r'C:\Users\pwm89\dev\nplogic\manager\client\excel_data_sample.json', 'w', encoding='utf-8') as f:
    json.dump(output, f, ensure_ascii=False, indent=2, default=str)

print('JSON 파일로 저장 완료: excel_data_sample.json')
