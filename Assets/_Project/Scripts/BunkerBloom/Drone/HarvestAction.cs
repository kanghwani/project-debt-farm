using System;
using System.Collections.Generic;
using UnityEngine;

// 책임: 익은 작물 수확 (드론 -10P or 맨몸 0P+부상위험) → 인벤토리 적재
// 변환은 턴 종료 시 ConvertChoiceUI에서 결정
public class HarvestAction : IDroneAction
{
    public Dictionary<ResourceType, float> ResourceCost { get; } = new()
    {
        { ResourceType.Power, 10f }
    };

    public bool CanExecute(CropPlot plot)
    {
        if (plot.State != PlotState.Ripe) return false;
        if (TurnManager.Instance == null || !TurnManager.Instance.HasActionsLeft()) return false;
        return true;  // Power 부족해도 맨몸 수확 가능
    }

    public void Execute(CropPlot plot, Action onComplete = null)
    {
        if (!CanExecute(plot)) { onComplete?.Invoke(); return; }
        if (!TurnManager.Instance.TrySpendAction()) { onComplete?.Invoke(); return; }

        var crop = plot.Crop;
        bool useDrone = ResourceManager.Instance.CanAfford(ResourceType.Power, ResourceCost[ResourceType.Power]);

        if (useDrone)
        {
            ResourceManager.Instance.Modify(ResourceType.Power, -ResourceCost[ResourceType.Power]);
            Debug.Log($"[HarvestAction] Drone harvest: {crop.cropName} +{crop.harvestCount} (-10P)");
        }
        else
        {
            SurvivorManager.Instance?.RegisterManualHarvest();
            Debug.Log($"[HarvestAction] !MANUAL harvest: {crop.cropName} +{crop.harvestCount} (power too low)");
        }

        CropInventory.Instance?.Add(crop, crop.harvestCount);
        plot.Clear();
        onComplete?.Invoke();
    }
}
