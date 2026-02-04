# Changelog

모든 주요 변경사항을 이 파일에 기록합니다.

## [Unreleased]

### 2026-02-04

#### 대시보드 물건 누락(초기 페이지 스킵) 문제 해결

**문제점**
- 프로그램을 선택하면 서버 페이지네이션으로 **1페이지(1~50)**를 정상 로드함
- 그런데 **같은 순간** `SelectedProjectId` 변경이 자동 새로고침(`RefreshDataAsync`)을 다시 호출함
- 이 새로고침이 **구(legacy) 로딩 경로**(`LoadDashboardPropertiesAsync`)를 실행하면서
  방금 받은 **1페이지 데이터를 덮어씀**
- 결과: 화면에는 1페이지가 사라진 것처럼 보이고, 이후 페이지는 정상 정렬됨

**해결 방법**
- 프로그램 로딩 중에는 `SelectedProjectId` 변경으로 인한 **자동 새로고침을 일시 차단**
- 한 번의 로딩 경로만 실행되도록 만들어 **서버 페이지네이션 결과가 덮어쓰이지 않게 함**

**변경된 파일**
- `src/NPLogic.App/ViewModels/DashboardViewModel.cs`