using System.Collections;
using TMPro;
using UnityEngine;

public class UIWaveManager : MonoBehaviour
{
    [SerializeField] private GameObject wavePanel;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private float waveTextDuration = 2f;

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
        string text = "Wave " + waveNumber;
        if (WaveManager.Instance != null && !WaveManager.Instance.IsEndless)
        {
            text += " / " + WaveManager.Instance.TotalWaves;
        }

        Show(text);
    }

    public void OnWaveCompleted(int waveNumber)
    {
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