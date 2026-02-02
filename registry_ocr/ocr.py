import os
import cv2
import json
import base64
import requests
import numpy as np
import pytesseract
import uuid
import tempfile

from PIL import Image
from pdf2image import convert_from_path


class ClovaOCR:
    def __init__(self, secret_key: str, invoke_url: str, dpi: int = 300, enable_table: bool = False):
        self.secret_key = secret_key
        self.invoke_url = invoke_url
        self.dpi = dpi
        self.enable_table = enable_table
        self.session = requests.Session()

    def ocr_image(self, image_path: str):
        with open(image_path, "rb") as f:
            image_data = base64.b64encode(f.read()).decode('utf-8')

        headers = {
            "X-OCR-SECRET": self.secret_key,
            "Content-Type": "application/json",
        }

        data = {
            "images": [
                {
                    "format": "jpg",
                    "name": os.path.basename(image_path),
                    "data": image_data,
                }
            ],
            "requestId": "sample_id",
            "version": "V2",
            "timestamp": 0,
            "enableTableDetection": self.enable_table,
        }

        resp = self.session.post(self.invoke_url, headers=headers, data=json.dumps(data))
        if resp.status_code == 200:
            return resp.json()
        else:
            print("OCR API 오류:", resp.status_code, resp.text)
            return {}

    def close(self) -> None:
        try:
            self.session.close()
        except Exception:
            pass


class ClovaOCRProcessor:
    def __init__(self, secret_key: str, invoke_url: str, dpi: int = 300, enable_table: bool = False):
        self.ocr = ClovaOCR(secret_key, invoke_url, dpi, enable_table)

    def process_image(self, image: Image.Image, apply_sharpening: bool = False):
        if apply_sharpening:
            img = cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR)
            kernel = np.array([[0, -1, 0], [-1, 5, -1], [0, -1, 0]])
            img = cv2.filter2D(img, -1, kernel)
            temp_path = os.path.join(tempfile.gettempdir(), f"temp_sharpened_{os.getpid()}_{uuid.uuid4().hex}.png")
            cv2.imwrite(temp_path, img)
        else:
            temp_path = os.path.join(tempfile.gettempdir(), f"temp_image_{os.getpid()}_{uuid.uuid4().hex}.png")
            image.save(temp_path)

        try:
            result = self.ocr.ocr_image(temp_path)
        finally:
            try:
                if os.path.exists(temp_path):
                    os.remove(temp_path)
            except Exception:
                pass
        return result


class SummaryPageFinder:
    KEYWORDS = ["주요 등기사항 요약", "주요 등기사항 요약 (참고용)", "주요등기사항요약", "주요등기사항요약(참고용)"]

    @staticmethod
    def _contains_keyword(text: str) -> bool:
        if not text:
            return False
        return any(k in text for k in SummaryPageFinder.KEYWORDS)

    @staticmethod
    def _extract_text_from_clova(result: dict) -> str:
        if not isinstance(result, dict):
            return ""
        texts: list[str] = []

        def walk(obj):
            if isinstance(obj, dict):
                for k, v in obj.items():
                    if k == 'inferText' and isinstance(v, str):
                        texts.append(v)
                    else:
                        walk(v)
            elif isinstance(obj, list):
                for it in obj:
                    walk(it)

        walk(result)
        # 줄바꿈 없이 연속된 문자열로 결합
        return "".join(texts)

    @staticmethod
    def find_summary_start_page(pdf_path: str, dpi: int = 200, fallback_clova: bool = True, clova_secret_key: str | None = None, clova_invoke_url: str | None = None) -> int | None:
        images = convert_from_path(pdf_path, dpi=dpi)
        # 1) 1차: pytesseract로 후방 탐색
        for i in range(len(images) - 1, -1, -1):
            img_np = np.array(images[i])
            try:
                text = pytesseract.image_to_string(img_np, lang='kor')
            except Exception:
                text = ""
            if SummaryPageFinder._contains_keyword(text):
                return i

        # 2) 실패 시 CLOVA OCR로 재탐색 (enable_table=False)
        if not fallback_clova:
            return None

        # env 기반 자동 로딩 허용
        secret = clova_secret_key or os.environ.get('CLOVA_SECRET_KEY') or os.environ.get('CLOVA_OCR_SECRET')
        url = clova_invoke_url or os.environ.get('CLOVA_INVOKE_URL') or os.environ.get('CLOVA_OCR_URL')
        if not (secret and url):
            return None

        processor = ClovaOCRProcessor(secret, url, dpi=dpi, enable_table=False)
        for i in range(len(images) - 1, -1, -1):
            img: Image.Image = images[i]
            try:
                result = processor.process_image(img, apply_sharpening=False)
                text = SummaryPageFinder._extract_text_from_clova(result)
                if SummaryPageFinder._contains_keyword(text):
                    return i
            except Exception as e:
                continue
        return None


def images_from_pdf_after(pdf_path: str, start_page: int, dpi: int = 300):
    pages = convert_from_path(pdf_path, dpi=dpi)
    if start_page is None:
        return []
    return pages[start_page:]


