using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 구조물(드론상점 박스, 연못 등) 주변 칸을 농사 불가로 차단한다.
/// Well / ShippingBox와 동일한 static HashSet 패턴.
///
/// 사용법:
///   1. 차단할 오브젝트에 이 컴포넌트 추가
///   2. Grid 연결 (비워두면 자동 탐색)
///   3. Radius로 차단 반경 조절
/// </summary>
public class TileBlocker : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [Tooltip("중심 기준 차단 반경 (칸 수). 1 = 3×3, 2 = 5×5")]
    [SerializeField] private int radius = 1;

    /// <summary>FarmingManager.IsFarmable()이 참조하는 차단 셀 집합</summary>
    public static HashSet<Vector3Int> BlockedCells = new();

    private readonly List<Vector3Int> _myCells = new();

    private void Start()
    {
        if (grid == null) grid = FindFirstObjectByType<Grid>();

        Vector3Int center = grid.WorldToCell(transform.position);
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        {
            Vector3Int cell = center + new Vector3Int(x, y, 0);
            _myCells.Add(cell);
            BlockedCells.Add(cell);
        }
    }

    private void OnDestroy()
    {
        foreach (var cell in _myCells)
            BlockedCells.Remove(cell);
    }

    private void OnDrawGizmos()
    {
        if (grid == null) grid = FindFirstObjectByType<Grid>();
        if (grid == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
        Vector3Int center = grid.WorldToCell(transform.position);
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        {
            Vector3 worldPos = grid.CellToWorld(center + new Vector3Int(x, y, 0))
                             + grid.cellSize * 0.5f;
            Gizmos.DrawWireCube(worldPos, grid.cellSize);
        }
    }
}
