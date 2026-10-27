using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Visuals for the robot's "Servo Sweep" attack, made only from 2D rectangles:
//  - the FAN: a protractor-like arc that shows the exact angle the laser will sweep through
//  - the BEAM: the laser itself
// Created automatically by RobotEnemy.
public class RobotLaser : MonoBehaviour
{
    private static readonly Color ScanColor = new Color(0.20f, 0.75f, 1f, 0.11f);
    private static readonly Color EdgeColor = new Color(1f, 0.55f, 0.10f, 0.95f);
    private static readonly Color DotColor = new Color(0.65f, 0.92f, 1f, 1f);
    private static readonly Color BeamGlowColor = new Color(1f, 0.55f, 0.15f, 0.55f);
    private static readonly Color BeamCoreColor = new Color(1f, 0.97f, 0.85f, 1f);

    private GameObject fanRoot;
    private GameObject beamRoot;

    private readonly List<SpriteRenderer> scanLines = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> ticks = new List<SpriteRenderer>();

    private SpriteRenderer edgeA;
    private SpriteRenderer edgeB;
    private SpriteRenderer beamGlow;
    private SpriteRenderer beamCore;
    private SpriteRenderer beamSpark;
    private TextMeshPro label;

    private bool locked;
    private int shownDegrees;

    private void Awake()
    {
        fanRoot = new GameObject("Fan");
        fanRoot.transform.SetParent(transform, false);

        beamRoot = new GameObject("Beam");
        beamRoot.transform.SetParent(transform, false);

        edgeA = MakeRect(fanRoot.transform, "EdgeA", EdgeColor, -34);
        edgeB = MakeRect(fanRoot.transform, "EdgeB", EdgeColor, -34);

        beamGlow = MakeRect(beamRoot.transform, "Glow", BeamGlowColor, 30);
        beamCore = MakeRect(beamRoot.transform, "Core", BeamCoreColor, 31);
        beamSpark = MakeRect(beamRoot.transform, "Spark", BeamCoreColor, 32);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(fanRoot.transform, false);
        label = labelObject.AddComponent<TextMeshPro>();
        label.fontSize = 3f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = DotColor;
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(10, 20, 40, 255);
        label.rectTransform.sizeDelta = new Vector2(6f, 2f);
        labelObject.GetComponent<MeshRenderer>().sortingOrder = 75;

        fanRoot.SetActive(false);
        beamRoot.SetActive(false);
    }

    // ------------------------------------------------------------------ public API

    public void ShowFan(Vector2 origin, float centerAngle, float sweepAngle, System.Func<float, float> lengthAt)
    {
        locked = false;
        fanRoot.SetActive(true);
        UpdateFan(origin, centerAngle, sweepAngle, lengthAt);
    }

    public void UpdateFan(Vector2 origin, float centerAngle, float sweepAngle, System.Func<float, float> lengthAt)
    {
        shownDegrees = Mathf.RoundToInt(sweepAngle);

        float half = sweepAngle * 0.5f;
        int n = Mathf.Max(2, Mathf.CeilToInt(sweepAngle / 5f));

        EnsureCount(scanLines, n + 1, "Scan", ScanColor, -41);
        EnsureCount(dots, n + 1, "Dot", DotColor, -36);
        EnsureCount(ticks, n + 1, "Tick", Color.white, -35);

        for (int i = 0; i <= n; i++)
        {
            float a = centerAngle - half + sweepAngle * i / n;
            float len = lengthAt(a);
            Vector2 dir = Dir(a);

            PlaceRect(scanLines[i], origin + dir * (len * 0.5f), a, len, 0.5f);
            PlaceRect(dots[i], origin + dir * len, a, 0.14f, 0.14f);

            bool showTick = i % 3 == 0;
            ticks[i].gameObject.SetActive(showTick);
            if (showTick)
            {
                PlaceRect(ticks[i], origin + dir * (len - 0.2f), a, 0.4f, 0.05f);
            }

            if (i == 0) PlaceRect(edgeA, origin + dir * (len * 0.5f), a, len, 0.07f);
            if (i == n) PlaceRect(edgeB, origin + dir * (len * 0.5f), a, len, 0.07f);
        }

        float centerLength = lengthAt(centerAngle);
        label.transform.position = origin + Dir(centerAngle) * (centerLength + 0.7f);
        label.text = BuildLabel();
    }

    public void SetLocked(bool value)
    {
        if (value == locked) return;

        locked = value;
        label.text = BuildLabel();
    }

    public void ShowBeam(Vector2 origin, float angle, float length, float width)
    {
        beamRoot.SetActive(true);

        Vector2 dir = Dir(angle);
        PlaceRect(beamGlow, origin + dir * (length * 0.5f), angle, length, width * 1.8f);
        PlaceRect(beamCore, origin + dir * (length * 0.5f), angle, length, width * 0.45f);
        PlaceRect(beamSpark, origin + dir * length, angle + 45f, width * 1.2f, width * 1.2f);
    }

    public void HideAll()
    {
        fanRoot.SetActive(false);
        beamRoot.SetActive(false);
    }

    // ------------------------------------------------------------------ internals

    private void Update()
    {
        if (beamRoot.activeSelf)
        {
            float flicker = 0.85f + Mathf.Sin(Time.time * 80f) * 0.15f;
            Color glow = BeamGlowColor;
            glow.a = BeamGlowColor.a * flicker;
            beamGlow.color = glow;
        }

        if (fanRoot.activeSelf)
        {
            Color edge = EdgeColor;
            if (locked)
            {
                edge.a = 0.6f + 0.4f * (Mathf.Sin(Time.time * 40f) * 0.5f + 0.5f);
            }
            edgeA.color = edge;
            edgeB.color = edge;
        }
    }

    private string BuildLabel()
    {
        string title = locked ? "LOCKED" : "CALIBRATING";
        return title + "\n" + shownDegrees + " DEG";
    }

    private void EnsureCount(List<SpriteRenderer> list, int count, string objName, Color color, int order)
    {
        while (list.Count < count)
        {
            list.Add(MakeRect(fanRoot.transform, objName, color, order));
        }

        for (int i = 0; i < list.Count; i++)
        {
            bool on = i < count;
            if (list[i].gameObject.activeSelf != on)
            {
                list[i].gameObject.SetActive(on);
            }
        }
    }

    private SpriteRenderer MakeRect(Transform parent, string objName, Color color, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GameFX.Pixel;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    private static void PlaceRect(SpriteRenderer sr, Vector2 position, float angleDegrees, float length, float thickness)
    {
        sr.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDegrees));
        sr.transform.localScale = new Vector3(length, thickness, 1f);
    }

    private static Vector2 Dir(float angleDegrees)
    {
        float r = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }
}
