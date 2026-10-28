using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 10;

    private AudioSource musicSource;
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private Coroutine musicRoutine;
    private readonly List<Coroutine> pendingSfx = new List<Coroutine>();
    private float musicScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;

        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxSources.Add(src);
        }
    }

    public SfxBuilder Sfx(AudioClip clip) => new SfxBuilder(this, clip);
    public MusicBuilder Music(AudioClip clip) => new MusicBuilder(this, clip);

    public void PlaySFX(AudioClip clip) => Sfx(clip).Play();
    public void PlayMusic(AudioClip clip) => Music(clip).Play();


    public void StopMusic(float fadeOut = 0f)
    {
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(StopMusicRoutine(fadeOut));
    }

    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    public void StopAllSFX()
    {
        foreach (Coroutine c in pendingSfx)
            if (c != null) StopCoroutine(c);
        pendingSfx.Clear();

        foreach (AudioSource src in sfxSources) src.Stop();
    }

    public void StopSFX(AudioClip clip)
    {
        foreach (AudioSource src in sfxSources)
        {
            if (src.isPlaying && src.clip == clip) src.Stop();
        }
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume * musicScale;
    }

    public void SetSFXVolume(float value) => sfxVolume = Mathf.Clamp01(value);

    internal void ExecuteSfx(SfxBuilder b)
    {
        if (b.Clip == null) return;

        if (b.DelaySeconds > 0f) pendingSfx.Add(StartCoroutine(DelayedSfx(b)));
        else PlaySfxNow(b);
    }

    internal void ExecuteMusic(MusicBuilder b)
    {
        if (b.Clip == null) return;

        if (musicSource.clip == b.Clip && musicSource.isPlaying) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(MusicRoutine(b));
    }


    private void PlaySfxNow(SfxBuilder b)
    {
        AudioSource src = GetFreeSfxSource();
        src.clip = b.Clip;
        src.volume = sfxVolume * b.VolumeScale;
        src.pitch = b.UseRandomPitch ? Random.Range(b.MinPitch, b.MaxPitch) : b.PitchValue;
        src.loop = b.IsLoop;
        src.Play();
    }

    private IEnumerator DelayedSfx(SfxBuilder b)
    {
        yield return new WaitForSeconds(b.DelaySeconds);
        PlaySfxNow(b);
    }

    private IEnumerator MusicRoutine(MusicBuilder b)
    {
        if (musicSource.isPlaying && b.FadeOutPreviousSeconds > 0f)
            yield return FadeMusicTo(0f, b.FadeOutPreviousSeconds);

        musicScale = b.VolumeScale;
        float targetVolume = musicVolume * musicScale;

        musicSource.Stop();
        musicSource.clip = b.Clip;
        musicSource.loop = b.IsLoop;
        musicSource.volume = b.FadeInSeconds > 0f ? 0f : targetVolume;
        musicSource.Play();

        if (b.FadeInSeconds > 0f)
            yield return FadeMusicTo(targetVolume, b.FadeInSeconds);

        musicRoutine = null;
    }

    private IEnumerator StopMusicRoutine(float fadeOut)
    {
        if (fadeOut > 0f && musicSource.isPlaying)
            yield return FadeMusicTo(0f, fadeOut);

        musicSource.Stop();
        musicSource.clip = null;
        musicRoutine = null;
    }

    private IEnumerator FadeMusicTo(float target, float duration)
    {
        float start = musicSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        musicSource.volume = target;
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (AudioSource src in sfxSources)
        {
            if (!src.isPlaying) return src;
        }
        return sfxSources[0];
    }
}

public class SfxBuilder
{
    private readonly AudioManager manager;

    public AudioClip Clip;
    public float VolumeScale = 1f;
    public float PitchValue = 1f;
    public bool UseRandomPitch;
    public float MinPitch = 0.9f;
    public float MaxPitch = 1.1f;
    public float DelaySeconds;
    public bool IsLoop;

    internal SfxBuilder(AudioManager manager, AudioClip clip)
    {
        this.manager = manager;
        Clip = clip;
    }

    public SfxBuilder Volume(float value) { VolumeScale = Mathf.Clamp01(value); return this; }
    public SfxBuilder Pitch(float value) { PitchValue = value; UseRandomPitch = false; return this; }

    public SfxBuilder RandomPitch(float min = 0.9f, float max = 1.1f)
    {
        MinPitch = min;
        MaxPitch = max;
        UseRandomPitch = true;
        return this;
    }

    public SfxBuilder Delay(float seconds) { DelaySeconds = Mathf.Max(0f, seconds); return this; }
    public SfxBuilder Loop(bool value = true) { IsLoop = value; return this; }

    public void Play() => manager.ExecuteSfx(this);
}

public class MusicBuilder
{
    private readonly AudioManager manager;

    public AudioClip Clip;
    public float VolumeScale = 1f;
    public bool IsLoop = true;
    public float FadeInSeconds;
    public float FadeOutPreviousSeconds;

    internal MusicBuilder(AudioManager manager, AudioClip clip)
    {
        this.manager = manager;
        Clip = clip;
    }

    public MusicBuilder Volume(float value) { VolumeScale = Mathf.Clamp01(value); return this; }
    public MusicBuilder Loop(bool value = true) { IsLoop = value; return this; }
    public MusicBuilder FadeIn(float seconds) { FadeInSeconds = Mathf.Max(0f, seconds); return this; }
    public MusicBuilder FadeOutPrevious(float seconds) { FadeOutPreviousSeconds = Mathf.Max(0f, seconds); return this; }

    public void Play() => manager.ExecuteMusic(this);
}