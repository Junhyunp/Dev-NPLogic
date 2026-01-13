# -*- coding: utf-8 -*-
import openpyxl
wb = openpyxl.load_workbook(r'C:\Users\pwm89\dev\nplogic\manager\client\IBK 2025-3 Program Data Disk I_Pool B.xlsx', read_only=True, data_only=True)
ws = wb['Sheet C-1']

# 헤더 Row 10의 정확한 값 확인
print('=== Sheet C-1 Header (Row 10) - Exact Values ===')
print()

# 매핑에서 사용하는 컬럼들
key_cols = {
    5: 'borrower_number (차주일련번호)',
    6: 'borrower_name (차주명)', 
    7: 'collateral_number (물건번호)',
    8: 'property_serial (Property 일련번호)',
    13: 'property_type (Property Type)',
    63: 'property_number (경매사건번호)',
}

for col, expected in key_cols.items():
    val = ws.cell(10, col).value
    if val:
        # repr로 정확한 값 출력 (줄바꿈 등 확인)
        print(f'Col {col}: {repr(val)}')
        print(f'  -> Expected mapping: {expected}')
        print()

# 매핑 규칙에서 찾는 값들과 비교
print('=== Mapping Rules (from SheetMappingConfig) ===')
mapping_rules = [
    ('차주일련번호', 'borrower_number'),
    ('차주명', 'borrower_name'),
    ('물건번호', 'collateral_number'),
    ('Property 일련번호', 'property_serial'),
    ('Property일련번호', 'property_serial'),
    ('Property Type', 'property_type'),
    ('경매사건번호', 'property_number'),
    ('경매사건번호(IBK)', 'property_number'),
    ('경매사건번호 (IBK)', 'property_number'),
]

for excel_name, db_col in mapping_rules:
    print(f'  "{excel_name}" -> {db_col}')
