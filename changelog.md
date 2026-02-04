# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-04

#### 대시보드 물건 누락(초기 페이지 스킵) 문제 해결

**문제점**
- 프로그램 로딩 중 `SelectedProjectId` 변경이 `RefreshDataAsync`를 트리거
- Legacy 경로의 `LoadDashboardPropertiesAsync`가 서버 페이지네이션 결과를 덮어써 초기 페이지가 누락됨

**해결 방법**
- 프로그램 로딩 중 `SelectedProjectId` 변경에 따른 자동 새로고침을 억제하는 보호 플래그 추가
- 서버 페이지네이션 결과가 덮어써지지 않도록 경로 충돌 방지

**변경된 파일**
- `src/NPLogic.App/ViewModels/DashboardViewModel.cs`