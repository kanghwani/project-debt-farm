using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.InputSystem;

public class TimingBarUI : MonoBehaviour
{
    public static TimingBarUI Instance { get; private set; }

    [Header("UI Objects")]
    [SerializeField] private GameObject barContainer;   // 전체 패널 (켜고 끄기)
    [SerializeField] private RectTransform zonesRect;   // Img_Zones (범위 자동 계산용)
    [SerializeField] private RectTransform indicator;   // 흰 다이아몬드 (X 이동)

    [Header("Follow Target")]
    [SerializeField] private Transform followTarget;    // 플레이어 Transform
    [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.5f);  // 캐릭터 위쪽 오프셋
    [SerializeField] private Canvas parentCanvas;       // TimingBarUI가 속한 Canvas

    [Header("Settings")]
    public float cursorSpeed = 450f;
    [Tooltip("인디케이터가 이미지 끝에서 안쪽으로 얼마나 띄울지 (픽셀)")]
    public float edgeMarginPx = 8f;

    // 이동 범위: zonesRect 절반 - 여백
    // 판정 범위: zonesRect 절반 기준 0~1 (이미지 비율 그대로)
    private float moveHalfWidth;   // 인디케이터 실제 이동 범위
    private float zoneHalfWidth;   // 판정 좌표 기준 (= zonesRect 절반)

    [Header("Zone Ranges (0 = 왼쪽 끝, 1 = 오른쪽 끝)")]
    [Tooltip("PERFECT 구간 — 이미지의 노란 영역에 맞게 조정")]
    [SerializeField] private float perfectMin = 0.45f;
    [SerializeField] private float perfectMax = 0.55f;
    [Tooltip("GOOD 구간 — 이미지의 파란 영역에 맞게 조정")]
    [SerializeField] private float goodMin    = 0.30f;
    [SerializeField] private float goodMax    = 0.70f;

    private bool isOperating  = false;
    private bool inputReady   = false;   // 시작 직후 1프레임 입력 무시용
    private float currentPos  = 0f;
    private float direction   = 1f;
    private Action<float> onActionCompleted;
    private ToolType currentTool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        barContainer.SetActive(false);
    }

    private void Start()
    {
        if (zonesRect != null)
        {
            zoneHalfWidth = zonesRect.rect.width * 0.5f;
            moveHalfWidth = zoneHalfWidth - edgeMarginPx;  // 여백만큼 안쪽으로
        }
    }

    public void StartTimingAction(Action<float> callback, ToolType tool = ToolType.Till)
    {
        isOperating       = true;
        inputReady        = false;
        currentPos        = -moveHalfWidth;  // 왼쪽 끝 = BAD 구간 시작
        direction         = 1f;
        onActionCompleted = callback;
        currentTool       = tool;
        barContainer.SetActive(true);
        AudioManager.PlaySFX(SfxType.TimingStart);
        UpdateIndicatorPosition();
    }

    private void Update()
    {
        if (!isOperating) return;

        // 시작 직후 1프레임은 입력 무시 (누른 프레임에 즉시 release 판정 방지)
        if (!inputReady)
        {
            inputReady = true;
            return;
        }

        // 다이아몬드 좌우 왕복
        currentPos += direction * cursorSpeed * Time.deltaTime;
        if (currentPos >= moveHalfWidth)  { currentPos = moveHalfWidth;  direction = -1f; }
        if (currentPos <= -moveHalfWidth) { currentPos = -moveHalfWidth; direction =  1f; }
        UpdateIndicatorPosition();
        UpdateBarPosition();

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            Confirm();
    }

    private void Confirm()
    {
        isOperating = false;
        barContainer.SetActive(false);

        // zoneHalfWidth 기준으로 0~1 정규화 → 이미지 비율과 1:1 대응
        float normalized = (currentPos + zoneHalfWidth) / (zoneHalfWidth * 2f);
        float score      = CalculateScore(normalized);

        // 스페이스를 뗀 순간 즉시 판정 효과음 재생 (ActionFeedback 콜백 전)
        PlayJudgeSFX(score);

        onActionCompleted?.Invoke(score);
    }

    private void PlayJudgeSFX(float score)
    {
        SfxType sfx;
        if (score >= 40f)
            sfx = currentTool == ToolType.Till ? SfxType.TillPerfect : SfxType.WaterPerfect;
        else if (score >= 20f)
            sfx = currentTool == ToolType.Till ? SfxType.TillGood    : SfxType.WaterGood;
        else
            sfx = currentTool == ToolType.Till ? SfxType.TillBad     : SfxType.WaterBad;

        AudioManager.PlaySFX(sfx);
    }

    private void UpdateBarPosition()
    {
        if (followTarget == null || parentCanvas == null) return;

        // 플레이어 월드 좌표 → 스크린 좌표 → 캔버스 로컬 좌표
        Vector3 worldPos  = followTarget.position + (Vector3)worldOffset;
        Vector3 screenPos3 = Camera.main.WorldToScreenPoint(worldPos);

        // 카메라 뒤이거나 Near Clip Plane 위에 있으면 표시 생략
        if (Camera.main == null || screenPos3.z <= 0) return;

        // 화면 범위 안으로 클램프
        Vector2 screenPos = new Vector2(
            Mathf.Clamp(screenPos3.x, 0f, Screen.width),
            Mathf.Clamp(screenPos3.y, 0f, Screen.height)
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            screenPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out Vector2 canvasPos
        );

        barContainer.GetComponent<RectTransform>().anchoredPosition = canvasPos;
    }

    private void UpdateIndicatorPosition()
    {
        if (indicator == null) return;
        var pos = indicator.anchoredPosition;
        pos.x = currentPos;
        indicator.anchoredPosition = pos;
    }

    private float CalculateScore(float t)
    {
        if (t >= perfectMin && t <= perfectMax) return 40f;
        if (t >= goodMin    && t <= goodMax)    return 20f;
        return 0f;
    }
}
