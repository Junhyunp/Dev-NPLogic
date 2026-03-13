# Technology Stack

**Analysis Date:** 2026-03-14

## Languages

**Primary:**
- C# 10+ - .NET 10 WPF desktop application (`src/NPLogic.App/`, `src/NPLogic.Data/`, `src/NPLogic.Core/`, `src/NPLogic.UI/`)

**Secondary:**
- Python 3.11 - FastAPI backend server for OCR and recommendation processing (`python/`)

## Runtime

**Environment:**
- .NET 10 (LTS)
  - Desktop: Windows-only (.NET 10-windows)
  - Server: .NET 10 cross-platform (`python/` FastAPI server uses Python 3.11)

**Package Managers:**
- NuGet (C# dependencies) - via `*.csproj` files
- pip (Python dependencies) - `python/requirements.txt`

**Deployment:**
- Windows self-contained deployment (win-x64):
  - `RuntimeIdentifier`: win-x64
  - `SelfContained`: true
  - `PublishReadyToRun`: true
  - Bundles .NET 10 runtime with application

## Frameworks

**Core Application:**
- WPF (Windows Presentation Foundation) - `net10.0-windows` UI framework
- MVVM (Model-View-ViewModel) pattern via CommunityToolkit.Mvvm 8.2.2

**UI Components:**
- MaterialDesignThemes 4.9.0 - Material Design theming
- LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc2 - Data visualization and charting
- WebView2 1.0.3650.58 - Embedded Chromium browser control for HTML content

**Backend Framework:**
- FastAPI 0.115.6 - Python REST API framework
- Uvicorn 0.34.0 - ASGI server

**Data Access:**
- supabase-csharp 0.16.2 - Supabase client for C#
- postgrest-csharp 3.5.1 - PostgREST REST client for C#
- supabase (Python) 2.3.4 - Supabase client for Python

**Business Logic:**
- EPPlus 7.0.5 - Excel reading/writing (.xlsx)
- ClosedXML 0.102.3 - XML/Excel manipulation (legacy)

## Key Dependencies

**Critical - Infrastructure:**
- supabase-csharp 0.16.2 - Database and authentication backend (ProjectID: `nlddampvgxamaukflqhd`)
- postgrest-csharp 3.5.1 - PostgREST API client for direct database queries

**Critical - UI/UX:**
- MaterialDesignThemes 4.9.0 - Application design language
- LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc2 - Charts and graphs on dashboard

**Application Framework:**
- CommunityToolkit.Mvvm 8.2.2 - MVVM pattern implementation
- Microsoft.Extensions.DependencyInjection 8.0.0 - IoC container
- Microsoft.Extensions.Configuration 8.0.0, 8.0.0 - Configuration management

**Logging & Debugging:**
- Serilog 3.1.1 - Structured logging
- Serilog.Sinks.File 5.0.0 - File-based log sink

**Python Backend - OCR:**
- pytesseract 0.3.10 - Tesseract OCR engine wrapper
- pdf2image 1.16.3 - PDF to image conversion
- Pillow 10.1.0 - Image processing
- opencv-python-headless 4.8.1.78 - Computer vision (image preprocessing)
- PyPDF2 3.0.1 - PDF manipulation

**Python Backend - Server:**
- fastapi 0.115.6 - Web framework
- uvicorn[standard] 0.34.0 - ASGI server
- python-multipart 0.0.19 - File upload handling

**Python Backend - Data Processing:**
- pandas 2.1.4 - Data structures and analysis
- numpy 1.26.2 - Numerical computing
- PyYAML 6.0.1 - YAML configuration parsing

**Cross-Platform Dependencies:**
- System.Security.Cryptography.ProtectedData 10.0.0 - Secure credential storage (Windows-specific)
- requests 2.31.0 - HTTP requests for Python

## Configuration

**Environment (C# Application):**
- `src/NPLogic.App/appsettings.json` - Template for application settings
  - Supabase connection details (URL, API Key)
  - External API endpoints (Kakao Maps, Data.go.kr)
  - Map server configuration
  - Logging levels

**Build Configuration:**
- Self-contained Windows deployment with precompiled assemblies (`PublishReadyToRun`: true)
- Resource embedding: Assets (PNG, JPG, JPEG, ICO, SVG) in `src/NPLogic.App/Assets/`
- Content files copied to output: `appsettings.json`, `mapping_template.json`, Map HTML files

**Environment (Python Backend):**
- `python/.env.example` - Template for environment variables
  - `SUPABASE_URL`, `SUPABASE_KEY` - Supabase credentials for recommendation system
  - `CLOVA_SECRET_KEY`, `CLOVA_INVOKE_URL` - Naver Clova OCR API credentials
- `python/docker-compose.yml` - Container orchestration with environment variable support

## Platform Requirements

**Development:**
- OS: Windows (WPF is Windows-only)
- .NET 10 SDK or Runtime
- Visual Studio 2022 17.12+ or VS Code
- WebView2 Runtime (edge browser component)
- Python 3.10+ (for `python/` backend development)
- Tesseract OCR system package (for local Python development)
- Poppler system package (for PDF processing in Python)

**Production:**
- Deployment Target: Windows 64-bit (win-x64)
- Bundled: .NET 10 runtime (self-contained deployment)
- Optional: EC2 remote Python backend server at `http://3.34.10.57:8000` (for OCR/recommendation features)
- Fallback: Local Python backend execution if remote unavailable

**Container (Python Backend):**
- Base: `python:3.11-slim`
- System dependencies: Poppler, Tesseract OCR, Tesseract Korean language pack, libgl1, libglib2.0-0
- Port exposure: 8000
- Healthcheck: `/api/health` endpoint with 30s interval

---

*Stack analysis: 2026-03-14*
