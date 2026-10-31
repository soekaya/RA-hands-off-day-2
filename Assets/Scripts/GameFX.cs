using TMPro;
using UnityEngine;

public static class GameFX
{
    private static Sprite pixel;

    // A 1x1 unit white sprite. Scale it to any size and tint it with SpriteRenderer.color.
    public static Sprite Pixel
    {
        get
        {
            if (pixel == null)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return pixel;
        }
    }

    public static void Burst(Vector3 position, Color color, int count, float speed, float size, float life, Vector2 direction)
    {
        Burst(position, new Color[] { color }, count, speed, size, life, direction);
    }

    public static void Burst(Vector3 position, Color[] palette, int count, float speed, float size, float life, Vector2 direction)
    {
        if (palette == null || palette.Length == 0)
        {
            palette = new Color[] { Color.white };
        }

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("FX");

            Vector2 jitter = Random.insideUnitCircle * 0.25f;
            go.transform.position = new Vector3(position.x + jitter.x, position.y + jitter.y, 0f);

            float s = size * Random.Range(0.6f, 1.3f);
            go.transform.localScale = new Vector3(s, s, 1f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Pixel;

            Color c = palette[Random.Range(0, palette.Length)];
            c.a = 1f;
            sr.color = c;
            sr.sortingOrder = 100;

            Vector2 v = Random.insideUnitCircle.normalized * speed * Random.Range(0.3f, 1f);
            v += direction * speed * 0.7f;

            go.AddComponent<FXParticle>().Init(v, life * Random.Range(0.7f, 1.2f), 5f);
        }
    }

    public static void Popup(Vector3 position, string text, Color color, float size)
    {
        GameObject go = new GameObject("Popup");
        go.transform.position = new Vector3(position.x + Random.Range(-0.3f, 0.3f), position.y, 0f);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts/BoldPixels SDF");
        tmp.fontSharedMaterial = Resources.Load<Material>("Fonts/BoldPixels SDF Outline");
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.outlineWidth = 0.25f;
        tmp.rectTransform.sizeDelta = new Vector2(6f, 2f);
        go.GetComponent<MeshRenderer>().sortingOrder = 200;

        go.AddComponent<FXParticle>().InitText(tmp, new Vector2(0f, 4f), 0.8f, 5f);
    }

    public static void CenterText(string title, string subtitle)
    {
        Camera cam = Camera.main;
        Vector3 center = cam != null ? cam.transform.position : Vector3.zero;
        center.z = 0f;

        GameObject back = new GameObject("CenterTextBack");
        back.transform.position = center;
        back.transform.localScale = new Vector3(40f, 4.6f, 1f);

        SpriteRenderer sr = back.AddComponent<SpriteRenderer>();
        sr.sprite = Pixel;
        sr.color = new Color(0.05f, 0.04f, 0.08f, 0.72f);
        sr.sortingOrder = 190;

        MakeText("CenterTitle", title, center + new Vector3(0f, 0.55f, 0f), 12f, new Color(1f, 0.35f, 0.35f, 1f), 201);
        MakeText("CenterSubtitle", subtitle, center + new Vector3(0f, -1.15f, 0f), 4.5f, Color.white, 201);
    }

    private static void MakeText(string objName, string text, Vector3 position, float size, Color color, int order)
    {
        GameObject go = new GameObject(objName);
        go.transform.position = position;
        
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts/BoldPixels SDF");
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(30f, 6f);
        go.GetComponent<MeshRenderer>().sortingOrder = order;
    }
}
