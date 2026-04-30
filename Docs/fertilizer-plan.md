# 계획서: 퀀텀 비료 시스템

> **목표**: 타이밍바(퀄리티) + 비료 + 유행 3단 곱연산으로 작물 가격이 뻥튀기되는 "뽕맛" 구현.
> 발라트로식 콤보 타격감. 10G짜리 무가 1,000G로 변하는 잭팟 + 화면 흔들림 체험.

---

## 0. 설계 원칙

- **ScriptableObject 미사용** → `CropData` 방식 그대로 복제 (`[System.Serializable]` POCO + `DataManager` Inspector 배열).
- **SOLID 준수** → 비료 적용은 `IFarmAction` 신규 구현체로. `GradeCalculator`는 곱연산 체인으로 리팩터링(OCP).
- **기존 `hasFertilizer: bool` 제거** → `List<Fertilizer>` 로 교체 (여러 비료 누적).
- **유기농 프리미엄 제거** — 비료 없을 때 x2 규칙 삭제, 공식 깔끔하게.
- **뼈대 먼저** → Step 1~4까지 데이터+1개 비료 end-to-end 동작 후 잭팟/유행/VFX 확장.

---

## 1. 데이터 모델 (Step 1)

### 1-1. enum (`Data/FertilizerType.cs` 신규)

```csharp
public enum Fertilizer {
    None,
    // Tag
    SpicySauce, SugarCrystal, RainbowOre,
    // Stats & Mult
    PopcornYeast, LonelyTonic, CommunalCompost,
    // Special
    GrowthAccelerator, GamblerLye, GeneModifier, DebtorsTears
}

public enum CropTag { None, Spicy, Sweet, Ornamental }

public enum FertilizerSpecial {
    None,
    LonelyBonus,      // 주변 8칸 비면 x2
    CommunalBonus,    // 주변 같은 작물 1개당 +0.2
    GrowthBoost,      // 성장 50%, 최종 x0.8
    Gambler,          // 0~200% 랜덤
    GeneCopy,         // 상하좌우 태그 복사
    DebtorsCut        // 남은 빚의 5% 추가
}
```

### 1-2. `FertilizerData` (`Data/FertilizerData.cs` — `CropData` 복제)

```csharp
[System.Serializable]
public class FertilizerData {
    [Header("Basic")]
    public Fertilizer type;
    public string displayName;
    public int buyPrice;

    [Header("Effect")]
    public CropTag grantTag = CropTag.None;
    public int flatBonus = 0;
    public float multAdd = 0f;
    public FertilizerSpecial special = FertilizerSpecial.None;

    [Header("Visual")]
    public Color vfxColor = Color.white;
    public Sprite icon;
}
```

### 1-3. `TileData` 확장

```csharp
// 기존 bool hasFertilizer 제거
public List<Fertilizer> appliedFertilizers = new();  // 최대 3개
public List<CropTag> activeTags = new();             // 중복 없이 누적
```

**중복 규칙**: 다른 종류 무제한, **같은 종류는 1개**, **총합 최대 3개** (`ApplyFertilizerAction.CanExecute`에서 강제).

### 1-4. `DataManager` 확장

```csharp
[SerializeField] private FertilizerData[] allFertilizers;
private Dictionary<Fertilizer, FertilizerData> fertDict = new();
public FertilizerData GetFertilizer(Fertilizer t);
```

---

## 2. 가격 공식 리팩터링 (Step 2)

**유기농 프리미엄 제거**. 새 공식:

```
최종가 = (basePrice + Σ flatBonus) × gradeMult × (1 + Σ multAdd + specialMultAdd) × trendMult
```

### 2-1. 새 API

