using System.Collections.Generic;
using UnityEngine;

// 타이밍 점수에 따라 영향받는 타일 좌표 목록을 반환하는 헬퍼
public static class FarmActionHelper
{
    // score >= 40 : PERFECT → 3×3 (9칸)
    // score >= 20 : GOOD    → 앞으로 3칸
    // score <  20 : BAD     → 1칸
    public static List<Vector3Int> GetAffectedPositions(Vector3Int origin, Vector2Int facing, float score)
    {
        var list = new List<Vector3Int> { origin };

        if (score >= 40f)
        {
            // 3×3 전체 (origin 이미 추가됨)
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                list.Add(origin + new Vector3Int(x, y, 0));
            }
        }
        else if (score >= 20f)
        {
            // 바라보는 방향으로 2칸 더 (총 3칸)
            var dir = new Vector3Int(facing.x, facing.y, 0);
            list.Add(origin + dir);
            list.Add(origin + dir * 2);
        }

        return list;
    }
}
