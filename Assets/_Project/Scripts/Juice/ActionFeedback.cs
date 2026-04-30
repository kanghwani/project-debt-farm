using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 타이밍 바 판정 결과에 따라 HitStop·Squash&Stretch·카메라 흔들림을 실행한다.
/// TillAction / WaterAction에서 onComplete를 여기에 위임해 애니메이션 완료 후 이동 잠금이 풀린다.
/// </summary>
public class ActionFeedback : MonoBehaviour
{
    public static ActionFeedback Instance { get; private set; }

    // ── HitStop ───────────────────────────────────────────────────────────────
    [Header("HitStop (PERFECT 전용)")]
    [SerializeField] private float hitStopScale    = 0.05f;  // 0에 가까울수록 더 강한 경직
    [SerializeField] private float hitStopDuration = 0.06f;  // 경직 유지 시간(초, 실제시간)
    [SerializeField] private float hitStopRecover  = 0.08f;  // 복구 시간(초)

    // ── Squash & Stretch ──────────────────────────────────────────────────────
    [Header("Squash & Stretch")]
    [Tooltip("스쿼시·스트레치를 적용할 플레이어 스프라이트 Transform")]
    [SerializeField] private Transform spriteTransform;

    [Header("GOOD 판정")]
    [SerializeField] private float goodPunchX      =  0.15f; // X 납작해지는 강도
    [SerializeField] private float goodPunchY      = -0.10f; // Y 납작해지는 강도
    [SerializeField] private float goodDuration    =  0.25f;

    [Header("PERFECT 판정")]
    [SerializeField] private float perfectStretchY =  0.30f; // 내리치기 전 Y 스트레치
    [SerializeField] private float perfectPunchX   =  0.35f; // 내리치는 순간 X 납작
    [SerializeField] private float perfectPunchY   = -0.25f;
    [SerializeField] private float perfectDuration =  0.35f;

    // ── Camera Shake (PERFECT 전용) ───────────────────────────────────────────
    [Header("Camera Shake (PERFECT 전용)")]
    [SerializeField] private float shakeStrength   = 0.10f;
    [SerializeField] private float shakeDuration   = 0.20f;
    [SerializeField] private int   shakeVibrato    = 15;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 판정 점수와 도구 종류에 따라 적절한 연출을 실행하고,
    /// 모든 연출이 끝난 뒤 onComplete를 호출한다 (이동 잠금 해제 시점).
    /// </summary>
    /// <param name="score">타이밍 바 판정 점수 (0~100)</param>
    /// <param name="toolSfxPrefix">SfxType에 사용할 도구 접두어 ("Till" / "Water")</param>
    /// <param name="onComplete">연출 완료 후 호출할 콜백 (isPlayerBusy = false)</param>
    public void Play(float score, ToolType tool, Action onComplete)
    {
        if (score >= 40f)
            StartCoroutine(PlayPerfect(tool, onComplete));
        else if (score >= 20f)
            StartCoroutine(PlayGood(tool, onComplete));
        else
            PlayBad(tool, onComplete);
    }

    // ── BAD: 즉시 완료 ────────────────────────────────────────────────────────

    private void PlayBad(ToolType tool, Action onComplete)
    {
        // SFX는 TimingBarUI.Confirm()에서 스페이스 떼는 순간 이미 재생됨
        onComplete?.Invoke();
    }

    // ── GOOD: 가벼운 Squash → 즉시 잠금 해제 (애니메이션은 백그라운드) ──────────

    private IEnumerator PlayGood(ToolType tool, Action onComplete)
    {
        // SFX는 TimingBarUI.Confirm()에서 스페이스 떼는 순간 이미 재생됨
        if (spriteTransform != null)
        {
            spriteTransform.DOKill();
            spriteTransform.DOPunchScale(
                new Vector3(goodPunchX, goodPunchY, 0f),
                goodDuration, 3, 0.5f)
                .OnComplete(RestoreSprite);  // 복구는 백그라운드에서
        }

        // GOOD은 애니메이션 기다리지 않고 즉시 이동 잠금 해제
        onComplete?.Invoke();
        yield break;
    }

    // ── PERFECT: Stretch → HitStop만 기다린 뒤 잠금 해제, Squash·Shake는 백그라운드 ──

    private IEnumerator PlayPerfect(ToolType tool, Action onComplete)
    {
        GameStatsTracker.Instance?.TrackPerfect();
        // SFX는 TimingBarUI.Confirm()에서 스페이스 떼는 순간 이미 재생됨
        if (spriteTransform != null)
        {
            spriteTransform.DOKill();
            // 내리치기 전 Y 스트레치
            spriteTransform.DOScaleY(1f + perfectStretchY, perfectDuration * 0.3f)
                .SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(perfectDuration * 0.3f);

        // HitStop — DOTween ID "timescale" (JackpotFeedback와 공유)
        DOTween.Kill("timescale");
        Time.timeScale      = hitStopScale;
        Time.fixedDeltaTime = 0.02f * hitStopScale;

        // HitStop 복구 (실제 시간 기준)
        DOTween.To(
            () => Time.timeScale,
            x  => { Time.timeScale = x; Time.fixedDeltaTime = 0.02f * x; },
            1f, hitStopRecover)
            .SetDelay(hitStopDuration)
            .SetUpdate(true)
            .SetId("timescale");

        yield return new WaitForSecondsRealtime(hitStopDuration + hitStopRecover);

        // ── 여기서 이동 잠금 해제 (HitStop 끝나자마자 플레이어 자유) ──────────
        onComplete?.Invoke();

        // 이하 연출은 백그라운드에서 계속 (이동에 영향 없음)
        if (spriteTransform != null)
        {
            spriteTransform.DOKill();
            spriteTransform.DOPunchScale(
                new Vector3(perfectPunchX, perfectPunchY, 0f),
                perfectDuration, 4, 0.5f)
                .OnComplete(RestoreSprite);
        }

        // Camera Shake (백그라운드)
        if (Camera.main != null)
        {
            Camera.main.transform
                .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                .SetUpdate(true);
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private void RestoreSprite()
    {
        if (spriteTransform == null) return;
        spriteTransform.DOKill();
        spriteTransform.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBack);
    }
}

/// <summary>ActionFeedback에서 도구 종류를 구분하는 enum</summary>
public enum ToolType { Till, Water }
