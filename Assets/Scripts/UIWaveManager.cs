using System.Collections;
using TMPro;
using UnityEngine;

// Same as the original: only "Wave x / total" and null checks were added.
public class UIWaveManager : MonoBehaviour
{
    [SerializeField] private GameObject wavePanel;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private float waveTextDuration = 2f;

    public void OnWaveStarted(int waveNumber)
    {
        string text = "Wave " + waveNumber;
        if (WaveManager.Instance != null)
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
        if (waveText == null) return;

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