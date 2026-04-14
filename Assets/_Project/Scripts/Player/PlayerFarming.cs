using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFarming : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Grid오브젝트를 넣으세요 ")] 
    [SerializeField] private Grid MapGrid;

    [Tooltip("네모 커서 넣기")] 
    [SerializeField] private Transform targetCursor;

    [Header("Settings")] [Tooltip("캐릭터 방향 부터 얼만큼")] [SerializeField]
    private float targetDistance = 1.0f;
    
    [SerializeField] private FarmingManager farmingManager;
    private PlayerInventory inventory;
    
    private Rigidbody2D rb;
    private Vector2 currentFacingDir = Vector2.down;
    
    [Header("Input")]
    [SerializeField] private PlayerInputReader inputReader;


    
    
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inventory = GetComponent<PlayerInventory>();
        
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
    }
    private void OnEnable()
    {
        inputReader.OnInteractEvent += HandleInteract;
        inputReader.OnTool1Event += EquipUniversalHand;
        inputReader.OnTool2Event += EquipSeed;
    }
    private void OnDisable()
    {
        inputReader.OnInteractEvent -= HandleInteract;
        inputReader.OnTool1Event -= EquipUniversalHand;
        inputReader.OnTool2Event -= EquipSeed;
    }

    private void Update()
    {
        UpdateFacingDirection();
        UpdateCursorPosition();
        
        }
    
    private void UpdateFacingDirection()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 moveDir = rb.linearVelocity.normalized;

            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
            {
                currentFacingDir = moveDir.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                currentFacingDir = moveDir.y > 0 ? Vector2.up : Vector2.down;
            }
            
        }
        
    }

    private void UpdateCursorPosition()
    {
        // 1. 내 위치에서 내가 바라보는 방향으로 targetDistance(1칸)만큼 떨어진 좌표를 구합니다.
        Vector3 lookPosition = transform.position + (Vector3)currentFacingDir * targetDistance;

        // 2. 유니티 Grid 마법: 그 좌표가 속한 '타일 맵의 칸(Cell) 좌표'를 구합니다.
        Vector3Int cellPos = MapGrid.WorldToCell(lookPosition);

        // 3. 커서를 그 타일 칸의 '정중앙'으로 자석처럼 이동시킵니다!
        targetCursor.position = MapGrid.GetCellCenterWorld(cellPos);
    }
    private void HandleInteract()
    {
        Vector3 lookPosition = transform.position + (Vector3)currentFacingDir * targetDistance;
        Vector3Int cellPos = MapGrid.WorldToCell(lookPosition);
        
        farmingManager.InteractWithTile(cellPos, inventory.currentSlot, inventory);
    }
    private void EquipUniversalHand()
    {
        inventory.currentSlot = PlayerInventory.EquipSlot.UniversalHand;
        Debug.Log("장착: 🦾 만능손 (상황에 맞춰 알아서 작동)");
    }

    private void EquipSeed()
    {
        if (inventory.currentSlot == PlayerInventory.EquipSlot.Seed)
        {
            inventory.CycleSeed();
        }
        else
        {
            inventory.currentSlot = PlayerInventory.EquipSlot.Seed;
            Debug.Log($"장착: 🌱 {inventory.currentSelectedSeed} 씨앗");
        }
    }

    
}
