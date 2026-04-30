using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 하루가 넘어갈 때 (CompleteSettlement → AdvanceToNextDay 직후) 화면을 덮고
/// "N일차 아침" 텍스트를 보여준 뒤 페이드아웃한다.
/// CanvasGroup alpha 패턴 — SetActive(false) 사용 안 함.
/// DailySettlementManager.CompleteSettlement() 맨 끝에서 Trigger() 호출.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
    public static DayTransitionUI Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private CanvasGroup  rootGroup;      // 루트 CanvasGroup
    [SerializeField] private Image        blackOverlay;   // 검정 패널 (color alpha만 씀)
    [SerializeField] private TextMeshProUGUI dayText;     // "N일차 아침"
    [SerializeField] private TextMeshProUGUI subText;     // 한 줄 부제 (선택)

    [Header("타이밍")]
    [SerializeField] private float fadeInDuration  = 0.4f;
    [SerializeField] private float holdDuration    = 1.2f;  // 텍스트 보여주는 시간
    [SerializeField] private float fadeOutDuration = 0.6f;

    // 일차별 부제 (index = day-1, 범위 밖이면 마지막 값 재사용)
    private static readonly string[] subLines =
    {
        "빚은 기다려주지 않는다.",
        "오늘도 흙을 파야 산다.",
        "이자가 불어나고 있다.",
        "손이 아파도 쉴 수 없어.",
        "사채업자의 눈이 빛난다.",
        "포기는 죽음이다. 계속 파라.",
        "마지막 날까지 버텨라.",
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Canvas에서 항상 가장 마지막 자식 = 최상단 렌더링
        // DroneShop, DailySettlementUI보다 늦게 그려지도록 보장
        transform.SetAsLastSibling();

        HideImmediate();
    }

    private void HideImmediate()
    {
        if (rootGroup == null) return;
        rootGroup.alpha          = 0f;
        rootGroup.interactable   = false;
        rootGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 정산 완료 직후 호출. day = 새로 시작하는 날 번호.
    /// </summary>
    public void Trigger(int day)
    {
        StartCoroutine(PlayTransition(day));
    }

    private IEnumerator PlayTransition(int day)
    {
        // 텍스트 세팅
        if (dayText != null)
            dayText.text = $"{day}일차 아침";

        if (subText != null)
        {
            int idx = Mathf.Clamp(day - 1, 0, subLines.Length - 1);
            subText.text = subLines[idx];
        }

        // 페이드인 (실제시간 기준 — timeScale이 이미 복구됐지만 혹시 몰라)
        rootGroup.blocksRaycasts = true;
        rootGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
        yield return new WaitForSecondsRealtime(fadeInDuration);

        // 유지
        yield return new WaitForSecondsRealtime(holdDuration);

        // 페이드아웃
        rootGroup.DOFade(0f, fadeOutDuration).SetUpdate(true)
            .OnComplete(() =>
            {
                rootGroup.interactable   = false;
                rootGroup.blocksRaycasts = false;
            });
    }
}
