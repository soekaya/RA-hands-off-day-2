using UnityEngine;
using TMPro;

// Put this on the "ChalkStats" object (inside Classroom > WallDecor).
// It shows the kills and coins from LevelManager on the blackboard.
public class ChalkboardStats : MonoBehaviour
{
    private TextMeshPro text;
    private int lastKills = -1;
    private int lastCoins = -1;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (text == null || LevelManager.instance == null) return;

        int kills = LevelManager.instance.killCount;
        int coins = LevelManager.instance.coinCount;

        if (kills != lastKills || coins != lastCoins)
        {
            lastKills = kills;
            lastCoins = coins;
            text.text = "FILKOM UB - SURVIVE THE CLASS\nKills: " + kills + "    Coins: " + coins;
        }
    }
}
