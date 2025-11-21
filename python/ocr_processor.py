"""
NPLogic OCR Processor

등기부등본 PDF에서 데이터를 추출하는 OCR 프로세서
실제 클라이언트 제공 코드로 대체 필요
"""

import sys
import json
from pathlib import Path


def process_pdf(pdf_path: str) -> dict:
    """
    PDF 파일을 OCR 처리하여 데이터 추출
    
    Args:
        pdf_path: PDF 파일 경로
        
    Returns:
        추출된 데이터 딕셔너리
    """
    # TODO: 클라이언트 제공 OCR 코드로 대체
    
    # 샘플 출력 (실제 구현 필요)
    result = {
        "success": True,
        "file_path": pdf_path,
        "registry_type": "토지",
        "registry_number": "1234-5678-901234",
        "owners": [
            {
                "name": "홍길동",
                "regno": "123456-1******",
                "share_ratio": "1/2"
            },
            {
                "name": "김철수",
                "regno": "234567-1******",
                "share_ratio": "1/2"
            }
        ],
        "rights": [
            {
                "type": "근저당",
                "order": 1,
                "holder": "○○은행",
                "amount": 100000000,
                "date": "2023-01-15",
                "number": "2023-접수-12345"
            },
            {
                "type": "가압류",
                "order": 2,
                "holder": "채권자A",
                "amount": 50000000,
                "date": "2023-06-20",
                "number": "2023-접수-67890"
            }
        ],
        "land_info": {
            "address": "서울특별시 강남구 테헤란로 123",
            "area": 85.5,
            "land_category": "대"
        }
    }
    
    return result


def main():
    """메인 함수"""
    if len(sys.argv) < 2:
        error = {
            "success": False,
            "error": "PDF 파일 경로가 제공되지 않았습니다."
        }
        print(json.dumps(error, ensure_ascii=False))
        sys.exit(1)
    
    pdf_path = sys.argv[1]
    
    # 파일 존재 확인
    if not Path(pdf_path).exists():
        error = {
            "success": False,
            "error": f"파일을 찾을 수 없습니다: {pdf_path}"
        }
        print(json.dumps(error, ensure_ascii=False))
        sys.exit(1)
    
    try:
        # OCR 처리
        result = process_pdf(pdf_path)
        
        # JSON 출력
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

