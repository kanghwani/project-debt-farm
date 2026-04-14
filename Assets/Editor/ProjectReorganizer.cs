// Assets/Editor/ProjectReorganizer.cs
// Unity 현업 스타일 폴더 구조 재조직 도구
// Tools > Project Reorganizer 메뉴에서 실행

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectReorganizer
{
    // ──────────────────────────────────────────
    // 이동 목록 미리 보기 (실제 이동 없음)
    // ──────────────────────────────────────────
    [MenuItem("Tools/Project Reorganizer/1. Dry Run (Preview)")]
    public static void DryRun()
    {
        var ops = BuildOperationList();
        int missing = 0;

        Debug.Log($"[ProjectReorganizer] Dry Run — 총 {ops.Count}개 이동 예정");
        foreach (var op in ops)
        {
            bool exists = File.Exists(op.from) || Directory.Exists(op.from);
            if (!exists) missing++;
            Debug.Log($"[{(exists ? "OK" : "MISSING")}] {op.label}\n  FROM: {op.from}\n  TO:   {op.to}");
        }

        Debug.Log($"[ProjectReorganizer] Dry Run 완료 — 총 {ops.Count}개 중 누락 {missing}개\n" +
                  "이상 없으면 'Execute Reorganization'을 실행하세요.");
        EditorUtility.DisplayDialog("Dry Run 완료",
            $"이동 예정: {ops.Count}개\n누락 파일: {missing}개\n\nConsole 창에서 전체 목록을 확인하세요.", "확인");
    }

    // ──────────────────────────────────────────
    // 실제 실행
    // ──────────────────────────────────────────
    [MenuItem("Tools/Project Reorganizer/2. Execute Reorganization")]
    public static void Execute()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "폴더 재구조화 확인",
            "프로젝트 폴더 구조를 재조직합니다.\n\n" +
            "⚠ 실행 전 Git 커밋을 권장합니다.\n\n" +
            "계속하시겠습니까?",
            "실행", "취소");

        if (!confirmed) return;

        var successLog = new List<string>();
        var failLog    = new List<string>();

        // 폴더를 먼저 생성하고 DB에 등록한 뒤 이동 시작
        EnsureFolders();
        AssetDatabase.Refresh();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var op in BuildOperationList())
                ExecuteMove(op, successLog, failLog);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        // _Recovery 삭제
        DeleteAssetIfExists("Assets/_Recovery", successLog, failLog);

        // 빈 폴더 정리
        CleanEmptyFolders(successLog, failLog);

        AssetDatabase.Refresh();
        PrintReport(successLog, failLog);
    }

    // ──────────────────────────────────────────
    // 이동 목록 빌드
    // ──────────────────────────────────────────
    private static List<MoveOp> BuildOperationList()
    {
        var ops = new List<MoveOp>();

        // ---- Scripts/Player ----
        ops.Add(Mv("Assets/01.Script/Animation/PlayerVisuals.cs",
                   "Assets/_Project/Scripts/Player/PlayerVisuals.cs"));
        ops.Add(Mv("Assets/01.Script/Player/PlayerMove2D.cs",
                   "Assets/_Project/Scripts/Player/PlayerMove2D.cs"));
        ops.Add(Mv("Assets/01.Script/Player/PlayerInputReader.cs",
                   "Assets/_Project/Scripts/Player/PlayerInputReader.cs"));
        ops.Add(Mv("Assets/01.Script/Player/PlayerInventory.cs",
                   "Assets/_Project/Scripts/Player/PlayerInventory.cs"));
        ops.Add(Mv("Assets/01.Script/Player/PlayerFarming.cs",
                   "Assets/_Project/Scripts/Player/PlayerFarming.cs"));

        // ---- Scripts/Farming ----
        ops.Add(Mv("Assets/01.Script/Player/FarmingManager.cs",
                   "Assets/_Project/Scripts/Farming/FarmingManager.cs"));
        ops.Add(Mv("Assets/01.Script/Tiles/TileData.cs",
                   "Assets/_Project/Scripts/Farming/TileData.cs"));
        ops.Add(Mv("Assets/01.Script/Shipping/ShippingBox.cs",
                   "Assets/_Project/Scripts/Farming/ShippingBox.cs"));

        // ---- Scripts/Core ----
        ops.Add(Mv("Assets/01.Script/Time/TimeManager.cs",
                   "Assets/_Project/Scripts/Core/TimeManager.cs"));
        ops.Add(Mv("Assets/03.Data/DataManager.cs",
                   "Assets/_Project/Scripts/Core/DataManager.cs"));

        // ---- Scripts/UI ----
        ops.Add(Mv("Assets/01.Script/UI/UiManager.cs",
                   "Assets/_Project/Scripts/UI/UiManager.cs"));
        ops.Add(Mv("Assets/01.Script/UI/ClockUI.cs",
                   "Assets/_Project/Scripts/UI/ClockUI.cs"));

        // ---- Scripts/Data ----
        ops.Add(Mv("Assets/03.Data/CropData.cs",
                   "Assets/_Project/Scripts/Data/CropData.cs"));

        // ---- ScriptableObjects/Crops ----
        string[] crops = { "Crop_Potato", "Crop_Pumpkin", "Crop_Radish", "Crop_Tomato", "Crop_ginseng" };
        foreach (var c in crops)
            ops.Add(Mv($"Assets/03.Data/{c}.asset",
                       $"Assets/_Project/ScriptableObjects/Crops/{c}.asset"));

        // ---- Art/Tiles (Rule Tile + Palettes) ----
        ops.Add(Mv("Assets/01.Script/Tiles/New Rule Tile.asset",
                   "Assets/_Project/Art/Tiles/New Rule Tile.asset"));
        ops.Add(Mv("Assets/Sprite/Tiles/FarmPalette.prefab",
                   "Assets/_Project/Art/Tiles/FarmPalette.prefab"));
        ops.Add(Mv("Assets/Sprite/Tiles/New Tile Palette.prefab",
                   "Assets/_Project/Art/Tiles/New Tile Palette.prefab"));

        // ---- Art/Sprites/Characters ----
        ops.Add(Mv("Assets/Sprite/Player_Sprite.png",
                   "Assets/_Project/Art/Sprites/Characters/Player_Sprite.png"));

        // ---- Art/Sprites/Tiles (소스 이미지들) ----
        // Sprite/ 루트의 Gemini JPEGs
        ops.Add(Mv("Assets/Sprite/Gemini_Generated_Image_eosk6heosk6heosk.jpeg",
                   "Assets/_Project/Art/Sprites/Tiles/Gemini_Generated_Image_eosk6heosk6heosk.jpeg"));
        ops.Add(Mv("Assets/Sprite/Gemini_Generated_Image_iiaa24iiaa24iiaa.jpeg",
                   "Assets/_Project/Art/Sprites/Tiles/Gemini_Generated_Image_iiaa24iiaa24iiaa.jpeg"));
        ops.Add(Mv("Assets/Sprite/Gemini_Generated_Image_wiwsifwiwsifwiws.jpeg",
                   "Assets/_Project/Art/Sprites/Tiles/Gemini_Generated_Image_wiwsifwiwsifwiws.jpeg"));
        // Sprite/Tiles/ 안의 Gemini JPEGs
        ops.Add(Mv("Assets/Sprite/Tiles/Gemini_Generated_Image_gkjrocgkjrocgkjr.jpeg",
                   "Assets/_Project/Art/Sprites/Tiles/Gemini_Generated_Image_gkjrocgkjrocgkjr.jpeg"));
        ops.Add(Mv("Assets/Sprite/Tiles/Gemini_Generated_Image_obfwylobfwylobfw.jpeg",
                   "Assets/_Project/Art/Sprites/Tiles/Gemini_Generated_Image_obfwylobfwylobfw.jpeg"));
        // spr_tileset 소스 PNG
        ops.Add(Mv("Assets/Sprite/Tiles/spr_tileset_sunnysideworld_16px.png",
                   "Assets/_Project/Art/Sprites/Tiles/spr_tileset_sunnysideworld_16px.png"));

        // ---- Art/Tiles (Tile .asset 파일들 — 동적 수집) ----
        // Sprite/Tiles/ 안의 모든 .asset 파일 (Gemini 슬라이스 + spr_tileset 타일들)
        var seen = new HashSet<string>();
        string[] guids = AssetDatabase.FindAssets("t:Object", new[] { "Assets/Sprite/Tiles" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".asset")) continue;
            if (!seen.Add(path)) continue; // 중복 제거

            string fileName = Path.GetFileName(path);
            ops.Add(Mv(path, $"Assets/_Project/Art/Tiles/{fileName}"));
        }

        // ---- Art/Materials ----
        ops.Add(Mv("Assets/Sprite/Materials/Player_Sprite.mat",
                   "Assets/_Project/Art/Materials/Player_Sprite.mat"));

        // ---- Art/Fonts ----
        string[] fontsMain = {
            "MonaS8x12.otf", "MonaS10.otf", "MonaS10-Bold.otf",
            "MonaS10x12.otf", "MonaS10x12-Bold.otf", "MonaS12.otf", "MonaS12-Bold.otf"
        };
        foreach (var f in fontsMain)
            ops.Add(Mv($"Assets/04. Fonts/01_Main/{f}", $"Assets/_Project/Art/Fonts/Main/{f}"));

        string[] fontsEmoji = { "Mona12Emoji.otf", "Mona12ColorEmoji.otf" };
        foreach (var f in fontsEmoji)
            ops.Add(Mv($"Assets/04. Fonts/02_Emoji/{f}", $"Assets/_Project/Art/Fonts/Emoji/{f}"));

        string[] fontsText = {
            "MonaS12TextKR.otf", "MonaS12TextJP.otf", "MonaS12TextHK.otf",
            "MonaS12TextSC.otf", "MonaS12TextTC.otf"
        };
        foreach (var f in fontsText)
            ops.Add(Mv($"Assets/04. Fonts/03_Text/{f}", $"Assets/_Project/Art/Fonts/Text/{f}"));

        ops.Add(Mv("Assets/04. Fonts/naverfont.ttf",
                   "Assets/_Project/Art/Fonts/naverfont.ttf"));
        ops.Add(Mv("Assets/04. Fonts/naverfont SDF.asset",
                   "Assets/_Project/Art/Fonts/naverfont SDF.asset"));

        // ---- Input ----
        ops.Add(Mv("Assets/02.Action/PlayerControls.inputactions",
                   "Assets/_Project/Input/PlayerControls.inputactions"));

        // ---- Scenes (이동 + 이름 변경) ----
        ops.Add(Mv("Assets/Scenes/SampleScene.unity",
                   "Assets/_Project/Scenes/GameScene.unity"));

        return ops;
    }

    // ──────────────────────────────────────────
    // 목적지 폴더 사전 생성
    // ──────────────────────────────────────────
    private static void EnsureFolders()
    {
        string[] folders = {
            "Assets/_Project",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Player",
            "Assets/_Project/Scripts/Farming",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/Scripts/Data",
            "Assets/_Project/ScriptableObjects",
            "Assets/_Project/ScriptableObjects/Crops",
            "Assets/_Project/Art",
            "Assets/_Project/Art/Sprites",
            "Assets/_Project/Art/Sprites/Characters",
            "Assets/_Project/Art/Sprites/Tiles",
            "Assets/_Project/Art/Tiles",
            "Assets/_Project/Art/Materials",
            "Assets/_Project/Art/Fonts",
            "Assets/_Project/Art/Fonts/Main",
            "Assets/_Project/Art/Fonts/Emoji",
            "Assets/_Project/Art/Fonts/Text",
            "Assets/_Project/Input",
            "Assets/_Project/Scenes",
        };

        foreach (var folder in folders)
        {
            if (AssetDatabase.IsValidFolder(folder)) continue;
            int slash  = folder.LastIndexOf('/');
            string par = folder.Substring(0, slash);
            string chd = folder.Substring(slash + 1);
            AssetDatabase.CreateFolder(par, chd);
        }
    }

    // ──────────────────────────────────────────
    // 빈 폴더 정리
    // ──────────────────────────────────────────
    private static void CleanEmptyFolders(List<string> successLog, List<string> failLog)
    {
        string[] candidates = {
            "Assets/01.Script/Animation",
            "Assets/01.Script/Interface",
            "Assets/01.Script/Player",
            "Assets/01.Script/Shipping",
            "Assets/01.Script/Tiles",
            "Assets/01.Script/Time",
            "Assets/01.Script/UI",
            "Assets/01.Script",
            "Assets/02.Action",
            "Assets/03.Data",
            "Assets/04. Fonts/01_Main",
            "Assets/04. Fonts/02_Emoji",
            "Assets/04. Fonts/03_Text",
            "Assets/04. Fonts",
            "Assets/Sprite/Materials",
            "Assets/Sprite/Tiles",
            "Assets/Sprite",
            "Assets/Scenes",
        };

        foreach (var folder in candidates)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            // 폴더가 비어있는지 확인 (하위 파일/폴더 없음)
            string[] remaining = AssetDatabase.FindAssets("", new[] { folder });
            if (remaining.Length == 0)
                DeleteAssetIfExists(folder, successLog, failLog);
        }
    }

    // ──────────────────────────────────────────
    // 유틸리티
    // ──────────────────────────────────────────
    private struct MoveOp
    {
        public string from, to, label;
    }

    private static MoveOp Mv(string from, string to)
    {
        return new MoveOp { from = from, to = to, label = Path.GetFileName(from) + " → " + to };
    }

    private static void ExecuteMove(MoveOp op, List<string> successLog, List<string> failLog)
    {
        if (!File.Exists(op.from) && !Directory.Exists(op.from))
        {
            Debug.LogWarning($"[ProjectReorganizer] SKIP (소스 없음): {op.from}");
            return;
        }

        if (File.Exists(op.to))
        {
            successLog.Add($"[SKIP-EXISTS] {op.label}");
            return;
        }

        string error = AssetDatabase.MoveAsset(op.from, op.to);
        if (string.IsNullOrEmpty(error))
            successLog.Add($"[OK] {op.label}");
        else
        {
            failLog.Add($"[FAIL] {op.label} → {error}");
            Debug.LogError($"[ProjectReorganizer] 이동 실패: {op.label}\n오류: {error}");
        }
    }

    private static void DeleteAssetIfExists(string path, List<string> successLog, List<string> failLog)
    {
        if (!File.Exists(path) && !Directory.Exists(path) && !AssetDatabase.IsValidFolder(path))
            return;

        bool ok = AssetDatabase.DeleteAsset(path);
        if (ok)
            successLog.Add($"[DELETED] {path}");
        else
            failLog.Add($"[DELETE FAIL] {path}");
    }

    private static void PrintReport(List<string> successLog, List<string> failLog)
    {
        Debug.Log($"[ProjectReorganizer] 완료 — 성공 {successLog.Count}건 / 실패 {failLog.Count}건");
        foreach (var s in successLog) Debug.Log(s);
        foreach (var f in failLog)   Debug.LogError(f);

        string msg = failLog.Count == 0
            ? $"재조직 완료!\n\n성공: {successLog.Count}건\n\n" +
              "다음 단계:\n1. Unity Editor 재시작\n2. GameScene 열고 컴포넌트 확인\n3. File > Build Settings에서 씬 재등록"
            : $"일부 실패!\n\n성공: {successLog.Count}건\n실패: {failLog.Count}건\n\nConsole 창을 확인하세요.";

        EditorUtility.DisplayDialog(failLog.Count == 0 ? "완료" : "경고", msg, "확인");
    }
}
