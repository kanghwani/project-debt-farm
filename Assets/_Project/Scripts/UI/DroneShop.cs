using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class DroneShop : MonoBehaviour
{
    // ── 탭 ────────────────────────────────────────────────────────────────────
    public enum ShopTab { Seeds, Fertilizer }
    private ShopTab currentTab = ShopTab.Seeds;

    [Header("Tab Panels")]
    [Tooltip("씨앗 목록이 담긴 패널 (buttonContainer의 부모)")]
    public GameObject seedTabPanel;
    [Tooltip("비료 목록이 담긴 패널 (fertilizerContainer의 부모)")]
    public GameObject fertilizerTabPanel;

    // ── 피드백 ────────────────────────────────────────────────────────────────
    [Header("Visual Feedback")]
    public Image feedbackPanelImage;
    public float shakeMagnitude = 10f;
    public float shakeDuration  = 0.2f;

    private Color     originalPanelColor;
    private Vector2   originalPanelPosition;
    private RectTransform panelRect;

    // ── 상단 UI ───────────────────────────────────────────────────────────────
    [Header("Top UI")]
    public TextMeshProUGUI playerGoldText;

    [Header("UI Toggle")]
    public GameObject todayDebtUI;

    [Header("UI Panel")]
    public GameObject shopPanel;

    // ── 씨앗 탭 ───────────────────────────────────────────────────────────────
    [Header("Seed Shop")]
    public GameObject  buttonPrefab;
    public Transform   buttonContainer;
    public List<TileData.Crops> cropsToSell = new();

    // ── 비료 탭 ───────────────────────────────────────────────────────────────
    [Header("Fertilizer Shop")]
    [Tooltip("비료 버튼 프리팹 (FertilizerShopItemButton 컴포넌트 포함)")]
    public GameObject fertilizerButtonPrefab;
    [Tooltip("비료 버튼이 생성될 부모 Transform")]
    public Transform  fertilizerContainer;
    public List<Fertilizer> fertilizersToSell = new();

    // ── 하단 디테일 패널 (씨앗·비료 공용) ────────────────────────────────────
    [Header("Bottom Detail UI")]
    public Image              selectedIconImage;
    public TextMeshProUGUI    selectedNamePriceText;
    public Button             realBuyButton;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private PlayerInventory playerInventory;
    private bool isShopOpenedToday = false;

    private List<GameObject> spawnedSeedButtons       = new();
    private List<GameObject> spawnedFertilizerButtons = new();

    // 현재 선택된 씨앗
    private TileData.Crops currentSelectedCrop  = TileData.Crops.None;
    private int            currentSelectedPrice = 0;

    // 현재 선택된 비료
    private FertilizerData currentSelectedFertData  = null;
    
    // 하단 설명 
    [SerializeField] private TextMeshProUGUI descriptionText;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (feedbackPanelImage != null)
        {
            originalPanelColor    = feedbackPanelImage.color;
            panelRect             = feedbackPanelImage.rectTransform;
            originalPanelPosition = panelRect.anchoredPosition;
        }

        if (shopPanel == null)
        {
            Debug.LogError("[DroneShop] shopPanel이 Inspector에 연결되지 않았습니다!");
            return;
        }

        playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory == null)
            Debug.LogError("[DroneShop] PlayerInventory를 찾을 수 없습니다!");

        shopPanel.SetActive(false);

        TimeManager.OnTimeChanged += CheckShopOpenTime;
        TimeManager.OnDayChanged  += ResetShopStatus;

        // 상점은 DroneShopTrigger가 플레이어 접근 시 열어줌 (Start에서 자동 열기 제거)
        ClearBottomDetail();
        realBuyButton.onClick.RemoveAllListeners();
        realBuyButton.onClick.AddListener(ExecutePurchase);
    }

    private void OnDestroy()
    {
        TimeManager.OnTimeChanged -= CheckShopOpenTime;
        TimeManager.OnDayChanged  -= ResetShopStatus;
    }

    // ── 오픈 / 클로즈 ──────────────────────────────────────────────────────────

    private void CheckShopOpenTime(int hour, int minute)
    {
        if (hour == 6 && !isShopOpenedToday) OpenShop();
    }

    private void ResetShopStatus() => isShopOpenedToday = false;

    public void OpenShop()
    {
        isShopOpenedToday = true;
        shopPanel.SetActive(true);
        if (todayDebtUI != null) todayDebtUI.SetActive(false);

        ClearBottomDetail();
        SwitchTab(ShopTab.Seeds); // 열릴 때 항상 씨앗 탭부터
        UpdateGoldUI();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        if (todayDebtUI != null) todayDebtUI.SetActive(true);
    }

    // ── 탭 전환 ───────────────────────────────────────────────────────────────

    // Inspector의 탭 버튼 OnClick에 연결
    public void OnClickSeedsTab()        => SwitchTab(ShopTab.Seeds);
    public void OnClickFertilizerTab()   => SwitchTab(ShopTab.Fertilizer);

    private void SwitchTab(ShopTab tab)
    {
        currentTab = tab;
        ClearBottomDetail();

        bool isSeed = tab == ShopTab.Seeds;

        if (seedTabPanel != null)
            seedTabPanel.SetActive(isSeed);
        else if (isSeed)
            Debug.LogWarning("[DroneShop] Seed Tab Panel이 Inspector에 연결되지 않았습니다.");

        if (fertilizerTabPanel != null)
            fertilizerTabPanel.SetActive(!isSeed);
        else if (!isSeed)
            Debug.LogWarning("[DroneShop] Fertilizer Tab Panel이 Inspector에 연결되지 않았습니다.");

        if (isSeed)   UpdateSeedShopUI();
        else          UpdateFertilizerShopUI();
    }

    // ── 씨앗 탭 ───────────────────────────────────────────────────────────────

    private void UpdateSeedShopUI()
    {
        foreach (var btn in spawnedSeedButtons) Destroy(btn);
        spawnedSeedButtons.Clear();

        if (DataManager.Instance == null)
        {
            Debug.LogError("[DroneShop] DataManager.Instance가 null입니다. 씨앗 탭 로드 실패.");
            return;
        }

        foreach (TileData.Crops cropType in cropsToSell)
        {
            CropData data = DataManager.Instance.GetCrop(cropType);
            if (data == null) continue;

            GameObject obj = Instantiate(buttonPrefab, buttonContainer);
            spawnedSeedButtons.Add(obj);

            ShopItemButton shopBtn = obj.GetComponent<ShopItemButton>();
            shopBtn?.Setup(this, data);
        }
    }

    public void SelectCrop(CropData data)
    {
        AudioManager.PlaySFX(SfxType.UiClick);   // 아이템 선택 클릭음

        currentSelectedCrop  = data.cropType;
        currentSelectedPrice = data.seedPrice;
        currentSelectedFertData = null;

        selectedIconImage.sprite = data.cropIcon;
        selectedIconImage.color  = Color.white;
        selectedNamePriceText.text =
            $"{data.cropName} 씨앗\n<color=yellow>{data.seedPrice} G</color>";

        //  씨앗 설명 텍스트 업데이트
        if (descriptionText != null)
        {
            descriptionText.text = data.flavorText; 
        }
    }

    // ── 비료 탭 ───────────────────────────────────────────────────────────────

    private void UpdateFertilizerShopUI()
    {
        foreach (var btn in spawnedFertilizerButtons) Destroy(btn);
        spawnedFertilizerButtons.Clear();

        // ── 필수 레퍼런스 검증 ──────────────────────────────────────────────
        if (fertilizerButtonPrefab == null)
        {
            Debug.LogError("[DroneShop] Fertilizer Button Prefab이 Inspector에 연결되지 않았습니다!");
            return;
        }
        if (fertilizerContainer == null)
        {
            Debug.LogError("[DroneShop] Fertilizer Container가 Inspector에 연결되지 않았습니다!");
            return;
        }
        if (DataManager.Instance == null)
        {
            Debug.LogError("[DroneShop] DataManager.Instance가 null입니다. DataManager 오브젝트를 확인하세요.");
            return;
        }
        if (fertilizersToSell.Count == 0)
        {
            Debug.LogWarning("[DroneShop] fertilizersToSell 목록이 비어 있습니다. Inspector에서 비료를 추가하세요.");
            return;
        }

        // ── 버튼 생성 ───────────────────────────────────────────────────────
        foreach (Fertilizer fertType in fertilizersToSell)
        {
            FertilizerData data = DataManager.Instance.GetFertilizer(fertType);
            if (data == null)
            {
                Debug.LogWarning($"[DroneShop] {fertType} 비료 데이터가 DataManager에 없습니다. allFertilizers 배열을 확인하세요.");
                continue;
            }

            GameObject obj = Instantiate(fertilizerButtonPrefab, fertilizerContainer);
            spawnedFertilizerButtons.Add(obj);

            FertilizerShopItemButton btn = obj.GetComponent<FertilizerShopItemButton>();
            if (btn == null)
            {
                Debug.LogError($"[DroneShop] Fertilizer Button Prefab에 FertilizerShopItemButton 컴포넌트가 없습니다!");
                continue;
            }
            btn.Setup(this, data);
        }

        Debug.Log($"[DroneShop] 비료 버튼 {spawnedFertilizerButtons.Count}개 생성 완료.");
    }

    public void SelectFertilizer(FertilizerData data)
    {
        AudioManager.PlaySFX(SfxType.UiClick);   // 아이템 선택 클릭음

        currentSelectedFertData  = data;
        currentSelectedCrop      = TileData.Crops.None;
        currentSelectedPrice     = data.buyPrice;

        if (data.icon != null)
        {
            selectedIconImage.sprite = data.icon;
            selectedIconImage.color  = Color.white;
        }
        else
        {
            selectedIconImage.sprite = null;
            selectedIconImage.color  = data.vfxColor;
        }

        selectedNamePriceText.text =
            $"{data.displayName}\n<color=yellow>{data.buyPrice} G</color>";

        //  비료 설명 텍스트 업데이트
        if (descriptionText != null)
        {
            descriptionText.text = data.flavorText;
        }
    }

    // ── 구매 ──────────────────────────────────────────────────────────────────

    private void ExecutePurchase()
    {
        if (playerInventory == null) return;

        if (currentTab == ShopTab.Seeds)
        {
            if (currentSelectedCrop == TileData.Crops.None) return;
            TryBuy(currentSelectedPrice, () =>
                StartCoroutine(SeedDeliveryRoutine(currentSelectedCrop)));
        }
        else
        {
            if (currentSelectedFertData == null) return;
            TryBuy(currentSelectedFertData.buyPrice, () =>
                StartCoroutine(FertilizerDeliveryRoutine(currentSelectedFertData.type)));
        }
    }

    // 결제 공통 처리: 잔액 충분하면 onSuccess 호출, 부족하면 에러 피드백
    private void TryBuy(int price, System.Action onSuccess)
    {
        if (playerInventory.gold >= price)
        {
            AudioManager.PlaySFX(SfxType.ShopBuy);   // 구매 성공 동전음
            playerInventory.SpendGold(price);
            UpdateGoldUI();
            onSuccess?.Invoke();
        }
        else
        {
            AudioManager.PlaySFX(SfxType.UiError);   // 잔액 부족
            StopCoroutine("ErrorFeedbackRoutine");
            StartCoroutine("ErrorFeedbackRoutine");
        }
    }

    private IEnumerator SeedDeliveryRoutine(TileData.Crops type)
    {
        yield return new WaitForSeconds(1f);
        playerInventory.BuySeed(type, 0); // 이미 결제했으므로 가격 0
        Debug.Log($"🛸 배송 도착: {type} 씨앗");
    }

    private IEnumerator FertilizerDeliveryRoutine(Fertilizer type)
    {
        yield return new WaitForSeconds(1f);
        playerInventory.AddFertilizer(type, 1);
        Debug.Log($"🛸 배송 도착: {type} 비료");
    }

    // ── 공통 UI 헬퍼 ─────────────────────────────────────────────────────────

    private void ClearBottomDetail()
    {
        currentSelectedCrop     = TileData.Crops.None;
        currentSelectedFertData = null;
        currentSelectedPrice    = 0;

        if (selectedIconImage       != null) selectedIconImage.color  = Color.clear;
        if (selectedNamePriceText   != null) selectedNamePriceText.text = "항목을 선택해주세요.";
        
        // 아무것도 선택 안 했을 땐 설명창 비우기
        if (descriptionText != null) 
        {
            descriptionText.text = ""; 
        }
    }

    private void UpdateGoldUI()
    {
        if (playerGoldText != null && playerInventory != null)
            playerGoldText.text = $"소지금: {playerInventory.gold:N0} G";
    }

    private IEnumerator ErrorFeedbackRoutine()
    {
        if (feedbackPanelImage == null || panelRect == null) yield break;

        feedbackPanelImage.color = new Color(1f, 0.5f, 0.5f);
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            panelRect.anchoredPosition = new Vector2(
                originalPanelPosition.x + Random.Range(-1f, 1f) * shakeMagnitude,
                originalPanelPosition.y + Random.Range(-1f, 1f) * shakeMagnitude);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panelRect.anchoredPosition = originalPanelPosition;
        feedbackPanelImage.color   = originalPanelColor;
    }
}
