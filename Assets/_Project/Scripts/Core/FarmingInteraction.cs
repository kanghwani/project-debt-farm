using UnityEngine;
using System.Collections.Generic;

// 책임: 플레이어 입력을 받아 적절한 농사 행동(IFarmAction)을 찾아 실행한다
// 어떤 행동인지는 모른다 → 새 행동 추가 시 이 파일을 건드리지 않아도 된다 (OCP)
public class FarmingInteraction : MonoBehaviour
{
    public static FarmingInteraction Instance { get; private set; }

    [Header("Visual Effects")]
    public GameObject floatingTextPrefab;

    public bool isPlayerBusy { get; private set; } = false;

    // 장착 슬롯별 행동 목록. 위에서부터 순서대로 CanExecute() 를 확인해서 첫 번째 가능한 행동을 실행
    private List<IFarmAction> handActions;        // 만능손
    private List<IFarmAction> seedActions;        // 씨앗 주머니
    private List<IFarmAction> fertilizerActions;  // 비료 (Step 4에서 ApplyFertilizerAction 추가)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 각 액션에 showText 델리게이트를 주입 → 액션 클래스가 FarmingInteraction에 직접 의존하지 않음
        handActions = new List<IFarmAction>
        {
            new FillWaterAction(ShowFloatingText),
            new HarvestAction(ShowFloatingText),        // 오버로드 버전 사용
            new RemoveDeadAction(ShowFloatingText),
            new WaterAction(ShowFloatingText),
            new NoWaterWarningAction(ShowFloatingText),
            new TillAction(ShowFloatingText),
        };

        seedActions = new List<IFarmAction>
        {
            new SeedAction(ShowFloatingText),
        };

        fertilizerActions = new List<IFarmAction>
        {
            new ApplyFertilizerAction(ShowFloatingText),
        };
    }
    
    public bool CanInteract(Vector3Int cellPos, PlayerInventory.EquipSlot currentSlot, PlayerInventory inventory)
    {
        // 플레이어가 이미 행동 중이면 다른 행동을 예약할 수 없으므로 false (빨간색)
        if (isPlayerBusy) return false; 
        
        cellPos.z = 0;

        FarmingManager farm = FarmingManager.Instance;
        if (farm == null) return false;

        // 현재 들고 있는 도구에 따라 검사할 액션 리스트 결정
        List<IFarmAction> actions = currentSlot switch
        {
            PlayerInventory.EquipSlot.UniversalHand => handActions,
            PlayerInventory.EquipSlot.Seed          => seedActions,
            PlayerInventory.EquipSlot.Fertilizer    => fertilizerActions,
            _                                       => handActions,
        };

        // 리스트를 순회하면서 '하나라도' 가능한 행동이 있으면 즉시 true 반환!
        foreach (IFarmAction action in actions)
        {
            if (action.CanExecute(cellPos, inventory, farm))
            {
                return true; // 할 수 있는 행동이 있다 -> 커서 초록색(흰색)!
            }
        }

        // 아무 행동도 불가능하면 false 반환 -> 커서 빨간색!
        return false; 
    }

    public void InteractWithTile(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory.EquipSlot currentSlot, PlayerInventory inventory)
    {
        if (isPlayerBusy) return;
        cellPos.z = 0;

        FarmingManager farm = FarmingManager.Instance;
        if (farm == null)
        {
            Debug.LogError("[FarmingInteraction] FarmingManager를 찾을 수 없습니다!");
            return;
        }

        List<IFarmAction> actions = currentSlot switch
        {
            PlayerInventory.EquipSlot.UniversalHand => handActions,
            PlayerInventory.EquipSlot.Seed          => seedActions,
            PlayerInventory.EquipSlot.Fertilizer    => fertilizerActions,
            _                                       => handActions,
        };

        foreach (IFarmAction action in actions)
        {
            if (action.CanExecute(cellPos, inventory, farm))
            {
                isPlayerBusy = true;
                action.Execute(cellPos, facingDir, inventory, farm, () => isPlayerBusy = false);
                return;
            }
        }
    }

    public void ShowFloatingText(Vector3 pos, string text, Color color)
    {
        if (floatingTextPrefab == null) return;
        Vector3 spawnPos = pos + new Vector3(0, 0.5f, 0);
        GameObject obj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<FloatingText>().Setup(text, color);
    }

    public void ShowFloatingText(Vector3 pos, string text, Color color,
        float sizeScale, HarvestJuiceStyle style)
    {
        if (floatingTextPrefab == null) return;
        Vector3 spawnPos = pos + new Vector3(0, 0.5f, 0);
        GameObject obj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<FloatingText>().Setup(text, color, sizeScale, style);
    }
}
