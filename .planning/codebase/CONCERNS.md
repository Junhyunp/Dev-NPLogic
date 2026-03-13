# Codebase Concerns

**Analysis Date:** 2026-03-14

## Tech Debt

### Large ViewModel Classes (>3500 LOC)

**Issue:** Multiple ViewModels exceed recommended class size limits
- Files: `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs` (4364 lines)
- Files: `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs` (3576 lines)
- Files: `src/NPLogic.App/ViewModels/SeniorRightsViewModel.cs` (2627 lines)
- Files: `src/NPLogic.App/ViewModels/EvaluationTabViewModel.cs` (2609 lines)

**Impact:**
- Difficult to maintain and test
- Increased bug surface area
- Poor code organization and responsibility separation
- Slow compilation and development feedback

**Fix approach:**
- Extract related functionality into smaller, focused ViewModels
- Create service classes for business logic extraction
- Use composition over large inheritance hierarchies
- Consider state machine patterns for complex workflows

---

### Tab Caching Architecture Complexity

**Issue:** Multiple caching and event suppression mechanisms in tab navigation
- Files: `src/NPLogic.App/Views/DashboardView.xaml.cs` (caching: `_cachedNonCoreView`, `_suppressInnerTabChecked`)
- Files: `src/NPLogic.App/Views/NonCoreView.xaml.cs` (tab cache: `_tabViewCache`, `_tabViewModelCache`, `_suppressFunctionTabChecked`)

**Impact:**
- Non-obvious state management across views
- Double-load prevention flags create fragile coupling
- Cache invalidation is manual and error-prone
- Difficult to debug tab lifecycle issues

**Fix approach:**
- Implement formal tab state machine pattern
- Use coordinator pattern instead of view-level caching
- Create TabManager service to handle all tab transitions
- Document tab lifecycle clearly in unified location

---

### Manual Tab Data Refresh Requirements

**Concern:** NonCoreView tab cache requires explicit refresh on data changes
- Files: `src/NPLogic.App/Views/NonCoreView.xaml.cs` lines 37-41
- Pattern: Tab content stale until explicitly refreshed

**Impact:**
- User sees outdated data after property changes
- Requires developers to remember to trigger refresh
- No automatic cache invalidation

**Recommendation:**
- Implement property change notifications with automatic cache invalidation
- Use weak references for cached ViewModels
- Consider observable cache pattern with expiration

---

## Known Bugs

### JWT Token DateTime Kind Mismatch

**Symptoms:** PGRST303 JWT expired errors occur during active sessions, causing mid-operation failures
- Files: `src/NPLogic.Data/Services/SupabaseService.cs` lines 236-256

**Root Cause:**
- `supabase-csharp` library returns `ExpiresAt()` with inconsistent DateTime.Kind (Local/Unspecified/Utc)
- Token expiry check fails when comparing Unspecified/Local time against UtcNow
- Particularly affects long operations (Data Disk uploads 15+ minutes)

**Current Workaround:**
- Complex DateTime.Kind detection logic with fallback calculation
- Gap detection mechanism (5+ minute silence triggers force refresh)
- Manual token refresh on system resume/unlock

**Improvement Path:**
- Upgrade `supabase-csharp` library when fixes available
- Implement more aggressive pre-emptive token refresh (5-minute threshold)
- Add explicit token validation before critical operations

---

### Session Expiry During Long Operations

**Concern:** Risk of token expiration during batch operations (Data Disk upload, mass OCR)
- Files: `src/NPLogic.Data/Services/SupabaseService.cs` lines 217-232, `src/NPLogic.App/Services/DataDiskUploadService.cs`

**Symptoms:**
- Session suddenly invalid mid-operation
- User forced to restart operation from beginning

**Current Mitigations:**
- 50-minute automatic refresh timer (JWT typically 1 hour)
- 10-minute pre-expiry refresh threshold
- Force refresh on system resume/unlock

**Workaround:** Consider request-level timeout extension or rolling refresh tokens

---

