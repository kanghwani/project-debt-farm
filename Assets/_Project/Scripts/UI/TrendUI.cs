using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 오늘의 유행과 (라디오 구매 시) 내일의 유행 예보를 표시한다.
/// TrendManager.OnTrendChanged 를 구독해 자동 갱신.
/// </summary>
public class TrendUI : MonoBehaviour
{
    [Header("오늘 유행")]
    [SerializeField] private TextMeshProUGUI trendText;

    [Header("내일 유행 예보 (라디오 구매 후 활성화)")]
    [Tooltip("라디오를 구매하면 SetForecastUnlocked(true) 호출 — 내일 유행 표시")]
    [SerializeField] private TextMeshProUGUI forecastText;

    [Header("애니메이션")]
    [SerializeField] private float punchScale    = 0.2f;
    [SerializeField] private float punchDuration = 0.5f;

    private bool _forecastUnlocked = false;

    // ── 라이프사이클 ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        TrendManager.OnTrendChanged += Refresh;
        if (TrendManager.Instance?.TodayTrend != null)
            Refresh(TrendManager.Instance.TodayTrend);
    }

    private void OnDisable() => TrendManager.OnTrendChanged -= Refresh;

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 라디오·신문 아이템 구매 시 호출.
    /// true로 설정하면 forecastText에 내일 유행이 표시된다.
    /// </summary>
    public void SetForecastUnlocked(bool unlocked)
    {
        _forecastUnlocked = unlocked;
        RefreshForecast();
    }

    // ── 내부 갱신 ────────────────────────────────────────────────────────────

    void Refresh(TrendData data)
    {
        // 오늘 유행
        if (trendText != null)
        {
            trendText.text = data == null || data.tag == CropTag.None
                ? "<color=#AAAAAA>📰 [오늘의 뉴스]: 조용한 하루입니다.</color>"
                : $"📰 [오늘의 뉴스]: {TagEmoji(data.tag)} {data.flavorText}";
        }

        // 내일 예보
        RefreshForecast();

        // 등장 연출
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * punchScale, punchDuration, vibrato: 5, elasticity: 1f);
    }

    void RefreshForecast()
    {
        if (forecastText == null) return;

        if (!_forecastUnlocked)
        {
            forecastText.text = "<color=#555555>📻 [내일 예보]: ??? (라디오가 필요합니다)</color>";
            return;
        }

        TrendData next = TrendManager.Instance?.NextDayTrend;
        if (next == null || next.tag == CropTag.None)
        {
            forecastText.text = "<color=#AAAAAA>📻 [내일 예보]: 평온한 하루 예상</color>";
            return;
        }

        string rateText = next.isPositive
            ? $"<color=#FFE600>▲ ×{next.multiplier:F1}</color>"
            : $"<color=#FF5555>▼ -{Mathf.RoundToInt((1f - next.multiplier) * 100f)}%</color>";

        forecastText.text = $"📻 [내일 예보]: {TagEmoji(next.tag)} "
                          + $"<b>{TagName(next.tag)}</b> {rateText}";
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    static string TagEmoji(CropTag tag) => tag switch
    {
        CropTag.Spicy      => "🔥",
        CropTag.Sweet      => "🍬",
        CropTag.Ornamental => "✨",
        CropTag.Medicinal  => "💊",
        CropTag.Forbidden  => "⚠️",
        _                  => "📈",
    };

    static string TagName(CropTag tag) => tag switch
    {
        CropTag.Spicy      => "매운맛",
        CropTag.Sweet      => "단맛",
        CropTag.Ornamental => "관상용",
        CropTag.Medicinal  => "약용",
        CropTag.Forbidden  => "독성",
        _                  => tag.ToString(),
    };
}