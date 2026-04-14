using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [Header("References")] [Tooltip("플레이어의 인벤토리 스크립트")] [SerializeField]
    private PlayerInventory playerInventory;

    [Header("UI Elements")] [SerializeField]
    private TextMeshProUGUI goldText;

    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private Slider weightSlider;

    [Header("Seed UI")] [Tooltip("현재 어떤 씨앗을 들고 있는지 보여주는 텍스트")] [SerializeField]
    private TextMeshProUGUI currentSeedText;

    [Tooltip("현재 도구/씨앗 슬롯 상태를 보여주는 텍스트")] [SerializeField]
    private TextMeshProUGUI currentSlotText;

    private void OnEnable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += UpdateUI;
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        if (goldText != null)
        {
            goldText.text = $" {playerInventory.gold:N0} G";
        }

        //무게 텍스트 업데이트 (예:  14.5 / 50.0 kg)
        if (weightText != null)
        {
            weightText.text = $"{playerInventory.currentWeight:F1} / {playerInventory.maxWeightPenalty:F1} kg";
        }

        // 무게 게이지 바 업데이트
        if (weightSlider != null)
        {
            // 게이지의 최대치를 인벤토리의 최대 무게로 설정
            weightSlider.maxValue = playerInventory.maxWeightPenalty;
            // 현재 게이지 값을 현재 무게로 설정
            weightSlider.value = playerInventory.currentWeight;
        }

        if (currentSlotText != null)
        {
            string slotName = playerInventory.currentSlot == PlayerInventory.EquipSlot.UniversalHand
                ? " 만능손"
                : " 씨앗 주머니";
            currentSlotText.text = $"장착: {slotName}";
        }

        //  씨앗 정보 업데이트
        if (currentSeedText != null)
        {
            if (playerInventory.currentSlot == PlayerInventory.EquipSlot.Seed)
            {
                // 현재 선택된 씨앗 종류와 개수를 가져옵니다.
                var seedType = playerInventory.currentSelectedSeed;
                int count = 0;
                playerInventory.seedInventory.TryGetValue(seedType, out count);

                currentSeedText.text = $"선택됨: {seedType} ({count}개)";
                currentSeedText.gameObject.SetActive(true);
            }
            else
            {
                // 만능손일 때는 씨앗 정보를 숨깁니다.
                currentSeedText.gameObject.SetActive(false);
            }
        }
    }
}