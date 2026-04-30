using UnityEngine;
using System.Collections.Generic;

// 책임: 자신에게 붙은 Collider 영역을 계산하여 '물 충전 가능 좌표'로 전역 등록한다.
[RequireComponent(typeof(BoxCollider2D))]
public class WaterSource : MonoBehaviour
{
    // 게임 내 모든 물 충전소(우물, 연못 등)의 좌표를 모아두는 정적(Static) 집합
    public static HashSet<Vector3Int> ValidWaterCells { get; private set; } = new HashSet<Vector3Int>();

    private void Start()
    {
        RegisterWaterCells();
    }

    private void RegisterWaterCells()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        
        Grid grid = FindFirstObjectByType<Grid>(); 

        if (col == null || grid == null) return;

        
        Bounds bounds = col.bounds;
        
        // 월드 좌표를 그리드의 셀 좌표(Vector3Int)로 변환
        Vector3Int minCell = grid.WorldToCell(bounds.min);
        Vector3Int maxCell = grid.WorldToCell(bounds.max);

        
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                ValidWaterCells.Add(new Vector3Int(x, y, 0));
            }
        }
        
        Debug.Log($"[WaterSource] '{gameObject.name}'의 물 충전 좌표 {ValidWaterCells.Count}개 등록 완료.");
    }
}