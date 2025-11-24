# 주간 레포트 작성

Git 커밋 메시지 기반 주간 레포트 생성 및 WBS 동기화

## 파라미터

```
/report-weekly {월요일날짜}
```

- 월요일날짜: YYMMDD 형식 (예: 251118)
- 자동으로 해당 주 월~일 (7일) 기간 계산
- 프로젝트 루트 디렉토리에서 실행

## 프로세스

### 1. 데이터 수집
- 월요일 날짜로부터 일요일 날짜 자동 계산 (+6일)
- Git 로그: `git log --since="YYYY-MM-DD" --until="YYYY-MM-DD" --pretty=format:"%h|%ad|%s" --date=short`
- `manager/plan/WBS_*.csv` 읽기
- `manager/plan/mapping.txt` 읽기 (배포 URL, 노션 WBS ID)

### 2. 완료 항목 추출
커밋 메시지 분석하여 완료된 기능 리스트업

### 3. WBS 업데이트
- CSV에서 완료된 작업ID 상태 변경: "대기" → "완료"
- 파일 상단에 주석 추가: `# {YYMMDD} 업데이트: {작업ID} 완료`

### 4. 노션 동기화
- mapping.txt에서 노션 DB ID 추출
- MCP로 완료된 작업들 상태 업데이트

### 5. 레포트 작성

**형식:**
```markdown
# {프로젝트명} 주간 레포트 (YYMMDD-YYMMDD)

## 📊 진행률
- 전체: {완료}/{전체} ({백분율}%)
- 이번 주: {이번 주 완료}개

## ✅ 완료

- [x] {기능명}
  - 접근: {URL 또는 경로}
  - 커밋: {해시}

## 🔄 WBS 변경
- {작업ID} 완료

## 📝 다음 주
- {다음 주차 WBS 작업들}
```

**저장**: `manager/report/{월요일}-{일요일}_report.md`

## 접근 경로 형식
- 웹: `https://example.com/path`
- 앱: `메뉴 > 화면명`
- API: `POST /api/endpoint`
- 기타: 파일 경로 또는 위치

