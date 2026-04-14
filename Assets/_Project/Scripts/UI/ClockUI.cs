using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image clockBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color tensionColor = Color.red;

    private void OnEnable()
    {
        TimeManager.OnTimeChanged += UpdateClockText;
        TimeManager.OnNightTension += StartTensionEffect;
        //다음 날이 되면 초기화하는 이벤트 구독
        TimeManager.OnDayChanged += ResetTensionEffect; 
    }

    private void OnDisable()
    {
        TimeManager.OnTimeChanged -= UpdateClockText;
        TimeManager.OnNightTension -= StartTensionEffect;
        //  구독 해제
        TimeManager.OnDayChanged -= ResetTensionEffect; 
    }

    private void UpdateClockText(int hour, int minute)
    {
        string displayHour = (hour == 24) ? "00" : hour.ToString("D2");
        timeText.text = $"{displayHour}:{minute.ToString("D2")}";
    }

    private void StartTensionEffect()
    {
        if (clockBackground != null)
        {
            clockBackground.color = tensionColor;
        }
        timeText.color = tensionColor;
    }

    //  아침이 되면 시계를 원래 색으로 되돌립니다.
    private void ResetTensionEffect()
    {
        if (clockBackground != null)
        {
            clockBackground.color = normalColor;
        }
        timeText.color = normalColor;
    }
}