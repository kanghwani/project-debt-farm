using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 게임 오버 연출 + 통계 표시.
/// DebtManager.OnGameOver 이벤트를 구독해 자동 트리거.
/// 항상 활성화된 부모 오브젝트(GameOverManager)에 부착할 것.
/// </summary>
public class BankruptcyScreen : MonoBehaviour
{
    // ── 상단 텍스트 ──────────────────────────────────────────────────────────
    [Header("상단 텍스트")]
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtSubtitle;
    public TextMeshProUGUI txtDay;
    public TextMeshProUGUI txtBalance;

    // ── 스탯 값 텍스트 ────────────────────────────────────────────────────────
    [Header("스탯 Value 텍스트 (순서 고정)")]
    [Tooltip("0: 총 획득 골드  1: 누적 상환액  2: 수확 횟수  3: PERFECT 횟수  4: 최고 잭팟  5: 최고가 작물")]
    public TextMeshProUGUI[] statValues;

    // ── 버튼 ─────────────────────────────────────────────────────────────────
    [Header("버튼")]
    public Button btnRestart;
    public Button btnMainMenu;

    // ── 패널 ─────────────────────────────────────────────────────────────────
    [Header("패널")]
    public RectTransform panelCard;

    [Header("루트")]
    [Tooltip("BG_Overlay — 평소 비활성화, ShowGameOver 시 활성화")]
    public GameObject panelRoot;

    // ── 효과음 ────────────────────────────────────────────────────────────────
    [Header("효과음")]
    public SfxType sfxAppear   = SfxType.BankruptcyAppear;   // 패널 등장
    public SfxType sfxTitle    = SfxType.BankruptcyTitle;    // 타이틀 등장
    public SfxType sfxStatRow  = SfxType.BankruptcyStatRow;  // 스탯 행 하나씩
    public SfxType sfxButton   = SfxType.BankruptcyButton;   // 버튼 등장

    private const string GameScene    = "GameScene";
    private const string MainMenuScene = "StartScene";

    // ── 라이프사이클 ──────────────────────────────────────────────────────────

    private void Awake()
    {
        DebtManager.OnGameOver += ShowGameOver;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        DebtManager.OnGameOver -= ShowGameOver;
    }

    // ── 진입점 ────────────────────────────────────────────────────────────────

    private void ShowGameOver(int finalDay, int finalGold)
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (btnRestart  != null) { btnRestart.onClick.RemoveAllListeners();  btnRestart.onClick.AddListener(OnClickRestart); }
        if (btnMainMenu != null) { btnMainMenu.onClick.RemoveAllListeners(); btnMainMenu.onClick.AddListener(OnClickMainMenu); }

