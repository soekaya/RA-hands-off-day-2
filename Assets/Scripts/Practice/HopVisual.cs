using UnityEngine;

public class HopVisual : MonoBehaviour
{
    public float hopSpeed = 12f;
    public float hopHeight = 0.1f;
    public float squashAmount = 0.15f;
    public float tiltAngle = 8f;
    public float maxSpeed = 5f;
    public float smooth = 12f;

    private Transform visual;
    private Vector3 basePos, baseScale, lastPos;
    private float hopTimer;
    private float facing = 1f;

    private void Awake()
    {
        visual = transform.GetChild(0);
    }

    private void Start()
    {
        basePos = visual.localPosition;
        baseScale = visual.localScale;
        lastPos = transform.position;
    }

    private void Update()
    {
        Vector2 vel = Vector2.zero;
        if (Time.deltaTime > 0f)
            vel = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;

        bool moving = vel.magnitude > 0.1f;
        if (vel.x > 0.1f) facing = 1f;
        else if (vel.x < -0.1f) facing = -1f;

        Vector3 targetPos = basePos;
        Vector3 targetScale = new Vector3(baseScale.x * facing, baseScale.y, baseScale.z);
        float targetTilt = 0f;

        if (moving)
        {
            hopTimer += Time.deltaTime * hopSpeed;
            float hop = Mathf.Abs(Mathf.Sin(hopTimer));
            targetPos += Vector3.up * hop * hopHeight;

            float stretch = (hop - 0.5f) * 2f * squashAmount;
            targetScale = new Vector3(
                baseScale.x * facing * (1f - stretch * 0.5f),
                baseScale.y * (1f + stretch),
                baseScale.z);

            targetTilt = -vel.x / maxSpeed * tiltAngle;
        }
        else
        {
            hopTimer = 0f;
            float breathe = Mathf.Sin(Time.time * 2f) * 0.03f;
            targetScale = new Vector3(baseScale.x * facing, baseScale.y * (1f + breathe), baseScale.z);
        }

        float t = Time.deltaTime * smooth;
        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, t);
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, t);
        visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0, 0, targetTilt), t);
    }
}