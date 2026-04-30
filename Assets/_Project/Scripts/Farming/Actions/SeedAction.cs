using UnityEngine;
using System;

// 책임: Tilled 상태의 타일에 현재 선택된 씨앗을 심는다
public class SeedAction : IFarmAction
{
    private readonly Action<Vector3, string, Color> showText;

    public SeedAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        if (!inventory.HasCurrentSeed()) return false;
        if (!farm.farmData.TryGetValue(cellPos, out TileData data)) return false;
        return data.currentState == TileData.TileState.Tilled;
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        TileData data = farm.farmData[cellPos];

        data.currentState = TileData.TileState.Seeded;
        data.cropType     = inventory.currentSelectedSeed;
        inventory.ConsumeCurrentSeed();

        // 씨앗 아이콘 표시
        CropData cropInfo = DataManager.Instance.GetCrop(data.cropType);
        if (cropInfo?.seededTile != null)
            farm.UpdateTileVisual(cellPos, cropInfo.seededTile);

        // 이미 물이 줘진 땅이라면 바로 성장 목록에 추가
        if (data.isWatered && !farm.activeCrops.Contains(cellPos))
            farm.activeCrops.Add(cellPos);

        AudioManager.PlaySFX(SfxType.SeedPlant);
        showText(cellPos, $"{data.cropType} 심기!", Color.green);
        Debug.Log($"[SeedAction] {data.cropType} 심기 완료 — {cellPos} / 물: {data.isWatered}");

        onComplete?.Invoke();
    }
}
