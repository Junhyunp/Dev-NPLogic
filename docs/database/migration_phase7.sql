-- Phase 7 DB 마이그레이션 스크립트
-- 생성일: 2024-12-17
-- 설명: 경매 일정에 인터링/상계회수 컬럼 추가

-- ============================================
-- 7.2 경매 일정에 인터링/상계회수 컬럼 추가
-- ============================================
ALTER TABLE auction_schedules 
ADD COLUMN IF NOT EXISTS interim_principal_offset DECIMAL(18, 2) DEFAULT 0;

ALTER TABLE auction_schedules 
ADD COLUMN IF NOT EXISTS interim_principal_recovery DECIMAL(18, 2) DEFAULT 0;

ALTER TABLE auction_schedules 
ADD COLUMN IF NOT EXISTS interim_interest_offset DECIMAL(18, 2) DEFAULT 0;

ALTER TABLE auction_schedules 
ADD COLUMN IF NOT EXISTS interim_interest_recovery DECIMAL(18, 2) DEFAULT 0;

COMMENT ON COLUMN auction_schedules.interim_principal_offset IS '인터링 원금상계';
COMMENT ON COLUMN auction_schedules.interim_principal_recovery IS '인터링 원금회수';
COMMENT ON COLUMN auction_schedules.interim_interest_offset IS '인터링 이자상계';
COMMENT ON COLUMN auction_schedules.interim_interest_recovery IS '인터링 이자회수';

-- ============================================
-- 확인 쿼리
-- ============================================
-- SELECT column_name, data_type, is_nullable, column_default 
-- FROM information_schema.columns 
-- WHERE table_name = 'auction_schedules' 
--   AND column_name LIKE 'interim_%';
