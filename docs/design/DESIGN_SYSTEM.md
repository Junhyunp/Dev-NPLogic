# NPLogic UI/UX 디자인 시스템

## 디자인 철학

**컨셉**: 차분하고 딱딱한 엑셀 같은 느낌
- 업무용 애플리케이션에 최적화
- 데이터 중심의 명확한 레이아웃
- 불필요한 장식 최소화
- 높은 가독성과 직관성

---

## 색상 팔레트

### Primary Colors (주요 색상)

#### Deep Navy (딥네이비 - 베이스)
```
Primary: #1A2332
Primary-Light: #2A3442
Primary-Dark: #0A1322
```
**사용처**: 헤더, 사이드바, 주요 버튼

#### Blue Gray (블루그레이 - 표)
```
Table-Header: #B0C4DE
Table-Hover: #C8D6E8
Table-Selected: #98B2D1
Table-Border: #8FA8C5
```
**사용처**: 데이터 그리드, 테이블 헤더

### Secondary Colors (보조 색상)

#### Neutral
```
Background: #F5F7FA
Surface: #FFFFFF
Border: #E1E4E8
Divider: #D0D5DD
Text-Primary: #1A1D23
Text-Secondary: #6B7280
Text-Disabled: #9CA3AF
```

#### Semantic Colors (상태 표시)
```
Success: #10B981 (성공, 완료)
Warning: #F59E0B (경고, 대기)
Error: #EF4444 (오류, 실패)
Info: #3B82F6 (정보)
```

### 색상 사용 가이드

| 구분 | 색상 | 용도 |
|------|------|------|
| 배경 | #F5F7FA | 전체 앱 배경 |
| 카드/패널 | #FFFFFF | 컨텐츠 영역 |
| 헤더 | #1A2332 | 상단 헤더, 사이드바 |
| 테이블 헤더 | #B0C4DE | 그리드 헤더 행 |
| 강조 버튼 | #1A2332 | Primary 액션 |
| 일반 버튼 | #6B7280 | Secondary 액션 |
| 링크/하이퍼링크 | #3B82F6 | 클릭 가능한 텍스트 |

---

## 타이포그래피

### 폰트 패밀리

**Primary**: Segoe UI (Windows 기본)
**Fallback**: Malgun Gothic (맑은 고딕)
**Monospace**: Consolas (숫자, 코드)

```xaml
<FontFamily>Segoe UI, Malgun Gothic, sans-serif</FontFamily>
```

### 폰트 크기 스케일

| 레벨 | 크기 | 사용처 |
|------|------|--------|
| H1 | 32px | 페이지 제목 |
| H2 | 24px | 섹션 제목 |
| H3 | 20px | 카드 제목 |
| H4 | 18px | 서브 헤딩 |
| Body | 14px | 본문 텍스트 |
| Small | 12px | 캡션, 힌트 |
| Tiny | 10px | 라벨, 태그 |

### 폰트 굵기

```
Light: 300 (사용 안 함)
Regular: 400 (기본 텍스트)
Medium: 500 (강조)
SemiBold: 600 (제목)
Bold: 700 (강한 강조)
```

### 줄 높이 (Line Height)

```
Tight: 1.2 (제목)
Normal: 1.5 (본문)
Relaxed: 1.8 (긴 텍스트)
```

---

## 간격 시스템 (Spacing)

### 기본 단위: 4px

```
XS: 4px
S: 8px
M: 16px
L: 24px
XL: 32px
XXL: 48px
```

### 컴포넌트 간격

| 요소 | 간격 |
|------|------|
| 컨트롤 간 | 8px (S) |
| 섹션 간 | 24px (L) |
| 페이지 패딩 | 32px (XL) |
| 카드 패딩 | 16px (M) |
| 테이블 셀 패딩 | 8px (S) |

---

## 레이아웃 구조

### 전체 레이아웃

```
┌─────────────────────────────────────────┐
│ Header (높이: 60px)                      │
├─────────┬───────────────────────────────┤
│         │                               │
│ Sidebar │ Main Content Area             │
│ (260px) │                               │
│         │                               │
├─────────┴───────────────────────────────┤
│ Status Bar (높이: 30px)                  │
└─────────────────────────────────────────┘
```

