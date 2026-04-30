using System;
using UnityEngine;

// ── SFX 키 enum ───────────────────────────────────────────────────────────────
// string 직접 사용 대신 enum으로 오타 방지 + IDE 자동완성 지원
public enum SfxType
{
    // [1] 이동
    FootstepGrass,      // 잔디 발걸음
    FootstepSoil,       // 밭(흙) 발걸음

    // [1.5] 인트로
    IntroTyping,        // 인트로 텍스트 타이핑 (타타타탁)

    // [2] 괭이질 & 물주기 (타이밍 바 판정)
    TillBad,            // 둔탁한 실패음
    TillGood,           // 가벼운 타격음
    TillPerfect,        // 역경직과 함께 터지는 묵직한 흙소리
    WaterBad,           // 어색한 물소리
    WaterGood,          // 물 뿌리는 소리
    WaterPerfect,       // 청량하고 마법 같은 물보라 소리

    // [3] 농사 기타 액션
    SeedPlant,          // 흙에 씨앗 툭 박히는 소리
    FertApply,          // 비료 뿌릴 때 샤라락/치이익
    CropRot,            // 작물 썩을 때 바스락/불길한 소리

    // [4] 수확 뽕맛 ⭐
    HarvestNormal,      // 배수 2x 미만 (가벼운 뾱!)
    HarvestCombo,       // 배수 2~5x (기분 좋은 짤랑!)
    HarvestJackpot,     // 배수 5x 이상 (챠칭+빠바밤!!)

    // [5] 타이밍 바 시스템
    TimingStart,        // 바 나타날 때 (슉)
    TimingTick,         // 인디케이터 왕복 틱틱

    // [6] 상점 & UI
    UiHover,            // 마우스 올릴 때 틱
    UiClick,            // 버튼 누를 때 딸깍
    UiError,            // 잔액/재료 부족 (둔탁음)
    ShopOpen,           // 상점 열릴 때 드론 기계음
    ShopBuy,            // 아이템 구매 동전 소리

    // [7] 밤 정산 연출 ⭐
    SettleStart,        // 정산 패널 등장
    SettleType,         // 영수증 타닥타닥 타이핑
    SettleStamp,        // 유행 도장 쾅!
    SettleRolling,      // 슬롯머신 숫자 촤르르륵
    SettleSuccess,      // 최종 수익 확정 팡파르
    SettleDebt,         // 사채업자 수금 (서걱!/쿵)
    SettleStrike,       // 연체 발생

    // [8] 파산 선고 ⭐
    BankruptcyAppear,   // 패널 등장 (묵직한 드럼/충격음)
    BankruptcyTitle,    // "파 산 선 고" 타이틀 등장 (불길한 스팅)
    BankruptcyStatRow,  // 스탯 행 하나씩 등장 (타닥 타이핑)
    BankruptcyButton,   // 버튼 등장 (틱)
}

/// <summary>
/// 효과음 재생 요청을 이벤트로 중계한다.
/// 실제 재생은 SoundManager가 OnPlaySFX를 구독해서 처리한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>효과음 요청. SoundManager가 구독해 AudioClip을 재생한다.</summary>
    public static event Action<SfxType> OnPlaySFX;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static void PlaySFX(SfxType sfx)
    {
        OnPlaySFX?.Invoke(sfx);
    }
}
