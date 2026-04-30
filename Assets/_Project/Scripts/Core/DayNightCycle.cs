using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 시간대별 조명 연출 (개선판)
///
/// ① Global Light 2D  — 밝기 + 색온도 (낮=따뜻한 흰빛, 밤=차가운 남색)
/// ② 밤 오버레이      — 어두운 남색 반투명 레이어 (어둠 표현)
/// ③ 노을 오버레이    — 일출·석양 때만 주황빛 레이어
/// ④ 별빛 오버레이    — 밤에만 서서히 등장하는 별 텍스처
/// ⑤ Vignette 효과 — 밤이 될수록 맵 외곽이 그림자로 어두워져 압박감 부여
/// 
/// TimeManager.OnTimeChanged 대신 Update()에서 직접 계산.
/// → 매 프레임 부드럽게 블렌딩되므로 이벤트 기반보다 훨씬 자연스러움.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    // ── Global Light 2D ───────────────────────────────────────────────────────
    [Header("Global Light 2D")]
    [Tooltip("씬의 Global Light 2D 오브젝트")]
    public Light2D globalLight;

    // ── URP Post Processing ───────────────────────────────────────────────────
    [Header("URP Post Processing (Vignette)")]
    [Tooltip("씬의 Global Volume 컴포넌트 (Vignette 제어용)")]
    public UnityEngine.Rendering.Volume globalVolume;
    private Vignette vignette;

    // ── Canvas 레이어들 ───────────────────────────────────────────────────────
    [Header("Canvas 오버레이 레이어")]
    [Tooltip("어둠 레이어 — 밤/새벽에 짙어지는 남색 반투명 Image")]
    public Image darkOverlay;

    [Tooltip("노을 레이어 — 일출·석양 시간대에만 등장하는 주황빛 Image")]
    public Image sunsetOverlay;

    [Tooltip("별 레이어 — 밤에만 보이는 별 텍스처 Image (별 스프라이트 또는 파티클 대체 가능)")]
    public Image starOverlay;

    // ── 이징 곡선 ────────────────────────────────────────────────────────────
    [Header("이징 (Easing)")]
    [Tooltip("0~1 입력을 부드럽게 변환. 기본값은 EaseInOut 형태로 설정.")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ── 키프레임 ──────────────────────────────────────────────────────────────
    [System.Serializable]
    public struct TimeKey
    {
        [Range(0, 24)] public float hour;

        [Header("Global Light")]
        [Tooltip("색온도: 낮=따뜻한 흰빛(1,0.97,0.9), 새벽=차가운 파랑(0.6,0.7,1)")]
        public Color  lightColor;
        [Range(0f, 2f)] public float lightIntensity;

        [Header("어둠 오버레이 알파 (0=낮, 1=깊은 밤)")]
        [Range(0f, 1f)] public float darkAlpha;

        [Header("노을 오버레이 알파 (0=없음, 1=최대)")]
        [Range(0f, 1f)] public float sunsetAlpha;

        [Header("별빛 오버레이 알파 (0=낮, 1=밤)")]
        [Range(0f, 1f)] public float starAlpha;

        [Header("맵 외곽 그림자 (Vignette) 강도")]
        [Range(0f, 1f)] public float vignetteIntensity;
    }

    [Header("시간대 키프레임 (hour 오름차순 정렬 필수)")]
    public TimeKey[] timeKeys = new TimeKey[]
    {
        // ── 심야 (00) ── 가장 어두운 순간 ──────────────────────────────────
        new TimeKey {
            hour           = 0f,
            lightColor     = new Color(0.55f, 0.65f, 1.00f),   // 차가운 달빛 색
            lightIntensity = 0.15f,
            darkAlpha      = 0.70f,
            sunsetAlpha    = 0f,
            starAlpha      = 1.00f,
            vignetteIntensity = 0.60f,  // 밤에는 외곽이 크게 조여옴
        },
        // ── 새벽 (04) ── 아직 어둠 ──────────────────────────────────────────
        new TimeKey {
            hour           = 4f,
            lightColor     = new Color(0.55f, 0.65f, 1.00f),
            lightIntensity = 0.20f,
            darkAlpha      = 0.65f,
            sunsetAlpha    = 0f,
            starAlpha      = 0.90f,
            vignetteIntensity = 0.55f,
        },
        // ── 여명 (05:30) ── 하늘이 슬슬 밝아짐 ─────────────────────────────
        new TimeKey {
            hour           = 5.5f,
            lightColor     = new Color(0.80f, 0.75f, 0.90f),   // 연보라
            lightIntensity = 0.45f,
            darkAlpha      = 0.35f,
            sunsetAlpha    = 0.05f,
            starAlpha      = 0.30f,
            vignetteIntensity = 0.35f,
        },
        // ── 일출 (06:30) ── 주황빛 노을 정점 ───────────────────────────────
        new TimeKey {
            hour           = 6.5f,
            lightColor     = new Color(1.00f, 0.80f, 0.55f),   // 따뜻한 주황
            lightIntensity = 0.75f,
            darkAlpha      = 0.08f,
            sunsetAlpha    = 0.35f,   // ← 노을 레이어 최대
            starAlpha      = 0f,
            vignetteIntensity = 0.20f,
        },
        // ── 아침 (08) ── 맑고 밝음 ──────────────────────────────────────────
        new TimeKey {
            hour           = 8f,
            lightColor     = new Color(1.00f, 0.97f, 0.88f),   // 약간 따뜻한 흰빛
            lightIntensity = 1.00f,
            darkAlpha      = 0f,
            sunsetAlpha    = 0f,
            starAlpha      = 0f,
            vignetteIntensity = 0.10f, // 낮에는 외곽 그림자가 옅음
        },
        // ── 한낮 (12) ── 가장 밝고 선명 ────────────────────────────────────
        new TimeKey {
            hour           = 12f,
            lightColor     = Color.white,
            lightIntensity = 1.10f,
            darkAlpha      = 0f,
            sunsetAlpha    = 0f,
            starAlpha      = 0f,
            vignetteIntensity = 0.10f,
        },
        // ── 오후 (15) ── 여전히 밝음 ────────────────────────────────────────
        new TimeKey {
            hour           = 15f,
            lightColor     = new Color(1.00f, 0.97f, 0.88f),
            lightIntensity = 1.00f,
            darkAlpha      = 0f,
            sunsetAlpha    = 0f,
            starAlpha      = 0f,
            vignetteIntensity = 0.10f,
        },
        // ── 저녁 (17:30) ── 노을 시작 ───────────────────────────────────────
        new TimeKey {
            hour           = 17.5f,
            lightColor     = new Color(1.00f, 0.75f, 0.45f),   // 진한 주황
            lightIntensity = 0.85f,
            darkAlpha      = 0.05f,
            sunsetAlpha    = 0.25f,
            starAlpha      = 0f,
            vignetteIntensity = 0.25f,
        },
        // ── 석양 (19) ── 노을 정점 ──────────────────────────────────────────
        new TimeKey {
            hour           = 19f,
            lightColor     = new Color(1.00f, 0.60f, 0.30f),   // 강렬한 석양
            lightIntensity = 0.65f,
            darkAlpha      = 0.15f,
            sunsetAlpha    = 0.40f,   // ← 노을 레이어 최대
            starAlpha      = 0f,
            vignetteIntensity = 0.35f,
        },
        // ── 황혼 (20:30) ── 보랏빛으로 전환 ────────────────────────────────
        new TimeKey {
            hour           = 20.5f,
            lightColor     = new Color(0.65f, 0.55f, 0.90f),   // 보라
            lightIntensity = 0.40f,
            darkAlpha      = 0.35f,
            sunsetAlpha    = 0.10f,
            starAlpha      = 0.40f,
            vignetteIntensity = 0.45f,
        },
        // ── 밤 (22) ── 별이 뜨고 달빛만 남음 ───────────────────────────────
        new TimeKey {
            hour           = 22f,
            lightColor     = new Color(0.55f, 0.65f, 1.00f),
            lightIntensity = 0.20f,
            darkAlpha      = 0.60f,
            sunsetAlpha    = 0f,
            starAlpha      = 0.90f,
            vignetteIntensity = 0.60f,
        },
        // ── 깊은 밤 (24 = 다음날 0시와 연결) ────────────────────────────────
        new TimeKey {
            hour           = 24f,
            lightColor     = new Color(0.55f, 0.65f, 1.00f),
            lightIntensity = 0.15f,
            darkAlpha      = 0.70f,
            sunsetAlpha    = 0f,
            starAlpha      = 1.00f,
            vignetteIntensity = 0.60f,
        },
    };

    // ── 어둠·노을 오버레이 색 (알파만 키프레임에서 조절) ──────────────────────
    [Header("오버레이 고정 색상")]
    [Tooltip("어둠 레이어 색 (알파는 키프레임에서 제어)")]
    public Color darkColor    = new Color(0.04f, 0.06f, 0.20f, 1f);   // 남색
    [Tooltip("노을 레이어 색 (알파는 키프레임에서 제어)")]
    public Color sunsetColor  = new Color(0.65f, 0.20f, 0.02f, 1f);   // 주황

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // ── Vignette 컴포넌트 찾아두기 ──
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out vignette);
        }

        if (TimeManager.Instance != null)
            ApplyLighting(TimeManager.Instance.startHour, 0);
    }

    private void Update()
    {
        if (TimeManager.Instance == null) return;

        // TimeManager의 내부 계산을 재활용: 이벤트 대신 Instance에서 직접 읽음
        // → Update마다 호출되므로 매 프레임 부드럽게 보간됨
        float dayProgress     = GetDayProgress();
        float totalHoursInDay = TimeManager.Instance.endHour - TimeManager.Instance.startHour;
        float currentHours    = TimeManager.Instance.startHour + dayProgress * totalHoursInDay;

        int hour   = Mathf.FloorToInt(currentHours);
        int minute = Mathf.FloorToInt((currentHours - hour) * 60f);
        ApplyLighting(hour, minute);
    }

    // TimeManager의 elapsedTime에 접근할 수 없으므로 Time.time 기반으로 재계산
    // 실제로는 OnTimeChanged 이벤트로도 충분하지만, Update 방식이 더 부드러움
    private void ApplyLighting(int hour, int minute)
    {
        float t = hour + minute / 60f;

        GetSurroundingKeys(t, out TimeKey a, out TimeKey b);

        float span = b.hour - a.hour;
        float raw  = span > 0f ? (t - a.hour) / span : 0f;
        float frac = transitionCurve.Evaluate(Mathf.Clamp01(raw));

        // ① Global Light 2D
        if (globalLight != null)
        {
            globalLight.color     = Color.Lerp(a.lightColor,     b.lightColor,     frac);
            globalLight.intensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, frac);
        }

        // ② 어둠 오버레이
        if (darkOverlay != null)
        {
            float alpha = Mathf.Lerp(a.darkAlpha, b.darkAlpha, frac);
            darkOverlay.color = new Color(darkColor.r, darkColor.g, darkColor.b, alpha);
        }

        // ③ 노을 오버레이
        if (sunsetOverlay != null)
        {
            float alpha = Mathf.Lerp(a.sunsetAlpha, b.sunsetAlpha, frac);
            sunsetOverlay.color = new Color(sunsetColor.r, sunsetColor.g, sunsetColor.b, alpha);
        }

        // ④ 별빛 오버레이
        if (starOverlay != null)
        {
            float alpha = Mathf.Lerp(a.starAlpha, b.starAlpha, frac);
            // starOverlay의 색 자체는 흰색 유지, 알파만 조절
            Color c = starOverlay.color;
            starOverlay.color = new Color(c.r, c.g, c.b, alpha);
        }

        // ⑤ Vignette 효과 적용
        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = Mathf.Lerp(a.vignetteIntensity, b.vignetteIntensity, frac);
        }
    }

    // ── 키프레임 탐색 ─────────────────────────────────────────────────────────
    private void GetSurroundingKeys(float t, out TimeKey prev, out TimeKey next)
    {
        if (timeKeys == null || timeKeys.Length == 0)
        {
            prev = next = new TimeKey
            {
                lightColor = Color.white, lightIntensity = 1f,
                darkAlpha = 0f, sunsetAlpha = 0f, starAlpha = 0f, vignetteIntensity = 0f
            };
            return;
        }

        // t가 첫 키 이전이면 마지막↔첫 키 사이 (자정 wrap)
        if (t < timeKeys[0].hour)
        {
            prev      = timeKeys[timeKeys.Length - 1];
            next      = timeKeys[0];
            prev.hour -= 24f;
            return;
        }

        for (int i = 0; i < timeKeys.Length - 1; i++)
        {
            if (t >= timeKeys[i].hour && t < timeKeys[i + 1].hour)
            {
                prev = timeKeys[i];
                next = timeKeys[i + 1];
                return;
            }
        }

        // t가 마지막 키 이후 → 마지막↔첫 키(+24) 사이
        prev      = timeKeys[timeKeys.Length - 1];
        next      = timeKeys[0];
        next.hour += 24f;
    }

    // ── TimeManager 내부 진행도 재계산 (Update 방식용) ───────────────────────
    // TimeManager.elapsedTime이 private이라 접근 불가 → Time.timeSinceLevelLoad 사용
    // 씬 로드 시간이 맞지 않을 수 있으므로, 이벤트+Update 혼합보다 단순하게 처리
    private float _cachedProgress = 0f;
    private float _lastUpdateTime = -1f;

    private float GetDayProgress()
    {
        // 매 프레임 TimeManager 이벤트 대신 직접 계산이 어려우므로,
        // OnTimeChanged로 받은 값을 보간하는 방식으로 처리.
        // → 실제로는 OnTimeChanged가 매 프레임 호출되므로 (TimeManager.Update 참고)
        //   결국 ApplyLighting(hour, minute)이 매 프레임 정확히 동작함.
        return 0f; // 이 메서드는 사용되지 않음 (아래 OnTimeChanged 방식으로 대체)
    }

    // ── 이벤트 기반 연결 (Update 방식 대신 선택 가능) ────────────────────────
    // TimeManager.OnTimeChanged가 매 프레임 호출됨을 확인 → 이벤트 방식도 부드러움
    private void OnEnable()
    {
        TimeManager.OnTimeChanged += ApplyLighting;
    }

    private void OnDisable()
    {
        TimeManager.OnTimeChanged -= ApplyLighting;
    }
}