### Header (헤더)
- **높이**: 60px
- **배경**: Deep Navy (#1A2332)
- **구성**:
  - 좌측: 로고 + 앱 이름
  - 우측: 사용자 정보, 알림, 설정 아이콘

### Sidebar (사이드바)
- **너비**: 260px (접힌 상태: 60px)
- **배경**: Deep Navy (#1A2332)
- **구성**:
  - 네비게이션 메뉴 (아이콘 + 텍스트)
  - 역할별 메뉴 구성 (PM, 평가자, 관리자)
  - 하단: 접기/펴기 버튼

### Main Content Area
- **배경**: #F5F7FA
- **패딩**: 32px
- **최대 너비**: 제한 없음 (반응형)

### Status Bar (상태바)
- **높이**: 30px
- **배경**: #E1E4E8
- **구성**: 상태 메시지, 진행 중인 작업 표시

---

## 공통 컴포넌트

### 1. 버튼 (Button)

#### Primary Button
```
배경: #1A2332
텍스트: #FFFFFF
높이: 36px
패딩: 12px 24px
둥근 모서리: 4px
Hover: #2A3442
Active: #0A1322
```

#### Secondary Button
```
배경: #FFFFFF
텍스트: #1A2332
테두리: 1px solid #D0D5DD
높이: 36px
패딩: 12px 24px
둥근 모서리: 4px
Hover: #F5F7FA
```

#### Danger Button
```
배경: #EF4444
텍스트: #FFFFFF
높이: 36px
패딩: 12px 24px
둥근 모서리: 4px
Hover: #DC2626
```

#### Icon Button
```
크기: 36x36px
배경: 투명
Hover: #F5F7FA
아이콘 크기: 20x20px
```

### 2. 입력 필드 (Input)

#### Text Input
```
높이: 36px
패딩: 8px 12px
배경: #FFFFFF
테두리: 1px solid #D0D5DD
둥근 모서리: 4px
Focus: 테두리 #1A2332 (2px)
```

#### Textarea
```
최소 높이: 80px
패딩: 12px
나머지: Text Input과 동일
```

#### Select (Dropdown)
```
높이: 36px
화살표 아이콘: 우측
나머지: Text Input과 동일
```

#### Checkbox & Radio
```
크기: 20x20px
테두리: 1px solid #D0D5DD
Checked: 배경 #1A2332
```

### 3. 데이터 그리드 (DataGrid)

#### Table Header
```
배경: #B0C4DE
텍스트: #1A1D23
폰트: SemiBold
높이: 40px
패딩: 8px 12px
```

#### Table Row
```
배경: #FFFFFF
홀수 행: #FFFFFF
짝수 행: #F9FAFB (선택적)
높이: 36px
패딩: 8px 12px
Hover: #C8D6E8
Selected: #98B2D1
```

#### Table Cell
```
텍스트 정렬: 좌측 (텍스트), 우측 (숫자)
패딩: 8px 12px
테두리: 1px solid #E1E4E8
```

### 4. 카드 (Card)

```
배경: #FFFFFF
테두리: 1px solid #E1E4E8
둥근 모서리: 8px
패딩: 16px
그림자: 0 1px 3px rgba(0,0,0,0.1)
```

### 5. 모달 (Modal/Dialog)

```
오버레이: rgba(0,0,0,0.5)
배경: #FFFFFF
둥근 모서리: 8px
패딩: 24px
그림자: 0 4px 12px rgba(0,0,0,0.15)
최소 너비: 400px
최대 너비: 800px
```

#### Modal Header
```
폰트 크기: 20px (H3)
하단 구분선: 1px solid #E1E4E8
패딩 하단: 16px
```

#### Modal Footer
```
상단 구분선: 1px solid #E1E4E8
패딩 상단: 16px
버튼 정렬: 우측
버튼 간격: 8px
```

### 6. 탭 (Tab)

```
Tab Header:
  배경: #F5F7FA
  높이: 48px
  패딩: 12px 24px
  
Active Tab:
  배경: #FFFFFF
  하단 테두리: 3px solid #1A2332
  텍스트: #1A2332 (SemiBold)
  
Inactive Tab:
  텍스트: #6B7280
  Hover: 배경 #FFFFFF
```

### 7. 프로그레스바 (ProgressBar)

```
높이: 8px
배경: #E1E4E8
진행 바: #1A2332
둥근 모서리: 4px
```

### 8. 뱃지/태그 (Badge)

```
높이: 24px
패딩: 4px 8px
둥근 모서리: 4px
폰트 크기: 12px

Success: 배경 #D1FAE5, 텍스트 #065F46
Warning: 배경 #FEF3C7, 텍스트 #92400E
Error: 배경 #FEE2E2, 텍스트 #991B1B
Info: 배경 #DBEAFE, 텍스트 #1E40AF
```

### 9. 토스트/알림 (Toast Notification)

```
위치: 우측 상단
너비: 360px
배경: #FFFFFF
테두리: 1px solid #E1E4E8
그림자: 0 4px 12px rgba(0,0,0,0.15)
둥근 모서리: 8px
패딩: 16px
아이콘: 좌측 (Success/Warning/Error/Info)
```

### 10. 툴팁 (Tooltip)

```
배경: #1A2332
텍스트: #FFFFFF
폰트 크기: 12px
패딩: 8px 12px
둥근 모서리: 4px
화살표: 있음
```

---

## 아이콘

### 아이콘 라이브러리
- **Material Design Icons** (MaterialDesignThemes에 포함)
- **통일된 스타일**: Outlined (선형) 스타일 사용

### 아이콘 크기
```
Small: 16x16px (버튼 내, 텍스트 옆)
Medium: 24x24px (기본)
Large: 32x32px (헤더, 중요 액션)
XLarge: 48x48px (빈 상태, 대형 아이콘)
```

### 주요 아이콘 목록
- 홈: Home
- 물건 목록: ViewList
- 물건 상세: FileDocument
- 업로드: Upload
- 다운로드: Download
- 설정: Cog
- 사용자: Account
- 로그아웃: Logout
- 검색: Magnify
- 필터: Filter
- 정렬: Sort
- 추가: Plus
- 수정: Pencil
- 삭제: Delete
- 저장: ContentSave
- 닫기: Close
- 확장: ChevronDown
- 접기: ChevronUp

---

## 반응형 디자인

### 브레이크포인트
윈도우 데스크톱 앱이므로 반응형보다는 **최소 해상도** 지원

```
최소 너비: 1280px
최소 높이: 720px
권장: 1920x1080 (Full HD)
```

### 동작
- 최소 크기 이하로 줄이면 스크롤바 표시
- Sidebar 접기/펴기로 공간 확보

---

## 애니메이션 & 트랜지션

### 기본 원칙
- **빠르고 부드럽게**: 100-300ms
- **목적성**: 사용자 피드백, 상태 변화 표시
- **과하지 않게**: 업무용 앱 특성 고려

### 주요 애니메이션

#### Fade In/Out
```
Duration: 200ms
Easing: EaseOut
```

#### Slide In/Out (Sidebar, Modal)
```
Duration: 250ms
Easing: EaseInOut
```

#### Button Hover/Press
```
Duration: 150ms
Easing: Linear
```

#### Loading Spinner
```
Rotation: 360deg
Duration: 1000ms
Loop: Infinite
```

---

## 상태 표시

### 빈 상태 (Empty State)
```
중앙 정렬
아이콘: 48x48px (회색)
제목: 18px (SemiBold)
설명: 14px (Regular)
액션 버튼: Primary Button
```

### 로딩 상태 (Loading)
```
오버레이: rgba(255,255,255,0.8)
스피너: 중앙 정렬, 40x40px
텍스트: "로딩 중..." (선택적)
```

### 에러 상태 (Error)
```
배경: #FEE2E2
텍스트: #991B1B
아이콘: AlertCircle (Error 색상)
액션: "다시 시도" 버튼
```

---

## 접근성 (Accessibility)

### 색상 대비
- 텍스트-배경 대비: 최소 4.5:1 (WCAG AA)
- 중요 요소: 7:1 이상 (WCAG AAA)

### 키보드 네비게이션
- Tab 순서: 논리적 흐름
- Focus 표시: 명확한 아웃라인
- Shortcut 키: 주요 기능에 적용

### 스크린 리더
- AutomationProperties 설정
- 의미 있는 라벨 제공

---

## XAML 리소스 구조

```
Styles/
├── Colors.xaml           # 색상 정의
├── Typography.xaml       # 폰트 스타일
├── Controls.xaml         # 기본 컨트롤 스타일
├── Buttons.xaml          # 버튼 스타일
├── Inputs.xaml           # 입력 필드 스타일
├── DataGrid.xaml         # 그리드 스타일
└── Layout.xaml           # 레이아웃 템플릿
```

---

## 다음 단계

1. XAML 리소스 파일 생성
2. 공통 컴포넌트 구현
3. 샘플 화면으로 테스트
4. 디자인 가이드 문서와 함께 사용

---

## 참고 이미지 (엑셀 스타일 참고)

요구사항 기획서 `비핵심 프로그램화_v6-1.xlsx`의 화면 레이아웃을 참고하여 엑셀과 유사한 느낌 유지

