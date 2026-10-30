using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private enum State { Chase, Windup, Dash, Recover }

    [Header("Chase")]
    public float moveSpeed = 3f;
    public float stopDistance = 0.9f;
    public float acceleration = 30f;
    public float separationRadius = 1.1f;
    public float separationForce = 2f;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    public float contactKnockback = 6f;

    [Header("Dash Attack")]
    public bool canDash = true;
    public float dashMinRange = 2f;
    public float dashMaxRange = 5.5f;
    public float dashCooldown = 3.5f;
    public float windupTime = 0.8f;
    public float lockTime = 0.2f;        // aim is frozen for the last part of the windup
    public float dashSpeed = 16f;
    public float dashDistance = 4.5f;
    public int dashDamage = 1;
    public float dashKnockback = 12f;
    public float recoverTime = 0.8f;
    public float wallStunBonus = 0.8f;   // extra dazed time when it crashes into a wall
    public float laneWidth = 1f;
    [Header("Sound")]
    public AudioClip dashSfx;
    public float dashVolume = 1f;
    public AudioClip contactSfx;      
    public float contactVolume = 1f;
    public AudioClip spawnSfx;
    public float spawnVolume = 0.6f;


    private const float WallPadding = 0.7f;
    private static readonly Color DustColor = new Color(0.80f, 0.80f, 0.86f, 1f);
    private static readonly List<EnemyMovement> all = new List<EnemyMovement>();

    private State state = State.Chase;
    private float stateTimer;
    private float nextDashTime;
    private float nextContactTime;
    private float recoverDuration;

    private Vector2 dashDirection = Vector2.right;
    private Vector2 dashStart;
    private float dashLength;
    private bool dashHit;

    private Vector2 knockVelocity;
    private float knockTimer;
    private float knockDuration = 0.18f;

    private Transform player;
    private Health playerHealth;
    private Rigidbody2D rb;
    private Health health;
    private HopVisual hop;
    private HitFeedback feedback;
    private AttackTelegraph telegraph;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        // a sleeping body would stop OnTriggerStay2D, so the enemy never sleeps
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        Collider2D[] cols = GetComponents<Collider2D>();
        if (cols.Length == 0)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, 1.9f);
            box.isTrigger = true;
        }
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].isTrigger = true;
        }

        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }

        hop = GetComponent<HopVisual>();
        if (hop != null)
        {
            hop.autoFace = false; // this script decides which way the enemy looks
        }
    }

    private void OnEnable()
    {
        all.Add(this);
        if (health != null)
        {
            health.onDamaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        all.Remove(this);
        if (health != null)
        {
            health.onDamaged -= OnDamaged;
        }
        if (telegraph != null)
        {
            telegraph.Hide();
        }
    }

    private void OnDestroy()
    {
        if (telegraph != null)
        {
            Destroy(telegraph.gameObject);
        }
    }

    private void Start()
    {
        feedback = GetComponent<HitFeedback>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
            if (playerHealth == null)
            {
                playerHealth = p.AddComponent<Health>();
            }
        }

        nextDashTime = Time.time + Random.Range(1f, dashCooldown);
        nextContactTime = Time.time + 0.6f; // short grace period right after spawning

        GameFX.Burst(transform.position, DustColor, 6, 2.5f, 0.2f, 0.35f, Vector2.zero);
        if (spawnSfx != null)
        {
            AudioSource.PlayClipAtPoint(spawnSfx, Camera.main.transform.position, spawnVolume);
        }
    }

    private void Update()
    {
        if (player == null || hop == null) return;

        if (state == State.Windup || state == State.Dash)
        {
            hop.Face(dashDirection.x);
        }
        else
        {
            hop.Face(player.position.x - transform.position.x);
        }

        hop.dashDirection = dashDirection;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        float dt = Time.fixedDeltaTime;

        // knockback has priority (but a dash can't be interrupted)
        if (knockTimer > 0f && state != State.Dash)
        {
            knockTimer -= dt;
            rb.linearVelocity = knockVelocity * Mathf.Clamp01(knockTimer / knockDuration);
            return;
        }

        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            if (state != State.Chase)
            {
                ResetToChase();
            }
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
            return;
        }

        switch (state)
        {
            case State.Chase: UpdateChase(dt); break;
            case State.Windup: UpdateWindup(dt); break;
            case State.Dash: UpdateDash(dt); break;
            case State.Recover: UpdateRecover(dt); break;
        }
    }

    // ------------------------------------------------------------------ states

    private void UpdateChase(float dt)
    {
        Vector2 pos = rb.position;
        Vector2 toPlayer = (Vector2)player.position - pos;
        float dist = toPlayer.magnitude;
        Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.zero;

        // stop next to the player instead of jittering back and forth on top of them
        Vector2 desired = dist > stopDistance ? dir * moveSpeed : Vector2.zero;
        desired += GetSeparation(pos);

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * dt);

        if (canDash && Time.time >= nextDashTime && dist >= dashMinRange && dist <= dashMaxRange)
        {
            StartWindup(dir);
        }
    }

    private void StartWindup(Vector2 dir)
    {
        state = State.Windup;
        stateTimer = 0f;
        dashStart = rb.position;
        dashDirection = dir;
        dashLength = ComputeDashLength(dashStart, dashDirection);

        EnsureTelegraph();
        float head = feedback != null ? feedback.TopOffset + 0.5f : 1.8f;
        telegraph.Show(dashStart, dashDirection, dashLength, laneWidth, transform, head);

        if (feedback != null)
        {
            feedback.Shake(0.07f, windupTime, false);
        }
    }

    private void UpdateWindup(float dt)
    {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
        stateTimer += dt;

        float lock_ = Mathf.Min(lockTime, windupTime * 0.5f);
        bool locked = stateTimer >= windupTime - lock_;

        if (!locked)
        {
            // keep aiming at the player until the lock moment
            Vector2 toPlayer = (Vector2)player.position - rb.position;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                dashStart = rb.position;
                dashDirection = toPlayer.normalized;
                dashLength = ComputeDashLength(dashStart, dashDirection);
                telegraph.Aim(dashStart, dashDirection, dashLength);
            }
        }

        telegraph.SetLocked(locked);
        telegraph.SetProgress(stateTimer / windupTime);

        if (hop != null)
        {
            hop.charge = Mathf.Clamp01(stateTimer / windupTime);
        }

        if (stateTimer >= windupTime)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        state = State.Dash;
        stateTimer = 0f;
        dashHit = false;
        dashStart = rb.position;

        telegraph.Hide();

        if (dashSfx != null)
        {
            AudioSource.PlayClipAtPoint(dashSfx, Camera.main.transform.position, dashVolume);
        }

        if (feedback != null)
        {
            feedback.StopShake();
        }
        if (hop != null)
        {
            hop.charge = 0f;
            hop.dashing = true;
        }

        GameFX.Burst(transform.position, DustColor, 6, 3f, 0.2f, 0.3f, -dashDirection);
    }

    private void UpdateDash(float dt)
    {
        stateTimer += dt;
        rb.linearVelocity = dashDirection * dashSpeed;

        float traveled = Vector2.Distance(rb.position, dashStart);
        float maxTime = dashLength / dashSpeed + 0.15f;

        if (traveled >= dashLength || stateTimer >= maxTime)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        bool crashed = dashLength < dashDistance - 0.05f;

        rb.linearVelocity = Vector2.zero;
        state = State.Recover;
        stateTimer = 0f;
        recoverDuration = recoverTime + (crashed ? wallStunBonus : 0f);
        nextDashTime = Time.time + dashCooldown * Random.Range(0.8f, 1.2f);

        if (hop != null)
        {
            hop.dashing = false;
        }

        if (crashed)
        {
            Vector3 crashPos = transform.position + (Vector3)(dashDirection * 0.7f);
            GameFX.Burst(crashPos, DustColor, 10, 5f, 0.2f, 0.4f, -dashDirection);
            CameraShake.Shake(0.12f, 0.15f);

            if (feedback != null)
            {
                feedback.Shake(0.08f, 0.3f);
            }
        }
    }

    private void UpdateRecover(float dt)
    {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
        stateTimer += dt;

        if (stateTimer >= recoverDuration)
        {
            state = State.Chase;
            stateTimer = 0f;
        }
    }

    private void ResetToChase()
    {
        state = State.Chase;
        stateTimer = 0f;

        if (hop != null)
        {
            hop.charge = 0f;
            hop.dashing = false;
        }
        if (telegraph != null)
        {
            telegraph.Hide();
        }
        if (feedback != null)
        {
            feedback.StopShake();
        }
    }

    // ------------------------------------------------------------------ helpers

    private void EnsureTelegraph()
    {
        if (telegraph != null) return;

        GameObject go = new GameObject("DashTelegraph");
        telegraph = go.AddComponent<AttackTelegraph>();
    }

    // Shortens the dash so it ends at a classroom wall instead of passing through it.
    private float ComputeDashLength(Vector2 origin, Vector2 dir)
    {
        float best = dashDistance;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, dashDistance + WallPadding + 0.5f);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].collider.GetComponent<ClassroomWall>() == null) continue;

            float d = hits[i].distance - WallPadding;
            if (d < best) best = d;
        }

        return Mathf.Max(0.5f, best);
    }

    private Vector2 GetSeparation(Vector2 pos)
    {
        Vector2 push = Vector2.zero;

        for (int i = 0; i < all.Count; i++)
        {
            EnemyMovement other = all[i];
            if (other == null || other == this) continue;

            Vector2 diff = pos - (Vector2)other.transform.position;
            float d = diff.magnitude;

            if (d < 0.001f)
            {
                push += Random.insideUnitCircle;
            }
            else if (d < separationRadius)
            {
                push += diff / d * (1f - d / separationRadius);
            }
        }

        return Vector2.ClampMagnitude(push * separationForce, separationForce);
    }

    private void OnDamaged(int amount, Vector2 hitDir, float knockback)
    {
        // shooting an enemy while it charges cancels the dash
        if (state == State.Windup)
        {
            ResetToChase();
            nextDashTime = Time.time + 1.2f;
        }

        if (state == State.Dash) return;

        if (knockback > 0f && hitDir.sqrMagnitude > 0.0001f)
        {
            knockVelocity = hitDir.normalized * knockback;
            knockDuration = 0.18f;
            knockTimer = knockDuration;
        }
    }

    // ------------------------------------------------------------------ damage to the player

    private void OnTriggerStay2D(Collider2D other)
    {
        Health target = other.GetComponentInParent<Health>();
        if (target == null || target.IsDead || !target.CompareTag("Player")) return;

        if (state == State.Dash)
        {
            if (dashHit) return;
            dashHit = true;
            target.TakeDamage(dashDamage, dashDirection, dashKnockback);
            return;
        }

        if (state == State.Recover) return; // dazed enemies can't attack
        if (Time.time < nextContactTime) return;

        nextContactTime = Time.time + contactCooldown;

        Vector2 dir = (Vector2)(target.transform.position - transform.position);
        target.TakeDamage(contactDamage, dir, contactKnockback);
        if (contactSfx != null)                                   // <- baru dari sini
        {
            AudioSource.PlayClipAtPoint(contactSfx, Camera.main.transform.position, contactVolume);
        }
    }
}