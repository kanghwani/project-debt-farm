# 기획서: 유행 고도화 · 존버 긴장감 · 수확 쥬스

> **목표**: 유행을 읽고 타이밍을 재는 전략 + 수확할 때 배수에 비례한 뽕맛 연출.

---

## 0. 기존 코드 충돌 분석 (작업 전 필독)

### ✅ 이미 구현된 것 (기획서와 겹쳐 보이지만 추가 작업 불필요)

| 항목 | 위치 | 상태 |
|------|------|------|
| Harvestable 상태에서 비료 적용 가능 | `ApplyFertilizerAction.CanExecute()` | **완료** — Seeded·Harvestable 모두 허용 |
| 존버 시 썩음 전이 | `CropStateEngine.CheckCropState()` | **완료** — Harvestable → timer >= rotTime×0.5 → Rotting |
| JackpotFeedback 카메라 흔들림 | `HarvestAction.Execute()` | **완료** — JackpotFeedback.TriggerJackpot() 호출 |

### ⚠️ 충돌 / 수정 필요

| 충돌 | 원인 | 해결 방향 |
|------|------|----------|
| **JackpotTier 기준 불일치** | 기존 코드: `<3x=None, 3~6x=Small, 6~12x=Big, 12x+=Mega` / 기획서: `<2x=일반, 2~5x=콤보, 5x+=잭팟` | **기획서 기준으로 통일** — enum 이름 변경 + GradeCalculator 판정 기준 수정 |
| **TrendManager 이벤트 시그니처** | 현재 `OnTrendChanged(CropTag, float)` — 부정 유행 추가 시 "배수 or 페널티" 구분 불가 | `TrendData` 구조체 도입 → 이벤트·호출부 전체 교체 |
| **UiManager 내 trend 코드 중복** | UiManager에 `UpdateTrendUI(CropTag, float)` 이미 있음 / 신규 TrendUI 컴포넌트와 이중 처리됨 | UiManager에서 trend 관련 코드 **제거** 후 TrendUI로 일원화 |
| **FloatingText Update Lerp** | 현재 `Update()`에서 Lerp로 scale 애니메이션 / DOTween 방식 혼재 시 충돌 | `Setup()` 오버로드 추가 — 기존 방식 유지하되 DOTween 파라미터 선택적 적용 |
| **GetMultiplierFor() 반환값** | 현재 양수 배수만 반환 (부정 유행 처리 불가) | `TrendData` 도입 후 메서드 시그니처 변경 → **HarvestAction 호출부도 수정** |

---

## 1. 유행 시스템 고도화

### 1-1. TrendData 구조체 신설 (`Data/TrendData.cs`)

```csharp
[System.Serializable]
public class TrendData
{
    public CropTag  tag;
    public bool     isPositive;   // true=상승장, false=하락장
    public float    multiplier;   // 상승: 1.5~4.0 / 하락: 0.5~0.8
    public string   displayName;  // 예: "매운맛 떡상", "육식 다이어트"
}
```

### 1-2. TrendManager 개편

**변경 사항**:
- `trendMultiplier float` 단일값 → `TrendData` 구조체로 교체
- `nextDayTrend` 미리 캐싱 (라디오 아이템 연동 대비)
- 일수 기반 확률: 초반(1~3일) 긍정 70% / 후반(7일+) 50:50
- 이벤트 시그니처 변경: `OnTrendChanged(TrendData)` — **UiManager 호출부 동시 수정 필요**

```
RollTrend() 흐름:
  1. nextDayTrend(이미 캐싱된 값)를 todayTrend에 승격
  2. 새 내일 유행 롤 (isPositive 확률 적용)
  3. OnTrendChanged?.Invoke(todayTrend)
  4. nextDayTrend 캐싱
```

**공개 API**:
```csharp
public TrendData TodayTrend { get; private set; }
public TrendData NextDayTrend { get; private set; }  // 라디오 아이템용
public float GetMultiplierFor(List<CropTag> tags);   // 반환: 상승이면 multiplier, 하락이면 0.5~0.8, 없으면 1f
```

