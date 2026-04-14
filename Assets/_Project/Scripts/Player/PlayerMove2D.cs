using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove2D : MonoBehaviour
{
    
    [Header("Setting")] [SerializeField] private float moveSpeed = 5f;
    
    [Header("Reference")] 
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private PlayerInputReader inputReader;
    
    [Tooltip("무게 페널티를 계산하기 위한 인벤토리")]
    [SerializeField] private PlayerInventory inventory;

    public Vector2 CurrentDirection { get; private set; } = Vector2.down;
    
    [Header("Movement Settings")] 
    [Tooltip("아무리 무거워도 보장되는 최소 스피드")]
    public float minSpeed = 0.2f;
    
    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }
    }

    private void FixedUpdate()
    {
        if (inputReader == null) return;

        Vector2 moveInput = inputReader.MoveVector;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            CurrentDirection = moveInput.normalized;
        }

        float currentSpeed = moveSpeed; 
        float weightRatio = 0f; 

        if (inventory != null && inventory.maxWeightPenalty > 0f)
        {
            // 비율 계산 (0.0 ~ 1.0)
            weightRatio = inventory.currentWeight / inventory.maxWeightPenalty;
            weightRatio = Mathf.Clamp01(weightRatio); 
            

            currentSpeed = Mathf.Lerp(moveSpeed, minSpeed, weightRatio);
        }
        rb.linearVelocity = moveInput * currentSpeed;
    }




}
