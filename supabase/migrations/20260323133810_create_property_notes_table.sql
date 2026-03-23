-- property_notes: 물건별 탭별 비고 (1 property + 1 tab = 1 note)
CREATE TABLE IF NOT EXISTS property_notes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES properties(id) ON DELETE CASCADE,
  tab_name TEXT NOT NULL,
  note_text TEXT NOT NULL DEFAULT '',
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_property_notes_property_tab UNIQUE (property_id, tab_name)
);

-- 인덱스: property_id로 전체 비고 조회 최적화
CREATE INDEX IF NOT EXISTS idx_property_notes_property_id ON property_notes(property_id);

-- RLS 활성화
ALTER TABLE property_notes ENABLE ROW LEVEL SECURITY;

-- RLS 정책: 인증된 사용자 전체 접근 (기존 properties 테이블과 동일한 패턴)
CREATE POLICY "Authenticated users can read property_notes"
  ON property_notes FOR SELECT
  TO authenticated
  USING (true);

CREATE POLICY "Authenticated users can insert property_notes"
  ON property_notes FOR INSERT
  TO authenticated
  WITH CHECK (true);

CREATE POLICY "Authenticated users can update property_notes"
  ON property_notes FOR UPDATE
  TO authenticated
  USING (true)
  WITH CHECK (true);

CREATE POLICY "Authenticated users can delete property_notes"
  ON property_notes FOR DELETE
  TO authenticated
  USING (true);
