using UnityEngine;


public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)][SerializeField] private float volume = 1f;
    [SerializeField] private float fadeInSeconds = 1f;
    [SerializeField] private float fadeOutPreviousSeconds = 0.5f;

    private void Start()
    {
        if (music == null)
        {
            Debug.LogWarning("MusicPlayer: drag a music clip into 'Music'.");
            return;
        }

        // lets you press Play from any scene, even if the AudioManager is not there yet
        if (AudioManager.Instance == null)
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        AudioManager.Instance.Music(music)
            .Volume(volume)
            .Loop()
            .FadeIn(fadeInSeconds)
            .FadeOutPrevious(fadeOutPreviousSeconds)
            .Play();
    }
}
