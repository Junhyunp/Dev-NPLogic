# NPLogic 데이터베이스 스키마 설계

## Supabase PostgreSQL 구조

> **🛠️ 개발 도구**: 이 프로젝트에서는 Supabase MCP를 사용하여 DB를 직접 제어할 수 있습니다.
> - 테이블 생성: `mcp_supabase_apply_migration`
> - SQL 실행: `mcp_supabase_execute_sql`
> - 테이블 조회: `mcp_supabase_list_tables`
> - 로그 확인: `mcp_supabase_get_logs`
> - TypeScript 타입 생성: `mcp_supabase_generate_typescript_types`

---

## 테이블 목록

1. **users** - 사용자 정보
2. **properties** - 물건 기본 정보
3. **data_disks** - 엑셀 데이터 디스크
4. **registry_documents** - 등기부등본 정보
5. **registry_owners** - 등기부 소유자 정보
6. **registry_rights** - 등기부 권리 정보 (근저당, 가압류 등)
7. **right_analysis** - 권리 분석 결과
8. **evaluations** - 평가 정보
9. **auction_schedules** - 경매 일정
10. **public_sale_schedules** - 공매 일정
11. **loan_info** - 대출 정보
12. **statistics** - 통계 데이터 (선택적)
13. **audit_logs** - 작업 이력
14. **settings** - 시스템 설정
15. **calculation_formulas** - 계산 수식 설정

---

## 상세 스키마

### 1. users (사용자)

```sql
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  auth_user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
  email VARCHAR(255) UNIQUE NOT NULL,
  name VARCHAR(100) NOT NULL,
  role VARCHAR(20) NOT NULL CHECK (role IN ('pm', 'evaluator', 'admin')),
  status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive')),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_users_auth_user_id ON users(auth_user_id);
CREATE INDEX idx_users_role ON users(role);
```

**역할 (role)**:
- `pm`: 프로젝트 매니저
- `evaluator`: 평가자 (회계사)
- `admin`: 관리자

---

### 2. properties (물건 기본 정보)

```sql
CREATE TABLE properties (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  project_id VARCHAR(50), -- 프로젝트 번호
  property_number VARCHAR(100), -- 물건번호
  property_type VARCHAR(50), -- 물건 유형 (아파트, 상가, 토지 등)
  
  -- 주소 정보
  address_full TEXT, -- 전체 주소
  address_road TEXT, -- 도로명주소
  address_jibun TEXT, -- 지번주소
  address_detail TEXT, -- 상세주소
  
  -- 기본 정보
  land_area DECIMAL(15,2), -- 토지 면적 (㎡)
  building_area DECIMAL(15,2), -- 건물 면적 (㎡)
  floors VARCHAR(50), -- 층수 정보
  completion_date DATE, -- 준공일
  
  -- 가격 정보
  appraisal_value DECIMAL(15,2), -- 감정가
  minimum_bid DECIMAL(15,2), -- 최저입찰가
  sale_price DECIMAL(15,2), -- 낙찰가
  
  -- 위치 정보
  latitude DECIMAL(10,8), -- 위도
  longitude DECIMAL(11,8), -- 경도
  
  -- 상태
  status VARCHAR(20) DEFAULT 'pending', -- pending, processing, completed
  
  -- 담당자
  assigned_to UUID REFERENCES users(id),
  
  -- 메타데이터
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW(),
  
  UNIQUE(project_id, property_number)
);

CREATE INDEX idx_properties_project_id ON properties(project_id);
CREATE INDEX idx_properties_status ON properties(status);
CREATE INDEX idx_properties_assigned_to ON properties(assigned_to);
CREATE INDEX idx_properties_address ON properties USING gin(to_tsvector('korean', address_full));
```

---

### 3. data_disks (엑셀 데이터 디스크)

```sql
CREATE TABLE data_disks (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 차주 정보
  debtor_name VARCHAR(200),
  debtor_regno VARCHAR(50), -- 주민/사업자등록번호
  
  -- 채권 정보
  debt_type VARCHAR(50),
  principal_amount DECIMAL(15,2), -- 원금
  interest_rate DECIMAL(5,2), -- 이자율
  overdue_interest_rate DECIMAL(5,2), -- 연체이자율
  
  -- 경매/공매 정보
  sale_type VARCHAR(20), -- 경매, 공매
  court_name VARCHAR(100), -- 법원명
  case_number VARCHAR(100), -- 사건번호
  
  -- 기타 데이터
  data_json JSONB, -- 추가 데이터 (유연한 구조)
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_data_disks_property_id ON data_disks(property_id);
```

