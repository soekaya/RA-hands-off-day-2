using UnityEngine;

// Added to the Main Camera automatically the first time something calls CameraShake.Shake / HitStop.
[DefaultExecutionOrder(-1000)]
public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;

    private float strength;
    private float timeLeft;
    private float total = 0.0001f;

    private bool offsetApplied;
    private Vector3 lastOffset;

    private float hitStopLeft;
    private bool stopActive;

    public static void Shake(float shakeStrength, float duration)
    {
        CameraShake s = GetInstance();
        if (s == null) return;

        // keep the stronger shake if one is already running
        float currentAmp = s.timeLeft > 0f ? s.strength * (s.timeLeft / s.total) : 0f;
        if (shakeStrength < currentAmp) return;

        s.strength = shakeStrength;
        s.timeLeft = duration;
        s.total = Mathf.Max(0.0001f, duration);
    }

    // Short freeze frame when something is hit (makes hits feel heavy)
    public static void HitStop(float duration)
    {
        CameraShake s = GetInstance();
        if (s == null) return;

        s.hitStopLeft = Mathf.Max(s.hitStopLeft, duration);
    }

    private static CameraShake GetInstance()
    {
        if (instance != null) return instance;

        Camera cam = Camera.main;
        if (cam == null) return null;

        instance = cam.GetComponent<CameraShake>();
        if (instance == null)
        {
            instance = cam.gameObject.AddComponent<CameraShake>();
        }

        return instance;
    }

    private void Update()
    {
        RemoveOffset();

        if (hitStopLeft > 0f)
        {
            hitStopLeft -= Time.unscaledDeltaTime;

            if (hitStopLeft > 0f)
            {
                Time.timeScale = 0.05f;
                stopActive = true;
            }
            else
            {
                Time.timeScale = 1f;
                stopActive = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f) return;

        timeLeft -= Time.unscaledDeltaTime;

        float amp = strength * Mathf.Clamp01(timeLeft / total);
        Vector2 random = Random.insideUnitCircle * amp;
        lastOffset = new Vector3(random.x, random.y, 0f);

        transform.position += lastOffset;
        offsetApplied = true;
    }

    private void RemoveOffset()
    {
        if (!offsetApplied) return;

        transform.position -= lastOffset;
        lastOffset = Vector3.zero;
        offsetApplied = false;
    }

    private void OnDisable()
    {
        RemoveOffset();

        if (stopActive)
        {
            Time.timeScale = 1f;
            stopActive = false;
        }
    }
}
