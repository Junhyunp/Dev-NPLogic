# Architecture

**Analysis Date:** 2026-03-14

## Pattern Overview

**Overall:** Layered MVVM Architecture with Domain-Driven Design

**Key Characteristics:**
- WPF desktop application with strict separation of concerns (MVVM pattern)
- Four-layer structure: UI (Views) → Presentation (ViewModels) → Domain (Core) → Data (Repositories)
- Dependency Injection via Microsoft.Extensions.DependencyInjection container
- Supabase PostgreSQL backend with REST API via PostgREST
- Supporting Python microservice for OCR and ML-based recommendations
- Entity-based domain model with rich business logic
- Observable objects using CommunityToolkit.MVVM

## Layers

**Presentation (Views & ViewModels):**
- Purpose: UI rendering, user interaction handling, view state management
- Location: `src/NPLogic.App/Views/`, `src/NPLogic.App/ViewModels/`
- Contains: XAML views, code-behind, ObservableObject-based ViewModels
- Depends on: Core domain models, Data repositories, App services
- Used by: XAML bindings, user input events
- Key ViewModels: `DashboardViewModel`, `PropertyDetailViewModel`, `EvaluationTabViewModel` (103KB), `ProgramManagementViewModel` (165KB), `LoanSheetViewModel`

**Domain (Core Models & Business Logic):**
- Purpose: Business rules, entity definitions, domain calculations
- Location: `src/NPLogic.Core/Models/`, `src/NPLogic.Core/Services/`
- Contains: Domain entities (Property, Loan, Borrower, Evaluation, etc.), domain services
- Depends on: Nothing (zero external dependencies except JSON serialization)
- Used by: ViewModels, Repositories
- Key services: `RightAnalysisRuleEngine` (권리분석 규칙 엔진), `XnpvCalculator` (순현재가 계산)
- Models: 40+ entity classes representing financial/property domain concepts

**Data Access (Repositories & Services):**
- Purpose: Persistence, external API communication, data transformation
- Location: `src/NPLogic.Data/Repositories/`, `src/NPLogic.Data/Services/`
- Contains: Repository pattern implementations, Supabase client wrapper, authentication
- Depends on: Core models, Supabase client library, HTTP clients
- Used by: ViewModels via constructor injection
- Key repositories: PropertyRepository, LoanRepository, BorrowerRepository, RegistryRepository, EvaluationRepository, ProgramRepository (20+ total)
- Key services: `SupabaseService` (DB connection, JWT refresh, session recovery), `AuthService` (user authentication with auto-sign-in), `CommercialDistrictService` (commercial property queries)

**UI Components & Utilities:**
- Purpose: Reusable UI controls, styling, formatting, mapping utilities
- Location: `src/NPLogic.UI/Controls/`, `src/NPLogic.UI/Converters/`, `src/NPLogic.App/Converters/`, `src/NPLogic.App/Services/`
- Contains: Custom controls, value converters, WPF resource dictionaries
- Key service classes: `MapService` (VWORLD API, map tile rendering), `StaticMapService` (static map image generation), `VworldService` (공시가격/PNU lookup), `ExcelService` (Excel I/O via EPPlus), `StorageService` (file operations)

## Data Flow

**Property Workflow (典型的なフロー):**

1. User selects/creates property in DashboardView
2. DashboardViewModel loads property details via PropertyRepository.GetByIdAsync()
3. PropertyDetailViewModel receives property entity and initializes sub-tabs
4. Each tab (Registry, RightsAnalysis, Evaluation, etc.) loads related data:
   - RegistryTabViewModel → RegistryRepository → registry_runs + OCR data
   - EvaluationTabViewModel → EvaluationRepository + type-specific repositories
   - RightsAnalysisTabViewModel → RightAnalysisRepository + RightAnalysisRuleEngine
5. User modifies data in UI
6. ViewModel calls repository update methods
7. Repository serializes to Supabase tables
8. Supabase RLS policies enforce row-level security

**OCR Pipeline (등기부등본 스캔):**

1. User selects PDF in RegistryTabViewModel.SelectPdfCommand
2. PDF sent to PythonBackendService.EnsureServerRunningAsync()
   - Tries remote EC2 server (3.34.10.57:8000) first
   - Falls back to local server if remote unavailable
3. RegistryOcrService.ProcessPdfAsync() → /api/ocr/registry endpoint
4. Python FastAPI server processes PDF with Tesseract OCR
5. Response includes: extracted text, structured data, summary image
6. RegistryTabViewModel updates registry_runs, basic_info, gapgu_rows tables
7. EvaluationTab refreshes with OCR-extracted data

**Server-Side Pagination (Dashboard물건목록):**

1. DashboardViewModel.LoadPropertyDataAsync() fires on init and page changes
2. PropertyRepository queries with limit/offset (PageSize=50)
3. DataGrid shows 50 items, additional items loaded on scroll-to-bottom
4. Cooldown timer prevents duplicate loads during rapid scrolling
5. Total count cached to determine if "more data" exists

**State Management:**

