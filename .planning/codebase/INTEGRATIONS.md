# External Integrations

**Analysis Date:** 2026-03-14

## APIs & External Services

**Supabase Backend:**
- Primary database provider (PostgreSQL + PostgREST)
  - ProjectID: `nlddampvgxamaukflqhd`
  - URL: `https://nlddampvgxamaukflqhd.supabase.co`
  - SDK/Client: `supabase-csharp 0.16.2`, `supabase 2.3.4` (Python)
  - Auth: Anon key hardcoded in `src/NPLogic.App/App.xaml.cs` and `src/NPLogic.Data/Services/SupabaseService.cs`
  - Uses: All data persistence, user authentication, RPC functions

**Vworld (Vector World) - Land/Property Data:**
- Public address-to-PNU mapping and property information
  - Service: `src/NPLogic.App/Services/VworldService.cs`
  - API Endpoint: `https://api.vworld.kr/req/search`
  - Auth: API key retrieved from Supabase Edge Function (MapService)
  - Uses: Convert addresses to PNU (property identification numbers), retrieve property registration data

**Kakao Map API:**
- Map visualization and geolocation
  - Configuration: `src/NPLogic.App/appsettings.json` (template)
  - API Key: Placeholder `YOUR_KAKAO_MAP_API_KEY` - requires environment configuration
  - Developers: https://developers.kakao.com/
  - Uses: Interactive maps display in UI

**Data.go.kr (Government Real Estate API):**
- Ministry of Land, Infrastructure and Transport real estate transaction data
  - Configuration: `src/NPLogic.App/appsettings.json` (template, RealEstateAPI.DataGoKrKey)
  - API: https://www.data.go.kr/data/15057511/openapi.do
  - Uses: Historical transaction prices for property valuation

**Naver Clova OCR API:**
- Optical Character Recognition for registry documents
  - Service: `python/clova_ocr.py`
  - Credentials:
    - `CLOVA_SECRET_KEY`: Naver Cloud Platform secret key
    - `CLOVA_INVOKE_URL`: OCR endpoint URL
  - Uses: Extract text from property registry documents (등기부등본) PDF files
  - Configuration: Environment variables (`python/.env.example`)

**External Trade Data (Supabase RPC):**
- Secondary Supabase project for real estate transaction data
  - Service: `src/NPLogic.App/Services/TradeService.cs`
  - URL: `https://gkchpqzbpnmhzcjhsodf.supabase.co`
  - Auth: Service account credentials (hardcoded - security risk)
    - Email: `wpf-service@nplogic-map.com`
    - RPC Function: `get_trades()`
  - Uses: Retrieve similar property transactions for recommendation system

**NPLogic Map Server:**
- Internal/custom map tile and property boundary server
  - Configuration: `src/NPLogic.App/appsettings.json`
  - MapServer.Url: `https://nplogic-map.com/`
  - Uses: Cadastral maps (지적도) and property location maps

## Data Storage

**Primary Database:**
- Supabase (PostgreSQL)
  - ProjectID: `nlddampvgxamaukflqhd`
  - Connection: Via `supabase-csharp 0.16.2`
  - Tables: borrowers, loans, properties, registry_sheet_data, basic_info, gapgu_rows, eulgu_rows, credit_guarantees, borrower_restructuring, etc.
  - Client: PostgREST via `postgrest-csharp 3.5.1`
  - Auth: JWT tokens (ES256, auto-rotated, refresh handling in `src/NPLogic.Data/Services/SupabaseService.cs`)

**File Storage:**
- Local filesystem only
  - Temporary directories for PDF processing (`tempfile` in Python backend)
  - Application output: `src/NPLogic.App/bin/` and `src/NPLogic.App/obj/`
  - Log files: Configured via Serilog to file sink (configurable destination)

**Caching:**
- In-memory ViewModel caches
  - Tab view cache: `_tabViewCache`, `_tabViewModelCache` in DashboardViewModel
  - No distributed cache (application-scoped only)

## Authentication & Identity

**Auth Provider:**
- Supabase Gotrue (built-in authentication)
  - Implementation: `src/NPLogic.Data/Services/AuthService.cs`
  - Methods: Email/password signup and login
  - JWT Token Flow: 1-hour expiration, auto-refresh every 50 minutes
  - Session Storage: Windows Registry protected storage (`System.Security.Cryptography.ProtectedData`)
  - Session Recovery: Auto-refresh on system wake/unlock via `SystemEvents.PowerModeChanged`, `SystemEvents.SessionSwitch`

