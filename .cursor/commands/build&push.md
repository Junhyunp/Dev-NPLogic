# 빌드 & 에러수정 & 커밋 & 푸시

## 작업 흐름
1. 빌드 명령 실행 (예: npm run build, ./gradlew build, dotnet build 등)
2. 에러 있으면 수정 후 재빌드 (반복)
3. 성공하면 아래 커밋 프로세스 실행
4. 푸시

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

빌드: ✅
```

**검증:**
- [ ] 신규 파일(A) 모두 포함?
- [ ] 대량 변경 파일 설명?

### 3. 실행
```bash
git commit -m "메시지"
git push
```