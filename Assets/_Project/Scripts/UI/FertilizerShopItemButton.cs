using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 비료 상점 버튼 하나. ShopItemButton의 비료 버전.
public class FertilizerShopItemButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private DroneShop shop;

    public void Setup(DroneShop shopRef, FertilizerData data)
    {
        this.shop = shopRef;

        if (nameText  != null) nameText.text  = data.displayName;
        if (priceText != null) priceText.text = $"{data.buyPrice} G";

        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color  = Color.white;
            }
            else
            {
                // 아이콘 없으면 vfxColor 색상 블록으로 표시
                iconImage.sprite = null;
                iconImage.color  = data.vfxColor;
            }
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => shop.SelectFertilizer(data));
    }
}
