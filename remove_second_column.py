import codecs
import re

# DashboardView.xaml - 두 번째 경매개시 컬럼 삭제
file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\DashboardView.xaml'
with codecs.open(file, 'r', 'utf-8-sig') as f:
    content = f.read()

# 두 번째 경매개시여부 컬럼 찾아서 삭제 (경매개시여부 2)
pattern = r'<!-- 경매개시여부 2 -->.*?</DataGridTemplateColumn>\s*'
content = re.sub(pattern, '', content, flags=re.DOTALL, count=1)

with codecs.open(file, 'w', 'utf-8-sig') as f:
    f.write(content)
print(f'DashboardView.xaml: 수정 완료')

# RollupWindow.xaml - 두 번째 경매개시 컬럼 삭제
file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\RollupWindow.xaml'
with codecs.open(file, 'r', 'utf-8-sig') as f:
    content = f.read()

# 두 번째 경매개시 컬럼 삭제 - AuctionStart2 바인딩 찾기
pattern = r'<!-- 경매개시 -->\s*<DataGridTextColumn Header="경매개시"\s*Binding="\{Binding AuctionStart2\}".*?IsReadOnly="True"/>\s*'
new_content = re.sub(pattern, '', content, flags=re.DOTALL, count=1)

if new_content != content:
    with codecs.open(file, 'w', 'utf-8-sig') as f:
        f.write(new_content)
    print(f'RollupWindow.xaml: 수정 완료')
else:
    print(f'RollupWindow.xaml: 변경 없음 (이미 1개일 수 있음)')

# DashboardViewModel.cs - 엑셀 헤더에서 두 번째 경매개시 삭제
file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\ViewModels\DashboardViewModel.cs'
with codecs.open(file, 'r', 'utf-8-sig') as f:
    content = f.read()

# "경매개시", "경매개시" -> "경매개시"
new_content = content.replace('"경매개시", "경매개시"', '"경매개시"')

if new_content != content:
    with codecs.open(file, 'w', 'utf-8-sig') as f:
        f.write(new_content)
    print(f'DashboardViewModel.cs: 수정 완료')
else:
    print(f'DashboardViewModel.cs: 변경 없음')

# RollupWindow.xaml.cs - 엑셀 헤더에서 두 번째 경매개시 삭제
file = r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\RollupWindow.xaml.cs'
with codecs.open(file, 'r', 'utf-8-sig') as f:
    content = f.read()

new_content = content.replace('"경매개시", "경매개시"', '"경매개시"')

if new_content != content:
    with codecs.open(file, 'w', 'utf-8-sig') as f:
        f.write(new_content)
    print(f'RollupWindow.xaml.cs: 수정 완료')
else:
    print(f'RollupWindow.xaml.cs: 변경 없음')

print('완료!')
