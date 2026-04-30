using UnityEngine;
using UnityEngine.Tilemaps;

// 유니티 인스펙터 창에 이 클래스의 내용이 보이도록 만들어주는 속성
[System.Serializable] 
public class CropData
{
    
    
    [Header("Basic Info")]
    public TileData.Crops cropType;      // 작물 종류
    public string cropName;              // 화면에 띄울 진짜 이름
    
    [Header("Time Attack Data")]
    public float requireGrowTime = 10f;  // 수확 가능까지 걸리는 시간
    public float requireRotTime = 20f;   // 썩기까지 버티는 시간
    
    [Header("Economy / Physics")]
    public int basePrice = 100;          // 팔때 가격
    public int seedPrice = 50;           // 살때 가격 
    public float baseWeight = 1.5f;      // 무게
    
    [Header("Visual Data")]
    public TileBase seededTile;          // 씨앗 타일
    public TileBase harvestableTile;     // 수확 가능 타일
    public TileBase rottingTile;         // 썩어가는 타일
    
    public Sprite cropIcon;
    
    [Header("Description")]
    [TextArea(3, 10)] // 인스펙터에서 여러 줄로 편하게 입력할 수 있게 해줍니다.
    public string flavorText;
    
    
}