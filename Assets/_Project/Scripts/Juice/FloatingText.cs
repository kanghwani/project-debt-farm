using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    [Header("기본 동작 (HarvestJuiceStyle.None)")]
    public float moveSpeed   = 0.8f;
    public float destroyTime = 2.0f;

    private TextMeshPro textMesh;
    private Color       textColor;
    private bool        isSetup   = false;
    private bool        useDOTween = false;

    // ── 기본 Setup — 기존 방식 그대로 (도구 장착 텍스트 등) ─────────────────
    public void Setup(string text, Color color)
    {
        Init(text, color, 1f);
        // 기존 Lerp 방식 사용
        transform.localScale = Vector3.zero;
        isSetup    = true;
        useDOTween = false;
        Destroy(gameObject, destroyTime);
    }

    // ── 수확 전용 오버로드 — DOTween 애니메이션 ──────────────────────────────
    public void Setup(string text, Color color, float sizeScale, HarvestJuiceStyle style)
    {
        Init(text, color, sizeScale);
        useDOTween = true;
        isSetup    = true;

        switch (style)
        {
            case HarvestJuiceStyle.Combo:
                // 1.3배로 커졌다가 원래 크기 → 위로 float
                transform.localScale = Vector3.one * sizeScale;
                transform.DOScale(Vector3.one * sizeScale * 1.3f, 0.12f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                        transform.DOScale(Vector3.one * sizeScale, 0.1f)
                            .SetEase(Ease.InQuad));
                StartFloat(destroyTime);
                break;

            case HarvestJuiceStyle.Jackpot:
                // Elastic으로 강하게 튀어오름
                transform.localScale = Vector3.zero;
                transform.DOScale(Vector3.one * sizeScale, 0.25f)
                    .SetEase(Ease.OutElastic);
                StartFloat(destroyTime);
                break;

            default:
                // None → 기존 방식 fallback
                transform.localScale = Vector3.zero;
                useDOTween = false;
                Destroy(gameObject, destroyTime);
                break;
        }
    }

    void Init(string text, Color color, float sizeScale)
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null) return;

        textMesh.sortingOrder = 100;
        textMesh.text         = text;
        textMesh.color        = color;
        textMesh.fontSize    *= sizeScale;
        textColor             = color;
    }

    void StartFloat(float lifetime)
    {
        // 위로 떠오르며 fade
        transform.DOMoveY(transform.position.y + moveSpeed * lifetime, lifetime)
            .SetEase(Ease.OutQuad);
        textMesh.DOFade(0f, lifetime * 0.6f)
            .SetDelay(lifetime * 0.4f);
        Destroy(gameObject, lifetime);
    }

    // ── Update — 기본 방식 전용 ───────────────────────────────────────────────
    private void Update()
    {
        if (!isSetup || useDOTween || textMesh == null) return;

        transform.position   += Vector3.up * moveSpeed * Time.deltaTime;
        transform.localScale  = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 10f);

        textColor.a -= Time.deltaTime / destroyTime;
        textMesh.color = textColor;
    }
}
