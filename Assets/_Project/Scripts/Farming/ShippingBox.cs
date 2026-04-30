using UnityEngine;

// 책임: 플레이어·수레에서 작물을 받아 DailySettlementManager에 등록한다.
// 자정 정산·골드 지급은 DailySettlementManager → DailySettlementUI가 담당.
public class ShippingBox : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Tile Block")]
    [SerializeField] private Grid grid;
    [Tooltip("상자 중심 기준 괭이질 차단 반경 (칸 수)")]
    [SerializeField] private int radius = 1;

    public static System.Collections.Generic.HashSet<Vector3Int> BoxCells = new();
    private System.Collections.Generic.List<Vector3Int> _myCells = new();

    private bool            isPlayerNearby  = false;
    private PlayerInventory playerInventory = null;
    private PlayerInputReader playerInput   = null;

    // ── 라이프사이클 ──────────────────────────────────────────────────────────

    private void Start()
    {
        if (grid == null) grid = FindFirstObjectByType<Grid>();

        Vector3Int center = grid.WorldToCell(transform.position);
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        {
            Vector3Int cell = center + new Vector3Int(x, y, 0);
            _myCells.Add(cell);
            BoxCells.Add(cell);
        }
    }

    private void OnDestroy()
    {
        foreach (var cell in _myCells)
            BoxCells.Remove(cell);
    }

    // ── 트리거 ────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 진입
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            if (collision.TryGetComponent(out PlayerInventory inv) &&
                collision.TryGetComponent(out PlayerInputReader input))
            {
                isPlayerNearby = true;
                playerInventory = inv;
                playerInput     = input;
                playerInput.OnInteractEvent += HandleShipping;
                Debug.Log("[ShippingBox] 드론 상자: Space로 납품 가능");
            }
        }

        // 수레 진입
        if (collision.TryGetComponent(out Trolley trolley))
        {
            Debug.Log("[ShippingBox] 수레 납품 시작");
            HandleTrolleyShipping(trolley);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            isPlayerNearby = false;
            if (playerInput != null)
            {
                playerInput.OnInteractEvent -= HandleShipping;
                playerInput = null;
            }
            playerInventory = null;
        }
    }

    // ── 납품 처리 ────────────────────────────────────────────────────────────

    private void HandleShipping()
    {
        if (!isPlayerNearby || playerInventory == null) return;
        if (playerInventory.heldItems.Count == 0) return;

        var items = new System.Collections.Generic.List<StackedCrop>(playerInventory.heldItems);

        foreach (var item in items)
            DailySettlementManager.Instance?.RegisterItem(item);

        playerInventory.ClearInventory();

        ShowShippingFeedback(items, transform.position);
    }

    private void HandleTrolleyShipping(Trolley trolley)
    {
        var items = new System.Collections.Generic.List<StackedCrop>();
        while (trolley.cropStack.Count > 0)
        {
            StackedCrop? item = trolley.PopCrop();
            if (item.HasValue)
            {
                DailySettlementManager.Instance?.RegisterItem(item.Value);
                items.Add(item.Value);
            }
        }

        if (items.Count > 0)
            ShowShippingFeedback(items, transform.position);
    }

    // ── 납품 시각 피드백 ──────────────────────────────────────────────────────

    private void ShowShippingFeedback(System.Collections.Generic.List<StackedCrop> items, Vector3 boxPos)
    {
        if (FarmingInteraction.Instance == null) return;

        int total = 0;
        foreach (var item in items) total += item.price;

        // ── 아이템별 소형 플로팅 텍스트 (등급 색상, 조금씩 오프셋) ──────────
        for (int i = 0; i < items.Count; i++)
        {
            var item  = items[i];
            float xOff = (i - (items.Count - 1) * 0.5f) * 0.55f;  // 좌우 분산
            Vector3 pos = boxPos + new Vector3(xOff, 0.4f + i * 0.3f, 0f);

            string cropName = item.data != null ? item.data.cropName : "작물";
            string label    = $"{GradeIcon(item.grade)}{item.grade} {cropName}\n+{item.price}G";
            Color  col      = GradeColor(item.grade);

            bool   isCombo   = item.jackpotTier >= JackpotTier.Combo;
            float  itemScale = isCombo ? 1.0f : 0.85f;
            var    itemStyle = isCombo ? HarvestJuiceStyle.Combo : HarvestJuiceStyle.None;
            FarmingInteraction.Instance.ShowFloatingText(pos, label, col, itemScale, itemStyle);
        }

        // ── 합계 대형 플로팅 텍스트 (박스 위 중앙) ───────────────────────────
        Vector3 totalPos = boxPos + new Vector3(0f, 1.6f + items.Count * 0.15f, 0f);

        string totalLabel;
        HarvestJuiceStyle totalStyle;
        Color totalColor;

        if (total >= 1000)
        {
            totalLabel  = $"📦 납품 완료!\n+{total}G 💰";
            totalStyle  = HarvestJuiceStyle.Jackpot;
            totalColor  = new Color(1f, 0.85f, 0.1f);   // 금색
        }
        else
        {
            totalLabel  = $"📦 납품 완료!  +{total}G";
            totalStyle  = HarvestJuiceStyle.Combo;
            totalColor  = new Color(0.6f, 1f, 0.6f);    // 연두색
        }

        FarmingInteraction.Instance.ShowFloatingText(totalPos, totalLabel, totalColor, 1.2f, totalStyle);

        // ── SFX ──────────────────────────────────────────────────────────────
        AudioManager.PlaySFX(SfxType.ShopBuy);
    }

    private static string GradeIcon(string grade) => grade switch
    {
        "S" => "✨",
        "A" => "⭐",
        "B" => "🌿",
        _   => ""
    };

    private static Color GradeColor(string grade) => grade switch
    {
        "S" => new Color(1f,   0.84f, 0f),    // 금색
        "A" => new Color(0.6f, 0.9f,  1f),    // 하늘색
        "B" => new Color(0.7f, 1f,    0.7f),  // 연두색
        _   => new Color(0.8f, 0.8f,  0.8f)   // 회색
    };

    // ── Gizmo ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (grid == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
        Vector3Int center = grid.WorldToCell(transform.position);
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        {
            Vector3 worldPos = grid.CellToWorld(center + new Vector3Int(x, y, 0))
                               + grid.cellSize * 0.5f;
            Gizmos.DrawWireCube(worldPos, grid.cellSize);
        }
    }
}
