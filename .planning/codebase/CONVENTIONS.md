# Coding Conventions

**Analysis Date:** 2026-03-14

## Naming Patterns

**Files (C#):**
- PascalCase for all filenames: `PropertyRepository.cs`, `DashboardViewModel.cs`, `ExcelService.cs`
- ViewModel files end with `ViewModel`: `PropertyDetailViewModel.cs`, `DashboardViewModel.cs`
- Repository files end with `Repository`: `PropertyRepository.cs`, `BorrowerRepository.cs`
- Service files end with `Service`: `ExcelService.cs`, `SupabaseService.cs`, `AuthService.cs`
- Converter files use pattern: `BoolToVisibilityConverter.cs`, `InverseBoolConverter.cs`
- Views and Models follow type suffix pattern: `PropertyDetailView.xaml`, `Property.cs`

**Files (Python):**
- snake_case for filenames: `clova_ocr.py`, `ocr_processor.py`, `recommend_processor.py`
- Directories use snake_case: `recommend/` directory

**Classes:**
- PascalCase (C#): `PropertyRepository`, `DashboardViewModel`, `ExcelService`
- PascalCase (Python): `ClovaOCR`, `ClovaOCRProcessor`, `SummaryPageFinder`

**Properties/Variables (C#):**
- PascalCase for public properties: `PropertyNumber`, `AddressFull`, `BuildingArea`
- camelCase for private fields with underscore prefix: `_propertyRepository`, `_supabaseService`, `_currentPage`
- MVVM properties use `[ObservableProperty]` attribute with private backing field: `[ObservableProperty] private int _agreementDocPercent;`

**Methods/Functions:**
- PascalCase (C#): `GetByIdAsync()`, `ExportPropertiesToExcelAsync()`, `ReadExcelFileAsync()`
- snake_case (Python): `ocr_image()`, `process_pdf()`, `process_recommend()`, `_extract_text_from_clova()`

**Types/Enums:**
- PascalCase for all type declarations: `Property`, `PropertyTable`, `ProgramSummary`
- Exception classes end with `Exception`: `SessionExpiredException`

**Constants:**
- UPPER_SNAKE_CASE (C#) or ALL_CAPS: `SupabaseUrl`, `SupabaseKey`, `PageSize`, `InitialLoadCooldownSeconds`
- UPPER_SNAKE_CASE (Python): `CLOVA_SECRET_KEY`, `CLOVA_INVOKE_URL`, `KEYWORDS`

## Code Style

**Formatting (C#):**
- .NET 10.0 / C# 13 conventions
- Implicit using statements enabled: `<ImplicitUsings>enable</ImplicitUsings>`
- Nullable reference types enabled: `<Nullable>enable</Nullable>`
- 4-space indentation
- Opening braces on same line
- Allman style for exception handling

**Formatting (Python):**
- PEP 8 style (implied from code patterns)
- 4-space indentation
- Triple-quote docstrings for module and function documentation
- Type hints used in function signatures: `def find_summary_start_page(pdf_path: str, dpi: int = 200) -> int | None:`

**Linting (C#):**
- No explicit ESLint/linter configuration found
- Code uses implicit usings and nullable reference type annotations

**Linting (Python):**
- No explicit configuration file found (no setup.cfg, pyproject.toml, pylintrc)
- Code appears to follow PEP 8 conventions informally

## Import Organization

**C# Order:**
1. System namespaces: `using System;`, `using System.Collections.Generic;`
2. System.* namespaces: `using System.Collections.ObjectModel;`, `using System.Threading.Tasks;`
3. Third-party framework namespaces: `using CommunityToolkit.Mvvm.ComponentModel;`, `using Microsoft.Win32;`
4. NPLogic domain namespaces: `using NPLogic.Core.Models;`, `using NPLogic.Data.Repositories;`
5. Supabase/PostgREST: `using Postgrest.Constants;`

**Python Order:**
1. Standard library imports: `import os`, `import sys`, `import json`
2. Third-party imports: `import requests`, `import numpy as np`, `from PIL import Image`
3. Local module imports: `from clova_ocr import ClovaOCR, ClovaOCRProcessor`

**Path Aliases:**
- C#: Uses fully qualified namespaces (no aliases observed)
- Python: Relative imports within same directory: `from clova_ocr import ...`

## Error Handling

**C# Patterns:**
- Try-catch blocks in Repository methods wrapping Supabase calls
- Exception re-wrapping with context: `throw new Exception($"물건 목록 조회 실패: {ex.Message}", ex);`
- Custom exception: `SessionExpiredException` for session timeout handling
- Global exception handler in `App.xaml.cs` dispatcher unhandled exception event
- Inner exception chain traversal for nested exception detection
- AggregateException handling for async operations
- Return tuple `(bool success, string? error)` for operation status

**Python Patterns:**
- Try-finally blocks for resource cleanup: temp file deletion in `ClovaOCRProcessor.process_image()`
- Exception swallowing with pass statements for graceful degradation
- Return dict with `{"success": bool, "error": str}` format for operation status
- Status checking before processing: `if not CLOVA_SECRET_KEY or not CLOVA_INVOKE_URL: return {"success": False, ...}`

## Logging

**Framework:**
- C#: Serilog (NuGet package version 3.1.1)
- Python: Print statements and `Debug.WriteLine()` for diagnostic output

**Patterns (C#):**
- `System.Diagnostics.Debug.WriteLine()` for debug messages: `Debug.WriteLine("[App] Python 백엔드 서버 사전 시작...");`
- Serilog configured in NuGet but usage not visible in examined files
- Structured logging approach indicated by package inclusion

**Patterns (Python):**
- Print statements for status: `print("OCR API 오류:", resp.status_code, resp.text)`
- No explicit logging framework configuration

## Comments

**When to Comment (C#):**
- XML documentation comments (triple-slash) for public classes and methods
- Inline comments explaining complex logic or workarounds
- Section markers for logical code blocks: `// ========== 페이지네이션 상태 ==========`
- Korean comments for domain-specific context

**JSDoc/TSDoc/XML Docs (C#):**
- Extensive XML documentation tags: `<summary>`, `<param>`, `<returns>`, `<remarks>`
- Applied to public classes, methods, and important properties
- Example: `/// <summary>모든 물건 조회</summary>`

**Patterns (Python):**
- Triple-quote docstrings for functions and classes
- Parameter documentation in docstring: `Args:`, `Returns:`, `Raises:`
- Inline comments for algorithm explanation
- Example from `ocr_processor.py`:
```python
def process_pdf(pdf_path: str, extract_summary: bool = True) -> dict:
    """
    PDF 파일을 OCR 처리하여 데이터 추출

    Args:
        pdf_path: PDF 파일 경로
        extract_summary: True면 "주요 등기사항 요약" 페이지부터 추출

    Returns:
        추출된 데이터 딕셔너리
    """
```

## Function Design

**Size (C#):**
- Average repository methods: 10-25 lines
- Service methods vary: simple ones 5-10 lines, complex orchestration 30-50+ lines
- ViewModel properties often use single-expression getters
- Example small: `IsSessionExpiredException()` checks exception type (5 lines)

**Parameters (C#):**
- Favor dependency injection via constructor
- Async methods use `CancellationToken` as optional last parameter (pattern not consistently shown)
- Null coalescing: `_supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));`
- Out parameters rare; prefer tuples: `async Task<(List<string> Columns, List<Dictionary<string, object>> Data)>`

**Return Values (C#):**
- Async methods return `Task<T>` where T is domain type or tuple
- Repository methods return `List<T>`, `T?`, or tuples
- Status operations return `(bool success, object? data)` tuples
- Null safety: use nullable reference types; check with `?.` null-conditional operator

**Parameters (Python):**
- Type hints used: `def process_pdf(pdf_path: str, extract_summary: bool = True) -> dict:`
- Optional parameters with defaults: `dpi: int = 300`, `enable_table: bool = False`
- `*args` and `**kwargs` not widely observed

**Return Values (Python):**
- Dict return type for complex results with status: `{"success": bool, "error": str, "data": ...}`
- Static methods return None or status dict
- Type hints include Union and Optional: `-> int | None`, `-> str | None`

## Module Design

**Exports (C#):**
- Public classes are primary exports
- Services and Repositories are singleton/transient exports via DI
- No barrel files or re-export patterns observed
- Each file typically contains one public class
- Helper classes often internal: `class ColumnProgressInfo : ObservableObject` (internal scope)

**Barrel Files (C#):**
- Not used; namespaces provide organization
- Full qualified names required: `NPLogic.Data.Repositories.PropertyRepository`

**Exports (Python):**
- Classes and functions imported directly: `from clova_ocr import ClovaOCR, ClovaOCRProcessor`
- Module-level constants and functions available on import
- `__init__.py` used for package structure (minimal observed)

**Singleton Pattern:**
- C#: MVVM uses `[ObservableProperty]` attributes; Services registered in DI container as Singleton
- Python: `PythonBackendService.Singleton.Instance` pattern mentioned in CLAUDE.md for service access

## MVVM Conventions (C#)

**ViewModel Pattern:**
- Inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Properties decorated with `[ObservableProperty]` attribute for two-way binding
- Commands decorated with `[RelayCommand]` for ICommand implementation
- Partial class declaration for code generation
- Example:
```csharp
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private User? _currentUser;

    [RelayCommand]
    private async Task LoadData()
    {
        // command implementation
    }
}
```

**Data Binding:**
- WPF converters for type conversion: `BoolToVisibilityConverter`, `NullToVisibilityConverter`
- DataContext set in code-behind or XAML
- Value converters follow pattern: implement `IValueConverter` with `Convert()` and `ConvertBack()`

## Async/Await Patterns

**C#:**
- Methods suffixed with `Async`: `GetByIdAsync()`, `ReadExcelFileAsync()`
- Returns `Task<T>` for operations with results, `Task` for void operations
- `await` used for dependency calls: `await _supabaseService.GetClientAsync()`
- Fire-and-forget patterns with `_ = Task.Run(...)` for background initialization
- Exception handling through try-catch blocks around awaits

**Python:**
- FastAPI uses async/await: `async def`, `await` on async operations
- `@app.post()` decorators for async route handlers
- Synchronous operations run with `await Task.Run()` equivalent in C#

---

*Convention analysis: 2026-03-14*
