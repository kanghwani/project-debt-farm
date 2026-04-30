# 코드 셀프 체크리스트 — 새 스크립트 작성 전후에 확인

> 코드를 다 짰다고 끝이 아니다. 이 목록을 훑고 나서 커밋한다.

---

## ✍️ 작성 전 — 설계 단계

- [ ] **이 클래스의 이름을 한 문장으로 설명할 수 있나?**
  - "이 클래스는 ___한다" 가 단순하게 완성되면 OK
  - 설명이 길거나 "그리고"가 들어가면 → 분리 고려

- [ ] **어떤 이벤트를 구독하면 되나, 아니면 직접 참조해야 하나?**
  - TimeManager 이벤트로 해결 가능한 건 이벤트를 우선 선택

- [ ] **새 Singleton이 꼭 필요한가?**
  - Inspector SerializeField로 연결하면 Singleton 없이도 해결되는 경우가 많다

---

## 🔍 작성 후 — 코드 리뷰

### 기본 품질

- [ ] `FindFirstObjectByType` 을 `Start()`에서 쓰고 있지 않은가?
  - 있다면 → Inspector SerializeField 또는 이벤트로 교체 고려
- [ ] 이벤트 구독을 `OnEnable`에서 하고, 해제를 `OnDisable`에서 하고 있나?
  - `Start`에서 구독하면 오브젝트 비활성 후 재활성 시 이중 구독 발생
- [ ] Null 체크가 필요한 곳에 있는가?
  - Inspector에서 연결 안 했을 때 NullReferenceException이 터지면 디버그 힘들다
  - `if (x == null) { Debug.LogError("..."); return; }` 패턴 사용

### 싱글톤 패턴 확인

```csharp
// 아래 패턴이 정확하게 지켜지고 있나?
private void Awake()
{
    if (Instance == null) Instance = this;
    else Destroy(gameObject); // ← else 빠지면 씬 재로드 시 Instance가 null이 됨
}
```

### 이벤트 메모리 누수 확인

```csharp
// OnEnable에서 구독 ↔ OnDisable에서 해제 짝이 맞는가?
private void OnEnable()  { SomeManager.OnEvent += MyMethod; }
private void OnDisable() { SomeManager.OnEvent -= MyMethod; } // ← 이게 없으면 메모리 누수
```

---

## 🎮 게임 로직 확인

### 타이밍 바 사용 시

- [ ] `StartTimingAction()` 호출 전에 `isPlayerBusy` 체크를 하고 있나?
- [ ] 콜백 끝에서 `isPlayerBusy = false` 를 반드시 되돌리고 있나?
- [ ] `TimingBarUI.Instance == null` 인 경우 방어 코드가 있나?

### 농장 데이터 수정 시

- [ ] `FarmingManager.farmData` 딕셔너리를 직접 수정할 때 `activeCrops` 리스트도 같이 관리하고 있나?
  - 추가: `activeCrops.Add(pos)`
  - 제거: `farm.RemoveTileData(pos)` 사용 (직접 Remove 대신)
- [ ] `UpdateTileVisual()` 로 시각적 갱신을 하고 있나? (타일맵과 데이터가 따로 놀지 않도록)

### 인벤토리 골드 변경 시

- [ ] `gold += amount` 직접 수정 대신 `AddGold()` / `SpendGold()` 를 통하고 있나?
  - 직접 수정하면 `OnInventoryChanged` 이벤트가 안 울려서 UI가 안 갱신됨

---

## 🏷️ 네이밍 규칙 (이 프로젝트 기준)

| 종류 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `FarmingManager` |
| public 변수 | camelCase | `currentWeight` |
| private 변수 | camelCase | `elapsedTime` |
| 이벤트 | `On` + PascalCase | `OnDayChanged` |
| 코루틴 | `~Routine` | `ChatRoutine` |
| static 인스턴스 | `Instance` | `TimeManager.Instance` |
| bool 변수 | is/has/can 접두사 | `isWatered`, `hasFertilizer` |

---

## 📦 씬에 배치할 때 체크

새 스크립트를 오브젝트에 붙일 때 인스펙터 확인:

- [ ] `[Header]` 구분대로 필요한 레퍼런스가 모두 연결되어 있나?
- [ ] Singleton 클래스는 씬에 **단 1개**만 존재하나?
- [ ] DataManager는 DontDestroyOnLoad라서 씬 전환 후 2개가 될 수 있음 → 주의

---

## 💾 커밋 전 최종 확인

- [ ] 임시 디버그 `Debug.Log` 중 남겨야 할 것과 지워야 할 것 구분했나?
- [ ] `Time.timeScale = 0` 이 코드 어딘가에 남아있지 않나? (게임 정지 버그)
- [ ] Inspector에서만 연결되는 레퍼런스에 `[SerializeField]` 가 붙어있나?
  - `public` 으로 공개하면 다른 스크립트에서 실수로 건드릴 수 있음
