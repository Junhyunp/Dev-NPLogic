"""
엑셀 파일의 각 시트를 이미지로 변환하는 스크립트
"""
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib
from pathlib import Path
import warnings

# 한글 폰트 설정
matplotlib.rcParams['font.family'] = 'Malgun Gothic'
matplotlib.rcParams['axes.unicode_minus'] = False

# 경고 무시
warnings.filterwarnings('ignore')

def excel_sheets_to_images(excel_path: str, output_dir: str):
    """엑셀 파일의 각 시트를 이미지로 변환"""
    
    # 출력 디렉토리 생성
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # 엑셀 파일 읽기
    xl = pd.ExcelFile(excel_path)
    sheet_names = xl.sheet_names
    
    print(f"총 {len(sheet_names)}개의 시트 발견:")
    for i, name in enumerate(sheet_names, 1):
        print(f"  {i}. {name}")
    
    print("\n이미지 변환 시작...\n")
    
    for i, sheet_name in enumerate(sheet_names, 1):
        try:
            # 시트 읽기
            df = pd.read_excel(excel_path, sheet_name=sheet_name, header=None)
            
            if df.empty:
                print(f"[{i}/{len(sheet_names)}] {sheet_name}: 빈 시트, 건너뜀")
                continue
            
            # 파일명에 사용할 수 없는 문자 제거
            safe_name = sheet_name.replace('/', '_').replace('\\', '_').replace(':', '_')
            safe_name = safe_name.replace('*', '_').replace('?', '_').replace('"', '_')
            safe_name = safe_name.replace('<', '_').replace('>', '_').replace('|', '_')
            
            # 이미지 크기 계산 (열과 행 수에 따라 동적 조정)
            rows, cols = df.shape
            fig_width = min(max(cols * 1.5, 12), 40)  # 최소 12, 최대 40
            fig_height = min(max(rows * 0.4, 6), 30)  # 최소 6, 최대 30
            
            # Figure 생성
            fig, ax = plt.subplots(figsize=(fig_width, fig_height))
            ax.axis('off')
            
            # 테이블 생성
            table = ax.table(
                cellText=df.fillna('').astype(str).values,
                cellLoc='center',
                loc='center',
                colWidths=[1.0/cols] * cols if cols > 0 else [1.0]
            )
            
            # 테이블 스타일 설정
            table.auto_set_font_size(False)
            table.set_fontsize(8)
            table.scale(1.2, 1.5)
            
            # 헤더 행 스타일 (첫 번째 행)
            for j in range(cols):
                cell = table[(0, j)]
                cell.set_facecolor('#4472C4')
                cell.set_text_props(color='white', weight='bold')
            
            # 제목 추가
            plt.title(f"시트: {sheet_name}", fontsize=14, fontweight='bold', pad=20)
            
            # 이미지 저장
            output_file = output_path / f"{i:02d}_{safe_name}.png"
            plt.savefig(output_file, dpi=150, bbox_inches='tight', 
                       facecolor='white', edgecolor='none')
            plt.close()
            
            print(f"[{i}/{len(sheet_names)}] {sheet_name} -> {output_file.name}")
            
        except Exception as e:
            print(f"[{i}/{len(sheet_names)}] {sheet_name}: 오류 발생 - {e}")
            plt.close()
    
    print(f"\n완료! 이미지 저장 위치: {output_path}")

if __name__ == "__main__":
    excel_file = r"C:\Users\pwm89\dev\nplogic\manager\client\비핵심 프로그램화_v6-1 (1).xlsx"
    output_dir = r"C:\Users\pwm89\dev\nplogic\manager\client\산출화면_시트_이미지"
    
    excel_sheets_to_images(excel_file, output_dir)
