/// <summary>
/// 하루치 유행 정보. TrendManager가 매일 생성해 OnTrendChanged로 전파한다.
/// </summary>
[System.Serializable]
public class TrendData
{
    public CropTag tag;           // 해당 작물 태그
    public bool    isPositive;    // true=상승장  false=하락장
    public float   multiplier;    // 상승: ×1.5~4.0 / 하락: ×0.5~0.8
    public string  flavorText;    // UI 출력용 문구  예) "매운맛 떡상!", "육식 다이어트"
}
