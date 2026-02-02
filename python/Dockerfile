# NPLogic Python Backend
# OCR + 추천 시스템 서버

FROM python:3.11-slim

# 시스템 의존성 설치 (Poppler, Tesseract)
RUN apt-get update && apt-get install -y --no-install-recommends \
    poppler-utils \
    tesseract-ocr \
    tesseract-ocr-kor \
    libgl1 \
    libglib2.0-0 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# 작업 디렉토리
WORKDIR /app

# Python 의존성 먼저 복사 (캐시 활용)
COPY requirements.txt .

# Python 패키지 설치
RUN pip install --no-cache-dir -r requirements.txt

# 애플리케이션 코드 복사
COPY . .

# 포트 노출
EXPOSE 8000

# 헬스체크
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8000/api/health || exit 1

# 서버 실행
CMD ["uvicorn", "server:app", "--host", "0.0.0.0", "--port", "8000"]
