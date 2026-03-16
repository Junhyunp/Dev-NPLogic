---
phase: 2
slug: datagrid-style
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-16
---

# Phase 2 — Validation Strategy

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
| 2-01-01 | 01 | 1 | GRID-01, GRID-02, GRID-03 | build + manual | `dotnet build NPLogic.sln --no-restore` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This phase modifies XAML only — build success confirms no syntax/binding errors.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| AlternatingRowBackground 통일 | GRID-03 | Visual change | 앱 실행 후 실거래가/사례평가/인터림 DataGrid에 번갈아 배경색 확인 |
| BorderThickness 통일 | GRID-03 | Visual change | 탐문 내역/결과 DataGrid 외곽 테두리가 0으로 변경 확인 |
| FontSize 통일 확인 | GRID-02 | Regression check | 모든 유형 DataGrid 폰트 크기 일관성 확인 |

---

## Validation Sign-Off

- [x] All tasks have build verify or manual verification
- [x] Sampling continuity: build check after every task
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
