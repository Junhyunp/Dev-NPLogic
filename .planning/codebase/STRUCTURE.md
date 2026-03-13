# Codebase Structure

**Analysis Date:** 2026-03-14

## Directory Layout

```
Dev-NPLogic/
├── src/                           # Main application source
│   ├── NPLogic.App/              # WPF desktop application (main project)
│   │   ├── Views/                # XAML UI files (.xaml + .xaml.cs)
│   │   │   ├── DashboardView.xaml(.cs)
│   │   │   ├── PropertyDetailView.xaml(.cs)
│   │   │   ├── Controls/         # Custom reusable controls
│   │   │   ├── Evaluation/       # Property-type-specific evaluation views
│   │   │   └── Loan/             # Loan-related views and sections
│   │   ├── ViewModels/           # MVVM presentation logic
│   │   │   ├── DashboardViewModel.cs (70KB)
│   │   │   ├── PropertyDetailViewModel.cs (167KB)
│   │   │   ├── EvaluationTabViewModel.cs (103KB)
│   │   │   ├── ProgramManagementViewModel.cs (165KB)
│   │   │   ├── Evaluation/       # Property-type-specific ViewModels
│   │   │   └── [30+ other ViewModels]
│   │   ├── Services/             # App-layer services
│   │   │   ├── PythonBackendService.cs (Singleton: server management)
│   │   │   ├── RegistryOcrService.cs (PDF OCR wrapper)
│   │   │   ├── MapService.cs (VWORLD API)
│   │   │   ├── StaticMapService.cs (tile rendering)
│   │   │   ├── VworldService.cs (공시가격 lookup)
│   │   │   ├── ExcelService.cs (EPPlus integration)
│   │   │   ├── PermissionService.cs (role-based access)
│   │   │   ├── RecommendService.cs (Python ML integration)
│   │   │   ├── StorageService.cs (file I/O)
│   │   │   ├── TradeService.cs (실거래가 data)
│   │   │   ├── DataDiskUploadService.cs (batch upload)
│   │   │   ├── InterimUploadService.cs (중도금 upload)
│   │   │   ├── RecoveryStrategyService.cs (회수전략)
│   │   │   ├── NavigationStateService.cs (UI navigation state)
│   │   │   └── UndoService.cs (LoanSheet undo/redo)
│   │   ├── Converters/           # WPF value converters (string → visibility, etc.)
│   │   ├── Assets/               # Images, maps, resources
│   │   └── App.xaml.cs           # DI configuration, startup logic
│   │
│   ├── NPLogic.Core/             # Business logic & domain model (zero dependencies)
│   │   ├── Models/               # 40+ domain entities
│   │   │   ├── Property.cs       # Main collateral entity
│   │   │   ├── Loan.cs           # Loan/financing info
│   │   │   ├── Borrower.cs       # Debtor info
│   │   │   ├── Evaluation.cs     # Property appraisal
│   │   │   ├── AuctionSchedule.cs
│   │   │   ├── Registry*.cs      # Registry (등기부등본) data
│   │   │   ├── RightAnalysis.cs  # Senior rights analysis
│   │   │   ├── Interim*.cs       # Interim advance/collection
│   │   │   ├── CashFlow.cs       # Cash flow analysis
│   │   │   ├── Program.cs        # Program/project info
│   │   │   └── [30+ other models]
│   │   └── Services/             # Domain business logic
│   │       ├── RightAnalysisRuleEngine.cs (규칙 기반 권리분석)
│   │       └── XnpvCalculator.cs (순현재가 계산)
│   │
│   ├── NPLogic.Data/             # Data access layer (Supabase integration)
│   │   ├── Repositories/         # Data access abstraction (20+ repositories)
│   │   │   ├── PropertyRepository.cs
│   │   │   ├── LoanRepository.cs
│   │   │   ├── BorrowerRepository.cs
│   │   │   ├── RegistryRepository.cs
│   │   │   ├── EvaluationRepository.cs
│   │   │   ├── RightAnalysisRepository.cs
│   │   │   ├── ProgramRepository.cs
│   │   │   ├── AuctionScheduleRepository.cs
│   │   │   └── [12+ other repositories]
│   │   ├── Services/
│   │   │   ├── SupabaseService.cs (Singleton: DB client, JWT refresh, session recovery)
│   │   │   ├── AuthService.cs (Singleton: user authentication, auto sign-in)
│   │   │   ├── SessionStorageService.cs (Windows Registry for persistent sessions)
│   │   │   └── CommercialDistrictService.cs (상권지표 queries)
│   │   └── Exceptions/
│   │       └── SessionExpiredException.cs (JWT expiry handler)
│   │
│   └── NPLogic.UI/               # Reusable UI components (library)
│       ├── Controls/             # Custom WPF controls
│       ├── Converters/           # Value converters
│       ├── Styles/               # XAML resource dictionaries
│       ├── Themes/               # Color/style themes
│       └── Services/             # UI utilities
│
├── python/                        # Python backend for OCR & recommendations
│   ├── recommend/                # ML-based property recommendations
│   └── server.py                 # FastAPI server (port 8000)
│
├── Controls/                      # Legacy WPF custom controls (root-level)
├── Styles/                        # Legacy XAML resources (root-level)
├── reference/                     # Reference documents & test data
│   ├── 등기부등본/               # Sample registry documents
│   ├── 데이터디스크/             # Data disk validation samples
│   └── 검증/                     # Test cases with expected outputs
│
├── NPLogic.sln                    # Visual Studio solution
├── CLAUDE.md                      # Project context for Claude
└── .planning/codebase/            # GSD planning documents (this folder)
```

