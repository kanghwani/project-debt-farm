using TMPro;
using UnityEngine;

public class FoodGaugeView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()  => BunkerBloomEvents.OnResourceChanged += OnChanged;
    private void OnDisable() => BunkerBloomEvents.OnResourceChanged -= OnChanged;

    private void OnChanged(ResourceType t, float v)
    {
        if (t != ResourceType.Food || label == null) return;
        label.text = $"FOOD  {v:F0}";
    }
}
