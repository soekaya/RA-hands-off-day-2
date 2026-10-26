using UnityEngine;

public class GunVisual : MonoBehaviour
{
    public float kickDistance = 0.15f;
    public float kickAngle = 12f;
    public float recover = 15f;

    private Transform visual;
    private Vector3 basePos;
    private float kick; // 1 = hentakan penuh, kembali ke 0

    private void Awake()
    {
        visual = transform.Find("Sprite");
    }

    private void Start()
    {
        basePos = visual.localPosition;
    }

    public void Kick()
    {
        kick = 1f;
    }

    private void Update()
    {
        kick = Mathf.Lerp(kick, 0f, Time.deltaTime * recover);

        visual.localPosition = basePos + Vector3.left * kick * kickDistance;
        visual.localRotation = Quaternion.Euler(0, 0, kick * kickAngle);
    }
}