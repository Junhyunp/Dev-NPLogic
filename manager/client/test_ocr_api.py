# -*- coding: utf-8 -*-
"""
등기부등본 OCR API 테스트 스크립트
"""
import requests
import json
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# API URL
url = "http://localhost:8000/api/ocr/registry"

# PDF 파일 경로 (테스트용 등기부등본 PDF)
pdf_path = os.path.join(SCRIPT_DIR, "테스트용 등기", "S-013-2-02 [토지] 경기도 화성시 팔탄면 노하리 282-18.pdf")

print(f"PDF 파일 경로: {pdf_path}")
print(f"파일 존재: {os.path.exists(pdf_path)}")
print(f"파일 크기: {os.path.getsize(pdf_path)} bytes")
print("-" * 50)

# API 호출
with open(pdf_path, "rb") as f:
    files = {"file": ("registry_test.pdf", f, "application/pdf")}
    response = requests.post(url, files=files)

print(f"Status Code: {response.status_code}")
print("-" * 50)

# 결과 저장 및 출력
result = response.json()
output_path = os.path.join(SCRIPT_DIR, "ocr_result.json")
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(result, f, ensure_ascii=False, indent=2)

print(f"결과 저장: {output_path}")
print(f"Success: {result.get('success')}")
print(f"Error: {result.get('error')}")

if result.get('data'):
    data = result['data']
    print(f"Owners: {len(data.get('owners', []))} items")
    print(f"Gapgu: {len(data.get('gapgu', []))} items")
    print(f"Eulgu: {len(data.get('eulgu', []))} items")

