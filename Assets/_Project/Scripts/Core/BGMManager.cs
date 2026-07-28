using System.Collections;
using UnityEngine;

/// <summary>
/// BGM 재생·트랙 교체·페이드인/아웃을 전담하는 싱글톤.
/// DontDestroyOnLoad → 씬 전환 시에도 유지.
///
/// 자동 전환 흐름:
///   TimeManager.OnTimeChanged → 시간대별 BGM 전환 (낮/저녁/밤)
///   DailySettlementManager 자정 → 정산 BGM
///   DebtManager.OnGameOver   → 게임오버 BGM
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    // ── BGM 클립 슬롯 (Inspector에서 연결) ───────────────────────────────────
    [Header("── BGM 클립 ──────────────────────────────")]
    [Tooltip("메인메뉴 BGM")]
    [SerializeField] private AudioClip menuClip;

    [Space(4)]
    [Tooltip("낮 BGM (06시 ~ eveningStartHour)")]
    [SerializeField] private AudioClip dayClip;

    [Tooltip("저녁 BGM (eveningStartHour ~ nightStartHour) — 비워두면 낮 BGM 유지")]
    [SerializeField] private AudioClip eveningClip;

    [Tooltip("밤 BGM (nightStartHour ~ loanSharkStartHour)")]
    [SerializeField] private AudioClip nightClip;

    [Space(4)]
    [Tooltip("사채업자 등장 BGM (loanSharkStartHour ~ 자정) — 긴장감 있는 트랙")]
    [SerializeField] private AudioClip loanSharkClip;

    [Space(4)]
    [Tooltip("매일밤 정산 BGM (자정 영수증 화면)")]
    [SerializeField] private AudioClip settlementClip;

    [Tooltip("게임오버/파산 BGM")]
    [SerializeField] private AudioClip gameOverClip;

    // ── 세팅 ─────────────────────────────────────────────────────────────────
    [Header("설정")]
    [SerializeField] private float defaultFadeDuration = 1.5f;
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.5f;

    // 시간대 경계 (인스펙터에서 조절 가능)
    [Header("── 시간대 경계 (24h) ──────────────────────")]
    [SerializeField] private int dayStartHour        = 6;
    [SerializeField] private int eveningStartHour    = 18;
    [SerializeField] private int nightStartHour      = 21;
    [Tooltip("사채업자 등장 시각 — LoanSharkController의 등장 시각과 맞춰주세요")]
    [SerializeField] private int loanSharkStartHour  = 22;

    // ── 내부 ─────────────────────────────────────────────────────────────────
    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _active;
    private AudioSource _standby;
    private Coroutine   _fadeRoutine;

    private bool _settlementActive = false;   // 정산 중이면 시간대 전환 막기
    private bool _gameOverActive   = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSource[] existing = GetComponents<AudioSource>();
        _sourceA = existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>();
        _sourceB = existing.Length > 1 ? existing[1] : gameObject.AddComponent<AudioSource>();
        _sourceA.loop = _sourceB.loop = true;
        _sourceA.volume = masterVolume;
        _sourceB.volume = 0f;
        _active  = _sourceA;
        _standby = _sourceB;
    }

    private void OnEnable()
    {
        // BunkerBloom 전환: TimeManager·DebtManager 의존성 제거
        // 향후 BunkerBloomEvents.OnTurnStarted 구독으로 교체 예정
    }

    private void Start()
    {
        // 게임 씬 진입 시 현재 시각 기준 BGM 자동 재생
        // (OnTimeChanged는 시간이 변할 때만 발사되므로 시작 시점엔 직접 호출해야 함)
        PlayGameBGM();
    }

    private void OnDisable()
    {
        // BunkerBloom 전환: TimeManager·DebtManager 의존성 제거
    }

    // ── 씬별 진입점 (외부에서 호출) ──────────────────────────────────────────

    /// <summary>메인메뉴 씬 진입 시 호출</summary>
    public void PlayMenuBGM()
    {
        _settlementActive = false;
        _gameOverActive   = false;
        Play(menuClip);
    }

    /// <summary>게임 씬 진입 시 호출 — BunkerBloom 전환 후엔 day BGM만 재생.</summary>
    public void PlayGameBGM()
    {
        _settlementActive = false;
        _gameOverActive   = false;
        Play(dayClip);
    }

    /// <summary>자정 정산 시작 시 DailySettlementManager에서 호출</summary>
    public void PlaySettlementBGM()
    {
        _settlementActive = true;
        Play(settlementClip, fadeDuration: 1.0f);
    }

    /// <summary>정산 완료 후 다음날 BGM 복귀</summary>
    public void ResumeGameBGM()
    {
        _settlementActive = false;
        Play(dayClip);
    }

    // ── 시간대 자동 전환 ──────────────────────────────────────────────────────

    private void OnTimeChanged(int hour, int minute)
    {
        // 정산·게임오버 중에는 시간대 전환 무시
        if (_settlementActive || _gameOverActive) return;

        ApplyTimeBasedBGM(hour);
    }

    private void ApplyTimeBasedBGM(int hour)
    {
        AudioClip target;
        string slot;

        // 사채업자 구간 (loanSharkStartHour ~ 자정)
        if (hour >= loanSharkStartHour)
        {
            target = loanSharkClip != null ? loanSharkClip : (nightClip != null ? nightClip : dayClip);
            slot   = "LoanShark";
        }
        // 밤 구간 (nightStartHour ~ loanSharkStartHour)
        else if (hour >= nightStartHour || hour < dayStartHour)
        {
            target = nightClip != null ? nightClip : dayClip;
            slot   = "Night";
        }
        // 저녁 구간
        else if (hour >= eveningStartHour)
        {
            target = eveningClip != null ? eveningClip : dayClip;
            slot   = "Evening";
        }
        // 낮 구간
        else
        {
            target = dayClip;
            slot   = "Day";
        }

        if (target == null)
        {
            Debug.LogWarning($"[BGMManager] {hour}시 → {slot} 슬롯이 비어있고 fallback도 없음! Day Clip이라도 연결하세요.");
            return;
        }

        Play(target);
    }

    private void OnGameOver(int finalDay, int finalGold)
    {
        _gameOverActive = true;
        Play(gameOverClip, fadeDuration: 2.0f);
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>새 트랙으로 크로스페이드. 같은 트랙 재생/페이드 중이면 무시.</summary>
    public void Play(AudioClip clip, float fadeDuration = -1f)
    {
        if (clip == null) return;

        // 가드 1: 이미 활성 소스가 같은 클립이면 무시 (재생 중 여부와 무관)
        if (_active.clip == clip) return;

        // 가드 2: 페이드 중이고 standby가 같은 클립이면 무시 (중복 호출 방지)
        if (_fadeRoutine != null && _standby.clip == clip) return;

        float duration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(CrossFade(clip, duration));
    }

    /// <summary>현재 BGM 페이드아웃 후 정지.</summary>
    public void Stop(float fadeDuration = -1f)
    {
        float duration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOut(_active, duration, stopAfter: true));
    }

    public void SetVolume(float volume)
    {
        masterVolume   = Mathf.Clamp01(volume);
        _active.volume = masterVolume;
    }

    // ── 코루틴 ───────────────────────────────────────────────────────────────

    private IEnumerator CrossFade(AudioClip newClip, float duration)
    {
        var fadeOut = _active;
        var fadeIn  = _standby;

        fadeIn.clip   = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        // ★ 즉시 swap — 페이드가 끝나기 전에도 _active는 새 클립을 가리킨다.
        //   덕분에 매 프레임 호출되는 OnTimeChanged가 Play() 가드에 막혀
        //   CrossFade가 중복 시작되지 않음.
        _active  = fadeIn;
        _standby = fadeOut;

        float elapsed     = 0f;
        float startVolume = fadeOut.volume;

        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            float t        = Mathf.Clamp01(elapsed / duration);
            fadeOut.volume = Mathf.Lerp(startVolume, 0f,           t);
            fadeIn.volume  = Mathf.Lerp(0f,          masterVolume, t);
            yield return null;
        }

        fadeOut.volume = 0f;
        fadeOut.Stop();
        fadeOut.clip   = null;     // 다음 가드 검사를 위해 정리
        fadeIn.volume  = masterVolume;
        _fadeRoutine   = null;
    }

    private IEnumerator FadeOut(AudioSource source, float duration, bool stopAfter)
    {
        float elapsed     = 0f;
        float startVolume = source.volume;

        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            source.volume  = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.volume = 0f;
        if (stopAfter) source.Stop();
    }
}