---

### 4. registry_documents (등기부등본)

```sql
CREATE TABLE registry_documents (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 파일 정보
  file_path TEXT NOT NULL, -- Supabase Storage 경로
  file_name VARCHAR(255),
  file_size BIGINT,
  
  -- OCR 처리
  ocr_status VARCHAR(20) DEFAULT 'pending', -- pending, processing, completed, failed
  ocr_processed_at TIMESTAMPTZ,
  ocr_error TEXT,
  
  -- 등기부 기본 정보
  registry_type VARCHAR(20), -- 토지, 건물
  registry_number VARCHAR(100), -- 등기번호
  
  -- OCR 추출 데이터 (JSON)
  extracted_data JSONB,
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_registry_documents_property_id ON registry_documents(property_id);
CREATE INDEX idx_registry_documents_ocr_status ON registry_documents(ocr_status);
```

---

### 5. registry_owners (등기부 소유자)

```sql
CREATE TABLE registry_owners (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  registry_document_id UUID REFERENCES registry_documents(id) ON DELETE CASCADE,
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 소유자 정보
  owner_name VARCHAR(200),
  owner_regno VARCHAR(50), -- 주민/사업자등록번호
  share_ratio VARCHAR(50), -- 지분 비율 (예: "1/2")
  
  -- 등기 정보
  registration_date DATE, -- 등기일
  registration_cause TEXT, -- 등기 원인
  
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_registry_owners_property_id ON registry_owners(property_id);
CREATE INDEX idx_registry_owners_registry_doc ON registry_owners(registry_document_id);
```

---

### 6. registry_rights (등기부 권리 정보)

```sql
CREATE TABLE registry_rights (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  registry_document_id UUID REFERENCES registry_documents(id) ON DELETE CASCADE,
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 권리 유형
  right_type VARCHAR(50) NOT NULL, -- 근저당, 가압류, 가등기, 전세권 등
  right_order INTEGER, -- 순위
  
  -- 권리자 정보
  right_holder VARCHAR(200), -- 권리자 이름
  
  -- 금액 정보
  claim_amount DECIMAL(15,2), -- 채권 최고액
  
  -- 등기 정보
  registration_date DATE, -- 등기일
  registration_number VARCHAR(100), -- 접수번호
  registration_cause TEXT, -- 등기 원인
  
  -- 상태
  status VARCHAR(20) DEFAULT 'active', -- active, cancelled
  
  -- 추가 정보
  notes TEXT,
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_registry_rights_property_id ON registry_rights(property_id);
CREATE INDEX idx_registry_rights_type ON registry_rights(right_type);
CREATE INDEX idx_registry_rights_order ON registry_rights(right_order);
```

---

### 7. right_analysis (권리 분석)

```sql
CREATE TABLE right_analysis (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 선순위 분석
  senior_rights_total DECIMAL(15,2), -- 선순위 합계
  mortgage_count INTEGER, -- 근저당 개수
  seizure_count INTEGER, -- 가압류 개수
  
  -- 배당 분석
  distribution_analysis JSONB, -- 배당 시뮬레이션 결과
  
  -- 권리 평가
  risk_level VARCHAR(20), -- high, medium, low
  recommendations TEXT, -- 권리 분석 의견
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_right_analysis_property_id ON right_analysis(property_id);
```

---

### 8. evaluations (평가 정보)

```sql
CREATE TABLE evaluations (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 평가 유형
  evaluation_type VARCHAR(50), -- 아파트, 상가, 토지 등
  
  -- 평가 금액
  market_value DECIMAL(15,2), -- 시세
  evaluated_value DECIMAL(15,2), -- 평가액
  recovery_rate DECIMAL(5,2), -- 회수율 (%)
  
  -- 평가 상세 (JSON)
  evaluation_details JSONB,
  
  -- 평가자
  evaluated_by UUID REFERENCES users(id),
  evaluated_at TIMESTAMPTZ,
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_evaluations_property_id ON evaluations(property_id);
CREATE INDEX idx_evaluations_evaluated_by ON evaluations(evaluated_by);
```

---

### 9. auction_schedules (경매 일정)

```sql
CREATE TABLE auction_schedules (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 경매 정보
  auction_number VARCHAR(100), -- 경매 차수
  auction_date DATE, -- 경매일
  bid_date DATE, -- 입찰일
  
  -- 가격 정보
  minimum_bid DECIMAL(15,2), -- 최저 입찰가
  sale_price DECIMAL(15,2), -- 낙찰가
  
  -- 상태
  status VARCHAR(20), -- scheduled, completed, cancelled
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_auction_schedules_property_id ON auction_schedules(property_id);
CREATE INDEX idx_auction_schedules_date ON auction_schedules(auction_date);
```

