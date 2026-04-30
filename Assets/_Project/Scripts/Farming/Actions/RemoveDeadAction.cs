using UnityEngine;
using System;

// 책임: 썩거나(Rotting) 완전히 죽은(Dead) 작물을 타일에서 제거한다
public class RemoveDeadAction : IFarmAction
{
    private readonly Action<Vector3, string, Color> showText;

    public RemoveDeadAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        if (!farm.farmData.TryGetValue(cellPos, out TileData data)) return false;
        return data.currentState == TileData.TileState.Rotting
            || data.currentState == TileData.TileState.Dead;
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        if (!farm.farmData.TryGetValue(cellPos, out TileData data))
        {
            onComplete?.Invoke();
            return;
        }

        bool isRotting = data.currentState == TileData.TileState.Rotting;

        // 작물 제거 시 비료도 소멸 (소모성 귀속형)
        data.appliedFertilizers.Clear();
        data.activeTags.Clear();
        data.isRotResistant = false;
        farm.RemoveTileData(cellPos);

        if (isRotting)
            showText(cellPos, "작물이 썩었습니다!", new Color(1f, 0.35f, 0.1f)); // 선명한 주황-빨강
        else
            showText(cellPos, "폐기물", Color.gray);

        onComplete?.Invoke();
    }
}
