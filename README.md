# 🌾 Project Debt Farm

> **빚을 갚기 위해 농사짓는 탑다운 2D 픽셀아트 게임**

![Unity](https://img.shields.io/badge/Unity-6000.3.10f1-black?logo=unity)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-blue)
![Language](https://img.shields.io/badge/Language-C%23-purple)

---

## 📖 스토리

아버지에게서 물려받은 건 낡은 농장이 아니었다.  
그 땅을 담보로 빌린 **천문학적인 사채 빚**이었다.

이미 상속 포기 기간은 지나버렸고, 오늘 밤 자정. 첫 수금이 시작된다.  
갚지 못하면... 내가 이 썩어가는 밭의 거름이 될 것이다.

---

## 🎮 게임플레이

매일 **자정까지 작물을 재배·수확·납품**해 빚을 갚아야 한다.  
3번 연체하면 게임 오버. 시간은 항상 흐르고, 작물은 썩으며, 빚은 불어난다.

### 핵심 조작

| 키 | 행동 |
|----|------|
| `WASD` / 방향키 | 이동 |
| `1` | 만능손 장착 |
| `2` | 씨앗 장착 (재입력 시 종류 변경) |
| `3` | 비료 장착 (재입력 시 종류 변경) |
| `Space` | 괭이질 · 씨앗 심기 · 물주기 · 수확 · 납품 |

### 농사 흐름

```
잔디 → [Space] 괭이질 → 씨앗 심기 → 물주기 → 성장 → 수확 → 납품 → 💰
```

### 타이밍 바 시스템

괭이질·물주기 시 타이밍 바가 등장한다. **Space를 떼는 순간** 판정.  
판정 결과가 누적되어 수확 시 작물 등급을 결정한다.

| 판정 | 점수 |
|------|------|
| ⬜ BAD | +0 |
| 🔵 GOOD | +15 |
| 🌟 PERFECT | +40 |

| 등급 | 누적 점수 | 가격 배율 |
|------|-----------|-----------|
| C | 0~19 | ×0.5 |
| B | 20~49 | ×1.0 |
| A | 50~79 | ×1.5 |
| S | 80+ | ×2.0 |

### 비료 시스템

작물에 비료를 뿌리면 **태그 부여·가격 보너스·배수 증가** 효과를 얻는다.  
매일 뉴스를 확인해 유행 태그에 맞는 비료를 전략적으로 활용하자.

---

## ⚙️ 기술 스택

- **엔진**: Unity 6000.3.10f1 (URP 2D)
- **언어**: C# / .NET 4.7.1
- **주요 패키지**: Input System, DOTween, TextMesh Pro, Tilemap Extras, Cinemachine

### 주요 시스템

- **Dual-Grid 밭 타일**: 4방향 이웃 조합으로 16종 경계 타일 자동 선택
- **IFarmAction 패턴**: OCP 기반 농사 행동 확장 (TillAction, WaterAction, SeedAction, HarvestAction 등)
- **이벤트 버스**: `TimeManager` static 이벤트 중심의 느슨한 시스템 연결
- **BGMManager**: DontDestroyOnLoad, 시간대별 BGM 자동 크로스페이드 (낮/저녁/밤/사채업자/정산)
- **자정 정산**: TimeScale=0 환경의 4단계 정산 UI 애니메이션

---

## 🗂️ 프로젝트 구조

```
Assets/_Project/
├── Scripts/
│   ├── Actor/      플레이어·NPC·사채업자
│   ├── Core/       게임 규칙 (TimeManager, DebtManager, BGMManager 등)
│   ├── Data/       CropData, FertilizerData, TileData
│   ├── Farming/    농장 인프라 + IFarmAction 구현체
│   ├── Juice/      연출·피드백 (FloatingText, ActionFeedback, JackpotFeedback)
│   └── UI/         모든 화면 (정산UI, 파산화면, 드론상점 등)
└── Art/
    ├── Tiles/      밭 타일 16종 (DualGrid)
    ├── Sprites/    캐릭터·배경 스프라이트
    └── Shaders/    Unlit 오버레이 셰이더 (주야간 사이클)
```

---

## 🚀 실행 방법

1. Unity 6000.3.10f1 이상 설치
2. 프로젝트 클론
   ```bash
   git clone https://github.com/kanghwani/project-debt-farm.git
   ```
3. Unity Hub에서 프로젝트 열기
4. `Assets/_Project/Scenes/` 에서 `IntroScene` 또는 `GameScene` 열기
5. Play 버튼

---

## 📝 개발 현황

- [x] 농사 루프 (괭이질 → 씨앗 → 물 → 수확 → 납품)
- [x] 타이밍 바 시스템 + 등급 판정
- [x] 비료 시스템 (10종)
- [x] 자정 정산 + 빚 시스템
- [x] 유행(트렌드) 시스템
- [x] 사채업자 NPC
- [x] 드론 상점
- [x] BGM 시간대 자동 전환
- [x] 파산 화면 + 런 통계
- [ ] 작물 오브젝트 시스템 (CropBehaviour)
- [ ] 연못 구현
- [ ] 라디오 아이템 (트렌드 예보)