---

### 10. public_sale_schedules (공매 일정)

```sql
CREATE TABLE public_sale_schedules (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 공매 정보
  sale_number VARCHAR(100),
  sale_date DATE,
  
  -- 가격 정보
  minimum_bid DECIMAL(15,2),
  sale_price DECIMAL(15,2),
  
  -- 상태
  status VARCHAR(20),
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_public_sale_schedules_property_id ON public_sale_schedules(property_id);
```

---

### 11. loan_info (대출 정보)

```sql
CREATE TABLE loan_info (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  property_id UUID REFERENCES properties(id) ON DELETE CASCADE,
  
  -- 대출 유형
  loan_type VARCHAR(50), -- 일반, 일반+해지부보증, 일반보증, 해지부보증
  
  -- 대출 금액
  loan_amount DECIMAL(15,2),
  interest_rate DECIMAL(5,2),
  
  -- 보증 정보
  guarantee_type VARCHAR(50),
  guarantee_amount DECIMAL(15,2),
  
  -- 대출 상세
  loan_details JSONB,
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_loan_info_property_id ON loan_info(property_id);
```

---

### 12. audit_logs (작업 이력)

```sql
CREATE TABLE audit_logs (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  
  -- 작업 정보
  table_name VARCHAR(100), -- 대상 테이블
  record_id UUID, -- 대상 레코드 ID
  action VARCHAR(20), -- INSERT, UPDATE, DELETE
  
  -- 변경 내역
  old_data JSONB, -- 변경 전 데이터
  new_data JSONB, -- 변경 후 데이터
  
  -- 사용자 정보
  user_id UUID REFERENCES users(id),
  user_email VARCHAR(255),
  
  -- 메타데이터
  ip_address VARCHAR(50),
  user_agent TEXT,
  
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_table_record ON audit_logs(table_name, record_id);
CREATE INDEX idx_audit_logs_user ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_created ON audit_logs(created_at);
```

---

### 13. settings (시스템 설정)

```sql
CREATE TABLE settings (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  
  -- 설정 정보
  setting_key VARCHAR(100) UNIQUE NOT NULL,
  setting_value JSONB,
  setting_type VARCHAR(50), -- 계산수식, 데이터매핑, 시스템환경
  
  -- 설명
  description TEXT,
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_settings_key ON settings(setting_key);
CREATE INDEX idx_settings_type ON settings(setting_type);
```

---

### 14. calculation_formulas (계산 수식)

```sql
CREATE TABLE calculation_formulas (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  
  -- 수식 정보
  formula_name VARCHAR(100) UNIQUE NOT NULL,
  formula_expression TEXT NOT NULL, -- 수식 표현
  formula_description TEXT,
  
  -- 적용 대상
  applies_to VARCHAR(50), -- property_type 등
  
  -- 상태
  is_active BOOLEAN DEFAULT true,
  
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_formulas_name ON calculation_formulas(formula_name);
CREATE INDEX idx_formulas_active ON calculation_formulas(is_active);
```

---

## Row Level Security (RLS) 정책

### 1. users 테이블

```sql
-- Enable RLS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- 관리자는 모든 사용자 조회/수정 가능
CREATE POLICY "Admins can view all users"
ON users FOR SELECT
TO authenticated
USING (
  EXISTS (
    SELECT 1 FROM users
    WHERE auth_user_id = auth.uid()
    AND role = 'admin'
  )
);

-- 사용자는 자신의 정보만 조회 가능
CREATE POLICY "Users can view own data"
ON users FOR SELECT
TO authenticated
USING (auth_user_id = auth.uid());
```

### 2. properties 테이블

```sql
ALTER TABLE properties ENABLE ROW LEVEL SECURITY;

-- PM과 관리자는 모든 물건 조회 가능
CREATE POLICY "PM and Admin can view all properties"
ON properties FOR SELECT
TO authenticated
USING (
  EXISTS (
    SELECT 1 FROM users
    WHERE auth_user_id = auth.uid()
    AND role IN ('pm', 'admin')
  )
);

-- 평가자는 자신에게 할당된 물건만 조회 가능
CREATE POLICY "Evaluators can view assigned properties"
ON properties FOR SELECT
TO authenticated
USING (
  assigned_to IN (
    SELECT id FROM users WHERE auth_user_id = auth.uid()
  )
);

-- PM과 관리자는 모든 물건 수정 가능
CREATE POLICY "PM and Admin can modify properties"
ON properties FOR ALL
TO authenticated
USING (
  EXISTS (
    SELECT 1 FROM users
    WHERE auth_user_id = auth.uid()
    AND role IN ('pm', 'admin')
  )
);

-- 평가자는 할당된 물건만 수정 가능
CREATE POLICY "Evaluators can modify assigned properties"
ON properties FOR UPDATE
TO authenticated
USING (
  assigned_to IN (
    SELECT id FROM users WHERE auth_user_id = auth.uid()
  )
);
```

