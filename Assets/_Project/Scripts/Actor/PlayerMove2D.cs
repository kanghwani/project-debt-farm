using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove2D : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Reference")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerInputReader inputReader;

    [Tooltip("무게 페널티를 계산하기 위한 인벤토리")]
    [SerializeField] private PlayerInventory inventory;

    // 애니메이터를 조종하기 위한 변수
    [SerializeField] private Animator anim;

    public Vector2 CurrentDirection { get; private set; } = Vector2.down;

    [Header("Movement Settings")]
    [Tooltip("아무리 무거워도 보장되는 최소 스피드")]
    public float minSpeed = 0.2f;

    // ── 발걸음 SFX ─────────────────────────────────────────────────────────────
    [Header("Footstep SFX")]
    [Tooltip("발걸음 소리 재생 간격(초). 이동 속도에 맞게 조절.")]
    [SerializeField] private float stepInterval = 0.38f;
    private float _stepTimer = 0f;

    private PlayerTrolleyDriver trolleyDriver;
    
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        
        // 시작할 때 애니메이터 컴포넌트 찾아오기
        if (anim == null) anim = GetComponent<Animator>();
    }

    private void Start()
    {
        trolleyDriver = GetComponent<PlayerTrolleyDriver>();
    }

    private void FixedUpdate()
    {
        // 수레를 탈 때 관성 때문에 미끄러지는 현상 방지
        if (trolleyDriver != null && trolleyDriver.isDrivingTrolley)
        {
            rb.linearVelocity = Vector2.zero;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        // 농사 액션 중 이동 잠금 (타이밍 바 포함)
        // isPlayerBusy가 true인 동안은 속도를 0으로 유지
        if (FarmingInteraction.Instance != null && FarmingInteraction.Instance.isPlayerBusy)
        {
            rb.linearVelocity = Vector2.zero;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        if (inputReader == null) return;

        Vector2 moveInput = inputReader.MoveVector;

        //  (정규화)
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            CurrentDirection = moveInput.normalized;
        }

        // --- 무게 페널티 계산 (기존 코드 완벽함) ---
        float currentSpeed = moveSpeed; 
        float weightRatio = 0f; 

        if (inventory != null && inventory.maxWeightPenalty > 0f)
        {
            weightRatio = inventory.currentWeight / inventory.maxWeightPenalty;
            weightRatio = Mathf.Clamp01(weightRatio); 
            currentSpeed = Mathf.Lerp(moveSpeed, minSpeed, weightRatio);
        }
        
        rb.linearVelocity = moveInput * currentSpeed;

        // ── 애니메이터 연동 ──────────────────────────────────────────────────
        if (anim != null)
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                anim.SetFloat("MoveX", moveInput.x);
                anim.SetFloat("MoveY", moveInput.y);
                CurrentDirection = moveInput;
            }
            anim.SetFloat("Speed", moveInput.sqrMagnitude);
        }

        // ── 발걸음 SFX ────────────────────────────────────────────────────────
        if (moveInput.sqrMagnitude > 0.01f)
        {
            _stepTimer -= Time.fixedDeltaTime;
            if (_stepTimer <= 0f)
            {
                _stepTimer = stepInterval;
                PlayFootstepSFX();
            }
        }
        else
        {
            // 멈추면 타이머 리셋 (재이동 시 즉시 소리)
            _stepTimer = 0f;
        }
    }

    /// <summary>현재 셀이 밭(farmData 존재)이면 Soil, 아니면 Grass 발걸음 SFX.</summary>
    private void PlayFootstepSFX()
    {
        SfxType sfx = SfxType.FootstepGrass;

        var fm = FarmingManager.Instance;
        if (fm != null && fm.baseTilemap != null)
        {
            Vector3Int cell = fm.baseTilemap.WorldToCell(transform.position);
            if (fm.farmData.ContainsKey(cell))
                sfx = SfxType.FootstepSoil;
        }

        AudioManager.PlaySFX(sfx);
    }
}