# 데모 버전 로드맵 — Project Debt Farm

> **데모 목표:** 플레이어가 처음 실행해서 게임 오버 또는 7일 클리어까지
> 혼자서 경험할 수 있는 완결된 루프를 만든다.

---

## 데모 범위 정의

| 구분 | 포함 | 제외 |
|------|------|------|
| 씬 | 메인 게임 씬 1개 + 메인 메뉴 | 다음 챕터, 맵 확장 |
| 작물 | 무, 감자, 토마토 (기존 3종) | 호박, 인삼 |
| 하루 루프 | 농사 → 판매 → 빚 정산 → 다음날 | 이벤트 랜덤 날씨 |
| 저장 | 날짜·골드·연체 (PlayerPrefs) | 농장 배치 저장 |
| 게임 오버 | 3 Strike → 게임 오버 화면 | 랭킹, 클라우드 저장 |

---

## 마일스톤

### 🔴 M0 — 지금 당장 고쳐야 하는 크리티컬 버그
> 없으면 데모가 불가능한 것들

- [ ] `DebtManager.GameOver()` — 현재 `Time.timeScale = 0` 으로만 멈춤
  - 게임 오버 UI 패널 연결 + 재시작 버튼 구현
- [ ] `SaveManager.LoadAllData()` 미구현 — 앱 껐다 켜면 1일 1000G로 초기화
  - `PlayerPrefs`에서 CurrentDay·Gold·Strikes 불러오는 함수 작성
- [ ] 씬이 `SampleScene` 이름으로 Build Settings에 등록되어 있을 수 있음
  - File → Build Settings에서 `_Project/Scenes/GameScene` 으로 재등록 확인

---

### 🟡 M1 — 핵심 루프 완성 (플레이 가능한 첫 버전)

**농사 루프**
- [ ] 수레(Trolley) 기능 연결 — `PlayerTrolleyDriver.cs` 씬에 배치·테스트
- [ ] ShippingBox 판매 결과 UI — 몇 G 판매했는지 요약 팝업
- [ ] 작물이 죽었을 때(Dead) 플로팅 텍스트 "시들었다..." 표시

**빚 시스템**
- [ ] Strike 3회 시 게임 오버 화면 (M0와 연계)
- [ ] 7일 완주 시 엔딩 화면 (간단한 텍스트라도)

**저장/불러오기**
- [ ] `SaveManager.LoadAllData()` 구현 (M0와 연계)
- [ ] 게임 시작 시 자동 로드

---

### 🟢 M2 — 게임 느낌 다듬기 (데모 퀄리티)

**연출 (Juice)**
- [ ] 밤 10시 사채업자 등장 시 화면 흔들림 or 음악 전환
- [ ] 빚 정산 성공 시 축하 효과 (골드 파티클 or DOTween 팡파르)
- [ ] 타이밍 바 PERFECT 시 화면 플래시 효과

**UI 마무리**
- [ ] 오늘의 목표 금액 HUD에 항상 표시 (현재 UiManager에 debtText 있음)
- [ ] 게임 일시정지 (ESC) 메뉴
- [ ] 메인 메뉴 → 게임 씬 전환 (`MainMenuManager.cs` 연결)

**밸런스**
- [ ] `TimeManager.realSecondsPerDay` 조정 — 현재 120초, 데모는 180~240초 권장
- [ ] `DebtManager.dailyDebtGoals` 7일 수치 검토
- [ ] 작물 성장 시간(`requireGrowTime`) 게임 시간 대비 적절한지 확인

---

### 🔵 M3 — 데모 배포 준비

- [ ] Build Settings — Windows/Mac 빌드 테스트
- [ ] 해상도 1920×1080 고정 or 비율 유지 설정
- [ ] 첫 실행 시 튜토리얼 (텍스트 힌트 최소 1개 — "Space로 상호작용")
- [ ] 크래시 없이 게임 오버 → 재시작 → 다시 플레이 가능 확인

---

## 현재 코드에서 확인된 TODO 목록

> 코드에 `// TODO` 주석이 달린 것들. 데모 전에 해결 여부를 결정해야 한다.

| 파일 | 내용 | 우선순위 |
|------|------|---------|
| `DebtManager.cs:87` | 게임 오버 UI + 씬 재로드 | 🔴 M0 |
| `SaveManager.cs` | LoadAllData 없음 | 🔴 M0 |
| `PlayerInventory.cs:133` | AddGold 효과음 | 🟢 M2 |

---

## 완성 체크 기준

데모 완성이란 아래를 모두 만족하는 것이다.

- [ ] 처음 실행한 사람이 아무 설명 없이 1분 안에 첫 수확을 할 수 있다
- [ ] 3 Strike 게임 오버를 경험할 수 있다
- [ ] 앱을 껐다 켜도 진행상황이 유지된다
- [ ] 7일 루프를 끝까지 돌릴 수 있다
- [ ] 빌드 후 에디터 없이 실행 가능하다
