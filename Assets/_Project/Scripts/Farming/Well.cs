using System.Collections.Generic;
using UnityEngine;

// 우물: 플레이어가 상호작용하면 물통을 가득 채워준다
// Grid 오브젝트를 Inspector에 연결하면 셀 좌표를 자동 등록
public class Well : MonoBehaviour
{
    [SerializeField] private Grid grid;

    [Tooltip("우물 중심 기준 차단/상호작용 반경 (칸 수)")]
    [SerializeField] private int radius = 1;

    // FillWaterAction이 참조하는 우물 셀 좌표 집합
    public static HashSet<Vector3Int> WellCells = new();

    private List<Vector3Int> _myCells = new();

    private void Start()
    {
        if (grid == null) grid = FindFirstObjectByType<Grid>();
        Vector3Int center = grid.WorldToCell(transform.position);

        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        {
            Vector3Int cell = center + new Vector3Int(x, y, 0);
            _myCells.Add(cell);
            WellCells.Add(cell);
        }
    }

    private void OnDestroy()
    {
        foreach (var cell in _myCells)
            WellCells.Remove(cell);
    }

    private void OnDrawGizmos()
    {
        if (grid == null) return;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
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
