using UnityEngine;

public class AnimalSound : MonoBehaviour
{

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip audioClip;


    private void OnEnable()
    {
        PlaySFX();
    }


    private void PlaySFX()
    {
        audioSource.PlayOneShot(audioClip);
    }
}
