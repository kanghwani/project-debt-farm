using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DualGridEditorTilemap : MonoBehaviour
{
    static readonly Vector3Int[] OFFSETS =
    {
        new(0, 0, 0),
        new(1, 0, 0),
        new(0, 1, 0),
        new(1, 1, 0)
    };

    Dictionary<(bool, bool, bool, bool), TileBase> ruleMap;

#if UNITY_EDITOR
    int lastDataHash;
    bool hasCachedHash;
#endif

    [Header("Tilemaps")]
    public Tilemap dataTilemap;
    public Tilemap displayTilemap;

    [Header("Data Marker")]
    [Tooltip("RoadData tilemap on cells painted with this marker are treated as road.")]
    public TileBase markerTile;

    [Header("Display Tiles (0~15)")]
    [Tooltip("Dual-grid display tiles. Must contain 16 entries.")]
    public TileBase[] tiles;

#if UNITY_EDITOR
    [Header("Editor")]
    [Tooltip("When enabled, repainting RoadData automatically rebuilds RoadDisplay in Edit Mode.")]
    public bool autoRebuildInEditor = true;
#endif

    void OnEnable()
    {
        BuildRuleMap();

#if UNITY_EDITOR
        CacheCurrentDataHash();
#endif
    }

    void OnValidate()
    {
        BuildRuleMap();

#if UNITY_EDITOR
        hasCachedHash = false;
#endif
    }

    void BuildRuleMap()
    {
        if (tiles == null || tiles.Length < 16)
        {
            ruleMap = null;
            return;
        }

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

    bool HasData(Vector3Int pos)
    {
        if (dataTilemap == null)
            return false;

        TileBase tile = dataTilemap.GetTile(pos);
        if (tile == null)
            return false;

        return markerTile == null || tile == markerTile;
    }

    TileBase CalculateTile(Vector3Int displayPos)
    {
        if (ruleMap == null)
            return null;

        bool tl = HasData(displayPos - OFFSETS[1]);
        bool tr = HasData(displayPos - OFFSETS[0]);
        bool bl = HasData(displayPos - OFFSETS[3]);
        bool br = HasData(displayPos - OFFSETS[2]);

        var key = (tl, tr, bl, br);
        return ruleMap.TryGetValue(key, out TileBase tile) ? tile : null;
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Application.isPlaying || !autoRebuildInEditor || dataTilemap == null || displayTilemap == null)
            return;

        int currentHash = CalculateDataHash();
        if (hasCachedHash && currentHash == lastDataHash)
            return;

        RebuildAll();
    }

    int CalculateDataHash()
    {
        if (dataTilemap == null)
            return 0;

        BoundsInt bounds = dataTilemap.cellBounds;
        int hash = 17;

        hash = hash * 31 + bounds.xMin;
        hash = hash * 31 + bounds.xMax;
        hash = hash * 31 + bounds.yMin;
        hash = hash * 31 + bounds.yMax;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                bool hasData = HasData(new Vector3Int(x, y, 0));
                hash = hash * 31 + (hasData ? 1 : 0);
            }
        }

        return hash;
    }

    void CacheCurrentDataHash()
    {
        lastDataHash = CalculateDataHash();
        hasCachedHash = true;
    }
#endif

    [ContextMenu("Rebuild Display Tilemap")]
    public void RebuildAll()
    {
        if (dataTilemap == null || displayTilemap == null)
        {
            Debug.LogWarning("[DualGridEditorTilemap] Tilemap references are missing.", this);
            return;
        }

        BuildRuleMap();
        if (ruleMap == null)
        {
            Debug.LogWarning("[DualGridEditorTilemap] 16 display tiles are required.", this);
            return;
        }

        BoundsInt bounds = dataTilemap.cellBounds;
        displayTilemap.ClearAllTiles();

        for (int x = bounds.xMin - 1; x <= bounds.xMax + 1; x++)
        {
            for (int y = bounds.yMin - 1; y <= bounds.yMax + 1; y++)
            {
                Vector3Int displayPos = new(x, y, 0);
                TileBase tile = CalculateTile(displayPos);
                if (tile != null)
                    displayTilemap.SetTile(displayPos, tile);
            }
        }

#if UNITY_EDITOR
        CacheCurrentDataHash();
        EditorUtility.SetDirty(displayTilemap);
#endif
    }
}