### PDF Filename Encoding Loss

**Issue:** .NET HttpClient RFC 5987 encoding breaks Korean filenames in Deno Edge Function
- Files: `src/NPLogic.Data/Repositories/RegistryRepository.cs` lines 252-254

**Symptoms:**
- Original PDF filename lost after OCR upload
- Edge Function receives garbled or encoded filename

**Current Mitigation:**
- Filename passed separately via form-data field `source_pdf_name`
- Workaround embedded in code but fragile

**Fix approach:**
- Verify Deno Edge Function properly handles `source_pdf_name` field
- Consider Base64 encoding for filename in header
- Add integration tests for multi-byte character filenames

---

## Security Considerations

### CORS Wildcard Configuration

**Risk:** Open CORS allows any origin to call Python backend endpoints
- Files: `python/server.py` lines 32-38

**Current Configuration:**
```python
allow_origins=["*"]
allow_credentials=True
```

**Impact:**
- Python OCR and recommendation endpoints accessible from any website
- Potential for abuse of OCR service and recommendation engine
- No origin validation

**Recommendations:**
- Restrict `allow_origins` to specific frontend domain(s)
- Environment-based CORS configuration for dev/prod
- Add rate limiting per origin
- Implement API key authentication

---

### JWT Manual Verification Gap

**Concern:** Edge Function uses `verify_jwt: false`, manual JWT validation missing
- Files: `src/NPLogic.Data/Repositories/RegistryRepository.cs` line 231-240 (no explicit JWT validation before calling Edge Function)

**Risk:**
- Edge Function relies on Bearer token without verification
- Potential token spoofing if headers manipulated
- No check that token belongs to current user before OCR processing

**Recommendation:**
- Implement explicit JWT validation before sensitive operations
- Verify token scopes and user permissions
- Log all OCR operations with user ID for audit trail

---

### Supabase Session Storage Not Secured

**Concern:** SessionStorageService writes session tokens to local storage
- Files: `src/NPLogic.Data/Services/SessionStorageService.cs` (location: Registry/Local Machine)

**Risk:**
- Local machine has access to raw JWT tokens
- No encryption of stored credentials
- Vulnerable to local admin attacks

**Recommendation:**
- Store only refresh tokens (shorter lived than access tokens)
- Use Windows Credential Manager or CredentialCache instead of Registry
- Implement token encryption at rest
- Add session timeout for idle periods

---

## Performance Bottlenecks

### Large ViewModel Property Change Notifications

**Problem:** PropertyDetailViewModel subscribes to many properties; all changes trigger UI updates
- Files: `src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs` (2500+ observable properties)

**Cause:** MVVM Toolkit generates change notifications for all ObservableProperty fields

**Impact:**
- Rapid property changes flood dispatcher queue
- UI becomes unresponsive during bulk updates
- Excessive memory allocations for notification objects

**Improvement Path:**
- Batch property changes with update tokens
- Defer non-critical updates until idle time
- Profile change notification volume with diagnostics
- Consider lazy-loading properties for large forms

---

### DataGrid Server-Side Pagination Not Implemented

**Concern:** Virtual collection requires full dataset in memory for large property lists
- Files: `src/NPLogic.App/Views/DashboardView.xaml.cs` (DataGrid binding)

**Impact:**
- Memory grows linearly with dataset size
- Slow initial load for 1000+ properties
- No sorting/filtering at database level

**Improvement Path:**
- Implement true server-side pagination with Supabase query limits
- Add incremental loading (infinite scroll)
- Push filtering/sorting to database layer

---

### Python OCR Processing Timeout Risk

**Problem:** 10-minute timeout may be insufficient for large PDFs
- Files: `python/server.py` line 24, `src/NPLogic.Data/Repositories/RegistryRepository.cs` line 24

**Current Timeout:** 10 minutes (600 seconds)

**Impact:**
- Large registry PDFs (50+ pages) may timeout
- No retry mechanism
- User loses upload progress

