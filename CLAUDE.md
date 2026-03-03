# CLAUDE.md

이 파일은 Claude Code가 프로젝트 컨텍스트를 파악하기 위한 핵심 지침이다.
상세 문서는 [PROJECT.md](PROJECT.md) 참조.

## 빌드 및 실행

```bash
dotnet build NPLogic.sln
dotnet run --project src/NPLogic.App/NPLogic.App.csproj
```

### Python 백엔드 (OCR/추천)

EC2 서버(3.34.10.57)에서 Docker로 실행 중 (포트 8000):
```bash
cd python
docker compose up -d --build
```

로컬 개발:
```bash
cd python
pip install -r requirements.txt
python server.py
```

## 프로젝트 구조

```
src/
├── NPLogic.App     # 메인 WPF 애플리케이션 (Views, ViewModels, Services)
├── NPLogic.Core    # 핵심 비즈니스 로직 및 도메인 모델
├── NPLogic.Data    # 데이터 접근 계층 (Repositories, Supabase 연동)
└── NPLogic.UI      # 재사용 가능 UI 컴포넌트

python/             # Python 보조 서버 (OCR, 유사물건 추천)
reference/          # 원청 참고자료
```

### 의존성 방향

```
NPLogic.App → NPLogic.Core, NPLogic.Data, NPLogic.UI
NPLogic.Data → NPLogic.Core
NPLogic.UI → (독립적)
NPLogic.Core → (독립적)
```

## 핵심 아키텍처

- **MVVM**: CommunityToolkit.Mvvm 기반
- **DI**: `App.xaml.cs`에서 Microsoft.Extensions.DependencyInjection으로 등록
- **전역 접근**: `App.ServiceProvider.GetRequiredService<T>()`
- **DB**: Supabase (PostgreSQL + PostgREST API)
- **Supabase Project ID**: `nlddampvgxamaukflqhd`

### 주요 서비스

- `SupabaseService`: DB 연결 및 인증 (JWT UTC/Local 시간대 보정 포함)
- `AuthService`: 사용자 인증, 자동 로그인
- `PythonBackendService`: Python OCR/추천 서버 통신 (Singleton.Instance 패턴)
- `VworldService`: VWORLD API 연동 (공시가격, PNU 조회)
- `TradeService`: 외부 Supabase RPC 기반 실거래가 연동
- `RightAnalysisRuleEngine`: 권리 분석 규칙 엔진
- `XnpvCalculator`: 순현재가 계산

### UI 네비게이션

```
MainWindow
└── DashboardView
    ├── 목록 모드: DataGrid (서버 사이드 페이지네이션)
    └── 상세 모드: 내부 탭 (RadioButton 기반)
        ├── 비핵심 (NonCoreView) → 11개 기능 탭
        ├── 등기부등본 (RegistryTab)
        ├── 권리분석
        └── 기타 탭들
```

## 등기부등본 OCR 파이프라인

```
WPF App (RegistryTabViewModel)
    → PDF 파일 선택 + 물건 자동 매칭
    → Edge Function "ocr-registry-save" (v12, verify_jwt: false) 호출
        → EC2 Python OCR 서버 (/api/ocr/registry)
        → OCR 결과 + DD 데이터 병합
        → registry_runs / basic_info / gapgu_rows / eulgu_rows 저장
    ← 응답: run 정보, 정제 데이터, 요약 이미지
```

## 알려진 주의사항

- **NonCoreView 탭 캐시**: `_tabViewCache`/`_tabViewModelCache`로 캐시됨. 데이터 갱신 필요 시 명시적 새로고침 필요.
- **DashboardView 탭 이벤트**: `_suppressInnerTabChecked`/`_suppressFunctionTabChecked` 플래그로 이중 로드 방지.
- **JWT 토큰**: `supabase-csharp`가 ES256 → Edge Function은 `verify_jwt: false` + `getUser()` 수동 검증.
- **파일명 인코딩**: .NET HttpClient 한글 파일명 → `source_pdf_name` 별도 전달.
- **세션/JWT 갱신**: 절전/잠금 복귀 시 회귀 위험 높음. `EnsureValidSessionAsync()` 참고.

## 데이터디스크 시트 매핑

| 시트 | 테이블 | 대표컬럼 수 |
|------|--------|-------------|
| 차주일반정보 (Sheet A) | `borrowers` | 9 |
| 회생차주정보 (Sheet A-1, F) | `borrower_restructuring` | 14 |
| 채권일반정보 (Sheet B, B-1) | `loans` | 14 |
| 물건정보 (Sheet C-1) | `properties` | 56 |
| 등기부등본정보 (Sheet C-2) | `registry_sheet_data` | 8 |
| 신용보증서 (Sheet D) | `credit_guarantees` | 10 |

상세 컬럼 목록은 PROJECT.md 섹션 14 및 `reference/검증/` 참조.
