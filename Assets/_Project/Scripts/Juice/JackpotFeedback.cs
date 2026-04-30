using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 잭팟 티어별 카메라 흔들림 + 슬로모션 + 스크린 플래시
public class JackpotFeedback : MonoBehaviour
{
    public static JackpotFeedback Instance { get; private set; }

    [Header("Camera Shake — Combo (2~5x)")]
    [SerializeField] private float shakeComboStrength   = 0.08f;
    [SerializeField] private float shakeComboDuration   = 0.25f;

    [Header("Camera Shake — Jackpot (5~10x)")]
    [SerializeField] private float shakeJackpotStrength = 0.22f;
    [SerializeField] private float shakeJackpotDuration = 0.4f;

    [Header("Camera Shake — Mega (10x+)")]
    [SerializeField] private float shakeMegaStrength    = 0.45f;
    [SerializeField] private float shakeMegaDuration    = 0.55f;

    [Header("Slow Motion (Mega only)")]
    [SerializeField] private float slowTimeScale        = 0.25f;
    [SerializeField] private float slowDuration         = 0.35f;
    [SerializeField] private float slowRecoverDuration  = 0.5f;

    [Header("Screen Flash")]
    [Tooltip("전체화면 흰 Image (alpha 0에서 시작)")]
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashPeakAlphaCombo   = 0.15f;
    [SerializeField] private float flashPeakAlphaJackpot = 0.35f;
    [SerializeField] private float flashPeakAlphaMega    = 0.65f;
    [SerializeField] private float flashDuration         = 0.4f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (flashImage != null)
            flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    public void TriggerJackpot(JackpotTier tier, Vector3 worldPos)
    {
        switch (tier)
        {
            case JackpotTier.Combo:
                ShakeCamera(shakeComboStrength, shakeComboDuration);
                Flash(flashPeakAlphaCombo);
                break;

            case JackpotTier.Jackpot:
                ShakeCamera(shakeJackpotStrength, shakeJackpotDuration);
                Flash(flashPeakAlphaJackpot);
                break;

            case JackpotTier.Mega:
                ShakeCamera(shakeMegaStrength, shakeMegaDuration);
                Flash(flashPeakAlphaMega);
                SlowMotion();
                break;
            // Normal은 아무 연출 없음
        }
    }

    void ShakeCamera(float strength, float duration)
    {
        if (Camera.main == null) return;
        Camera.main.transform.DOShakePosition(duration, strength, 20, 90f, false, true)
            .SetUpdate(true);
    }

    void Flash(float peakAlpha)
    {
        if (flashImage == null) return;
        flashImage.DOKill();
        flashImage.color = new Color(1f, 1f, 1f, peakAlpha);
        flashImage.DOFade(0f, flashDuration).SetUpdate(true);
    }

    void SlowMotion()
    {
        DOTween.Kill("slowmo");
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        DOTween.To(() => Time.timeScale, x =>
        {
            Time.timeScale = x;
            Time.fixedDeltaTime = 0.02f * x;
        }, 1f, slowRecoverDuration)
            .SetDelay(slowDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .SetId("slowmo");
    }
}
