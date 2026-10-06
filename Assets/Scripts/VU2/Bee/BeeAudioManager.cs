// =============================================================================
// BeeAudioManager - Quản lý âm thanh môi trường cho game BeeBloom.
// BeeAudioManager - Environmental audio manager for BeeBloom.
//
// CHIẾN LƯỢC ÂM THANH / AUDIO STRATEGY
//   1. AMBIENT: tiếng côn trùng, gió, nước — thay đổi theo EcosystemStatus
//      Thriving  → âm thanh tươi vui, chim hót, ong vo ve nhộn nhịp
//      Stable    → cân bằng, nhẹ nhàng
//      Declining → tiếng gió lạnh hơn, ít côn trùng
//      Collapsed → im lặng đáng sợ + tiếng gió rú
//
//   2. MUSIC: nhạc nền adaptive
//      CrossFade giữa các track theo trạng thái
//
//   3. SFX HUB: hub trung tâm cho các script khác play SFX
//      → Không cần mỗi script tự hold AudioSource riêng
//
// SỬ DỤNG / USAGE
//   BeeAudioManager.Instance.PlaySFX(clip, position)
//   BeeAudioManager.Instance.SetStatus(EcosystemStatus.Thriving)
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BeeAudioManager : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================
    public static BeeAudioManager Instance { get; private set; }

    // =========================================================================
    // AMBIENT SETTINGS / CÀI ĐẶT AMBIENT
    // =========================================================================
    [Header("Ambient Layers / Lớp môi trường")]
    [Tooltip("Tiếng côn trùng/chim khi Thriving.")]
    public AudioClip ambientThriving;

    [Tooltip("Tiếng môi trường ổn định.")]
    public AudioClip ambientStable;

    [Tooltip("Tiếng gió lạnh, ít côn trùng khi Declining.")]
    public AudioClip ambientDeclining;

    [Tooltip("Tiếng im lặng + gió rú khi Collapsed.")]
    public AudioClip ambientCollapsed;

    [Header("Music / Nhạc nền")]
    [Tooltip("Nhạc khi Thriving — vui tươi.")]
    public AudioClip musicThriving;

    [Tooltip("Nhạc cân bằng (Stable).")]
    public AudioClip musicStable;

    [Tooltip("Nhạc căng thẳng khi Declining.")]
    public AudioClip musicDeclining;

    [Header("SFX / Hiệu ứng âm thanh")]
    [Tooltip("Tiếng ong buzz khi bay.")]
    public AudioClip sfxBuzz;

    [Tooltip("Tiếng hút mật.")]
    public AudioClip sfxNectar;

    [Tooltip("Tiếng thụ phấn thành công.")]
    public AudioClip sfxPollinate;

    [Tooltip("Tiếng giao mật về tổ.")]
    public AudioClip sfxHoneyDeliver;

    [Tooltip("Tiếng hoa héo.")]
    public AudioClip sfxFlowerWilt;

    [Tooltip("Tiếng hoa chết.")]
    public AudioClip sfxFlowerDead;

    [Tooltip("Tiếng ong dịch tấn công.")]
    public AudioClip sfxEnemyAttack;

    [Tooltip("Tiếng hoàn thành quest.")]
    public AudioClip sfxQuestComplete;

    [Tooltip("Tiếng win.")]
    public AudioClip sfxWin;

    [Tooltip("Tiếng thua.")]
    public AudioClip sfxLose;

    // =========================================================================
    // VOLUME SETTINGS
    // =========================================================================
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume   = 1f;
    [Range(0f, 1f)] public float ambientVolume   = 0.6f;
    [Range(0f, 1f)] public float musicVolume     = 0.5f;
    [Range(0f, 1f)] public float sfxVolume       = 0.8f;

    [Tooltip("Thời gian crossfade giữa 2 ambient track (giây).")]
    [Range(0.5f, 5f)]
    public float crossfadeDuration = 2f;

    // =========================================================================
    // REFERENCES
    // =========================================================================
    [Header("References")]
    public BeeSustainabilityManager sustainability;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private AudioSource _ambientA, _ambientB;   // crossfade pair
    private AudioSource _musicSource;
    private bool _usingA = true;

    private readonly List<AudioSource> _sfxPool = new();
    private const int SFX_POOL_SIZE = 8;

    private BeeSustainabilityManager.EcosystemStatus _lastStatus
        = BeeSustainabilityManager.EcosystemStatus.Stable;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildAudioSources();

        if (sustainability == null)
            sustainability = FindObjectOfType<BeeSustainabilityManager>();
    }

    private void OnEnable()
    {
        if (sustainability != null)
            sustainability.OnStatusChanged += HandleStatusChanged;
    }

    private void OnDisable()
    {
        if (sustainability != null)
            sustainability.OnStatusChanged -= HandleStatusChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Bắt đầu với Stable
        PlayAmbient(ambientStable);
        PlayMusic(musicStable);
    }

    // =========================================================================
    // HandleStatusChanged - Chuyển ambient và music theo trạng thái.
    // =========================================================================
    private void HandleStatusChanged(BeeSustainabilityManager.EcosystemStatus status)
    {
        if (status == _lastStatus) return;
        _lastStatus = status;

        (AudioClip ambient, AudioClip music) = status switch
        {
            BeeSustainabilityManager.EcosystemStatus.Thriving  => (ambientThriving,  musicThriving),
            BeeSustainabilityManager.EcosystemStatus.Declining => (ambientDeclining, musicDeclining),
            BeeSustainabilityManager.EcosystemStatus.Collapsed => (ambientCollapsed, null),
            _                                                   => (ambientStable,   musicStable)
        };

        if (ambient != null) StartCoroutine(CrossfadeAmbient(ambient));
        if (music   != null) PlayMusic(music);
        if (status == BeeSustainabilityManager.EcosystemStatus.Collapsed)
            PlaySFX(sfxLose, transform.position);

        Debug.Log($"[BeeAudioManager] Status → {status}: ambient={ambient?.name}, music={music?.name}");
    }

    // =========================================================================
    // PlayAmbient - Phát ambient loop (không crossfade, khi khởi tạo).
    // =========================================================================
    private void PlayAmbient(AudioClip clip)
    {
        if (clip == null) return;
        var src = _usingA ? _ambientA : _ambientB;
        src.clip   = clip;
        src.volume = ambientVolume * masterVolume;
        src.Play();
    }

    // =========================================================================
    // CrossfadeAmbient - Crossfade sang ambient track mới.
    // =========================================================================
    private IEnumerator CrossfadeAmbient(AudioClip newClip)
    {
        AudioSource current = _usingA ? _ambientA : _ambientB;
        AudioSource next    = _usingA ? _ambientB : _ambientA;

        float targetVol = ambientVolume * masterVolume;
        next.clip   = newClip;
        next.volume = 0f;
        next.Play();

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float ratio = t / crossfadeDuration;
            current.volume = Mathf.Lerp(targetVol, 0f, ratio);
            next.volume    = Mathf.Lerp(0f, targetVol, ratio);
            yield return null;
        }

        current.Stop();
        current.volume = targetVol;
        _usingA = !_usingA;
    }

    // =========================================================================
    // PlayMusic
    // =========================================================================
    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource == null) return;
        if (_musicSource.clip == clip) return;

        _musicSource.clip   = clip;
        _musicSource.volume = musicVolume * masterVolume;
        _musicSource.Play();
    }

    // =========================================================================
    // PUBLIC API — PlaySFX
    // =========================================================================

    /// <summary>Phát SFX tại vị trí 3D (dùng pool).</summary>
    public void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        var src = GetFreeSFXSource();
        src.transform.position = position;
        src.clip   = clip;
        src.volume = sfxVolume * masterVolume;
        src.Play();
    }

    /// <summary>Phát SFX tại vị trí ong player (shortcut).</summary>
    public void PlaySFX(AudioClip clip) => PlaySFX(clip, transform.position);

    /// <summary>Phát SFX theo tên enum.</summary>
    public void PlaySFX(SFXType type, Vector3 pos)
    {
        AudioClip clip = type switch
        {
            SFXType.Buzz           => sfxBuzz,
            SFXType.Nectar         => sfxNectar,
            SFXType.Pollinate      => sfxPollinate,
            SFXType.HoneyDeliver   => sfxHoneyDeliver,
            SFXType.FlowerWilt     => sfxFlowerWilt,
            SFXType.FlowerDead     => sfxFlowerDead,
            SFXType.EnemyAttack    => sfxEnemyAttack,
            SFXType.QuestComplete  => sfxQuestComplete,
            SFXType.Win            => sfxWin,
            SFXType.Lose           => sfxLose,
            _                      => null
        };
        PlaySFX(clip, pos);
    }

    // =========================================================================
    // SetVolume - Cập nhật volume runtime.
    // =========================================================================
    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        _ambientA.volume = ambientVolume * masterVolume;
        _ambientB.volume = ambientVolume * masterVolume;
        _musicSource.volume = musicVolume * masterVolume;
    }

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private void BuildAudioSources()
    {
        _ambientA = MakeSource("Ambient_A", loop: true);
        _ambientB = MakeSource("Ambient_B", loop: true);
        _musicSource = MakeSource("Music", loop: true);

        for (int i = 0; i < SFX_POOL_SIZE; i++)
            _sfxPool.Add(MakeSource($"SFX_{i}", loop: false));
    }

    private AudioSource MakeSource(string goName, bool loop)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.loop        = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D cho ambient/music, 1D để subclass override
        return src;
    }

    private AudioSource GetFreeSFXSource()
    {
        foreach (var s in _sfxPool)
            if (!s.isPlaying) return s;

        // Mọi source đều bận → dùng cái lâu nhất (lấy đầu tiên)
        return _sfxPool[0];
    }

    // =========================================================================
    // SFX Type Enum
    // =========================================================================
    public enum SFXType
    {
        Buzz,
        Nectar,
        Pollinate,
        HoneyDeliver,
        FlowerWilt,
        FlowerDead,
        EnemyAttack,
        QuestComplete,
        Win,
        Lose
    }
}
