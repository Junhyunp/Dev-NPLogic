# API 키 설정 가이드

NPLogic 애플리케이션에서 사용하는 외부 API 키 발급 및 설정 방법입니다.

## 1. 카카오맵 API (필수)

지도 표시, 주소 검색, 로드뷰 기능에 필요합니다.

### 발급 방법

1. [카카오 개발자 센터](https://developers.kakao.com/) 접속
2. 로그인 후 "내 애플리케이션" 메뉴 클릭
3. "애플리케이션 추가하기" 클릭
4. 앱 이름, 사업자명 입력 후 저장
5. 생성된 앱 선택 → "앱 키" 메뉴에서 **JavaScript 키** 복사

### 플랫폼 등록 (중요!)

1. "앱 설정" → "플랫폼" 메뉴
2. "Web 플랫폼 등록" 클릭
3. 사이트 도메인에 다음 추가:
   - `http://localhost` (개발용)
   - `file://` (WPF 로컬 파일 접근용)

### 설정 방법

`appsettings.json`에 추가:

```json
{
  "KakaoMap": {
    "ApiKey": "발급받은_JavaScript_키"
  }
}
```

---

## 2. 국토교통부 실거래가 API (선택)

아파트 실거래가 조회 기능에 필요합니다.

### 발급 방법

1. [공공데이터포털](https://www.data.go.kr/) 접속
2. 회원가입 및 로그인
3. "국토교통부 아파트매매 실거래자료" 검색
4. [API 신청 링크](https://www.data.go.kr/data/15057511/openapi.do) 접속
5. "활용신청" 버튼 클릭
6. 활용 목적 입력 후 신청 (승인까지 1-2일 소요)
7. 승인 후 "마이페이지" → "API 활용관리"에서 인증키 확인

### 설정 방법

`appsettings.json`에 추가:

```json
{
  "RealEstateAPI": {
    "DataGoKrKey": "발급받은_인증키"
  }
}
```

---

## 3. 한국토지정보시스템 API (선택)

공시지가 조회 및 토지이용계획 확인에 필요합니다.

### 발급 방법

1. [공공데이터포털](https://www.data.go.kr/) 접속
2. "한국토지정보시스템" 또는 "공시지가정보" 검색
3. 원하는 API 선택 후 활용신청

### 관련 API 목록

| API명 | 용도 | 링크 |
|-------|------|------|
| 개별공시지가정보 | 토지 공시지가 조회 | [바로가기](https://www.data.go.kr/data/15057267/openapi.do) |
| 토지이용규제정보 | 용도지역/지구 확인 | [바로가기](https://www.data.go.kr/data/15016775/openapi.do) |

---

## 4. KB시세 정보 (참고)

KB시세는 공식 API를 제공하지 않습니다. 대안:

1. **수동 조회**: [KB부동산](https://kbland.kr/) 사이트에서 직접 조회
2. **실거래가 API 활용**: 국토교통부 실거래가 API로 대체
3. **크롤링**: 이용약관 확인 후 자체 구현 (권장하지 않음)

현재 구현에서는 KB시세 사이트로 바로가기 버튼을 제공합니다.

---

## appsettings.json 전체 예시

```json
{
  "Supabase": {
    "Url": "https://your-project-id.supabase.co",
    "Key": "your-anon-key-here"
  },
  "KakaoMap": {
    "ApiKey": "your-kakao-javascript-key"
  },
  "RealEstateAPI": {
    "DataGoKrKey": "your-data-go-kr-key"
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

---

## 문제 해결

### 카카오맵이 표시되지 않을 때

1. API 키가 올바르게 입력되었는지 확인
2. 플랫폼 등록에 `http://localhost`와 `file://`이 포함되어 있는지 확인
3. 일일 사용량 제한(300,000회) 초과 여부 확인

### 실거래가 API가 작동하지 않을 때

1. 인증키 활성화 상태 확인 (공공데이터포털 마이페이지)
2. 신청 승인 완료 여부 확인
3. 인코딩 방식 확인 (UTF-8)

---

## 참고 링크

- [카카오맵 API 문서](https://apis.map.kakao.com/web/documentation/)
- [공공데이터포털](https://www.data.go.kr/)
- [KB부동산](https://kbland.kr/)
- [국토이용정보서비스(LURIS)](https://www.eum.go.kr/)

