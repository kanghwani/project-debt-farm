using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlotState { Empty, Growing, Ripe }

// 책임: 한 셀의 상태 + 성장 카운터
// (SRP) UI 표시는 PlotCellView가 담당. 여기는 데이터만.
public class CropPlot : MonoBehaviour
{
    public static readonly List<CropPlot> AllPlots = new();

    public PlotState State     { get; private set; } = PlotState.Empty;
    public CropData  Crop      { get; private set; }
    public int       TurnsLeft { get; private set; }

    /// <summary>상태가 바뀔 때마다 발행. PlotCellView가 구독.</summary>
    public event Action OnStateChanged;

    private void OnEnable()  => AllPlots.Add(this);
    private void OnDisable() => AllPlots.Remove(this);

    public void Plant(CropData crop)
    {
        Crop      = crop;
        TurnsLeft = crop.growTurns;
        State     = PlotState.Growing;
        OnStateChanged?.Invoke();
    }

    public void AdvanceTurn()
    {
        if (State != PlotState.Growing) return;

        TurnsLeft--;
        if (TurnsLeft <= 0)
        {
            State = PlotState.Ripe;
            OnStateChanged?.Invoke();
        }
        else
        {
            OnStateChanged?.Invoke();  // 카운터 표시 갱신
        }
    }

    public void Clear()
    {
        Crop      = null;
        TurnsLeft = 0;
        State     = PlotState.Empty;
        OnStateChanged?.Invoke();
    }
}
