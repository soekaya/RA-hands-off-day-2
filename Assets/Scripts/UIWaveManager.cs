using System.Collections;
using TMPro;
using UnityEngine;

public class UIWaveManager : MonoBehaviour
{
    [SerializeField] private GameObject wavePanel;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private float waveTextDuration = 2f;

    // ignore the same wave arriving twice (code event + Inspector event), so sound/text do not double
    private int lastStartedWave = -1;
    private int lastCompletedWave = -1;

    private void OnEnable()
    {
        WaveManager.WaveStartedEvent += OnWaveStarted;
        WaveManager.WaveCompletedEvent += OnWaveCompleted;
    }

    private void OnDisable()
    {
        WaveManager.WaveStartedEvent -= OnWaveStarted;
        WaveManager.WaveCompletedEvent -= OnWaveCompleted;
    }

    public void OnWaveStarted(int waveNumber)
    {
        if (waveNumber == lastStartedWave) return;
        lastStartedWave = waveNumber;
        GameSounds.WaveStart();

        string text = "Wave " + waveNumber;
        if (WaveManager.Instance != null && !WaveManager.Instance.IsEndless)
        {
            text += " / " + WaveManager.Instance.TotalWaves;
        }

        Show(text);
    }

    public void OnWaveCompleted(int waveNumber)
    {
        if (waveNumber == lastCompletedWave) return;
        lastCompletedWave = waveNumber;
        GameSounds.WaveComplete();

        Show("Wave " + waveNumber + " Completed!");
    }

    private void Show(string text)
    {
        Debug.Log("UIWaveManager: showing '" + text + "'");

        if (waveText == null)
        {
            Debug.LogWarning("UIWaveManager: 'Wave Text' is not assigned in the Inspector.");
            return;
        }

        if (wavePanel != null) wavePanel.SetActive(true);
        waveText.text = text;

        StopAllCoroutines();
        StartCoroutine(HideWaveTextAfterDelay());
    }

    private IEnumerator HideWaveTextAfterDelay()
    {
        yield return new WaitForSeconds(waveTextDuration);

        if (wavePanel != null) wavePanel.SetActive(false);
    }
}