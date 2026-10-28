using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    public KeyCode dashKey = KeyCode.Space;
    public float dashSpeed = 14f;
    public float dashTime = 0.18f;
    public float cooldown = 0.8f;

    public GameObject smokePrefab;
    public Vector2 feetOffset = new Vector2(0f, -0.9f);

    public bool IsDashing { get; private set; }

    private Rigidbody2D rb;
    private Health health;
    private SpriteRenderer[] bodyParts;
    private Vector2 lastDirection = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndTime;
    private float nextDashTime;
    private float nextEffectTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        Transform body = transform.Find("Sprite");
        if (body != null)
        {
            bodyParts = body.GetComponentsInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 0.01f)
        {
            lastDirection = input.normalized;
        }

        if (Input.GetKeyDown(dashKey) && CanDash())
        {
            StartDash();
        }

        if (!IsDashing) return;

        if (Time.time >= dashEndTime)
        {
            EndDash();
        }
        else if (Time.time >= nextEffectTime)
        {
            nextEffectTime = Time.time + 0.04f;
            SpawnGhost();
            SpawnSmoke();
        }
    }

    private void FixedUpdate()
    {
        if (IsDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }
    }

    private bool CanDash()
    {
        if (IsDashing || Time.time < nextDashTime) return false;
        if (health != null && health.IsDead) return false;
        return true;
    }

    private void StartDash()
    {
        IsDashing = true;
        dashDirection = lastDirection;
        dashEndTime = Time.time + dashTime;
        nextDashTime = Time.time + cooldown;
        nextEffectTime = 0f;

        // enemies cannot hurt the player while dashing
        if (health != null)
        {
            health.SetInvincible(dashTime + 0.1f);
        }

        CameraShake.Shake(0.05f, 0.1f);

        for (int i = 0; i < 3; i++)
        {
            SpawnSmoke();
        }
    }

    private void EndDash()
    {
        IsDashing = false;
        SpawnSmoke();
    }

    private void SpawnSmoke()
    {
        Vector2 offset = feetOffset + Random.insideUnitCircle * 0.15f;
        Vector3 position = transform.position + new Vector3(offset.x, offset.y, 0f);

        if (smokePrefab != null)
        {
            Instantiate(smokePrefab, position, Quaternion.identity);
        }
        else
        {
            GameFX.Burst(position, new Color(0.9f, 0.9f, 0.95f, 1f), 4, 1.5f, 0.2f, 0.4f, Vector2.zero);
        }
    }

    // the "blur": a see-through copy of the body is left behind and fades away
    private void SpawnGhost()
    {
        if (bodyParts == null) return;

        for (int i = 0; i < bodyParts.Length; i++)
        {
            SpriteRenderer part = bodyParts[i];
            if (part == null || !part.enabled) continue;

            GameObject ghost = new GameObject("DashGhost");
            ghost.transform.position = part.transform.position;
            ghost.transform.rotation = part.transform.rotation;
            ghost.transform.localScale = part.transform.lossyScale;

            SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = part.sprite;
            sr.flipX = part.flipX;
            sr.flipY = part.flipY;
            sr.color = new Color(0.55f, 0.85f, 1f, 0.55f);
            sr.sortingOrder = part.sortingOrder - 1;

            ghost.AddComponent<FXParticle>().Init(Vector2.zero, 0.3f, 0f);
        }
    }
}