**영향받는 파일**: `TrendManager.cs`, `HarvestAction.cs`(trendMult 전달부), `UiManager.cs`(이벤트 구독 제거)

### 1-3. TrendUI 신설 (`UI/TrendUI.cs`)

- UiManager에서 분리된 단일 책임 컴포넌트
- `TrendManager.OnTrendChanged(TrendData)` 구독
- 출력: `[오늘의 시세: 🔥매운맛 떡상! x3.0]` / `[오늘의 시세: 🥗육식 다이어트 하락장 -30%]`
- 색상: 상승=노란색, 하락=파란색

**UiManager 수정**: `UpdateTrendUI()` 메서드 + `TrendManager.OnTrendChanged` 구독/해제 코드 **삭제**

---

## 2. 존버 긴장감 확립

### 2-1. ApplyFertilizerAction — 추가 작업 없음

`CanExecute()`가 이미 `Harvestable` 상태를 허용하고 있음. **기획서의 "확인 및 수정" 요구사항은 이미 충족.**

### 2-2. CropStateEngine — 썩음 방지 확장성만 추가

기존 Harvestable→Rotting 전이 로직은 이미 작동 중. `TileData`에 플래그 하나만 추가:

```csharp
// TileData.cs에 추가
public bool isRotResistant = false;  // 향후 비료(썩음방지제)용 확장 플래그
```

`CheckCropState()` 분기에 조건 삽입:

```csharp
else if (data.currentState == Harvestable
      && data.currentTimer >= cropInfo.requireRotTime * 0.5f
      && !data.isRotResistant)   // ← 이 한 줄만 추가
{
    data.currentState = TileData.TileState.Rotting;
    ...
}
```

**수확/제거 시** `isRotResistant = false` 리셋도 `HarvestAction`·`RemoveDeadAction`에 추가.

---

## 3. 수확 쥬스

### 3-1. JackpotTier 기준 통일

기존 `FertilizerType.cs`의 `JackpotTier` enum **이름·기준 변경**:

```csharp
public enum JackpotTier
{
    Normal,   // totalMult < 2x  (기존 None)
    Combo,    // 2x ~ 5x         (기존 Small)
    Jackpot,  // 5x ~ 10x        (기존 Big)
    Mega      // 10x+            (기존 Mega)
}
```

`GradeCalculator` 판정 기준도 동일하게 수정:
```csharp
JackpotTier tier = totalMult < 2f  ? JackpotTier.Normal
                 : totalMult < 5f  ? JackpotTier.Combo
                 : totalMult < 10f ? JackpotTier.Jackpot
                                   : JackpotTier.Mega;
```

**영향받는 파일**: `FertilizerType.cs`, `GradeCalculator.cs`, `JackpotFeedback.cs`(switch 분기), `HarvestAction.cs`(prefix 문자열)

### 3-2. FloatingText DOTween 확장

기존 `Update()` Lerp 로직은 **유지** (도구 장착 텍스트 등 기본 용도).  
`Setup()` 오버로드 추가 — 선택적 DOTween 모드:

```csharp
// 기존 — 변경 없음
public void Setup(string text, Color color) { ... }

// 신규 오버로드
public void Setup(string text, Color color, float sizeScale, HarvestJuiceStyle style)
```

```csharp
public enum HarvestJuiceStyle { None, Combo, Jackpot }
```

| Style | 동작 |
|-------|------|
| None | 기존 Lerp 방식 그대로 |
| Combo | DOTween: 1.3배 커졌다 → 원래 크기 → 위로 떠오름 + fade |
| Jackpot | DOTween: 1.8배 튀어오름(Elastic) → 위로 빠르게 + fade |

`FarmingInteraction.ShowFloatingText()` 에 오버로드 추가:
```csharp
public void ShowFloatingText(Vector3 pos, string text, Color color,
    float sizeScale = 1f, HarvestJuiceStyle style = HarvestJuiceStyle.None)
```

### 3-3. HarvestAction 수정

수확 텍스트를 JackpotTier에 따라 분기:

