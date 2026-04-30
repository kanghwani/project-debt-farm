using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하루 동안 납품된 작물을 보관하다가 자정에 DailySettlementUI를 실행한다.
/// 골드 지급·빚 정산은 UI 연출 완료 후 CompleteSettlement()에서 처리한다.
/// </summary>
public class DailySettlementManager : MonoBehaviour
{
    public static DailySettlementManager Instance { get; private set; }

    private readonly List<StackedCrop> todayShippedItems = new();

    /// <summary>오늘 납품된 총 예정 수령액 (ShippingBox UI 표시용)</summary>
    public int TodayPendingTotal { get; private set; }

    private PlayerInventory playerInventory;

    // ── 라이프사이클 ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void OnEnable()  => TimeManager.OnMidnight += HandleMidnight;
    private void OnDisable() => TimeManager.OnMidnight -= HandleMidnight;

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>ShippingBox에서 작물을 받을 때 호출한다.</summary>
    public void RegisterItem(StackedCrop item)
    {
        todayShippedItems.Add(item);
        TodayPendingTotal += item.price;
    }

    /// <summary>
    /// DailySettlementUI가 모든 페이즈를 마친 뒤 호출한다.
    /// earnedGold = 오늘 번 총액 (빚 차감 전).
    /// 순서: AddGold → ProcessDailyDebt(빚 차감·연체 처리) → timeScale 복구.
    /// </summary>
    public void CompleteSettlement(int earnedGold)
    {
        // 순서: 골드 지급 → 빚 차감 → 게임오버 여부 확인 → 날짜 전환 → 저장 → 시간 재개
        GameStatsTracker.Instance?.TrackGoldEarned(earnedGold);
        playerInventory?.AddGold(earnedGold);
        DebtManager.Instance?.ProcessDailyDebt();

        // ── 게임오버 발생 시 이후 처리 중단 ────────────────────────────────────
        // ProcessDailyDebt() 안에서 GameOver()가 호출되면
        //   - Time.timeScale = 0 (게임 정지)
        //   - OnGameOver 이벤트 발사 (BankruptcyScreen 등장)
        // 이 상태에서 AdvanceToNextDay·DayTransitionUI·timeScale 복구를 하면 안 됨
        bool isGameOver = DebtManager.Instance != null
                       && DebtManager.Instance.currentStrikes >= DebtManager.Instance.maxStrikes;

        if (isGameOver)
        {
            Debug.Log("[DailySettlement] 게임오버 — 날짜 전환·DayTransitionUI 생략");
            return;
        }

        TimeManager.Instance?.AdvanceToNextDay();   // CurrentDay++, OnDayChanged 발사
        SaveManager.Instance?.SaveAllData();         // 정산 완료 후 올바른 상태 저장

        Time.timeScale = 1f;

        // 다음날 BGM 복귀
        BGMManager.Instance?.ResumeGameBGM();

        // 하루 전환 연출 (DayTransitionUI가 없어도 안전)
        int newDay = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
        DayTransitionUI.Instance?.Trigger(newDay);

        Debug.Log($"[DailySettlement] 정산 완료. 지급: {earnedGold}G / 잔액: {playerInventory?.gold ?? 0}G");
    }

    // ── 내부 ──────────────────────────────────────────────────────────────────

    private void HandleMidnight()
    {
        // 시간 정지 (UI 코루틴은 WaitForSecondsRealtime 사용)
        Time.timeScale = 0f;

        var snapshot = new List<StackedCrop>(todayShippedItems);
        todayShippedItems.Clear();
        TodayPendingTotal = 0;

        // 정산 BGM 전환
        BGMManager.Instance?.PlaySettlementBGM();

        if (DailySettlementUI.Instance != null)
        {
            Debug.Log("[DailySettlement] UI 정산 시작");
            DailySettlementUI.Instance.BeginSettlement(snapshot);
        }
        else
        {
            // UI가 없으면 즉시 정산 처리 (CompleteSettlement가 날짜 전환·저장까지 담당)
            Debug.LogError("[DailySettlement] ❌ DailySettlementUI.Instance가 null입니다! " +
                           "DailySettlementUI 오브젝트가 씬에 활성화 상태로 존재하는지 확인하세요.");
            int total = 0;
            foreach (var item in snapshot) total += item.price;
            CompleteSettlement(total);
            Time.timeScale = 1f;
        }
    }
}
