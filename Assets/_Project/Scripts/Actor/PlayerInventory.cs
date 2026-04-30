using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    // 내가 들고 있는 장비의 종류 (1번 도구, 2번 씨앗, 3번 비료)
    public enum EquipSlot { UniversalHand, Seed, Fertilizer }
    public EquipSlot currentSlot = EquipSlot.UniversalHand; // 기본은 만능손

    public long gold = 1000;

    [Header("Watering Can")]
    public int wateringCanLevel = 0;
    public int wateringCanMax   = 30;
    public bool HasWater()          => wateringCanLevel > 0;
    public bool IsWateringCanFull() => wateringCanLevel >= wateringCanMax;
    public void UseWater()   { wateringCanLevel = Mathf.Max(0, wateringCanLevel - 1); OnInventoryChanged?.Invoke(); }
    public void FillWater()  { wateringCanLevel = wateringCanMax;                     OnInventoryChanged?.Invoke(); }

    [Header("Weight Settings")]
    [Tooltip("이 무게 초과하면 더이상 물건을 주울 수 없습니다")] 
    public float maxCarryCapacity = 50.0f;
    public float currentWeight = 0f;

    [Tooltip("이 무게에 도달하면 이동 속도가 최하로 떨어집니다.")]
    public float maxWeightPenalty = 40.0f;
    
    public List<StackedCrop> heldItems = new List<StackedCrop>();
    
    
    [Header("Database")]
    [Tooltip("가격을 알기위해 도감 확인")]
    public Dictionary<TileData.Crops, int> seedInventory = new();

    [Header("Fertilizer")]
    public Dictionary<Fertilizer, int> fertilizerBag = new();
    public Fertilizer currentSelectedFertilizer = Fertilizer.None;

    public TileData.Crops currentSelectedSeed = TileData.Crops.Radish;
    public event Action OnInventoryChanged;

    private void Awake()
    {
        // 새 게임 기본 지급 — SaveManager.Start()에서 저장 데이터가 있으면
        // LoadInventory()가 seedInventory.Clear() 후 덮어쓰므로 충돌 없음.
        seedInventory[TileData.Crops.Radish] = 30;
    }
    
    private void Start()
    {
        OnInventoryChanged?.Invoke();
    }

    public void BuySeed(TileData.Crops type, int price)
    {
        if (gold >= price)
        {
            gold -= price;
            if (seedInventory.ContainsKey(type)) seedInventory[type]++;
            else seedInventory.Add(type, 1);
            
            // 문자열 버그 수정 완료 ({type}으로 변경)
            Debug.Log($"🛸 드론배송: {type} 씨앗 구매 완료! (잔액 : {gold}G)");
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.Log(" 잔액이 부족합니다!");
        }
    }

    public void CycleSeed()
    {
        // 내 인벤토리에 있는 씨앗 종류들만 리스트로 뽑음
        List<TileData.Crops> ownedSeeds = new List<TileData.Crops>(seedInventory.Keys);
        if (ownedSeeds.Count == 0) return;

        int currentIndex = ownedSeeds.IndexOf(currentSelectedSeed);
        int nextIndex = (currentIndex + 1) % ownedSeeds.Count;
        currentSelectedSeed = ownedSeeds[nextIndex];

        Debug.Log($"🌱 현재 선택된 씨앗: {currentSelectedSeed} (보유: {seedInventory[currentSelectedSeed]}개)");
        OnInventoryChanged?.Invoke();
    }

    // ★ 추가: 현재 선택된 씨앗이 있는지 확인하는 헬퍼 함수
    public bool HasCurrentSeed()
    {
        return seedInventory.ContainsKey(currentSelectedSeed) && seedInventory[currentSelectedSeed] > 0;
    }

    // ★ 추가: 씨앗을 하나 심었을 때 차감하는 함수
    public void ConsumeCurrentSeed()
    {
        if (HasCurrentSeed())
        {
            seedInventory[currentSelectedSeed]--;
            OnInventoryChanged?.Invoke();
        }
    }

    // ── 비료 ─────────────────────────────────────────────────────────────────

    public void AddFertilizer(Fertilizer type, int count = 1)
    {
        if (fertilizerBag.ContainsKey(type)) fertilizerBag[type] += count;
        else fertilizerBag.Add(type, count);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[비료] {type} x{count} 추가. 보유: {fertilizerBag[type]}개");
    }

    public bool HasFertilizer(Fertilizer type) =>
        fertilizerBag.ContainsKey(type) && fertilizerBag[type] > 0;

    public void ConsumeFertilizer(Fertilizer type)
    {
        if (!HasFertilizer(type)) return;
        fertilizerBag[type]--;
        OnInventoryChanged?.Invoke();
    }

    // 3번 키 — 씨앗 순환과 동일한 방식
    public void CycleFertilizer()
    {
        List<Fertilizer> owned = new();
        foreach (var kv in fertilizerBag)
            if (kv.Value > 0) owned.Add(kv.Key);

        if (owned.Count == 0) return;

        int idx = owned.IndexOf(currentSelectedFertilizer);
        currentSelectedFertilizer = owned[(idx + 1) % owned.Count];
        Debug.Log($"[비료] 선택: {currentSelectedFertilizer} (보유: {fertilizerBag[currentSelectedFertilizer]}개)");
        OnInventoryChanged?.Invoke();
    }

    public bool AddCrop(TileData.Crops cropType, float weight, int price, string grade,
        float totalMultiplier = 1f, int flatBonusTotal = 0,
        JackpotTier jackpotTier = JackpotTier.Normal,
        string fertilizerSummary = "", string trendSummary = "")
    {
        // 1. 무게 체크: 지금 주우려는 게 내 체력을 벗어나는가?
        if (currentWeight + weight > maxCarryCapacity)
        {
            Debug.Log(" 너무 무거워서 더 이상 들 수 없습니다! 수레를 이용하세요.");
            return false;
        }

        // 2. 데이터 가져오기 및 포장
        CropData data = DataManager.Instance.GetCrop(cropType);
        if (data == null) return false;

        StackedCrop newItem = new StackedCrop
        {
            data               = data,
            weight             = weight,
            price              = price,
            grade              = grade,
            totalMultiplier    = totalMultiplier,
            flatBonusTotal     = flatBonusTotal,
            jackpotTier        = jackpotTier,
            fertilizerSummary  = fertilizerSummary,
            trendSummary       = trendSummary,
        };

        // 3. 리스트에 추가
        heldItems.Add(newItem);
        currentWeight += weight;
        
        OnInventoryChanged?.Invoke();
        Debug.Log($" {grade} {data.cropName} 수확! 현재 무게: {currentWeight:F1}/{maxCarryCapacity}kg");
        return true;
    }
    
    //  주머니를 비우는 로직 
    public void ClearInventory()
    {
        heldItems.Clear();
        currentWeight = 0f;
        OnInventoryChanged?.Invoke();
        Debug.Log(" 주머니를 비웠습니다. 몸이 가벼워집니다!");
    }

    public void AddGold(int amount)
    {
        gold += amount;
        AudioManager.PlaySFX(SfxType.ShopBuy);
        OnInventoryChanged?.Invoke();
        Debug.Log($"{amount}G 획득 ! 잔액 {gold}G");
    }

    //  빚을 갚거나 물건을 살 때 돈을 차감하는 함수
    public void SpendGold(int amount)
    {
        gold -= amount;
        OnInventoryChanged?.Invoke(); // 내 집이니까 안전하게 알림 울리기!
    }

    // 파산(연체) 시 가진 돈을 전부 몰수하는 함수
    public void ResetGoldToZero()
    {
        gold = 0;
        OnInventoryChanged?.Invoke();
    }

    // SaveManager가 데이터를 복원한 뒤 UI 갱신을 요청할 때 씁니다.
    // 외부에서 OnInventoryChanged 이벤트를 직접 Invoke 할 수 없으므로 (C# event 규칙)
    // 이 메서드로 우회합니다.
    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}