# NPLogic.Core - 핵심 비즈니스 로직

**Parent:** ../AGENTS.md
**Generated:** 2026-02-02

## 목적

NPLogic의 도메인 모델과 비즈니스 로직을 담당하는 핵심 라이브러리입니다. 다른 프로젝트에 의존하지 않는 독립적인 레이어로, 부동산 금융 도메인의 순수한 비즈니스 규칙을 구현합니다.

## 프로젝트 정보

- **타겟 프레임워크**: .NET 10.0
- **출력 타입**: 클래스 라이브러리
- **의존성**: 없음 (완전히 독립적)
- **원칙**: 순수 비즈니스 로직, UI/데이터 접근 로직 분리

## 폴더 구조

| 폴더 | 파일 수 | 설명 |
|------|---------|------|
| **Models/** | 29+ | 도메인 모델 (엔티티) |
| **Services/** | 2+ | 비즈니스 로직 서비스 |
| **Enums/** | 15+ | 열거형 타입 |
| **Interfaces/** | 10+ | 인터페이스 정의 |
| **Validators/** | 8+ | 도메인 검증 로직 |
| **Exceptions/** | 5+ | 도메인 예외 |

## 주요 도메인 모델 (Models/)

### 차주 관련
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `Borrower` | 차주 (개인/법인) | 차주일련번호, 차주명, 차주형태, 미상환원금잔액 |
| `BorrowerRestructuring` | 회생차주 정보 | 회생사건번호, 관할법원, 개시결정일, 인가일 |
| `RelatedBorrower` | 관련차주 관계 | 주차주, 관련차주, 관계유형 |

### 물건 관련
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `Property` | 부동산 물건 | 물건일련번호, 물건종류, 소재지, 면적, 감정평가액 |
| `PropertyAddress` | 물건 주소 | 도로명주소, 지번주소, 상세주소 |
| `PropertyEvaluation` | 감정평가 정보 | 평가일자, 평가기관, 토지평가액, 건물평가액 |
| `PropertyPriorRight` | 선순위 권리 | 설정액, 임차보증금, 조세채권, 당해세 |

### 평가 관련
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `Evaluation` | 평가 기본 정보 | 평가일자, 평가자, 평가유형, 총평가액 |
| `ResidentialEvaluation` | 주택 평가 | 전용면적, 방/욕실 수, 층수, 엘리베이터 |
| `CommercialEvaluation` | 상업시설 평가 | 업종, 임대료, 공실률, 주차대수 |
| `IndustrialEvaluation` | 공장 평가 | 용도지역, 설비내역, 가동률, 전력용량 |

### 대출 관련
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `Loan` | 대출 채권 | 대출일련번호, 계좌번호, 대출과목, 원금잔액, 미수이자 |
| `LoanInterest` | 이자 정보 | 정상이자율, 연체이자율, 이자계산방식 |
| `LoanRepayment` | 상환 내역 | 상환일자, 상환금액, 상환유형 |
| `CreditGuarantee` | 신용보증서 | 보증기관, 보증서번호, 보증비율, 보증잔액 |

### 경매 관련
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `Auction` | 경매 정보 | 사건번호, 관할법원, 개시일자, 배당요구종기일 |
| `AuctionSchedule` | 경매 기일 | 경매회차, 경매일자, 최저입찰가, 경매결과 |
| `AuctionResult` | 경매 결과 | 낙찰금액, 낙찰자, 낙찰일자 |

### 권리 분석 관련
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `RightAnalysis` | 권리 분석 결과 | 분석일자, 분석자, 케이스코드, 결론 |
| `RightAnalysisDetail` | 분석 상세 | 등기목적, 접수일자, 권리자, 금액, 우선순위 |
| `RegistryData` | 등기부 원본 데이터 | 표제부, 갑구, 을구 JSON |

### 기타 모델
| 모델 | 설명 | 주요 속성 |
|------|------|-----------|
| `User` | 사용자 | 이메일, 이름, 역할, 부서 |
| `Permission` | 권한 | 권한코드, 권한명, 설명 |
| `AuditLog` | 감사로그 | 작업일시, 사용자, 작업유형, 대상 |
| `Attachment` | 첨부파일 | 파일명, 경로, 크기, 업로드일시 |

## 핵심 비즈니스 서비스 (Services/)

### RightAnalysisRuleEngine
**목적**: 등기부등본 권리 분석 규칙 엔진 (40+ 케이스)

**주요 메서드**:
```csharp
public class RightAnalysisRuleEngine
{
    // 전체 분석 실행
    public RightAnalysis AnalyzeProperty(Property property, RegistryData registry);

    // 케이스별 매칭
    public string MatchCase(List<RightAnalysisDetail> details);

    // 우선순위 계산
    public int CalculatePriority(RightAnalysisDetail detail);

    // 선순위 합계 계산
    public decimal CalculatePriorRightsTotal(List<RightAnalysisDetail> details);
}
```

**40+ 케이스 분류**:
| 케이스 그룹 | 케이스 수 | 예시 |
|------------|----------|------|
| **근저당권** | 10+ | 단순근저당, 공동근저당, 순위변경 |
| **가압류/가처분** | 8+ | 단순가압류, 중복가압류, 본압류 전환 |
| **소유권 이전** | 6+ | 매매, 증여, 상속, 경매낙찰 |
| **전세권/임차권** | 5+ | 주택임차, 상가임차, 전세권설정 |
| **압류** | 4+ | 국세압류, 지방세압류, 법인세압류 |
| **경매** | 3+ | 임의경매, 강제경매, 공매 |
| **기타** | 4+ | 지역권, 지상권, 분묘기지권 |

**케이스 매칭 알고리즘**:
1. 등기 목적 키워드 추출
2. 접수일자 기준 시간순 정렬
3. 우선순위 규칙 적용 (접수일자, 순위번호)
4. 금액 합산 (선순위 vs 후순위)
5. 케이스 코드 부여 (예: `CASE_MORTGAGE_001`)

### XnpvCalculator
**목적**: 순현재가치(XNPV) 계산 엔진

**주요 메서드**:
```csharp
public class XnpvCalculator
{
    // XNPV 계산
    public decimal CalculateXnpv(decimal rate, List<CashFlow> cashFlows);

    // IRR 계산
    public decimal CalculateIrr(List<CashFlow> cashFlows);

    // NPV 계산
    public decimal CalculateNpv(decimal rate, List<CashFlow> cashFlows);
}

public class CashFlow
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}
```

**사용 예시**:
```csharp
var cashFlows = new List<CashFlow>
{
    new() { Date = new DateTime(2024, 1, 1), Amount = -100000000 }, // 투자
    new() { Date = new DateTime(2024, 6, 1), Amount = 10000000 },   // 수익
    new() { Date = new DateTime(2024, 12, 1), Amount = 110000000 }  // 회수
};

var xnpv = calculator.CalculateXnpv(0.05m, cashFlows);
// XNPV 계산: 할인율 5%, 불규칙 현금흐름
```

## 주요 열거형 (Enums/)

### 차주 관련
```csharp
public enum BorrowerType
{
    Individual,      // 개인
    Corporation,     // 법인
    SoleProprietor   // 개인사업자
}

public enum RestructuringStage
{
    None,                   // 없음
    Preservation,           // 보전처분
    CommencementDecision,   // 개시결정
    Approval,               // 인가
    Termination             // 폐지
}
```

### 물건 관련
```csharp
public enum PropertyType
{
    Residential,    // 주택
    Commercial,     // 상업시설
    Industrial,     // 공장
    Land,           // 토지
    Mixed           // 복합
}

public enum EvaluationType
{
    Appraisal,      // 감정평가
    Market,         // 시세조사
    Internal        // 내부평가
}
```

### 대출 관련
```csharp
public enum LoanStatus
{
    Normal,         // 정상
    Overdue,        // 연체
    Dishonored,     // 부도
    WrittenOff      // 상각
}

public enum LoanSubject
{
    CorporateLoan,  // 기업대출
    Overdraft,      // 당좌대출
    Discounted,     // 어음할인
    Guarantee       // 지급보증
}
```

### 경매 관련
```csharp
public enum AuctionType
{
    Voluntary,      // 임의경매
    Compulsory,     // 강제경매
    PublicSale      // 공매
}

public enum AuctionResult
{
    Scheduled,      // 예정
    Passed,         // 유찰
    Sold,           // 낙찰
    Withdrawn       // 취하
}
```

## 검증 로직 (Validators/)

### FluentValidation 사용
```csharp
public class BorrowerValidator : AbstractValidator<Borrower>
{
    public BorrowerValidator()
    {
        RuleFor(x => x.BorrowerSerial)
            .NotEmpty().WithMessage("차주일련번호는 필수입니다.");

        RuleFor(x => x.BorrowerName)
            .NotEmpty().WithMessage("차주명은 필수입니다.")
            .MaximumLength(100);

        RuleFor(x => x.OutstandingPrincipal)
            .GreaterThanOrEqualTo(0).WithMessage("미상환원금은 0 이상이어야 합니다.");
    }
}
```

### 주요 Validator
| Validator | 대상 모델 | 검증 규칙 |
|-----------|----------|-----------|
| `BorrowerValidator` | `Borrower` | 필수 필드, 금액 범위 |
| `PropertyValidator` | `Property` | 주소, 면적, 평가액 |
| `LoanValidator` | `Loan` | 계좌번호, 이자율, 잔액 |
| `EvaluationValidator` | `Evaluation` | 평가일자, 평가액 |

## 도메인 예외 (Exceptions/)

```csharp
public class DomainException : Exception
{
    public string Code { get; }
    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class BorrowerNotFoundException : DomainException
{
    public BorrowerNotFoundException(string borrowerSerial)
        : base("BORROWER_NOT_FOUND", $"차주를 찾을 수 없습니다: {borrowerSerial}") { }
}

public class PropertyNotFoundException : DomainException { }
public class InvalidLoanDataException : DomainException { }
public class RightAnalysisException : DomainException { }
```

## AI 에이전트 작업 지침

### 모델 설계 원칙

1. **순수 도메인 모델**
   - UI, DB 로직 포함 금지
   - 비즈니스 규칙만 포함
   - 다른 프로젝트 참조 금지

2. **불변성 (Immutability)**
   - 가능한 경우 `init` 사용
   - 컬렉션은 `IReadOnlyList` 반환
   - 상태 변경은 명시적 메서드로

3. **Rich Domain Model**
   - Anemic Model 지양
   - 비즈니스 메서드 포함
   ```csharp
   public class Loan
   {
       public decimal OutstandingPrincipal { get; private set; }

       public void Repay(decimal amount)
       {
           if (amount <= 0)
               throw new InvalidLoanDataException("상환금액은 0보다 커야 합니다.");
           if (amount > OutstandingPrincipal)
               throw new InvalidLoanDataException("상환금액이 잔액을 초과합니다.");

           OutstandingPrincipal -= amount;
       }
   }
   ```

### 서비스 작성 원칙

1. **단일 책임 원칙**
   - 한 서비스는 한 가지 도메인 기능만
   - 예: RightAnalysisRuleEngine (권리 분석만)

2. **상태 비저장 (Stateless)**
   - 서비스는 상태를 가지지 않음
   - 모든 데이터는 메서드 파라미터로 전달

3. **순수 함수**
   - 같은 입력 → 같은 출력
   - 외부 의존성 최소화

### 새 도메인 모델 추가 순서

1. **모델 클래스 작성** (Models/)
   ```csharp
   public class MyDomainModel
   {
       public int Id { get; init; }
       public string Name { get; init; }

       // 비즈니스 메서드
       public void DoSomething() { }
   }
   ```

2. **열거형 정의** (Enums/) - 필요시
   ```csharp
   public enum MyStatus { Active, Inactive }
   ```

3. **Validator 작성** (Validators/)
   ```csharp
   public class MyDomainModelValidator : AbstractValidator<MyDomainModel> { }
   ```

4. **예외 클래스** (Exceptions/) - 필요시
   ```csharp
   public class MyDomainException : DomainException { }
   ```

5. **단위 테스트 작성**
   - 모델 비즈니스 메서드 테스트
   - Validator 검증 테스트
   - 예외 시나리오 테스트

### 테스트 요구사항

모든 비즈니스 로직은 단위 테스트 필수:

```csharp
[Fact]
public void Loan_Repay_ShouldReduceOutstandingPrincipal()
{
    // Arrange
    var loan = new Loan { OutstandingPrincipal = 100000 };

    // Act
    loan.Repay(30000);

    // Assert
    Assert.Equal(70000, loan.OutstandingPrincipal);
}

[Fact]
public void Loan_Repay_WithNegativeAmount_ShouldThrowException()
{
    // Arrange
    var loan = new Loan { OutstandingPrincipal = 100000 };

    // Act & Assert
    Assert.Throws<InvalidLoanDataException>(() => loan.Repay(-1000));
}
```

### 권리 분석 케이스 추가

새로운 권리 분석 케이스 추가 시:

1. **케이스 코드 정의**
   ```csharp
   public const string CASE_NEW_RIGHT = "CASE_NEW_RIGHT_001";
   ```

2. **매칭 규칙 추가**
   ```csharp
   public string MatchCase(List<RightAnalysisDetail> details)
   {
       // 새 케이스 조건 체크
       if (IsNewRightCase(details))
           return CASE_NEW_RIGHT;

       // 기존 케이스들...
   }
   ```

3. **우선순위 계산 로직**
   ```csharp
   private bool IsNewRightCase(List<RightAnalysisDetail> details)
   {
       // 케이스 판별 로직
       return details.Any(d => d.RegistryPurpose.Contains("새권리"));
   }
   ```

4. **테스트 케이스 작성**
   ```csharp
   [Fact]
   public void RightAnalysisRuleEngine_MatchCase_NewRight_ShouldReturnCorrectCase()
   {
       // 테스트 로직
   }
   ```

## 데이터 흐름 예시

### 권리 분석 프로세스
```
1. PropertyRepository에서 Property + RegistryData 조회
2. RightAnalysisRuleEngine.AnalyzeProperty() 호출
3. 등기부 갑구/을구 파싱
4. RightAnalysisDetail 리스트 생성
5. 우선순위 계산 (접수일자, 순위번호)
6. 케이스 매칭 (40+ 규칙)
7. 선순위 합계 계산
8. RightAnalysis 결과 반환
```

### XNPV 계산 프로세스
```
1. Loan 데이터에서 현금흐름 추출
2. CashFlow 리스트 생성 (날짜, 금액)
3. XnpvCalculator.CalculateXnpv() 호출
4. 할인율 적용
5. 순현재가치 반환
```

## 네이밍 규칙

### 모델 클래스
- PascalCase
- 명사형
- 도메인 용어 사용
- 예: `Borrower`, `Property`, `RightAnalysis`

### 속성
- PascalCase
- 명확한 이름
- 예: `OutstandingPrincipal`, `BorrowerSerial`

### 메서드
- PascalCase
- 동사로 시작
- 예: `CalculatePriority()`, `AnalyzeProperty()`

### 열거형
- PascalCase (타입 및 값)
- 복수형 지양
- 예: `LoanStatus.Normal`, `PropertyType.Residential`

## 외부 의존성

이 프로젝트는 최소한의 외부 의존성만 사용:

| 패키지 | 용도 | 비고 |
|--------|------|------|
| `FluentValidation` | 도메인 검증 | 선택적 |
| `System.Text.Json` | JSON 직렬화 | .NET 내장 |

**주의**: UI, ORM, HTTP 클라이언트 등 외부 인프라 패키지 사용 금지.

## 참고 사항

### 독립성 유지
- 이 프로젝트는 다른 NPLogic 프로젝트를 참조하지 않음
- 순수 비즈니스 로직만 포함
- 데이터 접근, UI 로직은 NPLogic.Data, NPLogic.App에서 구현

### 재사용성
- 다른 UI (WinForms, Blazor 등)에서도 사용 가능
- 다른 데이터 소스 (SQL Server, MongoDB 등)에서도 사용 가능

### 테스트 용이성
- 순수 C# 코드로 테스트 쉬움
- Mocking 불필요 (상태 비저장 서비스)

## 다음 단계

- 새 도메인 모델 추가 시 위 가이드라인 준수
- 비즈니스 로직은 모델 또는 서비스에 캡슐화
- 단위 테스트 필수 작성
- 도메인 전문가와 협업하여 규칙 검증
