using UnityEngine;
using DG.Tweening;

public class PlayerVisuals : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private Transform spriteTranform;

    [SerializeField] private SpriteRenderer spriteRender;
    
    [SerializeField] private ParticleSystem dustParticle;
    
    [Header("Bounce Settings")] 
    [SerializeField] private float bounceSpeed = 0.15f;

    [SerializeField] private float bounceAmount = 0.85f;

    private Tween walkBounceTween;
    private bool isBouncing = false;

    private void Update()
    {
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            if (rb.linearVelocity.x < -0.01f) spriteRender.flipX = true;
            else if ( rb.linearVelocity.x > 0.01f) spriteRender.flipX = false;
        }
        
        if (isMoving && !isBouncing)
        {
            StartBounce();
        }
        else if (!isMoving && isBouncing)
        {
            StopBounce();
        }
    }

    private void StopBounce()
    {
        isBouncing = false;

        if (dustParticle != null) dustParticle.Stop();
        walkBounceTween.Kill();
        spriteTranform.DOScale(1f, 0.1f);
    }

    private void StartBounce()
    {
        isBouncing = true;
        
        if (dustParticle != null) dustParticle.Play();

        walkBounceTween = spriteTranform.DOScale(bounceAmount, bounceSpeed).SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}
