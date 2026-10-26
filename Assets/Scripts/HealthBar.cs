using UnityEngine;

// Added automatically by Health. Player: always visible. Enemies: show for a few seconds after being hit.
public class HealthBar : MonoBehaviour
{
    private const float Width = 1.1f;
    private const float Height = 0.14f;

    private static readonly Color BackColor = new Color(0.08f, 0.07f, 0.10f, 1f);
    private static readonly Color EmptyColor = new Color(0.30f, 0.27f, 0.33f, 1f);
    private static readonly Color EnemyColor = new Color(0.90f, 0.29f, 0.29f, 1f);
    private static readonly Color PlayerHigh = new Color(0.35f, 0.85f, 0.45f, 1f);
    private static readonly Color PlayerLow = new Color(0.95f, 0.25f, 0.25f, 1f);

    private Health health;
    private GameObject barRoot;
    private Transform fill;
    private SpriteRenderer fillRenderer;
    private bool alwaysVisible;
    private float showTimer;
    private int lastHealth;
    private float displayRatio = 1f;

    private void Awake()
    {
        health = GetComponent<Health>();

        HitFeedback feedback = GetComponent<HitFeedback>();
        float top = feedback != null ? feedback.TopOffset : 1.2f;

        alwaysVisible = CompareTag("Player");
        lastHealth = health != null ? health.CurrentHealth : 0;

        Build(top + 0.3f);
        barRoot.SetActive(alwaysVisible);
    }

    private void Build(float height)
    {
        barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = new Vector3(0f, height, 0f);

        MakeRect("Back", 0f, Width + 0.12f, Height + 0.12f, BackColor, 60);
        MakeRect("Empty", 0f, Width, Height, EmptyColor, 61);

        SpriteRenderer fillSr = MakeRect("Fill", 0f, Width, Height, EnemyColor, 62);
        fill = fillSr.transform;
        fillRenderer = fillSr;

        // small dividers so you can count the hits
        if (health != null && health.MaxHealth > 1 && health.MaxHealth <= 12)
        {
            for (int i = 1; i < health.MaxHealth; i++)
            {
                float x = -Width * 0.5f + Width * i / health.MaxHealth;
                MakeRect("Divider", x, 0.03f, Height, BackColor, 63);
            }
        }
    }

    private SpriteRenderer MakeRect(string objName, float x, float w, float h, Color color, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(barRoot.transform, false);
        go.transform.localPosition = new Vector3(x, 0f, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GameFX.Pixel;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    private void Update()
    {
        if (health == null || barRoot == null) return;

        if (health.CurrentHealth != lastHealth)
        {
            lastHealth = health.CurrentHealth;
            showTimer = 3f;
        }

        if (showTimer > 0f) showTimer -= Time.deltaTime;

        bool visible = !health.IsDead && (alwaysVisible || showTimer > 0f);
        if (barRoot.activeSelf != visible)
        {
            barRoot.SetActive(visible);
        }
        if (!visible) return;

        float ratio = health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0f;
        displayRatio = Mathf.MoveTowards(displayRatio, ratio, Time.deltaTime * 1.5f);

        float w = Mathf.Max(0.0001f, Width * displayRatio);
        fill.localScale = new Vector3(w, Height, 1f);
        fill.localPosition = new Vector3(-Width * 0.5f + w * 0.5f, 0f, 0f);

        fillRenderer.color = alwaysVisible ? Color.Lerp(PlayerLow, PlayerHigh, ratio) : EnemyColor;
    }
}