**Recommendation:**
- Monitor actual OCR processing times
- Implement exponential backoff retry with status endpoint
- Add async job queue (Celery/RQ) for long-running OCR
- Provide progress updates to client

---

## Fragile Areas

### NonCoreView Tab Loading Order Dependency

**Files:** `src/NPLogic.App/Views/NonCoreView.xaml.cs` lines 61-77, 468-490

**Why Fragile:**
- `_initialLoadDone` flag prevents duplicate loads but requires careful sequencing
- `ResetToHomeTabAsync()` called during property change but timing with `Loaded` event is fragile
- Request version tracking (`_tabLoadRequestVersion`) can get out of sync if exceptions occur
- CancellationToken cleanup may leak if cancellation not properly awaited

**Safe Modification:**
- Add state machine to track initialization phases
- Implement proper cancellation scope using `using` statements
- Add extensive logging for tab lifecycle debug
- Unit test tab loading with various property change sequences

---

### Property Number Pattern Matching

**Files:**
- `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs` lines 462-487
- `src/NPLogic.App/ViewModels/DashboardViewModel.cs` lines 1654-1672
- `src/NPLogic.App/ViewModels/EvaluationTabViewModel.cs` line 878

**Why Fragile:**
- Multiple regex patterns for property number (R-XXX-XX, R_XXX_XX, RXXX_XX, etc.)
- Sorting logic depends on parsing success (returns int.MaxValue on failure)
- No centralized pattern definition
- If pattern changes, multiple files must update

**Safe Modification:**
- Create PropertyNumberParser utility class in NPLogic.Core
- Implement single authoritative regex pattern
- Add unit tests for all observed property number formats
- Use constant for sort fallback values

**Test Coverage Gaps:**
- No tests for edge cases: empty property numbers, malformed patterns
- Sorting with mixed valid/invalid numbers untested

---

### DashboardView Tab Event Suppression

**Files:** `src/NPLogic.App/Views/DashboardView.xaml.cs` lines 414-418, 567, 1011-1015

**Why Fragile:**
- Manual `_suppressInnerTabChecked` flag prevents event re-entry
- If exception occurs while flag is set, flag remains stuck
- No guarantee flag is reset (missing finally block)
- Multiple sources of tab changes (RadioButton, command, code)

**Safe Modification:**
```csharp
// Use try-finally or IDisposable pattern instead
private class TabSuppressionScope : IDisposable
{
    private Action _restoreAction;
    public TabSuppressionScope(Action restoreAction) => _restoreAction = restoreAction;
    public void Dispose() => _restoreAction();
}

// Usage:
using (new TabSuppressionScope(() => _suppressInnerTabChecked = false))
{
    _suppressInnerTabChecked = true;
    // ... code ...
}
```

---

### Python Backend Exception Handling Too Broad

**Files:** `python/server.py` lines 136, 166; `python/ocr_processor.py` lines 163, 299

**Why Fragile:**
- Bare `except Exception` catches all errors including KeyboardInterrupt, SystemExit
- No distinction between user errors and system failures
- Client receives generic error messages with no context
- No error logging to persistent store

**Impact:**
- Difficult to diagnose OCR failures
- May silently fail critical operations
- No audit trail for debugging

**Safe Modification:**
- Catch specific exceptions (ValueError, IOError, etc.)
- Log full traceback with context (file size, page count)
- Return structured error responses with error codes
- Implement proper logging to file/service

---

### ProgramManagement Data Validation Gaps

**Files:** `src/NPLogic.App/ViewModels/ProgramManagementViewModel.cs` lines 1427-1433

**Issue:** Skips rows with empty BorrowerNumber silently
- Pattern: Bank DataDisks often include summary/total rows at bottom
- No logging of skipped row count

**Why Fragile:**
- User doesn't see which rows were skipped
- Silent data loss during upload
- Hard to reproduce if user doesn't keep original Excel

**Recommendation:**
- Log skipped rows with reason and line numbers
- Display skip summary to user after upload
- Provide skip report export for audit trail

---

## Missing Critical Features

