using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dual-Grid 시스템
// placeholderTilemap: 어디가 갈아진 밭인지 마킹 (보이지 않음)
// displayTilemap:     4방향 이웃 조합으로 16종 타일 자동 선택 (0.5칸 오프셋)
public class DualGridFarmTilemap : MonoBehaviour
{
    static readonly Vector3Int[] OFFSETS = {
        new(0, 0, 0), new(1, 0, 0),
        new(0, 1, 0), new(1, 1, 0)
    };

    static Dictionary<(bool, bool, bool, bool), Tile> ruleMap;

    [Header("Tilemaps")]
    public Tilemap placeholderTilemap;
    public Tilemap displayTilemap;

    [Header("Placeholder Marker (보이지 않는 마커)")]
    public Tile tilledMarker;

    [Header("Display Tiles (0~15 순서)")]
    public Tile[] tiles;

    void Start()
    {
        BuildRuleMap();
    }

    void BuildRuleMap()
    {
        ruleMap = new()
        {
            { (false, false, false, false), null      },
            { (true,  true,  true,  false), tiles[13] },
            { (true,  true,  false, true ), tiles[0]  },
            { (true,  false, true,  true ), tiles[8]  },
            { (false, true,  true,  true ), tiles[15] },
            { (true,  false, true,  false), tiles[1]  },
            { (false, true,  false, true ), tiles[11] },
            { (true,  true,  false, false), tiles[3]  },
            { (false, false, true,  true ), tiles[9]  },
            { (true,  false, false, false), tiles[5]  },
            { (false, true,  false, false), tiles[2]  },
            { (false, false, true,  false), tiles[10] },
            { (false, false, false, true ), tiles[7]  },
            { (true,  false, false, true ), tiles[14] },
            { (false, true,  true,  false), tiles[4]  },
            { (true,  true,  true,  true ), tiles[12] },
        };
    }

    bool IsTilled(Vector3Int pos) =>
        placeholderTilemap.GetTile(pos) == tilledMarker;

    Tile CalculateTile(Vector3Int displayPos)
    {
        bool tl = IsTilled(displayPos - OFFSETS[1]);
        bool tr = IsTilled(displayPos - OFFSETS[0]);
        bool bl = IsTilled(displayPos - OFFSETS[3]);
        bool br = IsTilled(displayPos - OFFSETS[2]);

        var key = (tl, tr, bl, br);
        return ruleMap.TryGetValue(key, out Tile t) ? t : null;
    }

    void RefreshAround(Vector3Int placeholderPos)
    {
        for (int i = 0; i < OFFSETS.Length; i++)
        {
            Vector3Int dp = placeholderPos + OFFSETS[i];
            displayTilemap.SetTile(dp, CalculateTile(dp));
        }
    }

    public void SetTilled(Vector3Int pos)
    {
        placeholderTilemap.SetTile(pos, tilledMarker);
        RefreshAround(pos);
    }

    public void SetUntilled(Vector3Int pos)
    {
        placeholderTilemap.SetTile(pos, null);
        RefreshAround(pos);
    }
}
