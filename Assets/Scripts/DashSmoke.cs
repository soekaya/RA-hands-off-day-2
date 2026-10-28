using UnityEngine;

public class DashSmoke : MonoBehaviour
{
    public float lifeTime = 0.5f;
    public float growth = 1.2f;
    public float driftSpeed = 0.6f;

    private SpriteRenderer[] parts;
    private float[] alphas;
    private Vector3 startScale;
    private Vector2 drift;
    private float age;

    private void Start()
    {
        parts = GetComponentsInChildren<SpriteRenderer>();
        alphas = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            alphas[i] = parts[i].color.a;
        }

        startScale = transform.localScale;
        drift = Random.insideUnitCircle * driftSpeed;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / lifeTime);

        transform.localScale = startScale * (1f + growth * t);
        transform.position += (Vector3)(drift * Time.deltaTime);

        for (int i = 0; i < parts.Length; i++)
        {
            Color c = parts[i].color;
            c.a = alphas[i] * (1f - t);
            parts[i].color = c;
        }

        if (age >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
