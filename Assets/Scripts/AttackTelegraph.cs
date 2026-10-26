using System.Collections.Generic;
using UnityEngine;

// Classroom-themed dash warning: a red highlighter lane that fills up, a dashed red-pen line,
// a red "X" (like a wrong answer on an exam) where the dash ends, and a "!" above the enemy.
// Created automatically by EnemyMovement.
public class AttackTelegraph : MonoBehaviour
{
    private static readonly Color PenRed = new Color(0.90f, 0.12f, 0.18f, 1f);
    private static readonly Color Highlighter = new Color(1f, 0.20f, 0.28f, 0.20f);
    private static readonly Color HighlighterFill = new Color(1f, 0.20f, 0.28f, 0.45f);

    private SpriteRenderer lane;
    private SpriteRenderer fill;
    private SpriteRenderer markA;
    private SpriteRenderer markB;
    private SpriteRenderer bangBar;
    private SpriteRenderer bangDot;
    private readonly List<SpriteRenderer> dashes = new List<SpriteRenderer>();

    private Transform follow;
    private float headHeight = 1.8f;
    private Vector2 start;
    private Vector2 dir = Vector2.right;
    private float length = 1f;
    private float width = 1f;
    private float progress;
    private bool locked;
    private Quaternion rot = Quaternion.identity;

    private void Awake()
    {
        lane = MakeRect("Lane", Highlighter, -40);
        fill = MakeRect("Fill", HighlighterFill, -39);
        markA = MakeRect("MarkA", PenRed, -38);
        markB = MakeRect("MarkB", PenRed, -38);
        bangBar = MakeRect("BangBar", PenRed, 70);
        bangDot = MakeRect("BangDot", PenRed, 70);
    }

    private SpriteRenderer MakeRect(string objName, Color color, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GameFX.Pixel;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // ------------------------------------------------------------------ public API

    public void Show(Vector2 startPos, Vector2 direction, float len, float laneWidth, Transform followTarget, float head)
    {
        follow = followTarget;
        headHeight = head;
        width = laneWidth;
        progress = 0f;
        locked = false;

        Aim(startPos, direction, len);
        gameObject.SetActive(true);
    }

    public void Aim(Vector2 startPos, Vector2 direction, float len)
    {
        start = startPos;
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        length = Mathf.Max(0.1f, len);
        Layout();
    }

    public void SetProgress(float p)
    {
        progress = Mathf.Clamp01(p);
        UpdateFill();
    }

    public void SetLocked(bool value)
    {
        locked = value;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ layout

    private void Layout()
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rot = Quaternion.Euler(0f, 0f, angle);

        lane.transform.SetPositionAndRotation(start + dir * (length * 0.5f), rot);
        lane.transform.localScale = new Vector3(length, width, 1f);

        // dashed red-pen line down the middle of the lane
        int count = Mathf.Max(1, Mathf.FloorToInt(length / 0.6f));
        while (dashes.Count < count)
        {
            dashes.Add(MakeRect("Dash", PenRed, -37));
        }

        for (int i = 0; i < dashes.Count; i++)
        {
            bool on = i < count;
            dashes[i].gameObject.SetActive(on);
            if (!on) continue;

            float d = 0.3f + i * 0.6f;
            dashes[i].transform.SetPositionAndRotation(start + dir * d, rot);
            dashes[i].transform.localScale = new Vector3(0.32f, 0.09f, 1f);
        }

        // red X where the dash ends
        Vector3 end = start + dir * length;
        markA.transform.SetPositionAndRotation(end, Quaternion.Euler(0f, 0f, 45f));
        markB.transform.SetPositionAndRotation(end, Quaternion.Euler(0f, 0f, -45f));

        UpdateFill();
    }

    private void UpdateFill()
    {
        float l = Mathf.Max(0.001f, length * progress);
        fill.transform.SetPositionAndRotation(start + dir * (l * 0.5f), rot);
        fill.transform.localScale = new Vector3(l, width, 1f);
    }

    private void Update()
    {
        float pulse = Mathf.Sin(Time.time * (locked ? 40f : 18f)) * 0.5f + 0.5f;

        Color c = Highlighter;
        c.a = locked ? 0.30f + pulse * 0.10f : 0.14f + pulse * 0.08f;
        lane.color = c;

        float s = 1f + pulse * (locked ? 0.35f : 0.15f);
        Vector3 markScale = new Vector3(0.9f * s, 0.16f * s, 1f);
        markA.transform.localScale = markScale;
        markB.transform.localScale = markScale;

        if (follow != null)
        {
            Vector3 head = follow.position + Vector3.up * headHeight;
            float b = 1f + pulse * 0.25f;

            bangBar.transform.position = head + Vector3.up * 0.2f;
            bangBar.transform.localScale = new Vector3(0.17f * b, 0.42f * b, 1f);

            bangDot.transform.position = head - Vector3.up * 0.18f;
            bangDot.transform.localScale = new Vector3(0.17f * b, 0.17f * b, 1f);
        }
    }
}
