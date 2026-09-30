using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource ambfgAudioSource;
    [SerializeField] private AudioSource ambbgAudioSource;
    [Header("Audio Clips")]
    [SerializeField] private AudioClip handPumping;
    [SerializeField] private AudioClip timeOver;

    [Header("Gameplay SFX")]
    [Tooltip("Phát khi đặt/xây công trình hoặc trồng cây thành công.")]
    [SerializeField] private AudioClip buildingClip;
    [Tooltip("Phát khi thu hoạch / nhặt vật phẩm.")]
    [SerializeField] private AudioClip collectClip;
    [Tooltip("Phát khi đất nứt / sụt lún kích hoạt level mới.")]
    [SerializeField] private AudioClip crackingGroundClip;
    [Tooltip("Phát khi cây đổ do sụt lún.")]
    [SerializeField] private AudioClip treeFallClip;
    [Tooltip("Phát khi hiển thị message / popup UI.")]
    [SerializeField] private AudioClip messageClip;
    [Tooltip("Phát khi mực nước dâng cao (lụt).")]
    [SerializeField] private AudioClip riverWaterClip;
    [Tooltip("Phát footsteps (loop hoặc oneshot).")]
    [SerializeField] private AudioClip footstepsClip;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
    }

    public void PlaySoundEffect(AudioClip clip)
    {
        sfxAudioSource.PlayOneShot(clip);
    }

    public void PlayFGAmbience(AudioClip clip)
    {
        ambfgAudioSource.PlayOneShot(clip);
    }

    public void PlayBGAmbience(AudioClip clip)
    {
        ambbgAudioSource.clip = clip;
        ambbgAudioSource.Play();
    }

    public void PlayPumpSound()
    {
        sfxAudioSource.PlayOneShot(handPumping);
    }

    public void PlayTimeOverSound()
    {
        sfxAudioSource.PlayOneShot(timeOver);
    }

    // ====== Gameplay SFX helpers ======
    // Tất cả đều null-guard để không crash nếu clip hoặc source chưa gán trong Inspector.

    /// <summary>SFX khi xây / trồng cây thành công.</summary>
    public void PlayBuilding(Vector3? worldPos = null) => PlaySafeOneShot(buildingClip, worldPos);

    /// <summary>SFX khi thu hoạch hoặc nhặt vật phẩm.</summary>
    public void PlayCollect(Vector3? worldPos = null) => PlaySafeOneShot(collectClip, worldPos);

    /// <summary>SFX khi đất nứt / sụt lún kích hoạt level mới.</summary>
    public void PlayCrackingGround(Vector3? worldPos = null) => PlaySafeOneShot(crackingGroundClip, worldPos);

    /// <summary>SFX khi cây đổ do sụt lún / lũ.</summary>
    public void PlayTreeFalling(Vector3? worldPos = null) => PlaySafeOneShot(treeFallClip, worldPos);

    /// <summary>SFX khi hiện UI message / popup.</summary>
    public void PlayMessage() => PlaySafeOneShot(messageClip, null);

    /// <summary>SFX khi mực nước dâng cao (lũ).</summary>
    public void PlayRiverWater(Vector3? worldPos = null) => PlaySafeOneShot(riverWaterClip, worldPos);

    /// <summary>SFX bước chân.</summary>
    public void PlayFootstep(Vector3? worldPos = null) => PlaySafeOneShot(footstepsClip, worldPos);

    /// <summary>API tổng quát: phát một clip do caller cung cấp (vd: harvest sound riêng từng cây).</summary>
    public void PlayClip(AudioClip clip, Vector3? worldPos = null) => PlaySafeOneShot(clip, worldPos);

    private void PlaySafeOneShot(AudioClip clip, Vector3? worldPos)
    {
        if (clip == null) return;
        // Spatial 3D: nếu cung cấp vị trí, dùng PlayClipAtPoint để âm thanh có không gian.
        if (worldPos.HasValue)
        {
            AudioSource.PlayClipAtPoint(clip, worldPos.Value);
            return;
        }
        if (sfxAudioSource != null) sfxAudioSource.PlayOneShot(clip);
    }
}
