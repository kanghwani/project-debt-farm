using TMPro;
using UnityEngine;

// 책임: Power 자원 값 표시
// (DIP) ResourceManager 직접 참조 없이 BunkerBloomEvents 구독만
public class PowerGaugeView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()  => BunkerBloomEvents.OnResourceChanged += OnChanged;
    private void OnDisable() => BunkerBloomEvents.OnResourceChanged -= OnChanged;

    private void OnChanged(ResourceType t, float v)
    {
        if (t != ResourceType.Power || label == null) return;
        label.text = $"POWER  {v:F0}";
    }
}
