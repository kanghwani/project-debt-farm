using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 책임: 인트로 씬 전체 흐름 제어
// 스토리 타이핑 → 조작 설명 → GameScene 로드
public class IntroSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextTypewriter typewriter;
    [SerializeField] private TextMeshProUGUI hintText;      // 힌트 텍스트
    [SerializeField] private CanvasGroup storyGroup;        // 스토리 패널
    [SerializeField] private CanvasGroup controlsGroup;     // 조작 설명 패널
    [SerializeField] private CanvasGroup screenFade;        // 검정 페이드 오버레이

    [Header("Skip")]
    [Tooltip("우하단 스킵 버튼 (Canvas에 만들어서 연결)")]
    [SerializeField] private Button skipButton;

    [Header("BGM")]
    [SerializeField] private AudioClip introBGM;  // 비워두면 StartScene BGM 그대로 유지

    [Header("Settings")]
    [SerializeField] private float fadeDuration      = 0.6f;
    [SerializeField] private float linePause         = 0.8f;   // 타이핑 완료 후 자동 넘어가기 전 대기
    [SerializeField] private float autoAdvanceDelay  = 1.4f;   // 다음 줄로 넘어가기 전 추가 대기
    [SerializeField] private string gameSceneName = "GameScene";

    // ── 스토리 라인 ───────────────────────────────────────────────────────────
    private readonly string[] storyLines =
    {
        "나는 평생 그 양반... 아니, 아버지를 딱 세 번 봤다.",
        "내 기억 속 그는 항상 무언가에 쫓기듯 짐을 싸는 뒷모습뿐이었다.",
        "나는 저렇게 살기 싫었다. 그래서 악착같이 위로 기어 올라갔다.",
        "도시의 차가운 빌딩 숲, 내 이름이 적힌 번듯한 오피스.\n나는 내 힘으로 성공했다.",
        "그런데 며칠 전, 변호사에게서 유산 상속 통지서가 날아왔다.",
        "이름도 모를 시골 촌구석의 낡은 농장.",
        "푼돈이라도 건질까 싶어 비웃으며 내려간 그곳에서...",
        "나는 섣불리 상속에 동의한 내 오만함을 저주해야 했다.",
        "진짜 상속된 건 흙먼지 날리는 땅 쪼가리가 아니었다.",
        "그 땅을 담보로 빌린,\n내 평생의 자산을 털어 넣어도 모자랄 <b>'천문학적인 사채 빚'</b>이었다.",
        "이미 상속 포기 기간은 지나버렸고,\n놈들의 덫은 완벽하게 내 목을 조였다.",
        "오늘 밤 자정. 첫 수금이 시작된다.",
        "갚지 못하면...\n내가 이 썩어가는 밭의 거름이 될 것이다.",
    };

    private string controlsText =>
        "<size=130%><b>— 조 작 법 —</b></size>\n\n" +

        // ── 기본 조작 ──
        "<color=#AAAAAA><size=85%>■ 기본 조작</size></color>\n" +
        "<b>WASD / 방향키</b>   이동\n" +
        "<b>1</b>  만능손    " +
        "<b>2</b>  씨앗 장착 <size=80%>(재입력 시 종류 변경)</size>    " +
        "<b>3</b>  비료 장착 <size=80%>(재입력 시 종류 변경)</size>\n" +
        "<b>Space</b>   괭이질 · 씨앗 심기 · 물주기 · 비료 뿌리기 · 수확\n\n" +

        // ── 농사 흐름 ──
        "<color=#AAAAAA><size=85%>■ 농사 흐름</size></color>\n" +
        "잔디에서 <b>Space</b>  →  밭 만들기\n" +
        "<b>우물</b> 앞에서 <b>Space</b>  →  물통 충전\n" +
        "밭에서 <b>Space</b>  →  물주기 <size=80%>(씨앗이 있으면 성장 시작)</size>\n" +
        "씨앗 장착 후 밭에서 <b>Space</b>  →  씨앗 심기\n" +
        "작물이 익으면 <b>Space</b>  →  수확\n\n" +

        // ── 타이밍 바 & 등급 ──
        "<color=#AAAAAA><size=85%>■ 타이밍 바 & 작물 등급</size></color>\n" +
        "괭이질·물주기 시 타이밍 바가 등장 → <b>Space 떼는 순간</b> 판정\n" +
        "<color=#888888>BAD +0점</color>  <color=#00CFFF>GOOD +15점</color>  <color=#FFD700>PERFECT +40점</color>  " +
        "<size=80%>(두 행동 누적 → 수확 시 등급 결정)</size>\n" +
        "<color=#888888>C등급 ×0.5</color>  " +
        "<color=#00CFFF>B등급 ×1.0</color>  " +
        "<color=#FF8C00>A등급 ×1.5</color>  " +
        "<color=#FFD700>S등급 ×2.0</color>\n\n" +

        // ── 비료 & 유행 ──
        "<color=#AAAAAA><size=85%>■ 비료 & 유행</size></color>\n" +
        "비료를 심긴 작물에 뿌리면 <b>태그 부여 · 가격 보너스 · 배수 증가</b>\n" +
        "매일 뉴스 확인 — 유행 태그 작물은 <color=#FFD700>가격 폭등</color>, " +
        "비인기 태그는 <color=#FF5555>가격 하락</color>\n" +
        "비료로 작물 태그를 유행에 맞게 조절하면 대박!\n\n" +

        // ── 납품 & 정산 ──
        "<color=#AAAAAA><size=85%>■ 납품 & 자정 정산</size></color>\n" +
        "<b>배송 박스</b> 앞에서 <b>Space</b>  →  인벤토리 전체 납품 <size=80%>(주머니 비워짐)</size>\n" +
        "<color=#FFD700><b>자정</b></color>이 되면 정산 화면 등장 → 오늘 수익 확인 → 빚 자동 차감\n" +
        "3회 연체 시 <color=#FF5555><b>게임 오버</b></color>  |  매일 상환액이 증가함\n\n" +

        "<color=#FFD700><size=110%><b>자정까지 빚을 갚아라.</b></size></color>\n\n" +
        "<size=85%><color=#888888>[ Space — 시작 ]</color></size>";

    // ─────────────────────────────────────────────────────────────────────────

    private enum State { Typing, Waiting, Controls, Done }
    private State _state = State.Typing;
    private int   _lineIndex = 0;
    private bool  _spacePressed = false;

    private bool _skipRequested = false;

    private void Awake()
    {
        controlsGroup.alpha          = 0;
        controlsGroup.interactable   = false;
        controlsGroup.blocksRaycasts = false;
        screenFade.alpha             = 1f;

        if (skipButton != null)
            skipButton.onClick.AddListener(RequestSkip);
    }

    private void Start()
    {
        // introBGM이 지정된 경우만 트랙 교체 (없으면 이전 씬 BGM 그대로 유지)
        // BGMManager가 씬에 없으면 아무 소리도 안 남 → IntroScene에도 BGMManager 오브젝트 필요
        if (BGMManager.Instance == null)
            Debug.LogWarning("[IntroSequence] BGMManager.Instance가 null입니다. " +
                             "IntroScene에 BGMManager 오브젝트가 있는지 확인하세요.");

        if (introBGM != null)
            BGMManager.Instance?.Play(introBGM);

        StartCoroutine(RunIntro());
    }

    /// <summary>스킵 버튼 클릭 시 호출. RunIntro 코루틴이 다음 분기에서 종료된다.</summary>
    private void RequestSkip()
    {
        if (_state == State.Done) return;
        _skipRequested = true;
        if (skipButton != null) skipButton.interactable = false;  // 중복 클릭 차단
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            _spacePressed = true;
    }

    // ── 메인 흐름 ─────────────────────────────────────────────────────────────
    private IEnumerator RunIntro()
    {
        // 페이드 인
        yield return StartCoroutine(Fade(screenFade, 1f, 0f));

        // 두 줄씩 묶어서 표시 (스킵 요청 시 즉시 빠져나감)
        for (int i = 0; i < storyLines.Length; i += 2)
        {
            if (_skipRequested) break;

            string combined = (i + 1 < storyLines.Length)
                ? storyLines[i] + "\n\n" + storyLines[i + 1]
                : storyLines[i];
            yield return StartCoroutine(ShowLine(combined));
        }

        // 스킵된 경우 즉시 GameScene으로
        if (_skipRequested)
        {
            yield return StartCoroutine(LoadGameScene());
            yield break;
        }

        // 스토리 → 조작 설명
        yield return StartCoroutine(Fade(storyGroup, 1f, 0f));
        ShowControls();
        yield return StartCoroutine(Fade(controlsGroup, 0f, 1f));

        _state = State.Controls;
        _spacePressed = false;
        hintText.text = "";   // 조작 설명 패널 안에 "[ Space - 시작 ]" 이 있으므로 별도 힌트 불필요

        yield return new WaitUntil(() => _spacePressed || _skipRequested);

        yield return StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        _state = State.Done;
        BGMManager.Instance?.Stop(fadeDuration);
        yield return StartCoroutine(Fade(screenFade, 0f, 1f));
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator ShowLine(string line)
    {
        _spacePressed = false;
        _state = State.Typing;
        hintText.text = "- 데모 플레이 -";

        typewriter.Play(line);

        // 타이핑 중 Space → 즉시 완성, 스킵 요청 → 즉시 종료
        while (typewriter.IsPlaying)
        {
            if (_skipRequested) { typewriter.SkipToEnd(); yield break; }
            if (_spacePressed)
            {
                typewriter.SkipToEnd();
                _spacePressed = false;
                break;
            }
            yield return null;
        }

        // 타이핑 완료 후 힌트 숨기고 자동 대기 (스킵 시 단축)
        hintText.text = "";
        _state = State.Waiting;

        float waitTotal = linePause + autoAdvanceDelay;
        float waited = 0f;
        while (waited < waitTotal)
        {
            if (_skipRequested) yield break;
            waited += Time.deltaTime;
            yield return null;
        }

        // 페이드 아웃 → 다음 줄 준비
        yield return StartCoroutine(Fade(storyGroup, 1f, 0f));
        typewriter.Clear();
        yield return StartCoroutine(Fade(storyGroup, 0f, 1f));
    }

    private void ShowControls()
    {
        // StoryGroup은 이미 Fade out 됐으므로 텍스트만 정리
        typewriter.Clear();
        controlsGroup.GetComponentInChildren<TextMeshProUGUI>().text = controlsText;
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────
    private IEnumerator Fade(CanvasGroup group, float from, float to)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = to;
    }
}
