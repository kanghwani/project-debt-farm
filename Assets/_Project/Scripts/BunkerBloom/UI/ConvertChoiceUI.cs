using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 책임: 턴 종료 시 인벤토리 변환 결정 (3안: 전부 식량 / 전부 전기 / 50:50)
// CanvasGroup 패턴으로 표시·숨김. 슬라이더 대신 버튼 3개 — 검증 단순화.
public class ConvertChoiceUI : MonoBehaviour
{
    public static ConvertChoiceUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private CanvasGroup panel;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI inventoryLabel;
    [SerializeField] private TextMeshProUGUI previewFoodLabel;     // "식량 +N"
    [SerializeField] private TextMeshProUGUI previewPowerLabel;    // "전기 +N"
    [SerializeField] private TextMeshProUGUI preview5050Label;     // "F+N / P+M"

    [Header("Buttons")]
    [SerializeField] private Button btnAllFood;
    [SerializeField] private Button btnAllPower;
    [SerializeField] private Button btnHalfHalf;

    private Action _onConfirmed;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    public void ConfirmAllFood()  => Confirm(1.0f);
    public void ConfirmAllPower() => Confirm(0.0f);
    public void ConfirmHalfHalf() => Confirm(0.5f);

    public void Show(Action onConfirmed)
    {
        _onConfirmed = onConfirmed;
        if (panel != null)
        {
            panel.alpha = 1f;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }
        UpdatePreviews();
    }

    private void Hide()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    private void UpdatePreviews()
    {
        var inv = CropInventory.Instance;
        if (inv == null) return;

        if (inventoryLabel != null)
        {
            string s = "";
            foreach (var kv in inv.GetAll()) s += $"{kv.Key.cropName} {kv.Value}  ";
            inventoryLabel.text = s.Trim();
        }
        if (previewFoodLabel  != null) { var p = inv.Preview(1.0f); previewFoodLabel.text  = $"FOOD +{p.food:F0}"; }
        if (previewPowerLabel != null) { var p = inv.Preview(0.0f); previewPowerLabel.text = $"POWER +{p.power:F0}"; }
        if (preview5050Label  != null) { var p = inv.Preview(0.5f); preview5050Label.text  = $"F+{p.food:F0}  /  P+{p.power:F0}"; }
    }

    private void Confirm(float foodRatio)
    {
        CropInventory.Instance?.Convert(foodRatio);
        Hide();
        _onConfirmed?.Invoke();
        _onConfirmed = null;
    }
}
