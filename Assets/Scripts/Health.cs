using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    public int maxHealth = 3;

    [Header("Player Only")]
    public float playerInvincibleTime = 0.8f;

    // SOUNDS: every prefab (player, enemy, fast enemy, robot...) has its own Health component,
    [Header("Sounds (this prefab's own)")]
    public AudioClip hitSFX;
    public AudioClip deathSFX;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("Optional: play this prefab's sounds from its own AudioSource (the player uses this). Empty = use the AudioManager.")]
    public AudioSource hitAudioSource;
    public AudioSource deathAudioSource;
    public AudioClip dropSFX;
    public AudioClip healDropSFX;

    [Header("Weak Point")]
    public float damageMultiplier = 1f;   // the robot sets this to 2 while it is overheated

    [Header("Enemy Drop (optional)")]
    public GameObject dropPrefab;
    [Range(0f, 1f)] public float dropChance = 0.5f;

    [Header("Heal Drop (optional, e.g. the POROS butterfly)")]
    public GameObject healDropPrefab;
    [Range(0f, 1f)] public float healDropChance = 0.15f;

    // damage, hit direction, knockback force
    public event System.Action<int, Vector2, float> onDamaged;
    public event System.Action onDied;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public bool IsDead { get { return isDead; } }

    private int currentHealth;
    private bool isDead;
    private bool isPlayer;
    private bool canRestart;
    private float invincibleUntil;
    private HitFeedback feedback;

    private static readonly Color PlayerHitColor = new Color(1f, 0.12f, 0.12f, 1f);
    private static readonly Color PlayerLightColor = new Color(1f, 0.55f, 0.55f, 1f);
    private static readonly Color PopupColor = new Color(1f, 0.92f, 0.45f, 1f);
    private static readonly Color HealColor = new Color(0.35f, 0.90f, 0.45f, 1f);

    private void Awake()
    {
        isPlayer = CompareTag("Player");

        // endless mode: enemies that appear in later waves have more HP
        if (!isPlayer && CompareTag("Enemy") && WaveManager.Instance != null)
        {
            maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * WaveManager.Instance.EnemyHealthMultiplier));
        }

        currentHealth = maxHealth;

        EnsureFeedback();

        if (GetComponent<HealthBar>() == null)
        {
            gameObject.AddComponent<HealthBar>();
        }
    }

    private void Update()
    {
        if (isDead && isPlayer && canRestart && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }

    private void EnsureFeedback()
    {
        if (feedback != null) return;

        feedback = GetComponent<HitFeedback>();
        if (feedback == null)
        {
            feedback = gameObject.AddComponent<HitFeedback>();
        }
    }

    // One place for all sounds of this object (player and enemies alike).
    // 1) its own AudioSource if given, 2) the AudioManager, 3) a temporary 2D source.
    private void PlaySound(AudioClip clip, AudioSource ownSource)
    {
        if (clip == null) return;

        if (ownSource != null)
        {
            ownSource.PlayOneShot(clip, sfxVolume);
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Sfx(clip).Volume(sfxVolume).RandomPitch(0.95f, 1.05f).Play();
            return;
        }

        // survives this object being destroyed
        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = transform.position;
        AudioSource aSource = tempAudio.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.volume = sfxVolume;
        aSource.spatialBlend = 0f;
        aSource.Play();
        Destroy(tempAudio, clip.length);
    }

    // Gives health back (never above max). Returns how much was really healed.
    public int Heal(int amount)
    {
        if (isDead || amount <= 0) return 0;

        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        int gained = currentHealth - before;
        if (gained <= 0) return 0;

        EnsureFeedback();
        GameFX.Popup(transform.position + Vector3.up * (feedback.TopOffset + 0.2f), "+" + gained, HealColor, 5f);
        feedback.Flash(new Color(0.65f, 1f, 0.75f, 1f), 0.15f);
        return gained;
    }

    // Raises max HP and gives that HP right away (used by the Heart Container card)
    public void AddMaxHealth(int amount)
    {
        if (isDead || amount <= 0) return;

        maxHealth += amount;
        currentHealth += amount;

        EnsureFeedback();
        GameFX.Popup(transform.position + Vector3.up * (feedback.TopOffset + 0.2f), "+" + amount + " MAX HP", HealColor, 4f);
    }

    // The player cannot be hurt for this many seconds (used by the dash)
    public void SetInvincible(float seconds)
    {
        invincibleUntil = Mathf.Max(invincibleUntil, Time.time + seconds);
    }

    // Old signature, still works for any script that calls TakeDamage(1)
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero, 0f);
    }

    public void TakeDamage(int damage, Vector2 hitDirection, float knockback = 0f)
    {
        if (isDead || damage <= 0) return;
        if (isPlayer && Time.time < invincibleUntil) return;

        EnsureFeedback();

        // weak point: e.g. the robot takes double damage while overheated
        damage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Vector2 dir = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector2.zero;

        Vector3 headPos = transform.position + Vector3.up * (feedback.TopOffset + 0.2f);
        Vector3 bodyPos = transform.position + Vector3.up * (feedback.TopOffset * 0.5f);

        // let the movement scripts react first (knockback, interrupt an attack...)
        if (onDamaged != null)
        {
            onDamaged(damage, dir, knockback);
        }

        if (isPlayer)
        {
            invincibleUntil = Time.time + playerInvincibleTime;

            PlaySound(hitSFX, hitAudioSource);

            feedback.Flash(PlayerHitColor, 0.2f);
            feedback.Shake(0.22f, 0.3f);
            feedback.Blink(playerInvincibleTime);

            GameFX.Burst(bodyPos, new Color[] { PlayerHitColor, PlayerLightColor }, 10, 5f, 0.18f, 0.45f, dir);
            GameFX.Popup(headPos, "-" + damage, PlayerHitColor, 5f);

            CameraShake.Shake(0.3f, 0.3f);
            CameraShake.HitStop(0.07f);
        }
        else
        {
            PlaySound(hitSFX, hitAudioSource);

            feedback.Flash(Color.white, 0.1f);
            feedback.Shake(0.12f, 0.15f);

            GameFX.Burst(bodyPos, feedback.OriginalColors, 6, 4.5f, 0.15f, 0.35f, dir);
            GameFX.Popup(headPos, damage.ToString(), PopupColor, 4f);

            CameraShake.Shake(0.07f, 0.1f);
        }

        if (currentHealth <= 0)
        {
            Die(dir);
        }
    }

    private void Die(Vector2 hitDirection)
    {
        if (isDead) return;
        isDead = true;

        if (onDied != null)
        {
            onDied();
        }

        if (isPlayer)
        {
            PlayerDie();
            return;
        }

        if (CompareTag("Enemy"))
        {
            PlaySound(deathSFX, deathAudioSource);

            if (LevelManager.instance != null)
            {
                LevelManager.instance.AddKill();
            }

            // the wave system counts the kills
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.RegisterKill();
            }
        }

        Vector3 bodyPos = transform.position + Vector3.up * (feedback.TopOffset * 0.5f);
        GameFX.Burst(bodyPos, feedback.OriginalColors, 16, 7f, 0.22f, 0.6f, hitDirection);
        CameraShake.Shake(0.15f, 0.18f);
        CameraShake.HitStop(0.05f);

        if (dropPrefab != null && Random.value <= dropChance)
        {
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        }

        // a second, separate drop: the heal item (each one has its own chance)
        if (healDropPrefab != null && Random.value <= healDropChance)
        {
            Instantiate(healDropPrefab, transform.position, Quaternion.identity);
        }

        // stop everything, show the white flash for a moment, then remove
        EnemyMovement ai = GetComponent<EnemyMovement>();
        if (ai != null) ai.enabled = false;

        RobotEnemy robot = GetComponent<RobotEnemy>();
        if (robot != null) robot.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        feedback.Flash(Color.white, 0.2f);
        Destroy(gameObject, 0.08f);
    }

    private void PlayerDie()
    {
        PlayerMovement move = GetComponent<PlayerMovement>();
        if (move != null) move.enabled = false;

        Gun[] guns = GetComponentsInChildren<Gun>();
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        PlaySound(deathSFX, deathAudioSource);

        Vector3 bodyPos = transform.position + Vector3.up * (feedback.TopOffset * 0.5f);
        GameFX.Burst(bodyPos, feedback.OriginalColors, 28, 8f, 0.25f, 0.9f, Vector2.zero);
        GameFX.Burst(bodyPos, new Color[] { PlayerHitColor, PlayerLightColor }, 14, 6f, 0.2f, 0.7f, Vector2.zero);
        CameraShake.Shake(0.5f, 0.5f);

        feedback.Hide();

        // R only works when this scene is in the Build Profiles scene list
        canRestart = Application.CanStreamedLevelBeLoaded(gameObject.scene.name);
        string hint = canRestart ? "Press R to restart" : "Stop and press Play to try again";

        // score line: wave reached, kills, best wave
        if (WaveManager.Instance != null)
        {
            int kills = LevelManager.instance != null ? LevelManager.instance.killCount : 0;
            string score = "Wave " + WaveManager.Instance.CurrentWave + "     Kills " + kills
                         + "     Best: Wave " + WaveManager.Instance.BestWave;
            if (WaveManager.Instance.IsNewBest) score += "   NEW BEST!";

            hint = score + "\n" + hint;
        }

        GameFX.CenterText("YOU DIED", hint);
    }
}