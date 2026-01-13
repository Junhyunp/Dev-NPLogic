import pandas as pd
import os
import re

# manager 폴더에서 실제 테스트 자료 폴더 찾기
manager_dir = r'c:\Users\pwm89\dev\nplogic\manager'

# 폴더 목록 출력
print("manager 폴더 내용:")
for item in os.listdir(manager_dir):
    print(f"  - {item}")

# 실제 테스트 자료 폴더 찾기
test_dir = None
for item in os.listdir(manager_dir):
    item_path = os.path.join(manager_dir, item)
    if os.path.isdir(item_path) and '테스트' in item:
        test_dir = item_path
        print(f"\n테스트 폴더 발견: {item}")
        break

if not test_dir:
    print("테스트 자료 폴더를 찾을 수 없습니다.")
    exit(1)

output_dir = os.path.join(manager_dir, 'csv_output_test')
os.makedirs(output_dir, exist_ok=True)

excel_files = [f for f in os.listdir(test_dir) if f.endswith('.xlsx')]
print(f'\n찾은 엑셀 파일: {len(excel_files)}개')

for excel_file in excel_files:
    excel_path = os.path.join(test_dir, excel_file)
    print(f'\n========================================')
    print(f'파일: {excel_file}')
    print(f'========================================')
    
    # 파일명에서 키워드 추출
    if 'Assign' in excel_file:
        file_prefix = 'Assign'
    elif 'interim' in excel_file.lower():
        file_prefix = 'Interim'
    else:
        file_prefix = 'Unknown'
    
    # 엑셀 파일의 모든 시트 읽기
    xl = pd.ExcelFile(excel_path)
    sheet_names = xl.sheet_names
    print(f'시트 개수: {len(sheet_names)}')
    print(f'시트 목록:')
    for i, name in enumerate(sheet_names):
        print(f'  {i+1}. {name}')
    
    for sheet_name in sheet_names:
        try:
            df = pd.read_excel(excel_path, sheet_name=sheet_name, header=None)
            # 파일명 생성 (특수문자 제거)
            safe_sheet_name = re.sub(r'[/\\:*?"<>|]', '_', sheet_name)
            csv_filename = f'{file_prefix}_{safe_sheet_name}.csv'
            csv_path = os.path.join(output_dir, csv_filename)
            df.to_csv(csv_path, index=False, header=False, encoding='utf-8-sig')
            print(f'  [OK] {sheet_name} ({df.shape[0]}행 x {df.shape[1]}열)')
        except Exception as e:
            print(f'  [FAIL] {sheet_name}: {e}')

print(f'\n완료! CSV 파일이 {output_dir}에 저장되었습니다.')
