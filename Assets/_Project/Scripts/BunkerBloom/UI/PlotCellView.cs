using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 책임: 셀 한 개의 시각 + 클릭 라우팅
//   빈 칸 클릭 → PlantAction
//   익은 칸 클릭 → HarvestAction
// (SRP) 게임 로직은 IDroneAction이 처리. 여기는 클릭→액션 매핑과 표시만.
[RequireComponent(typeof(Button))]
public class PlotCellView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CropPlot        plot;
    [SerializeField] private CropData        defaultCrop;     // Step 1: 기름덩굴
    [SerializeField] private TextMeshProUGUI stateLabel;
    [SerializeField] private Image           background;

    [Header("색상")]
    [SerializeField] private Color emptyColor   = new(0.10f, 0.20f, 0.10f);
    [SerializeField] private Color growingColor = new(0.20f, 0.40f, 0.20f);
    [SerializeField] private Color ripeColor    = new(0.50f, 0.90f, 0.20f);

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClicked);
    }

    private void OnEnable()
    {
        if (plot != null) plot.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (plot != null) plot.OnStateChanged -= Refresh;
    }

    private void OnClicked()
    {
        if (plot == null) return;

        // 심기 시 CropSelectorUI 선택 작물 우선, 없으면 defaultCrop 폴백
        CropData cropToPlant = CropSelectorUI.CurrentSelection != null
            ? CropSelectorUI.CurrentSelection
            : defaultCrop;

        IDroneAction action = plot.State switch
        {
            PlotState.Empty => cropToPlant != null ? new PlantAction(cropToPlant) : null,
            PlotState.Ripe  => new HarvestAction(),
            _               => null
        };

        if (action == null) return;

        if (!action.CanExecute(plot))
        {
            Debug.Log($"[PlotCell] {plot.State} - action denied (no actions or insufficient resources)");
            return;
        }

        action.Execute(plot);
    }

    private void Refresh()
    {
        if (plot == null || stateLabel == null || background == null) return;

        switch (plot.State)
        {
            case PlotState.Empty:
                stateLabel.text  = "EMPTY";
                background.color = emptyColor;
                break;
            case PlotState.Growing:
                stateLabel.text  = $"{plot.Crop.cropName}\n{plot.TurnsLeft}T";
                background.color = growingColor;
                break;
            case PlotState.Ripe:
                stateLabel.text  = $"{plot.Crop.cropName}\nRIPE";
                background.color = ripeColor;
                break;
        }
    }
}
