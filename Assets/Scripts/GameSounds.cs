using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSounds
{
    private const string Folder = "Sound/";
    public static bool DebugLog = true;   // set to false when everything works

    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, AudioSource> sources = new Dictionary<string, AudioSource>();
    private static readonly HashSet<string> warned = new HashSet<string>();
    private static GameSoundsRunner runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        cache.Clear();
        sources.Clear();
        runner = null;
        EnsureRunner();

        string[] all = { "sfx_click", "sfx_wave_start", "sfx_wave_complete", "sfx_game_over" };
        for (int i = 0; i < all.Length; i++)
        {
            AudioClip clip = Load(all[i]);
            if (clip != null) clip.LoadAudioData();
        }
    }

    public static void Click() { Play("sfx_click", 0.8f, 0f); }
    public static void WaveStart() { Play("sfx_wave_start", 0.9f, 0f); }
    public static void WaveComplete() { Play("sfx_wave_complete", 1f, 0f); }
    public static void GameOver() { Play("sfx_game_over", 1f, 0.3f); }

    private static void Play(string name, float volume, float delay)
    {
        AudioClip clip = Load(name);
        if (clip == null) return;

        EnsureRunner();

        if (DebugLog)
        {
            Debug.Log("[GameSounds] " + name + " clip=" + clip.name + " len=" + clip.length.ToString("F2")
                      + " vol=" + volume + " delay=" + delay
                      + " | listenerPause=" + AudioListener.pause
                      + " listenerVolume=" + AudioListener.volume
                      + " timeScale=" + Time.timeScale, clip);
        }

        if (delay > 0f) runner.StartCoroutine(PlayDelayed(name, clip, volume, delay));
        else PlayNow(name, clip, volume);
    }

    private static IEnumerator PlayDelayed(string name, AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayNow(name, clip, volume);
    }

    private static void PlayNow(string name, AudioClip clip, float volume)
    {
        AudioSource src = GetSource(name);
        if (src == null) return;

        src.Stop();
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.loop = false;
        src.mute = false;
        src.Play();

        if (DebugLog) Debug.Log("[GameSounds] " + name + " isPlaying=" + src.isPlaying);
    }

    private static AudioSource GetSource(string name)
    {
        AudioSource src;
        if (sources.TryGetValue(name, out src) && src != null) return src;

        EnsureRunner();

        src = runner.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;            // 2D
        src.priority = 0;                 // highest priority, never dropped
        src.ignoreListenerPause = true;   // plays even while paused
        sources[name] = src;
        return src;
    }

    private static void EnsureRunner()
    {
        if (runner != null) return;

        GameObject go = new GameObject("GameSoundsRunner");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<GameSoundsRunner>();
        sources.Clear();   // old sources died with the old object
    }

    private static AudioClip Load(string name)
    {
        AudioClip clip;
        if (cache.TryGetValue(name, out clip) && clip != null) return clip;

        clip = Resources.Load<AudioClip>(Folder + name);
        if (clip == null)
        {
            if (warned.Add(name))
            {
                Debug.LogWarning("GameSounds: put '" + name + "' in Assets/Resources/" + Folder + " to hear this sound.");
            }
            return null;
        }

        cache[name] = clip;
        return clip;
    }

    public static void TestKeys()
    {
        if (Input.GetKeyDown(KeyCode.F9)) WaveComplete();
        if (Input.GetKeyDown(KeyCode.F10)) WaveStart();
        if (Input.GetKeyDown(KeyCode.F11)) Click();
    }
}

// must be a top-level class so AddComponent works
public class GameSoundsRunner : MonoBehaviour
{
    private void Update()
    {
        GameSounds.TestKeys();
    }
}