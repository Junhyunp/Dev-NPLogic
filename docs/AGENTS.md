# docs/ - 프로젝트 문서 및 개발 가이드

**Parent:** [../AGENTS.md](../AGENTS.md)

## 디렉토리 목적

이전 개발사가 전달한 NPLogic 프로젝트의 기술 문서 및 개발 가이드입니다.
개발 환경 설정, 데이터베이스 스키마, 마이그레이션, 비즈니스 로직 등 시스템 이해에 필요한 핵심 문서들이 포함되어 있습니다.

## 디렉토리 구조

```
docs/
├── AGENTS.md (이 파일)
├── ENVIRONMENT_SETUP.md     # 개발 환경 설정 가이드
├── ERD.md                   # 데이터베이스 스키마 정의
├── MIGRATION_GUIDE.md       # Supabase 마이그레이션 가이드
├── SERVICE_GUIDE.md         # 서비스 API 가이드
├── BUSINESS_LOGIC.md        # 비즈니스 로직 상세 설명
├── DEPLOYMENT_GUIDE.md      # 배포 가이드
├── .bkit-memory.json        # (자동 생성) 도구 메모리 파일
└── .pdca-status.json        # (자동 생성) PDCA 상태 파일
```

## 주요 파일 설명

### 1. ENVIRONMENT_SETUP.md - 개발 환경 설정
**목적**: 프로젝트 개발 환경을 처음 설정하는 개발자를 위한 가이드

**내용**:
- .NET 10.0 SDK 설치 및 설정
- Python 백엔드 환경 구성 (OCR, 추천 서버)
- Supabase 프로젝트 연결 설정
- 필요한 NuGet 패키지 및 Python 라이브러리

**AI 에이전트 지침**:
- 신규 개발자 온보딩 시 참고
- 환경 의존성 추가 시 이 파일 업데이트
- 실제 설치 스크립트 개선 시 반영 필요

### 2. ERD.md - 데이터베이스 스키마
**목적**: Supabase PostgreSQL 데이터베이스의 전체 테이블 구조 및 관계 정의

**내용**:
- **42개 테이블** 스키마 (2026-02-02 업데이트 완료)
- 테이블 간 관계 (FK, 참조 무결성)
- 인덱스 및 제약 조건
- RLS (Row Level Security) 정책

**주요 테이블 그룹**:
| 그룹 | 테이블 수 | 주요 테이블 |
|------|----------|-------------|
| 사용자/프로그램 | 3 | users, programs, program_users |
| 물건/차주/대출 | 5 | properties, borrowers, loans, borrower_restructuring, credit_guarantees |
| 등기부등본 | 4 | registry_documents, registry_owners, registry_rights, registry_sheet_data |
| 권리분석 | 1 | right_analysis |
| 평가 | 7 | evaluations, evaluation_cases 등 |
| 경매/공매 | 3 | auction_schedules, public_sale_schedules, auction_cases |
| 기준정보 | 8 | courts, common_codes, lease_standards 등 |
| 시스템 | 11 | settings, audit_logs, data_disks 등 |

**AI 에이전트 지침**:
- 문서는 참고용, 실제 DB와 차이 있을 수 있음
- **반드시** Supabase MCP 도구로 실제 스키마 확인 권장:
  - `mcp__supabase__list_tables` - 실제 테이블 목록 조회
  - `mcp__supabase__execute_sql` - 스키마 정보 쿼리
- 테이블 추가/변경 시 ERD.md 업데이트 필수
- 마이그레이션 파일 (`supabase/migrations/`)이 진실의 원천(Source of Truth)

### 3. MIGRATION_GUIDE.md - Supabase 마이그레이션
**목적**: 데이터베이스 스키마 변경 이력 및 마이그레이션 실행 가이드

**내용**:
- 48개 마이그레이션 파일 설명
- 마이그레이션 실행 순서
- 롤백 절차
- 환경별 적용 전략 (개발/스테이징/프로덕션)

**AI 에이전트 지침**:
- 스키마 변경 시 `mcp__supabase__apply_migration` 사용
- 마이그레이션 파일명: `YYYYMMDDHHMMSS_description.sql` 형식
- 반드시 롤백 가능하도록 작성
- 데이터 마이그레이션 시 생성된 ID에 의존하지 말 것 (예: 하드코딩 금지)

### 4. SERVICE_GUIDE.md - 서비스 API 가이드
**목적**: NPLogic.App의 핵심 서비스 계층 API 사용법

**내용**:
- `SupabaseService` - DB 연결 및 인증
- `AuthService` - 사용자 인증
- `PermissionService` - 권한 관리
- `PythonBackendService` - Python 서버 통신
- Repository 패턴 사용법

**AI 에이전트 지침**:
- 새로운 기능 개발 시 기존 서비스 재사용 우선
- 서비스는 Singleton으로 등록됨 (`App.xaml.cs`)
- 전역 접근: `App.ServiceProvider.GetRequiredService<T>()`
- 새 서비스 추가 시 DI 컨테이너 등록 필수

### 5. BUSINESS_LOGIC.md - 비즈니스 로직
**목적**: 부동산 금융 도메인의 핵심 비즈니스 규칙 및 알고리즘 설명

