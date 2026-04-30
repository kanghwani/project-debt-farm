[System.Serializable]
public class TileData
{
    public enum TileState { Empty, Tilled, Seeded, Harvestable, Rotting, Dead }
    public enum Crops { 
        None, 
        Parsnip,    
        Radish,     
        Carrot,     
        Potato,      
        RedRadish,   
        Beet,        
        Cabbage,     
        Pumpkin,     
        Sunflower,   
        Rice         }
    
    public TileState currentState = TileState.Empty;
    public Crops cropType = Crops.None;
    
    // 필수 상태 변수
    public bool isWatered = false;

    // 비료 시스템 (다른 종류 무제한 / 같은 종류 1개 / 총 최대 3개)
    public System.Collections.Generic.List<Fertilizer> appliedFertilizers = new();
    public System.Collections.Generic.List<CropTag> activeTags = new();

    // 썩음 방지 플래그 (향후 비료 확장용 — 기본 false)
    public bool isRotResistant = false;

    // ️ 타임어택 & 숙련도 로직용 변수
    public float currentTimer = 0f; // 현재 상태에서 흐른 시간(초)
    public float qualityScore = 0f; // 조작으로 얻은 누적 등급 점수 (0~100)
}