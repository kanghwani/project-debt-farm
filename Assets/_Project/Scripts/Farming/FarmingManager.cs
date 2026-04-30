using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance { get; private set; }

    [Header("Tilemaps")]
    public Tilemap baseTilemap;  // 잔디 맵 (괭이질 가능 여부 판단용)
    public Tilemap farmTilemap;  // 밭 데이터 맵 (DualGrid placeholder)

    [Header("Dual Grid")]
    [Tooltip("DualGridFarmTilemap 컴포넌트가 붙은 오브젝트")]
    public DualGridFarmTilemap dualGrid;

    [Header("Tiles")]
    [Tooltip("작물이 죽었을 때 표시용 (임시 - CropBehaviour 도입 후 제거)")]
    public TileBase deadTile;

    [Header("Water Overlay")]
    [Tooltip("물 준 칸 위에 얹는 반투명 레이어 (Order in Layer > FarmDisplay)")]
    public Tilemap waterOverlayTilemap;
    [Tooltip("물 표시용 단색 타일 (tilledMarker 재사용 가능)")]
    public Tile waterOverlayTile;

    [Header("Crop Visual")]
    [Tooltip("씨앗/작물 아이콘 표시 레이어 (Order in Layer 최상단)")]
    public Tilemap cropVisualTilemap;

    [Header("Fertilizer VFX")]
    [Tooltip("비료 파티클 프리팹 (ParticleSystem 포함, 색상을 startColor로 받음)")]
    public GameObject fertilizerVFXPrefab;

    [Header("Tutorial / Pre-placed Tiles")]
    [Tooltip("게임 시작 시 미리 갈려있을 밭 칸 좌표. Inspector에서 직접 입력하거나 씬뷰에서 확인하세요.")]
    public Vector3Int[] initialTilledCells = System.Array.Empty<Vector3Int>();

    public Dictionary<Vector3Int, TileData> farmData { get; private set; } = new();
    public List<Vector3Int> activeCrops { get; private set; } = new();

    // 셀 위치 → 활성 VFX 오브젝트
    private readonly Dictionary<Vector3Int, GameObject> activeVFX = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PlaceInitialTilledCells();
    }

    /// <summary>
    /// Inspector에 등록된 initialTilledCells 좌표를 게임 시작 시 자동으로 갈아놓는다.
    /// FarmingManager.Till() 과 동일한 경로를 사용하므로 DualGrid 시각, farmData 모두 정상 등록된다.
    /// </summary>
    private void PlaceInitialTilledCells()
    {
        if (initialTilledCells == null || initialTilledCells.Length == 0) return;

        foreach (var pos in initialTilledCells)
        {
            // 이미 등록되어 있거나 잔디 없는 칸은 건너뜀
            if (farmData.ContainsKey(pos)) continue;
            if (!baseTilemap.HasTile(pos)) continue;

            // DualGrid 시각 적용 (placeholder + display)
            dualGrid.SetTilled(pos);

            // farmData 등록 (qualityScore 0 = C등급 수준으로 시작)
            farmData[pos] = new TileData
            {
                currentState = TileData.TileState.Tilled,
                qualityScore = 0f
            };
        }

        Debug.Log($"[FarmingManager] 튜토리얼 밭 {initialTilledCells.Length}칸 사전 배치 완료");
    }

    /// <summary>
    /// Editor 전용: 현재 farmData에 등록된 모든 셀 좌표를 Console에 출력.
    /// Inspector 우클릭 → "현재 farmData 셀 좌표 출력" 으로 실행.
    /// </summary>
    [ContextMenu("현재 farmData 셀 좌표 출력")]
    private void PrintFarmDataPositions()
    {
        if (farmData.Count == 0) { Debug.Log("[FarmingManager] farmData 비어있음"); return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[FarmingManager] farmData {farmData.Count}개:");
        foreach (var kv in farmData)
            sb.AppendLine($"  ({kv.Key.x}, {kv.Key.y}, {kv.Key.z})  state={kv.Value.currentState}");
        Debug.Log(sb.ToString());
    }

    // 잔디 있고, 밭 없고, 특수 오브젝트(우물·출하상자·드론상점·연못 등) 없는 칸 = 괭이질 가능
    public bool IsFarmable(Vector3Int pos) =>
        baseTilemap.HasTile(pos) && !farmTilemap.HasTile(pos)
        && !Well.WellCells.Contains(pos)
        && !ShippingBox.BoxCells.Contains(pos)
        && !TileBlocker.BlockedCells.Contains(pos);

    // 밭 타일 있는 칸 = 물주기/씨앗 가능
    public bool IsTilled(Vector3Int pos) => farmTilemap.HasTile(pos);

    // 작물 아이콘 표시: cropVisualTilemap에 그림 (밭 모양 위에 겹침)
    public void UpdateTileVisual(Vector3Int pos, TileBase tile)
    {
        cropVisualTilemap?.SetTile(pos, tile);
    }

    // 괭이질: DualGrid에 tilled 마킹 → 자동으로 16종 타일 선택
    public void Till(Vector3Int pos)
    {
        dualGrid.SetTilled(pos);
    }

    // 물 주기: 오버레이 레이어에 반투명 타일 배치
    public void SetWatered(Vector3Int pos)
    {
        if (waterOverlayTilemap != null && waterOverlayTile != null)
        {
            waterOverlayTilemap.SetTile(pos, waterOverlayTile);
            waterOverlayTilemap.SetTileFlags(pos, TileFlags.None);
            waterOverlayTilemap.SetColor(pos, new Color(0.3f, 0.45f, 0.7f, 0.3f));
        }
        if (farmData.TryGetValue(pos, out TileData data)) data.isWatered = true;
    }

    // 마름 (다음날 자정 등): 오버레이 타일 제거
    public void SetDry(Vector3Int pos)
    {
        waterOverlayTilemap?.SetTile(pos, null);
        if (farmData.TryGetValue(pos, out TileData data)) data.isWatered = false;
    }

    // 밭 제거: DualGrid에 untilled 마킹 → 잔디로 복구
    public void RemoveTileData(Vector3Int pos)
    {
        farmData.Remove(pos);
        dualGrid.SetUntilled(pos);
        waterOverlayTilemap?.SetTile(pos, null);
        cropVisualTilemap?.SetTile(pos, null);
        activeCrops.Remove(pos);
        ClearFertilizerVFX(pos);
    }

    // ── 비료 VFX ──────────────────────────────────────────────────────────────

    // 비료 적용 시: 기존 VFX 교체 or 신규 생성, 색상 업데이트
    public void SpawnFertilizerVFX(Vector3Int pos, Color color)
    {
        if (fertilizerVFXPrefab == null) return;

        Vector3 worldPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);

        if (activeVFX.TryGetValue(pos, out GameObject existing) && existing != null)
        {
            // 이미 있으면 색만 교체
            var ps = existing.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
            }
        }
        else
        {
            GameObject vfx = Instantiate(fertilizerVFXPrefab, worldPos, Quaternion.identity);
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
            }
            activeVFX[pos] = vfx;
        }
    }

    // 수확/제거 시: VFX 파괴
    public void ClearFertilizerVFX(Vector3Int pos)
    {
        if (activeVFX.TryGetValue(pos, out GameObject vfx))
        {
            if (vfx != null) Destroy(vfx);
            activeVFX.Remove(pos);
        }
    }
}
