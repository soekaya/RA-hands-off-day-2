using TMPro;
using UnityEngine;

// Created by GameFX. Moves, fades and destroys itself. Works for square particles and floating text.
public class FXParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float life = 0.5f;
    private float age;
    private float drag = 5f;

    private SpriteRenderer sprite;
    private TextMeshPro text;
    private Vector3 startScale = Vector3.one;
    private Color startColor = Color.white;

    public void Init(Vector2 startVelocity, float lifeTime, float dragAmount)
    {
        velocity = startVelocity;
        life = Mathf.Max(0.05f, lifeTime);
        drag = dragAmount;

        startScale = transform.localScale;
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            startColor = sprite.color;
        }
    }

    public void InitText(TextMeshPro tmp, Vector2 startVelocity, float lifeTime, float dragAmount)
    {
        Init(startVelocity, lifeTime, dragAmount);
        text = tmp;
        startColor = tmp.color;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        float t = Mathf.Clamp01(age / life);

        transform.position += (Vector3)(velocity * dt);
        velocity = Vector2.Lerp(velocity, Vector2.zero, Mathf.Clamp01(drag * dt));

        Color c = startColor;
        c.a = startColor.a * (1f - t * t);

        if (text != null)
        {
            text.color = c;
            transform.localScale = startScale * (1f + 0.35f * (1f - t));
        }
        else if (sprite != null)
        {
            sprite.color = c;
            transform.localScale = startScale * (1f - t * 0.8f);
        }

        if (age >= life)
        {
            Destroy(gameObject);
        }
    }
}
