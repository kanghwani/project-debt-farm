using TMPro;
using UnityEngine;

public class SurvivorCounterView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()  => SurvivorManager.OnSurvivorsChanged += OnChanged;
    private void OnDisable() => SurvivorManager.OnSurvivorsChanged -= OnChanged;

    private void OnChanged(int count)
    {
        if (label == null) return;
        label.text = $"SURVIVORS  {count}";
    }
}