## Directory Purposes

**src/NPLogic.App/Views:**
- Purpose: All XAML user interface definitions and code-behind
- Contains: .xaml markup files + .xaml.cs code-behind files
- Key files: `DashboardView.xaml` (main 2-panel layout), `PropertyDetailView.xaml` (multi-tab property editor), `DataUploadView.xaml` (batch import)
- Subdirectories:
  - `Controls/`: Reusable control definitions (CaseMapControl, BidStatisticsControl, etc.)
  - `Evaluation/`: Property-type-specific evaluation forms (Apartment, Commercial, Factory, HouseLand, MultiFamily)
  - `Loan/`: Loan detail and sheet editing (LoanSheetView with sections)

**src/NPLogic.App/ViewModels:**
- Purpose: Presentation logic, data binding, command handling
- Contains: ObservableObject-derived classes with `[ObservableProperty]` and `[RelayCommand]` attributes
- Key files: `DashboardViewModel.cs` (program/property drilldown), `PropertyDetailViewModel.cs` (tab navigation), `EvaluationTabViewModel.cs` (type-agnostic evaluation logic)
- Subdirectories:
  - `Evaluation/`: Type-specific evaluation logic (EvaluationBaseViewModel for common methods, subclasses for Apartment/Commercial/Factory/HouseLand)

**src/NPLogic.App/Services:**
- Purpose: Application services (not domain-specific) - OCR, mapping, Excel, permissions
- Singleton instances: PythonBackendService, SupabaseService, AuthService, ExcelService, MapService
- File count: 19 service classes
- Key distinctions:
  - Stateless utilities: ExcelService, StorageService, StaticMapService (create new instance OK)
  - Stateful singletons: PythonBackendService (maintains server process), SupabaseService (maintains JWT)
  - Cross-cutting: PermissionService (role checks on all views)

**src/NPLogic.Core/Models:**
- Purpose: Domain entities representing business concepts
- Contains: POCO classes with properties + computed displayProperties
- Pure model layer: no database code, no service dependencies
- Database mapping: Postgrest attributes map to table names (implicit naming convention)
- Key entity categories:
  - Core: Property, Loan, Borrower (core loan/collateral domain)
  - Evaluation: Evaluation, EvaluationCase, EvaluationCommercialData, EvaluationLandParcel, EvaluationMachinery (appraisal)
  - Registry: RegistryRun, RegistryBasicInfo, RegistryGapguRow, RegistryEulguRow (OCR + registration)
  - Analysis: RightAnalysis, LeaseItem, WageClaimItem (senior rights)
  - Finance: CashFlow, Interim*, Loan (payment/recovery analysis)
  - Administration: Program, ProgramUser, User, AuditLog

