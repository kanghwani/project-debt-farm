[System.Serializable]
public struct StackedCrop
{
    public CropData data;       // 원본 작물 데이터
    public float    weight;     // 실제 무게
    public int      price;      // 등급·비료·유행 배수가 반영된 최종 판매가
    public string   grade;      // 등급 (S / A / B / C)

    // ── 정산 연출용 (HarvestAction에서 채워짐) ──────────────────────────
    public float       totalMultiplier; // 최종 배수 (Phase 2 슬롯머신에 사용)
    public int         flatBonusTotal;  // 고정 보너스 합계
    public JackpotTier jackpotTier;     // 잭팟 티어

    // ── 영수증 상세 표시용 ─────────────────────────────────────────────
    /// <summary>사용한 비료 목록. ex) "팝콘이스트 + 설탕크리스탈"  비료 없으면 ""</summary>
    public string fertilizerSummary;
    /// <summary>유행 배수가 적용된 경우. ex) "매운맛↑ ×3.0"  영향 없으면 ""</summary>
    public string trendSummary;
}
