using UnityEngine;
using TMPro;

public class ChalkboardStats : MonoBehaviour
{
    private TextMeshPro text;
    private int lastKills = -1;
    private int lastCoins = -1;
    private int lastWave = -1;
    private bool lastFinished;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (text == null || LevelManager.instance == null) return;

        int kills = LevelManager.instance.killCount;
        int coins = LevelManager.instance.coinCount;

        int wave = -1;
        bool finished = false;
        if (WaveManager.Instance != null)
        {
            wave = WaveManager.Instance.CurrentWave;
            finished = WaveManager.Instance.IsFinished;
        }

        if (kills != lastKills || coins != lastCoins || wave != lastWave || finished != lastFinished)
        {
            lastKills = kills;
            lastCoins = coins;
            lastWave = wave;
            lastFinished = finished;

            text.text = BuildTitle(wave, finished) + "\nKills: " + kills + "    Coins: " + coins;
        }
    }

    private string BuildTitle(int wave, bool finished)
    {
        if (WaveManager.Instance == null) return "SURVIVE THE CLASS";
        if (finished) return "CLASS DISMISSED!";
        if (wave <= 0) return "GET READY";

        return "WAVE " + wave + "/" + WaveManager.Instance.TotalWaves;
    }
}