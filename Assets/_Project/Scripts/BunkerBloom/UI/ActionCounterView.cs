using TMPro;
using UnityEngine;

public class ActionCounterView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()  => BunkerBloomEvents.OnActionsChanged += OnChanged;
    private void OnDisable() => BunkerBloomEvents.OnActionsChanged -= OnChanged;

    private void OnChanged(int actions)
    {
        if (label == null) return;
        int cap = TurnManager.Instance != null ? TurnManager.Instance.ActionCap : 3;
        label.text = $"ACTIONS  {actions}/{cap}";
    }
}
