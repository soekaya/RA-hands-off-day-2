using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Health health;
    private float horizontal;
    private float vertical;

    private Vector2 knockVelocity;
    private float knockTimer;
    private float knockDuration = 0.18f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // the player needs a Health component to take damage; add one if you forgot
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.onDamaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged -= OnDamaged;
        }
    }

    private void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        // while knocked back, the push fades out and input is ignored for a moment
        if (knockTimer > 0f)
        {
            knockTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockVelocity * Mathf.Clamp01(knockTimer / knockDuration);
            return;
        }

        Vector2 movement = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        rb.linearVelocity = movement * moveSpeed;
    }

    private void OnDamaged(int amount, Vector2 hitDir, float knockback)
    {
        if (knockback > 0f && hitDir.sqrMagnitude > 0.0001f)
        {
            knockVelocity = hitDir.normalized * knockback;
            knockTimer = knockDuration;
        }
    }
}