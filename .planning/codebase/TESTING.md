# Testing Patterns

**Analysis Date:** 2026-03-14

## Test Framework

**Runner (C#):**
- Not detected - no xUnit, NUnit, or MSTest packages in examined `.csproj` files
- No dedicated test projects found in solution structure
- Testing appears manual or ad-hoc

**Runner (Python):**
- Not detected - no pytest, unittest, or test runner configuration in `requirements.txt`
- Reference test files exist: `test.ipynb`, `test.bat`, `test_excel.py`, `test_recommend.json`
- These are example/reference tests, not part of automated test suite

**Assertion Library (C#):**
- Not applicable - no testing framework detected

**Assertion Library (Python):**
- Not configured - standard `assert` statements would be used if tests existed

**Run Commands:**
```bash
# Not configured - no test runner setup
# Manual testing expected
```

## Test File Organization

**Location (C#):**
- Not detected - no `*.Tests.cs` or `*Test.cs` files in `/src/` directories
- No `/Tests/` or `tests/` directory structure

**Location (Python):**
- Reference/example tests: `/reference/Auction-Certificate/test.ipynb`
- Manual test: `/src/NPLogic.App/test_excel.py`
- Test data: `/test_recommend.json`
- No `/tests/` or `test_*/ ` directory for automated testing

**Naming (C#):**
- Pattern not established - no test files present
- Convention would likely follow: `ClassNameTests.cs` or `ClassNameTest.cs`

**Naming (Python):**
- Reference: `test_excel.py`, `test.ipynb`
- Convention appears: `test_*.py` prefix

**Structure:**
```
# C# (not established)
# No test project directories found

# Python (reference only)
reference/Auction-Certificate/
├── test.ipynb                  # Jupyter notebook with manual tests
├── scripts/
│   └── test.bat               # Batch script runner
test_recommend.json             # Test data for recommendation
src/NPLogic.App/
└── test_excel.py              # Ad-hoc Excel functionality test
```

## Test Structure

**C# (Not Established):**
- Testing approach not documented or implemented
- Suggested pattern would use xUnit or NUnit with test classes inheriting from base test class

**Python (Reference Only):**
- Jupyter notebook (`test.ipynb`) contains inline code + markdown documentation
- No standard test suite structure (setUp/tearDown, fixtures, parameterized tests)
- Appears to be exploratory/development notebook rather than formal test suite

```python
# Reference pattern from reference code (not actual test):
# Manual testing shown in notebooks with cell-by-cell execution
# No formal test decorators or assertions
```

## Mocking

**Framework (C#):**
- No mocking framework detected in project files (no Moq, NSubstitute, etc.)

**Framework (Python):**
- No mocking framework detected in requirements.txt (no unittest.mock, pytest-mock, etc.)

**Patterns (C#):**
- Not established - no mocking examples found

**Patterns (Python):**
- Not established - no mocking examples found

**What to Mock (C#):**
- Likely candidates (based on architecture):
  - `SupabaseService` - Database/API calls
  - `AuthService` - Authentication operations
  - `PythonBackendService` - External service communication
  - Repository methods returning Task<T>
  - HTTP clients

**What NOT to Mock (C#):**
- Model/Entity classes (Property, Borrower, etc.)
- Value objects and POCOs
- Converter implementations (generally synchronous, stateless)
- Utility/helper methods

**What to Mock (Python):**
- External API calls (Clova OCR, Supabase)
- File I/O operations
- requests.Session() calls
- PDF conversion operations

**What NOT to Mock (Python):**
- Data transformation logic
- String parsing and regex operations
- Model classes (BaseModel subclasses)
- Utility functions for calculations

## Fixtures and Factories

**Test Data (C#):**
- Not detected - no fixture files or factory classes
- Mock data would typically be created inline in tests
- Suggested location: `src/NPLogic.Core/Testing/Fixtures/` or `src/*/Mocks/`

**Test Data (Python):**
- `test_recommend.json` - JSON fixture for recommendation testing
```json
{
  "subject": {
    "property_id": "...",
    "address": "..."
  },
  "candidates": [...]
}
```
- Fixtures would be placed in `/python/tests/fixtures/` if formal testing were implemented

**Factories (C#):**
- Not implemented - would use builder or factory pattern
- Suggested: `PropertyBuilder`, `BorrowerFactory` classes in test utilities
- Pattern: fluent API for building complex test objects
```csharp
// Suggested pattern (not implemented)
var property = new PropertyBuilder()
    .WithAddress("서울시 강남구")
    .WithBuildingArea(100m)
    .Build();
```

**Factories (Python):**
- Not implemented - would use factory functions or `factory_boy`
- Suggested: `/python/tests/factories.py`
- Pattern: factory functions returning initialized objects
```python
# Suggested pattern (not implemented)
def create_property(**kwargs):
    defaults = {"address": "서울", "area": 100}
    defaults.update(kwargs)
    return Property(**defaults)
```

## Coverage

**Requirements (C#):**
- Not enforced - no configuration found for minimum coverage percentage
- No coverage reporting tools detected in project setup

**Requirements (Python):**
- Not enforced - no pytest.ini or setup.cfg configuration

**View Coverage (C#):**
```bash
# Not configured
# Would use: dotnet test /p:CollectCoverage=true
```

**View Coverage (Python):**
```bash
# Not configured
# Would use: pytest --cov=. --cov-report=html
```

## Test Types

**Unit Tests (C#):**
- Not implemented
- Would test:
  - Repository methods (without database)
  - Service business logic
  - ViewModel commands and properties
  - Converter implementations
  - Utility/helper functions
- Approach: Mock Supabase client; verify repository logic

**Unit Tests (Python):**
- Not implemented
- Would test:
  - OCR text extraction logic (`_extract_text_from_clova()`)
  - String parsing and regex patterns
  - Recommendation scoring algorithms
  - Data transformation functions
- Approach: Mock external API calls; verify processing logic

**Integration Tests (C#):**
- Not implemented
- Would test:
  - End-to-end ViewModel workflows (load data → filter → update UI)
  - Repository + Service combinations
  - Supabase client interactions with test database
  - Authentication flow with session handling
- Approach: Could use Docker Supabase container for testing; would verify real data access

**Integration Tests (Python):**
- Not implemented
- Would test:
  - FastAPI endpoints end-to-end
  - OCR pipeline (PDF → image conversion → API call → parsing)
  - Recommendation pipeline (query property → fetch candidates → score → rank)
- Approach: Mock FastAPI TestClient; could test with sample PDF files

**E2E Tests (C#):**
- Not detected
- Would test WPF UI interactions, navigation, and complete workflows
- Tools: Could use Appium or WinAppDriver for desktop automation

**E2E Tests (Python):**
- Not detected
- Could be tested via HTTP client against running FastAPI server

## Common Patterns

**Async Testing (C#):**
- Not established - no async test methods found
- Suggested pattern would use `async Task` test methods
```csharp
// Suggested pattern (not implemented)
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsProperty()
{
    // Arrange
    var mockService = new Mock<SupabaseService>();
    var repository = new PropertyRepository(mockService.Object);

    // Act
    var result = await repository.GetByIdAsync(Guid.NewGuid());

    // Assert
    Assert.NotNull(result);
}
```

**Async Testing (Python):**
- Not established
- Suggested pattern with FastAPI TestClient:
```python
# Suggested pattern (not implemented)
def test_ocr_endpoint():
    with TestClient(app) as client:
        response = client.post("/api/ocr/registry", files={"file": ("test.pdf", b"...")})
        assert response.status_code == 200
        assert response.json()["success"] is True
```

**Error Testing (C#):**
- Not established
- Suggested approach: verify exception handling
```csharp
// Suggested pattern (not implemented)
[Fact]
public async Task GetByIdAsync_WithInvalidId_ThrowsException()
{
    var repository = new PropertyRepository(mockService);

    // Act & Assert
    await Assert.ThrowsAsync<Exception>(() =>
        repository.GetByIdAsync(Guid.Empty));
}
```

**Error Testing (Python):**
- Not established
- Suggested approach: verify error responses
```python
# Suggested pattern (not implemented)
def test_ocr_with_missing_file():
    with TestClient(app) as client:
        response = client.post("/api/ocr/registry", files={})
        assert response.status_code == 400
        assert response.json()["success"] is False
```

## Manual Testing Evidence

**C# Application:**
- WPF UI tested through interactive use - no automated test suite
- Database operations verified against Supabase manually
- Session handling tested through application restart scenarios

**Python Backend:**
- Reference notebook (`test.ipynb`) shows exploratory testing
- OCR functionality tested against sample PDFs
- API endpoints tested via curl or Postman (implied)
- Recommendation engine tested with JSON fixtures

## Known Testing Gaps

**C#:**
- No unit test project in solution structure
- No Repository mock/stub implementations
- No ViewModel command testing
- No XAML converter testing
- No integration test with real Supabase (or testable integration)
- No UI automation tests

**Python:**
- No pytest configuration
- No unit test for OCR text extraction
- No integration tests for FastAPI endpoints
- No mocking of Clova OCR API
- No fixture-based testing
- No parameterized tests for multiple PDF types

---

*Testing analysis: 2026-03-14*
