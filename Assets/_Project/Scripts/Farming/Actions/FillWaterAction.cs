using UnityEngine;
using System;

// 책임: 커서가 '물 충전 구역' 위에 있을 때 물통을 가득 채운다
public class FillWaterAction : IFarmAction
{
    private readonly Action<Vector3, string, Color> showText;

    public FillWaterAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        // 수정됨: Well.WellCells 대신 범용 WaterSource.ValidWaterCells 사용
        return WaterSource.ValidWaterCells.Contains(cellPos) && !inventory.IsWateringCanFull();
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        inventory.FillWater();
        showText(cellPos, $"물 {inventory.wateringCanLevel}/{inventory.wateringCanMax}", Color.cyan);
        Debug.Log($"[WaterAction] 물통 충전 완료: {inventory.wateringCanLevel}/{inventory.wateringCanMax}");
        onComplete?.Invoke();
    }
}