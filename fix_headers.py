import codecs

files_to_fix = [
    r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\DashboardView.xaml',
    r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\RollupWindow.xaml',
]

cs_files_to_fix = [
    r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\ViewModels\DashboardViewModel.cs',
    r'C:\Users\pwm89\dev\nplogic\src\NPLogic.App\Views\RollupWindow.xaml.cs',
]

# XAML 파일 수정
for file in files_to_fix:
    with codecs.open(file, 'r', 'utf-8-sig') as f:
        content = f.read()
    
    original = content
    content = content.replace('Header="경개1"', 'Header="경매개시"')
    content = content.replace('Header="경개2"', 'Header="경매개시"')
    
    with codecs.open(file, 'w', 'utf-8-sig') as f:
        f.write(content)
    
    print(f'{file}: {"변경됨" if original != content else "변경없음"}')

# CS 파일 수정
for file in cs_files_to_fix:
    with codecs.open(file, 'r', 'utf-8-sig') as f:
        content = f.read()
    
    original = content
    content = content.replace('"경개1", "경개2"', '"경매개시", "경매개시"')
    
    with codecs.open(file, 'w', 'utf-8-sig') as f:
        f.write(content)
    
    print(f'{file}: {"변경됨" if original != content else "변경없음"}')

print("완료!")
