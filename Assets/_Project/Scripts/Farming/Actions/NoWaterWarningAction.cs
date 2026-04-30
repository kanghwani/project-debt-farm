using UnityEngine;
using System;

// 책임: 물이 없는 상태로 밭에 상호작용할 때 경고 메시지를 띄운다
public class NoWaterWarningAction : IFarmAction
{
    private readonly Action<Vector3, string, Color> showText;

    public NoWaterWarningAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        // 물이 없고, 해당 칸이 물을 줄 수 있는 상태일 때
        if (inventory.HasWater()) return false;
        if (!farm.farmData.TryGetValue(cellPos, out TileData data)) return false;
        if (data.isWatered) return false;

        return data.currentState == TileData.TileState.Tilled
            || data.currentState == TileData.TileState.Seeded;
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        showText(cellPos, "물 없음! 우물에서 채우세요", new Color(1f, 0.4f, 0.4f));
        onComplete?.Invoke();
    }
}
