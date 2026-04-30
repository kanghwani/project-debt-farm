using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class SettlementItemRow : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private StackedCrop _item;

    public void Init(StackedCrop item)
    {
        _item = item;
        _text = GetComponent<TextMeshProUGUI>();
        if (_text == null) _text = gameObject.AddComponent<TextMeshProUGUI>();

        _text.fontSize = 26f; // 기존 18f에서 가독성을 위해 확대
        _text.richText = true;
        _text.overflowMode = TextOverflowModes.Overflow;
        
        // 초기 상태: 이름과 등급, 기본 가격만 노출
        string gradeColor = GetGradeColor(item.grade);
        _text.text = $"<color=#2C1A0A>{item.data.cropName}</color>  <color={gradeColor}>[{item.grade}등급]</color>  <color=#888888>{item.data.basePrice}G</color>";
        
        Debug.Log($"[SettlementItemRow] Init 완료! 작물명: {item.data.cropName}, 폰트할당여부: {_text.font != null}");
    }

    public IEnumerator PlayAnimation(float delayBetweenSteps)
    {
        // 1. 등장 애니메이션 (작아졌다 커지기)
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.15f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.15f);

        // 2. 고정 보너스 및 배수 계산
        int baseWithBonus = _item.data.basePrice;
        string calcString = $"{_item.data.basePrice}G";

        if (_item.flatBonusTotal > 0)
        {
            yield return new WaitForSecondsRealtime(delayBetweenSteps);
            baseWithBonus += _item.flatBonusTotal;
            calcString = $"({_item.data.basePrice} <color=#3A6B35>+{_item.flatBonusTotal}</color>)";
            UpdateText(calcString, "");
            AudioManager.PlaySFX(SfxType.UiClick);
        }

        if (Mathf.Abs(_item.totalMultiplier - 1f) > 0.01f)
        {
            yield return new WaitForSecondsRealtime(delayBetweenSteps);
            calcString += $" <color=#CC7700>×{_item.totalMultiplier:F1}</color>";
            UpdateText(calcString, "");
            AudioManager.PlaySFX(SfxType.UiClick);
        }

        // 3. 최종 가격 표시 (쾅!)
        yield return new WaitForSecondsRealtime(delayBetweenSteps);
        UpdateText(calcString, $"  <b><color=#8B4513>+ {_item.price}G</color></b>");
        
        // 텍스트 살짝 튕기기
        _text.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f).SetUpdate(true);
        AudioManager.PlaySFX(SfxType.UiHover);

        // 4. 비료/유행 텍스트가 있다면 아래에 한 줄씩 추가
        string subLines = "";
        if (!string.IsNullOrEmpty(_item.fertilizerSummary))
        {
            yield return new WaitForSecondsRealtime(delayBetweenSteps);
            subLines += $"\n<size=80%><color=#3A6B35>  ┕ 비료: {_item.fertilizerSummary}</color></size>";
            UpdateText(calcString, $"  <b><color=#8B4513>+ {_item.price}G</color></b>", subLines);
            AudioManager.PlaySFX(SfxType.UiClick);
        }

        if (!string.IsNullOrEmpty(_item.trendSummary))
        {
            yield return new WaitForSecondsRealtime(delayBetweenSteps);
            subLines += $"\n<size=80%><color=#8B6914>  ┕ 유행: {_item.trendSummary}</color></size>";
            UpdateText(calcString, $"  <b><color=#8B4513>+ {_item.price}G</color></b>", subLines);
            AudioManager.PlaySFX(SfxType.UiClick);
        }

        yield return new WaitForSecondsRealtime(0.1f);
    }

    private void UpdateText(string calcString, string finalPriceString, string subLines = "")
    {
        string gradeColor = GetGradeColor(_item.grade);
        _text.text = $"<color=#2C1A0A>{_item.data.cropName}</color>  <color={gradeColor}>[{_item.grade}등급]</color>  <size=85%>{calcString}</size>{finalPriceString}{subLines}";
    }

    private string GetGradeColor(string grade)
    {
        return grade switch
        {
            "S" => "#B8860B",
            "A" => "#CC5500",
            "B" => "#1A5DAD",
            _   => "#555555",
        };
    }
}