### No Transaction Support for Multi-Table Operations

**Problem:** Data Disk uploads span multiple tables but use individual inserts
- Files: `src/NPLogic.App/Services/DataDiskUploadService.cs` (1941 lines - uploads borrowers, loans, properties, etc.)

**Blocks:**
- Partial uploads if error mid-way (orphaned records)
- Data consistency violations between related tables
- No rollback capability

**Recommendation:**
- Implement Supabase RPC function for atomic multi-table inserts
- Return transaction ID for audit/retry purposes
- Add idempotency keys for safe retries

---

### No Offline Support

**Problem:** All operations require live Supabase connection
- No caching of historical data
- No queue for offline operations

**Blocks:**
- Cannot work on slow networks
- Cannot pre-load data before going offline
- Lost work if connection drops mid-operation

**Priority:** Medium (depends on deployment environment)

---

### No Batch OCR Job Queue

**Problem:** OCR calls are synchronous; large batch uploads block UI
- Files: `src/NPLogic.App/ViewModels/RegistryTabViewModel.cs` (OCR upload)

**Blocks:**
- Cannot process multiple PDFs concurrently
- UI hangs during batch processing
- No progress estimates

**Recommendation:**
- Implement FastAPI background task queue
- Add job status tracking endpoint
- Display queue position and ETA to user

---

### Limited Error Recovery

**Problem:** Failed uploads have no retry mechanism
- Exception thrown and operation abandoned
- User must re-upload entire batch

**Recommendation:**
- Implement exponential backoff retry
- Store last successful checkpoint
- Allow resume from failure point

---

## Test Coverage Gaps

### No Unit Tests for Core Services

**What's not tested:**
- `SupabaseService` token refresh logic (JWT expiry handling)
- `DataDiskUploadService` multi-table coordination
- `ExcelService` sheet mapping and column detection
- `VworldService` API integration

**Files:** No test projects found (`*.csproj` with "Test" suffix)

**Risk:** High
- Token refresh bugs go undetected
- Column mapping silently produces wrong data
- API changes break integration unexpectedly

**Priority:** High - Create `NPLogic.Tests` project with:
- Unit tests for SupabaseService token scenarios
- Integration tests for Excel parsing with real DataDisk samples
- Mock tests for external API calls (VWORLD, Python backend)

---

### No E2E Tests for Critical Workflows

**Untested Workflows:**
- Data Disk upload → property creation → OCR processing
- Login → session refresh → long operation (>1 hour)
- Property selection → tab navigation → data persistence

**Blocks:** Cannot safely refactor without regression

---

### No Python Backend Tests

**Files:** `python/` directory - no test files found

**Untested Components:**
- OCR pipeline (parse_registry_tables, build_refined_registry_tables)
- Recommendation similarity scoring
- Error handling edge cases

**Recommendation:**
- Add pytest configuration
- Create fixtures with sample PDFs and OCR results
- Test recommendation algorithm accuracy
- Add integration tests with real Python backend

---

## Scaling Limits

### Supabase Row Limits

**Current Capacity:** PostgreSQL default row size ~8KB
- Files: `src/NPLogic.Data/Repositories/PropertyRepository.cs` (no query limits)

**Limit Hit At:**
- ~100k properties in single query
- Complex joined queries with many properties
- Large JSON columns (evaluation details, rights analysis)

**Scaling Path:**
- Implement pagination throughout (currently missing in many ViewModels)
- Archive old programs to separate schema
- Implement materialized views for statistics queries

---

### WPF UI Rendering Performance

**Limit:** DataGrid with 1000+ rows becomes unresponsive
- Files: `src/NPLogic.App/Views/DashboardView.xaml.cs` (DataGrid binding to ObservableCollection)

**Scaling Path:**
- Virtualization already implemented (default in WPF)
- But full collection still loaded in memory
- Need server-side pagination (see Performance section)

---

### Python OCR Server Memory

**Issue:** Large PDF processing consumes significant memory
- Files: `python/server.py`, `python/ocr_processor.py`