```csharp
// 현재
showText(cellPos, $"{prefix}{result.gradeLabel}등급 +{result.finalPrice}G{suffix}", gradeColor);

// 변경 후
(string label, Color color, float scale, HarvestJuiceStyle style) = result.jackpotTier switch
{
    JackpotTier.Mega    => ("💥 JACKPOT! ",   Color.red,    2.0f, HarvestJuiceStyle.Jackpot),
    JackpotTier.Jackpot => ("🔥 ",            Color.red,    1.6f, HarvestJuiceStyle.Jackpot),
    JackpotTier.Combo   => ("✨ ",            Color.yellow, 1.3f, HarvestJuiceStyle.Combo),
    _                   => ("",              Color.white,  1.0f, HarvestJuiceStyle.None),
};
showText(cellPos, $"{label}{result.gradeLabel}등급 +{result.finalPrice}G", color, scale, style);
```

### 3-4. JackpotFeedback 기준 동기화

`TriggerJackpot()` switch를 새 enum 기준으로 업데이트:

```csharp
case JackpotTier.Combo:   // 가벼운 흔들림
case JackpotTier.Jackpot: // 강한 흔들림 + 플래시
case JackpotTier.Mega:    // 슬로모션 + 강한 흔들림 + 플래시
```

### 3-5. AudioManager 스텁 (`Core/AudioManager.cs`)

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public static event Action<string> OnPlaySFX; // "harvest_normal" / "harvest_combo" / "harvest_jackpot"

    public static void PlaySFX(string key) => OnPlaySFX?.Invoke(key);
}
```

`HarvestAction`에서 호출만 추가:
```csharp
string sfxKey = result.jackpotTier switch {
    JackpotTier.Normal  => "harvest_normal",
    JackpotTier.Combo   => "harvest_combo",
    _                   => "harvest_jackpot",
};
AudioManager.PlaySFX(sfxKey);
```

실제 AudioSource 연결은 나중에 — 구조만 잡아둠.

---

## 4. 구현 순서

| 순서 | 작업 | 영향 범위 |
|------|------|----------|
| 1 | `TrendData` 구조체 신설 | 신규 파일 |
| 2 | `TrendManager` 개편 (TrendData + nextDayTrend) | TrendManager.cs |
| 3 | `UiManager` trend 코드 제거 + `TrendUI` 신설 | UiManager.cs, TrendUI.cs |
| 4 | `JackpotTier` enum 이름·기준 변경 | FertilizerType.cs, GradeCalculator.cs, JackpotFeedback.cs, HarvestAction.cs |
| 5 | `TileData.isRotResistant` 추가 + `CropStateEngine` 조건 삽입 | TileData.cs, CropStateEngine.cs, HarvestAction.cs, RemoveDeadAction.cs |
| 6 | `FloatingText` DOTween 오버로드 + `HarvestJuiceStyle` enum | FloatingText.cs, FarmingInteraction.cs |
| 7 | `HarvestAction` 티어별 텍스트 분기 + `JackpotFeedback` 동기화 | HarvestAction.cs, JackpotFeedback.cs |
| 8 | `AudioManager` 스텁 + HarvestAction 호출 | AudioManager.cs, HarvestAction.cs |

**순서 1~3이 선행** — TrendData 없으면 이후 유행 관련 코드 컴파일 불가.  
**순서 4가 선행** — JackpotTier enum 기준 바뀌면 6~7 코드가 달라짐.

---

## 5. 결정 필요 사항

| 항목 | 선택지 |
|------|--------|
| 하락장 페널티 강도 | -20%(`×0.8`) / -30%(`×0.7`) / -50%(`×0.5`) — 기획서는 -30% 예시 |
| 하락장 발생 확률 | 일차 무관 50:50 / 일수 비례(후반 갈수록 증가) / Inspector 수동 설정 |
| nextDayTrend UI 노출 | 라디오 아이템 구매 전까지 완전 비공개 / 힌트만 흘림(아이콘만) |
| Mega 티어 기준 | 현재 10x+ — 유지 or 기획서 기준 5x로 낮추기 |
