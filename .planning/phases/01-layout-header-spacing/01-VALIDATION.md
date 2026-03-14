---
phase: 1
slug: layout-header-spacing
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-14
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build (compile check) |
| **Config file** | NPLogic.sln |
| **Quick run command** | `dotnet build NPLogic.sln --no-restore` |
| **Full suite command** | `dotnet build NPLogic.sln` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build NPLogic.sln --no-restore`
- **After every plan wave:** Run `dotnet build NPLogic.sln`
- **Before `/gsd:verify-work`:** Full build must succeed
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | LAYOUT-01, LAYOUT-03, SPC-01, SPC-02 | build + manual | `dotnet build NPLogic.sln --no-restore` | N/A | ⬜ pending |
| 1-01-02 | 01 | 1 | HDR-01, HDR-02 | build + manual | `dotnet build NPLogic.sln --no-restore` | N/A | ⬜ pending |
| 1-01-03 | 01 | 1 | LAYOUT-02 | manual | Visual inspection | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This phase modifies XAML only — build success confirms no syntax/binding errors.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DesignHeight 통일 확인 | LAYOUT-01 | XAML attribute inspection | grep d:DesignHeight in modified files |
| CardBorder Margin 통일 | LAYOUT-03, SPC-02 | Visual layout change | 앱 실행 후 각 유형별 섹션 간격 확인 |
| CardBorder Padding 통일 | SPC-01 | Visual layout change | 앱 실행 후 섹션 내부 여백 확인 |
| SectionHeader 통일 | HDR-01 | Visual style change | 모든 유형 전환하며 헤더 스타일 일관성 확인 |
| PrimaryBrush 헤더 제거 | HDR-02 | Visual style change | 평가결과/지번별평가 헤더가 이모지+텍스트로 변경 확인 |
| 섹션 순서 유지 | LAYOUT-02 | Regression check | 기존 섹션 순서가 변경되지 않았는지 확인 |

---

## Validation Sign-Off

- [x] All tasks have build verify or manual verification
- [x] Sampling continuity: build check after every task
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
