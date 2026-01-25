"""
NPLogic Python Backend Server

FastAPI 서버 - OCR, 추천 등 Python 기반 기능 제공
"""

import os
import sys
import tempfile
import shutil
from pathlib import Path
from typing import Optional

from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import uvicorn

# 현재 디렉토리를 path에 추가
sys.path.insert(0, str(Path(__file__).parent))

from ocr_processor import process_pdf
from recommend_processor import process_recommend

app = FastAPI(
    title="NPLogic Backend",
    description="OCR 및 유사물건 추천 API",
    version="1.0.0"
)

# CORS 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# === 응답 모델 ===

class HealthResponse(BaseModel):
    status: str
    message: str


class OcrResponse(BaseModel):
    success: bool
    file_path: Optional[str] = None
    registry_type: Optional[str] = None
    registry_number: Optional[str] = None
    owners: Optional[list] = None
    rights: Optional[list] = None
    land_info: Optional[dict] = None
    error: Optional[str] = None


class RecommendRequest(BaseModel):
    subject: dict  # 대상 물건 정보
    candidates_source: str = "supabase"
    candidates_path: Optional[str] = None
    rule_index: Optional[int] = None
    similar_land: bool = False
    region_scope: str = "big"
    topk: int = 10


class RecommendResponse(BaseModel):
    success: bool
    results: Optional[list] = None
    error: Optional[str] = None


# === 엔드포인트 ===

@app.get("/api/health", response_model=HealthResponse)
async def health_check():
    """서버 상태 확인"""
    return HealthResponse(status="ok", message="NPLogic Backend is running")


@app.post("/api/ocr/registry", response_model=OcrResponse)
async def ocr_registry(file: UploadFile = File(...)):
    """
    등기부등본 PDF OCR 처리

    Args:
        file: 업로드된 PDF 파일

    Returns:
        OCR 처리 결과
    """
    # 파일 확장자 확인
    if not file.filename or not file.filename.lower().endswith('.pdf'):
        raise HTTPException(status_code=400, detail="PDF 파일만 업로드 가능합니다.")

    # 임시 파일에 저장
    temp_dir = tempfile.mkdtemp()
    temp_path = Path(temp_dir) / file.filename

    try:
        # 파일 저장
        with open(temp_path, "wb") as f:
            content = await file.read()
            f.write(content)

        # OCR 처리
        result = process_pdf(str(temp_path))

        return OcrResponse(**result)

    except Exception as e:
        return OcrResponse(success=False, error=str(e))

    finally:
        # 임시 파일 정리
        shutil.rmtree(temp_dir, ignore_errors=True)


@app.post("/api/recommend/similar", response_model=RecommendResponse)
async def recommend_similar(request: RecommendRequest):
    """
    유사 물건 추천

    Args:
        request: 추천 요청 (subject, candidates_source, topk 등)

    Returns:
        유사 물건 목록
    """
    try:
        result = process_recommend(
            subject=request.subject,
            candidates_source=request.candidates_source,
            candidates_path=request.candidates_path,
            rule_index=request.rule_index,
            similar_land=request.similar_land,
            region_scope=request.region_scope,
            topk=request.topk,
        )
        return RecommendResponse(success=True, results=result.get("results", []))
    except Exception as e:
        return RecommendResponse(success=False, error=str(e))


if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run(app, host="0.0.0.0", port=port)
