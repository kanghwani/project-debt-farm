public enum Fertilizer
{
    None,
    // Tag
    SpicySauce,         // 불닭 소스 액비 — #매운맛
    SugarCrystal,       // 설탕 결정 — #단맛
    RainbowOre,         // 무지개빛 광물 — #관상용
    // Stats & Mult
    PopcornYeast,       // 뻥튀기 효모 — +50G
    LonelyTonic,        // 고독한 영양제 — 주변 비면 x2
    CommunalCompost,    // 품앗이 퇴비 — 같은 작물 1개당 +0.2
    // Special
    GrowthAccelerator,  // 가속 촉진제 — 성장 50%, x0.8 페널티
    GamblerLye,         // 도박사의 잿물 — 0~200% 랜덤
    GeneModifier,       // 유전자 변조제 — 주변 태그 1개 복사
    DebtorsTears        // 채무자의 눈물 — 남은 빚의 5% 추가
}

public enum CropTag
{
    None,
    Spicy,              // 매운맛
    Sweet,              // 단맛
    Ornamental,          // 관상용
    Medicinal,  
    Forbidden
}

public enum FertilizerSpecial
{
    None,
    LonelyBonus,        // 주변 8칸 비면 x2
    CommunalBonus,      // 주변 같은 작물 1개당 +0.2
    GrowthBoost,        // 성장 50%, 최종 x0.8
    Gambler,            // 0~200% 랜덤
    GeneCopy,           // 상하좌우 태그 복사
    DebtorsCut          // 남은 빚의 5% 추가
}

public enum HarvestJuiceStyle
{
    None,       // 기본 Lerp 방식
    Combo,      // 커졌다 원래 크기 → float
    Jackpot     // Elastic 튀어오름 → float
}

public enum JackpotTier
{
    Normal,     // totalMult < 2x   — 기본 수확
    Combo,      // 2x ~ 5x          — 콤보
    Jackpot,    // 5x ~ 10x         — 잭팟
    Mega        // 10x+             — 메가 잭팟
}