### 3. 다른 테이블들

나머지 테이블들도 유사한 정책 적용:
- 기본적으로 property_id를 통해 접근 권한 확인
- audit_logs는 읽기 전용 (관리자만)
- settings는 관리자만 수정 가능

---

## Supabase Storage 버킷

### 1. registry-pdfs (등기부등본 PDF)

```javascript
// 버킷 생성
supabase.storage.createBucket('registry-pdfs', {
  public: false,
  fileSizeLimit: 52428800 // 50MB
});

// RLS 정책
// 인증된 사용자만 업로드/다운로드
```

**경로 구조**:
```
registry-pdfs/
  ├── {project_id}/
  │   ├── {property_number}/
  │   │   ├── registry_land.pdf
  │   │   ├── registry_building.pdf
```

### 2. excel-files (엑셀 파일)

```javascript
supabase.storage.createBucket('excel-files', {
  public: false,
  fileSizeLimit: 104857600 // 100MB
});
```

**경로 구조**:
```
excel-files/
  ├── uploads/
  │   ├── data_disk_{timestamp}.xlsx
  ├── exports/
  │   ├── property_{property_id}_{timestamp}.xlsx
  │   ├── statistics_{timestamp}.xlsx
```

---

## 인덱싱 전략

### 복합 인덱스

```sql
-- 물건 검색 최적화
CREATE INDEX idx_properties_search 
ON properties(status, assigned_to, created_at DESC);

-- 등기부 OCR 처리 조회
CREATE INDEX idx_registry_ocr_pending
ON registry_documents(ocr_status, created_at)
WHERE ocr_status = 'pending';

-- 작업 이력 조회 최적화
CREATE INDEX idx_audit_logs_user_date
ON audit_logs(user_id, created_at DESC);
```

### Full-Text Search (한글)

```sql
-- 주소 검색
CREATE INDEX idx_properties_address_fts
ON properties
USING gin(to_tsvector('korean', address_full));

-- 사용 예시
SELECT * FROM properties
WHERE to_tsvector('korean', address_full) @@ to_tsquery('korean', '서울');
```

---

## 데이터 마이그레이션 스크립트

### 초기 데이터

```sql
-- 기본 관리자 계정 (Supabase Auth 연동 후)
INSERT INTO users (auth_user_id, email, name, role)
VALUES (
  'auth-user-uuid',
  'admin@nplogic.com',
  '관리자',
  'admin'
);

-- 기본 시스템 설정
INSERT INTO settings (setting_key, setting_value, setting_type)
VALUES
  ('default_recovery_rate', '{"value": 70}', '시스템환경'),
  ('ocr_batch_size', '{"value": 50}', '시스템환경'),
  ('max_file_size_mb', '{"value": 50}', '시스템환경');
```

---

## 백업 및 복원

### Supabase 자동 백업
- 매일 자동 백업 (Supabase 플랫폼 기능)
- Point-in-Time Recovery (PITR) 지원

### 수동 백업
```bash
# pg_dump를 이용한 백업
pg_dump -h db.xxxxx.supabase.co -U postgres nplogic > backup.sql

# 복원
psql -h db.xxxxx.supabase.co -U postgres nplogic < backup.sql
```

---

## 성능 최적화

### 1. Connection Pooling
- Supabase는 기본적으로 PgBouncer 사용
- 최대 커넥션: 무료 플랜 60개, Pro 플랜 200개

### 2. 쿼리 최적화
- EXPLAIN ANALYZE로 쿼리 성능 확인
- 필요한 컬럼만 SELECT
- 페이지네이션 사용 (LIMIT, OFFSET)

### 3. JSONB 인덱싱
```sql
-- JSONB 필드 특정 키 인덱싱
CREATE INDEX idx_extracted_data_owner
ON registry_documents ((extracted_data->>'owner_name'));
```

---

## 다음 단계

1. Supabase 프로젝트 생성
2. SQL 스크립트 실행
3. RLS 정책 테스트
4. Storage 버킷 생성
5. C# 모델 클래스 생성

