using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    // 내가 들고 있는 장비의 종류 (1번 도구, 2번 씨앗)
    public enum EquipSlot { UniversalHand, Seed }
    public EquipSlot currentSlot = EquipSlot.UniversalHand; // 기본은 만능손

    public long gold = 1000;
    
    [Tooltip("이 무게에 도달하면 속도가 최하로 떨어집니다")] 
    public float maxWeightPenalty = 50.0f;
    public float currentWeight = 0f;

    [Header("Database")] 
    [Tooltip("가격을 알기위해 도감 확인")]
    public Dictionary<TileData.Crops, int> harvestedCrops = new();
    public Dictionary<TileData.Crops, int> seedInventory = new();
    
    public TileData.Crops currentSelectedSeed = TileData.Crops.radish;
    public event Action OnInventoryChanged;

    private void Awake()
    {
        seedInventory.Add(TileData.Crops.radish, 5); // 테스트용 기본 지급
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

    public void AddCrop(TileData.Crops cropType, float weight)
    {
        if (harvestedCrops.ContainsKey(cropType))
            harvestedCrops[cropType]++;
        else
            harvestedCrops.Add(cropType, 1);

        currentWeight += weight;
        OnInventoryChanged?.Invoke();
        Debug.Log($" {cropType} 수확 완료! (+{weight:F1}kg) 현재 무게: {currentWeight:F1}kg");
    }

    public int ShipAllItems()
    {
        int totalEarnings = 0;

        foreach (var item in harvestedCrops)
        {
            // 도서관에 전화해서 물어봅니다.
            CropData cropInfo = DataManager.Instance.GetCrop(item.Key);
            int price = cropInfo != null ? cropInfo.basePrice : 0;
            totalEarnings += item.Value * price;
        }

        gold += totalEarnings;
        harvestedCrops.Clear();
        currentWeight = 0f;
        
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"🛸 드론 출하 완료! {totalEarnings}G 획득. 가방이 가벼워졌습니다!");
        return totalEarnings;
    }
}