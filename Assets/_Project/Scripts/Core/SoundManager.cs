using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioManager.OnPlaySFX 이벤트를 구독해 실제 AudioClip을 재생한다.
/// Inspector에서 SfxType → AudioClip 매핑을 설정한다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [Tooltip("효과음 전용 AudioSource (PlayOneShot 사용)")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX 클립 매핑")]
    [Tooltip("SfxType enum 순서와 일치하는 AudioClip 배열. 비워두면 해당 SFX는 무음.")]
    [SerializeField] private SfxEntry[] sfxEntries;

    // SfxType → AudioClip 빠른 검색용 딕셔너리
    private readonly Dictionary<SfxType, AudioClip> _clipMap    = new();
    // SfxType → 볼륨 배율
    private readonly Dictionary<SfxType, float>     _volumeMap  = new();

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Inspector 배열 → 딕셔너리
        foreach (var entry in sfxEntries)
        {
            if (entry.clip != null)
            {
                _clipMap[entry.sfxType]   = entry.clip;
                // volume이 0이면 실수로 0 설정한 것으로 보고 1로 처리
                _volumeMap[entry.sfxType] = entry.volume > 0f ? entry.volume : 1f;
            }
        }

        // AudioSource 자동 생성
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        AudioManager.OnPlaySFX += HandlePlaySFX;
    }

    private void OnDisable()
    {
        AudioManager.OnPlaySFX -= HandlePlaySFX;
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────────────────────

    private void HandlePlaySFX(SfxType sfx)
    {
        if (_clipMap.TryGetValue(sfx, out AudioClip clip))
            sfxSource.PlayOneShot(clip, _volumeMap.TryGetValue(sfx, out float vol) ? vol : 1f);
        // 클립이 없으면 조용히 무시
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Inspector에서 SfxType과 AudioClip을 쌍으로 연결하기 위한 직렬화 구조체.</summary>
    [System.Serializable]
    public struct SfxEntry
    {
        public SfxType   sfxType;
        public AudioClip clip;
        [Range(0f, 2f)]
        [Tooltip("재생 볼륨. 1=원본, 0.5=절반, 1.3=30% 증가")]
        public float     volume;
    }
}
