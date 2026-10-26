using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Hop")]
    public float hopSpeed = 14f;
    public float hopHeight = 0.12f;

    [Header("Squash & Stretch")]
    public float squashAmount = 0.15f;

    [Header("Tilt & Smooth")]
    public float tiltAngle = 8f;
    public float smooth = 12f;

    private Rigidbody2D rb;
    private Transform visual;

    private float hopTimer;
    private float facing = 1f;
    private Vector3 basePos;
    private Vector3 baseScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        visual = transform.Find("Sprite"); // child pertama = Sprite
    }

    private void Start()
    {
        basePos = visual.localPosition;
        baseScale = visual.localScale;
    }

    private void Update()
    {
        Vector2 vel = rb.linearVelocity;
        bool moving = vel.magnitude > 0.1f;

        // arah hadap
        if (vel.x > 0.1f) facing = 1f;
        else if (vel.x < -0.1f) facing = -1f;

        Vector3 targetPos = basePos;
        Vector3 targetScale = new Vector3(baseScale.x * facing, baseScale.y, baseScale.z);
        float targetTilt = 0f;

        if (moving)
        {
            hopTimer += Time.deltaTime * hopSpeed;
            float hop = Mathf.Abs(Mathf.Sin(hopTimer)); // 0 = di tanah, 1 = puncak

            targetPos += Vector3.up * hop * hopHeight;

            float stretch = (hop - 0.5f) * 2f * squashAmount; // + saat naik, - saat mendarat
            targetScale = new Vector3(
                baseScale.x * facing * (1f - stretch * 0.5f),
                baseScale.y * (1f + stretch),
                baseScale.z);

            targetTilt = -vel.x / 5f * tiltAngle;
        }
        else
        {
            hopTimer = 0f;
            float breathe = Mathf.Sin(Time.time * 2f) * 0.03f; // napas idle
            targetScale = new Vector3(baseScale.x * facing, baseScale.y * (1f + breathe), baseScale.z);
        }

        float t = Time.deltaTime * smooth;
        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, t);
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, t);
        visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0, 0, targetTilt), t);
    }
}