**Trade Service Auth:**
- Secondary Supabase service account authentication
  - Service: `src/NPLogic.App/Services/TradeService.cs`
  - Method: Service account email/password login
  - Token Management: In-memory with 5-minute refresh buffer
  - Credentials: Hardcoded (security concern - should use environment variables)

## Monitoring & Observability

**Error Tracking:**
- None detected - no integration with Sentry, Application Insights, etc.

**Logs:**
- Serilog structured logging
  - Sink: File output via `Serilog.Sinks.File 5.0.0`
  - Configuration: `src/NPLogic.App/appsettings.json` (LogLevel.Default: Information, LogLevel.Microsoft: Warning)
  - Python backend: FastAPI built-in logging to console (captured by docker-compose)

**Python Backend Health:**
- Health check endpoint: `GET /api/health` returns `{"status": "ok", "message": "NPLogic Backend is running"}`
- Docker healthcheck: Configured in `python/docker-compose.yml` (30s interval, 10s timeout, 3 retries)

## CI/CD & Deployment

**Hosting:**
- Primary: Desktop application deployed as self-contained Windows executable (win-x64)
- Secondary: Python backend deployed on EC2 instance (`http://3.34.10.57:8000`) via Docker
- Fallback: Local Python backend started on-demand from bundled executable

**Build Process:**
- C# Application Build:
  ```bash
  dotnet build NPLogic.sln
  dotnet run --project src/NPLogic.App/NPLogic.App.csproj
  ```
- Published Deployment:
  ```bash
  dotnet publish -c Release --self-contained --runtime win-x64
  ```
- Python Backend Build: Docker image build in `python/` directory with `Dockerfile`

**CI Pipeline:**
- Not detected - no GitHub Actions, Azure Pipelines, or similar CI configuration

## Environment Configuration

**Required env vars (C# Application):**
- `SUPABASE_URL`: Hardcoded in `App.xaml.cs` (production: `https://nlddampvgxamaukflqhd.supabase.co`)
- `SUPABASE_KEY`: Hardcoded in `App.xaml.cs` (anon key)
- `KAKAO_MAP_API_KEY`: Template in `appsettings.json`, not required for core functionality
- `DATA_GO_KR_API_KEY`: Template in `appsettings.json`, not required for core functionality

**Required env vars (Python Backend):**
- `SUPABASE_URL`: From docker-compose or `.env` - for recommendation system database queries
- `SUPABASE_KEY`: From docker-compose or `.env`
- `CLOVA_SECRET_KEY`: Naver Cloud Platform secret for OCR API
- `CLOVA_INVOKE_URL`: Naver Clova OCR invoke endpoint
- `PORT`: Server port (default: 8000)

**Secrets location:**
- C# Application:
  - Hardcoded in `src/NPLogic.App/App.xaml.cs` (production keys)
  - Configuration template: `src/NPLogic.App/appsettings.json.template`
  - Session tokens: Windows Registry with DPAPI encryption

- Python Backend:
  - `.env` file (not committed, use `.env.example` as template)
  - Docker: Pass via `docker-compose.yml` environment section

**Security Issues:**
- Supabase anon key hardcoded in application code (should use environment variables)
- Trade service credentials hardcoded in `TradeService.cs` (hardcoded email: `wpf-service@nplogic-map.com`, password)
- API keys in `appsettings.json` template not protected

## Webhooks & Callbacks

**Incoming:**
- None detected - application does not receive external webhooks

**Outgoing:**
- Python backend sends results via HTTP responses to calling WPF application
- No outbound webhooks to external systems detected

## Data Flow Diagram

```
WPF Application (src/NPLogic.App)
├─ SupabaseService (src/NPLogic.Data/Services)
│  └─ Supabase ProjectID: nlddampvgxamaukflqhd
│     ├─ PostgreSQL Database
│     └─ PostgREST API
├─ Python Backend (python/)
│  ├─ Clova OCR API (registry document processing)
│  │  └─ PDF → OCR → Registry data
│  ├─ Supabase (recommendation system queries)
│  │  └─ SELECT similar properties
│  └─ Runs on: EC2 (3.34.10.57:8000) or local fallback
├─ VworldService
│  └─ Vworld API (address → PNU mapping)
├─ TradeService
│  └─ External Supabase (gkchpqzbpnmhzcjhsodf) RPC get_trades()
└─ MapServices
   └─ Kakao Map API
   └─ NPLogic Map Server (https://nplogic-map.com/)
```

---

*Integration audit: 2026-03-14*
