using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 책임: TURN END 버튼 클릭 → TurnManager.EndTurn() 호출 + 월 라벨 표시
[RequireComponent(typeof(Button))]
public class TurnEndButtonView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI monthLabel;

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(() => TurnManager.Instance?.EndTurn());
    }

    private void OnEnable()
    {
        BunkerBloomEvents.OnTurnStarted += OnTurnStarted;
        BunkerBloomEvents.OnGameOver    += OnGameOver;
    }

    private void OnDisable()
    {
        BunkerBloomEvents.OnTurnStarted -= OnTurnStarted;
        BunkerBloomEvents.OnGameOver    -= OnGameOver;
    }

    private void OnTurnStarted(int month)
    {
        if (monthLabel != null) monthLabel.text = $"M.{month:D2}";
    }

    private void OnGameOver()
    {
        if (monthLabel != null) monthLabel.text = "GAME OVER";
        if (_btn != null)       _btn.interactable = false;
    }
}