```csharp
public struct HarvestResult {
    public int finalPrice;
    public string gradeLabel;
    public float totalMultiplier;   // 잭팟 티어 판정용
    public int flatBonusTotal;
    public JackpotTier jackpotTier; // None/Small/Big/Mega
}

public enum JackpotTier { None, Small, Big, Mega }

public static class GradeCalculator {
    public static HarvestResult CalculateHarvestResult(
        TileData tile,
        CropData crop,
        Vector3Int cellPos,
        FarmingManager farm,
        DebtManager debt,
        float trendMult);
}
```

### 2-2. 잭팟 티어 판정

```
totalMultiplier = gradeMult × (1 + Σmult) × trendMult
  < 3.0  → None
  3~6    → Small   (화면 살짝 흔들림 + 기본가 텍스트 1.2배)
  6~12   → Big     (강한 흔들림 + 텍스트 펀치 + 골드 파티클)
  12+    → Mega    (슬로모션 0.3s + 카메라 줌 + 스크린 플래시 + 큰 텍스트)
```

### 2-3. 호출부
- `HarvestAction.Execute()` 한 군데만 수정.

---

## 3. 인벤토리 / 도구 (Step 3)

### 3-1. `PlayerInventory`
- `Dictionary<Fertilizer, int> fertilizerBag`
- `Fertilizer equippedFertilizer = Fertilizer.None`
- `AddFertilizer(type, count)`, `ConsumeFertilizer(type)`, `HasFertilizer(type)`

### 3-2. 입력 — **3번 키 = 씨앗과 동일 스타일**
- `PlayerControls.inputactions` → `3` 키 추가.
- 동작 방식: `2`번 씨앗 순환과 동일 — 이미 장착 중이면 다음 비료 종류로 순환, 없으면 첫 번째 비료 장착.
- 장착 상태에서 `Space` → `ApplyFertilizerAction` 발동.

### 3-3. 구매 — **DroneShop 통합**
- 기존 `DroneShop` UI에 비료 탭/섹션 추가 (별도 상점 X).
- 가격은 `FertilizerData.buyPrice` 사용.

---

## 4. IFarmAction: `ApplyFertilizerAction` (Step 4)

`Farming/Actions/ApplyFertilizerAction.cs`

```csharp
public class ApplyFertilizerAction : IFarmAction {
    public bool CanExecute(cellPos, inventory, farm) {
        if (!farm.farmData.TryGetValue(cellPos, out var d)) return false;
        if (d.currentState != Seeded && d.currentState != Harvestable) return false;
        if (inventory.equippedFertilizer == Fertilizer.None) return false;
        if (!inventory.HasFertilizer(inventory.equippedFertilizer)) return false;
        if (d.appliedFertilizers.Contains(inventory.equippedFertilizer)) return false; // 같은 종류 1개
        if (d.appliedFertilizers.Count >= 3) return false;                               // 총 3개 상한
        return true;
    }

    public void Execute(...) {
        // 1. 인벤토리 차감
        // 2. tile.appliedFertilizers.Add(type)
        // 3. grantTag 있으면 tile.activeTags에 중복 없이 추가
        // 4. SpawnFertilizerVFX(cellPos, vfxColor)
        // 5. showText("비료 적용!", vfxColor)
    }
}
```

`FarmingInteraction` 의 action 리스트에 추가 — 여기만 수정(OCP).

**존버 허용**: `Harvestable` 상태에서도 비료 추가 가능 → "유행 올 때까지 안 수확" 전략 성립.

---

## 5. 유행 시스템 스텁 (Step 5)

`Core/TrendManager.cs` (MonoBehaviour 싱글톤, 씬 종속):

```csharp
public class TrendManager : MonoBehaviour {
    public static TrendManager Instance;
    public CropTag todayTag;
    public float trendMultiplier = 3f;

    void OnEnable()  => TimeManager.OnDayChanged += RollTrend;
    void OnDisable() => TimeManager.OnDayChanged -= RollTrend;

    public float GetMultiplierFor(List<CropTag> tags) =>
        tags.Contains(todayTag) ? trendMultiplier : 1f;

    void RollTrend() { /* 랜덤 태그 */ }
}
```

