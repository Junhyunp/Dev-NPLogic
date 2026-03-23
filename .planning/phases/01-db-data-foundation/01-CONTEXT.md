# Phase 1: DB 및 데이터 기반 - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Supabase에 property_notes 테이블을 생성하고, C# 모델(PropertyNote) + Repository(PropertyNoteRepository)를 구현한다. 물건별+탭별 비고를 CRUD할 수 있는 데이터 기반을 마련한다.

</domain>

<decisions>
## Implementation Decisions

### DB 테이블
- 테이블명: `property_notes`
- 컬럼: property_id (UUID, FK → properties), tab_name (TEXT), note_text (TEXT), updated_at (TIMESTAMPTZ)
- 유니크 제약: property_id + tab_name (물건별+탭별 1개 비고)
- RLS: 기존 properties 테이블과 동일한 정책 적용

### C# 모델
- `PropertyNote` 클래스: NPLogic.Core.Models에 위치
- Postgrest 어트리뷰트로 테이블/컬럼 매핑

### Repository
- `PropertyNoteRepository`: NPLogic.Data.Repositories에 위치
- 메서드: GetByPropertyIdAsync(Guid), GetByPropertyAndTabAsync(Guid, string), UpsertAsync(PropertyNote), DeleteAsync(Guid)
- 기존 Repository 패턴(SupabaseService 주입) 따름

### tab_name 규칙
- 영문 소문자 snake_case: home, borrower_overview, loan, collateral_property, senior_rights, evaluation, auction_schedule, interim, registry, rights_analysis, basic_data, qa_summary, cashflow_summary, npv_comparison, closing

### Claude's Discretion
- RLS 정책 세부 구현
- Repository 에러 핸들링 패턴
- DI 등록 방식 (App.xaml.cs)

</decisions>

<code_context>
## Existing Code Insights

### 기존 패턴 참고
- Repository 예: `src/NPLogic.Data/Repositories/PropertyRepository.cs`
- Model 예: `src/NPLogic.Core/Models/Property.cs`
- Supabase 연동: `src/NPLogic.Data/Services/SupabaseService.cs`
- DI 등록: `src/NPLogic.App/App.xaml.cs`

### Supabase Project
- Project ID: nlddampvgxamaukflqhd
- Migration은 Supabase Dashboard 또는 MCP로 적용

</code_context>

---

*Phase: 01-db-data-foundation*
*Context gathered: 2026-03-23*
