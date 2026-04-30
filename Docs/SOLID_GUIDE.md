# SOLID 원칙 가이드 — Project Debt Farm 기준

> 이론이 아니라 **지금 내 코드**로 배우는 SOLID.
> 각 원칙마다 "현재 코드 어디서 지켜지고 있는가" + "어디서 깨지고 있는가"를 함께 확인한다.

---

## S — 단일 책임 원칙 (Single Responsibility)

> **"한 클래스는 하나의 이유로만 바뀌어야 한다."**
> 쉽게 말하면: 클래스 이름 그대로의 일만 한다.

### ✅ 잘 지켜진 예 — `GradeCalculator.cs`

```csharp
// 이 클래스는 딱 하나만 합니다: 점수 → 등급+가격 계산
public static class GradeCalculator
{
    public static (int finalPrice, string gradeLabel) CalculateHarvestResult(...) { }
}
```

- 외부에서 어떻게 쓰는지 모른다
- UI를 그리지 않는다
- 데이터를 저장하지 않는다
- **오직 계산만** → 책임이 하나

### ✅ 잘 지켜진 예 — `DayNightCycle.cs`

```csharp
// 이 클래스는 딱 하나만 합니다: 시간 → 조명 색상 변환
private void UpdateLighting(int hour, int minute) { ... }
```

- 시간을 직접 계산하지 않는다 (TimeManager에 구독만 함)
- 빚 계산을 모른다
- **오직 조명만**

---

### ❌ 현재 위반 — `FarmingInteraction.cs`

`InteractWithTile()` 하나가 너무 많은 일을 한다.

```
InteractWithTile() 안에서 하는 일들:
1. 땅 파기 (Tilled 상태로 전환)
2. 씨앗 심기
3. 물 주기
4. 수확
5. 죽은 작물 치우기
6. 타이밍 바 띄우기
7. 플레이어 바쁨 상태 관리
8. 플로팅 텍스트 띄우기
```

**왜 문제인가?** 물 주기 로직을 수정하면 수확 로직이 깨질 수 있다. 테스트도 어렵다.

**앞으로 개선 방향 (지금 당장 안 해도 됨):**

```csharp
// 각 행동을 분리하는 방향
public class TillingAction   { public void Execute(...) { } }
public class SeedingAction   { public void Execute(...) { } }
public class WateringAction  { public void Execute(...) { } }
public class HarvestAction   { public void Execute(...) { } }
```

---

## O — 개방-폐쇄 원칙 (Open-Closed)

> **"확장에는 열려있고, 수정에는 닫혀있어야 한다."**
> 새 기능을 추가할 때 기존 코드를 건드리지 않아야 한다.

### ✅ 잘 지켜진 예 — 작물 추가 방법

현재 구조에서 `고구마`를 새로 추가할 때:

```csharp
// 1. TileData.cs — enum에 추가만 하면 됨
public enum Crops { None, radish, potato, tomato, pumpkin, sweetpotato } // ← 추가

// 2. Crops/ 폴더에 .asset 파일 하나 만들어서 DataManager에 등록
// 기존 코드 수정 없음!
```

`CropStateEngine`, `FarmingInteraction`, `GradeCalculator` 는 **건드릴 필요 없다.**
이게 개방-폐쇄 원칙이 지켜지는 것이다.

---

### ❌ 현재 위반 가능성 — `GradeCalculator.cs`

```csharp
// 현재: if/else로 하드코딩된 등급 구간
if (qualityScore >= 80f)      { gradeMultiplier = 2.0f; gradeLabel = "S"; }
else if (qualityScore >= 50f) { gradeMultiplier = 1.5f; gradeLabel = "A"; }
```

**문제:** SS 등급을 추가하려면 이 함수 안을 수정해야 한다. 

**개선 아이디어 (참고용):**

```csharp
// 등급 데이터를 외부에서 주입받는 방식으로 바꾸면 수정 없이 확장 가능
[Serializable]
public struct GradeTier { public float minScore; public float multiplier; public string label; }
```

---

## L — 리스코프 치환 원칙 (Liskov Substitution)

> **"자식 클래스는 부모 클래스를 완전히 대체할 수 있어야 한다."**
> 상속할 때 부모의 '약속'을 깨면 안 된다.

