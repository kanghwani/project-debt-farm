using UnityEngine;

[System.Serializable]
public class FertilizerData
{
    [Header("Basic")]
    public Fertilizer type;
    public string displayName;
    public int buyPrice;

    [Header("Effect")]
    [Tooltip("부여할 작물 태그 (없으면 None)")]
    public CropTag grantTag = CropTag.None;
    [Tooltip("기본가에 더하는 고정 보너스 (예: PopcornYeast +50)")]
    public int flatBonus = 0;
    [Tooltip("최종 배수에 더하는 값 (예: 0.5 → 배수 +0.5)")]
    public float multAdd = 0f;
    [Tooltip("조건부 특수 효과")]
    public FertilizerSpecial special = FertilizerSpecial.None;

    [Header("Visual")]
    [Tooltip("파티클 색상 (속성별로 구분)")]
    public Color vfxColor = Color.white;
    public Sprite icon;
    
    [Header("Description")]
    [TextArea(3, 10)] // 인스펙터에서 여러 줄로 편하게 입력할 수 있게 해줍니다.
    public string flavorText;
}
