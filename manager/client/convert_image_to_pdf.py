# -*- coding: utf-8 -*-
"""
이미지를 PDF로 변환하는 스크립트
"""

from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader
from PIL import Image
import os

# 현재 스크립트 위치
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# 파일 경로
img_path = os.path.join(SCRIPT_DIR, "등기부등본.png")
pdf_path = os.path.join(SCRIPT_DIR, "등기부등본_테스트.pdf")

print(f"이미지 경로: {img_path}")
print(f"파일 존재: {os.path.exists(img_path)}")

# 이미지 열기
img = Image.open(img_path)
img_width, img_height = img.size

# PDF 크기를 이미지 비율에 맞게 조정 (A4 기준)
pdf_width, pdf_height = A4
scale = min(pdf_width / img_width, pdf_height / img_height)
new_width = img_width * scale
new_height = img_height * scale

# PDF 생성
c = canvas.Canvas(pdf_path, pagesize=(new_width, new_height))
c.drawImage(ImageReader(img), 0, 0, width=new_width, height=new_height)
c.save()

print(f"\n✓ PDF 생성 완료!")
print(f"  파일 경로: {pdf_path}")
print(f"  원본 이미지 크기: {img_width}x{img_height}")
print(f"  PDF 크기: {new_width:.0f}x{new_height:.0f}")
