# NPLogic.Data - 데이터 접근 계층

**Parent:** ../AGENTS.md
**Generated:** 2026-02-02

## 목적

NPLogic의 데이터 접근 계층으로, Supabase (PostgreSQL) 데이터베이스와의 모든 상호작용을 담당합니다. Repository 패턴을 사용하여 데이터 접근 로직을 캡슐화합니다.

## 프로젝트 정보

- **타겟 프레임워크**: .NET 10.0
- **출력 타입**: 클래스 라이브러리
- **데이터베이스**: Supabase (PostgreSQL + PostgREST API)
- **Supabase Project ID**: `nlddampvgxamaukflqhd`
- **의존성**: NPLogic.Core (도메인 모델)

## 폴더 구조

| 폴더 | 파일 수 | 설명 |
|------|---------|------|
| **Repositories/** | 27+ | 데이터 접근 리포지토리 |
| **Services/** | 3+ | 데이터 관련 서비스 |
| **Interfaces/** | 27+ | 리포지토리 인터페이스 |
| **Mappers/** | 10+ | DTO ↔ 도메인 모델 매핑 |
| **DTOs/** | 15+ | 데이터 전송 객체 |

## Supabase 테이블 구조 (42개)

### 차주 관련 (5개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `borrowers` | 차주 기본 정보 | `BorrowerRepository` |
| `borrower_restructurings` | 회생차주 정보 | `BorrowerRestructuringRepository` |
| `related_borrowers` | 관련차주 관계 | `RelatedBorrowerRepository` |
| `borrower_contacts` | 차주 연락처 | `BorrowerContactRepository` |
| `borrower_history` | 차주 이력 | `BorrowerHistoryRepository` |

### 물건 관련 (8개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `properties` | 물건 기본 정보 | `PropertyRepository` |
| `property_evaluations` | 감정평가 정보 | `PropertyEvaluationRepository` |
| `property_prior_rights` | 선순위 권리 | `PropertyPriorRightRepository` |
| `residential_evaluations` | 주택 평가 | `ResidentialEvaluationRepository` |
| `commercial_evaluations` | 상업시설 평가 | `CommercialEvaluationRepository` |
| `industrial_evaluations` | 공장 평가 | `IndustrialEvaluationRepository` |
| `property_images` | 물건 이미지 | `PropertyImageRepository` |
| `property_history` | 물건 이력 | `PropertyHistoryRepository` |

### 대출 관련 (7개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `loans` | 대출 기본 정보 | `LoanRepository` |
| `loan_interests` | 이자 정보 | `LoanInterestRepository` |
| `loan_repayments` | 상환 내역 | `LoanRepaymentRepository` |
| `credit_guarantees` | 신용보증서 | `CreditGuaranteeRepository` |
| `loan_collaterals` | 담보 연결 | `LoanCollateralRepository` |
| `loan_schedules` | 상환 계획 | `LoanScheduleRepository` |
| `loan_history` | 대출 이력 | `LoanHistoryRepository` |

### 경매 관련 (4개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `auctions` | 경매 기본 정보 | `AuctionRepository` |
| `auction_schedules` | 경매 기일 | `AuctionScheduleRepository` |
| `auction_results` | 경매 결과 | `AuctionResultRepository` |
| `auction_documents` | 경매 서류 | `AuctionDocumentRepository` |

### 권리 분석 관련 (3개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `right_analyses` | 권리 분석 결과 | `RightAnalysisRepository` |
| `right_analysis_details` | 분석 상세 | `RightAnalysisDetailRepository` |
| `registry_sheet_data` | 등기부 원본 데이터 | `RegistrySheetDataRepository` |

### 사용자 및 권한 (5개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `users` | 사용자 정보 | `UserRepository` |
| `roles` | 역할 | `RoleRepository` |
| `permissions` | 권한 | `PermissionRepository` |
| `user_roles` | 사용자-역할 매핑 | `UserRoleRepository` |
| `role_permissions` | 역할-권한 매핑 | `RolePermissionRepository` |

### 기타 (10개)
| 테이블명 | 설명 | Repository |
|---------|------|------------|
| `attachments` | 첨부파일 | `AttachmentRepository` |
| `audit_logs` | 감사로그 | `AuditLogRepository` |
| `notifications` | 알림 | `NotificationRepository` |
| `settings` | 설정 | `SettingRepository` |
| `code_groups` | 코드그룹 | `CodeGroupRepository` |
| `codes` | 공통코드 | `CodeRepository` |
| `templates` | 템플릿 | `TemplateRepository` |
| `reports` | 보고서 | `ReportRepository` |
| `dashboards` | 대시보드 설정 | `DashboardRepository` |
| `bookmarks` | 즐겨찾기 | `BookmarkRepository` |

## 핵심 서비스 (Services/)

### SupabaseService
**목적**: Supabase 클라이언트 관리 및 연결

```csharp
public class SupabaseService
{
    private readonly Supabase.Client _client;

    public SupabaseService()
    {
        var url = "https://nlddampvgxamaukflqhd.supabase.co";
        var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
        _client = new Supabase.Client(url, key);
    }

    public Supabase.Client GetClient() => _client;

    public async Task<bool> IsConnectedAsync()
    {
        // 연결 상태 확인
    }
}
```

### AuthService
**목적**: 사용자 인증 및 세션 관리

```csharp
public class AuthService
{
    private readonly Supabase.Client _supabase;
    private readonly SessionStorageService _sessionStorage;

    // 로그인
    public async Task<User> LoginAsync(string email, string password);

    // 자동 로그인
    public async Task<User> AutoLoginAsync();

    // 로그아웃
    public async Task LogoutAsync();

    // 현재 사용자
    public User CurrentUser { get; private set; }

    // 세션 토큰
    public string AccessToken { get; private set; }
}
```

### SessionStorageService
**목적**: 세션 데이터 암호화 저장

```csharp
public class SessionStorageService
{
    // 세션 저장
    public void SaveSession(string accessToken, string refreshToken);

    // 세션 로드
    public (string accessToken, string refreshToken)? LoadSession();

    // 세션 삭제
    public void ClearSession();

    // 암호화/복호화
    private string Encrypt(string data);
    private string Decrypt(string encryptedData);
}
```

### PermissionService
**목적**: 권한 검증 및 관리

```csharp
public class PermissionService
{
    // 권한 확인
    public async Task<bool> HasPermissionAsync(int userId, string permissionCode);

    // 사용자 권한 목록
    public async Task<List<Permission>> GetUserPermissionsAsync(int userId);

    // 역할별 권한
    public async Task<List<Permission>> GetRolePermissionsAsync(int roleId);
}
```

## Repository 패턴

### 기본 인터페이스
```csharp
public interface IRepository<T> where T : class
{
    // 조회
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

    // 생성
    Task<T> CreateAsync(T entity);
    Task<List<T>> CreateRangeAsync(List<T> entities);

    // 수정
    Task<T> UpdateAsync(T entity);
    Task<List<T>> UpdateRangeAsync(List<T> entities);

    // 삭제
    Task DeleteAsync(int id);
    Task DeleteRangeAsync(List<int> ids);

    // 페이징
    Task<(List<T> items, int total)> GetPagedAsync(int page, int pageSize);
}
```

### Repository 구현 예시
```csharp
public class PropertyRepository : IRepository<Property>
{
    private readonly Supabase.Client _supabase;

    public PropertyRepository(SupabaseService supabaseService)
    {
        _supabase = supabaseService.GetClient();
    }

    public async Task<Property?> GetByIdAsync(int id)
    {
        var response = await _supabase
            .From<PropertyDto>()
            .Where(x => x.Id == id)
            .Single();

        return PropertyMapper.ToModel(response);
    }

    public async Task<List<Property>> GetAllAsync()
    {
        var response = await _supabase
            .From<PropertyDto>()
            .Get();

        return response.Models
            .Select(PropertyMapper.ToModel)
            .ToList();
    }

    public async Task<Property> CreateAsync(Property property)
    {
        var dto = PropertyMapper.ToDto(property);
        var response = await _supabase
            .From<PropertyDto>()
            .Insert(dto);

        return PropertyMapper.ToModel(response.Model);
    }

    // 물건 관련 특화 메서드
    public async Task<List<Property>> GetByBorrowerSerialAsync(string borrowerSerial)
    {
        var response = await _supabase
            .From<PropertyDto>()
            .Where(x => x.BorrowerSerial == borrowerSerial)
            .Get();

        return response.Models
            .Select(PropertyMapper.ToModel)
            .ToList();
    }

    public async Task<List<Property>> SearchByAddressAsync(string address)
    {
        var response = await _supabase
            .From<PropertyDto>()
            .Filter("address", Operator.ILike, $"%{address}%")
            .Get();

        return response.Models
            .Select(PropertyMapper.ToModel)
            .ToList();
    }
}
```

## 주요 Repository 특화 메서드

### BorrowerRepository
```csharp
public class BorrowerRepository
{
    // 차주 검색
    Task<List<Borrower>> SearchAsync(string keyword);

    // 회생 차주만 조회
    Task<List<Borrower>> GetRestructuringBorrowersAsync();

    // 대출잔액 범위 조회
    Task<List<Borrower>> GetByOutstandingRangeAsync(decimal min, decimal max);

    // 관련차주 포함 조회
    Task<Borrower> GetWithRelatedBorrowersAsync(string borrowerSerial);
}
```

### LoanRepository
```csharp
public class LoanRepository
{
    // 계좌번호로 조회
    Task<Loan?> GetByAccountNumberAsync(string accountNumber);

    // 차주별 대출 목록
    Task<List<Loan>> GetByBorrowerSerialAsync(string borrowerSerial);

    // 연체 대출 조회
    Task<List<Loan>> GetOverdueLoansAsync();

    // 대출 총계 계산
    Task<decimal> GetTotalOutstandingAsync();

    // 상환 내역 포함 조회
    Task<Loan> GetWithRepaymentsAsync(int loanId);
}
```

### PropertyRepository
```csharp
public class PropertyRepository
{
    // 주소 검색
    Task<List<Property>> SearchByAddressAsync(string address);

    // 물건 종류별 조회
    Task<List<Property>> GetByPropertyTypeAsync(PropertyType type);

    // 평가액 범위 조회
    Task<List<Property>> GetByEvaluationRangeAsync(decimal min, decimal max);

    // 경매 진행중 물건
    Task<List<Property>> GetAuctionInProgressAsync();

    // 평가 정보 포함 조회
    Task<Property> GetWithEvaluationAsync(int propertyId);
}
```

### AuctionRepository
```csharp
public class AuctionRepository
{
    // 사건번호로 조회
    Task<Auction?> GetByCaseNumberAsync(string caseNumber);

    // 관할법원별 조회
    Task<List<Auction>> GetByCourtAsync(string court);

    // 경매 기일 임박 조회
    Task<List<Auction>> GetUpcomingAuctionsAsync(int days);

    // 경매 결과 포함 조회
    Task<Auction> GetWithResultsAsync(int auctionId);
}
```

### RightAnalysisRepository
```csharp
public class RightAnalysisRepository
{
    // 물건별 최신 분석
    Task<RightAnalysis?> GetLatestByPropertyIdAsync(int propertyId);

    // 케이스별 조회
    Task<List<RightAnalysis>> GetByCaseCodeAsync(string caseCode);

    // 분석 상세 포함 조회
    Task<RightAnalysis> GetWithDetailsAsync(int analysisId);

    // 등기부 원본 데이터 조회
    Task<RegistryData> GetRegistryDataAsync(int propertyId);
}
```

## DTO 및 Mapper

### DTO (Data Transfer Object)
```csharp
[Table("properties")]
public class PropertyDto
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("borrower_serial")]
    public string BorrowerSerial { get; set; }

    [Column("property_serial")]
    public string PropertySerial { get; set; }

    [Column("property_type")]
    public string PropertyType { get; set; }

    [Column("address_1")]
    public string Address1 { get; set; }

    [Column("land_area")]
    public decimal? LandArea { get; set; }

    [Column("building_area")]
    public decimal? BuildingArea { get; set; }

    [Column("appraisal_value")]
    public decimal? AppraisalValue { get; set; }

    // ... 56개 컬럼
}
```

### Mapper (DTO ↔ 도메인 모델)
```csharp
public static class PropertyMapper
{
    public static Property ToModel(PropertyDto dto)
    {
        if (dto == null) return null;

        return new Property
        {
            Id = dto.Id,
            BorrowerSerial = dto.BorrowerSerial,
            PropertySerial = dto.PropertySerial,
            PropertyType = Enum.Parse<PropertyType>(dto.PropertyType),
            Address = new PropertyAddress
            {
                Address1 = dto.Address1,
                Address2 = dto.Address2,
                Address3 = dto.Address3,
                Address4 = dto.Address4
            },
            LandArea = dto.LandArea,
            BuildingArea = dto.BuildingArea,
            AppraisalValue = dto.AppraisalValue,
            // ... 나머지 매핑
        };
    }

    public static PropertyDto ToDto(Property model)
    {
        if (model == null) return null;

        return new PropertyDto
        {
            Id = model.Id,
            BorrowerSerial = model.BorrowerSerial,
            PropertySerial = model.PropertySerial,
            PropertyType = model.PropertyType.ToString(),
            Address1 = model.Address?.Address1,
            Address2 = model.Address?.Address2,
            Address3 = model.Address?.Address3,
            Address4 = model.Address?.Address4,
            LandArea = model.LandArea,
            BuildingArea = model.BuildingArea,
            AppraisalValue = model.AppraisalValue,
            // ... 나머지 매핑
        };
    }

    public static List<Property> ToModelList(List<PropertyDto> dtos)
        => dtos?.Select(ToModel).ToList() ?? new List<Property>();

    public static List<PropertyDto> ToDtoList(List<Property> models)
        => models?.Select(ToDto).ToList() ?? new List<PropertyDto>();
}
```

## AI 에이전트 작업 지침

### Repository 작성 원칙

1. **인터페이스 우선**
   - 모든 Repository는 인터페이스 정의
   - `IRepository<T>` 상속
   - DI 컨테이너에 인터페이스로 등록

2. **Supabase 클라이언트 사용**
   ```csharp
   var response = await _supabase
       .From<TDto>()
       .Select("*")
       .Where(x => x.Id == id)
       .Single();
   ```

3. **에러 처리**
   ```csharp
   try
   {
       var response = await _supabase.From<PropertyDto>().Get();
       return PropertyMapper.ToModelList(response.Models);
   }
   catch (Exception ex)
   {
       _logger.LogError(ex, "물건 조회 실패");
       throw new DataAccessException("물건 조회에 실패했습니다.", ex);
   }
   ```

4. **비동기 패턴**
   - 모든 DB 작업은 비동기 (async/await)
   - CancellationToken 지원

### 쿼리 작성 가이드

#### 기본 조회
```csharp
// 전체 조회
var response = await _supabase.From<PropertyDto>().Get();

// 조건 조회
var response = await _supabase
    .From<PropertyDto>()
    .Where(x => x.BorrowerSerial == serial)
    .Get();

// 단건 조회
var response = await _supabase
    .From<PropertyDto>()
    .Where(x => x.Id == id)
    .Single();
```

#### 필터링
```csharp
// LIKE 검색
var response = await _supabase
    .From<PropertyDto>()
    .Filter("address_1", Operator.ILike, $"%{keyword}%")
    .Get();

// 범위 검색
var response = await _supabase
    .From<PropertyDto>()
    .Filter("appraisal_value", Operator.GreaterThanOrEqual, minValue)
    .Filter("appraisal_value", Operator.LessThanOrEqual, maxValue)
    .Get();

// IN 검색
var response = await _supabase
    .From<PropertyDto>()
    .Filter("property_type", Operator.In, new[] { "Residential", "Commercial" })
    .Get();
```

#### 정렬 및 페이징
```csharp
// 정렬
var response = await _supabase
    .From<PropertyDto>()
    .Order("created_at", Ordering.Descending)
    .Get();

// 페이징
var response = await _supabase
    .From<PropertyDto>()
    .Range((page - 1) * pageSize, page * pageSize - 1)
    .Get();

// 카운트
var count = await _supabase
    .From<PropertyDto>()
    .Count(CountType.Exact);
```

#### JOIN (Foreign Key 관계)
```csharp
// 관련 데이터 포함
var response = await _supabase
    .From<PropertyDto>()
    .Select("*, property_evaluations(*)")
    .Where(x => x.Id == id)
    .Single();
```

### 새 Repository 추가 순서

1. **인터페이스 정의** (Interfaces/)
   ```csharp
   public interface IMyRepository : IRepository<MyModel>
   {
       Task<MyModel> GetByCustomFieldAsync(string field);
   }
   ```

2. **DTO 작성** (DTOs/)
   ```csharp
   [Table("my_table")]
   public class MyDto
   {
       [PrimaryKey("id")]
       public int Id { get; set; }
       // 컬럼들...
   }
   ```

3. **Mapper 작성** (Mappers/)
   ```csharp
   public static class MyMapper
   {
       public static MyModel ToModel(MyDto dto) { }
       public static MyDto ToDto(MyModel model) { }
   }
   ```

4. **Repository 구현** (Repositories/)
   ```csharp
   public class MyRepository : IMyRepository
   {
       private readonly Supabase.Client _supabase;

       public MyRepository(SupabaseService supabaseService)
       {
           _supabase = supabaseService.GetClient();
       }

       // 메서드 구현...
   }
   ```

5. **DI 등록** (App.xaml.cs에서)
   ```csharp
   services.AddSingleton<IMyRepository, MyRepository>();
   ```

6. **통합 테스트 작성**
   ```csharp
   [Fact]
   public async Task MyRepository_Create_ShouldSaveToDatabase()
   {
       // Arrange
       var repository = new MyRepository(_supabaseService);
       var model = new MyModel { /* ... */ };

       // Act
       var result = await repository.CreateAsync(model);

       // Assert
       Assert.NotNull(result);
       Assert.True(result.Id > 0);
   }
   ```

### 테스트 요구사항

#### 통합 테스트
- Repository와 실제 Supabase 연동 테스트
- 테스트 데이터베이스 사용
- 트랜잭션 롤백으로 데이터 정리

```csharp
public class PropertyRepositoryTests : IDisposable
{
    private readonly PropertyRepository _repository;
    private readonly List<int> _createdIds = new();

