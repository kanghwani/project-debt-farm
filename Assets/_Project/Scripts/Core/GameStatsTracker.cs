using UnityEngine;

/// <summary>
/// 현재 런의 플레이 통계를 수집한다.
/// 각 시스템에서 Track*() 메서드를 호출해 데이터를 쌓고,
/// 게임오버 시 GameOverUI가 GetSnapshot()으로 읽어간다.
/// </summary>
public class GameStatsTracker : MonoBehaviour
{
    public static GameStatsTracker Instance { get; private set; }

    // ── 통계 필드 ─────────────────────────────────────────────────────────────
    public int   TotalGoldEarned  { get; private set; }   // 총 획득 골드
    public int   TotalRepaid      { get; private set; }   // 누적 상환액
    public int   TotalHarvests    { get; private set; }   // 총 수확 횟수
    public int   PerfectCount     { get; private set; }   // PERFECT 판정 횟수
    public JackpotTier BestJackpot { get; private set; } = JackpotTier.Normal; // 최고 잭팟 티어

    // 가장 비쌌던 작물
    public int    BestCropPrice    { get; private set; }
    public string BestCropName     { get; private set; } = "-";
    public string BestCropGrade    { get; private set; } = "-";

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── 수집 메서드 (각 시스템에서 호출) ─────────────────────────────────────

    /// <summary>DailySettlementManager.CompleteSettlement()에서 호출.</summary>
    public void TrackGoldEarned(int amount)
    {
        TotalGoldEarned += amount;
    }

    /// <summary>DebtManager.ProcessDailyDebt() 성공(상환 완료) 시 호출.</summary>
    public void TrackRepaid(int amount)
    {
        TotalRepaid += amount;
    }

    /// <summary>HarvestAction.Execute() 수확 성공 시 호출.</summary>
    public void TrackHarvest(int price, string cropName, string grade, JackpotTier jackpotTier)
    {
        TotalHarvests++;

        if (jackpotTier > BestJackpot)
            BestJackpot = jackpotTier;

        if (price > BestCropPrice)
        {
            BestCropPrice = price;
            BestCropName  = cropName;
            BestCropGrade = grade;
        }
    }

    /// <summary>ActionFeedback.PlayPerfect() 진입 시 호출.</summary>
    public void TrackPerfect()
    {
        PerfectCount++;
    }
}
