// Assets/Editor/GameOverUIBuilder.cs
// Tools > UI Builder > Build GameOver UI 실행하면 씬에 게임오버 패널이 자동 생성됩니다.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class GameOverUIBuilder
{
    // Row 정의: 아이콘 / 레이블
    private static readonly (string icon, string label)[] RowDefs =
    {
        ("💰", "총 획득 골드"),
        ("📋", "누적 상환액"),
        ("🌾", "총 수확 횟수"),
        ("✨", "PERFECT 횟수"),
        ("🏆", "최고 잭팟"),
        ("🌟", "최고가 작물"),
    };

    [MenuItem("Tools/UI Builder/Build GameOver UI")]
    public static void Build()
    {
        // ── 0. 기존 패널 존재 확인 ────────────────────────────────────────
        GameObject existing = GameObject.Find("BG_Overlay");
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "이미 존재합니다",
                "씬에 BG_Overlay가 이미 있습니다. 삭제하고 다시 만들까요?",
                "덮어쓰기", "취소");
            if (!overwrite) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // ── 1. UIManager 또는 최상단 Canvas 찾기 ─────────────────────────────────────────
        GameObject uiManager = GameObject.Find("UIManager");
        Canvas canvas = null;

        if (uiManager != null)
        {
            canvas = uiManager.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // ── 2. BG_Overlay (panelRoot) ─────────────────────────────────────
        GameObject bgOverlay = CreateUIObject("BG_Overlay", canvas.transform);
        SetFullStretch(bgOverlay.GetComponent<RectTransform>());
        Image bgImage = bgOverlay.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.03f, 0.02f, 0.92f);
        bgOverlay.SetActive(false);

        // ── 3. Panel_Card ─────────────────────────────────────────────────
        GameObject card = CreateUIObject("Panel_Card", bgOverlay.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRect.pivot            = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta        = new Vector2(540f, 580f);

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.067f, 0.055f, 0.039f, 1f);

        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor    = new Color(0.75f, 0.15f, 0.15f, 0.8f);
        cardOutline.effectDistance = new Vector2(3f, -3f);

        // ── 4. TXT_Title ──────────────────────────────────────────────────
        TextMeshProUGUI titleTMP = CreateLabel(card.transform, "TXT_Title",
            "파 산 선 고", 46f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.90f, 0.20f, 0.20f, 1f),
            new Vector2(0f, -28f), new Vector2(460f, 64f));
        titleTMP.gameObject.AddComponent<CanvasGroup>();

        // ── 5. TXT_Subtitle ───────────────────────────────────────────────
        TextMeshProUGUI subtitleTMP = CreateLabel(card.transform, "TXT_Subtitle",
            "HARVEST OR DIE", 16f, FontStyles.Normal, TextAlignmentOptions.Center,
            new Color(0.55f, 0.30f, 0.30f, 1f),
            new Vector2(0f, -96f), new Vector2(460f, 36f));
        subtitleTMP.gameObject.AddComponent<CanvasGroup>();

        // ── 6. Divider_Top ────────────────────────────────────────────────
        CreateDivider(card.transform, "Divider_Top", new Vector2(0f, -136f));

        // ── 7. TXT_Day ────────────────────────────────────────────────────
        TextMeshProUGUI dayTMP = CreateLabel(card.transform, "TXT_Day",
            "3일차에 파산했습니다", 22f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.85f, 0.75f, 0.65f, 1f),
            new Vector2(0f, -155f), new Vector2(460f, 34f));
        dayTMP.gameObject.AddComponent<CanvasGroup>();

        // ── 8. TXT_Balance ────────────────────────────────────────────────
        TextMeshProUGUI balTMP = CreateLabel(card.transform, "TXT_Balance",
            "마지막 잔액:  <color=#e74c3c>0 G</color>", 18f, FontStyles.Normal,
            TextAlignmentOptions.Center, new Color(0.70f, 0.62f, 0.53f, 1f),
            new Vector2(0f, -190f), new Vector2(460f, 28f));
        balTMP.gameObject.AddComponent<CanvasGroup>();

        // ── 9. Stats_Container ────────────────────────────────────────────
        GameObject statsContainer = CreateUIObject("Stats_Container", card.transform);
        RectTransform statsRect = statsContainer.GetComponent<RectTransform>();
        statsRect.anchorMin        = new Vector2(0.5f, 1f);
        statsRect.anchorMax        = new Vector2(0.5f, 1f);
        statsRect.pivot            = new Vector2(0.5f, 1f);
        statsRect.anchoredPosition = new Vector2(0f, -225f);
        statsRect.sizeDelta        = new Vector2(460f, 0f);

        VerticalLayoutGroup vlg = statsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = true;
        vlg.childControlWidth      = true;

        ContentSizeFitter csf = statsContainer.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── 10. Row 6개 생성 ─────────────────────────────────────────────
        TextMeshProUGUI[] statValueTMPs = new TextMeshProUGUI[RowDefs.Length];
        for (int i = 0; i < RowDefs.Length; i++)
            statValueTMPs[i] = CreateStatRow(statsContainer.transform, RowDefs[i].icon, RowDefs[i].label);

        // ── 11. Divider_Bot ───────────────────────────────────────────────
        CreateDivider(card.transform, "Divider_Bot", new Vector2(0f, -462f));

        // ── 12. Btn_Group ─────────────────────────────────────────────────
        GameObject btnGroup = CreateUIObject("Btn_Group", card.transform);
        RectTransform btnGroupRect = btnGroup.GetComponent<RectTransform>();
        btnGroupRect.anchorMin        = new Vector2(0.5f, 1f);
        btnGroupRect.anchorMax        = new Vector2(0.5f, 1f);
        btnGroupRect.pivot            = new Vector2(0.5f, 1f);
        btnGroupRect.anchoredPosition = new Vector2(0f, -480f);
        btnGroupRect.sizeDelta        = new Vector2(460f, 60f);

        HorizontalLayoutGroup hlg = btnGroup.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 16f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        Button restartBtn  = CreateButton(btnGroup.transform, "BTN_Restart",  "다시 시작",
            new Color(0.75f, 0.15f, 0.15f, 1f), Color.white);
        Button mainMenuBtn = CreateButton(btnGroup.transform, "BTN_MainMenu", "메인 메뉴",
            new Color(0.22f, 0.18f, 0.18f, 1f), new Color(0.80f, 0.70f, 0.60f, 1f));

        // ── 13. BankruptcyScreen + GameStatsTracker 부착 및 레퍼런스 연결 ─
        GameObject managerGO = GameObject.Find("GameOverManager")
            ?? new GameObject("GameOverManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Create GameOverManager");

        BankruptcyScreen screen = managerGO.GetComponent<BankruptcyScreen>()
            ?? managerGO.AddComponent<BankruptcyScreen>();
        if (managerGO.GetComponent<GameStatsTracker>() == null)
            managerGO.AddComponent<GameStatsTracker>();

        SerializedObject so = new SerializedObject(screen);
        so.FindProperty("panelRoot").objectReferenceValue   = bgOverlay;
        so.FindProperty("panelCard").objectReferenceValue   = cardRect;
        so.FindProperty("txtTitle").objectReferenceValue    = titleTMP;
        so.FindProperty("txtSubtitle").objectReferenceValue = subtitleTMP;
        so.FindProperty("txtDay").objectReferenceValue      = dayTMP;
        so.FindProperty("txtBalance").objectReferenceValue  = balTMP;
        so.FindProperty("btnRestart").objectReferenceValue  = restartBtn;
        so.FindProperty("btnMainMenu").objectReferenceValue = mainMenuBtn;

        SerializedProperty arr = so.FindProperty("statValues");
        arr.arraySize = statValueTMPs.Length;
        for (int i = 0; i < statValueTMPs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = statValueTMPs[i];

        so.ApplyModifiedProperties();

        // ── 14. 완료 ──────────────────────────────────────────────────────
        Undo.RegisterCreatedObjectUndo(bgOverlay, "Build GameOver UI");
        Selection.activeGameObject = managerGO;

        Debug.Log("[GameOverUIBuilder] BankruptcyScreen UI 생성 완료!");
        EditorUtility.DisplayDialog("완료",
            "게임오버 UI가 생성됐습니다!\n\n" +
            "확인 사항:\n" +
            "1. GameOverManager — BankruptcyScreen + GameStatsTracker 부착\n" +
            "2. BG_Overlay — Canvas 하위, 기본 비활성\n" +
            "3. Inspector — 레퍼런스 자동 연결 완료\n" +
            "4. 폰트 — 각 TMP에 프로젝트 Font Asset 수동 지정 필요", "확인");
    }

    // ── 헬퍼: TMP 레이블 ─────────────────────────────────────────────────────
    private static TextMeshProUGUI CreateLabel(
        Transform parent, string name, string text,
        float fontSize, FontStyles style, TextAlignmentOptions align, Color color,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color     = color;
        return tmp;
    }

    // ── 헬퍼: 구분선 ─────────────────────────────────────────────────────────
    private static void CreateDivider(Transform parent, string name, Vector2 anchoredPos)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(460f, 2f);
        go.AddComponent<Image>().color = new Color(0.75f, 0.15f, 0.15f, 0.5f);
    }

    // ── 헬퍼: 스탯 Row ────────────────────────────────────────────────────────
    // Row (HLG + CanvasGroup)
    //   ├── TXT_Icon
    //   ├── TXT_Label
    //   └── TXT_Value  ← 반환
    private static TextMeshProUGUI CreateStatRow(Transform parent, string icon, string label)
    {
        string rowName = "Row_" + label.Replace(" ", "");
        GameObject row = CreateUIObject(rowName, parent);
        row.AddComponent<CanvasGroup>();

        LayoutElement rowLE   = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 32f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 0f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlHeight     = true;
        hlg.childControlWidth      = false;
        hlg.padding                = new RectOffset(8, 8, 0, 0);

        // 아이콘
        GameObject iconGO = CreateUIObject("TXT_Icon", row.transform);
        iconGO.AddComponent<LayoutElement>().preferredWidth = 36f;
        TextMeshProUGUI iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = icon;
        iconTMP.fontSize  = 18f;
        iconTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // 레이블
        GameObject labelGO = CreateUIObject("TXT_Label", row.transform);
        LayoutElement labelLE  = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 200f;
        labelLE.flexibleWidth  = 1f;
        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = label;
        labelTMP.fontSize  = 16f;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.color     = new Color(0.55f, 0.50f, 0.43f, 1f);

        // 값
        GameObject valueGO = CreateUIObject("TXT_Value", row.transform);
        valueGO.AddComponent<LayoutElement>().preferredWidth = 160f;
        TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
        valueTMP.text      = "---";
        valueTMP.fontSize  = 16f;
        valueTMP.fontStyle = FontStyles.Bold;
        valueTMP.alignment = TextAlignmentOptions.MidlineRight;
        valueTMP.color     = new Color(0.78f, 0.72f, 0.60f, 1f);

        return valueTMP;
    }

    // ── 헬퍼: 버튼 ──────────────────────────────────────────────────────────
    private static Button CreateButton(Transform parent, string name, string label,
        Color bgColor, Color textColor)
    {
        GameObject btn = CreateUIObject(name, parent);
        btn.AddComponent<CanvasGroup>();

        Image btnImage = btn.AddComponent<Image>();
        btnImage.color = bgColor;

        Outline outline = btn.AddComponent<Outline>();
        outline.effectColor    = new Color(1f, 1f, 1f, 0.12f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button         = btn.AddComponent<Button>();
        ColorBlock cb         = button.colors;
        cb.normalColor        = bgColor;
        cb.highlightedColor   = bgColor * 1.3f;
        cb.pressedColor       = bgColor * 0.7f;
        button.colors         = cb;
        button.targetGraphic  = btnImage;

        GameObject textGO = CreateUIObject("Label", btn.transform);
        SetFullStretch(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = textColor;

        return button;
    }

    // ── 공통 유틸 ────────────────────────────────────────────────────────────
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