**src/NPLogic.Core/Services:**
- Purpose: Domain/business logic services (no UI or database dependencies)
- File count: 2 critical services
  - `RightAnalysisRuleEngine.cs`: Applies rule-based logic to determine senior rights conflicts and recovery strategy recommendations
  - `XnpvCalculator.cs`: Calculates XNPV (net present value) for financial analysis

**src/NPLogic.Data/Repositories:**
- Purpose: Abstraction layer for database access
- Pattern: One repository per domain entity (PropertyRepository, LoanRepository, etc.)
- Methods: GetByIdAsync, GetAllAsync, GetByProjectIdAsync, InsertAsync, UpdateAsync, DeleteAsync (async everywhere)
- Database client: All use `_supabaseService.GetClientAsync()` for Postgrest queries
- File count: 20+ repositories

**src/NPLogic.Data/Services:**
- Purpose: Data-layer services (authentication, Supabase client management)
- Key files:
  - `SupabaseService.cs`: Singleton managing Supabase client, JWT tokens, refresh timers, system event handlers (sleep/unlock detection)
  - `AuthService.cs`: Singleton for user login/logout, auto sign-in from registry, current user tracking
  - `SessionStorageService.cs`: Windows Registry wrapper for persistent session storage
  - `CommercialDistrictService.cs`: Queries commercial property indicators

**src/NPLogic.UI:**
- Purpose: Reusable, non-domain-specific UI components
- Contents: Custom WPF controls (not tied to property domain)
- Examples: Generic data grids, input controls, dialogs
- Styling: XAML resource dictionaries in Themes/ and Styles/

**python/ (Backend):**
- Purpose: OCR processing and ML-based recommendations
- Technology: FastAPI (Python) on port 8000
- Deployment: EC2 server (3.34.10.57) for production; falls back to localhost:8000 for development
- Called by: RegistryOcrService and RecommendService from C# app
- Endpoints: `/api/health`, `/api/ocr/registry`, `/api/recommend/*`

## Key File Locations

**Entry Points:**
- `src/NPLogic.App/App.xaml.cs`: Application startup, DI configuration, initial window selection
- `src/NPLogic.App/Views/MainWindow.xaml`: Root window (navigation root)
- `src/NPLogic.App/Views/DashboardView.xaml`: Primary working UI (property list + tabbed editor)

**Configuration:**
- `src/NPLogic.App/App.xaml.cs`: Hardcoded Supabase URL + key (lines 24-25) — should move to config
- `src/NPLogic.App/Services/PythonBackendService.cs`: Remote/local server URLs (lines 27-29)
- `CLAUDE.md`: Project context and architecture notes

**Core Logic:**
- `src/NPLogic.Core/Services/RightAnalysisRuleEngine.cs`: Senior rights conflict rules
- `src/NPLogic.Core/Services/XnpvCalculator.cs`: NPV calculations
- `src/NPLogic.App/Services/EvaluationService.cs`: Appraisal computation

**Testing:**
- No test projects present in solution
- Test data: `reference/검증/` contains sample datasets for manual validation

**Database:**
- Supabase project: nlddampvgxamaukflqhd
- DB URL: https://nlddampvgxamaukflqhd.supabase.co
- Schema defined server-side (not in repo)

## Naming Conventions

**Files:**
- `[Feature]View.xaml`, `[Feature]View.xaml.cs` - UI views
- `[Feature]ViewModel.cs` - Presentation logic
- `[Entity]Repository.cs` - Data access
- `[Service]Service.cs` - Cross-cutting services
- `[Entity].cs` - Domain models

