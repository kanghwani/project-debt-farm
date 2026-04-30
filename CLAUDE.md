# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 게임 소개

**Project Debt Farm** — 빚을 갚기 위해 농사짓는 탑다운 2D 픽셀아트 게임.

플레이어는 사채업자에게 빚을 진 농부다. 매일 자정까지 작물을 재배·수확·납품해 빚을 갚아야 한다.
3번 연체하면 게임 오버. 시간은 항상 흐르고, 작물은 썩으며, 빚은 불어난다.

**핵심 감각**: 타이밍 바를 잘 맞출수록 작물 품질이 올라간다 (괭이질·물주기 각 PERFECT=+40점, GOOD=+15점).
누적 qualityScore → 등급(S/A/B/C) → 최종 판매가 결정. 빠른 손놀림과 판단력이 생존을 결정한다.

- 엔진: Unity 6000.3.10f1 (Universal Render Pipeline 2D)
- 스크립팅: C# / .NET Framework 4.7.1
- 주요 패키지: Input System 1.18, DOTween (Plugins/), TextMesh Pro, Tilemap Extras, Cinemachine

---

## 폴더 구조

```
Assets/_Project/Scripts/
├── Actor/      플레이어·NPC 제어
│               PlayerMove2D, PlayerFarming, PlayerInputReader, PlayerInventory
│               PlayerTrolleyDriver, Trolley, LoanSharkController
├── Core/       게임 규칙 시스템
│               TimeManager, DebtManager, FarmingInteraction, CropStateEngine
│               GradeCalculator, TrendManager, DailySettlementManager
│               DayNightCycle, SaveManager, GameStatsTracker
│               AudioManager (SfxType enum + OnPlaySFX 이벤트 버스)
│               SoundManager (OnPlaySFX 구독 → AudioClip 재생)
│               BGMManager (DontDestroyOnLoad, 시간대별 BGM 자동 전환)
│               DebugCheats (에디터 전용 치트키 + 타일 디버그 오버레이)
├── Data/       데이터 타입
│               CropData, DataManager, TileData, StackedCrop
│               FertilizerData, FertilizerType (JackpotTier·HarvestJuiceStyle enum 포함)
│               TrendData
├── Farming/    농장 인프라
│               FarmingManager, DualGridFarmTilemap, DualGridEditorTilemap
│               ShippingBox, Well, TileBlocker
│   └── Actions/  IFarmAction 구현체
│               TillAction, WaterAction, SeedAction, HarvestAction
│               ApplyFertilizerAction, FillWaterAction, RemoveDeadAction, NoWaterWarningAction
├── Juice/      연출·피드백
│               FloatingText, PlayerVisuals, JackpotFeedback, ActionFeedback
└── UI/         화면
                UiManager, ClockUI, TimingBarUI, DroneShop, DroneShopTrigger
                DailySettlementUI, DayTransitionUI, BankruptcyScreen
                CropTooltipUI, TrendUI, MainMenuManager
                FertilizerShopItemButton, ShopItemButton, TextTypewriter, IntroSequence

Assets/_Project/Art/
├── Tiles/      밭 타일 16종 (DualGrid용), 마커 타일
├── Sprites/    캐릭터·도구·배경 스프라이트
├── Materials/  GroundTilemapMaterial (Unlit 오버레이 셰이더)
└── Shaders/    TextureOverlayShader.shader (Unlit, 반복 텍스처 오버레이)

Assets/Editor/
└── TimingBarUIEditor.cs   Inspector Zone Preview 커스텀 에디터
```

---

## 아키텍처: 이벤트 버스 패턴

모든 게임 시스템은 **TimeManager의 static 이벤트**를 중심으로 느슨하게 연결된다.
직접 참조 대신 이벤트를 구독하는 방식으로 의존성을 제거한다.

```
TimeManager (Update마다 시간 계산)
    │
    ├─ OnTimeChanged(hour, minute)  → ClockUI, DayNightCycle, BGMManager, LoanSharkController
    ├─ OnDayChanged                 → CropStateEngine, TrendManager (유행 갱신), UiManager
    ├─ OnMidnight                   → DailySettlementManager (정산 시작), LoanSharkController (퇴장)
    └─ OnNightTension               → (연결 예정)

DebtManager
    ├─ OnGameOver(finalDay, finalGold)  → BGMManager, BankruptcyScreen
    └─ OnStrikeUpdated(strikes)         → UiManager

TrendManager
    └─ OnTrendChanged(TrendData)        → TrendUI
```

