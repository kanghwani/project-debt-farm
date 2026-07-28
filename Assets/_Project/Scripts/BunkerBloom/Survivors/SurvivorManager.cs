using System;
using UnityEngine;

// 책임: 생존자 수 관리 + 식량 차감 + 부상/기아 사망 처리
public class SurvivorManager : MonoBehaviour
{
    public static SurvivorManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int   startSurvivors               = 3;
    [SerializeField] private float foodPerSurvivor              = 10f;
    [SerializeField] private int   manualHarvestInjuryThreshold = 2;

    public int   Count         { get; private set; }
    public float DailyFoodNeed => Count * foodPerSurvivor;

    private int  _manualHarvestsThisTurn = 0;
    private bool _starvationPending      = false;

    public static event Action<int> OnSurvivorsChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Count = startSurvivors;
    }

    private void Start() => OnSurvivorsChanged?.Invoke(Count);

    public void RegisterManualHarvest()
    {
        _manualHarvestsThisTurn++;
        Debug.Log($"[Survivor] ! Manual harvest {_manualHarvestsThisTurn}x this turn");
    }

    /// <summary>턴 종료 시 호출: 식량 차감 → 맨몸 부상 → 기아 처리.</summary>
    public void ProcessTurnEnd()
    {
        if (Count <= 0) return;

        // 1) 식량 차감
        ResourceManager.Instance?.Modify(ResourceType.Food, -DailyFoodNeed);

        // 2) 맨몸 수확 부상 (같은 턴 2회 누적 → 사망)
        if (_manualHarvestsThisTurn >= manualHarvestInjuryThreshold)
            KillOne("Manual harvest injury (radiation/contamination)");

        // 3) 식량 부족 — 음수 1턴 누적되면 다음 턴 사망
        float food = ResourceManager.Instance?.Get(ResourceType.Food) ?? 0f;
        if (food < 0f)
        {
            if (_starvationPending)
            {
                KillOne("Starvation");
                ResourceManager.Instance?.SetTo(ResourceType.Food, 0f);
                _starvationPending = false;
            }
            else
            {
                _starvationPending = true;
                Debug.Log("[Survivor] ! Food shortage (1 turn pending)");
            }
        }
        else
        {
            _starvationPending = false;
        }

        _manualHarvestsThisTurn = 0;
    }

    private void KillOne(string reason)
    {
        if (Count <= 0) return;
        Count--;
        OnSurvivorsChanged?.Invoke(Count);
        Debug.Log($"X [{reason}] survivors: {Count}");
    }
}
