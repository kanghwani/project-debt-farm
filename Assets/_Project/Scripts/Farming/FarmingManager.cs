using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FarmingManager : MonoBehaviour
{
    [Header("Tilemaps")] 
    [Tooltip("농사 가능 구역 마스크 (Tilemap_Farm)")] 
    [SerializeField] private Tilemap farmTilemap;

    [Header("Tiles")] 
    [SerializeField] private TileBase tilledTile;
    [SerializeField] private TileBase seededTile;
    [SerializeField] private TileBase wateredTiledTile;
    [SerializeField] private TileBase harvestTile;
    
    private Dictionary<Vector3Int, TileData> farmData = new();
    
    // farmTilemap에 타일이 있으면 → 농사 가능
    private bool IsFarmable(Vector3Int cellPos)
    {
        return farmTilemap.GetTile(cellPos) != null;
    }

    private void OnEnable()
    {
        TimeManager.OnDayChanged += ProcessNextDay;
    }

    private void OnDisable()
    {
        TimeManager.OnDayChanged -= ProcessNextDay;
    }

    private void ProcessNextDay()
    {
        Debug.Log("farmingmanager가 장부를 확인하여 작물을 성장시킴");

        // 모든 타일 데이터 꺼내서 확인 (farmdata에 있는) 
        foreach (var item in farmData)
        {
            Vector3Int cellPos = item.Key;
            TileData data = item.Value;

            // 물 먹은 씨앗은 ? 성장 
            if (data.currentState == TileData.TileState.Seeded && data.isWatered)
            {
                data.growthDays += data.hasFertilizer ? 2 : 1;

                CropData cropInfo = DataManager.Instance.GetCrop(data.cropType);
                int requireDays = cropInfo != null ? cropInfo.growthDays : 3;

                if (data.growthDays >= requireDays)
                {
                    data.currentState = TileData.TileState.Harvestable;
                    Debug.Log($"[{cellPos}] 작물이 다 자랐습니다 바로 수확하세요 ! ");

                    if (cropInfo != null && cropInfo.harvestableTile != null)
                    {
                        farmTilemap.SetTile(cellPos, cropInfo.harvestableTile);
                    }
                }
            }

            if (data.isWatered)
            {
                data.isWatered = false;

                // 그림도 다시 마른 흙/마른씨앗 흙으로 돌리기 
                if (data.currentState == TileData.TileState.Tilled)
                    farmTilemap.SetTile(cellPos, tilledTile);
                else if (data.currentState == TileData.TileState.Seeded)
                    farmTilemap.SetTile(cellPos, seededTile);
            }
        }
    }

    public void InteractWithTile(Vector3Int cellPos, PlayerInventory.EquipSlot currentSlot, PlayerInventory inventory)
    {
        cellPos.z = 0;
        
        if (currentSlot == PlayerInventory.EquipSlot.UniversalHand)
        {
            // 1. 이미 파놓은 땅을 만났을 때 -> 물을 준다 또는 수확!
            if (farmData.ContainsKey(cellPos))
            {
                TileData data = farmData[cellPos];

                // 수확 로직
                if (data.currentState == TileData.TileState.Harvestable)
                {
                    CropData cropInfo = DataManager.Instance.GetCrop(data.cropType);
                    float harvestedWeight = cropInfo != null ? cropInfo.baseWeight : 1.0f;

                    inventory.AddCrop(data.cropType, harvestedWeight);
                    
                    // 리무브 해줘야 함 
                    farmData.Remove(cellPos);
                    farmTilemap.SetTile(cellPos, null);
                    return;
                }

                // 물 주기 로직
                if (data.isWatered)
                {
                    Debug.Log(" 이미 촉촉하게 젖은 땅입니다!");
                    return;
                }

                data.isWatered = true;
                farmTilemap.SetTile(cellPos, wateredTiledTile);
                Debug.Log(" 만능손: 파놓은 흙에 물을 주었습니다!");
                return; 
            }

            // 2. 등록되지 않은 쌩땅을 만났을 때 -> 밭을 간다!
            if (!IsFarmable(cellPos))
            {
                Debug.Log("여기는 밭을 갈 수 없는 땅입니다!");
                return;
            }

            TileData newData = new TileData { currentState = TileData.TileState.Tilled };
            farmData.Add(cellPos, newData);
            farmTilemap.SetTile(cellPos, tilledTile); 
            Debug.Log(" 만능손: 땅을 갈아엎었습니다!");
        }
        //  씨앗 주머니 
        else if (currentSlot == PlayerInventory.EquipSlot.Seed)
        {
            // 현재 선택된 씨앗이 장부에 있는지
            if (!inventory.HasCurrentSeed())
            {
                Debug.Log(" 해당 씨앗이 부족합니다!");
                return;
            }

            if (farmData.TryGetValue(cellPos, out TileData data))
            {
                if (data.currentState == TileData.TileState.Tilled)
                {
                    data.currentState = TileData.TileState.Seeded;
                    
                    data.cropType = inventory.currentSelectedSeed;
                    
                    farmTilemap.SetTile(cellPos, seededTile);
                    
                    inventory.ConsumeCurrentSeed();
                    
                    Debug.Log($" 씨앗 심기 완료! ({inventory.currentSelectedSeed})");
                }
                else if (data.currentState == TileData.TileState.Seeded)
                {
                    Debug.Log("이미 씨앗이 심어져있습니다");
                }
            }
            else
            {
                Debug.Log("씨앗은 갈아엎은 흙 위에만 심을 수 있습니다!");
            }
        }
    }
}