새 시스템이 시간에 반응해야 할 때는 **OnEnable/OnDisable에서 구독/해제**한다.
`static event`는 씬 재로드 시 구독자가 사라지지 않으면 누적되므로 반드시 해제할 것.

---

## 핵심 데이터 흐름

### 농사 루프

```
PlayerFarming (입력 감지, CurrentCursorCell 프로퍼티 노출)
    └─ FarmingInteraction.InteractWithTile(cellPos, facingDir, slot, inventory)
            │
            ├─ IFarmAction.CanExecute() → Execute()   (OCP: 새 행동 추가 시 이 파일 건드리지 않음)
            │
            ├─ FarmingManager.farmData[pos]  (Dictionary<Vector3Int, TileData>)
            │       TileData 상태: Empty → Tilled → Seeded → Harvestable → Rotting → Dead
            │
            ├─ CropStateEngine.Update()  (activeCrops 리스트 매 프레임 순회)
            │       물 준 + 씨앗 심긴 칸만 activeCrops에 등록 → 타이머 진행
            │
            └─ GradeCalculator.CalculateHarvestResult(tile, crop, cellPos, farm, trendMult)
                    qualityScore → 등급(S/A/B/C)
                    + 비료 flatBonus/multAdd/special 처리
                    + TrendManager.trendMult 적용
                    → HarvestResult (finalPrice, gradeLabel, totalMultiplier, jackpotTier)
```

### 괭이질 가능 여부 판단 (IsFarmable)

```csharp
// FarmingManager.IsFarmable(pos)
baseTilemap.HasTile(pos)          // 잔디가 있는 칸
&& !farmTilemap.HasTile(pos)      // 이미 밭이 아닌 칸
&& !Well.WellCells.Contains(pos)  // 우물 영역 아님
&& !ShippingBox.BoxCells.Contains(pos)   // 출하 박스 영역 아님
&& !TileBlocker.BlockedCells.Contains(pos) // 드론상점·연못 등 차단 영역 아님
```

구조물에 농사 차단 영역을 추가하려면 해당 GameObject에 **`TileBlocker`** 컴포넌트를 붙이면 된다.

### 타이밍 바 (게임 감각의 핵심)

`TillAction`·`WaterAction` 실행 시 `TimingBarUI.StartTimingAction(callback, toolType)` 호출 → 인디케이터 왕복 → Space 뗄 때 판정.

**SFX 재생 타이밍**: Space를 떼는 그 프레임, `TimingBarUI.Confirm()` 안에서 즉시 재생 (`TillBad/Good/Perfect` 또는 `WaterBad/Good/Perfect`). ActionFeedback은 비주얼 연출만 담당.

판정 결과는 `onActionCompleted(score)` 콜백으로 전달 → `ActionFeedback.Play(score, toolType, onComplete)` 위임.

| 판정 | 조건 | qualityScore 기여 |
|------|------|-------------------|
| BAD  | score < 20 | +0 |
| GOOD | score ≥ 20 | +15 |
| PERFECT | score ≥ 40 | +40 |

**등급 판정** (괭이질+물주기 누적, 수확 시 최종 적용)

| 누적점수 | 등급 | 가격 배율 |
|---------|------|---------|
| 0~19 | C | ×0.5 |
| 20~49 | B | ×1.0 |
| 50~79 | A | ×1.5 |
| 80+ | S | ×2.0 |

- `HarvestAction`은 타이밍바 없이 즉시 수확, 누적 qualityScore로 등급 판정
- 타이밍 바 Zone 비율: 5등분 `[BAD|GOOD|PERFECT|GOOD|BAD]` = 0.2씩
- `TimingBarUIEditor.cs`: Inspector에서 구간 미리보기 시각화 지원

### ActionFeedback (Juice/ActionFeedback.cs)

타이밍 바 판정 직후 **비주얼 연출**을 담당하는 싱글톤. SFX는 재생하지 않음 (TimingBarUI에서 처리).

```
ActionFeedback.Play(score, ToolType.Till/Water, onComplete)
    BAD  (score < 20)  → 즉시 onComplete 호출 (연출 없음)
    GOOD (score ≥ 20)  → DOPunchScale(X납작) 시작 → 즉시 onComplete (애니메이션은 백그라운드)
    PERFECT(score ≥ 40)→ StretchY → HitStop(0.14초 실제시간) → onComplete → Squash + CameraShake (백그라운드)
```

