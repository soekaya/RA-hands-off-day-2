using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    public float popTime = 0.12f;
    public float stretchX = 1.3f;
    public float wobble = 0.1f;

    private Transform visual;
    private Vector3 baseScale;
    private float age;

    private void Awake()
    {
        visual = transform.Find("Sprite");
        baseScale = visual.localScale;
    }

    private void Update()
    {
        age += Time.deltaTime;

        float pop = Mathf.Clamp01(age / popTime);
        pop = 1f + Mathf.Sin(pop * Mathf.PI) * 0.3f; // membesar sedikit lalu normal
        if (age >= popTime) pop = 1f;

        float w = 1f + Mathf.Sin(age * 30f) * wobble;

        visual.localScale = new Vector3(baseScale.x * stretchX * pop * w, baseScale.y * pop / w, baseScale.z);
    }
}