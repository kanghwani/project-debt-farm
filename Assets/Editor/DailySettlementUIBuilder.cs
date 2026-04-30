// Assets/Editor/DailySettlementUIBuilder.cs
// Tools > UI Builder > Build DailySettlement UI

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class DailySettlementUIBuilder
{
    // ── 카드 크기 (Nightcheckmoney 비율 2:3) ─────────────────────────────────
    private const float CARD_W = 540f;
    private const float CARD_H = 810f;

    // ── 레이아웃 Y 기준 (앵커: 카드 top-center, Y 아래로 음수) ────────────────
    // 헤더 타이틀
    private const float TITLE_Y    = -62f;
    private const float TITLE_H    = 52f;

    // 메인 컨텐츠 박스 (점선 아래 ~ 하단 구분선)
    private const float BOX_TOP    = -168f;   // 메인 박스 시작
    private const float BOX_H      = 395f;    // 메인 박스 높이
    private const float BOX_W      = 440f;

    // 확인 버튼 (메인 박스 오른쪽 하단 작은 버튼 영역)
    private const float BTN_Y      = -590f;
    private const float BTN_X      = +108f;   // 오른쪽 정렬
    private const float BTN_W      = 178f;
    private const float BTN_H      = 42f;

    // 하단 푸터 박스 (빚 정보 — Phase 4)
    private const float FOOTER_Y   = -660f;
    private const float FOOTER_H   = 122f;
    private const float FOOTER_W   = 460f;

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/UI Builder/Build DailySettlement UI")]
    public static void Build()
    {
        // ── 0. 기존 존재 확인 ─────────────────────────────────────────────────
        GameObject existing = GameObject.Find("DailySettlementUI");
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "이미 존재합니다",
                "씬에 DailySettlementUI가 이미 있습니다. 삭제하고 다시 만들까요?",
                "덮어쓰기", "취소");
            if (!overwrite) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // ── 1. UIManager 또는 최상단 Canvas 찾기 ────────────────────────────────────────────────────
        GameObject uiManager = GameObject.Find("UIManager");
        Canvas canvas = null;

        if (uiManager != null)
        {
            canvas = uiManager.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            // UIManager가 없거나 Canvas가 안 붙어있으면 씬의 첫 번째 Canvas 사용
            canvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음",
                "씬에 Canvas(또는 UIManager)가 없습니다. 먼저 만들어주세요.", "확인");
            return;
        }

        // ── 2. Nightcheckmoney 스프라이트 로드 ───────────────────────────────
        string spritePath = "Assets/_Project/Art/Sprites/Nightcheckmoney.png";
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (bgSprite == null)
            Debug.LogWarning($"[DailySettlementUIBuilder] 스프라이트를 찾지 못했습니다: {spritePath}\n수동으로 Image에 연결하세요.");

        // ── 3. 루트: DailySettlementUI (풀스트레치 + CanvasGroup) ─────────────
        GameObject rootGO = CreateUIObject("DailySettlementUI", canvas.transform);
        SetFullStretch(rootGO.GetComponent<RectTransform>());
        CanvasGroup rootCG = rootGO.AddComponent<CanvasGroup>();
        rootCG.alpha          = 0f;
        rootCG.interactable   = false;
        rootCG.blocksRaycasts = false;

        // 배경 어둡게 (반투명 오버레이)
        Image dimImg = rootGO.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.72f);

        // ── 4. 카드 (양피지 스프라이트) ──────────────────────────────────────
        GameObject card = CreateUIObject("SettlementCard", rootGO.transform);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta        = new Vector2(CARD_W, CARD_H);

        Image cardImg = card.AddComponent<Image>();
        cardImg.sprite = bgSprite;
        cardImg.type   = Image.Type.Sliced;
        if (bgSprite == null) cardImg.color = new Color(0.80f, 0.72f, 0.55f, 1f);

        // ── 4.5. 네이버 폰트 자동 로드 (텍스트 투명/흐림 방지) ─────────────────────
        TMP_FontAsset customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Art/Fonts/naverfont SDF.asset");
        if (customFont == null) Debug.LogWarning("[DailySettlementUIBuilder] 네이버 폰트를 찾지 못했습니다. 기본 폰트가 적용됩니다.");

        // ── 5. 헤더 타이틀 ───────────────────────────────────────────────────
        CreateTMP(card.transform, "TXT_Title",
            "오늘의 정산", 42f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.30f, 0.18f, 0.08f, 1f),
            new Vector2(0f, TITLE_Y), new Vector2(BOX_W, TITLE_H), customFont);

        // ── 6. PhaseContainer (VLG — Phase1/2/3 세로로 쌓임) ────────────────
        // 메인 박스 전체를 ScrollView로 감싸서 내용이 넘칠 때 스크롤 가능
        GameObject mainScroll = CreateUIObject("MainScrollView", card.transform);
        RectTransform msRT = mainScroll.GetComponent<RectTransform>();
        msRT.anchorMin        = new Vector2(0.5f, 1f);
        msRT.anchorMax        = new Vector2(0.5f, 1f);
        msRT.pivot            = new Vector2(0.5f, 1f);
        msRT.anchoredPosition = new Vector2(0f, BOX_TOP);
        msRT.sizeDelta        = new Vector2(BOX_W, BOX_H);
        mainScroll.AddComponent<Image>().color = Color.clear;
        ScrollRect mainSR = mainScroll.AddComponent<ScrollRect>();
        mainSR.horizontal = false;

        GameObject mainVP = CreateUIObject("Viewport", mainScroll.transform);
        SetFullStretch(mainVP.GetComponent<RectTransform>());
        // Image 컴포넌트는 Mask에만 필요하므로 RectMask2D에는 제거 (또는 안 넣음)
        mainVP.AddComponent<RectMask2D>(); 
        mainSR.viewport = mainVP.GetComponent<RectTransform>();

        // PhaseContainer: 모든 Phase를 담는 VLG 컨테이너
        GameObject phaseContainer = CreateUIObject("PhaseContainer", mainVP.transform);
        RectTransform pcRT = phaseContainer.GetComponent<RectTransform>();
        pcRT.anchorMin = new Vector2(0f, 1f);
        pcRT.anchorMax = new Vector2(1f, 1f);
        pcRT.pivot     = new Vector2(0.5f, 1f);
        pcRT.offsetMin = Vector2.zero;
        pcRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup pcVLG = phaseContainer.AddComponent<VerticalLayoutGroup>();
        pcVLG.spacing                = 0f;
        pcVLG.childForceExpandWidth  = true;
        pcVLG.childForceExpandHeight = false;
        pcVLG.childControlHeight     = true;
        pcVLG.childControlWidth      = true;
        ContentSizeFitter pcCSF = phaseContainer.AddComponent<ContentSizeFitter>();
        pcCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        mainSR.content = pcRT;

        // ── Phase1Panel: 영수증 목록 (253px - 넓게 확장) ─────────────────────────────────
        GameObject p1 = CreateUIObject("Phase1Panel", phaseContainer.transform);
        p1.AddComponent<LayoutElement>().preferredHeight = 253f;

        // 아이템 ScrollView (Phase1 안)
        GameObject scrollView = CreateUIObject("ScrollView", p1.transform);
        RectTransform svRT = scrollView.GetComponent<RectTransform>();
        svRT.anchorMin        = new Vector2(0f, 1f);
        svRT.anchorMax        = new Vector2(1f, 1f);
        svRT.pivot            = new Vector2(0.5f, 1f);
        svRT.anchoredPosition = Vector2.zero;
        svRT.sizeDelta        = new Vector2(0f, 220f); // 스크롤 뷰 확장
        scrollView.AddComponent<Image>().color = Color.clear;
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;

        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        SetFullStretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>(); // URP 2D Stencil 버퍼 문제 원천 차단
        sr.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = true;
        vlg.childControlWidth      = true;
        vlg.padding                = new RectOffset(6, 6, 4, 4);
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRT;

        // 기본 소계 텍스트 (Phase1 하단 고정 -> 누적 금액으로 사용)
        GameObject baseTotalGO = CreateUIObject("BaseTotalText", p1.transform);
        RectTransform btRT = baseTotalGO.GetComponent<RectTransform>();
        btRT.anchorMin        = new Vector2(0f, 0f);
        btRT.anchorMax        = new Vector2(1f, 0f);
        btRT.pivot            = new Vector2(0.5f, 0f);
        btRT.anchoredPosition = new Vector2(0f, 0f);
        btRT.sizeDelta        = new Vector2(0f, 28f);
        TextMeshProUGUI baseTotalTMP = baseTotalGO.AddComponent<TextMeshProUGUI>();
        baseTotalTMP.text      = "누적 금액: +0G";
        baseTotalTMP.fontSize  = 26f;
        baseTotalTMP.fontStyle = FontStyles.Bold;
        baseTotalTMP.alignment = TextAlignmentOptions.Right;
        baseTotalTMP.color     = new Color(0.28f, 0.16f, 0.06f, 1f);
        if (customFont != null) baseTotalTMP.font = customFont;

        // ── Phase2Panel: 최종 수익 (88px, 초기 숨김) ─────────────────────────
        GameObject p2 = CreateUIObject("Phase2Panel", phaseContainer.transform);
        p2.AddComponent<LayoutElement>().preferredHeight = 88f;
        p2.SetActive(false);

        // 구분선
        CreateHRule(p2.transform, new Vector2(0f, 0f), BOX_W - 20f);

        TextMeshProUGUI finalRevTMP = CreateTMP(p2.transform, "FinalRevenueText",
            "오늘 수익  <color=#8B3A00>+0G</color>",
            32f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.28f, 0.16f, 0.06f, 1f),
            new Vector2(0f, -48f), new Vector2(BOX_W - 20f, 60f), customFont);

        // GoldParticle
        GameObject particleGO = new GameObject("GoldParticle");
        particleGO.transform.SetParent(p2.transform, false);
        ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
        var psMain     = ps.main;
        psMain.startColor   = new Color(0.9f, 0.65f, 0.1f, 1f);
        psMain.startSize    = 0.06f;
        psMain.startSpeed   = 2f;
        psMain.maxParticles = 50;
        psMain.duration     = 1f;
        psMain.loop         = false;
        var psEmit  = ps.emission;
        psEmit.rateOverTime = 0;
        psEmit.SetBurst(0, new ParticleSystem.Burst(0f, 40));
        var psShape = ps.shape;
        psShape.enabled   = true;
        psShape.shapeType = ParticleSystemShapeType.Circle;
        psShape.radius    = 0.4f;
        particleGO.GetComponent<ParticleSystemRenderer>().sortingLayerName = "UI";

        // ── 확인 버튼 (메인 박스 우측 하단) ──────────────────────────────
        GameObject btnGO = CreateUIObject("ConfirmButton", card.transform);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 1f);
        btnRT.anchorMax        = new Vector2(0.5f, 1f);
        btnRT.pivot            = new Vector2(0.5f, 1f);
        btnRT.anchoredPosition = new Vector2(BTN_X, BTN_Y);
        btnRT.sizeDelta        = new Vector2(BTN_W, BTN_H);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.36f, 0.22f, 0.08f, 0.85f);
        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.36f, 0.22f, 0.08f, 0.85f);
        cb.highlightedColor = new Color(0.50f, 0.32f, 0.12f, 0.95f);
        cb.pressedColor     = new Color(0.24f, 0.14f, 0.05f, 0.95f);
        btn.colors          = cb;
        btn.targetGraphic   = btnImg;

        GameObject btnLabelGO = CreateUIObject("Text", btnGO.transform);
        SetFullStretch(btnLabelGO.GetComponent<RectTransform>());
        TextMeshProUGUI btnTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text      = "다음 날로 →";
        btnTMP.fontSize  = 26f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color     = new Color(0.95f, 0.88f, 0.70f, 1f);
        if (customFont != null) btnTMP.font = customFont;

        btnGO.SetActive(false);

        // ── Phase3Panel (하단 푸터 박스 — 빚 정보) ───────────────────────
        GameObject p3 = CreatePanel(card.transform, "Phase3Panel",
            new Vector2(0f, FOOTER_Y), new Vector2(FOOTER_W, FOOTER_H));
        p3.SetActive(false);

        TextMeshProUGUI debtAmtTMP = CreateTMP(p3.transform, "DebtAmountText",
            "<color=#8B1A1A>상환액 -0G</color>",
            24f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.55f, 0.10f, 0.10f, 1f),
            new Vector2(0f, -(FOOTER_H * 0.30f)), new Vector2(FOOTER_W - 20f, 38f), customFont);

        TextMeshProUGUI netGoldTMP = CreateTMP(p3.transform, "NetGoldText",
            "수령액: 0G",
            20f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.28f, 0.16f, 0.06f, 1f),
            new Vector2(0f, -(FOOTER_H * 0.65f)), new Vector2(FOOTER_W - 20f, 32f), customFont);

        // ── 11. SettlementItemRow 프리팹 생성 (B안 용) ───────────────────────────────────────────
        string prefabFolder = "Assets/_Project/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

        string prefabPath = prefabFolder + "/SettlementItemRow.prefab";
        GameObject itemRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        // 무조건 최신 설정으로 덮어쓰기 위해 기존 프리팹 삭제
        if (itemRowPrefab != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
            itemRowPrefab = null;
        }

        if (itemRowPrefab == null)
        {
            GameObject rowProto = new GameObject("SettlementItemRow", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(SettlementItemRow), typeof(LayoutElement));
            rowProto.GetComponent<LayoutElement>().minHeight = 50f; // 40f -> 50f로 키움
            TextMeshProUGUI rowTMP = rowProto.GetComponent<TextMeshProUGUI>();
            rowTMP.text      = "작물명  [S등급]  +0G";
            rowTMP.fontSize  = 32f; // 시원하게 키움
            rowTMP.alignment = TextAlignmentOptions.MidlineLeft;
            rowTMP.color     = new Color(0.28f, 0.16f, 0.06f, 1f);
            if (customFont != null) rowTMP.font = customFont;

            itemRowPrefab = PrefabUtility.SaveAsPrefabAsset(rowProto, prefabPath);
            Object.DestroyImmediate(rowProto);
        }

        // ── 12. DailySettlementUI 스크립트 + B안 레퍼런스 연결 ───────────────────
        DailySettlementUI ui = rootGO.GetComponent<DailySettlementUI>()
            ?? rootGO.AddComponent<DailySettlementUI>();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("rootGroup").objectReferenceValue         = rootCG;
        so.FindProperty("phase1Panel").objectReferenceValue       = p1;
        so.FindProperty("itemListContent").objectReferenceValue   = content.transform;
        so.FindProperty("itemRowPrefab").objectReferenceValue     = itemRowPrefab;
        so.FindProperty("baseTotalText").objectReferenceValue     = baseTotalTMP;
        
        so.FindProperty("phase2Panel").objectReferenceValue       = p2;
        so.FindProperty("finalRevenueText").objectReferenceValue  = finalRevTMP;
        so.FindProperty("goldParticle").objectReferenceValue      = ps;
        
        so.FindProperty("phase3Panel").objectReferenceValue       = p3;
        so.FindProperty("debtAmountText").objectReferenceValue    = debtAmtTMP;
        so.FindProperty("netGoldText").objectReferenceValue       = netGoldTMP;
        
        so.FindProperty("confirmButton").objectReferenceValue     = btn;
        so.FindProperty("confirmButtonText").objectReferenceValue = btnTMP;
        so.ApplyModifiedProperties();

        // ── 13. 완료 ──────────────────────────────────────────────────────────
        Undo.RegisterCreatedObjectUndo(rootGO, "Build DailySettlement UI");
        Selection.activeGameObject = card;
        AssetDatabase.SaveAssets();

        Debug.Log("[DailySettlementUIBuilder] Nightcheckmoney 레이아웃으로 생성 완료!");
        EditorUtility.DisplayDialog("완료",
            "밤 정산 UI (양피지 레이아웃) 생성 완료!\n\n" +
            "확인 사항:\n" +
            "1. Nightcheckmoney 스프라이트 자동 연결\n" +
            "2. Sprite Type → Simple (Inspector에서 확인)\n" +
            "3. 폰트 — TMP Font Asset 수동 지정 필요\n" +
            "4. 위치 미세 조정은 상수 BOX_TOP / FOOTER_Y 수정", "확인");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        float fontSize, FontStyles style, TextAlignmentOptions align, Color color,
        Vector2 anchoredPos, Vector2 sizeDelta, TMP_FontAsset font = null)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color     = color;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return go;
    }

    private static void CreateHRule(Transform parent, Vector2 anchoredPos, float width)
    {
        GameObject go = CreateUIObject("Divider", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(width, 1f);
        go.AddComponent<Image>().color = new Color(0.40f, 0.25f, 0.10f, 0.45f);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
