// 이 파일 전체가 에디터 전용입니다.
// #if UNITY_EDITOR ~ #endif 안의 코드는 빌드(배포) 시 완전히 제거됩니다.
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem; // 프로젝트 표준 Input System

// 이 클래스의 책임: 플레이 중 테스트를 빠르게 할 수 있는 치트키 모음
// 씬의 빈 오브젝트(DebugHelper)에 붙여서 사용합니다.
//
// ── 단축키 (Shift + 영문자) ───────────────────────────────────────────────
// Shift + G : 강제 게임오버     (Game over)
// Shift + M : 골드 +10000      (Money)
// Shift + K : Strike +1        (striKe)
// Shift + N : 하루 강제 종료   (Next day)
// ─────────────────────────────────────────────────────────────────────────
public class DebugCheats : MonoBehaviour
{
    [Header("치트 ON/OFF")]
    [Tooltip("체크 해제하면 모든 치트키가 비활성화됩니다")]
    public bool cheatsEnabled = true;

    [Header("타일 디버그 오버레이")]
    [Tooltip("체크하면 화면 우상단에 커서 셀 실시간 상태를 표시합니다")]
    public bool showTileDebugOverlay = true;

    private void Update()
    {
        if (!cheatsEnabled) return;

        // Keyboard.current 가 null 이면 키보드가 없는 상태 — 안전 체크
        if (Keyboard.current == null) return;

        // Shift 가 눌려있지 않으면 아무것도 하지 않음
        // isPressed : 현재 프레임에 키를 누르고 있는 동안 계속 true
        bool shiftHeld = Keyboard.current.leftShiftKey.isPressed
                      || Keyboard.current.rightShiftKey.isPressed;
        if (!shiftHeld) return;

        // Shift + G : 강제 게임오버
        // wasPressedThisFrame : 누른 순간 딱 한 프레임만 true (구식 GetKeyDown 과 동일)
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            Debug.Log("[치트] Shift+G : 강제 게임오버");
            DebtManager.Instance?.DEBUG_ForceGameOver();
        }

