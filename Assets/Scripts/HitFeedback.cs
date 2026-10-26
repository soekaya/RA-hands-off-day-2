using System.Collections.Generic;
using UnityEngine;

// Runs BEFORE HopVisual / PlayerVisual so the shake offset can be removed
// before they animate, and added back after (LateUpdate). No fighting between scripts.
[DefaultExecutionOrder(-100)]
public class HitFeedback : MonoBehaviour
{
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private readonly List<Transform> shakeTargets = new List<Transform>();

    private bool offsetApplied;
    private Vector3 lastOffset;

    private Color flashColor = Color.white;
    private float flashTimer;

    private float shakeStrength;
    private float shakeTimer;
    private float shakeTotal = 0.0001f;
    private bool shakeFade = true;

    private float blinkTimer;
    private bool colorsDirty;

    public float TopOffset { get; private set; }
    public Color[] OriginalColors { get { return originalColors; } }

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];

        float top = float.MinValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;

            if (renderers[i].enabled)
            {
                top = Mathf.Max(top, renderers[i].bounds.max.y);
            }
        }

        TopOffset = top > float.MinValue ? Mathf.Max(0.5f, top - transform.position.y) : 1.2f;

        // every direct child that contains sprites (Sprite, Gun...) shakes together
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<SpriteRenderer>(true) != null)
            {
                shakeTargets.Add(child);
            }
        }
    }

    private void OnDisable()
    {
        RemoveOffset();
    }

    private void Update()
    {
        RemoveOffset();

        float dt = Time.deltaTime;
        if (flashTimer > 0f) flashTimer -= dt;
        if (shakeTimer > 0f) shakeTimer -= dt;
        if (blinkTimer > 0f) blinkTimer -= dt;
    }

    private void LateUpdate()
    {
        ApplyShake();
        ApplyColors();
    }

    // ------------------------------------------------------------------ public API

    public void Flash(Color color, float duration)
    {
        flashColor = color;
        flashTimer = duration;
        colorsDirty = true;
    }

    public void Shake(float strength, float duration, bool fade = true)
    {
        shakeStrength = strength;
        shakeTimer = duration;
        shakeTotal = Mathf.Max(0.0001f, duration);
        shakeFade = fade;
    }

    public void StopShake()
    {
        shakeTimer = 0f;
    }

    public void Blink(float duration)
    {
        blinkTimer = duration;
        colorsDirty = true;
    }

    public void Hide()
    {
        RemoveOffset();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        enabled = false;
    }

    // ------------------------------------------------------------------ internals

    private void ApplyShake()
    {
        if (shakeTimer <= 0f) return;

        float amp = shakeStrength * (shakeFade ? Mathf.Clamp01(shakeTimer / shakeTotal) : 1f);
        Vector2 random = Random.insideUnitCircle * amp;
        lastOffset = new Vector3(random.x, random.y, 0f);

        for (int i = 0; i < shakeTargets.Count; i++)
        {
            if (shakeTargets[i] != null)
            {
                shakeTargets[i].localPosition += lastOffset;
            }
        }

        offsetApplied = true;
    }

    private void RemoveOffset()
    {
        if (!offsetApplied) return;

        for (int i = 0; i < shakeTargets.Count; i++)
        {
            if (shakeTargets[i] != null)
            {
                shakeTargets[i].localPosition -= lastOffset;
            }
        }

        lastOffset = Vector3.zero;
        offsetApplied = false;
    }

    private void ApplyColors()
    {
        bool flashing = flashTimer > 0f;
        bool blinking = blinkTimer > 0f;

        if (!flashing && !blinking && !colorsDirty) return;

        float alphaMultiplier = 1f;
        if (blinking)
        {
            alphaMultiplier = (Mathf.FloorToInt(Time.time * 14f) % 2 == 0) ? 0.35f : 1f;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Color c = flashing ? flashColor : originalColors[i];
            c.a = originalColors[i].a * alphaMultiplier;
            renderers[i].color = c;
        }

        colorsDirty = flashing || blinking;
    }
}