**HitStop**: `DOTween.Kill("timescale")` → `Time.timeScale = 0.05` → 복구 DOTween.
`"timescale"` ID는 `JackpotFeedback.SlowMotion()`과 공유 — 충돌 방지.
GOOD·PERFECT 모두 onComplete 직후 플레이어가 자유로워진다 (애니메이션 완료를 기다리지 않음).

### Dual-Grid 밭 타일 시스템

```
DualGridFarmTilemap
    ├─ placeholderTilemap  (Tilemap_FarmData, 렌더러 OFF)
    │       tilledMarker 유무로 갈린 땅 여부 판단
    └─ displayTilemap      (Tilemap_FarmDisplay, -0.5,-0.5 오프셋)
            4방향 이웃 조합(TL/TR/BL/BR) → 16종 타일 자동 선택 (둥근 경계)
```

물 주기 시각화: `Tilemap_WaterOverlay` (별도 레이어, 반투명 청색)  
작물 아이콘: `Tilemap_CropVisual` (최상단 레이어, CropData.seededTile 등 연결)  
도로: `Tilemap_RoadData` (데이터) + `Tilemap_RoadDisplay` (시각, DualGrid 방식)

### 튜토리얼 사전 배치 밭

`FarmingManager.initialTilledCells` (Inspector `Vector3Int[]` 배열):
- 게임 시작 시 `Start()`에서 자동으로 해당 칸을 갈린 밭으로 등록
- `dualGrid.SetTilled()` + `farmData` 동시 등록 — TillAction과 완전히 동일한 경로
- **좌표 알아내기**: Inspector 우클릭 → "현재 farmData 셀 좌표 출력" (플레이 중 실행)

### 납품 흐름

```
PlayerInventory.heldItems (List<StackedCrop>)  ← 수확 시 AddCrop()으로 추가

ShippingBox (출하 박스 GameObject에 부착)
    OnTriggerEnter2D → Space 누르면 HandleShipping()
    ├─ DailySettlementManager.RegisterItem(item)  등록만 (즉시 골드 없음)
    ├─ ShowShippingFeedback()
    │       아이템별 플로팅 텍스트 (등급 색상, 좌우 분산)
    │       합계 대형 텍스트 (1000G 이상이면 Jackpot 스타일)
    │       AudioManager.PlaySFX(SfxType.ShopBuy)
    └─ playerInventory.ClearInventory()

자정 (TimeManager.OnMidnight)
    └─ DailySettlementManager.HandleMidnight()
            BGMManager.PlaySettlementBGM()
            Time.timeScale = 0
            DailySettlementUI.BeginSettlement(snapshot)
                Phase1: 영수증 출력 (SettleType 효과음)
                Phase2: 배수 슬롯머신 숫자 카운트업 (SettleRolling)
                Phase3: 최종 수익 확정 + 파티클 (SettleSuccess)
                Phase4: 빚 수금 카운트다운 (SettleDebt / SettleStrike)
                → 확인 버튼 클릭
            DailySettlementManager.CompleteSettlement(earnedGold)
                GameStatsTracker.TrackGoldEarned()
                PlayerInventory.AddGold(earnedGold)
                DebtManager.ProcessDailyDebt()   Strike or 정상 처리
                isGameOver 체크 → true면 이후 처리 중단 (DayTransition 생략)
                TimeManager.AdvanceToNextDay()
                SaveManager.SaveAllData()
                Time.timeScale = 1
                BGMManager.ResumeGameBGM()
                DayTransitionUI.Trigger(newDay)
```

`StackedCrop` 필드: `data(CropData)`, `grade`, `price`, `totalMultiplier`, `flatBonusTotal`, `jackpotTier`  
DailySettlementUI는 `CanvasGroup.alpha` 패턴으로 숨김 (`SetActive(false)` 금지 — 스크립트 비활성 방지).  
모든 코루틴 대기 `WaitForSecondsRealtime`, DOTween `.SetUpdate(true)` 사용 (`timeScale=0` 대응).

### DroneShop (드론 상점)