    public PropertyRepositoryTests()
    {
        // 테스트 Supabase 클라이언트 설정
        var supabaseService = new SupabaseService(useTestDb: true);
        _repository = new PropertyRepository(supabaseService);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnProperty()
    {
        // Arrange
        var property = new Property { /* 테스트 데이터 */ };
        var created = await _repository.CreateAsync(property);
        _createdIds.Add(created.Id);

        // Act
        var result = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    public void Dispose()
    {
        // 테스트 데이터 정리
        foreach (var id in _createdIds)
        {
            _repository.DeleteAsync(id).Wait();
        }
    }
}
```

## 성능 최적화

### 일괄 처리
```csharp
// 좋음: 일괄 생성
public async Task<List<Property>> CreateRangeAsync(List<Property> properties)
{
    var dtos = PropertyMapper.ToDtoList(properties);
    var response = await _supabase
        .From<PropertyDto>()
        .Insert(dtos);
    return PropertyMapper.ToModelList(response.Models);
}

// 나쁨: 루프 안에서 개별 생성
foreach (var property in properties)
{
    await CreateAsync(property); // N번 DB 호출
}
```

### 선택적 로딩
```csharp
// 필요한 컬럼만 조회
var response = await _supabase
    .From<PropertyDto>()
    .Select("id, property_serial, address_1")
    .Get();
```

### 캐싱
```csharp
// 자주 사용되는 공통코드는 캐싱
private static List<Code>? _cachedCodes;

public async Task<List<Code>> GetCodesAsync()
{
    if (_cachedCodes != null)
        return _cachedCodes;

    _cachedCodes = await _supabase.From<CodeDto>().Get()
        .Select(CodeMapper.ToModel).ToList();

    return _cachedCodes;
}
```

## 트랜잭션 처리

Supabase는 PostgreSQL 트랜잭션을 지원하지만, postgrest-csharp 클라이언트는 직접 지원하지 않습니다. 대신 다음 전략 사용:

### 1. RPC 함수 사용
```sql
-- Supabase에서 트랜잭션 함수 생성
CREATE OR REPLACE FUNCTION create_property_with_evaluation(
    p_property JSONB,
    p_evaluation JSONB
) RETURNS JSONB AS $$
DECLARE
    v_property_id INT;
BEGIN
    -- 트랜잭션 시작 (함수는 자동으로 트랜잭션)
    INSERT INTO properties (...) VALUES (...) RETURNING id INTO v_property_id;
    INSERT INTO property_evaluations (...) VALUES (...);

    RETURN jsonb_build_object('property_id', v_property_id);
END;
$$ LANGUAGE plpgsql;
```

```csharp
// C#에서 RPC 호출
public async Task<int> CreatePropertyWithEvaluationAsync(
    Property property,
    PropertyEvaluation evaluation)
{
    var result = await _supabase.Rpc("create_property_with_evaluation", new
    {
        p_property = PropertyMapper.ToDto(property),
        p_evaluation = PropertyEvaluationMapper.ToDto(evaluation)
    });

    return result["property_id"].ToObject<int>();
}
```

### 2. 보상 트랜잭션 패턴
```csharp
public async Task<bool> CreatePropertyWithEvaluationAsync(
    Property property,
    PropertyEvaluation evaluation)
{
    int? propertyId = null;
    try
    {
        // 1. Property 생성
        var createdProperty = await _propertyRepository.CreateAsync(property);
        propertyId = createdProperty.Id;

        // 2. Evaluation 생성
        evaluation.PropertyId = propertyId.Value;
        await _evaluationRepository.CreateAsync(evaluation);

        return true;
    }
    catch (Exception ex)
    {
        // 롤백: Property 삭제
        if (propertyId.HasValue)
        {
            try
            {
                await _propertyRepository.DeleteAsync(propertyId.Value);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "롤백 실패");
            }
        }

        _logger.LogError(ex, "생성 실패");
        throw;
    }
}
```

## 외부 의존성

| 패키지 | 버전 | 용도 |
|--------|------|------|
| `supabase-csharp` | latest | Supabase 클라이언트 |
| `postgrest-csharp` | latest | PostgREST API 클라이언트 |
| `Newtonsoft.Json` | latest | JSON 직렬화 |
| `Microsoft.Extensions.Logging` | latest | 로깅 |

## 데이터 마이그레이션

Supabase 마이그레이션은 SQL 파일로 관리:

```sql
-- migrations/20240101_create_properties.sql
CREATE TABLE properties (
    id SERIAL PRIMARY KEY,
    borrower_serial VARCHAR(50) NOT NULL,
    property_serial VARCHAR(50) NOT NULL,
    property_type VARCHAR(50),
    -- ... 56개 컬럼
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_properties_borrower ON properties(borrower_serial);
CREATE INDEX idx_properties_serial ON properties(property_serial);
```

## 참고 사항

### Supabase 제약사항
- Row Level Security (RLS) 정책 준수
- API Rate Limiting 고려
- 최대 페이로드 크기: 1MB

### 보안
- API 키는 환경변수로 관리
- RLS 정책으로 데이터 접근 제어
- 민감한 데이터는 암호화 저장

### 모니터링
- Supabase 대시보드에서 쿼리 성능 모니터링
- 느린 쿼리 최적화
- 인덱스 추가

## 다음 단계

- 새 Repository 추가 시 위 가이드라인 준수
- DTO와 Mapper 작성 필수
- 통합 테스트 작성
- 성능 최적화 (일괄 처리, 캐싱) 적용
