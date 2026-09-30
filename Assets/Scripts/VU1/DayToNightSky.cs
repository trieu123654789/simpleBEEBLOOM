using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayToNightSky : MonoBehaviour
{
    [Header("Skybox")]
    public Light directionalLight;
    public Material skyboxMaterial; // Procedural Skybox material

    [Header("Mùa mưa (Rainy) — 2 phút đầu")]
    public Color rainyTint = new Color(0.35f, 0.4f, 0.5f, 1f);   // xám xanh, u ám
    public float rainyLightIntensity = 0.6f;
    public float rainyExposure = 0.8f;

    [Header("Mùa khô (Dry) — sau 2 phút")]
    public Color dryTint = new Color(0.5f, 0.75f, 1f, 1f);       // xanh da trời, trong sáng
    public float dryLightIntensity = 1.8f;
    public float dryExposure = 1.3f;

    [Header("Thời gian")]
    [Tooltip("Thời gian mùa mưa (giây). Mặc định 120 = 2 phút.")]
    public float rainyDuration = 120f;
    [Tooltip("Thời gian chuyển đổi mượt giữa 2 mùa (giây).")]
    public float transitionDuration = 10f;

    [Header("Âm thanh")]
    public AudioSource audioSound;
    public AudioClip rainyClip;   // Nhạc / tiếng mưa cho mùa mưa
    public AudioClip dryClip;     // Nhạc cho mùa khô

    private float elapsed = 0f;
    private bool switchedToDry = false;

    void Start()
    {
        RenderSettings.skybox = skyboxMaterial;
        if (audioSound == null)
            audioSound = GetComponent<AudioSource>();

        // Bắt đầu với bầu trời mùa mưa
        ApplySky(rainyTint, rainyLightIntensity, rainyExposure);

        if (rainyClip != null)
            ChangeAudioClip(rainyClip);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // Giai đoạn 1: Mùa mưa (0 → rainyDuration)
        if (elapsed <= rainyDuration)
        {
            // Giữ nguyên bầu trời mùa mưa, không cần làm gì thêm
            return;
        }

        // Giai đoạn chuyển tiếp: rainyDuration → rainyDuration + transitionDuration
        float transitionStart = rainyDuration;
        float transitionEnd = rainyDuration + transitionDuration;

        if (elapsed < transitionEnd)
        {
            float t = Mathf.Clamp01((elapsed - transitionStart) / transitionDuration);

            Color currentTint = Color.Lerp(rainyTint, dryTint, t);
            float currentIntensity = Mathf.Lerp(rainyLightIntensity, dryLightIntensity, t);
            float currentExposure = Mathf.Lerp(rainyExposure, dryExposure, t);

            ApplySky(currentTint, currentIntensity, currentExposure);

            // Chuyển nhạc khi bắt đầu chuyển mùa
            if (!switchedToDry)
            {
                switchedToDry = true;
                if (dryClip != null)
                    ChangeAudioClip(dryClip);
            }
            return;
        }

        // Giai đoạn 2: Mùa khô (sau chuyển tiếp)
        if (!switchedToDry)
        {
            switchedToDry = true;
            if (dryClip != null)
                ChangeAudioClip(dryClip);
        }
        ApplySky(dryTint, dryLightIntensity, dryExposure);
    }

    void ApplySky(Color tint, float lightIntensity, float exposure)
    {
        skyboxMaterial.SetColor("_Tint", tint);

        if (skyboxMaterial.HasProperty("_Exposure"))
            skyboxMaterial.SetFloat("_Exposure", exposure);

        if (directionalLight != null)
            directionalLight.intensity = lightIntensity;

        DynamicGI.UpdateEnvironment();
    }

    void ChangeAudioClip(AudioClip clip)
    {
        if (audioSound == null || clip == null) return;
        audioSound.Stop();
        audioSound.clip = clip;
        audioSound.Play();
    }
}
