using UnityEngine;

public class PlayerTrolleyDriver : MonoBehaviour
{
    [Header("Trolley Interaction")] public float interactRange = 1.5f;
    public LayerMask trolleyLayer;

    private PlayerInputReader inputReader;
    private PlayerInventory inventory;
    private Trolley currentTrolley;

    //  추가: 플레이어의 충돌체를 담을 변수
    private Collider2D playerCollider;

    public bool isDrivingTrolley { get; private set; } = false;

    private void Start()
    {
        inputReader = GetComponent<PlayerInputReader>();
        inventory = GetComponent<PlayerInventory>();

        //  추가: 내 충돌체 가져오기
        playerCollider = GetComponent<Collider2D>();

        inputReader.OnInteractEvent += TryInteractWithTrolley;
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.OnInteractEvent -= TryInteractWithTrolley;
    }

    private void Update()
    {
        if (isDrivingTrolley && currentTrolley != null)
        {
            transform.position = currentTrolley.transform.position + new Vector3(0, -0.5f, 0);
        }
    }

    private void FixedUpdate()
    {
        if (isDrivingTrolley && currentTrolley != null)
        {
            Vector2 moveInput = inputReader.MoveVector;
            currentTrolley.Drive(moveInput);
        }
    }

    private void TryInteractWithTrolley()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, trolleyLayer);

        if (hit != null)
        {
            Trolley trolley = hit.GetComponent<Trolley>();

            // 상태 A: 리어카를 운전 중이었다면 -> 놓기
            if (isDrivingTrolley)
            {
                Physics2D.IgnoreCollision(playerCollider, currentTrolley.GetComponent<Collider2D>(), false);

                isDrivingTrolley = false;
                currentTrolley = null;
                Debug.Log("✋ 리어카 손잡이를 놓았습니다.");
                return;
            }

            // 🔥 상태 B: 내 주머니(List)에 작물이 있다면 -> 리어카에 전부 싣기
            if (inventory.heldItems.Count > 0)
            {
                // 주머니에 있는 모든 작물을 순회하며 수레 스택에 하나씩 Push!
                foreach (StackedCrop item in inventory.heldItems)
                {
                    trolley.PushCrop(item);
                }

                // 다 실었으니 주머니 비우기
                inventory.ClearInventory();
                Debug.Log($"🛒 주머니의 작물 {inventory.heldItems.Count}개를 모두 수레에 실었습니다!");
                return;
            }

            // 🔥 상태 C: 내 주머니가 비어있다면 -> 운전 시작
            if (inventory.heldItems.Count == 0)
            {
                isDrivingTrolley = true;
                currentTrolley = trolley;

                Physics2D.IgnoreCollision(playerCollider, currentTrolley.GetComponent<Collider2D>(), true);

                Debug.Log("🏎️ 리어카 손잡이를 잡았습니다! 운전 시작!");
                return;
            }
        }
    }
}