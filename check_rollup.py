file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\RollupWindow.xaml'
with open(file, 'rb') as f:
    content = f.read()

text = content.decode('utf-8-sig')
lines = text.split('\n')

print("경매개시 관련 라인:")
for i, line in enumerate(lines):
    if 'AuctionStart' in line or ('Header=' in line and i > 240 and i < 280):
        print(f'Line {i+1}: {line.strip()[:80]}')
