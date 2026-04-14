using System;


[Serializable]
public class TileData
{
    public enum TileState
    {
        Empty,
        Tilled,
        Seeded,
        Harvestable
    }

    public enum Crops
    {
        None, 
        radish,   // 기존 (무)
        potato,   // 기존 (감자)
        weed,     // 기존 (잡초)
        tomato,   // ★ 추가: 토마토
        pumpkin,  // ★ 추가: 호박
        ginseng   // ★ 추가: 인삼
    }


    public TileState currentState;
    public bool isWatered = false;

    public int growthDays = 0;
    public Crops cropType = Crops.None;
    public bool hasFertilizer = false;
    
    public float cropWeight = 0f;
}
