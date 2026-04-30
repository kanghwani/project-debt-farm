using UnityEngine;
using System;

public class DebtManager : MonoBehaviour
{
    public static DebtManager Instance { get; private set; }

    [Header("Debt Settings")]
    // ── 밸런스 기준 (realSecondsPerDay=180 기준) ──────────────────────────────
    // Day1: 튜토리얼. 씨앗 2~3개 수확하면 달성 가능.
    // Day2: 비료·유행 파악 시작. B~A등급 필요.
    // Day3~: 유행 저격·잭팟 노려야 여유.
    // Day7+: S등급 연속 + 유행 ×3 필수 압박.
    public int[] dailyDebtGoals = { 500, 1500, 3000, 5500, 9000, 14000, 22000 };
    
    [Header("Strike Status")]
    public int currentStrikes = 0;
    public int maxStrikes = 3;

    private PlayerInventory playerInventory;

    public static event Action<int> OnStrikeUpdated; // UI 업데이트용 이벤트
    public static event Action<int, int> OnGameOver; // (최종 일차, 최종 골드) 게임 오버 이벤트

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    // OnDebtSettlement 구독 제거 —
    // DailySettlementManager가 Phase 4 애니메이션 완료 후 직접 호출하므로
    // 이벤트로 즉시 실행하면 정산 UI 연출 전에 돈이 차감되는 타이밍 충돌 발생.

    public void ProcessDailyDebt()
    {
        int dayIndex = TimeManager.Instance.CurrentDay - 1;
        if (dayIndex >= dailyDebtGoals.Length) dayIndex = dailyDebtGoals.Length - 1;

        int requiredAmount = dailyDebtGoals[dayIndex];

        Debug.Log($" [채무 정산] {TimeManager.Instance.CurrentDay}일차 빚 {requiredAmount}G 징수 시작...");

        if (playerInventory != null)
        {
            if (playerInventory.gold >= requiredAmount)
            {
                playerInventory.SpendGold(requiredAmount);
                GameStatsTracker.Instance?.TrackRepaid(requiredAmount);
                
                Debug.Log($" 빚 청산 완료! 남은 돈: {playerInventory.gold}G");
            }
            else
            {
                currentStrikes++;
                OnStrikeUpdated?.Invoke(currentStrikes); 
                
                //  수정됨: 지갑 주인에게 돈을 0원으로 만들라고 지시합니다.
                playerInventory.ResetGoldToZero(); 
                
                Debug.Log($" [연체 발생] 목표액을 채우지 못했습니다! 현재 연체: {currentStrikes}/{maxStrikes}");

                if (currentStrikes >= maxStrikes)
                {
                    GameOver();
                }
            }
        }
    }
    

    public int GetTodayDebt()
    {
        int dayIndex = TimeManager.Instance.CurrentDay - 1;
        if (dayIndex >= dailyDebtGoals.Length) dayIndex = dailyDebtGoals.Length - 1;
        return dailyDebtGoals[dayIndex];
    }

    // SaveManager가 저장된 연체 횟수를 복원할 때
    public void LoadStrikes(int strikes)
    {
        currentStrikes = strikes;
        OnStrikeUpdated?.Invoke(currentStrikes);
        Debug.Log($"[SaveManager] 연체 횟수 복원: {currentStrikes}/{maxStrikes}");
    }

    // ── 디버그 전용 메서드들 ─────────────────────────────────────────────────
    // DebugCheats.cs에서 직접 currentStrikes++ 하면 OnStrikeUpdated 이벤트가
    // 발사되지 않아 UI가 갱신되지 않습니다.
    // 이 메서드를 통하면 이벤트까지 올바르게 발사됩니다.
    [ContextMenu(" 강제 Strike +1 (테스트)")]
    public void DEBUG_AddStrike()
    {
        currentStrikes++;
        OnStrikeUpdated?.Invoke(currentStrikes);
        Debug.Log($"[치트] Strike +1 → 현재 {currentStrikes}/{maxStrikes}");

        if (currentStrikes >= maxStrikes)
        {
            Debug.Log("[치트] Strike 한도 초과 → 강제 게임오버 발동");
            GameOver();
        }
    }

    [ContextMenu(" 강제 게임오버 (테스트)")]
    public void DEBUG_ForceGameOver() => GameOver();

    private void GameOver()
    {
        Debug.Log(" 파산했습니다! 게임 오버!");
        Time.timeScale = 0f; // 게임 일시정지

        // 최종 결과 데이터를 이벤트로 전달 — DebtManager는 UI를 모른다 (SRP)
        int finalDay  = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 0;
        int finalGold = playerInventory != null ? (int)playerInventory.gold : 0;
        OnGameOver?.Invoke(finalDay, finalGold);
    }
}