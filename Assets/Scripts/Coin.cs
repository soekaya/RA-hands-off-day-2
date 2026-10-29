
using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coinValue = 1;

    public AudioClip coinSFX;
    public float coinVolume = 1f;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.AddCoin(coinValue);

                if (coinSFX != null)
                {
                    AudioSource playerAudio = collision.GetComponent<AudioSource>();
                    if (playerAudio != null)
                    {
                        playerAudio.PlayOneShot(coinSFX, coinVolume);
                    }
                
                 }
                Destroy(gameObject);
            }
        }
    }
}