```
DroneShopTrigger (드론 박스 GameObject에 부착)
    OnTriggerEnter2D → DroneShop.OpenShop() + AudioManager.PlaySFX(ShopOpen)
    OnTriggerExit2D  → DroneShop.CloseShop()

DroneShop (UI 오브젝트에 부착)
    ├─ 씨앗 탭: DataManager에서 CropData 목록 → ShopItemButton 생성
    ├─ 비료 탭: DataManager에서 FertilizerData 목록 → FertilizerShopItemButton 생성
    └─ 구매: PlayerInventory.gold >= buyPrice → 차감 후 씨앗/비료 추가
```

DroneShop은 Start()에서 자동으로 열리지 않음 — DroneShopTrigger가 근접 시 열어줌.

### TileBlocker (농사 차단 영역)

`TileBlocker.BlockedCells` — static HashSet. 구조물에 붙이면 해당 반경 칸을 `IsFarmable()`에서 자동 차단.

```
사용 대상: 드론 상자, Pond(연못) 등 농사 불가 구조물
Well / ShippingBox는 자체 static HashSet 사용 (동일 패턴)
```

### BGM 시스템

```
BGMManager (DontDestroyOnLoad — 씬 전환 후에도 유지)
    두 AudioSource 핑퐁으로 크로스페이드
    │
    ├─ TimeManager.OnTimeChanged → ApplyTimeBasedBGM(hour)
    │       06시:  dayClip
    │       18시:  eveningClip (없으면 dayClip 유지)
    │       21시:  nightClip
    │       22시:  loanSharkClip (긴장감 BGM, 없으면 nightClip)
    │
    ├─ DailySettlementManager → PlaySettlementBGM() — 자정 정산 영수증
    ├─ DailySettlementManager → ResumeGameBGM()     — 정산 완료 후 낮 BGM 복귀
    └─ DebtManager.OnGameOver → gameOverClip (2초 페이드)

정산·게임오버 중에는 OnTimeChanged 시간대 전환 무시 (_settlementActive / _gameOverActive 플래그)
```

**Inspector 연결 슬롯**: `menuClip`, `dayClip`, `eveningClip`, `nightClip`, `loanSharkClip`, `settlementClip`, `gameOverClip`  
**시간대 경계**: `dayStartHour(6)`, `eveningStartHour(18)`, `nightStartHour(21)`, `loanSharkStartHour(22)` — Inspector에서 조절 가능.

### 오디오 시스템

```
AudioManager (이벤트 버스)
    static PlaySFX(SfxType)  →  OnPlaySFX 이벤트 발행

SoundManager (실제 재생)
    OnEnable: AudioManager.OnPlaySFX += HandlePlaySFX
    Inspector: SfxEntry[] { sfxType, clip, volume }
    HandlePlaySFX(sfx)  →  sfxSource.PlayOneShot(clip, volume)
```

`SfxType` enum 전체 목록 (AudioManager.cs):

| 카테고리 | SfxType |
|---------|---------|
| 이동 | `FootstepGrass`, `FootstepSoil` |
| 타이밍 바 판정 (괭이질) | `TillBad`, `TillGood`, `TillPerfect` |
| 타이밍 바 판정 (물주기) | `WaterBad`, `WaterGood`, `WaterPerfect` |
| 농사 기타 | `SeedPlant`, `FertApply`, `CropRot` |
| 수확 | `HarvestNormal`, `HarvestCombo`, `HarvestJackpot` |
| 타이밍 바 | `TimingStart`, `TimingTick` |
| UI/상점 | `UiHover`, `UiClick`, `UiError`, `ShopOpen`, `ShopBuy` |
| 밤 정산 | `SettleStart`, `SettleType`, `SettleStamp`, `SettleRolling`, `SettleSuccess`, `SettleDebt`, `SettleStrike` |
| 파산 선고 | `BankruptcyAppear`, `BankruptcyTitle`, `BankruptcyStatRow`, `BankruptcyButton` |

**SFX 볼륨 조절**: SoundManager Inspector의 `SfxEntry.volume` 필드 (0~2, 기본 1).  
**발걸음 소리**: `PlayerMove2D`가 `_stepTimer`로 0.38초 간격 재생. 현재 셀이 `farmData`에 있으면 `FootstepSoil`, 아니면 `FootstepGrass`.

### DayNightCycle

`DayNightCycle.cs` (Core/) — Global Light 강도 + Canvas 오버레이 색상으로 시간대 표현.

