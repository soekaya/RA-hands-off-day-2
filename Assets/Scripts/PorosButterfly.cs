using UnityEngine;

// POROS butterfly heal item.
// - It flutters around the room on its own.
// - When the player needs healing, it tries to keep away a little (but it is slower than the player).
// - Get close enough to catch it and you heal.
// - If nobody catches it, it fades away after 'lifeTime' seconds.
// No collider needed: the catch is just a distance check.
public class PorosButterfly : MonoBehaviour
{
    [Header("Heal")]
    public int healAmount = 1;
    public float catchRadius = 0.9f;
    public float lifeTime = 25f;

    [Header("Moving")]
    public float wanderSpeed = 1.2f;
    public float wanderRadius = 3f;
    public bool fleeFromPlayer = true;
    public float fleeRadius = 2.5f;
    public float fleeSpeed = 3.4f;       // slower than the player (5), so it can always be caught

    [Header("Look")]
    public float flapSpeed = 14f;
    public float bobHeight = 0.08f;
    public float glowSpeed = 3f;

    [Header("Room Limits (same as the classroom floor)")]
    public Vector2 roomCenter = Vector2.zero;
    public Vector2 roomSize = new Vector2(24f, 12f);
    public float wallMargin = 1.2f;

    private static readonly Color[] SparkleColors =
    {
        new Color(0.45f, 0.95f, 0.45f, 1f),
        new Color(0.75f, 1f, 0.60f, 1f),
        new Color(0.15f, 0.15f, 0.18f, 1f)
    };

    private Transform player;
    private Health playerHealth;
    private Transform visual;
    private Transform wingL;
    private Transform wingR;
    private SpriteRenderer[] glows;
    private float[] glowAlphas;
    private Vector3 visualBasePos;

    private Vector2 target;
    private float nextTargetTime;
    private float age;
    private float phase;
    private float tilt;
    private bool finished;

    private void Awake()
    {
        phase = Random.value * 10f;

        visual = transform.Find("Sprite");
        wingL = transform.Find("Sprite/WingL");
        wingR = transform.Find("Sprite/WingR");

        if (visual != null)
        {
            visualBasePos = visual.localPosition;
        }

        string[] glowNames = { "Glow1", "Glow2" };
        glows = new SpriteRenderer[glowNames.Length];
        glowAlphas = new float[glowNames.Length];

        for (int i = 0; i < glowNames.Length; i++)
        {
            Transform t = transform.Find("Sprite/" + glowNames[i]);
            if (t == null) continue;

            glows[i] = t.GetComponent<SpriteRenderer>();
            if (glows[i] != null)
            {
                glowAlphas[i] = glows[i].color.a;
            }
        }
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }
        else
        {
            Debug.LogWarning("PorosButterfly: no object with the tag 'Player' found, it cannot heal anyone.");
        }

        PickTarget();
    }

    private void Update()
    {
        if (finished) return;

        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            Vanish();
            return;
        }

        // shrink during the last 1.5 seconds so the player sees it is about to leave
        float remaining = lifeTime - age;
        transform.localScale = Vector3.one * Mathf.Clamp01(remaining / 1.5f);

        Vector2 move = Move();
        Animate(move);
        TryCatch();
    }

    // ------------------------------------------------------------------ moving

    private Vector2 Move()
    {
        Vector2 pos = transform.position;
        Vector2 move;

        bool fleeing = false;
        move = Vector2.zero;

        if (fleeFromPlayer && player != null && PlayerNeedsHeal())
        {
            Vector2 away = pos - (Vector2)player.position;
            float dist = away.magnitude;

            if (dist < fleeRadius && dist > 0.01f)
            {
                fleeing = true;

                Vector2 awayDir = away / dist;
                Vector2 sideways = new Vector2(-awayDir.y, awayDir.x);

                // a little sideways flutter so it can slide along walls instead of getting stuck
                move = awayDir * fleeSpeed + sideways * (Mathf.Sin(Time.time * 5f + phase) * 1.2f);
            }
        }

        if (!fleeing)
        {
            if (Vector2.Distance(pos, target) < 0.2f || Time.time >= nextTargetTime)
            {
                PickTarget();
            }

            Vector2 toTarget = target - pos;
            move = toTarget.normalized * wanderSpeed;
        }

        pos += move * Time.deltaTime;
        pos = ClampToRoom(pos);
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        return move;
    }

    private void PickTarget()
    {
        Vector2 pos = transform.position;
        target = ClampToRoom(pos + Random.insideUnitCircle * wanderRadius);
        nextTargetTime = Time.time + Random.Range(1.5f, 3f);
    }

    private Vector2 ClampToRoom(Vector2 p)
    {
        p.x = Mathf.Clamp(p.x, roomCenter.x - roomSize.x / 2f + wallMargin, roomCenter.x + roomSize.x / 2f - wallMargin);
        p.y = Mathf.Clamp(p.y, roomCenter.y - roomSize.y / 2f + wallMargin, roomCenter.y + roomSize.y / 2f - wallMargin);
        return p;
    }

    // ------------------------------------------------------------------ look

    private void Animate(Vector2 move)
    {
        // wings flap by getting narrower and wider
        float wingScale = Mathf.Lerp(0.2f, 1f, Mathf.Abs(Mathf.Sin((Time.time + phase) * flapSpeed)));
        if (wingL != null) wingL.localScale = new Vector3(wingScale, 1f, 1f);
        if (wingR != null) wingR.localScale = new Vector3(wingScale, 1f, 1f);

        if (visual != null)
        {
            // floating up and down
            visual.localPosition = visualBasePos + Vector3.up * (Mathf.Sin(Time.time * 6f + phase) * bobHeight);

            // lean a little in the direction it flies
            float targetTilt = Mathf.Clamp(-move.x * 4f, -20f, 20f);
            tilt = Mathf.Lerp(tilt, targetTilt, Time.deltaTime * 8f);
            visual.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        // soft glow pulse
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * glowSpeed + phase);
        for (int i = 0; i < glows.Length; i++)
        {
            if (glows[i] == null) continue;

            Color c = glows[i].color;
            c.a = glowAlphas[i] * pulse;
            glows[i].color = c;
        }
    }

    // ------------------------------------------------------------------ catching

    private bool PlayerNeedsHeal()
    {
        return playerHealth != null
            && !playerHealth.IsDead
            && playerHealth.CurrentHealth < playerHealth.MaxHealth;
    }

    private void TryCatch()
    {
        if (player == null || !PlayerNeedsHeal()) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > catchRadius) return;

        finished = true;
        playerHealth.Heal(healAmount);   // Health shows the green "+1" and the flash

        GameFX.Burst(transform.position, SparkleColors, 14, 5f, 0.16f, 0.6f, Vector2.zero);
        Destroy(gameObject);
    }

    private void Vanish()
    {
        finished = true;
        GameFX.Burst(transform.position, SparkleColors, 6, 3f, 0.12f, 0.4f, Vector2.zero);
        Destroy(gameObject);
    }
}
