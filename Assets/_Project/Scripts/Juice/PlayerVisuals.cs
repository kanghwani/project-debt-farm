using UnityEngine;
using DG.Tweening;

public class PlayerVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform spriteTranform;
    [SerializeField] private SpriteRenderer spriteRender;
    [SerializeField] private ParticleSystem dustParticle;

    [Header("Walk Bounce")]
    [Tooltip("걸을 때 크기가 줄어드는 비율 (1에 가까울수록 덜 흔들림)")]
    [SerializeField] private float walkBounceScale  = 0.95f;
    [Tooltip("한 번 흔들리는 속도 (초)")]
    [SerializeField] private float walkBounceSpeed  = 0.12f;

    [Header("Idle Breathing")]
    [Tooltip("숨쉴 때 올라가는 높이 (유니티 단위)")]
    [SerializeField] private float breathMoveY      = 0.04f;
    [Tooltip("한 번 숨쉬는 속도 (초), 클수록 느린 호흡)")]
    [SerializeField] private float breathSpeed      = 0.9f;

    private bool isWalking = false;

    private void Update()
    {
        Vector2 velocity = rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > 0.01f;

        // Animator 파라미터는 PlayerMove2D가 전담 → 여기선 DOTween 이펙트만 담당
        if (moving && !isWalking)  EnterWalk();
        if (!moving && isWalking)  EnterIdle();
    }

    private void EnterWalk()
    {
        isWalking = true;

        if (dustParticle != null) dustParticle.Play();

        // 기존 트윈 제거 후 새 트윈
        spriteTranform.DOKill();

        // 크기: 살짝 줄었다 원래대로 반복 (걷는 진동감)
        spriteTranform.DOScale(walkBounceScale, walkBounceSpeed)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void EnterIdle()
    {
        isWalking = false;

        if (dustParticle != null) dustParticle.Stop();

        spriteTranform.DOKill();

        // Y 위치를 즉시 0으로 초기화
        // → DOLocalMoveY가 누적 Y를 기준점으로 삼아 호출될 때마다
        //   스프라이트가 조금씩 위로 뜨는 드리프트 방지
        Vector3 lp = spriteTranform.localPosition;
        spriteTranform.localPosition = new Vector3(lp.x, 0f, lp.z);

        // 크기를 1로 복구
        spriteTranform.DOScale(1f, 0.15f).OnComplete(() =>
        {
            // 크기 복구 후 숨쉬기 시작 (y=0 → breathMoveY → y=0 반복)
            spriteTranform.DOLocalMoveY(breathMoveY, breathSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        });
    }

    private void OnDisable()
    {
        // 비활성화 시 트윈 정리 + 원래 상태 복구
        spriteTranform.DOKill();
        spriteTranform.localScale    = Vector3.one;
        spriteTranform.localPosition = new Vector3(
            spriteTranform.localPosition.x, 0f, spriteTranform.localPosition.z);
    }
}
