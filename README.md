# NPLogic

.NET 10.0 기반 WPF 데스크톱 애플리케이션으로, 부동산 금융 업무(부채정리, 대출, 부동산 평가, 경매/공매)를 관리하는 통합 금융 시스템입니다.

## 요구사항

- .NET 10.0 SDK
- Visual Studio 2022 (17.12+) 또는 VS Code

## 빌드 및 실행

```bash
dotnet build
dotnet run --project src/NPLogic.App
```

## 프로젝트 구조

```
src/
├── NPLogic.App     # 메인 WPF 애플리케이션 (Views, ViewModels, Services)
├── NPLogic.Core    # 핵심 비즈니스 로직 및 도메인 모델
├── NPLogic.Data    # 데이터 접근 계층 (Repositories, Supabase 연동)
└── NPLogic.UI      # 재사용 가능 UI 컴포넌트

python/             # Python OCR 보조 서버 (EC2 Docker)
docs/               # 개발 문서 (ERD, 서비스 가이드 등)
reference/          # 원청 참고자료
```

## 상세 문서

- **[CLAUDE.md](CLAUDE.md)** - 프로젝트 상세 가이드 (아키텍처, DB, OCR 파이프라인 등)
- **[changelog.md](changelog.md)** - 변경 이력
- **[docs/](docs/)** - 개발 환경 설정, ERD, 비즈니스 로직 등
