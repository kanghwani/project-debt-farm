using System;
using System.Collections.Generic;
using UnityEngine;

// 책임: 빈 칸에 작물 심기 (전기 -5, 액션 1)
public class PlantAction : IDroneAction
{
    private readonly CropData _crop;

    public Dictionary<ResourceType, float> ResourceCost { get; } = new()
    {
        { ResourceType.Power, 5f }
    };

    public PlantAction(CropData crop) => _crop = crop;

    public bool CanExecute(CropPlot plot)
    {
        if (plot.State != PlotState.Empty) return false;
        if (TurnManager.Instance == null || !TurnManager.Instance.HasActionsLeft()) return false;
        return ResourceManager.Instance.CanAfford(ResourceType.Power, ResourceCost[ResourceType.Power]);
    }

    public void Execute(CropPlot plot, Action onComplete = null)
    {
        if (!CanExecute(plot)) { onComplete?.Invoke(); return; }
        if (!TurnManager.Instance.TrySpendAction()) { onComplete?.Invoke(); return; }

        ResourceManager.Instance.Modify(ResourceType.Power, -ResourceCost[ResourceType.Power]);
        plot.Plant(_crop);
        Debug.Log($"[PlantAction] {_crop.cropName} planted (-5P, action -1)");
        onComplete?.Invoke();
    }
}
