using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 커서가 비료 붙은 Seeded/Harvestable 작물 위에 있을 때 태그·배수·예상 보너스를 표시
// PlayerFarming.CurrentCursorCell 을 매 프레임 읽어 갱신
public class CropTooltipUI : MonoBehaviour
{
    public static CropTooltipUI Instance { get; private set; }

    [Header("UI")]
    // Panel 슬롯은 비워두세요 — CanvasGroup으로 자기 자신을 제어합니다
    [SerializeField] private TextMeshProUGUI cropNameText;
    [SerializeField] private TextMeshProUGUI fertTagText;
    [SerializeField] private TextMeshProUGUI multText;

    private CanvasGroup canvasGroup;

    [Header("Follow Settings")]
    [SerializeField] private Vector2 worldOffset = new Vector2(0.6f, 0.6f);
    [SerializeField] private Canvas parentCanvas;

    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // CanvasGroup으로 투명도만 제어 — SetActive 안 씀 (자기 자신을 끄면 LateUpdate도 꺼짐)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        mainCam = Camera.main;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (PlayerFarming.Instance == null || FarmingManager.Instance == null)
        {
            SetVisible(false);
            return;
        }

        Vector3Int cell = PlayerFarming.Instance.CurrentCursorCell;
        FarmingManager farm = FarmingManager.Instance;

        // 씨앗 심겼거나 수확 가능한 칸이면 비료 유무 관계없이 표시
        if (!farm.farmData.TryGetValue(cell, out TileData data)
            || (data.currentState != TileData.TileState.Seeded
             && data.currentState != TileData.TileState.Harvestable))
        {
            SetVisible(false);
            return;
        }

        // 내용 갱신
        RefreshContent(cell, data);

        // 위치: 커서 월드 좌표 → 스크린 → Canvas 로컬
        FollowCursor(cell);
        SetVisible(true);
    }

    void RefreshContent(Vector3Int cell, TileData data)
    {
        // ── 작물 이름 + 예상 등급 ─────────────────────────────────────────────
        string cropName = data.cropType.ToString();
        CropData cd = DataManager.Instance?.GetCrop(data.cropType);
        if (cd != null) cropName = cd.cropName;

        string grade      = QualityToGrade(data.qualityScore);
        string gradeColor = grade switch
        {
            "S" => "#FFD700",
            "A" => "#FF8C00",
            "B" => "#00BFFF",
            _   => "#AAAAAA",
        };
        cropNameText.text = $"{cropName}  <color={gradeColor}>[{grade}등급 예상]</color>";

        // ── 비료 목록 + 유행 태그 ─────────────────────────────────────────────
        if (data.appliedFertilizers.Count > 0)
        {
            var fertLines = new List<string>();
            foreach (var f in data.appliedFertilizers)
            {
                FertilizerData fd = DataManager.Instance?.GetFertilizer(f);
                fertLines.Add(fd != null ? $"<color=#AAFFAA>{fd.displayName}</color>" : f.ToString());
            }
            string tagStr = data.activeTags.Count > 0
                ? "\n태그: " + string.Join(" ", data.activeTags)
                : "";
            fertTagText.text = "비료: " + string.Join(" + ", fertLines) + tagStr;
        }
        else
        {
            fertTagText.text = data.activeTags.Count > 0
                ? "태그: " + string.Join(" ", data.activeTags)
                : "<color=#666666>비료 없음</color>";
        }

        // ── 예상 배수 ─────────────────────────────────────────────────────────
        float fertMult = 1f;
        foreach (var f in data.appliedFertilizers)
        {
            FertilizerData fd = DataManager.Instance?.GetFertilizer(f);
            if (fd != null) fertMult += fd.multAdd;
        }
        float trendMult = TrendManager.Instance != null
            ? TrendManager.Instance.GetMultiplierFor(data.activeTags) : 1f;
        float total = Mathf.Max(0f, fertMult) * trendMult;

        multText.text = total > 1.05f ? $"<color=#FFD700>×{total:F1}</color>" : "";
    }

    /// <summary>qualityScore → 등급 문자열</summary>
    private static string QualityToGrade(float score) => score switch
    {
        >= 80f => "S",
        >= 50f => "A",
        >= 20f => "B",
        _      => "C",
    };

    void FollowCursor(Vector3Int cell)
    {
        if (parentCanvas == null || mainCam == null) return;

        Vector3 worldPos = new Vector3(cell.x + 0.5f + worldOffset.x,
                                       cell.y + 0.5f + worldOffset.y, 0f);
        Vector2 screenPos = mainCam.WorldToScreenPoint(worldPos);

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            ((RectTransform)transform).position = screenPos;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.GetComponent<RectTransform>(),
                screenPos, parentCanvas.worldCamera,
                out Vector2 localPos);
            ((RectTransform)transform).localPosition = localPos;
        }
    }

    void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
    }
}