- Session state: `SupabaseService` maintains JWT + refresh tokens, auto-refreshes every 50min
- Navigation state: `NavigationStateService` stores selected program/property/tab
- View cache: DashboardView caches NonCoreView and RegistryTab to preserve internal state across tab switches
- Undo state: `UndoService` maintains operation stack for reversible edits (in LoanSheetViewModel)
- UI flags: Multiple `_suppress*` flags in DashboardView prevent event loops during programmatic changes

## Key Abstractions

**Repository Pattern:**
- Purpose: Abstract database access, provide testable data layer
- Examples: `src/NPLogic.Data/Repositories/PropertyRepository.cs`, `LoanRepository.cs`, `EvaluationRepository.cs`
- Pattern: Generic async methods (GetByIdAsync, GetAllAsync, InsertAsync, UpdateAsync, DeleteAsync)
- Database table names mapped via Postgrest attributes

**Domain Entities:**
- Purpose: Represent domain concepts with calculated properties
- Examples: `src/NPLogic.Core/Models/Property.cs`, `Loan.cs`, `Borrower.cs`, `Evaluation.cs`
- Pattern: Domain-driven, contain business logic (e.g., Property.DisplayAddress computed fallback)
- No database persistence logic - clean separation

**ViewModel Base (ObservableObject):**
- Purpose: Enable WPF data binding with property change notifications
- Pattern: `[ObservableProperty]` attributes auto-generate INotifyPropertyChanged code
- Commands: `[RelayCommand]` attributes auto-generate ICommand implementations
- Inherited by: All ViewModels in NPLogic.App

**Service Classes:**
- Purpose: Encapsulate cross-cutting concerns (auth, mapping, Excel, OCR, Python backend)
- Singleton scope: SupabaseService, AuthService, PythonBackendService, MapService, ExcelService
- Transient scope: services created per ViewModel (no shared state)

## Entry Points

**Application Startup:**
- Location: `src/NPLogic.App/App.xaml.cs`
- Triggers: WPF Application OnStartup event
- Responsibilities:
  - Configure DI container (ServiceCollection → ServiceProvider)
  - Initialize Supabase client
  - Attempt auto sign-in via AuthService.TryAutoSignInAsync()
  - Show LoginWindow or MainWindow based on auth state
  - Pre-start Python backend server in background

**Main UI Window:**
- Location: `src/NPLogic.App/Views/MainWindow.xaml`
- Root of visual tree
- Contains: NavigationView with program/property list, DashboardView content area

**Dashboard View:**
- Location: `src/NPLogic.App/Views/DashboardView.xaml.cs`
- Two-panel layout: left sidebar (property list) + right content (tabbed interface)
- Inner tabs: NonCore, Registry, RightsAnalysis, BasicData, QASummary, CashFlowSummary, NPVComparison, Closing
- Cached sub-views preserve state across tab switches

## Error Handling

**Strategy:** Hierarchical exception handling with user feedback

**Patterns:**

- **Global exception handler** in App.xaml.cs.DispatcherUnhandledException
  - Catches unhandled UI thread exceptions
  - Special case: SessionExpiredException → redirect to LoginWindow
  - User-facing message boxes for all exceptions

- **Repository exception wrapping:**
  ```csharp
  catch (Exception ex)
  {
      throw new Exception($"물건 목록 조회 실패: {ex.Message}", ex);
  }
  ```
  - Preserves inner exception for debugging
  - Adds business context to error message

- **Session recovery** in SupabaseService:
  - Detects system sleep/unlock via PowerModeChanged + SessionSwitch events
  - Forces token refresh on resume
  - SemaphoreSlim prevents concurrent refresh calls

- **Supabase-specific** (SessionExpiredException):
  - Thrown when JWT validation fails in Edge Functions
  - Caught at app level, triggers full logout + LoginWindow

## Cross-Cutting Concerns

**Logging:**
- Ad-hoc Debug.WriteLine() calls throughout code
- Future: Centralized logging via Serilog or similar
- Special: DebugLog helper in DashboardView.xaml.cs writes JSON to file for agent analysis

**Validation:**
- Input validation in ViewModels before repository calls
- Business rule validation in domain services (RightAnalysisRuleEngine, XnpvCalculator)
- Database constraints enforced at Supabase level (NOT NULL, UNIQUE, FK constraints)

**Authentication:**
- JWT-based via Supabase Auth
- Token refresh every 50 minutes (configurable in SupabaseService)
- Auto-sign-in attempts stored credentials from SessionStorageService (Windows Registry)
- Edge Function calls require: verify_jwt=false + manual getUser() validation (workaround for ES256 mismatch)

**Authorization:**
- Role-based access control via PermissionService
- Three roles: Admin, PM (Program Manager), Evaluator
- Views/commands filtered by current user role
- RLS policies on Supabase tables enforce server-side restrictions

**Caching:**
- View cache: DashboardView._cachedNonCoreView, _cachedRegistryTab
- Program name cache: DashboardViewModel._programNameCache
- Program user cache: DashboardViewModel._pmProgramIds
- No explicit cache invalidation strategy (data reloads on property change)

**Background Tasks:**
- Python backend auto-start on app launch (Fire-and-forget Task.Run)
- Server health checks with exponential backoff
- Singleton PythonBackendService manages process lifecycle

---

*Architecture analysis: 2026-03-14*
