using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 사채업자 AI — 22시에 등장, 스트라이크 횟수에 따라 위협 수위 달라짐.
/// visualObject를 DOTween으로 슬라이드인/아웃, 말풍선은 CanvasGroup alpha로 제어.
/// </summary>
public class LoanSharkController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("사채업자 스프라이트·콜라이더가 있는 자식 오브젝트")]
    public GameObject  visualObject;
    public GameObject  speechBubbleUI;
    public TextMeshProUGUI speechText;
    public CanvasGroup bubbleCanvasGroup;   // 말풍선 페이드용 (없으면 자동 추가)

    [Header("Positions")]
    public Vector3 hiddenPosition  = new Vector3(-12f, 2f, 0f);   // 화면 밖
    public Vector3 standPosition   = new Vector3(-6f,  2f, 0f);   // 입구 대기 위치
    public float   slideInDuration = 0.6f;
    public float   chatInterval    = 5f;    // 대사 간격(초)
    public float   bubbleShowTime  = 2.5f;  // 대사 표시 시간(초)

    // ── 스트라이크별 대사 ─────────────────────────────────────────────────────
    // 0연체 (처음): 여유롭고 친절한 척
    private static readonly string[] comments0 = {
        "오늘도 열심히 하는군. 보기 좋아.",
        "자정까지 여유 있지? 서두르지 않아도 돼.",
        "농사 잘 되고 있어? 도움이 필요하면 말해.",
        "이자는 착실히 쌓이고 있다고. 참고로만.",
        "오늘 빚은 기억하지? 딱 그것만 내면 돼.",
    };
    // 1연체: 압박 시작
    private static readonly string[] comments1 = {
        "한 번 밀렸으니까 이번엔 꼭 내야 해.",
        "이자가 어떻게 불어나는지 알지?",
        "두 번은... 내가 참기 힘들어.",
        "작물 좀 더 빨리 팔 생각은 없어?",
        "시간이 얼마 없는데 뭐하는 거야.",
        "밭 좀 더 갈아야겠지 않아?",
    };
    // 2연체: 협박, 절박함
    private static readonly string[] comments2 = {
        "마지막 경고야. 이번엔 반드시 내.",
        "세 번째 밀리면... 알지?",
        "이 밭이 내 것이 될 수도 있어.",
        "도망갈 생각은 꿈도 꾸지 마.",
        "오늘 못 내면 다 끝이야. 알겠어?",
        "손이 떨리기 시작했어. 나쁜 징조지.",
        "마지막 기회야. 진심으로.",
    };

    private bool _arrived = false;

    // ── 라이프사이클 ─────────────────────────────────────────────────────────

    private void Awake()
    {
        TimeManager.OnTimeChanged += OnTimeChanged;
        TimeManager.OnMidnight    += OnMidnight;

        if (bubbleCanvasGroup == null && speechBubbleUI != null)
        {
            bubbleCanvasGroup = speechBubbleUI.GetComponent<CanvasGroup>();
            if (bubbleCanvasGroup == null)
                bubbleCanvasGroup = speechBubbleUI.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // 시작 시 화면 밖에 숨겨두기
        if (visualObject != null)
        {
            transform.position = hiddenPosition;
            visualObject.SetActive(false);
        }
        if (speechBubbleUI != null)
        {
            speechBubbleUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        TimeManager.OnTimeChanged -= OnTimeChanged;
        TimeManager.OnMidnight    -= OnMidnight;
    }

    // ── 시간 감시 ────────────────────────────────────────────────────────────

    private void OnTimeChanged(int hour, int minute)
    {
        if (hour == 22 && !_arrived)
            ArriveAtEntrance();
    }

    private void OnMidnight()
    {
        LeaveEntrance();
    }

    // ── 등장 / 퇴장 ──────────────────────────────────────────────────────────

    private void ArriveAtEntrance()
    {
        _arrived = true;
        transform.position = standPosition; // 시작부터 지정된 대기 위치에 배치

        if (visualObject != null) 
        {
            visualObject.SetActive(true);
            
            // SpriteRenderer가 있다면 페이드인 처리
            var sr = visualObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
                sr.DOFade(1f, slideInDuration);
            }
        }

        AudioManager.PlaySFX(SfxType.UiClick);   // 등장 효과음 (임시)
        Debug.Log("[LoanShark] 사채업자 등장 (페이드인)");

        StartCoroutine(ChatRoutine());
    }

    private void LeaveEntrance()
    {
        _arrived = false;
        StopAllCoroutines();

        // 말풍선 즉시 숨기기
        if (speechBubbleUI != null) speechBubbleUI.SetActive(false);

        // 페이드아웃 → 완료 후 비활성
        if (visualObject != null)
        {
            var sr = visualObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.DOFade(0f, slideInDuration).OnComplete(() => {
                    visualObject.SetActive(false);
                    transform.position = hiddenPosition;
                });
            }
            else
            {
                visualObject.SetActive(false);
                transform.position = hiddenPosition;
            }
        }

        Debug.Log("[LoanShark] 사채업자가 정산을 위해 떠났습니다. (페이드아웃)");
    }

    // ── 대화 루프 ────────────────────────────────────────────────────────────

    private IEnumerator ChatRoutine()
    {
        yield return new WaitForSeconds(1f);  // 등장 직후 잠깐 대기

        while (_arrived)
        {
            string line = PickComment();
            ShowBubble(line);
            yield return new WaitForSeconds(bubbleShowTime);
            HideBubble();
            yield return new WaitForSeconds(chatInterval);
        }
    }

    private string PickComment()
    {
        int strikes = DebtManager.Instance != null ? DebtManager.Instance.currentStrikes : 0;

        string[] pool = strikes switch
        {
            0 => comments0,
            1 => comments1,
            _ => comments2,
        };

        // 오늘 빚 금액을 일부 대사에 삽입
        int debt = DebtManager.Instance != null ? DebtManager.Instance.GetTodayDebt() : 0;
        string line = pool[Random.Range(0, pool.Length)];
        return line.Replace("{debt}", $"{debt:N0}G");
    }

    private void ShowBubble(string text)
    {
        if (speechBubbleUI == null) return;
        speechText.text = text;
        speechBubbleUI.SetActive(true);

        if (bubbleCanvasGroup != null)
        {
            bubbleCanvasGroup.alpha = 0f;
            bubbleCanvasGroup.DOFade(1f, 0.25f);
        }
    }

    private void HideBubble()
    {
        if (bubbleCanvasGroup != null)
        {
            bubbleCanvasGroup.DOFade(0f, 0.25f)
                .OnComplete(() => speechBubbleUI?.SetActive(false));
        }
        else
        {
            speechBubbleUI?.SetActive(false);
        }
    }

    // ── 디버그 ──────────────────────────────────────────────────────────────
    [ContextMenu("강제 등장 테스트")]
    private void DEBUG_ForceArrival() => ArriveAtEntrance();

    [ContextMenu("강제 퇴장 테스트")]
    private void DEBUG_ForceLeave() => LeaveEntrance();
}
