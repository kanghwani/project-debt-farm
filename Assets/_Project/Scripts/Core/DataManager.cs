using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    // ★ 싱글톤: 누구나 언제든 DataManager.Instance 로 접근 가능!
    public static DataManager Instance { get; private set; }

    [Header("모든 작물 도감 (여기에만 넣으면 끝!)")]
    [SerializeField] private CropData[] allCrops;

    // foreach 검색의 느린 속도를 해결하기 위한 마법의 책장
    private Dictionary<TileData.Crops, CropData> cropDictionary = new();

    private void Awake()
    {
        // 싱글톤 기본 세팅
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 도서관은 무너지지 않음
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ★ 핵심: 게임이 켜질 때, 배열에 있는 책들을 '딕셔너리'로 싹 정리합니다.
        foreach (var crop in allCrops)
        {
            if (!cropDictionary.ContainsKey(crop.cropType))
            {
                cropDictionary.Add(crop.cropType, crop);
            }
        }
    }

    // 다른 스크립트들이 책을 찾을 때 부르는 함수 (foreach 없이 0.001초 만에 찾음!)
    public CropData GetCrop(TileData.Crops type)
    {
        if (cropDictionary.TryGetValue(type, out CropData data))
        {
            return data;
        }
        
        Debug.LogWarning($"[DataManager] {type} 작물 데이터를 찾을 수 없습니다!");
        return null;
    }
}