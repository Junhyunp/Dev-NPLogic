import pandas as pd
import os
import re

test_dir = r'c:\Users\pwm89\dev\nplogic\manager\실제 테스트 자료'
output_dir = os.path.join(test_dir, 'csv_output')
os.makedirs(output_dir, exist_ok=True)

excel_files = [f for f in os.listdir(test_dir) if f.endswith('.xlsx')]
print(f'찾은 엑셀 파일: {len(excel_files)}개')

for excel_file in excel_files:
    excel_path = os.path.join(test_dir, excel_file)
    print(f'\n처리 중: {excel_file}')
    
    # 파일명에서 확장자 제거
    base_name = os.path.splitext(excel_file)[0]
    # 파일명이 너무 길면 앞부분만 사용
    if len(base_name) > 50:
        base_name = base_name[:50]
    
    # 엑셀 파일의 모든 시트 읽기
    xl = pd.ExcelFile(excel_path)
    sheet_names = xl.sheet_names
    print(f'  시트 개수: {len(sheet_names)}')
    print(f'  시트 목록: {sheet_names}')
    
    for sheet_name in sheet_names:
        try:
            df = pd.read_excel(excel_path, sheet_name=sheet_name, header=None)
            # 파일명 생성 (특수문자 제거)
            safe_sheet_name = re.sub(r'[/\\:*?"<>|]', '_', sheet_name)
            csv_filename = f'{base_name}_{safe_sheet_name}.csv'
            csv_path = os.path.join(output_dir, csv_filename)
            df.to_csv(csv_path, index=False, header=False, encoding='utf-8-sig')
            print(f'  [OK] {sheet_name} ({df.shape[0]}행 x {df.shape[1]}열) -> {csv_filename}')
        except Exception as e:
            print(f'  [FAIL] {sheet_name} 변환 실패: {e}')

print(f'\n완료! CSV 파일이 {output_dir}에 저장되었습니다.')
