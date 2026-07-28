using UnityEngine;

// 책임: 작물 한 종 정의 (성장 턴 + 수확량 + 자원 변환 단가)
// (OCP) 새 작물 추가 = SO 에셋 1개. 코어 코드 수정 X
[CreateAssetMenu(fileName = "CropData", menuName = "BunkerBloom/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("기본 정보")]
    public string cropName     = "OilVine";
    public int    growTurns    = 1;     // T1 prep → T2 수확
    public int    harvestCount = 3;     // 1회 수확 시 작물 수

    [Header("자원 변환 단가 (작물 1개당)")]
    public float foodPerUnit  = 1f;
    public float powerPerUnit = 10f;
}
