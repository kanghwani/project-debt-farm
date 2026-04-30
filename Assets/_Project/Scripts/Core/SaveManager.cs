using UnityEngine;
using System;

// ── 저장 항목 ────────────────────────────────────────────────────────────────
//   "CurrentDay"            int     현재 날짜
//   "Gold"                  string  보유 골드 (long이라 string으로 직렬화)
//   "Strikes"               int     연체 횟수
//   "WateringCan"           int     물뿌리개 잔량
//   "SelectedSeed"          int     마지막 선택 씨앗 (TileData.Crops enum → int)
//   "Seed_{cropType}"       int     씨앗 종류별 보유 수량
//   "Fert_{fertType}"       int     비료 종류별 보유 수량

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const string KEY_DAY           = "CurrentDay";
    public const string KEY_GOLD          = "Gold";
    public const string KEY_STRIKES       = "Strikes";
    public const string KEY_WATERING_CAN  = "WateringCan";
    public const string KEY_SELECTED_SEED = "SelectedSeed";
    public const string KEY_SEED_PREFIX   = "Seed_";
    public const string KEY_FERT_PREFIX   = "Fert_";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 모든 매니저의 Awake()가 끝난 뒤 Start()에서 로드
        LoadAllData();
    }

    // ── 저장 ─────────────────────────────────────────────────────────────────

    public void SaveAllData()
    {
        SaveTime();
        SaveInventory();
        SaveDebt();

        PlayerPrefs.Save();
        Debug.Log("[SaveManager] 저장 완료.");
    }

    private void SaveTime()
    {
        if (TimeManager.Instance == null) return;
        PlayerPrefs.SetInt(KEY_DAY, TimeManager.Instance.CurrentDay);
    }

    private void SaveInventory()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null) return;

        // 골드 (long → string)
        PlayerPrefs.SetString(KEY_GOLD, inventory.gold.ToString());

        // 물뿌리개 잔량
        PlayerPrefs.SetInt(KEY_WATERING_CAN, inventory.wateringCanLevel);

        // 마지막 선택 씨앗
        PlayerPrefs.SetInt(KEY_SELECTED_SEED, (int)inventory.currentSelectedSeed);

        // 씨앗 종류별 수량 (None 제외)
        foreach (TileData.Crops cropType in Enum.GetValues(typeof(TileData.Crops)))
        {
            if (cropType == TileData.Crops.None) continue;
            inventory.seedInventory.TryGetValue(cropType, out int count);
            PlayerPrefs.SetInt(KEY_SEED_PREFIX + cropType.ToString(), count);
        }

        // 비료 종류별 수량 (None 제외)
        foreach (Fertilizer fertType in Enum.GetValues(typeof(Fertilizer)))
        {
            if (fertType == Fertilizer.None) continue;
            inventory.fertilizerBag.TryGetValue(fertType, out int count);
            PlayerPrefs.SetInt(KEY_FERT_PREFIX + fertType.ToString(), count);
        }
    }

    private void SaveDebt()
    {
        if (DebtManager.Instance == null) return;
        PlayerPrefs.SetInt(KEY_STRIKES, DebtManager.Instance.currentStrikes);
    }

    // ── 불러오기 ─────────────────────────────────────────────────────────────

    public void LoadAllData()
    {
        if (!PlayerPrefs.HasKey(KEY_DAY))
        {
            Debug.Log("[SaveManager] 저장 데이터 없음 → 새 게임으로 시작합니다.");
            return;
        }

        LoadTime();
        LoadInventory();
        LoadDebt();

        Debug.Log("[SaveManager] 불러오기 완료.");
    }

    private void LoadTime()
    {
        if (TimeManager.Instance == null) return;
        int savedDay = PlayerPrefs.GetInt(KEY_DAY, 1);
        TimeManager.Instance.LoadDay(savedDay);
    }

    private void LoadInventory()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null) return;

        // 골드
        string goldStr = PlayerPrefs.GetString(KEY_GOLD, "1000");
        if (long.TryParse(goldStr, out long savedGold))
            inventory.gold = savedGold;

        // 물뿌리개
        inventory.wateringCanLevel = PlayerPrefs.GetInt(KEY_WATERING_CAN, 0);

        // 씨앗
        inventory.seedInventory.Clear();
        foreach (TileData.Crops cropType in Enum.GetValues(typeof(TileData.Crops)))
        {
            if (cropType == TileData.Crops.None) continue;
            string key = KEY_SEED_PREFIX + cropType.ToString();
            int count = PlayerPrefs.GetInt(key, 0);
            if (count > 0)
                inventory.seedInventory[cropType] = count;
        }

        // 마지막 선택 씨앗 복원 (보유 중인 씨앗이어야 함)
        int savedSeedInt = PlayerPrefs.GetInt(KEY_SELECTED_SEED, (int)TileData.Crops.Radish);
        var savedSeed = (TileData.Crops)savedSeedInt;
        inventory.currentSelectedSeed = inventory.seedInventory.ContainsKey(savedSeed)
            ? savedSeed
            : TileData.Crops.Radish;   // 저장된 씨앗이 없으면 Radish로 폴백

        // 비료
        inventory.fertilizerBag.Clear();
        foreach (Fertilizer fertType in Enum.GetValues(typeof(Fertilizer)))
        {
            if (fertType == Fertilizer.None) continue;
            string key = KEY_FERT_PREFIX + fertType.ToString();
            int count = PlayerPrefs.GetInt(key, 0);
            if (count > 0)
                inventory.fertilizerBag[fertType] = count;
        }

        // UI 갱신
        inventory.NotifyInventoryChanged();
    }

    private void LoadDebt()
    {
        if (DebtManager.Instance == null) return;
        int savedStrikes = PlayerPrefs.GetInt(KEY_STRIKES, 0);
        DebtManager.Instance.LoadStrikes(savedStrikes);
    }
}
