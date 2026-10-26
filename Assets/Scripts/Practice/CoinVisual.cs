using UnityEngine;

public class CoinVisual : MonoBehaviour
{
    public float spinSpeed = 3f;
    public float bobSpeed = 3f;
    public float bobHeight = 0.08f;

    private Transform visual;
    private Vector3 basePos, baseScale;
    private float offset;

    private void Awake()
    {
        visual = transform.Find("Sprite");
        offset = Random.value * 10f; // supaya banyak koin tidak sinkron
    }

    private void Start()
    {
        basePos = visual.localPosition;
        baseScale = visual.localScale;
    }

    private void Update()
    {
        float time = Time.time + offset;

        float spin = Mathf.Cos(time * spinSpeed);
        float pulse = 1f + Mathf.Sin(time * 6f) * 0.04f;

        visual.localScale = new Vector3(baseScale.x * spin * pulse, baseScale.y * pulse, baseScale.z);
        visual.localPosition = basePos + Vector3.up * Mathf.Sin(time * bobSpeed) * bobHeight;
    }
}