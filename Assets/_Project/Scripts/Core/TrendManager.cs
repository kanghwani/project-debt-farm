using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 매일 자정 유행(상승장/하락장)을 갱신한다.
/// 5가지 태그(매운맛, 단맛, 관상용, 약용, 독성)를 지원하며 라디오 연동을 위한 캐싱 기능 포함.
/// </summary>
public class TrendManager : MonoBehaviour
{
    public static TrendManager Instance { get; private set; }

    [Header("태그 목록")]
    [Tooltip("상점에서 판매하거나 비료로 부여할 수 있는 모든 태그")]
    public List<CropTag> rollableTags = new() 
    { 
        CropTag.Spicy, 
        CropTag.Sweet, 
        CropTag.Ornamental, 
        CropTag.Medicinal, 
        CropTag.Forbidden 
    };

    [Header("상승장 배수 범위")]
    public float positiveMultMin = 1.5f;
    public float positiveMultMax = 4.0f;

    [Header("하락장 배수 범위")]
    public float negativeMultMin = 0.5f;
    public float negativeMultMax = 0.8f;

    [Header("상승장 확률 (일수별)")]
    [Range(0f, 1f)] public float earlyPositiveChance  = 0.70f;
    [Range(0f, 1f)] public float midPositiveChance    = 0.60f;
    [Range(0f, 1f)] public float latePositiveChance   = 0.50f;

    public TrendData TodayTrend    { get; private set; }
    public TrendData NextDayTrend  { get; private set; }

