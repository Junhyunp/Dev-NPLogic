file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\DashboardView.xaml'
with open(file, 'rb') as f:
    content = f.read()

text = content.decode('utf-8-sig')
lines = text.split('\n')

print("Width=70 컬럼들:")
for i, line in enumerate(lines):
    if 'Width="70"' in line and 'Header=' in line:
        print(f'Line {i+1}: {line.strip()[:100]}')
