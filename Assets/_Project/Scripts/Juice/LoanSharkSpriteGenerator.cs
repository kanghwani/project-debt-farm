using UnityEngine;

/// <summary>
/// 런타임에 16×32 픽셀아트 사채업자 스프라이트를 절차적으로 생성한다.
/// LoanSharkController 오브젝트의 SpriteRenderer에 자동 적용.
///
/// 디자인: 검은 정장, 중절모, 흰 셔츠, 붉은 넥타이, 매서운 눈빛.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LoanSharkSpriteGenerator : MonoBehaviour
{
    [Header("생성 설정")]
    [SerializeField] private int pixelsPerUnit = 16;
    [SerializeField] private bool generateOnStart = true;

    // ── 팔레트 ────────────────────────────────────────────────────────────────
    private static readonly Color32 TRANS = new Color32(0,   0,   0,   0);   // 투명
    private static readonly Color32 BLACK = new Color32(20,  18,  22,  255); // 진한 검정
    private static readonly Color32 SUIT  = new Color32(38,  35,  45,  255); // 정장 (짙은 남보라)
    private static readonly Color32 SUIT2 = new Color32(55,  50,  65,  255); // 정장 하이라이트
    private static readonly Color32 HAT   = new Color32(28,  25,  35,  255); // 모자
    private static readonly Color32 SKIN  = new Color32(220, 175, 130, 255); // 피부
    private static readonly Color32 SKIN2 = new Color32(190, 145, 100, 255); // 피부 그림자
    private static readonly Color32 WHITE = new Color32(235, 230, 220, 255); // 셔츠
    private static readonly Color32 RED   = new Color32(190, 30,  30,  255); // 넥타이
    private static readonly Color32 RED2  = new Color32(140, 20,  20,  255); // 넥타이 그림자
    private static readonly Color32 EYE   = new Color32(220, 60,  60,  255); // 눈 (붉은빛)
    private static readonly Color32 HAIR  = new Color32(15,  12,  18,  255); // 머리카락
    private static readonly Color32 SHOE  = new Color32(30,  20,  15,  255); // 구두
    private static readonly Color32 GOLD  = new Color32(200, 160, 50,  255); // 시계줄/핀

    // 16×32 픽셀맵 (행0=맨 아래, 행31=맨 위 — Unity UV 좌표계)
    // X: 왼→오른(0→15), Y: 아래→위(0→31)
    // 각 행은 왼쪽→오른쪽 순서의 Color32 배열
    private Color32[] BuildPixels()
    {
        Color32[] px = new Color32[16 * 32];
        for (int i = 0; i < px.Length; i++) px[i] = TRANS;

        // 헬퍼
        void Set(int x, int y, Color32 c)
        {
            if (x < 0 || x >= 16 || y < 0 || y >= 32) return;
            px[y * 16 + x] = c;
        }
        void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
                Set(x + dx, y + dy, c);
        }

        // ── 구두 (y0~1) ──────────────────────────────────────────────────────
        Rect(4, 0, 3, 2, SHOE);
        Rect(9, 0, 3, 2, SHOE);

        // ── 다리 (y2~9) ──────────────────────────────────────────────────────
        Rect(4, 2, 3, 8, SUIT);
        Rect(9, 2, 3, 8, SUIT);
        Set(6, 5, SUIT2); Set(11, 5, SUIT2);   // 다리 하이라이트

        // ── 하체/허리 (y10~12) ───────────────────────────────────────────────
        Rect(4, 10, 8, 3, SUIT);
        Set(7, 10, SUIT2); Set(8, 10, SUIT2);

        // ── 몸통 정장 (y13~21) ───────────────────────────────────────────────
        Rect(3, 13, 10, 9, SUIT);
        // 셔츠 흰 부분
        Rect(6, 13, 4, 7, WHITE);
        // 넥타이
        Rect(7, 14, 2, 6, RED);
        Set(7, 17, RED2); Set(8, 17, RED2);
        // 정장 라펠 하이라이트
        Set(5, 15, SUIT2); Set(10, 15, SUIT2);
        Set(5, 18, SUIT2); Set(10, 18, SUIT2);
        // 황금 시계줄 힌트
        Set(4, 16, GOLD);

        // ── 어깨·팔 (y18~21) ─────────────────────────────────────────────────
        Rect(1, 18, 3, 5, SUIT);   // 왼팔
        Rect(12, 18, 3, 5, SUIT);  // 오른팔
        // 손 (살짝 드러남)
        Set(2, 18, SKIN); Set(13, 18, SKIN);

        // ── 목 (y22~23) ──────────────────────────────────────────────────────
        Rect(6, 22, 4, 2, SKIN);
        Set(7, 22, SKIN2); Set(8, 22, SKIN2);

        // ── 얼굴 (y24~28) ────────────────────────────────────────────────────
        Rect(4, 24, 8, 5, SKIN);
        // 윤곽 어둠
        Set(4, 24, SKIN2); Set(11, 24, SKIN2);
        Set(4, 28, SKIN2); Set(11, 28, SKIN2);
        // 눈 (좁고 매서운)
        Set(5, 27, EYE);  Set(6, 27, BLACK);
        Set(9, 27, EYE);  Set(10, 27, BLACK);
        // 눈썹 (찌푸림)
        Set(5, 28, HAIR); Set(6, 28, HAIR);
        Set(9, 28, HAIR); Set(10, 28, HAIR);
        // 코
        Set(7, 26, SKIN2); Set(8, 26, SKIN2);
        // 입 (일자, 위협적)
        Set(6, 24, BLACK); Set(7, 24, BLACK); Set(8, 24, BLACK); Set(9, 24, BLACK);

        // ── 귀 ───────────────────────────────────────────────────────────────
        Set(3, 26, SKIN); Set(3, 27, SKIN);
        Set(12, 26, SKIN); Set(12, 27, SKIN);

        // ── 중절모 (y29~31) ──────────────────────────────────────────────────
        // 챙
        Rect(2, 29, 12, 1, HAT);
        Set(2, 29, BLACK); Set(13, 29, BLACK);
        // 모자 본체
        Rect(4, 30, 8, 2, HAT);
        Set(4, 31, BLACK); Set(11, 31, BLACK);
        // 모자 밴드 (GOLD)
        Rect(4, 29, 8, 1, GOLD);

        return px;
    }

    private void Start()
    {
        // 유저가 지정한 스프라이트를 덮어쓰지 않도록 자동 생성 기능을 비활성화합니다.
        // if (generateOnStart) Apply();
    }

    [ContextMenu("스프라이트 생성/갱신 (비활성화됨)")]
    public void Apply()
    {
        Debug.LogWarning("[LoanSharkSprite] 자동 생성이 비활성화되었습니다. 인스펙터에 지정된 스프라이트를 유지합니다.");
        /*
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        var tex = new Texture2D(16, 32, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp,
        };
        tex.SetPixels32(BuildPixels());
        tex.Apply();

        sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 16, 32),
            new Vector2(0.5f, 0f),   // 피벗: 발 중앙
            pixelsPerUnit);

        Debug.Log("[LoanSharkSprite] 스프라이트 생성 완료 (16×32px)");
        */
    }
}
