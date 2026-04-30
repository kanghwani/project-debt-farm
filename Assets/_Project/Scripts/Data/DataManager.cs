using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    // 싱글톤: 누구나 언제든 DataManager.Instance 로 접근 가능
    public static DataManager Instance { get; private set; }

    [Header("Crop Database")]
    [Tooltip("여기에 작물 데이터를 배열 형태로 세팅합니다.")]
    [SerializeField] private CropData[] allCrops;

    [Header("Fertilizer Database")]
    [Tooltip("여기에 비료 데이터를 배열 형태로 세팅합니다.")]
    [SerializeField] private FertilizerData[] allFertilizers;

    // foreach 검색의 느린 속도를 해결하기 위한 딕셔너리
    private Dictionary<TileData.Crops, CropData> cropDictionary = new Dictionary<TileData.Crops, CropData>();
    private Dictionary<Fertilizer, FertilizerData> fertilizerDictionary = new Dictionary<Fertilizer, FertilizerData>();
    
    private void Awake()
    {
        // 싱글톤 및 씬 전환 시 파괴 방지(DontDestroyOnLoad) 세팅
        if (Instance == null)
        {
            Instance = this;
            
            // 최상단 오브젝트로 만들어야 DontDestroyOnLoad가 정상 작동합니다.
            transform.SetParent(null); 
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return; 
        }

        // 도감 초기화 실행
        InitializeCropDictionary();
        InitializeFertilizerDictionary();
    }

    private void InitializeCropDictionary()
    {
        // 방어 코드
        if (allCrops == null || allCrops.Length == 0)
        {
            Debug.LogWarning("[DataManager] 등록된 작물이 하나도 없습니다.");
            return;
        }

        // 게임이 켜질 때 배열의 데이터를 딕셔너리로 옮겨 담습니다.
        foreach (var crop in allCrops)
        {
            // 중복된 타입이 아닐 때만 넣습니다.
            if (crop != null && !cropDictionary.ContainsKey(crop.cropType))
            {
                cropDictionary.Add(crop.cropType, crop);
            }
        }
        
        Debug.Log($"[DataManager] 총 {cropDictionary.Count}개의 작물 도감 세팅 완료.");
    }

    private void InitializeFertilizerDictionary()
    {
        if (allFertilizers == null || allFertilizers.Length == 0)
        {
            Debug.LogWarning("[DataManager] 등록된 비료가 하나도 없습니다.");
            return;
        }

        foreach (var fert in allFertilizers)
        {
            if (fert != null && !fertilizerDictionary.ContainsKey(fert.type))
                fertilizerDictionary.Add(fert.type, fert);
        }

        Debug.Log($"[DataManager] 총 {fertilizerDictionary.Count}개의 비료 도감 세팅 완료.");
    }

    public FertilizerData GetFertilizer(Fertilizer type)
    {
        if (fertilizerDictionary.TryGetValue(type, out FertilizerData data))
            return data;

        Debug.LogWarning($"[DataManager] {type} 비료 데이터를 찾을 수 없습니다. 인스펙터를 확인하세요.");
        return null;
    }

    // 다른 스크립트들이 데이터를 찾을 때 부르는 함수
    public CropData GetCrop(TileData.Crops type)
    {
        if (cropDictionary.TryGetValue(type, out CropData data))
        {
            return data;
        }
        
        Debug.LogWarning($"[DataManager] {type} 작물 데이터를 찾을 수 없습니다. 인스펙터를 확인하세요.");
        return null;
    }
}