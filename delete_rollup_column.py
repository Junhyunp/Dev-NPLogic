file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\RollupWindow.xaml'
with open(file, 'rb') as f:
    content = f.read()

text = content.decode('utf-8-sig')
lines = text.split('\n')

print(f'원본 라인 수: {len(lines)}')

# Line 259-265 확인 (두 번째 경매개시 컬럼)
print("\n삭제할 라인들:")
for i in range(258, 266):
    if i < len(lines):
        print(f'Line {i+1}: {lines[i].strip()[:60]}')

# Line 259-264 삭제 (0-indexed: 258-263) - <!-- 경매개시 --> 부터 IsReadOnly="True"/> 까지
# 259: <!-- 경매개시 -->
# 260: <DataGridTextColumn Header="경매개시" 
# 261: Binding="{Binding AuctionStart2}"
# 262: Width="50"
# 263: IsReadOnly="True"/>
# 264: (빈 줄)

del lines[258:265]  # 259-265 삭제

print(f'\n삭제 후 라인 수: {len(lines)}')

new_text = '\n'.join(lines)
with open(file, 'wb') as f:
    f.write(new_text.encode('utf-8-sig'))

print('RollupWindow.xaml 수정 완료!')

# 확인
with open(file, 'rb') as f:
    content = f.read()
text = content.decode('utf-8-sig')
lines = text.split('\n')

print("\n수정 후 경매개시 관련:")
for i, line in enumerate(lines):
    if 'AuctionStart' in line:
        print(f'Line {i+1}: {line.strip()[:60]}')
