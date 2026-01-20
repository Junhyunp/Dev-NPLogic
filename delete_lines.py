file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\DashboardView.xaml'
with open(file, 'rb') as f:
    content = f.read()

text = content.decode('utf-8-sig')
lines = text.split('\n')

print(f'원본 라인 수: {len(lines)}')

# Line 1113-1129 삭제 (0-indexed: 1112-1128)
# 1113: <!-- 경매개시여부 2 -->
# 1114-1128: DataGridTemplateColumn
# 1129: 빈 줄

del lines[1112:1129]  # 1113-1129 삭제

print(f'삭제 후 라인 수: {len(lines)}')

new_text = '\n'.join(lines)
with open(file, 'wb') as f:
    f.write(new_text.encode('utf-8-sig'))

print('DashboardView.xaml 수정 완료!')

# 확인
with open(file, 'rb') as f:
    content = f.read()
text = content.decode('utf-8-sig')
lines = text.split('\n')

print("\n수정 후 Width=70 컬럼들:")
for i, line in enumerate(lines):
    if 'Width="70"' in line and 'Header=' in line:
        print(f'Line {i+1}: {line.strip()[:60]}')
