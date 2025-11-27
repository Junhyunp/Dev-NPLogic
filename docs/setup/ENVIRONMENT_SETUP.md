# NPLogic 개발 환경 설정 가이드

## 📋 목차

1. [필수 소프트웨어](#필수-소프트웨어)
2. [.NET 개발 환경](#net-개발-환경)
3. [Python 환경](#python-환경)
4. [Supabase 설정](#supabase-설정)
5. [프로젝트 설정](#프로젝트-설정)
6. [빌드 및 실행](#빌드-및-실행)
7. [문제 해결](#문제-해결)

---

## 필수 소프트웨어

### ✅ 필수
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (최신 버전)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 또는 [VS Code](https://code.visualstudio.com/)
- [Python 3.10+](https://www.python.org/downloads/)
- [Git](https://git-scm.com/)

### 🔧 권장
- [Visual Studio Code Extensions](https://marketplace.visualstudio.com/)
  - C# Dev Kit
  - Python
  - GitLens
- [Windows Terminal](https://aka.ms/terminal) (Windows 사용자)

---

## .NET 개발 환경

### 1. .NET SDK 설치 확인

```bash
dotnet --version
```

예상 출력: `8.0.x` 이상

### 2. Visual Studio 2022 설치 (권장)

**필수 워크로드**:
- .NET 데스크톱 개발
- .NET Core 크로스 플랫폼 개발

**선택 구성요소**:
- Git for Windows
- GitHub Extension for Visual Studio

### 3. VS Code 설정 (대안)

**필수 확장**:
```bash
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp
```

---

## Python 환경

### 1. Python 설치

**Windows**:
```bash
# Chocolatey 사용 (권장)
choco install python

# 또는 공식 설치 파일 다운로드
# https://www.python.org/downloads/
```

**버전 확인**:
```bash
python --version
```

예상 출력: `Python 3.10.x` 이상

### 2. Python 패키지 설치

프로젝트 루트에서:

```bash
cd python
pip install -r requirements.txt
```

**필수 패키지**:
- `pytesseract` - OCR 엔진
- `Pillow` - 이미지 처리
- `pdf2image` - PDF 변환
- `pandas` - 데이터 처리
- `PyPDF2` - PDF 읽기

### 3. Tesseract OCR 설치 (pytesseract 의존성)

**Windows**:
```bash
choco install tesseract
```

또는 [Tesseract 설치 파일 다운로드](https://github.com/UB-Mannheim/tesseract/wiki)

**설치 경로 확인** (보통):
```
C:\Program Files\Tesseract-OCR\tesseract.exe
```

**환경 변수 설정**:
시스템 PATH에 Tesseract 경로 추가

---

## Supabase 설정

### 1. Supabase 프로젝트 생성

1. [Supabase 웹사이트](https://supabase.com/)에서 계정 생성
2. "New Project" 클릭
3. 프로젝트 정보 입력:
   - **Name**: NPLogic
   - **Database Password**: 강력한 비밀번호 (저장 필요)
   - **Region**: Northeast Asia (Tokyo) - 한국과 가까움

### 2. API 키 확보

프로젝트 대시보드 → Settings → API

필요한 키:
- **Project URL**: `https://xxxxx.supabase.co`
- **anon (public) key**: `eyJhbGciOi...` (긴 토큰)

### 3. 데이터베이스 스키마 생성

Supabase SQL Editor에서 `docs/database/SCHEMA.md`의 SQL 스크립트 실행

또는 MCP 도구 사용:
```bash
# Supabase MCP를 통해 마이그레이션 적용
mcp_supabase_apply_migration
```

### 4. Storage 버킷 생성

Storage → New Bucket:
- **Name**: `pdf-documents`
- **Public**: No (비공개)

---

## 프로젝트 설정

### 1. 저장소 클론

```bash
git clone https://github.com/your-org/nplogic.git
cd nplogic
```

### 2. 솔루션 복원

```bash
dotnet restore NPLogic.sln
```

### 3. 설정 파일 생성

#### appsettings.json

`src/NPLogic.App/appsettings.json.template`을 복사:

```bash
cd src/NPLogic.App
copy appsettings.json.template appsettings.json
```

`appsettings.json` 편집:

```json
{
  "Supabase": {
    "Url": "https://your-project-id.supabase.co",
    "Key": "your-anon-key-here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Python": {
    "ExecutablePath": "python",
    "OcrScriptPath": "python/ocr_processor.py"
  }
}
```

**⚠️ 중요**: `appsettings.json`은 `.gitignore`에 포함되어 Git에 커밋되지 않습니다.

### 4. Python 경로 확인

`appsettings.json`의 `Python.ExecutablePath`가 시스템의 Python 경로와 일치하는지 확인:

```bash
where python  # Windows
which python  # macOS/Linux
```

필요시 절대 경로로 변경:
```json
"ExecutablePath": "C:\\Python310\\python.exe"
```

---

## 빌드 및 실행

### 1. 솔루션 빌드

```bash
dotnet build NPLogic.sln
```

또는 Visual Studio에서: `Ctrl + Shift + B`

### 2. 애플리케이션 실행

**명령줄**:
```bash
dotnet run --project src/NPLogic.App/NPLogic.App.csproj
```

**Visual Studio**:
1. NPLogic.App을 시작 프로젝트로 설정 (우클릭 → 시작 프로젝트로 설정)
2. F5 (디버그) 또는 Ctrl+F5 (디버그 없이 실행)

### 3. 테스트 실행 (향후)

```bash
dotnet test NPLogic.sln
```

---

## 문제 해결

### 빌드 오류

#### "SDK not found" 오류
```bash
dotnet --info
```
.NET 8.0 SDK가 설치되어 있는지 확인

#### NuGet 패키지 복원 실패
```bash
dotnet restore --force
dotnet nuget locals all --clear
```

### Python 관련 오류

#### "python not found" 오류
- PATH 환경 변수에 Python 경로 추가
- `appsettings.json`에 절대 경로 사용

#### "No module named 'pytesseract'" 오류
```bash
cd python
pip install -r requirements.txt
```

#### Tesseract 실행 오류
```python
# ocr_processor.py에 Tesseract 경로 명시
pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'
```

### Supabase 연결 오류

#### "Invalid API key" 오류
- `appsettings.json`의 키 확인
- Supabase 대시보드에서 키 재확인
- 키에 공백이나 특수문자 포함 여부 확인

#### "Network error" 오류
- 인터넷 연결 확인
- 방화벽 설정 확인
- Supabase 프로젝트 상태 확인

### Visual Studio 관련

#### Intellisense 작동 안 함
1. 솔루션 닫기
2. `.vs` 폴더 삭제
3. `bin`, `obj` 폴더 삭제
4. 솔루션 다시 열기
5. `dotnet restore`

#### WebView2 런타임 오류
WebView2 런타임 설치:
```bash
winget install Microsoft.EdgeWebView2Runtime
```

---

## 개발 도구 추천 설정

### Visual Studio 2022

**옵션 → 텍스트 편집기 → C#**:
- 탭: 4칸, 공백 사용
- 중괄호 자동 포맷
- using 자동 정렬

**확장 추천**:
- ReSharper (유료, 선택)
- CodeMaid (무료)
- Productivity Power Tools

### VS Code

**settings.json**:
```json
{
  "editor.formatOnSave": true,
  "editor.tabSize": 4,
  "files.exclude": {
    "**/bin": true,
    "**/obj": true
  },
  "omnisharp.enableEditorConfigSupport": true
}
```

---

## 환경 변수 설정 (선택)

시스템 환경 변수로 민감한 정보 관리:

**Windows**:
```cmd
setx NPLOGIC_SUPABASE_URL "https://xxxxx.supabase.co"
setx NPLOGIC_SUPABASE_KEY "your-key"
```

**코드에서 사용**:
```csharp
var url = Environment.GetEnvironmentVariable("NPLOGIC_SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("NPLOGIC_SUPABASE_KEY");
```

---

## 체크리스트

개발 환경이 올바르게 설정되었는지 확인:

- [ ] .NET 8 SDK 설치 확인 (`dotnet --version`)
- [ ] Python 3.10+ 설치 확인 (`python --version`)
- [ ] Python 패키지 설치 완료 (`pip list`)
- [ ] Tesseract OCR 설치 확인
- [ ] Supabase 프로젝트 생성
- [ ] API 키 확보 및 `appsettings.json` 설정
- [ ] 프로젝트 빌드 성공 (`dotnet build`)
- [ ] 애플리케이션 실행 가능 (`dotnet run`)

---

## 추가 리소스

- [.NET 8 문서](https://learn.microsoft.com/dotnet/)
- [WPF 가이드](https://learn.microsoft.com/dotnet/desktop/wpf/)
- [Supabase 문서](https://supabase.com/docs)
- [Material Design in XAML](http://materialdesigninxaml.net/)
- [MVVM Toolkit](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

---

**도움이 필요하면**: 팀 리더에게 문의하거나 프로젝트 이슈 트래커에 질문을 등록하세요.

**마지막 업데이트**: 2025-11-20





