---
phase: quick-2-pdf
plan: 01
subsystem: registry-image-viewer
tags: [wpf, popup, registry, image-viewer, refactor]
dependency_graph:
  requires: []
  provides: [RegistryImagePopupWindow]
  affects: [CollateralPropertyView, PropertyDetailViewModel]
tech_stack:
  added: []
  patterns: [popup-window, code-behind-image-loading]
key_files:
  created:
    - src/NPLogic.App/Views/RegistryImagePopupWindow.xaml
    - src/NPLogic.App/Views/RegistryImagePopupWindow.xaml.cs
  modified:
    - src/NPLogic.App/Views/CollateralPropertyView.xaml
    - src/NPLogic.App/ViewModels/PropertyDetailViewModel.cs
decisions:
  - MapPopupWindow 패턴을 따라 팝업 Window 생성 (일관된 UI 패턴)
  - ConvertBase64ToBitmapImage를 팝업 code-behind에 복제 (ViewModel에서 완전 분리)
  - 비모달 Show()로 메인 창과 동시 참조 가능
metrics:
  duration: 4min
  completed: "2026-03-25T15:09:23Z"
  tasks_completed: 2
  tasks_total: 2
  files_created: 2
  files_modified: 2
---

# Quick Task 2: 등기부등본 PDF 이미지 뷰어 팝업 전환 Summary

등기부등본 이미지 뷰어를 CollateralPropertyView 인라인 패널에서 독립 팝업 창(RegistryImagePopupWindow)으로 분리하여 화면 공간 효율성과 동시 참조 편의성 개선

## Changes Made

### Task 1: RegistryImagePopupWindow 생성 + 인라인 뷰어 제거 (e1aede9)

- **RegistryImagePopupWindow.xaml** 생성: MapPopupWindow 패턴 준수, PrimaryBrush 헤더(FileDocument 아이콘 + 닫기 버튼), BlueGray100Brush 물건지 선택 ComboBox, ScrollViewer 내 이미지 목록, 이미지 없음 안내 TextBlock
- **RegistryImagePopupWindow.xaml.cs** 생성: 생성자에서 RegistryRun 목록 수신, RunComboBox_SelectionChanged에서 Base64 이미지 변환 및 표시, ConvertBase64ToBitmapImage 메서드 포함
- **CollateralPropertyView.xaml** 수정: 인라인 등기부등본 이미지 뷰어 패널(약 50행) 전면 제거, 등기부등본 버튼은 유지

### Task 2: ViewModel 커맨드를 팝업 열기로 변경 (fea8610)

- **ToggleRegistryImagePanel()** 수정: `IsRegistryImageExpanded` 토글 로직 제거, 대신 `RegistryImagePopupWindow` 인스턴스를 생성하여 비모달(`Show()`)로 표시
- **불필요 속성 제거**: `_isRegistryImageExpanded`, `_selectedImageRun`, `_registryDocumentImages` (3개 ObservableProperty)
- **불필요 메서드 제거**: `OnSelectedImageRunChanged` partial method, `ConvertBase64ToBitmapImage` static method
- `_registryRuns`, `_selectedRegistryRun`은 DataGrid 드롭다운에서 사용하므로 유지

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- `dotnet build NPLogic.sln` -- 컴파일 에러(CS*) 없음 (파일 잠금 MSB3027은 VS 실행 중 환경 이슈)
- `IsRegistryImageExpanded`, `SelectedImageRun`, `RegistryDocumentImages` 참조 전체 검색 결과 0건 확인

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | e1aede9 | feat(quick-2-pdf): RegistryImagePopupWindow 생성 + 인라인 뷰어 제거 |
| 2 | fea8610 | refactor(quick-2-pdf): ViewModel 커맨드를 팝업 열기로 변경 |

## Self-Check: PASSED

- [x] RegistryImagePopupWindow.xaml created
- [x] RegistryImagePopupWindow.xaml.cs created
- [x] Inline viewer removed from CollateralPropertyView.xaml
- [x] VM properties removed from PropertyDetailViewModel.cs
- [x] Commit e1aede9 exists
- [x] Commit fea8610 exists
