#!/bin/bash
# NPLogic Backend 배포 스크립트
# Ubuntu 24.04 + Docker 환경용

set -e

echo "=========================================="
echo "NPLogic Backend 배포 시작"
echo "=========================================="

# 1. Docker 설치 확인 및 설치
if ! command -v docker &> /dev/null; then
    echo "[1/5] Docker 설치 중..."
    sudo apt-get update
    sudo apt-get install -y ca-certificates curl
    sudo install -m 0755 -d /etc/apt/keyrings
    sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    sudo chmod a+r /etc/apt/keyrings/docker.asc

    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

    sudo apt-get update
    sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # 현재 사용자를 docker 그룹에 추가
    sudo usermod -aG docker $USER
    echo "Docker 설치 완료. 로그아웃 후 다시 로그인하세요."
else
    echo "[1/5] Docker 이미 설치됨"
fi

# 2. .env 파일 확인
if [ ! -f .env ]; then
    echo "[2/5] .env 파일 생성 중..."
    cp .env.example .env
    echo ""
    echo "================================================"
    echo "중요: .env 파일을 편집하여 실제 값을 입력하세요!"
    echo "  nano .env"
    echo "================================================"
    echo ""
    exit 1
else
    echo "[2/5] .env 파일 존재"
fi

# 3. logs 디렉토리 생성
echo "[3/5] logs 디렉토리 생성..."
mkdir -p logs

# 4. Docker 이미지 빌드
echo "[4/5] Docker 이미지 빌드 중..."
docker compose build

# 5. 컨테이너 실행
echo "[5/5] 컨테이너 실행 중..."
docker compose up -d

echo ""
echo "=========================================="
echo "배포 완료!"
echo "=========================================="
echo ""
echo "서버 상태 확인:"
echo "  curl http://localhost:8000/api/health"
echo ""
echo "로그 확인:"
echo "  docker compose logs -f"
echo ""
echo "서버 중지:"
echo "  docker compose down"
echo ""
