using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    private AudioSource audioSource;
    private Rigidbody2D rb;
    private PlayerDash dash;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<PlayerDash>();
    }

    private void Update()
    {
        bool isDashing = dash != null && dash.IsDashing;
        bool isMoving = rb.linearVelocity.magnitude > 0.1f && !isDashing;

        if (isMoving && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (!isMoving && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}