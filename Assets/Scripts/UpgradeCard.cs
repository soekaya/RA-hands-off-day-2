using TMPro;
using UnityEngine;

public class UpgradeCard : MonoBehaviour
{
    public static readonly Vector2 Size = new Vector2(3.2f, 4.6f);

    private static readonly Color CostOk = new Color(0.16f, 0.12f, 0.04f, 1f);
    private static readonly Color CostBad = new Color(0.80f, 0.10f, 0.10f, 1f);

    private TextMeshPro title;
    private TextMeshPro cost;
    private TextMeshPro description;
    private TextMeshPro key;
    private SpriteRenderer art;
    private Vector3 baseScale;
    private float appear = 0.3f;
    private bool hovered;

    private void Awake()
    {
        title = FindText("Title");
        cost = FindText("Cost");
        description = FindText("Description");
        key = FindText("Key");

        Transform artTransform = transform.Find("Art");
        if (artTransform != null)
        {
            art = artTransform.GetComponent<SpriteRenderer>();
        }

        baseScale = transform.localScale;
    }

    private TextMeshPro FindText(string childName)
    {
        Transform t = transform.Find(childName);
        return t != null ? t.GetComponent<TextMeshPro>() : null;
    }

    public void Setup(int number, string titleText, string descriptionText, int price, bool canAfford, Color artColor)
    {
        if (title != null) title.text = titleText;
        if (description != null) description.text = descriptionText;
        if (key != null) key.text = "Press " + number;

        if (cost != null)
        {
            cost.text = price.ToString();
            cost.color = canAfford ? CostOk : CostBad;
        }

        if (art != null) art.color = artColor;
    }

    public void SetHover(bool value)
    {
        hovered = value;
    }

    public bool Contains(Vector2 point)
    {
        Vector2 center = transform.position;
        Vector2 half = Size * 0.5f * baseScale.x;
        return Mathf.Abs(point.x - center.x) <= half.x && Mathf.Abs(point.y - center.y) <= half.y;
    }

    private void Update()
    {
        appear = Mathf.MoveTowards(appear, 1f, Time.deltaTime * 6f);
        float hoverScale = hovered ? 1.06f : 1f;
        transform.localScale = baseScale * appear * hoverScale;
    }
}
