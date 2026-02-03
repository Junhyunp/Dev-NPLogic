"""
NPLogic OCR Processor

등기부등본 PDF에서 "주요 등기사항 요약" 페이지를 찾고 OCR 처리
Clova OCR API 사용
"""

import os
import sys
import json
import base64
import io
from pathlib import Path
from typing import Optional

from clova_ocr import ClovaOCR, ClovaOCRProcessor, SummaryPageFinder, images_from_pdf_after


# 환경변수에서 Clova OCR 설정 로드
CLOVA_SECRET_KEY = os.environ.get("CLOVA_SECRET_KEY", "")
CLOVA_INVOKE_URL = os.environ.get("CLOVA_INVOKE_URL", "")


def image_to_base64(pil_image, format: str = "PNG") -> str:
    """
    PIL Image를 Base64 문자열로 변환

    Args:
        pil_image: PIL Image 객체
        format: 이미지 포맷 (PNG, JPEG 등)

    Returns:
        Base64 인코딩된 문자열 (data URI 포함)
    """
    buffer = io.BytesIO()
    pil_image.save(buffer, format=format)
    buffer.seek(0)
    img_bytes = buffer.getvalue()
    b64_string = base64.b64encode(img_bytes).decode('utf-8')
    mime_type = f"image/{format.lower()}"
    return f"data:{mime_type};base64,{b64_string}"


def process_pdf(pdf_path: str, extract_summary: bool = True) -> dict:
    """
    PDF 파일을 OCR 처리하여 데이터 추출

    Args:
        pdf_path: PDF 파일 경로
        extract_summary: True면 "주요 등기사항 요약" 페이지부터 추출

    Returns:
        추출된 데이터 딕셔너리
    """
    if not Path(pdf_path).exists():
        return {
            "success": False,
            "error": f"파일을 찾을 수 없습니다: {pdf_path}"
        }

    if not CLOVA_SECRET_KEY or not CLOVA_INVOKE_URL:
        return {
            "success": False,
            "error": "Clova OCR API 키가 설정되지 않았습니다. CLOVA_SECRET_KEY, CLOVA_INVOKE_URL 환경변수를 확인하세요."
        }

    try:
        result_data = {
            "success": True,
            "file_path": pdf_path,
            "pages": [],
            "summary_start_page": None,
            "summary_image": None,  # Base64 인코딩된 요약 페이지 이미지
            "raw_texts": [],
        }

        # 1. "주요 등기사항 요약" 페이지 찾기
        if extract_summary:
            summary_page = SummaryPageFinder.find_summary_start_page(
                pdf_path,
                dpi=200,
                fallback_clova=True,
                clova_secret_key=CLOVA_SECRET_KEY,
                clova_invoke_url=CLOVA_INVOKE_URL
            )
            result_data["summary_start_page"] = summary_page

            if summary_page is not None:
                # 요약 페이지부터 끝까지 모든 이미지 추출
                # (주요 등기사항 요약은 PDF의 마지막 섹션이므로 끝까지 모두 요약임)
                images = images_from_pdf_after(pdf_path, summary_page, dpi=300)

                # ★ 수정: 모든 요약 페이지 이미지를 Base64 배열로 저장
                if images:
                    result_data["summary_images"] = [image_to_base64(img) for img in images]
                    # 하위 호환성: 첫 번째 이미지도 단일 필드로 유지
                    result_data["summary_image"] = result_data["summary_images"][0]
            else:
                # 요약 페이지를 찾지 못하면 전체 PDF 처리
                from pdf2image import convert_from_path
                images = convert_from_path(pdf_path, dpi=300)
        else:
            from pdf2image import convert_from_path
            images = convert_from_path(pdf_path, dpi=300)

        # 2. 각 페이지 OCR 처리
        processor = ClovaOCRProcessor(
            CLOVA_SECRET_KEY,
            CLOVA_INVOKE_URL,
            dpi=300,
            enable_table=True  # 테이블 감지 활성화
        )

        for i, img in enumerate(images):
            try:
                ocr_result = processor.process_image(img, apply_sharpening=False)

                # 텍스트 추출
                page_text = extract_text_from_ocr_result(ocr_result)

                result_data["pages"].append({
                    "page_number": i + 1,
                    "ocr_result": ocr_result,
                    "text": page_text
                })
                result_data["raw_texts"].append(page_text)

            except Exception as e:
                result_data["pages"].append({
                    "page_number": i + 1,
                    "error": str(e)
                })

        # 3. 전체 텍스트 합치기
        result_data["full_text"] = "\n\n".join(result_data["raw_texts"])

        return result_data

    except Exception as e:
        return {
            "success": False,
            "error": str(e),
            "file_path": pdf_path
        }


def extract_text_from_ocr_result(ocr_result: dict) -> str:
    """
    Clova OCR 결과에서 텍스트 추출
    """
    if not isinstance(ocr_result, dict):
        return ""

    texts = []

    def walk(obj):
        if isinstance(obj, dict):
            for k, v in obj.items():
                if k == 'inferText' and isinstance(v, str):
                    texts.append(v)
                else:
                    walk(v)
        elif isinstance(obj, list):
            for item in obj:
                walk(item)

    walk(ocr_result)
    return " ".join(texts)


def main():
    """CLI 진입점"""
    if len(sys.argv) < 2:
        error = {
            "success": False,
            "error": "PDF 파일 경로가 제공되지 않았습니다."
        }
        print(json.dumps(error, ensure_ascii=False))
        sys.exit(1)

    pdf_path = sys.argv[1]

    try:
        result = process_pdf(pdf_path)
        print(json.dumps(result, ensure_ascii=False, indent=2))
    except Exception as e:
        error = {
            "success": False,
            "error": str(e)
        }
        print(json.dumps(error, ensure_ascii=False))
        sys.exit(1)


if __name__ == "__main__":
    main()
