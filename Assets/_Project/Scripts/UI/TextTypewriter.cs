using System;
using System.Collections;
using TMPro;
using UnityEngine;

// 책임: TextMeshPro 텍스트를 한 글자씩 타이핑하듯 출력한다
public class TextTypewriter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float charDelay = 0.025f;
    [SerializeField] private AudioSource typeSound;

    [Header("Text Style")]
    [SerializeField] private TextAlignmentOptions alignment = TextAlignmentOptions.Left;
    [SerializeField] private float fontSize = 40f;

    public bool IsPlaying { get; private set; }
    public event Action OnComplete;

    private void Awake()
    {
        // Inspector에서 연결 안 됐으면 같은 오브젝트에서 자동 탐색
        if (textMesh == null)
            textMesh = GetComponent<TextMeshProUGUI>();

        textMesh.alignment = alignment;
        textMesh.fontSize  = fontSize;
    }

    private Coroutine _routine;

    public void Play(string text, float overrideDelay = -1f)
    {
        if (_routine != null) StopCoroutine(_routine);
        textMesh.text = text;
        textMesh.ForceMeshUpdate();
        textMesh.maxVisibleCharacters = 0;
        float delay = overrideDelay > 0 ? overrideDelay : charDelay;
        _routine = StartCoroutine(TypeRoutine(delay));
    }

    public void SkipToEnd()
    {
        if (_routine != null) StopCoroutine(_routine);
        textMesh.maxVisibleCharacters = int.MaxValue;
        IsPlaying = false;
        OnComplete?.Invoke();
    }

    public void Clear()
    {
        if (_routine != null) StopCoroutine(_routine);
        textMesh.text = "";
        textMesh.maxVisibleCharacters = 0;
        IsPlaying = false;
    }

    private IEnumerator TypeRoutine(float delay)
    {
        IsPlaying = true;
        int total = textMesh.textInfo.characterCount;
        float lastSoundTime = 0f;
        float soundCooldown = 0.04f; // 너무 잦은 소리 겹침 방지 (초 단위)

        for (int i = 0; i <= total; i++)
        {
            textMesh.maxVisibleCharacters = i;

            // 공백이 아닐 때 소리 재생 (쿨타임 적용)
            if (i > 0 && i <= total)
            {
                char c = textMesh.textInfo.characterInfo[i - 1].character;
                if (!char.IsWhiteSpace(c) && Time.time - lastSoundTime >= soundCooldown)
                {
                    if (typeSound != null) 
                        typeSound.Play();
                    else 
                        AudioManager.PlaySFX(SfxType.IntroTyping); // 임시로 지정해둔 타이핑 타입

                    lastSoundTime = Time.time;
                }
            }

            yield return new WaitForSeconds(delay);
        }

        IsPlaying = false;
        OnComplete?.Invoke();
    }
}