```
10개 TimeKey 키프레임 (hour, lightIntensity, overlayColor)
    0h  → 어두운 밤 (intensity 0.20, 오버레이 불투명)
    6h  → 새벽 전환
    8h  → 아침 밝아짐
    14h → 한낮 (intensity 1.00, 오버레이 없음)
    19h → 저녁
    22h → 밤 (intensity 낮아짐, 오버레이 불투명)
```

- Global Light color는 항상 white — intensity만 변화 (타일 색상 비균일 방지)
- Canvas 오버레이(DayNightOverlay Image)로 색조 표현 — max alpha 0.30
- `GetSurroundingKeys()`로 24h 랩어라운드 보간

### TextureOverlay 셰이더

```
Custom/OverlayShader (TextureOverlayShader.shader) — Unlit
    - 타일의 R채널을 마스크로 사용
    - 월드 좌표 기반 _OverlayTex 타일링 (_Scale 조절)
    - 타일 경계 무관 자연스러운 지면 질감

사용 마테리얼:
    GroundTilemapMaterial → Tilemap_Base (잔디)
    Plowed_Material       → Tilemap_FarmDisplay (밭)
```

모든 Tilemap을 동일한 Unlit 셰이더 계열로 통일해야 DayNightCycle 오버레이가 균일하게 적용됨.
Global Light(Lit) 셰이더와 Unlit 셰이더를 혼용하면 시간대별 색상이 불일치함.

### 비료 시스템

```
최종가 = (basePrice + Σflatbonus) × gradeMult × max(0, 1 + ΣmultAdd + specialMult) × trendMult
```

- **소모성 귀속형**: 수확 시 1회 발동 후 소멸
- **슬롯**: `3`번 키 장착, 이미 장착 중이면 순환
- **중복 규칙**: 같은 종류 1개 / 다른 종류 무제한 / 셀당 최대 3개

`FertilizerData` 필드: `type`, `displayName`, `buyPrice`, `grantTag`, `flatBonus`, `multAdd`, `special`, `vfxColor`

**비료 10종** (`FertilizerSpecial` enum으로 분기):

| 비료 | 효과 |
|------|------|
| SpicySauce / SugarCrystal / RainbowOre | CropTag 부여 (유행 저격) |
| PopcornYeast | flatBonus +50G |
| LonelyTonic | 주변 8칸 비면 multAdd +1.0 (×2) |
| CommunalCompost | 주변 같은 작물 1개당 multAdd +0.2 |
| GrowthAccelerator | 성장 50%, multAdd -0.2 페널티 |
| GamblerLye | 수확 시 totalMult × Random(0~2) |
| GeneModifier | 적용 시 상하좌우 이웃 태그 랜덤 1개 복사 |
| DebtorsTears | flatBonus += 오늘 빚 목표액 × 5% |

**잭팟 티어** (`JackpotTier` enum, totalMultiplier 기준):

| 티어 | 배율 | FloatingText | 카메라 |
|------|------|--------------|--------|
| Normal | <2x | 기본 (등급 색상) | 없음 |
| Combo | 2~5x | ✨ 노란색 1.3× | 가벼운 흔들림 |
| Jackpot | 5~10x | 🔥 빨간색 1.6× | 강한 흔들림 + 플래시 |
| Mega | 10x+ | 💥 JACKPOT! 빨간색 2.0× | 슬로모션 + 강한 흔들림 + 플래시 |

`HarvestJuiceStyle` enum (`FertilizerType.cs`): `None` / `Combo` / `Jackpot`  
`FloatingText.Setup(text, color, sizeScale, style)` — DOTween 오버로드

### 유행 시스템 (TrendManager)

`TrendData` struct: `tag`, `isPositive`, `multiplier`, `flavorText`

- 매일 `OnDayChanged` 시 `AdvanceTrend()` — nextDayTrend → todayTrend 승격
- 긍정(isPositive=true): 배율 ×3 상승 / 부정(false): 배율 ×0.7 하락
- 긍정 확률: 초반 70% / 중반 60% / 후반 50%
- `TrendManager.Instance.GetMultiplierFor(List<CropTag> tags)` → HarvestAction에서 호출
- `public static event Action<TrendData> OnTrendChanged` → TrendUI 구독

### FloatingText 시스템