스텁: Inspector로 `todayTag` 고정 → 이후 `ClockUI`/전용 UI 노출.

---

## 6. 시각 피드백 (Step 6)

### 6-1. 타일 파티클 (평상시)
- `FarmingManager.SpawnFertilizerVFX(cellPos, Color)` — ParticleSystem prefab 1개 재활용, 색만 교체.
- 동시 표시 최대 3개(상한이 3이라 자연스럽게 해결).
- `appliedFertilizers` 비면 VFX 제거.

### 6-2. 스마트 커서 툴팁
- `UI/CropTooltipUI.cs` (월드→스크린 좌표 추적).
- 커서가 `Seeded`/`Harvestable` 위 → 태그 + 예상 배수 + 플랫보너스 표시.
- 포맷: `[감자] 🔥매운맛 / x2.0 / +50G`

### 6-3. 잭팟 연출 — **티어별 차등**

| 티어 | 배율 | 연출 |
|------|------|------|
| None | <3x | 기본 FloatingText |
| Small | 3~6x | Impulse 가벼움 + 텍스트 1.2배 + 색 변화 |
| Big | 6~12x | Impulse 강함 + 텍스트 스케일 펀치 + 골드 파티클 버스트 |
| Mega | 12x+ | 슬로모션 0.3s + 카메라 줌인 + 스크린 플래시 + 큰 텍스트 + 사운드 |

- Cinemachine Impulse 또는 DOTween 카메라 `DOShakePosition`.
- 슬로모션: `Time.timeScale` 조작(DOTween sequence로 복구).

---

## 7. 구현 순서 (뼈대 우선)

| Step | 산출물 | 검증 |
|------|--------|------|
| 1 | enum + `FertilizerData` + `DataManager` 확장 + `TileData` 필드 교체 | `GetFertilizer()` null 아님 로그 |
| 2 | `GradeCalculator` 리팩터링 (유기농 프리미엄 제거), `HarvestAction` 호출부 수정 | 비료 없이 기존 수확 정상 |
| 3 | `PlayerInventory` 비료 가방 + 3번 키 순환 + DroneShop 비료 탭 | Debug 치트로 비료 추가 → 장착 로그 |
| 4 | `ApplyFertilizerAction` + FarmingInteraction 등록 | 뻥튀기 효모만 작동 → 수확 시 +50G |
| 5 | `TrendManager` + trendMult 체인 연결 | Inspector로 태그 고정, 매운맛+유행매운맛 x3 확인 |
| 6 | VFX + 툴팁 + 잭팟 티어 연출 | 눈 검증 |
| 7 | 나머지 9종 비료 (`FertilizerSpecial` switch) | 수동 확인 |

**Step 4가 최소 실플레이선** — 여기서 멈추고 밸런싱 가능.

---

## 8. 확장 지점 (미리 고려)

- **수확 시 소모**: `HarvestAction` 마지막에 `tile.appliedFertilizers.Clear()`, `tile.activeTags.Clear()` → 기획서 "1회 발동 후 사라짐" 충족.
- **죽은 작물**: `RemoveDeadAction` 도 동일 소멸.
- **존버**: `Harvestable` 에도 비료 부착 가능 (Step 4 `CanExecute` 반영).
- **저장**: `SaveManager` 확장 시 `appliedFertilizers`, `activeTags` 직렬화 필요 (현재 로드 미구현, 보류).

---

## 9. 결정된 사항

| 항목 | 결정 |
|------|------|
| 유기농 프리미엄 (비료 없을 때 x2) | **제거** |
| 비료 중복 부착 | 다른 종류 무제한 / **같은 종류 1개** / **총 상한 3개** |
| 구매처 | **DroneShop 통합** (전용 상점 X) |
| 장착 방식 | **3번 키** — 씨앗(2번)과 동일한 순환 스타일 |
| 잭팟 연출 | **배율 티어(None/Small/Big/Mega) 별 차등 연출** |
