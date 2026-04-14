using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Crop Data", menuName = "Farming/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("기본 정보")]
    public TileData.Crops cropType;      // 작물 종류 (enum)
    public string cropName;              // 화면에 띄울 진짜 이름 (예: "달콤한 무")
    
    [Header("성장 데이터")]
    public int growthDays = 3;           // 다 자라는 데 걸리는 시간
    
    [Header("경제/물리 데이터")]
    public int basePrice = 100;          // 출하 상자에서 팔릴 때 가격
    public float baseWeight = 1.5f;      // 인벤토리에 들어갈 때 무게
    
    [Header("시각 데이터 (그림)")]
    public TileBase seededTile;          // 씨앗 상태일 때 타일 그림
    public TileBase harvestableTile;     // 다 자랐을 때 타일 그림 (★ 매우 중요!)
    // public Sprite inventoryIcon;      // 나중에 UI에 띄울 아이콘 이미지용
}