```csharp
// 기본 (도구 장착 텍스트 등)
FarmingInteraction.Instance.ShowFloatingText(pos, text, color);

// 수확·납품 연출용 (DOTween 스타일)
FarmingInteraction.Instance.ShowFloatingText(pos, text, color, sizeScale, HarvestJuiceStyle);
```

`floatingTextPrefab`은 `FarmingInteraction` Inspector에 연결. 월드 좌표 기준으로 스폰됨.

---

## Tilemap 레이어 구조

| Tilemap | Order in Layer | 역할 |
|---------|---------------|------|
| Tilemap_Base | 0 | 잔디 (IsFarmable 판단 기준) |
| Tilemap_Obstacles | - | 벽·장애물 (TilemapCollider2D) |
| Tilemap_FarmDisplay | 1 | 밭 시각 (Dual-Grid 16종) |
| Tilemap_WaterOverlay | 2 | 물 준 칸 반투명 오버레이 |
| Tilemap_CropVisual | 3 | 씨앗·작물 아이콘 |
| Tilemap_FarmData | - | 데이터 전용 (렌더러 OFF) |
| Tilemap_RoadData | - | 도로 데이터 (렌더러 OFF) |
| Tilemap_RoadDisplay | - | 도로 시각 (DualGrid 방식) |

---

## 싱글톤 목록

| 클래스 | 씬 종속 | 특이사항 |
|--------|--------|---------|
| TimeManager | 씬 종속 | `startHour` 필드로 시작 시각 설정 |
| FarmingManager | 씬 종속 | `farmData`, `activeCrops`, `initialTilledCells` |
| FarmingInteraction | 씬 종속 | `floatingTextPrefab` 연결 필요 |
| DebtManager | 씬 종속 | `OnGameOver`, `OnStrikeUpdated` 이벤트 |
| TrendManager | 씬 종속 | `OnDayChanged` 구독, `OnTrendChanged` 발행 |
| TimingBarUI | 씬 종속 | `StartTimingAction(callback, ToolType)` — ToolType으로 SFX 분기 |
| PlayerFarming | 씬 종속 | `CurrentCursorCell` 프로퍼티 노출 |
| JackpotFeedback | 씬 종속 | DOTween 카메라 흔들림·슬로모션·플래시, `"timescale"` ID 공유 |
| ActionFeedback | 씬 종속 | 비주얼 전용 (SFX 없음), `ToolType { Till, Water }` |
| CropTooltipUI | 씬 종속 | CanvasGroup alpha 제어 (SetActive 미사용) |
| AudioManager | 씬 종속 | `SfxType` enum + `OnPlaySFX` static 이벤트 버스 |
| SoundManager | 씬 종속 | `SfxEntry[] { sfxType, clip, volume }` Inspector 매핑 |
| BGMManager | **DontDestroyOnLoad** | 씬 전환 후에도 유지, 크로스페이드, 7개 클립 슬롯 |
| DailySettlementManager | 씬 종속 | `TodayPendingTotal` 프로퍼티, `CompleteSettlement()` |
| DailySettlementUI | 씬 종속 | CanvasGroup alpha 패턴, 4페이즈 애니메이션 |
| GameStatsTracker | 씬 종속 | 런 통계 수집 → BankruptcyScreen에서 읽음 |
| BankruptcyScreen | 씬 종속 | `OnGameOver` 구독, Row별 CanvasGroup 순차 등장 |
| TrendUI | 씬 종속 | `OnTrendChanged` 구독, 긍정=노란색/부정=파란색 |
| SaveManager | 씬 종속 | - |
| **DataManager** | **DontDestroyOnLoad** | `allCrops`, `allFertilizers` 배열 |

---

## SOLID 원칙 적용 기준

이 프로젝트는 SOLID를 공부하며 진행한다.

- **S (단일 책임)** — `IFarmAction` 구현체마다 행동 하나. `TillAction`, `WaterAction`, `SeedAction`, `HarvestAction`, `RemoveDeadAction`, `ApplyFertilizerAction` 각각 단일 책임. `ActionFeedback`은 비주얼만, `TimingBarUI`는 SFX+판정만.
- **O (개방-폐쇄)** — 새 작물: `TileData.Crops` enum + `CropData` DataManager 등록만으로 완성. 새 농사 행동: `IFarmAction` 구현 + `FarmingInteraction` 리스트에 추가만. 새 차단 구조물: `TileBlocker` 추가만.
- **L (리스코프)** — MonoBehaviour 상속 시 base 동작을 깨지 않는다.
- **I (인터페이스 분리)** — `IFarmAction.CanExecute() / Execute(cellPos, facingDir, ...)` 로 행동 분리 완료.
- **D (의존 역전)** — `FindFirstObjectByType` 대신 Inspector SerializeField 또는 이벤트 구독 우선.

