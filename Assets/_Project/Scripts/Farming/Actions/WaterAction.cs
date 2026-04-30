using UnityEngine;
using System;

// 책임: 갈아진 밭에 물을 준다 + qualityScore 누적
public class WaterAction : IFarmAction
{
    private readonly Action<Vector3, string, Color> showText;

    // 괭이BAD+물BAD = 0점(C), 괭이GOOD+물GOOD = 30점(B), 둘다PERFECT = 80점(S)
    private const float BAD_QUALITY     =  0f;
    private const float GOOD_QUALITY    = 15f;
    private const float PERFECT_QUALITY = 40f;

    public WaterAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        if (!inventory.HasWater()) return false;
        if (!farm.farmData.TryGetValue(cellPos, out TileData data)) return false;
        if (data.isWatered) return false;

        return data.currentState == TileData.TileState.Tilled
            || data.currentState == TileData.TileState.Seeded;
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        if (TimingBarUI.Instance == null)
        {
            Debug.LogError("[WaterAction] TimingBarUI가 없습니다!");
            onComplete?.Invoke();
            return;
        }

        TimingBarUI.Instance.StartTimingAction((score) =>
        {
            if (!farm.farmData.TryGetValue(cellPos, out TileData data))
            {
                onComplete?.Invoke();
                return;
            }

            float quality = ScoreToQuality(score);

            farm.SetWatered(cellPos);
            inventory.UseWater();
            data.qualityScore += quality;

            // 씨앗이 심긴 칸이면 activeCrops 등록
            if (data.currentState == TileData.TileState.Seeded
             && !farm.activeCrops.Contains(cellPos))
                farm.activeCrops.Add(cellPos);

            ShowTimingText(cellPos, score, quality);

            // ActionFeedback에 onComplete 위임 → 연출 완료 후 이동 잠금 해제
            if (ActionFeedback.Instance != null)
                ActionFeedback.Instance.Play(score, ToolType.Water, onComplete);
            else
                onComplete?.Invoke();
        }, ToolType.Water);
    }

    private float ScoreToQuality(float score)
    {
        if (score >= 40f) return PERFECT_QUALITY;
        if (score >= 20f) return GOOD_QUALITY;
        return BAD_QUALITY;
    }

    private void ShowTimingText(Vector3Int cellPos, float score, float quality)
    {
        if      (score >= 40f) showText(cellPos, $"PERFECT! +{quality}점", Color.yellow);
        else if (score >= 20f) showText(cellPos, $"GOOD +{quality}점",     Color.cyan);
        else                   showText(cellPos, "BAD...",                  Color.gray);
    }
}
