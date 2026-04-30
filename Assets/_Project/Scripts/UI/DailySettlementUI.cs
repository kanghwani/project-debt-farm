using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class DailySettlementUI : MonoBehaviour
{
    public static DailySettlementUI Instance { get; private set; }

    // ── 루트 ─────────────────────────────────────────────────────────────────
    [Header("루트 패널")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private float fadeInDuration = 0.4f;

    // ── Phase 1: 영수증 (개별 아이템 순차 연출) ──────────────────────────────
    [Header("Phase 1 — 영수증 (큐 연출)")]
    [SerializeField] private GameObject phase1Panel;
    [SerializeField] private Transform  itemListContent;
    [SerializeField] private GameObject itemRowPrefab;   // SettlementItemRow가 부착된 프리팹
    [SerializeField] private TextMeshProUGUI baseTotalText;
    
    [Tooltip("기본 진행 속도 배율")]
    [SerializeField] private float defaultTimeScale = 1.0f;
    [Tooltip("Space 키 누를 때 배속")]
    [SerializeField] private float fastForwardScale = 4.0f;

    // ── Phase 2: 배수 슬롯머신 (통합됨) ───────────────────────────────────────
    [Header("Phase 2 — 최종 수익 확정")]
    [SerializeField] private GameObject phase2Panel; // 롤링 넘버와 함께 최종 수익 표시
    [SerializeField] private TextMeshProUGUI finalRevenueText;
    [SerializeField] private ParticleSystem goldParticle;

    // ── Phase 3: 사채 수금 ─────────────────────────────────────────────────────
    [Header("Phase 3 — 빚 수금")]
    [SerializeField] private GameObject      phase3Panel;
    [SerializeField] private TextMeshProUGUI debtAmountText;
    [SerializeField] private TextMeshProUGUI netGoldText;
    [SerializeField] private float           debtCutDuration = 1.0f;

    // ── 확인 버튼 ────────────────────────────────────────────────────────────
    [Header("확인 버튼")]
    [SerializeField] private Button          confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private int  _pendingEarnedGold  = 0;
    private bool _waitingForConfirm  = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        HideImmediate();

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    private void HideImmediate()
    {
        if (rootGroup == null) return;
        rootGroup.alpha          = 0f;
        rootGroup.interactable   = false;
        rootGroup.blocksRaycasts = false;
    }

    public void BeginSettlement(List<StackedCrop> items)
    {
        Debug.Log($"[DailySettlementUI] 정산 시작 (Queue 연출)! 아이템 개수: {items.Count}");
        StartCoroutine(SettlementSequence(items));
    }

    private IEnumerator SettlementSequence(List<StackedCrop> items)
    {
        if (rootGroup == null)
        {
            Debug.LogError("[DailySettlementUI] ❌ rootGroup이 연결되지 않았습니다! 빌더를 다시 실행하거나 Inspector를 확인하세요.");
            yield break;
        }

        if (itemRowPrefab == null || itemListContent == null)
        {
            Debug.LogError("[DailySettlementUI] ❌ itemRowPrefab 또는 itemListContent가 연결되지 않았습니다!");
            yield break;
        }

        // 화면 등장
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable   = true;
        rootGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
        yield return new WaitForSecondsRealtime(fadeInDuration);

        // 페이즈 초기화
        ShowPhase(1);
        AudioManager.PlaySFX(SfxType.SettleStart);
        
        int runningBase = 0;
        int runningFinal = 0;

        ScrollRect scrollRect = phase1Panel.GetComponentInChildren<ScrollRect>();

        if (items == null || items.Count == 0)
        {
            if (baseTotalText != null) baseTotalText.text = "오늘 납품한 작물이 없습니다.";
        }
        else
        {
            Queue<StackedCrop> queue = new Queue<StackedCrop>(items);
            
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                
                // 프리팹 생성 및 초기화
                GameObject rowObj = Instantiate(itemRowPrefab, itemListContent);
                rowObj.SetActive(true);
                
                var rowScript = rowObj.GetComponent<SettlementItemRow>();
                if (rowScript == null) rowScript = rowObj.AddComponent<SettlementItemRow>();
                
                rowScript.Init(item);

                // ⭐ 자동 스크롤 (아이템이 추가될 때마다 맨 아래로 포커스)
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                }
                
                // 애니메이션 대기 (Space 누르면 빨라짐)
                // 유저 피드백: 금방 사라지고 너무 빠름 -> 기본 딜레이를 0.5초로 늦춤
                float stepDelay = 0.5f;
                IEnumerator anim = rowScript.PlayAnimation(stepDelay);
                
                while (anim.MoveNext())
                {
                    // Input System 널 체크 안전장치 추가
                    bool isFast = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
                    float waitTime = isFast ? (0.02f) : (stepDelay);
                    yield return new WaitForSecondsRealtime(waitTime);
                }

                // 한 아이템 처리 완료 시 누적 금액 업데이트
                runningBase += item.data.basePrice;
                runningFinal += item.price;
                if (baseTotalText != null)
                {
                    baseTotalText.text = $"누적 금액: <color=#3A6B35>+{runningFinal}G</color>";
                    baseTotalText.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f).SetUpdate(true);
                }
                
                // 다음 작물로 넘어가기 전 대기시간 (읽을 시간 확보)
                bool isFastEnd = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
                yield return new WaitForSecondsRealtime(isFastEnd ? 0.05f : 0.8f);
            }
        }

        yield return new WaitForSecondsRealtime(0.8f);

        // ── Phase 2: 최종 수익 확정 ───────────────────────────────────────
        ShowPhase(2);
        AudioManager.PlaySFX(SfxType.SettleSuccess);

        if (finalRevenueText != null)
        {
            finalRevenueText.text = $"오늘 총 수익\n<color=#FFE600><size=150%>+{runningFinal}G</size></color>";
            finalRevenueText.transform
                .DOPunchScale(Vector3.one * 0.35f, 0.5f, 5, 0.5f)
                .SetUpdate(true);
        }

        goldParticle?.Play();
        _pendingEarnedGold = runningFinal;

        yield return new WaitForSecondsRealtime(1.5f);

        // ── Phase 3: 사채 수금 ────────────────────────────────────────────
        ShowPhase(3);
        AudioManager.PlaySFX(SfxType.SettleDebt);

        int  debtAmount = DebtManager.Instance?.GetTodayDebt() ?? 0;
        bool canPay     = runningFinal >= debtAmount;
        int  netGold    = canPay ? runningFinal - debtAmount : 0;

        if (debtAmountText != null)
        {
            debtAmountText.transform.localScale = Vector3.one * 2f;
            debtAmountText.text = $"<color=#FF5555>상환액 -{debtAmount}G</color>";
            debtAmountText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.4f);

        if (netGoldText != null)
        {
            float displayed2 = runningFinal;
            DOTween.To(
                () => displayed2,
                x => { displayed2 = x; netGoldText.text = $"수령액: {Mathf.RoundToInt(x)}G"; },
                netGold,
                debtCutDuration
            ).SetEase(Ease.InQuad).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(debtCutDuration);

        if (!canPay)
        {
            AudioManager.PlaySFX(SfxType.SettleStrike);
            if (netGoldText != null)
                netGoldText.text = "<color=#FF5555>⚠️ 잔액 부족 — 연체 발생!</color>";
        }

        yield return new WaitForSecondsRealtime(0.8f);

        // 버튼 활성화
        if (confirmButton != null)
        {
            if (confirmButtonText != null)
                confirmButtonText.text = canPay ? "다음 날로 →" : "연체... 다음 날로 →";

            confirmButton.gameObject.SetActive(true);
            _waitingForConfirm = true;
            yield return new WaitUntil(() => !_waitingForConfirm);
        }
        else
        {
            FinishAndClose();
        }
    }

    private void OnConfirmClicked()
    {
        if (!_waitingForConfirm) return;
        _waitingForConfirm = false;
        confirmButton.gameObject.SetActive(false);
        FinishAndClose();
    }

    private void FinishAndClose()
    {
        rootGroup.DOFade(0f, 0.4f).SetUpdate(true).OnComplete(() =>
        {
            rootGroup.interactable   = false;
            rootGroup.blocksRaycasts = false;
            ClearItemRows();
            ResetPanels();
        });

        DailySettlementManager.Instance?.CompleteSettlement(_pendingEarnedGold);
    }

    private void ShowPhase(int phase)
    {
        if (phase1Panel != null) phase1Panel.SetActive(phase >= 1);
        if (phase2Panel != null) phase2Panel.SetActive(phase >= 2);
        if (phase3Panel != null) phase3Panel.SetActive(phase >= 3);
    }

    private void ClearItemRows()
    {
        if (itemListContent == null) return;
        foreach (Transform child in itemListContent)
            Destroy(child.gameObject);
    }

    private void ResetPanels()
    {
        ShowPhase(0);
        if (baseTotalText    != null) baseTotalText.text    = "";
        if (finalRevenueText != null) finalRevenueText.text = "";
        if (debtAmountText   != null) debtAmountText.text   = "";
        if (netGoldText      != null) netGoldText.text      = "";
    }
}
