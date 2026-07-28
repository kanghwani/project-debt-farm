using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CropSelectorUI : MonoBehaviour
{
    public static CropData CurrentSelection { get; private set; }

    [Header("Crops")]
    [SerializeField] private CropData potatoCrop;
    [SerializeField] private CropData vineCrop;

    [Header("UI")]
    [SerializeField] private Button btnPotato;
    [SerializeField] private Button btnVine;
    [SerializeField] private TextMeshProUGUI selectionLabel;

    private void Awake()
    {
        if (btnPotato != null) btnPotato.onClick.AddListener(() => Select(potatoCrop));
        if (btnVine   != null) btnVine.onClick.AddListener(()   => Select(vineCrop));
    }

    private void Start()
    {
        // 기본값: 감자
        Select(potatoCrop != null ? potatoCrop : vineCrop);
    }

    public void SelectPotato() => Select(potatoCrop);
    public void SelectVine() => Select(vineCrop);

    private void Select(CropData crop)
    {
        if (crop == null) return;
        CurrentSelection = crop;
        if (selectionLabel != null) selectionLabel.text = $"Selected: {crop.cropName}";
        Debug.Log($"[CropSelector] Selected -> {crop.cropName}");
    }
}