### 현재 프로젝트에서의 의미

지금 직접 상속을 거의 쓰지 않기 때문에 당장 위반 사례는 없다.
이 원칙이 중요해지는 시점: **나중에 `MonsterBase → LoanShark, BossMonster` 같은 상속 계층이 생길 때.**

**지켜야 할 것:**

```csharp
// 나쁜 예: 부모의 Attack()을 자식에서 무효화
public class BossMonster : MonsterBase
{
    public override void Attack() 
    {
        throw new NotImplementedException(); // ← 부모 약속 파기!
    }
}

// 좋은 예: 동작은 다르지만 약속은 지킴
public override void Attack()
{
    // 보스만의 방식으로 공격하지만, 공격은 반드시 한다
    LaunchMissile();
}
```

---

## I — 인터페이스 분리 원칙 (Interface Segregation)

> **"사용하지 않는 메서드를 구현하도록 강요하지 마라."**
> 뚱뚱한 인터페이스 하나보다 작은 인터페이스 여럿이 낫다.

### 현재 프로젝트에서의 의미

현재 인터페이스가 없다. 이 원칙이 필요해지는 시점이 언제인지 알아두자.

**적용할 좋은 시점 — 상호작용 시스템 개선 시:**

```csharp
// 뚱뚱한 인터페이스 (나쁜 예)
public interface IInteractable
{
    void OnTill();       // 땅 파기
    void OnSeed();       // 씨앗 심기
    void OnWater();      // 물 주기
    void OnHarvest();    // 수확
    void OnPurchase();   // 구매 (ShippingBox에는 없는 기능)
}

// 분리된 인터페이스 (좋은 예)
public interface IFarmTile    { void OnFarm(PlayerInventory inv); }
public interface IPurchasable { void OnPurchase(PlayerInventory inv); }
// ShippingBox는 IFarmTile 없이 IPurchasable만 구현
```

---

## D — 의존 역전 원칙 (Dependency Inversion)

> **"구체적인 것에 의존하지 말고, 추상적인 것에 의존하라."**
> 고수준 모듈이 저수준 모듈을 직접 new하거나 FindObject하지 마라.

### ✅ 잘 지켜진 예 — 이벤트 구독 패턴

```csharp
// DebtManager가 TimeManager를 직접 참조하지 않고 이벤트로만 연결
private void OnEnable()
{
    TimeManager.OnDebtSettlement += ProcessDailyDebt; // ← 추상(이벤트)에 의존
}
```

`DebtManager`는 `TimeManager`가 어떻게 생겼는지 모른다. 이벤트만 구독한다.

---

### ❌ 현재 위반 — `DebtManager.cs`

```csharp
private void Start()
{
    playerInventory = FindFirstObjectByType<PlayerInventory>(); // ← 구체 클래스를 직접 탐색
}
```

**왜 문제인가?** `PlayerInventory`가 씬에 없으면 null. 이름이 바뀌면 연결이 끊긴다.

**개선 방법 (쉬운 순서대로):**

```csharp
// 방법 1: Inspector에서 연결 (가장 간단)
[SerializeField] private PlayerInventory playerInventory;

// 방법 2: 나중에 인터페이스 도입
[SerializeField] private IWallet wallet; // PlayerInventory가 IWallet을 구현
```

---

## 정리 — 지금 당장 기억할 것

| 원칙 | 한 줄 요약 | 내 코드에서 확인할 것 |
|------|-----------|-------------------|
| **S** | 하나의 클래스 = 하나의 역할 | 이 클래스 이름을 한 문장으로 설명할 수 있나? |
| **O** | 기능 추가 = 코드 추가, 기존 수정 최소화 | 새 작물 추가할 때 기존 코드 건드리나? |
| **L** | 자식이 부모의 약속을 지키는가 | 오버라이드 후 부모 기능이 여전히 동작하나? |
| **I** | 안 쓰는 메서드를 구현하게 하지 마라 | 인터페이스 메서드 중 빈 구현이 있나? |
| **D** | FindObject/new 대신 주입/이벤트 사용 | Start()에 FindObject가 있나? |
