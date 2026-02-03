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

    void Start()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
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
        StartCoroutine(Fade(targetVolume, 4));
    }

    IEnumerator Fade(float targetVolume, float duration)
    {
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
        if (canPlayHover)
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
        if (true)
        {
            sfxSource.PlayOneShot(paperSound);
        }
    }
    public void PlayPaperClosedSound()
    {
        if (true)
        {
            sfxSource.PlayOneShot(paperClosedSound);
        }
    }
}