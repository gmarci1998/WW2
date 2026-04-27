using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip paperSound;
    [SerializeField] private AudioClip paperClosedSound;

    private bool canPlayHover = true;

    void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource != null)
        {
            musicSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    public void FadeOutMusic()
    {
        StartCoroutine(FadeOut());
    }

    public void FadeInMusic()
    {
        StartCoroutine(FadeIn());
    }

    public void FadeMusic(float targetVolume)
    {
        StartCoroutine(Fade(targetVolume, 6));
    }

    IEnumerator Fade(float targetVolume, float duration)
    {
        if (musicSource == null)
        {
            yield break;
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    IEnumerator FadeOut()
    {
        if (musicSource == null)
        {
            yield break;
        }

        float startVolume = musicSource.volume;
        float targetVolume = 0.025f;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    IEnumerator FadeIn()
    {
        if (musicSource == null)
        {
            yield break;
        }

        float startVolume = musicSource.volume;
        float targetVolume = 1f;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void OnPointerEnter()
    {
        if (canPlayHover && sfxSource != null && hoverSound != null)
        {
            sfxSource.PlayOneShot(hoverSound);
            canPlayHover = false;
        }
    }

    public void OnPointerExit()
    {
        canPlayHover = true;
    }

    public void PlayPaperSound()
    {
        if (sfxSource != null && paperSound != null)
        {
            sfxSource.PlayOneShot(paperSound);
        }
    }
    public void PlayPaperClosedSound()
    {
        if (sfxSource != null && paperClosedSound != null)
        {
            sfxSource.PlayOneShot(paperClosedSound);
        }
    }
}