    public static event Action<TrendData> OnTrendChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        NextDayTrend = GenerateTrend();
        AdvanceTrend();
    }

    private void OnEnable()  => TimeManager.OnDayChanged += AdvanceTrend;
    private void OnDisable() => TimeManager.OnDayChanged -= AdvanceTrend;

    void AdvanceTrend()
    {
        TodayTrend   = NextDayTrend ?? GenerateTrend();
        NextDayTrend = GenerateTrend(excludeTag: TodayTrend?.tag ?? CropTag.None);

        Debug.Log($"[TrendManager] 오늘: {TodayTrend.tag} {(TodayTrend.isPositive ? "↑" : "↓")} ×{TodayTrend.multiplier:F1}");
        OnTrendChanged?.Invoke(TodayTrend);
    }

    TrendData GenerateTrend(CropTag excludeTag = CropTag.None)
    {
        if (rollableTags == null || rollableTags.Count == 0)
            return new TrendData { tag = CropTag.None, isPositive = true, multiplier = 1f, flavorText = "조용한 하루" };

        CropTag tag;
        if (rollableTags.Count > 1)
        {
            do { tag = rollableTags[UnityEngine.Random.Range(0, rollableTags.Count)]; }
            while (tag == excludeTag);
        }
        else tag = rollableTags[0];

        int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
        float positiveChance = day <= 3 ? earlyPositiveChance
                             : day <= 6 ? midPositiveChance
                                        : latePositiveChance;

        bool isPositive = UnityEngine.Random.value < positiveChance;

        float mult = isPositive
            ? UnityEngine.Random.Range(positiveMultMin, positiveMultMax)
            : UnityEngine.Random.Range(negativeMultMin, negativeMultMax);

        mult = (float)System.Math.Round(mult * 10f) / 10f;

        string flavor = GenerateHeadline(tag, isPositive, mult);

        return new TrendData { tag = tag, isPositive = isPositive, multiplier = mult, flavorText = flavor };
    }

    static string GenerateHeadline(CropTag tag, bool isPositive, float mult)
    {
        // 1. 코미디 헤드라인 데이터베이스
        string[] spicyPos = { "스트리머 매운맛 챌린지 대유행!", "도시인들의 스트레스성 매운맛 수요 폭발", "불닭 소스 품귀 현상!" };
        string[] spicyNeg = { "위장병 환자 급증... 매운맛 기피 현상", "캡사이신 규제 법안 발의", "너무 매워서 사채업자도 뱉음" };
        
        string[] sweetPos = { "당 충전 시급! 빚쟁이들의 설탕 홀릭", "정부 주도 단맛 장려 운동", "디저트 카페 창업 열풍!" };
        string[] sweetNeg = { "설탕세 폭탄 도입! 단맛 작물 폭락", "치과 의사 협회, 단맛 경고문 발표", "개미 떼 창궐로 단맛 혐오 확산" };

        string[] ornaPos  = { "도시 부자들의 사치품 대유행!", "인스타 감성 폭발! 관상용 작물 품귀", "먹지도 못하는 예쁜 쓰레기 열풍" };
        string[] ornaNeg  = { "사치세 도입... 관상용 작물 거래 급감", "배고픈 시절에 관상용이 웬 말?", "예쁜 쓰레기 처리 곤란" };

        string[] mediPos  = { "건강 염려증 사채업자들의 보약 열풍", "한방 건강 주스 프랜차이즈 급증", "기적의 약초설로 실버 마켓 평정" };
        string[] mediNeg  = { "가짜 약재 논란으로 신뢰도 급락", "뒷산 약초꾼들과의 가격 경쟁 심화", "약보다 밥이 먼저라는 여론 확산" };

        string[] forbPos  = { "어둠의 경로, 금기된 식물 밀매 급증!", "위험할수록 비싸다! 중독적인 불법 작물", "정부 몰래 키우는 독초 잭팟" };
        string[] forbNeg  = { "범정부 차원 불법 작물 특별 단속 기간", "독성 부작용 보고... 시장 퇴출 위기", "키우다 걸리면 빚이 두 배!" };

        string[] fallbackPos = { "시장 호황! 예상외의 수요 폭발" };
        string[] fallbackNeg = { "알 수 없는 이유로 시장 냉각..." };

        string[] selectedArray = tag switch
        {
            CropTag.Spicy      => isPositive ? spicyPos : spicyNeg,
            CropTag.Sweet      => isPositive ? sweetPos : sweetNeg,
            CropTag.Ornamental => isPositive ? ornaPos : ornaNeg,
            CropTag.Medicinal  => isPositive ? mediPos : mediNeg,
            CropTag.Forbidden  => isPositive ? forbPos : forbNeg,
            _                  => isPositive ? fallbackPos : fallbackNeg
        };

        string headline = selectedArray[UnityEngine.Random.Range(0, selectedArray.Length)];
        
        // 2. 수익률 강조 텍스트
        string rateText = isPositive ? $"<color=#FFE600>▲ ×{mult:F1}</color>" : $"<color=#FF5555>▼ -{Mathf.RoundToInt((1f - mult) * 100f)}%</color>";

        // 3. 직관적인 태그 설명 추가 (데모 버전 필수 피드백)
        string tagDisplayName = tag switch
        {
            CropTag.Spicy      => "🔥매운맛",
            CropTag.Sweet      => "🍬단맛",
            CropTag.Ornamental => "✨관상용",
            CropTag.Medicinal  => "💊약용",
            CropTag.Forbidden  => "⚠️독성",
            _                  => tag.ToString(),
        };
        
        string explainText = isPositive 
            ? $"<b>{tagDisplayName}</b> 태그 작물의 가격이 <color=#FFE600>상승</color>합니다."
            : $"<b>{tagDisplayName}</b> 태그 작물의 가격이 <color=#FF5555>하락</color>합니다.";

        // 4. 최종 출력: 헤드라인 + 배수 + (줄바꿈) + 직관적 설명
        // 텍스트 너비 문제를 방지하기 위해 헤드라인과 배수를 같은 줄에, 설명을 아랫줄에 배치
        return $"{headline} {rateText}\n<color=#AAAAAA><size=80%>{explainText}</size></color>";
    }

    public float GetMultiplierFor(List<CropTag> tags)
    {
        if (TodayTrend == null || TodayTrend.tag == CropTag.None) return 1f;
        return tags.Contains(TodayTrend.tag) ? TodayTrend.multiplier : 1f;
    }

    [ContextMenu("유행 강제 갱신 (테스트)")]
    public void DEBUG_Reroll() => AdvanceTrend();
}