using System;

// 책임: Bunker Bloom 시스템 간 결합을 끊는 static 이벤트 버스
// (DIP) UI는 매니저를 직접 참조하지 않고 이 버스만 구독
public static class BunkerBloomEvents
{
    public static event Action<int>                   OnTurnStarted;
    public static event Action<int>                   OnTurnEnded;
    public static event Action<ResourceType, float>   OnResourceChanged;
    public static event Action<int>                   OnActionsChanged;
    public static event Action                        OnInventoryChanged;
    public static event Action                        OnGameOver;

    public static void RaiseTurnStarted(int month)                   => OnTurnStarted?.Invoke(month);
    public static void RaiseTurnEnded(int month)                     => OnTurnEnded?.Invoke(month);
    public static void RaiseResourceChanged(ResourceType t, float v) => OnResourceChanged?.Invoke(t, v);
    public static void RaiseActionsChanged(int actions)              => OnActionsChanged?.Invoke(actions);
    public static void RaiseInventoryChanged()                       => OnInventoryChanged?.Invoke();
    public static void RaiseGameOver()                               => OnGameOver?.Invoke();
}
