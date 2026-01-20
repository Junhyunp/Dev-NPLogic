file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\DashboardView.xaml'
with open(file, 'rb') as f:
    content = f.read()

text = content.decode('utf-8-sig')
lines = text.split('\n')

# Line 1113-1130 삭제 (두 번째 경매개시 컬럼 전체)
# 1113: <!-- 경매개시여부 2 -->
# 1114-1128: DataGridTemplateColumn 전체

# 먼저 정확한 범위 찾기
start_line = None
end_line = None

for i, line in enumerate(lines):
    if '경매개시여부 2' in line:
        start_line = i
        print(f'시작: Line {i+1}: {line.strip()}')
    if start_line is not None and end_line is None:
        if '</DataGridTemplateColumn>' in line and i > start_line:
            end_line = i
            print(f'끝: Line {i+1}: {line.strip()}')
            break

if start_line is not None and end_line is not None:
    # 삭제
    del lines[start_line:end_line+2]  # +2 for the blank line after
    
    new_text = '\n'.join(lines)
    with open(file, 'wb') as f:
        f.write(new_text.encode('utf-8-sig'))
    print(f'삭제 완료: Line {start_line+1} ~ {end_line+2}')
else:
    print('두 번째 경매개시 컬럼을 찾지 못했습니다')
    # 대안: 1114 라인 근처 직접 삭제
    print('대안 시도...')
    
    # 1113번 라인부터 시작해서 </DataGridTemplateColumn> 찾기
    if len(lines) > 1113:
        # 1113 (0-indexed 1112) 부터 확인
        for i in range(1112, min(1112+20, len(lines))):
            print(f'Line {i+1}: {lines[i].strip()[:60]}')
