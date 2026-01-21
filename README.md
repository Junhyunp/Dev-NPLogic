# NPLogic

> **NPL (Non-Performing Loan) 평가 자동화 시스템**

NPLogic은 부실채권 담보물건의 권리분석, 평가, 회수율 계산을 자동화하는 Windows 데스크톱 애플리케이션입니다.

---

## 주요 기능

| 기능 | 설명 |
|------|------|
| **데이터디스크 업로드** | 4대 은행(KB/IBK/NH/신한) 엑셀 자동 매핑 |
| **권리분석** | 40+ 케이스 룰 엔진 기반 선순위 자동 판단 |
| **등기부등본 OCR** | PDF 등기부에서 소유자/권리 정보 자동 추출 |
| **평가 시스템** | 시나리오별 낙찰가 예측 및 XNPV 계산 |
| **경매/공매 관리** | 일정 관리 및 배당 시뮬레이션 |
| **유사물건 추천** | 과거 낙찰 사례 기반 자동 매칭 |
| **QA 시스템** | 평가자-PM 간 질의응답 |
| **Excel 출력** | 비핵심 전체 양식 보고서 생성 |

---

## 기술 스택

| 영역 | 기술 |
|------|------|
| **Frontend** | WPF (.NET 10), MVVM, MaterialDesignThemes |
| **Backend** | Supabase (PostgreSQL, Auth, Storage) |
| **OCR/추천** | Python (pytesseract, pandas) |
| **Excel** | ClosedXML |
| **지도** | WebView2 (Kakao/Naver Maps) |

---

## 빠른 시작

### 요구사항

- Windows 10/11 (64-bit)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [Python 3.10+](https://www.python.org/) (OCR 사용 시)

### 설치 및 실행

```bash
# 1. 저장소 클론
git clone https://github.com/your-org/nplogic.git
cd nplogic

# 2. 솔루션 복원
dotnet restore NPLogic.sln

# 3. 설정 파일 생성
cp src/NPLogic.App/appsettings.json.template src/NPLogic.App/appsettings.json
# appsettings.json에 Supabase URL/Key 입력

# 4. 빌드 및 실행
dotnet run --project src/NPLogic.App/NPLogic.App.csproj
```

### Visual Studio에서 실행

1. `NPLogic.sln` 열기
2. `NPLogic.App`을 시작 프로젝트로 설정
3. F5 (디버그 실행)

---

## 프로젝트 구조

```
nplogic/
├── src/
│   ├── NPLogic.App/      # WPF 메인 애플리케이션
│   ├── NPLogic.Core/     # 비즈니스 로직 (권리분석, XNPV 등)
│   ├── NPLogic.Data/     # Supabase 연동
│   └── NPLogic.UI/       # 공통 UI 컴포넌트
├── python/               # OCR/추천 Python 스크립트
├── docs/                 # 문서
└── tests/                # 테스트
```

---

## 문서

### 필수 문서

| 문서 | 설명 |
|------|------|
| [환경설정 가이드](docs/setup/ENVIRONMENT_SETUP.md) | 개발 환경 구축 |
| [프로젝트 구조](docs/setup/PROJECT_STRUCTURE.md) | 폴더 및 파일 구조 |
| [ERD](docs/database/ERD.md) | 데이터베이스 구조 다이어그램 |
| [마이그레이션 가이드](docs/database/MIGRATION_GUIDE.md) | DB 마이그레이션 방법 |

### 개발 문서

| 문서 | 설명 |
|------|------|
| [서비스 가이드](docs/api/SERVICE_GUIDE.md) | API/서비스 사용법 |
| [비즈니스 로직](docs/business/BUSINESS_LOGIC.md) | 권리분석, XNPV 계산 로직 |
| [배포 가이드](docs/setup/DEPLOYMENT_GUIDE.md) | 빌드 및 배포 방법 |

### 참고 문서

| 문서 | 설명 |
|------|------|
| [기술 스택](docs/architecture/TECH_STACK.md) | 아키텍처 상세 |
| [디자인 시스템](docs/design/DESIGN_SYSTEM.md) | UI/UX 가이드 |
| [화면 가이드](docs/screens/SCREEN_GUIDE.md) | 화면별 기능 설명 |

---

## 사용자 역할

| 역할 | 설명 | 주요 기능 |
|------|------|----------|
| **Admin** | 시스템 관리자 | 사용자/프로그램 관리 |
| **PM** | 프로젝트 매니저 | 물건 배정, 전체 조회 |
| **Evaluator** | 평가자 (회계사) | 할당된 물건 평가 |

---

## 주요 화면

### 대시보드
- 프로젝트별 진행 현황
- 물건 상태 통계
- QA 미회신 알림

### 물건 목록
- 고급 필터링 및 검색
- 일괄 작업 지원
- Excel 내보내기

### 물건 상세
- 기본 정보 / 등기부등본
- 권리분석 / 경매·공매
- 평가 / QA

---

## 개발 명령어

```bash
# 빌드
dotnet build NPLogic.sln

# Release 빌드
dotnet build NPLogic.sln --configuration Release

# 테스트
dotnet test NPLogic.sln

# Publish (배포용)
dotnet publish src/NPLogic.App/NPLogic.App.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish
```

---

## 환경 변수

| 변수 | 설명 | 예시 |
|------|------|------|
| `SUPABASE_URL` | Supabase 프로젝트 URL | `https://xxx.supabase.co` |
| `SUPABASE_KEY` | Supabase anon key | `eyJhbGci...` |

또는 `appsettings.json`에 설정:

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-key"
  }
}
```

---

## 라이선스

이 프로젝트는 비공개 상업용 소프트웨어입니다.

---

## 연락처

- **개발팀**: NPLogic 개발팀
- **이슈 트래커**: [GitHub Issues](https://github.com/your-org/nplogic/issues)

---**버전**: 1.0.0  
**마지막 업데이트**: 2026-01-21