---

## 디버그

`DebugCheats.cs` — 씬의 빈 오브젝트에 부착. 에디터 전용 (`#if UNITY_EDITOR`).

| 단축키 | 효과 |
|--------|------|
| Shift+G | 강제 게임오버 |
| Shift+M | 골드 +10000 |
| Shift+K | Strike +1 |
| Shift+N | 하루 강제 종료 |
| Shift+T | 시간 4시간 추가 |
| Shift+F | 뻥튀기 비료 3개 추가 |
| Shift+D | **현재 커서 셀 타일 상태 Console 상세 출력** |

**타일 디버그 오버레이** (`showTileDebugOverlay` 체크박스): 화면 우상단에 커서 셀 실시간 상태 표시.
```
셀 (-2, 0, 0)
  baseTile:O  farmTile:X  farmData:X
  Farmable:O  CanInteract:X  슬롯:UniversalHand
  상태:없음  작물:-  물:X
```

---

## 코드 미구현 (TODO)

- **CropBehaviour** — 씨앗·작물을 GameObject로 띄우는 시스템 (현재 Tilemap 아이콘으로 임시 대체). 대규모 리팩토링 필요.
- **연못** — `TileBlocker` 컴포넌트 준비됨. Tilemap 배치 + `TileBlocker` 추가하면 차단 자동 처리.
- **라디오 아이템** — `TrendUI.SetForecastUnlocked(true)` API 준비됨. 상점 구매 흐름 연결 필요.

---

## 에디터 작업 목록 (코드 완성 → Unity Inspector 연결 필요)

### SoundManager SfxEntry 연결

SoundManager Inspector의 `SfxEntry[]` 배열에 각 SfxType마다 AudioClip 연결:

| 카테고리 | SfxType | 권장 클립 특성 |
|---------|---------|-------------|
| 타이밍 판정 | TillBad / TillGood / TillPerfect | 둔탁 / 타격 / 묵직한 흙소리 |
| 타이밍 판정 | WaterBad / WaterGood / WaterPerfect | 어색 / 물소리 / 청량 물보라 |
| 수확 | HarvestNormal / HarvestCombo / HarvestJackpot | 뾱 / 짤랑 / 챠칭 |
| 이동 | FootstepGrass / FootstepSoil | 잔디 / 흙 발소리 |

볼륨 조절: `SfxEntry.volume` 필드 (FootstepGrass·Soil = 0.5, Till = 1.3, Water = 0.5 권장)

### BGMManager 클립 연결

| 슬롯 | 상황 | 전환 시점 |
|------|------|----------|
| Day Clip | 낮 게임플레이 | 06시 |
| Evening Clip | 저녁 (선택) | 18시 |
| Night Clip | 밤 | 21시 |
| Loan Shark Clip | 사채업자 등장 긴장감 | 22시 자동 |
| Settlement Clip | 정산 영수증 | 자정 자동 |
| Game Over Clip | 파산 | 게임오버 자동 |

### 드론상점 · 출하상자 세팅

**DroneShopTrigger** (드론 박스 GameObject):
- `DroneShopTrigger` 스크립트 + `Collider2D (IsTrigger)` 추가
- Inspector: `Drone Shop` → DroneShop 컴포넌트 연결, `Player Layer` 설정

**ShippingBox** (출하 박스 GameObject):
- `ShippingBox` 스크립트 + `Collider2D (IsTrigger)` 추가
- Inspector: `Player Layer` 설정, `Radius` 조절

**TileBlocker** (드론 박스 / 연못 등):
- `TileBlocker` 스크립트 추가
- Inspector: `Radius` 조절 (씬뷰에서 빨간 와이어프레임으로 범위 확인)

### BankruptcyScreen Hierarchy 구성

