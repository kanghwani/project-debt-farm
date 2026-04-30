using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class UiManager : MonoBehaviour
{
    [Header("References")] 
    [Tooltip("플레이어의 인벤토리 스크립트")] 
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI Elements")] 
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI debtText;
    [SerializeField] private TextMeshProUGUI strikeText;

    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private Slider weightSlider;

    [Header("Seed UI")]
    [SerializeField] private TextMeshProUGUI currentSeedText;
    [SerializeField] private TextMeshProUGUI currentSlotText;

    // Trend UI는 TrendUI.cs 컴포넌트로 분리됨

    private void Start()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += UpdateInventoryUI;
            UpdateInventoryUI();
        }

        TimeManager.OnDayChanged += UpdateDebtUI;
        UpdateDebtUI();

        DebtManager.OnStrikeUpdated += UpdateStrikeUI;

        if (DebtManager.Instance != null)
        {
            UpdateStrikeUI(DebtManager.Instance.currentStrikes); 
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= UpdateInventoryUI;
        }
        
        TimeManager.OnDayChanged -= UpdateDebtUI;
        DebtManager.OnStrikeUpdated -= UpdateStrikeUI;
    }

    private void UpdateInventoryUI()
    {
        if (goldText != null)
        {
            goldText.text = $" {playerInventory.gold:N0} G";
        }

        if (weightText != null)
        {
            weightText.text = $"{playerInventory.currentWeight:F1} / {playerInventory.maxWeightPenalty:F1} kg";
        }

        if (weightSlider != null)
        {
            weightSlider.maxValue = playerInventory.maxWeightPenalty;
            weightSlider.value = playerInventory.currentWeight;
        }

        if (currentSlotText != null)
        {
            string slotName = playerInventory.currentSlot switch
            {
                PlayerInventory.EquipSlot.UniversalHand => "만능손",
                PlayerInventory.EquipSlot.Seed          => "씨앗 주머니",
                PlayerInventory.EquipSlot.Fertilizer    => "비료",
                _                                       => "만능손",
            };
            currentSlotText.text = $"장착: {slotName}";
        }

        if (currentSeedText != null)
        {
            switch (playerInventory.currentSlot)
            {
                case PlayerInventory.EquipSlot.Seed:
                {
                    var seedType = playerInventory.currentSelectedSeed;
                    playerInventory.seedInventory.TryGetValue(seedType, out int count);
                    string displayName = seedType.ToString();
                    if (DataManager.Instance != null)
                    {
                        CropData data = DataManager.Instance.GetCrop(seedType);
                        if (data != null) displayName = data.cropName;
                    }
                    currentSeedText.text = $"선택됨: {displayName} ({count}개)";
                    currentSeedText.gameObject.SetActive(true);
                    break;
                }
                case PlayerInventory.EquipSlot.Fertilizer:
                {
                    var fertType = playerInventory.currentSelectedFertilizer;
                    if (fertType != Fertilizer.None)
                    {
                        playerInventory.fertilizerBag.TryGetValue(fertType, out int count);
                        string displayName = fertType.ToString();
                        if (DataManager.Instance != null)
                        {
                            FertilizerData fd = DataManager.Instance.GetFertilizer(fertType);
                            if (fd != null) displayName = fd.displayName;
                        }
                        currentSeedText.text = $"선택됨: {displayName} ({count}개)";
                        currentSeedText.gameObject.SetActive(true);
                    }
                    else
                    {
                        currentSeedText.text = "비료 없음";
                        currentSeedText.gameObject.SetActive(true);
                    }
                    break;
                }
                default:
                    currentSeedText.gameObject.SetActive(false);
                    break;
            }
        }
    }

    private void UpdateDebtUI()
    {
        if (debtText != null && DebtManager.Instance != null)
        {
            int todayDebt = DebtManager.Instance.GetTodayDebt();
            debtText.text = $"오늘의 빚: {todayDebt} G";
        }
    }

    private void UpdateStrikeUI(int strikes)
    {
        if (strikeText == null) return;

        // 슬롯 아이콘 형태로 표시
        strikeText.text = strikes switch
        {
            0 => "❤️ ❤️ ❤️",
            1 => "<color=#FF3333>💔</color> ❤️ ❤️",
            2 => "<color=#FF3333>💔 💔</color> ❤️",
            _ => "<color=#FF3333>💔 💔 💔</color>",
        };

        // 1연체 이상이면 흔들림 + 빨간색
        if (strikes >= 1)
        {
            strikeText.color = Color.white; // rich text 색상이 우선이므로 white 유지
            RectTransform rt = strikeText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.DOKill();
                rt.DOShakeAnchorPos(0.45f, new Vector2(10f, 4f), 18)
                  .SetUpdate(true);
            }
        }
    }
}