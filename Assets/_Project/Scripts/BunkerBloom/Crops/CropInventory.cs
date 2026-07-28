using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 책임: 수확한 작물 보관 + 식량/전기 변환 미리보기·확정
public class CropInventory : MonoBehaviour
{
    public static CropInventory Instance { get; private set; }

    private readonly Dictionary<CropData, int> _items = new();

    public bool HasItems => _items.Values.Sum() > 0;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Add(CropData crop, int count)
    {
        if (!_items.ContainsKey(crop)) _items[crop] = 0;
        _items[crop] += count;
        BunkerBloomEvents.RaiseInventoryChanged();
    }

    public int GetCount(CropData crop) => _items.TryGetValue(crop, out int n) ? n : 0;
    public IReadOnlyDictionary<CropData, int> GetAll() => _items;

    /// <summary>foodRatio(0~1) 비율로 변환 시 (식량, 전기) 미리보기. 값 변경 없음.</summary>
    public (float food, float power) Preview(float foodRatio)
    {
        float food = 0f, power = 0f;
        foreach (var kv in _items)
        {
            int foodN  = Mathf.RoundToInt(kv.Value * foodRatio);
            int powerN = kv.Value - foodN;
            food  += foodN  * kv.Key.foodPerUnit;
            power += powerN * kv.Key.powerPerUnit;
        }
        return (food, power);
    }

    public void Convert(float foodRatio)
    {
        var (food, power) = Preview(foodRatio);
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Modify(ResourceType.Food,  food);
            ResourceManager.Instance.Modify(ResourceType.Power, power);
        }
        Debug.Log($"[CropInventory] Converted ({foodRatio*100:F0}% food): F+{food:F0}, P+{power:F0}");
        _items.Clear();
        BunkerBloomEvents.RaiseInventoryChanged();
    }
}
