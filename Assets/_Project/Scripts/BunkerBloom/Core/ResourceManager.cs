using System.Collections.Generic;
using UnityEngine;

// 책임: 4종 자원의 값 보유 + 변경/조회/지불 가드
// (SRP) 게임오버 판정은 TurnManager에서 — 여기는 값만 관리
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("초기값")]
    [SerializeField] private float startOxygen = 0f;
    [SerializeField] private float startFood   = 50f;   // 식량 활성화
    [SerializeField] private float startPower  = 80f;
    [SerializeField] private float startWater  = 0f;

    private readonly Dictionary<ResourceType, float> _resources = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _resources[ResourceType.Oxygen] = startOxygen;
        _resources[ResourceType.Food]   = startFood;
        _resources[ResourceType.Power]  = startPower;
        _resources[ResourceType.Water]  = startWater;
    }

    private void Start()
    {
        // UI 초기화를 위해 시작값 강제 발행
        foreach (var kv in _resources)
            BunkerBloomEvents.RaiseResourceChanged(kv.Key, kv.Value);
    }

    public float Get(ResourceType t) => _resources[t];

    public bool CanAfford(ResourceType t, float cost) => _resources[t] >= cost;

    public void Modify(ResourceType t, float delta)
    {
        _resources[t] += delta;
        BunkerBloomEvents.RaiseResourceChanged(t, _resources[t]);
    }

    public void SetTo(ResourceType t, float value)
    {
        _resources[t] = value;
        BunkerBloomEvents.RaiseResourceChanged(t, value);
    }
}
