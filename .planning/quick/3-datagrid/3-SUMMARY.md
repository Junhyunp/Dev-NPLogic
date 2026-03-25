---
phase: quick
plan: 3
subsystem: registry-datagrid-ux
tags: [datagrid, ux, padding, row-move, rank-reset, eulgu-column]
dependency_graph:
  requires: []
  provides: [row-move-commands, rank-reset-commands, expanded-cell-padding]
  affects: [CollateralPropertyView, PropertyDetailViewModel]
tech_stack:
  added: []
  patterns: [ObservableCollection.Move, RelayCommand-sync-void, RelayCommand-async-db-save]
key_files:
  created: []
  modified:
    - src/NPLogic.App/Views/CollateralPropertyView.xaml
    - src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs
decisions:
  - Move commands are synchronous (void) - DB save deferred to existing Save button
  - ResetRankNumbers commands are async and save immediately to DB (rank is critical data)
metrics:
  duration: 5min
  completed: "2026-03-25T17:13:33Z"
---

# Quick Task 3: Registry DataGrid UX Improvements Summary

Cell padding expanded across 3 DataGrid styles, row move (up/down) and rank reset buttons added to gapgu/eulgu sections, eulgu column renamed from "dambo-jongryu" to "pidambo-chaemu".

## Changes Made

### Task 1: Cell Padding + Column Name + Button XAML (512491a)

| Change | Before | After |
|--------|--------|-------|
| ColumnHeader Padding | 12,6 | 16,8 |
| Cell Padding | 10,4 | 16,6 |
| TextBlock Padding | (none) | 4,0 |
| Eulgu column header | "dambo-jongryu" | "pidambo-chaemu" |

Added to gapgu/eulgu subsection headers:
- "Reset Rank" button (FormatListNumbered icon) bound to ResetGapgu/EulguRankNumbersCommand

Added to gapgu/eulgu button areas:
- ArrowUp / ArrowDown buttons bound to MoveGapgu/EulguRowUp/DownCommand with SelectedItem parameter

### Task 2: ViewModel Row Move + Rank Reset Commands (d73a398)

6 new RelayCommand methods in PropertyDetailViewModel:

| Command | Type | Behavior |
|---------|------|----------|
| MoveGapguRowUp | sync void | ObservableCollection.Move + SortIndex recalc |
| MoveGapguRowDown | sync void | ObservableCollection.Move + SortIndex recalc |
| MoveEulguRowUp | sync void | ObservableCollection.Move + SortIndex recalc |
| MoveEulguRowDown | sync void | ObservableCollection.Move + SortIndex recalc |
| ResetGapguRankNumbers | async Task | RankNo=1,2,3... + SortIndex + DB save per row |
| ResetEulguRankNumbers | async Task | RankNo=1,2,3... + SortIndex + DB save per row |

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- `dotnet build NPLogic.sln` compiles with zero code errors (only MSB3027 file-lock warnings from running VS instance)
- All XAML bindings reference correct Command names matching [RelayCommand]-generated properties

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 512491a | XAML: padding, buttons, column rename |
| 2 | d73a398 | ViewModel: 6 RelayCommand methods |

## Self-Check: PASSED

- [x] CollateralPropertyView.xaml exists
- [x] PropertyDetailViewModel.cs exists
- [x] 3-SUMMARY.md exists
- [x] Commit 512491a found
- [x] Commit d73a398 found