**Limit:**
- 50+ page PDFs may exceed memory quota
- Concurrent requests can cause OOM

**Scaling Path:**
- Implement streaming PDF processing
- Add memory limits with graceful failure
- Consider splitting large PDFs before OCR

---

## Dependencies at Risk

### Supabase C# SDK Incomplete

**Risk:** Missing features force workarounds
- JWT verification gaps (Edge Function uses `verify_jwt: false`)
- DateTime.Kind inconsistencies in token expiry
- No transaction support

**Impact:**
- Security validation must be implemented manually
- Token handling is fragile and error-prone
- Multi-table operations lack atomicity

**Migration Plan:**
- Monitor supabase-csharp releases for fixes
- When available, upgrade and remove workarounds
- Consider alternative: Npgsql direct + PostgREST client

---

### Python CLOVA OCR Service Dependency

**Risk:** External service outage blocks OCR functionality
- Files: `python/clova_ocr.py` (Naver CLOVA API)

**Impact:**
- No fallback OCR provider
- All registry PDFs unprocessable during outage

**Mitigation:**
- Add fallback to alternative OCR (AWS Textract, Azure Form Recognizer)
- Implement circuit breaker pattern
- Cache OCR results for retry after recovery

---

### FastAPI/Uvicorn Version Pinning

**Risk:** Unspecified versions in `python/requirements.txt`

**Recommendation:**
- Pin all Python dependencies to specific versions
- Regular security updates with testing
- Add pre-commit hook to validate dependency versions

---

## Converter Stub Methods

**Files:** `src/NPLogic.App/Converters/VisibilityConverters.cs` lines 71-376 and others

**Problem:** ConvertBack methods throw NotImplementedException

**Examples:**
- `NullToVisibilityConverter.ConvertBack()` - line 95
- Multiple other converters with unimplemented reverse binding

**Impact:**
- TwoWay bindings fail silently or throw at runtime
- Not obvious until converter is used in reverse

**Recommendation:**
- Implement all ConvertBack methods or mark converters OneWay
- Add validation in code review checklist

---

## Architecture Concerns

### Multiple Entry Points for Same Operations

**Issue:** Similar operations scattered across different ViewModels
- Property number parsing: 3 separate implementations (RegistryTabViewModel, DashboardViewModel, EvaluationTabViewModel)
- Tab cache management: 2 different approaches (DashboardView, NonCoreView)

**Impact:**
- Bugs fixed in one place not propagated to others
- Inconsistent behavior across UI

**Recommendation:**
- Create shared utility layer (NPLogic.Core.Utilities)
- Centralize all property number parsing
- Establish standard tab management patterns

---

### Hard-Coded Configuration Values

**Issue:** Service URLs, timeouts, thresholds scattered in code
- Files: Multiple services and repositories

**Examples:**
- 50-minute token refresh timer (SupabaseService line 143)
- 10-minute HTTP timeout (RegistryRepository line 24)
- 10-minute threshold for pre-expiry refresh (SupabaseService line 259)

**Recommendation:**
- Move to configuration file or environment variables
- Create IServiceConfiguration interface
- Make timeouts testable via dependency injection

---

## Summary by Priority

**Critical (Fix Soon):**
- JWT DateTime.Kind mismatch handling (token expiry bugs)
- CORS wildcard configuration (security)
- Session storage unencrypted (security)
- No transaction support for multi-table ops (data integrity)
- No unit tests (quality)

**High (Next Sprint):**
- Large ViewModel refactoring (>3.5k LOC)
- Tab caching fragility (reliability)
- Property number pattern centralization (consistency)
- Python backend exception handling (debuggability)

**Medium (Roadmap):**
- Server-side pagination (performance/scaling)
- Batch OCR job queue (usability)
- Offline support (depends on requirements)
- E2E test coverage (quality)

**Low (Nice to Have):**
- Converter stub implementations (completeness)
- Hard-coded configuration extraction (flexibility)

---

*Concerns audit: 2026-03-14*
