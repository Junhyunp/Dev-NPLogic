# Supabase Google OAuth 설정 가이드

## 1. Google Cloud Console 설정

### Step 1: Google Cloud 프로젝트 생성
1. [Google Cloud Console](https://console.cloud.google.com/) 접속
2. 프로젝트 생성 또는 기존 프로젝트 선택
3. 프로젝트 이름: `NPLogic` (또는 원하는 이름)

### Step 2: OAuth 동의 화면 구성
1. 좌측 메뉴 → **API 및 서비스** → **OAuth 동의 화면**
2. **외부** 선택 (내부 조직이 있으면 내부 선택 가능)
3. 앱 정보 입력:
   - 앱 이름: `NPLogic`
   - 사용자 지원 이메일: 본인 이메일
   - 앱 로고: (선택사항)
   - 승인된 도메인: `supabase.co`
   - 개발자 연락처 정보: 본인 이메일
4. **저장 후 계속**

### Step 3: 범위 추가 (선택사항)
1. 범위 추가 또는 삭제
2. 기본 범위만으로도 충분 (이메일, 프로필)
3. **저장 후 계속**

### Step 4: OAuth 2.0 클라이언트 ID 생성
1. 좌측 메뉴 → **API 및 서비스** → **사용자 인증 정보**
2. **사용자 인증 정보 만들기** → **OAuth 클라이언트 ID**
3. 애플리케이션 유형: **웹 애플리케이션**
4. 이름: `NPLogic Supabase`
5. **승인된 리디렉션 URI** 추가:
   ```
   https://vuepmhwrizaabswlgiiy.supabase.co/auth/v1/callback
   ```
6. **만들기** 클릭
7. 생성된 **클라이언트 ID**와 **클라이언트 보안 비밀번호**를 복사해둡니다

---

## 2. Supabase Dashboard 설정

### Step 1: Authentication 설정
1. [Supabase Dashboard](https://supabase.com/dashboard) 접속
2. NPLogic 프로젝트 선택
3. 좌측 메뉴 → **Authentication** → **Providers**

### Step 2: Google Provider 활성화
1. **Google** 찾아서 클릭
2. **Enable Sign in with Google** 토글 ON
3. 다음 정보 입력:
   - **Client ID**: Google Cloud Console에서 복사한 클라이언트 ID
   - **Client Secret**: Google Cloud Console에서 복사한 클라이언트 보안 비밀번호
4. **Save** 클릭

### Step 3: Site URL 설정
1. **Authentication** → **URL Configuration**
2. **Site URL**: 
   - 개발: `http://localhost:3000`
   - 프로덕션: 실제 도메인
3. **Redirect URLs** 추가:
   - `http://localhost:3000/auth/callback`
   - 프로덕션 도메인도 추가

---

## 3. C# 코드 구현 (이미 완료됨)

### AuthService.cs
```csharp
// 구글 로그인
var session = await client.Auth.SignIn(
    Supabase.Gotrue.Constants.Provider.Google,
    new Supabase.Gotrue.SignInOptions
    {
        RedirectTo = "http://localhost:3000/auth/callback"
    }
);
```

### LoginViewModel.cs
```csharp
[RelayCommand]
private async Task SignInWithGoogleAsync()
{
    var client = _supabaseService.GetClient();
    var session = await client.Auth.SignIn(
        Supabase.Gotrue.Constants.Provider.Google,
        new Supabase.Gotrue.SignInOptions
        {
            RedirectTo = "http://localhost:3000/auth/callback"
        }
    );
    
    if (session?.User != null)
    {
        // 로그인 성공 처리
        OnLoginSuccess();
    }
}
```

---

## 4. WPF에서 OAuth 리디렉션 처리

WPF 데스크톱 앱에서 OAuth 리디렉션을 처리하는 방법:

### 방법 1: 브라우저 기반 (권장)
1. 기본 브라우저로 OAuth URL 열기
2. 로컬 HTTP 서버로 콜백 수신
3. 토큰 추출 후 Supabase 세션 설정

### 방법 2: WebView2 사용
```csharp
// WebView2 컨트롤에서 OAuth 진행
var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
await webView.EnsureCoreWebView2Async();
webView.CoreWebView2.Navigate(oauthUrl);

// 네비게이션 완료 이벤트에서 토큰 추출
webView.CoreWebView2.NavigationCompleted += (s, e) =>
{
    // URL에서 토큰 파싱
};
```

---

## 5. 로컬 HTTP 서버 구현 (권장 방법)

### HttpListener를 사용한 콜백 수신

```csharp
using System.Net;
using System.Web;

public class OAuthCallbackServer
{
    private HttpListener? _listener;

    public async Task<string> ListenForCallbackAsync()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:3000/");
        _listener.Start();

        try
        {
            var context = await _listener.GetContextAsync();
            var response = context.Response;
            
            // URL에서 토큰 추출
            var query = context.Request.Url?.Query;
            var queryParams = HttpUtility.ParseQueryString(query ?? "");
            var accessToken = queryParams["access_token"];
            var refreshToken = queryParams["refresh_token"];

            // 성공 페이지 표시
            var responseString = "<html><body><h1>로그인 성공!</h1><p>창을 닫아주세요.</p></body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();

            return accessToken ?? "";
        }
        finally
        {
            _listener.Stop();
        }
    }
}
```

---

## 6. 테스트

### 1. Supabase 설정 확인
```bash
# Supabase Dashboard에서 확인
Authentication → Providers → Google (활성화됨)
```

### 2. Google OAuth 테스트
1. NPLogic 앱 실행
2. **Google 계정으로 로그인** 버튼 클릭
3. 브라우저에서 Google 계정 선택
4. 권한 승인
5. 리디렉션 후 자동 로그인

### 3. Supabase MCP로 확인
```sql
-- users 테이블에 사용자 추가되었는지 확인
SELECT * FROM users;
```

---

## 7. 트러블슈팅

### 문제 1: 리디렉션 URL 오류
- Google Cloud Console의 승인된 리디렉션 URI 확인
- Supabase의 콜백 URL과 정확히 일치하는지 확인

### 문제 2: CORS 오류
- Supabase Dashboard에서 URL Configuration 확인
- Site URL과 Redirect URLs가 올바른지 확인

### 문제 3: users 테이블에 사용자 없음
- LoginViewModel의 CreateUserRecordAsync 로직 확인
- Supabase RLS 정책 확인

---

## 8. 보안 고려사항

### 1. 클라이언트 시크릿 보호
- Google 클라이언트 시크릿은 Supabase에만 저장
- 절대 C# 코드에 하드코딩하지 말 것

### 2. 토큰 저장
```csharp
// 보안 저장소에 저장 (Windows Credential Manager 등)
var credential = new PasswordCredential
{
    UserName = "supabase_session",
    Password = refreshToken
};
```

### 3. 자동 로그인
```csharp
// 저장된 세션 복원
var session = await client.Auth.RetrieveSession();
if (session != null)
{
    // 자동 로그인 성공
}
```

---

## 9. 다음 단계

1. ✅ Google OAuth 설정 완료
2. ✅ C# 로그인 코드 구현
3. ⏳ HTTP 리스너 구현 (선택사항)
4. ⏳ 토큰 보안 저장
5. ⏳ 자동 로그인 구현

---

## 참고 자료

- [Supabase Auth Documentation](https://supabase.com/docs/guides/auth)
- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [Supabase C# Client](https://github.com/supabase-community/supabase-csharp)

---

**현재 상태**: 
- ✅ Supabase 프로젝트 설정 완료
- ⏳ Google OAuth 설정 필요 (위 가이드 따라하기)
- ✅ C# 코드 준비 완료

