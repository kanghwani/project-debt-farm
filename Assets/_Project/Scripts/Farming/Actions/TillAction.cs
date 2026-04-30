using UnityEngine;
using System;

// 책임: 잔디 위에서 괭이질 → DualGrid에 tilled 마킹 + qualityScore 누적
public class TillAction : IFarmAction
{
    private readonly Action<Vector3, string, Color> showText;

    // 타이밍 점수 기여량 (누적 → 수확 시 등급 반영)
    private const float BAD_QUALITY    =  0f;
    private const float GOOD_QUALITY   = 15f;
    private const float PERFECT_QUALITY= 40f;

    public TillAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        return farm.IsFarmable(cellPos) && !farm.farmData.ContainsKey(cellPos);
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        if (TimingBarUI.Instance == null)
        {
            Debug.LogError("[TillAction] TimingBarUI가 없습니다!");
            onComplete?.Invoke();
            return;
        }

        TimingBarUI.Instance.StartTimingAction((score) =>
        {
            float quality = ScoreToQuality(score);

            farm.Till(cellPos);
            farm.farmData.Add(cellPos, new TileData
            {
                currentState = TileData.TileState.Tilled,
                qualityScore = quality,
            });

            ShowTimingText(cellPos, score, quality);

            // ActionFeedback에 onComplete 위임 → 연출 완료 후 이동 잠금 해제
            if (ActionFeedback.Instance != null)
                ActionFeedback.Instance.Play(score, ToolType.Till, onComplete);
            else
                onComplete?.Invoke();
        }, ToolType.Till);
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
