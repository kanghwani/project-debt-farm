using UnityEngine;

// 책임: 턴 진행 + 액션 cap + 턴 종료 흐름 (변환 슬라이더 → 유지비 → 사망 체크)
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int   startMonth        = 1;
    [SerializeField] private int   actionCapPerTurn  = 3;
    [SerializeField] private float bunkerMaintenance = 10f;

    public int  CurrentMonth { get; private set; }
    public int  ActionsLeft  { get; private set; }
    public int  ActionCap    => actionCapPerTurn;
    public bool IsGameOver   { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CurrentMonth = startMonth;
        ActionsLeft  = actionCapPerTurn;
    }

    private void Start()
    {
        BunkerBloomEvents.RaiseTurnStarted(CurrentMonth);
        BunkerBloomEvents.RaiseActionsChanged(ActionsLeft);
        Debug.Log($"[TurnManager] M.{CurrentMonth:D2} start (actions {ActionsLeft}/{actionCapPerTurn})");
    }

    public bool HasActionsLeft() => ActionsLeft > 0 && !IsGameOver;

    public bool TrySpendAction()
    {
        if (!HasActionsLeft()) return false;
        ActionsLeft--;
        BunkerBloomEvents.RaiseActionsChanged(ActionsLeft);
        return true;
    }

    /// <summary>턴 종료 버튼 → 인벤토리 변환 → 자원 차감 → 사망 체크 → 다음 달.</summary>
    public void EndTurn()
    {
        if (IsGameOver) return;

        // 1) 인벤토리 있으면 변환 UI 거침
        var inv = CropInventory.Instance;
        if (inv != null && inv.HasItems)
        {
            if (ConvertChoiceUI.Instance != null)
            {
                ConvertChoiceUI.Instance.Show(FinalizeEndTurn);
                return;
            }
            // UI 없으면 자동 50:50 (검증용 폴백)
            inv.Convert(0.5f);
        }
        FinalizeEndTurn();
    }

    private void FinalizeEndTurn()
    {
        // 2) 작물 1턴 성장
        foreach (var plot in CropPlot.AllPlots) plot.AdvanceTurn();

        // 3) 유지비 + 식량 차감 (Survivor가 식량 처리)
        ResourceManager.Instance.Modify(ResourceType.Power, -bunkerMaintenance);
        SurvivorManager.Instance?.ProcessTurnEnd();

        // 4) 게임오버 판정
        if (ResourceManager.Instance.Get(ResourceType.Power) <= 0f)
        {
            IsGameOver = true;
            BunkerBloomEvents.RaiseGameOver();
            Debug.Log("[TurnManager] Power depleted - GAME OVER");
            return;
        }
        if (SurvivorManager.Instance != null && SurvivorManager.Instance.Count <= 0)
        {
            IsGameOver = true;
            BunkerBloomEvents.RaiseGameOver();
            Debug.Log("[TurnManager] All survivors dead - GAME OVER");
            return;
        }

        // 5) 다음 턴
        BunkerBloomEvents.RaiseTurnEnded(CurrentMonth);
        CurrentMonth++;
        ActionsLeft = actionCapPerTurn;
        BunkerBloomEvents.RaiseTurnStarted(CurrentMonth);
        BunkerBloomEvents.RaiseActionsChanged(ActionsLeft);
        Debug.Log($"[TurnManager] M.{CurrentMonth:D2} start");
    }
}
