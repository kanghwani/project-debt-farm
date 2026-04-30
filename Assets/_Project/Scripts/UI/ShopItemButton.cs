using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;
    
    private DroneShop shop;
    private TileData.Crops myCropType;

    // 상점(DroneShop)이 버튼을 찍어낼 때 데이터 넣기
    public void Setup(DroneShop shopRef, CropData cropData)
    {
        this.shop = shopRef;
        this.myCropType = cropData.cropType;

        // 텍스트 세팅
        if (nameText != null) 
            nameText.text = $"{cropData.cropName} 씨앗";
            
        if (priceText != null) 
            priceText.text = $"{cropData.seedPrice} G";

        
        if (iconImage != null && cropData.cropIcon != null)
        {
            iconImage.sprite = cropData.cropIcon;
            iconImage.color = Color.white; // 혹시 투명해졌을까 봐 흰색으로 꽉 채워줍니다.
        }

        // 중복되었던 버튼 클릭 이벤트 깔끔하게 정리 (SelectCrop 하나만 남기기)
        button.onClick.RemoveAllListeners(); 
        button.onClick.AddListener(() => shop.SelectCrop(cropData));
    }
}