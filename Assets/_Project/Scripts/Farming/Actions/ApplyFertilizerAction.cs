using UnityEngine;
using System;
using System.Collections.Generic;

// 책임: 장착된 비료를 Seeded/Harvestable 작물에 1회 바른다
// 조건: 같은 종류 중복 불가, 총 최대 3개, 비료 보유량 > 0
public class ApplyFertilizerAction : IFarmAction
{
    static readonly Vector3Int[] CARDINALS = {
        new(0,  1, 0), new(0, -1, 0),
        new(-1, 0, 0), new(1,  0, 0),
    };

    private readonly Action<Vector3, string, Color> showText;

    public ApplyFertilizerAction(Action<Vector3, string, Color> showText)
    {
        this.showText = showText;
    }

    public bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm)
    {
        Fertilizer selected = inventory.currentSelectedFertilizer;
        if (selected == Fertilizer.None) return false;
        if (!inventory.HasFertilizer(selected)) return false;

        if (!farm.farmData.TryGetValue(cellPos, out TileData data)) return false;
        if (data.currentState != TileData.TileState.Seeded &&
            data.currentState != TileData.TileState.Harvestable) return false;

        if (data.appliedFertilizers.Contains(selected)) return false; // 같은 종류 1개
        if (data.appliedFertilizers.Count >= 3)         return false; // 총 3개 상한

        return true;
    }

    public void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete)
    {
        Fertilizer selected = inventory.currentSelectedFertilizer;

        if (!farm.farmData.TryGetValue(cellPos, out TileData data))
        {
            onComplete?.Invoke();
            return;
        }

        FertilizerData fertData = DataManager.Instance.GetFertilizer(selected);
        if (fertData == null) { onComplete?.Invoke(); return; }

        // 1. 인벤토리 차감
        inventory.ConsumeFertilizer(selected);

        // 2. 타일에 비료 등록
        data.appliedFertilizers.Add(selected);

        // 3. 태그 부여 (중복 없이)
        if (fertData.grantTag != CropTag.None && !data.activeTags.Contains(fertData.grantTag))
            data.activeTags.Add(fertData.grantTag);

        // 4. GeneCopy 특수 처리 — 상하좌우 이웃에서 랜덤 태그 1개 복사
        if (fertData.special == FertilizerSpecial.GeneCopy)
            TryCopyNeighborTag(cellPos, data, farm);

        // 5. 타일 위 파티클 VFX (마지막 비료 색으로 갱신)
        FarmingManager.Instance?.SpawnFertilizerVFX(cellPos, fertData.vfxColor);

        // 6. 플로팅 텍스트
        string tagText = data.activeTags.Count > 0
            ? $" [{string.Join("/", data.activeTags)}]"
            : "";
        showText(cellPos, $"{fertData.displayName}{tagText}", fertData.vfxColor);

        Debug.Log($"[비료] {selected} → ({cellPos}) 적용. 현재 비료: {data.appliedFertilizers.Count}/3");

        onComplete?.Invoke();
    }

    // 상하좌우 이웃 타일의 activeTags 중 랜덤 1개를 복사
    void TryCopyNeighborTag(Vector3Int pos, TileData data, FarmingManager farm)
    {
        List<CropTag> candidates = new();
        foreach (var offset in CARDINALS)
        {
            Vector3Int neighbor = pos + offset;
            if (!farm.farmData.TryGetValue(neighbor, out TileData nd)) continue;
            foreach (var tag in nd.activeTags)
                if (!candidates.Contains(tag)) candidates.Add(tag);
        }

        if (candidates.Count == 0) return;

        CropTag copied = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (!data.activeTags.Contains(copied))
        {
            data.activeTags.Add(copied);
            Debug.Log($"[유전자 변조] 태그 복사: {copied}");
        }
    }
}
