using System.Collections;
using TMPro;
using UnityEngine;

public class UIWaveManager : MonoBehaviour
{
    [SerializeField] private GameObject wavePanel;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private float waveTextDuration = 2f;

    public void OnWaveStarted(int waveNumber)
    {
        if (waveText != null)
        {
            wavePanel.SetActive(true);
            waveText.text = $"Wave {waveNumber}";
            StopAllCoroutines();
            StartCoroutine(HideWaveTextAfterDelay());
        }
    }

    public void OnWaveCompleted(int waveNumber)
    {
        if (waveText != null)
        {
            wavePanel.SetActive(true);
            waveText.text = $"Wave {waveNumber} Completed!";
            StopAllCoroutines();
            StartCoroutine(HideWaveTextAfterDelay());
        }
    }

    private IEnumerator HideWaveTextAfterDelay()
    {
        yield return new WaitForSeconds(waveTextDuration);
        wavePanel.SetActive(false);
    }
}