        // Shift + M : 골드 +10000
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            var inv = FindFirstObjectByType<PlayerInventory>();
            if (inv != null)
            {
                inv.AddGold(10000);
                Debug.Log("[치트] Shift+M : 골드 +10000");
            }
        }
        
        // Shift + T : 시간 +5
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("[치트] Shift+T : 시간 4시간 추가");
            TimeManager.Instance?.DEBUG_ForceTimeSkip();
        }

        // Shift + K : Strike +1

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            DebtManager.Instance?.DEBUG_AddStrike();
        }

        // Shift + N : 하루 강제 종료
        // TimeManager.OnDebtSettlement 는 외부에서 직접 Invoke 불가 (C# event 규칙)
        // → TimeManager 안에 만든 DEBUG_ForceNextDay() 로 우회 호출합니다.
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            Debug.Log("[치트] Shift+N : 하루 강제 종료");
            TimeManager.Instance?.DEBUG_ForceNextDay();
        }
        
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            var inv = FindFirstObjectByType<PlayerInventory>();
            if (inv != null)
            {
                inv.AddFertilizer(Fertilizer.PopcornYeast, 3);
                Debug.Log("[치트] Shift+F : 뻥튀기 비료3");
            }
        }

        // Shift + D : 현재 커서 셀 타일 상태 Console 출력
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            PrintCursorCellDebug();
        }
    }

    // ── 타일 디버그 ────────────────────────────────────────────────────────────

    private void PrintCursorCellDebug()
    {
        if (PlayerFarming.Instance == null)      { Debug.LogWarning("[TileDebug] PlayerFarming 없음"); return; }
        if (FarmingManager.Instance == null)     { Debug.LogWarning("[TileDebug] FarmingManager 없음"); return; }
        if (FarmingInteraction.Instance == null) { Debug.LogWarning("[TileDebug] FarmingInteraction 없음"); return; }

        Vector3Int pos  = PlayerFarming.Instance.CurrentCursorCell;
        var farm        = FarmingManager.Instance;
        var inv         = FindFirstObjectByType<PlayerInventory>();

        bool hasBase      = farm.baseTilemap  != null && farm.baseTilemap.HasTile(pos);
        bool hasFarmTile  = farm.farmTilemap  != null && farm.farmTilemap.HasTile(pos);
        bool inFarmData   = farm.farmData.TryGetValue(pos, out TileData td);
        bool isFarmable   = farm.IsFarmable(pos);
        bool isTilled     = farm.IsTilled(pos);
        bool isWell       = Well.WellCells.Contains(pos);
        bool isBox        = ShippingBox.BoxCells.Contains(pos);
        bool canInteract  = inv != null && FarmingInteraction.Instance.CanInteract(pos, inv.currentSlot, inv);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ TileDebug @ {pos} ═══");
        sb.AppendLine($"  baseTile 있음    : {hasBase}");
        sb.AppendLine($"  farmTile 있음    : {hasFarmTile}");
        sb.AppendLine($"  farmData 등록    : {inFarmData}");
        sb.AppendLine($"  IsFarmable()     : {isFarmable}");
        sb.AppendLine($"  IsTilled()       : {isTilled}");
        sb.AppendLine($"  Well 셀          : {isWell}");
        sb.AppendLine($"  ShippingBox 셀   : {isBox}");
        sb.AppendLine($"  CanInteract()    : {canInteract}");
        if (inv != null)
            sb.AppendLine($"  현재 슬롯        : {inv.currentSlot}");
        if (inFarmData)
        {
            sb.AppendLine($"  ── TileData ──────────");
            sb.AppendLine($"  상태(currentState): {td.currentState}");
            sb.AppendLine($"  작물(cropType)    : {td.cropType}");
            sb.AppendLine($"  isWatered         : {td.isWatered}");
            sb.AppendLine($"  qualityScore      : {td.qualityScore:F1}");
            sb.AppendLine($"  currentTimer      : {td.currentTimer:F1}s");
            sb.AppendLine($"  isRotResistant    : {td.isRotResistant}");
            sb.AppendLine($"  비료 수           : {td.appliedFertilizers?.Count ?? 0}");
            sb.AppendLine($"  태그 수           : {td.activeTags?.Count ?? 0}");
        }
        else
        {
            sb.AppendLine("  (farmData 없음 — 미경작 상태)");
        }
        sb.AppendLine("════════════════════════════");

        Debug.Log(sb.ToString());
    }

    private string GetCursorCellSummary()
    {
        if (PlayerFarming.Instance == null || FarmingManager.Instance == null)
            return "PlayerFarming / FarmingManager 없음";

        Vector3Int pos = PlayerFarming.Instance.CurrentCursorCell;
        var farm = FarmingManager.Instance;
        var inv  = FindFirstObjectByType<PlayerInventory>();

        bool hasBase    = farm.baseTilemap != null && farm.baseTilemap.HasTile(pos);
        bool hasFarm    = farm.farmTilemap != null && farm.farmTilemap.HasTile(pos);
        bool inData     = farm.farmData.TryGetValue(pos, out TileData td);
        bool isFarmable = farm.IsFarmable(pos);
        bool canInteract = inv != null && FarmingInteraction.Instance != null
                        && FarmingInteraction.Instance.CanInteract(pos, inv.currentSlot, inv);

        string state     = inData ? td.currentState.ToString() : "없음";
        string crop      = inData ? td.cropType.ToString() : "-";
        bool   watered   = inData && td.isWatered;
        string slot      = inv != null ? inv.currentSlot.ToString() : "?";

        return $"셀 {pos}\n" +
               $"  baseTile:{YN(hasBase)}  farmTile:{YN(hasFarm)}  farmData:{YN(inData)}\n" +
               $"  Farmable:{YN(isFarmable)}  CanInteract:{YN(canInteract)}  슬롯:{slot}\n" +
               $"  상태:{state}  작물:{crop}  물:{YN(watered)}";
    }

    private static string YN(bool v) => v ? "O" : "X";

    // 화면에 디버그 정보를 표시합니다 (에디터 플레이 모드에서만 보입니다)
    private void OnGUI()
    {
        if (!cheatsEnabled) return;

        // ── 좌하단: 단축키 안내 ────────────────────────────────────────────────
        GUIStyle cheatStyle = new GUIStyle(GUI.skin.label);
        cheatStyle.fontSize         = 13;
        cheatStyle.normal.textColor = new Color(1f, 1f, 0f, 0.75f);

        GUI.Label(new Rect(10, Screen.height - 135, 320, 125),
            "[DEBUG CHEATS]\n" +
            "Shift+G : 강제 게임오버\n" +
            "Shift+M : 골드 +10000\n" +
            "Shift+K : Strike +1\n" +
            "Shift+N : 하루 강제 종료\n" +
            "Shift+T : 시간 4시간 추가\n" +
            "Shift+D : 커서 셀 상태 Console 출력",
            cheatStyle);

        // ── 우상단: 타일 상태 실시간 오버레이 ────────────────────────────────
        if (!showTileDebugOverlay) return;

        string summary = GetCursorCellSummary();

        GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.fontSize         = 12;
        bgStyle.normal.textColor = Color.white;
        bgStyle.alignment        = TextAnchor.UpperLeft;

        // 배경 박스
        float boxW = 310f;
        float boxH = 80f;
        float boxX = Screen.width - boxW - 10f;
        float boxY = 10f;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(new Rect(boxX - 4, boxY - 4, boxW + 8, boxH + 8), "");
        GUI.color = Color.white;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize         = 12;
        labelStyle.normal.textColor = new Color(0.6f, 1f, 0.6f, 1f);
        labelStyle.richText         = false;

        GUI.Label(new Rect(boxX, boxY, boxW, boxH), summary, labelStyle);
    }
}

#endif
