using UnityEngine;
using System;
using System.Collections.Generic;

// 책임: 수확 가능한 타일에서 작물을 즉시 수확한다 (품질은 괭이질·물주기 누적값 사용)
public class HarvestAction : IFarmAction
{
    private readonly Action<Vector3, string, Color, float, HarvestJuiceStyle> showText;

    public HarvestAction(Action<Vector3, string, Color, float, HarvestJuiceStyle> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        if (!farm.farmData.TryGetValue(cellPos, out TileData data)) return false;
        return data.currentState == TileData.TileState.Harvestable;
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        if (!farm.farmData.TryGetValue(cellPos, out TileData data))
        {
            onComplete?.Invoke();
            return;
        }

        CropData cropInfo = DataManager.Instance.GetCrop(data.cropType);
        if (cropInfo == null) { onComplete?.Invoke(); return; }

        float trendMult = TrendManager.Instance != null
            ? TrendManager.Instance.GetMultiplierFor(data.activeTags)
            : 1f;

        HarvestResult result = GradeCalculator.CalculateHarvestResult(
            data, cropInfo, cellPos, farm, trendMult);

        // ── 영수증 상세 문자열 빌드 ──────────────────────────────────────────
        // 비료 요약
        string fertSummary = "";
        if (data.appliedFertilizers.Count > 0)
        {
            var names = new List<string>();
            foreach (var f in data.appliedFertilizers)
            {
                FertilizerData fd = DataManager.Instance?.GetFertilizer(f);
                if (fd != null)
                {
                    string effectStr = "";
                    if (fd.flatBonus > 0) effectStr += $"+{fd.flatBonus}G ";
                    if (Mathf.Abs(fd.multAdd) > 0.01f) effectStr += $"{(fd.multAdd > 0 ? "+" : "")}{fd.multAdd:F1}배 ";
                    if (fd.special == FertilizerSpecial.LonelyBonus) effectStr += "고독보너스 ";
                    if (fd.special == FertilizerSpecial.CommunalBonus) effectStr += "군집보너스 ";
                    if (fd.special == FertilizerSpecial.GrowthBoost) effectStr += "성장가속(-0.2배) ";
                    if (fd.special == FertilizerSpecial.Gambler) effectStr += "도박사 ";
                    if (fd.special == FertilizerSpecial.DebtorsCut) effectStr += "사채업자보너스 ";
                    if (fd.grantTag != CropTag.None) effectStr += $"#{fd.grantTag} ";

                    if (!string.IsNullOrWhiteSpace(effectStr))
                        names.Add($"{fd.displayName}({effectStr.Trim()})");
                    else
                        names.Add(fd.displayName);
                }
                else
                {
                    names.Add(f.ToString());
                }
            }
            fertSummary = string.Join(" + ", names);
        }

        // 유행 요약 (trendMult가 1이 아닐 때만 표시)
        string trendSum = "";
        if (data.activeTags.Count > 0 && Mathf.Abs(trendMult - 1f) > 0.01f)
        {
            string tagStr = string.Join(" ", data.activeTags);
            string arrow  = trendMult > 1f ? "↑" : "↓";
            trendSum = $"{tagStr}{arrow} ×{trendMult:F1}";
        }

        bool success = inventory.AddCrop(
            data.cropType, cropInfo.baseWeight, result.finalPrice, result.gradeLabel,
            result.totalMultiplier, result.flatBonusTotal, result.jackpotTier,
            fertSummary, trendSum);

        if (success)
        {
            GameStatsTracker.Instance?.TrackHarvest(
                result.finalPrice, cropInfo.cropName, result.gradeLabel, result.jackpotTier);

            data.currentState  = TileData.TileState.Tilled;
            data.cropType      = TileData.Crops.None;
            data.isWatered     = false;
            data.appliedFertilizers.Clear();
            data.activeTags.Clear();
            data.isRotResistant = false;
            data.currentTimer   = 0f;
            data.qualityScore   = 0f;

            farm.UpdateTileVisual(cellPos, null);
            farm.SetDry(cellPos);
            farm.ClearFertilizerVFX(cellPos);
            farm.activeCrops.Remove(cellPos);

            Color gradeColor = result.gradeLabel switch
            {
                "S" => Color.yellow,
                "A" => new Color(1f, 0.6f, 0f),
                "B" => Color.cyan,
                _   => Color.gray,
            };

            // 잭팟 티어별 텍스트·크기·스타일 분기
            var (prefix, textColor2, sizeScale, juiceStyle) = result.jackpotTier switch
            {
                JackpotTier.Mega    => ("💥 JACKPOT! ", Color.red,    2.0f, HarvestJuiceStyle.Jackpot),
                JackpotTier.Jackpot => ("🔥 ",          Color.red,    1.6f, HarvestJuiceStyle.Jackpot),
                JackpotTier.Combo   => ("✨ ",          Color.yellow, 1.3f, HarvestJuiceStyle.Combo),
                _                   => ("",             gradeColor,   1.0f, HarvestJuiceStyle.None),
            };
            string suffix = result.flatBonusTotal > 0 ? $" (+{result.flatBonusTotal}G)" : "";
            showText(cellPos, $"{prefix}{result.gradeLabel}등급 +{result.finalPrice}G{suffix}",
                textColor2, sizeScale, juiceStyle);

            // 효과음
            SfxType sfx = result.jackpotTier switch
            {
                JackpotTier.Combo                       => SfxType.HarvestCombo,
                JackpotTier.Jackpot or JackpotTier.Mega => SfxType.HarvestJackpot,
                _                                       => SfxType.HarvestNormal,
            };
            AudioManager.PlaySFX(sfx);

            // 잭팟 연출 (Normal이면 아무것도 안 함)
            if (result.jackpotTier != JackpotTier.Normal)
            {
                Vector3 worldPos = new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
                JackpotFeedback.Instance?.TriggerJackpot(result.jackpotTier, worldPos);
            }
        }

        onComplete?.Invoke();
    }
}
