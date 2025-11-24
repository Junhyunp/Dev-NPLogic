# 커밋 (빌드 제외)

빌드 없이 커밋만 수행.

## 커밋 프로세스

### 1. 파일 확인
```bash
git add .
git diff --cached --name-status           # 전체 변경
git diff --cached --diff-filter=A         # 신규 파일 (⚠️ 필수)
```

### 2. 커밋 메시지 작성
**형식:**
```
[제목]

주요 변경:
- [카테고리]: 파일 - 설명

신규 파일:
- 파일: 용도 (⚠️ A 파일 전부)

빌드: N/A
```

**검증:**
- [ ] 신규 파일(A) 모두 포함?
- [ ] 대량 변경 파일 설명?

### 3. 실행
```bash
git commit -m "메시지"
```

## 예시
**입력:**
```
A  src/lib/orchestrator.ts
A  src/app/admin/page.tsx
M  src/app/api/chat/route.ts (+200줄)
```

**출력:**
```
중앙 제어 및 관리자 시스템 구축

주요 변경:
- [중앙 제어]: lib/orchestrator.ts - Gemini 기반 AI 선택
- [관리자]: admin/page.tsx - 대시보드 UI
- [API]: api/chat/route.ts - orchestrator 통합 (+200줄)

신규 파일:
- lib/orchestrator.ts: 중앙 제어 AI
- admin/page.tsx: 관리자 페이지

빌드: N/A
```

