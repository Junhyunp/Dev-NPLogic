# NPLogic 환경 설정

## Supabase 설정

### 프로젝트 정보
- **프로젝트 URL**: `https://vuepmhwrizaabswlgiiy.supabase.co`
- **Anon Key**: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ1ZXBtaHdyaXphYWJzd2xnaWl5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM2NDA2MzUsImV4cCI6MjA3OTIxNjYzNX0.VJ6CG3alGGVK7HYfZabHd47q2RDR6EgGMPD1GS8YNu4`

### C# 코드에서 사용 방법

```csharp
// App.xaml.cs 또는 초기화 코드에서
var supabaseService = new SupabaseService(
    "https://vuepmhwrizaabswlgiiy.supabase.co",
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ1ZXBtaHdyaXphYWJzd2xnaWl5Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM2NDA2MzUsImV4cCI6MjA3OTIxNjYzNX0.VJ6CG3alGGVK7HYfZabHd47q2RDR6EgGMPD1GS8YNu4"
);

await supabaseService.InitializeAsync();
```

## 데이터베이스 구조

### 생성된 테이블 (14개)

1. **users** - 사용자 정보 (PM, 평가자, 관리자)
2. **properties** - 물건 기본 정보
3. **data_disks** - 엑셀 데이터 디스크
4. **registry_documents** - 등기부등본 문서
5. **registry_owners** - 등기부 소유자
6. **registry_rights** - 등기부 권리 (근저당, 가압류 등)
7. **right_analysis** - 권리 분석 결과
8. **evaluations** - 평가 정보
9. **auction_schedules** - 경매 일정
10. **public_sale_schedules** - 공매 일정
11. **loan_info** - 대출 정보
12. **audit_logs** - 작업 이력 로그
13. **settings** - 시스템 설정
14. **calculation_formulas** - 계산 수식 설정

### RLS 정책

모든 테이블에 Row Level Security (RLS) 정책이 적용되어 있습니다:
- **PM & Admin**: 모든 데이터 접근 가능
- **Evaluator**: 자신에게 할당된 물건만 접근 가능
- **Settings & Formulas**: 관리자만 수정, 모든 사용자 조회 가능
- **Audit Logs**: 관리자만 조회 가능

## 생성된 C# 클래스

### Models
- `User.cs` - 사용자 모델
- `Property.cs` - 물건 모델
- `DataDisk.cs` - 데이터 디스크 모델
- `RegistryDocument.cs` - 등기부 문서 모델
- `RegistryOwner.cs` - 등기부 소유자 모델
- `RegistryRight.cs` - 등기부 권리 모델
- `Evaluation.cs` - 평가 모델

### Services
- `SupabaseService.cs` - Supabase 클라이언트 관리
- `AuthService.cs` - 인증 서비스
- `PropertyRepository.cs` - 물건 데이터 접근

## 다음 작업

### Phase 3: 로그인 화면 (예상 2-3시간)
1. LoginView.xaml 생성
2. LoginViewModel.cs 생성
3. App.xaml.cs DI 설정
4. 자동 로그인 기능

### Phase 4: 대시보드 화면 (예상 2-3시간)
1. DashboardView.xaml 생성
2. DashboardViewModel.cs 생성
3. 역할별 대시보드 구현
4. 통계 카드 데이터 바인딩

## 보안 주의사항

⚠️ **중요**: 
- Supabase URL과 Anon Key는 Git에 커밋하지 마세요
- 환경 변수 또는 사용자 설정 파일로 관리하세요
- `.gitignore`에 환경 설정 파일 추가

## 테스트 계정 (향후 추가 예정)

현재 테스트 계정이 없습니다. Supabase Dashboard에서 직접 생성하거나 회원가입 기능 구현 후 추가하세요.

