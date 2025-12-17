-- Phase 6 DB 마이그레이션 스크립트
-- 생성일: 2024-12-17
-- 설명: OPB 컬럼 및 상권 데이터 체크박스 추가

-- ============================================
-- 6.4 물건에 OPB 컬럼 추가
-- ============================================
ALTER TABLE properties 
ADD COLUMN IF NOT EXISTS opb DECIMAL(18, 2);

COMMENT ON COLUMN properties.opb IS 'OPB (Outstanding Principal Balance, 대출잔액)';

-- ============================================
-- 6.5 상가/아파트형공장 상권 데이터 체크박스
-- ============================================
ALTER TABLE properties 
ADD COLUMN IF NOT EXISTS has_commercial_district_data BOOLEAN DEFAULT FALSE;

COMMENT ON COLUMN properties.has_commercial_district_data IS '상권 데이터 확보 여부 (상가/아파트형공장 전용)';

-- ============================================
-- 인덱스 추가 (선택사항 - 성능 최적화)
-- ============================================
-- OPB로 정렬/필터링이 자주 필요한 경우
-- CREATE INDEX IF NOT EXISTS idx_properties_opb ON properties(opb);

-- ============================================
-- 확인 쿼리
-- ============================================
-- SELECT column_name, data_type, is_nullable, column_default 
-- FROM information_schema.columns 
-- WHERE table_name = 'properties' 
--   AND column_name IN ('opb', 'has_commercial_district_data');
