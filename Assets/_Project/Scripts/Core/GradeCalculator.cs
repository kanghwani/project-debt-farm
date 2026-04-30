using UnityEngine;

// 최종가 = (basePrice + Σflatbonus) × gradeMult × (1 + ΣmultAdd + specialMultAdd) × trendMult
public static class GradeCalculator
{
    static readonly Vector3Int[] NEIGHBORS_8 = {
        new(-1,  1, 0), new(0,  1, 0), new(1,  1, 0),
        new(-1,  0, 0),                new(1,  0, 0),
        new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0),
    };

    public static HarvestResult CalculateHarvestResult(
        TileData tile,
        CropData crop,
        Vector3Int cellPos,
        FarmingManager farm,
        float trendMult = 1f)
    {
        // ── 1. 등급 배수 ──────────────────────────────────────────────────────
        float gradeMult = 0.5f;
        string gradeLabel = "C";

        if      (tile.qualityScore >= 80f) { gradeMult = 2.0f; gradeLabel = "S"; }
        else if (tile.qualityScore >= 50f) { gradeMult = 1.5f; gradeLabel = "A"; }
        else if (tile.qualityScore >= 20f) { gradeMult = 1.0f; gradeLabel = "B"; }

        // ── 2. 비료 효과 순회 ─────────────────────────────────────────────────
        int   flatTotal   = 0;
        float multTotal   = 0f;  // (1 + multTotal) 형태로 사용
        bool  isGambler   = false;

        foreach (Fertilizer fertType in tile.appliedFertilizers)
        {
            FertilizerData fert = DataManager.Instance?.GetFertilizer(fertType);
            if (fert == null) continue;

            flatTotal += fert.flatBonus;
            multTotal += fert.multAdd;

            switch (fert.special)
            {
                case FertilizerSpecial.LonelyBonus:
                    if (IsLonely(cellPos, farm)) multTotal += 1.0f;
                    break;

                case FertilizerSpecial.CommunalBonus:
                    multTotal += CountSameCropNeighbors(cellPos, tile.cropType, farm) * 0.2f;
                    break;

                case FertilizerSpecial.GrowthBoost:
                    multTotal -= 0.2f; // x0.8 페널티
                    break;

                case FertilizerSpecial.DebtorsCut:
                    int todayDebt = DebtManager.Instance != null ? DebtManager.Instance.GetTodayDebt() : 0;
                    flatTotal += Mathf.RoundToInt(todayDebt * 0.05f);
                    break;

                case FertilizerSpecial.Gambler:
                    isGambler = true;
                    break;

                // GeneCopy: 태그 복사는 ApplyFertilizerAction 시점에 처리 → 여기선 없음
            }
        }

        // ── 3. 최종 배수 계산 ─────────────────────────────────────────────────
        int   adjustedBase = crop.basePrice + flatTotal;
        float fertMult     = Mathf.Max(0f, 1f + multTotal);
        float totalMult    = gradeMult * fertMult * trendMult;

        // 갬블러: 0~2배 랜덤
        if (isGambler) totalMult *= Random.Range(0f, 2f);

        int finalPrice = Mathf.Max(0, Mathf.RoundToInt(adjustedBase * totalMult));

        // ── 4. 잭팟 티어 판정 ────────────────────────────────────────────────
        JackpotTier tier = totalMult < 2f  ? JackpotTier.Normal
                         : totalMult < 5f  ? JackpotTier.Combo
                         : totalMult < 10f ? JackpotTier.Jackpot
                                           : JackpotTier.Mega;

        return new HarvestResult
        {
            finalPrice      = finalPrice,
            gradeLabel      = gradeLabel,
            totalMultiplier = totalMult,
            flatBonusTotal  = flatTotal,
            jackpotTier     = tier,
        };
    }

    // ── 이웃 판정 헬퍼 ───────────────────────────────────────────────────────

    static bool IsLonely(Vector3Int pos, FarmingManager farm)
    {
        foreach (var offset in NEIGHBORS_8)
        {
            Vector3Int neighbor = pos + offset;
            if (farm.farmData.TryGetValue(neighbor, out TileData nd)
                && nd.currentState != TileData.TileState.Empty
                && nd.currentState != TileData.TileState.Tilled)
                return false;
        }
        return true;
    }

    static int CountSameCropNeighbors(Vector3Int pos, TileData.Crops cropType, FarmingManager farm)
    {
        int count = 0;
        foreach (var offset in NEIGHBORS_8)
        {
            Vector3Int neighbor = pos + offset;
            if (farm.farmData.TryGetValue(neighbor, out TileData nd)
                && nd.cropType == cropType
                && nd.currentState != TileData.TileState.Empty)
                count++;
        }
        return count;
    }
}

// ── 결과 구조체 ───────────────────────────────────────────────────────────────
public struct HarvestResult
{
    public int         finalPrice;
    public string      gradeLabel;
    public float       totalMultiplier;
    public int         flatBonusTotal;
    public JackpotTier jackpotTier;
}