**내용**:
- `RightAnalysisRuleEngine` - 권리 분석 규칙 (선순위, 임차권, 조세채권)
- `XnpvCalculator` - 순현재가 계산 (NPV, IRR)
- 경매/공매 낙찰가 추정 로직
- 부채정리 시뮬레이션 규칙

**AI 에이전트 지침**:
- 비즈니스 로직은 `NPLogic.Core` 프로젝트에 위치
- 도메인 로직은 UI/데이터 접근 계층과 분리
- 규칙 변경 시 테스트 코드 필수 (있다면)
- 금융 계산 정확성 검증 필수

### 6. DEPLOYMENT_GUIDE.md - 배포 가이드
**목적**: 프로덕션 환경 배포 및 릴리스 절차

**내용**:
- WPF 애플리케이션 빌드 및 패키징
- Python 백엔드 서버 배포
- Supabase 프로덕션 환경 설정
- 버전 관리 및 릴리스 전략

**AI 에이전트 지침**:
- 배포 전 반드시 빌드 검증: `dotnet build --configuration Release`
- Python 백엔드 의존성 동결: `pip freeze > requirements.txt`
- Supabase 마이그레이션은 프로덕션 적용 전 스테이징 테스트 필수

## 자동 생성 파일

### .bkit-memory.json
- 빌더 킷(Builder Kit) 도구의 메모리 파일
- AI 에이전트 작업 이력 추적 (비공식)
- Git 무시됨 (`.gitignore`)

### .pdca-status.json
- PDCA(Plan-Do-Check-Act) 사이클 상태 추적
- AI 개발 워크플로 상태 관리 (비공식)
- Git 무시됨 (`.gitignore`)

## AI 에이전트 작업 지침

### 문서 읽기 우선순위
1. **ENVIRONMENT_SETUP.md** - 환경 설정 필요 시
2. **ERD.md** - DB 스키마 이해 필요 시 (단, 실제 DB 확인 병행)
3. **SERVICE_GUIDE.md** - 서비스 계층 사용 필요 시
4. **BUSINESS_LOGIC.md** - 비즈니스 규칙 구현/수정 시
5. **MIGRATION_GUIDE.md** - DB 변경 필요 시
6. **DEPLOYMENT_GUIDE.md** - 릴리스 준비 시

### 문서 신뢰도 주의사항
- **중요**: 이 문서들은 이전 개발사가 작성했으며 **참고용**입니다.
- 실제 코드베이스와 차이가 있을 수 있습니다.
- **진실의 원천(Source of Truth)**:
  - 코드: `src/` 디렉토리의 실제 C# 코드
  - DB 스키마: `supabase/migrations/` 마이그레이션 파일 + Supabase MCP 도구로 확인한 실제 DB
  - 설정: `appsettings.json`, `App.xaml.cs`

### 문서 업데이트가 필요한 경우
다음 작업 시 관련 문서 업데이트:
- 테이블 추가/변경 → `ERD.md`
- 마이그레이션 추가 → `MIGRATION_GUIDE.md`
- 새 서비스/Repository 추가 → `SERVICE_GUIDE.md`
- 비즈니스 규칙 변경 → `BUSINESS_LOGIC.md`
- 개발 환경 의존성 변경 → `ENVIRONMENT_SETUP.md`
- 배포 절차 변경 → `DEPLOYMENT_GUIDE.md`

### Supabase 작업 시 MCP 도구 활용
```bash
# 실제 테이블 목록 확인
mcp__supabase__list_tables

# 마이그레이션 이력 확인
mcp__supabase__list_migrations

# 스키마 정보 조회
mcp__supabase__execute_sql "SELECT * FROM information_schema.tables WHERE table_schema = 'public';"

# 마이그레이션 적용
mcp__supabase__apply_migration --name "add_new_column" --query "ALTER TABLE..."

# 어드바이저 확인 (보안, 성능)
mcp__supabase__get_advisors --type security
mcp__supabase__get_advisors --type performance
```

## 비핵심조서 관련 참고

비핵심조서(Non-Core Asset Report)는 부실채권 평가의 핵심 산출물입니다.
양식 및 샘플은 `reference/비핵심조서양식/` 폴더에 있습니다.

**비핵심조서 양식 구조** (17개 시트):
- 차주정보, 채권정보, 담보정보_부동산(63개 컬럼), 보증서, 선순위가정 등
- 개별 차주 조서 템플릿 (R-0072 시트)
- XNPV 계산용 현금흐름 시트

상세 정보는 [../reference/AGENTS.md](../reference/AGENTS.md)의 "비핵심조서양식/ 폴더" 섹션 참조.

## 관련 디렉토리
- [../AGENTS.md](../AGENTS.md) - 루트 프로젝트 가이드
- [../reference/AGENTS.md](../reference/AGENTS.md) - 원청 참고자료 (비핵심조서 양식 포함)
- [../src/AGENTS.md](../src/AGENTS.md) - 소스 코드 가이드
- [../python/AGENTS.md](../python/AGENTS.md) - Python 백엔드 가이드
