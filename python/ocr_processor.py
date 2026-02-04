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
import re
from pathlib import Path
from typing import Optional, List, Dict

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

        # 4. 표 데이터 파싱
        table_data = parse_registry_tables(result_data["pages"])

        # 5. 주소 추출
        address = extract_address_from_text(result_data["full_text"])

        # 6. data 필드에 구조화된 데이터 추가
        result_data["data"] = {
            "address": address,
            "owners": table_data["owners"],
            "gapgu": table_data["gapgu"],
            "eulgu": table_data["eulgu"]
        }

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


def extract_address_from_text(text: str) -> str:
    """
    텍스트에서 주소 추출

    Args:
        text: OCR 결과 텍스트

    Returns:
        추출된 주소 문자열
    """
    # 주소 패턴: 시/도로 시작하는 패턴
    address_patterns = [
        r'(서울[특별시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(부산[광역시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(대구[광역시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(인천[광역시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(광주[광역시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(대전[광역시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(울산[광역시]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(세종[특별자치시]*\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(경기도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(강원[특별자치도]*\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(충청북도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(충청남도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(전라북도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(전북특별자치도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(전라남도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(경상북도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(경상남도\s*[^\s]+\s*[^\s]+(?:\s*[^\s]+)?)',
        r'(제주[특별자치도]*\s*[^\s]+(?:\s*[^\s]+)?)',
    ]

    for pattern in address_patterns:
        match = re.search(pattern, text)
        if match:
            return match.group(1).strip()

    # "소재지번" 다음 텍스트 추출 시도
    sojaejibun_match = re.search(r'소재지번[:\s]*([^\n]+)', text)
    if sojaejibun_match:
        return sojaejibun_match.group(1).strip()

    return ""


def parse_table_from_ocr(table_data: dict) -> List[Dict[str, str]]:
    """
    Clova OCR 테이블 데이터를 파싱하여 딕셔너리 리스트로 변환

    Args:
        table_data: Clova OCR API의 table 객체

    Returns:
        각 행을 딕셔너리로 변환한 리스트
    """
    if not table_data or 'cells' not in table_data:
        return []

    cells = table_data['cells']

    # 셀을 행/열 기준으로 정렬
    grid = {}
    max_row = 0
    max_col = 0

    for cell in cells:
        row = cell.get('rowIndex', 0)
        col = cell.get('columnIndex', 0)
        text = cell.get('cellTextLines', [{}])[0].get('cellWords', [{}])[0].get('inferText', '')

        if row not in grid:
            grid[row] = {}
        grid[row][col] = text

        max_row = max(max_row, row)
        max_col = max(max_col, col)

    # 첫 번째 행을 헤더로 사용
    if 0 not in grid:
        return []

    headers = [grid[0].get(i, f'col_{i}') for i in range(max_col + 1)]

    # 데이터 행 파싱
    rows = []
    for row_idx in range(1, max_row + 1):
        if row_idx not in grid:
            continue

        row_dict = {}
        for col_idx, header in enumerate(headers):
            row_dict[header] = grid[row_idx].get(col_idx, '')

        rows.append(row_dict)

    return rows


def parse_registry_tables(ocr_results: List[dict]) -> Dict[str, any]:
    """
    OCR 결과에서 등기부등본 표 데이터 추출

    Args:
        ocr_results: 각 페이지의 OCR 결과 리스트

    Returns:
        파싱된 표 데이터 (owners, gapgu, eulgu)
    """
    owners = []
    gapgu = []
    eulgu = []

    for page_data in ocr_results:
        if 'ocr_result' not in page_data:
            continue

        ocr_result = page_data['ocr_result']

        # Clova OCR 테이블 구조: result["images"][0]["tables"]
        if 'images' not in ocr_result or not ocr_result['images']:
            continue

        image_data = ocr_result['images'][0]
        if 'tables' not in image_data:
            continue

        tables = image_data['tables']

        # 페이지 텍스트로 테이블 종류 판단
        page_text = page_data.get('text', '')

        for table in tables:
            parsed_rows = parse_table_from_ocr(table)

            if not parsed_rows:
                continue

            # 테이블 종류 판단 (헤더 또는 주변 텍스트 기반)
            first_row_keys = list(parsed_rows[0].keys()) if parsed_rows else []

            # "소유지분현황" 표
            if any('소유지분' in key or '소유자' in key for key in first_row_keys):
                owners.extend(parsed_rows)
            # "갑구" 관련 표
            elif any('순위번호' in key and ('등기목적' in key or '접수' in key) for key in first_row_keys):
                if '소유권' in page_text or '갑구' in page_text:
                    gapgu.extend(parsed_rows)
                elif '저당권' in page_text or '전세권' in page_text or '을구' in page_text:
                    eulgu.extend(parsed_rows)
            # 페이지 컨텍스트로 판단
            elif '소유지분' in page_text and not owners:
                owners.extend(parsed_rows)
            elif ('저당권' in page_text or '전세권' in page_text) and parsed_rows:
                eulgu.extend(parsed_rows)

    return {
        'owners': owners,
        'gapgu': gapgu,
        'eulgu': eulgu
    }


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
