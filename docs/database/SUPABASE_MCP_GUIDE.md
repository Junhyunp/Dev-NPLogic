# Supabase MCP 도구 사용 가이드

## 개요

이 프로젝트에서는 Supabase MCP (Model Context Protocol) 도구를 사용하여 데이터베이스를 직접 제어할 수 있습니다. Cursor에서 AI와 대화하면서 바로 DB 작업이 가능합니다.

---

## 주요 도구

### 1. 테이블 관리

#### 테이블 목록 조회
```
"Supabase 테이블 목록 보여줘"
```
도구: `mcp_supabase_list_tables`

#### 마이그레이션 적용 (테이블 생성/수정)
```
"users 테이블 생성해줘"
```
도구: `mcp_supabase_apply_migration`

### 2. 데이터 조작

#### SQL 쿼리 실행
```
"users 테이블에서 모든 데이터 조회해줘"
```
도구: `mcp_supabase_execute_sql`

#### 데이터 삽입/수정/삭제
```
"users 테이블에 테스트 사용자 추가해줘"
```
도구: `mcp_supabase_execute_sql`

### 3. 모니터링

#### 로그 확인
```
"Supabase API 로그 보여줘"
```
도구: `mcp_supabase_get_logs`
- 서비스: api, postgres, auth, storage, realtime

#### 보안 어드바이저
```
"Supabase 보안 문제 확인해줘"
```
도구: `mcp_supabase_get_advisors`

### 4. Edge Functions

#### Edge Function 목록
```
"Edge Functions 목록 보여줘"
```
도구: `mcp_supabase_list_edge_functions`

#### Edge Function 배포
```
"새 Edge Function 배포해줘"
```
도구: `mcp_supabase_deploy_edge_function`

### 5. TypeScript 타입 생성

```
"DB 스키마에서 TypeScript 타입 생성해줘"
```
도구: `mcp_supabase_generate_typescript_types`

---

## 실제 사용 예시

### 예시 1: 테이블 생성
```
나: "users 테이블 생성해줘. auth_user_id, email, name, role 컬럼 포함"

AI가 mcp_supabase_apply_migration 실행:
- name: "create_users_table"
- query: "CREATE TABLE users (...)"
```

### 예시 2: 데이터 조회
```
나: "users 테이블의 모든 PM 역할 사용자 조회해줘"

AI가 mcp_supabase_execute_sql 실행:
- query: "SELECT * FROM users WHERE role = 'pm'"
```

### 예시 3: RLS 정책 적용
```
나: "users 테이블에 RLS 정책 적용해줘. 관리자만 모든 데이터 볼 수 있게"

AI가 mcp_supabase_apply_migration 실행:
- name: "add_users_rls_policy"
- query: "ALTER TABLE users ENABLE ROW LEVEL SECURITY; ..."
```

---

## 장점

1. **빠른 개발**: SQL 파일 작성 → 실행 과정을 한 번에
2. **실시간 확인**: 바로 결과 확인 가능
3. **에러 디버깅**: 로그 즉시 확인
4. **타입 안전성**: TypeScript 타입 자동 생성

---

## 주의사항

⚠️ **프로덕션 DB 주의**
- 개발/테스트 환경에서 먼저 테스트
- 중요한 데이터 변경은 백업 후 진행
- DDL 작업은 `apply_migration` 사용 (롤백 가능)

⚠️ **권한 확인**
- Supabase 프로젝트에 적절한 권한 필요
- API 키가 환경 변수에 설정되어 있어야 함

---

## 다음 단계

1. Supabase 프로젝트 생성
2. API 키 설정
3. `docs/database/SCHEMA.md`의 스키마를 MCP로 적용
4. 데이터베이스 구축 완료!

이제 "Supabase에 users 테이블 만들어줘" 같은 명령으로 바로 DB 작업이 가능합니다. 🚀