        FillTexts(finalDay, finalGold);
        SetAllAlpha(0f);
        StartCoroutine(PlayOpenAnimation());
    }

    // ── 텍스트 세팅 ──────────────────────────────────────────────────────────

    private void FillTexts(int finalDay, int finalGold)
    {
        if (txtDay     != null) txtDay.text     = $"{finalDay}일차에 파산했습니다";
        if (txtBalance != null) txtBalance.text = $"마지막 잔액:  <color=#e74c3c>{finalGold:N0} G</color>";

        if (statValues == null || statValues.Length < 6) return;

        var s = GameStatsTracker.Instance;

        // 최고 잭팟
        string jackpotStr = s == null ? "-" : s.BestJackpot switch
        {
            JackpotTier.Mega    => "<color=#FF4444>💥 MEGA JACKPOT</color>",
            JackpotTier.Jackpot => "<color=#FF8800>🔥 JACKPOT</color>",
            JackpotTier.Combo   => "<color=#FFD700>✨ COMBO</color>",
            _                   => "<color=#888888>없음</color>",
        };

        // 최고가 작물
        string bestCropStr = (s != null && s.BestCropPrice > 0)
            ? $"<color=#FFD700>{s.BestCropName}</color>  " +
              $"[<color=#FF8C00>{s.BestCropGrade}등급</color>]  " +
              $"{s.BestCropPrice:N0} G"
            : "<color=#888888>-</color>";

        SetStat(0, s != null ? $"{s.TotalGoldEarned:N0} G" : "-");
        SetStat(1, s != null ? $"{s.TotalRepaid:N0} G"     : "-");
        SetStat(2, s != null ? $"{s.TotalHarvests} 회"     : "-");
        SetStat(3, s != null ? $"{s.PerfectCount} 회"      : "-");
        SetStat(4, jackpotStr);
        SetStat(5, bestCropStr);
    }

    /// <summary>statValues 배열 요소가 null이어도 안전하게 텍스트를 세팅.</summary>
    private void SetStat(int index, string value)
    {
        if (index < statValues.Length && statValues[index] != null)
            statValues[index].text = value;
    }

    // ── 오픈 애니메이션 ──────────────────────────────────────────────────────

    private IEnumerator PlayOpenAnimation()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        AudioManager.PlaySFX(sfxAppear);
        StartCoroutine(ShakePanel());

        yield return StartCoroutine(FadeIn(GetCg(txtTitle), 0.4f));
        AudioManager.PlaySFX(sfxTitle);
        StartCoroutine(FlickerTitle());

        yield return StartCoroutine(FadeInFromBottom(GetCg(txtSubtitle), 0.3f));
        yield return new WaitForSecondsRealtime(0.1f);
        yield return StartCoroutine(FadeInFromBottom(GetCg(txtDay),      0.3f));
        yield return StartCoroutine(FadeInFromBottom(GetCg(txtBalance),  0.3f));
        yield return new WaitForSecondsRealtime(0.2f);

        // 스탯 Row 순서대로 등장
        if (statValues != null && statValues.Length > 0 && statValues[0] != null)
        {
            Transform statsContainer = statValues[0].transform.parent.parent;
            for (int i = 0; i < statsContainer.childCount; i++)
            {
                CanvasGroup cg = statsContainer.GetChild(i).GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    AudioManager.PlaySFX(sfxStatRow);
                    yield return StartCoroutine(FadeInFromBottom(cg, 0.25f));
                }
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }

        yield return new WaitForSecondsRealtime(0.1f);
        AudioManager.PlaySFX(sfxButton);
        yield return StartCoroutine(FadeInFromBottom(GetCg(btnRestart),  0.3f));
        yield return StartCoroutine(FadeInFromBottom(GetCg(btnMainMenu), 0.2f));
    }

    // ── 코루틴 유틸 ─────────────────────────────────────────────────────────

    private IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = 0f;
        while (elapsed < duration)
        {
            elapsed  += Time.unscaledDeltaTime;
            cg.alpha  = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator FadeInFromBottom(CanvasGroup cg, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = 0f;

        RectTransform rt       = cg.GetComponent<RectTransform>();
        Vector2       origPos  = rt.anchoredPosition;
        Vector2       startPos = origPos + new Vector2(0f, -14f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            float smoothT = 1f - (1f - t) * (1f - t);
            cg.alpha            = t;
            rt.anchoredPosition = Vector2.Lerp(startPos, origPos, smoothT);
            yield return null;
        }
        cg.alpha            = 1f;
        rt.anchoredPosition = origPos;
    }

    private IEnumerator FlickerTitle()
    {
        CanvasGroup cg = GetCg(txtTitle);
        if (cg == null) yield break;
        while (true)
        {
            yield return new WaitForSecondsRealtime(3.5f);
            cg.alpha = 0.85f; yield return new WaitForSecondsRealtime(0.05f);
            cg.alpha = 0.70f; yield return new WaitForSecondsRealtime(0.05f);
            cg.alpha = 0.90f; yield return new WaitForSecondsRealtime(0.05f);
            cg.alpha = 1.00f;
        }
    }

    private IEnumerator ShakePanel()
    {
        if (panelCard == null) yield break;
        Vector2 origPos = panelCard.anchoredPosition;
        float[] shakeX  = { -6f, 6f, -4f, 4f, -2f, 2f, 0f };
        foreach (float x in shakeX)
        {
            panelCard.anchoredPosition = origPos + new Vector2(x, 0f);
            yield return new WaitForSecondsRealtime(0.05f);
        }
        panelCard.anchoredPosition = origPos;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static CanvasGroup GetCg(Component target)
    {
        if (target == null) return null;
        return target.GetComponent<CanvasGroup>()
            ?? target.gameObject.AddComponent<CanvasGroup>();
    }

    private void SetAllAlpha(float alpha)
    {
        foreach (var cg in GetComponentsInChildren<CanvasGroup>(true))
            cg.alpha = alpha;
    }

    // ── 버튼 콜백 ────────────────────────────────────────────────────────────

    private void OnClickRestart()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameScene);
    }

    private void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }
}
