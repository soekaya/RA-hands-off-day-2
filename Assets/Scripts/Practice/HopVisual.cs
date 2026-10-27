using UnityEngine;

public class HopVisual : MonoBehaviour
{
    public float hopSpeed = 12f;
    public float hopHeight = 0.1f;
    public float squashAmount = 0.15f;
    public float tiltAngle = 8f;
    public float maxSpeed = 5f;
    public float smooth = 12f;

    [Header("Facing")]
    public bool autoFace = true;         // EnemyMovement turns this off and calls Face() itself
    public float faceDeadZone = 0.4f;    // ignore tiny movements so it doesn't flip back and forth
    public float flipCooldown = 0.2f;

    // set by EnemyMovement
    [HideInInspector] public float charge;          // 0..1 crouch while charging an attack
    [HideInInspector] public bool dashing;          // stretched pose while dashing
    [HideInInspector] public Vector2 dashDirection = Vector2.right;

    private Rigidbody2D rb;
    private Transform visual;
    private Vector3 basePos;
    private Vector3 baseScale;
    private Vector3 lastPos;
    private Vector2 smoothVel;
    private float hopTimer;
    private float facing = 1f;
    private float nextFlipTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        visual = transform.Find("Sprite");
        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }

        if (visual == null)
        {
            Debug.LogWarning("HopVisual: no child named 'Sprite' found on " + name);
            enabled = false;
        }
    }

    private void Start()
    {
        basePos = visual.localPosition;
        baseScale = visual.localScale;
        lastPos = transform.position;
    }

    public void Face(float x)
    {
        if (Mathf.Abs(x) < faceDeadZone) return;

        float dir = x > 0f ? 1f : -1f;
        if (dir == facing || Time.time < nextFlipTime) return;

        facing = dir;
        nextFlipTime = Time.time + flipCooldown;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // use the rigidbody velocity when there is one (position deltas jitter between physics steps)
        Vector2 vel;
        if (rb != null)
        {
            vel = rb.linearVelocity;
        }
        else
        {
            vel = (Vector2)(transform.position - lastPos) / dt;
        }
        lastPos = transform.position;

        smoothVel = Vector2.Lerp(smoothVel, vel, 1f - Mathf.Exp(-15f * dt));
        bool moving = smoothVel.magnitude > 0.3f;

        if (autoFace && Mathf.Abs(smoothVel.x) > 0.3f)
        {
            Face(smoothVel.x);
        }

        Vector3 targetPos = basePos;
        Vector3 targetScale = new Vector3(baseScale.x * facing, baseScale.y, baseScale.z);
        float targetTilt = 0f;

        if (dashing)
        {
            // stretch along the dash direction
            if (Mathf.Abs(dashDirection.y) > Mathf.Abs(dashDirection.x))
            {
                targetScale = new Vector3(baseScale.x * facing * 0.8f, baseScale.y * 1.25f, baseScale.z);
            }
            else
            {
                targetScale = new Vector3(baseScale.x * facing * 1.3f, baseScale.y * 0.78f, baseScale.z);
                targetTilt = -facing * 12f;
            }
        }
        else if (charge > 0.01f)
        {
            // crouch down while charging
            targetScale = new Vector3(
                baseScale.x * facing * (1f + 0.15f * charge),
                baseScale.y * (1f - 0.25f * charge),
                baseScale.z);
            targetPos += Vector3.down * 0.1f * charge;
        }
        else if (moving)
        {
            float speedFactor = Mathf.Clamp01(smoothVel.magnitude / maxSpeed);

            hopTimer += dt * hopSpeed;
            float hop = Mathf.Abs(Mathf.Sin(hopTimer));

            targetPos += Vector3.up * hop * hopHeight * Mathf.Max(0.4f, speedFactor);

            float stretch = (hop - 0.5f) * 2f * squashAmount;
            targetScale = new Vector3(
                baseScale.x * facing * (1f - stretch * 0.5f),
                baseScale.y * (1f + stretch),
                baseScale.z);

            targetTilt = Mathf.Clamp(-smoothVel.x / maxSpeed, -1f, 1f) * tiltAngle;
        }
        else
        {
            hopTimer = 0f;
            float breathe = Mathf.Sin(Time.time * 2f) * 0.03f;
            targetScale = new Vector3(baseScale.x * facing, baseScale.y * (1f + breathe), baseScale.z);
        }

        float t = 1f - Mathf.Exp(-smooth * dt);
        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, t);
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, t);
        visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0f, 0f, targetTilt), t);
    }
}