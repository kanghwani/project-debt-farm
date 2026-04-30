using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFarming : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Grid오브젝트를 넣으세요 ")] 
    [SerializeField] private Grid MapGrid;

    [Tooltip("네모 커서 넣기")] 
    [SerializeField] private Transform targetCursor;
    
    [Header("Cursor Colors")]
    [SerializeField] private SpriteRenderer cursorSprite; // 커서의 SpriteRenderer 연결 필요!
    [SerializeField] private Color canInteractColor = new Color(1f, 1f, 1f, 0.8f); // 흰색 반투명 (가능)
    [SerializeField] private Color cannotInteractColor = new Color(1f, 0f, 0f, 0.5f); // 빨간 반투명 (불가능)
    
    [Header("Cursor Settings")]
    [Tooltip("커서가 부드럽게 따라오는 속도")]
    [SerializeField] private float cursorMoveSpeed = 15f;

    [Header("Settings")] [Tooltip("캐릭터 방향 부터 얼만큼")] [SerializeField]
    private float targetDistance = 1.0f;
    
    public static PlayerFarming Instance { get; private set; }
    public Vector3Int CurrentCursorCell { get; private set; }

    [SerializeField] private FarmingManager farmingManager;
    private PlayerInventory inventory;

    private Rigidbody2D rb;
    private Vector2 currentFacingDir = Vector2.down;
    
    [Header("Input")]
    [SerializeField] private PlayerInputReader inputReader;


    
    
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rb = GetComponent<Rigidbody2D>();
        inventory = GetComponent<PlayerInventory>();

        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();

        // 커서가 어떤 오브젝트의 자식이든 씬 루트로 분리
        // → Rigidbody2D 인터폴레이션 or 부모 DOTween이 커서 위치에 간섭하지 않도록
        if (targetCursor != null && targetCursor.parent != null)
            targetCursor.SetParent(null, worldPositionStays: true);
    }
    private void OnEnable()
    {
        inputReader.OnInteractEvent += HandleInteract;
        inputReader.OnTool1Event    += EquipUniversalHand;
        inputReader.OnTool2Event    += EquipSeed;
        inputReader.OnTool3Event    += EquipFertilizer;
    }
    private void OnDisable()
    {
        inputReader.OnInteractEvent -= HandleInteract;
        inputReader.OnTool1Event    -= EquipUniversalHand;
        inputReader.OnTool2Event    -= EquipSeed;
        inputReader.OnTool3Event    -= EquipFertilizer;
    }

    private void Update()
    {
        UpdateFacingDirection();
        UpdateCursorPosition();
        UpdateCursorColor();
        
        }
    
    private void UpdateFacingDirection()
    {
        // rb.linearVelocity 대신 inputReader.MoveVector 사용
        // → 물리 속도는 충돌/마찰로 대각선 값이 생길 수 있음
        // → 입력 벡터는 키 상태 그대로라 더 안정적
        Vector2 input = inputReader != null ? inputReader.MoveVector : Vector2.zero;
        if (input.sqrMagnitude < 0.01f) return;

        float ax = Mathf.Abs(input.x);
        float ay = Mathf.Abs(input.y);

        // 한 축이 다른 축보다 0.15 이상 클 때만 방향 갱신
        // 두 축이 비슷하면 (대각선 입력) currentFacingDir 유지 → 커서 떨림 방지
        if (ax > ay + 0.15f)
            currentFacingDir = input.x > 0 ? Vector2.right : Vector2.left;
        else if (ay > ax + 0.15f)
            currentFacingDir = input.y > 0 ? Vector2.up : Vector2.down;
        // else: 대각선 → 마지막 확정 방향 유지
    }

    private void UpdateCursorPosition()
    {
        Vector3 lookPosition = transform.position + (Vector3)currentFacingDir * targetDistance;
        Vector3Int cellPos   = MapGrid.WorldToCell(lookPosition);
        cellPos.z = 0;
        CurrentCursorCell = cellPos;

        // 타일 정중앙으로 즉시 스냅
        // Lerp는 targetCursor가 플레이어 자식일 때 부모 이동과 중첩돼 누적 오차 발생
        Vector3 targetPos = MapGrid.GetCellCenterWorld(cellPos);
        targetCursor.position = targetPos;
    }
    private void HandleInteract()
    {
        if (FarmingInteraction.Instance == null) return;

        Vector3 lookPosition = transform.position + (Vector3)currentFacingDir * targetDistance;
        Vector3Int cellPos = MapGrid.WorldToCell(lookPosition);

        Vector2Int facingInt = new Vector2Int(
            Mathf.RoundToInt(currentFacingDir.x),
            Mathf.RoundToInt(currentFacingDir.y));
        FarmingInteraction.Instance.InteractWithTile(cellPos, facingInt, inventory.currentSlot, inventory);
    }
    private void EquipUniversalHand()
    {
        inventory.currentSlot = PlayerInventory.EquipSlot.UniversalHand;
        ShowEquipText("🦾 만능손", Color.white);
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
        }

        string seedName = inventory.currentSelectedSeed.ToString();
        if (DataManager.Instance != null)
        {
            CropData cd = DataManager.Instance.GetCrop(inventory.currentSelectedSeed);
            if (cd != null) seedName = cd.cropName;
        }
        inventory.seedInventory.TryGetValue(inventory.currentSelectedSeed, out int count);
        ShowEquipText($"🌱 {seedName} ({count}개)", new Color(0.4f, 1f, 0.4f));
    }

    private void EquipFertilizer()
    {
        if (inventory.currentSlot == PlayerInventory.EquipSlot.Fertilizer)
        {
            inventory.CycleFertilizer();
        }
        else
        {
            inventory.currentSlot = PlayerInventory.EquipSlot.Fertilizer;
        }

        Fertilizer sel = inventory.currentSelectedFertilizer;
        if (sel == Fertilizer.None)
        {
            ShowEquipText("🧪 비료 없음", Color.gray);
        }
        else
        {
            string fertName = sel.ToString();
            Color fertColor = Color.cyan;
            if (DataManager.Instance != null)
            {
                FertilizerData fd = DataManager.Instance.GetFertilizer(sel);
                if (fd != null) { fertName = fd.displayName; fertColor = fd.vfxColor; }
            }
            inventory.fertilizerBag.TryGetValue(sel, out int count);
            ShowEquipText($"🧪 {fertName} ({count}개)", fertColor);
        }
    }

    private void ShowEquipText(string text, Color color)
    {
        if (FarmingInteraction.Instance == null) return;
        // 플레이어 머리 위 (y+1.2 오프셋)
        Vector3 headPos = transform.position + new Vector3(0f, 1.2f, 0f);
        FarmingInteraction.Instance.ShowFloatingText(headPos, text, color);
    }
    
    private void UpdateCursorColor()
    {
        if (cursorSprite == null || FarmingInteraction.Instance == null) return;

        Vector3 lookPosition = transform.position + (Vector3)currentFacingDir * targetDistance;
        Vector3Int cellPos = MapGrid.WorldToCell(lookPosition);

        // 👇 매개변수에 inventory를 추가해서 넘겨줍니다!
        bool canDoSomething = FarmingInteraction.Instance.CanInteract(cellPos, inventory.currentSlot, inventory);

        if (canDoSomething)
        {
            cursorSprite.color = canInteractColor;
        }
        else
        {
            cursorSprite.color = cannotInteractColor;
        }
    }

    
}