**Directories:**
- Feature-based: `Views/`, `ViewModels/` (not by entity)
- Logical grouping: `Views/Evaluation/`, `Views/Loan/Sections/`
- Layer-based: `Repositories/`, `Services/`

**Classes:**
- PascalCase throughout (C# convention)
- ViewModel suffix for all presentation classes
- Repository suffix for all data access classes
- Service suffix for utility classes

**Properties:**
- PascalCase for public properties
- camelCase for private fields (prefixed with `_`)
- ObservableProperty attributes (auto-generate INPC code)

**Methods:**
- AsyncSuffix: All async methods end with `Async()`
- Commands: `[Command]` relay commands auto-generate ICommand implementations

## Where to Add New Code

**New Property-Related Feature:**
- ViewModel: `src/NPLogic.App/ViewModels/[FeatureName]ViewModel.cs`
- View: `src/NPLogic.App/Views/[FeatureName]View.xaml` + `.xaml.cs`
- Repository (if DB access): `src/NPLogic.Data/Repositories/[Entity]Repository.cs`
- Model (if new domain entity): `src/NPLogic.Core/Models/[Entity].cs`
- Tests: Not currently present; would go in new `src/NPLogic.Tests/` project

**New Tab in Property Detail:**
- Add `.xaml` file to `src/NPLogic.App/Views/`
- Add `.xaml.cs` code-behind and create/register ViewModel
- Register in `App.xaml.cs` in `ConfigureServices()`
- Add to tab list in DashboardView/PropertyDetailView XAML
- Optional: Cache in DashboardView if expensive to create

**New Evaluation Type:**
- View: `src/NPLogic.App/Views/Evaluation/Evaluation[Type]View.xaml`
- ViewModel: `src/NPLogic.App/ViewModels/Evaluation/Evaluation[Type]ViewModel.cs`
- Base class: Inherit from `EvaluationBaseViewModel.cs`
- Model: Add type-specific model to `src/NPLogic.Core/Models/` (e.g., EvaluationCommercialData.cs)
- Repository: Add `Evaluation[Type]Repository.cs` to data layer

**New Service Class:**
- Stateless utilities: `src/NPLogic.App/Services/[Purpose]Service.cs`
- Register in `App.xaml.cs` ConfigureServices() with appropriate scope (Singleton for stateful, Transient for stateless)
- Inject via constructor into ViewModels that need it

**New Business Rule:**
- Add to `src/NPLogic.Core/Services/RightAnalysisRuleEngine.cs` or create new domain service
- No database or UI dependencies allowed in domain services
- Test via repository/ViewModel integration (manual tests only, no test project)

**Shared/Reusable Control:**
- Generic UI components: `src/NPLogic.UI/Controls/[ControlName].xaml`
- Domain-specific controls: `src/NPLogic.App/Views/Controls/[ControlName].xaml`

**Excel/File Import:**
- Import logic: Implement in ViewModel or Service
- Use: `ExcelService.ReadExcelFileAsync()` for reading
- Transform: Map Excel columns to domain models
- Persist: Call repository Insert/Update methods

## Special Directories

**src/NPLogic.App/Converters:**
- Purpose: WPF value converters for binding transformations
- Generated: No (hand-written)
- Committed: Yes
- Examples: Decimal to currency string, Enum to visibility, Status to color

**src/NPLogic.App/Resources:**
- Purpose: XAML resource definitions (styles, templates, colors)
- Generated: No
- Committed: Yes
- Usage: Referenced in XAML via `{StaticResource ...}` bindings

**src/NPLogic.App/Assets:**
- Purpose: Images, icons, map tile caches
- Generated: No (except map tiles downloaded at runtime)
- Committed: Yes (images), No (map tiles)

**reference/**
- Purpose: Reference documents, test data, expected outputs
- Generated: No
- Committed: Yes
- Usage: Manual validation and developer documentation

**python/**
- Purpose: Backend server source code
- Run: `docker compose up -d --build` (see CLAUDE.md)
- Deployment: EC2 instance (3.34.10.57:8000)

---

*Structure analysis: 2026-03-14*