```
[GameOverManager]  ← BankruptcyScreen + GameStatsTracker 스크립트
Canvas
└── BG_Overlay          (Image #0A0806, panelRoot 연결)
    └── Panel_Card       (Image #110E0A, panel_Card 연결)
        ├── TXT_Title    (TMP "파 산 선 고", #C0392B, txt_Title 연결)
        ├── TXT_Subtitle (TMP "HARVEST OR DIE", txt_Subtitle 연결)
        ├── TXT_Day      (TMP, txt_Day 연결)
        ├── TXT_Balance  (TMP, txt_Balance 연결)
        ├── Stats_Container  (Vertical Layout Group)
        │   ├── Row_Gold    ── TXT_Icon(💰) / TXT_Label / TXT_Value → statValues[0]
        │   ├── Row_Repaid  ── TXT_Icon(📋) / TXT_Label / TXT_Value → statValues[1]
        │   ├── Row_Harvest ── TXT_Icon(🌾) / TXT_Label / TXT_Value → statValues[2]
        │   ├── Row_Perfect ── TXT_Icon(✨) / TXT_Label / TXT_Value → statValues[3]
        │   ├── Row_Jackpot ── TXT_Icon(🏆) / TXT_Label / TXT_Value → statValues[4]
        │   └── Row_BestCrop── TXT_Icon(🌟) / TXT_Label / TXT_Value → statValues[5]
        └── Btn_Group
            ├── BTN_Restart   (btn_Restart 연결)
            └── BTN_MainMenu  (btn_MainMenu 연결)
```

CanvasGroup 필수 대상: TXT_Title, TXT_Subtitle, TXT_Day, TXT_Balance, 각 Row_xxx, 버튼들

### DailySettlementUI Canvas 계층

```
[DailySettlementUI] (CanvasGroup + DailySettlementUI 스크립트)
    ├── Phase1Panel → ItemListContent (VLG), BaseTotalText
    ├── Phase2Panel → RollingNumberText
    ├── Phase3Panel → FinalRevenueText, GoldParticle (ParticleSystem)
    ├── Phase4Panel → DebtAmountText, NetGoldText
    └── ConfirmButton → ConfirmButtonText
```

`ItemRowPrefab` — TextMeshProUGUI 하나짜리 프리팹 별도 생성 후 연결.

---

## 작물 추가 방법

1. `TileData.Crops` enum에 새 값 추가
2. `DataManager` Inspector의 `allCrops` 배열에 새 `CropData` 항목 추가
3. `CropData` 필드 설정: `cropType`, `cropName`, `requireGrowTime`, `requireRotTime`, `basePrice`, `seedPrice`, `baseWeight`
4. `seededTile`, `harvestableTile`, `rottingTile` 에 타일 에셋 연결
5. `Tilemap_CropVisual` 레이어에서 아이콘 표시됨

---

## 비료 추가 방법

1. `Fertilizer` enum에 새 값 추가 (`Data/FertilizerType.cs`)
2. `DataManager` Inspector의 `allFertilizers` 배열에 새 `FertilizerData` 항목 추가
3. `FertilizerData` 필드 설정: `type`, `displayName`, `buyPrice`, `grantTag`, `flatBonus`, `multAdd`, `special`, `vfxColor`
4. `FertilizerSpecial`이 `None`이 아니면 `GradeCalculator` switch 블록에 분기 추가

---

## 주요 인풋 키

- `Space` — 상호작용 (괭이질 / 물 주기 / 씨앗 심기 / 비료 / 수확 / 타이밍 바 확정 / 납품)
- `1` — 만능손 장착
- `2` — 씨앗 장착 (이미 장착 중이면 씨앗 종류 순환)
- `3` — 비료 장착 (이미 장착 중이면 비료 종류 순환)
- WASD / Arrow — 이동

---

## 폰트 설정 (TextMeshPro)

권장 폰트: **Galmuri11** (픽셀아트 한글 전용, OFL 라이선스)  
다운로드: https://github.com/quiple/galmuri/releases → `Galmuri.zip`

**TMP Font Asset 생성**:
1. `Window → TextMeshPro → Font Asset Creator`
2. Atlas Resolution: **4096 × 4096** (한글은 글자 수가 많음)
3. Character Set: `Custom Range`
4. Character Sequence (한글 전체):
   ```
   32-126,4352-4607,12288-12351,44032-55203
   ```
5. Generate Font Atlas → Save

**이모지**: 기존 Mona Emoji Font Asset을 메인 폰트의 `Fallback Font Assets`에 추가.
TMP가 메인 폰트에 없는 문자를 Fallback에서 자동 렌더링.
