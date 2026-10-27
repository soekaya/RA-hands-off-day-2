using System.Collections.Generic;
using UnityEngine;

public class RobotEnemy : MonoBehaviour
{
    private enum State { Chase, Calibrate, Sweep, Overheat }

    [Header("Chase")]
    public float moveSpeed = 2.4f;
    public float keepDistance = 3.2f;       // the robot stops this far from the player
    public float acceleration = 25f;
    public float separationRadius = 1.3f;
    public float separationForce = 2f;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactCooldown = 1f;
    public float contactKnockback = 6f;

    [Header("Servo Sweep Attack")]
    public float attackMinRange = 2.2f;
    public float attackMaxRange = 6f;
    public float attackCooldown = 4f;
    public float calibrateTime = 1.1f;
    public float lockTime = 0.3f;           // aim is frozen for the last part of the calibration
    public float turnSpeed = 140f;          // degrees per second, like a servo motor
    public float sweepAngle = 100f;
    public float sweepTime = 1.1f;
    public float beamLength = 6.5f;
    public float beamWidth = 0.5f;
    public int beamDamage = 1;
    public float beamKnockback = 9f;
    public float playerRadius = 0.5f;
    public Vector2 beamOrigin = new Vector2(0f, 0.25f);   // the chest core

    [Header("Overheat (weak point)")]
    public float overheatTime = 1.8f;
    public float overheatDamageMultiplier = 2f;

    private static readonly List<RobotEnemy> all = new List<RobotEnemy>();
    private static readonly Color CyanSpark = new Color(0.35f, 0.85f, 1f, 1f);
    private static readonly Color OrangeSpark = new Color(1f, 0.55f, 0.12f, 1f);
    private static readonly Color DeadCore = new Color(0.2f, 0.2f, 0.25f, 1f);

    private State state = State.Chase;
    private float timer;
    private float nextAttackTime;
    private float nextContactTime;
    private float sparkTimer;

    private float aimAngle;
    private float sweepStart;
    private float sweepEnd;
    private bool beamHit;

    private Vector2 knockVelocity;
    private float knockTimer;
    private float knockDuration = 0.18f;

    private Transform player;
    private Health playerHealth;
    private Rigidbody2D rb;
    private Health health;
    private HopVisual hop;
    private HitFeedback feedback;
    private RobotLaser laser;
    private SpriteRenderer core;
    private Color coreBase = Color.white;

    private Vector2 Origin
    {
        get { return rb.position + beamOrigin; }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        // a sleeping body would stop OnTriggerStay2D, so the robot never sleeps
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        Collider2D[] cols = GetComponents<Collider2D>();
        if (cols.Length == 0)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.3f, 2.2f);
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
            hop.autoFace = false; // this script decides which way the robot looks
        }

        Transform coreTransform = transform.Find("Sprite/Core");
        if (coreTransform != null)
        {
            core = coreTransform.GetComponent<SpriteRenderer>();
            if (core != null)
            {
                coreBase = core.color;
            }
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
        if (laser != null)
        {
            laser.HideAll();
        }
    }

    private void OnDestroy()
    {
        if (laser != null)
        {
            Destroy(laser.gameObject);
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

        nextAttackTime = Time.time + Random.Range(1.5f, attackCooldown);
        nextContactTime = Time.time + 0.6f; // short grace period right after spawning

        GameFX.Burst(transform.position, new Color[] { CyanSpark, OrangeSpark }, 8, 3f, 0.15f, 0.4f, Vector2.zero);
    }

    private void Update()
    {
        if (player != null && hop != null && state != State.Sweep)
        {
            hop.Face(player.position.x - transform.position.x);
        }

        UpdateCore();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        float dt = Time.fixedDeltaTime;

        // knockback has priority (but the laser sweep can't be interrupted)
        if (knockTimer > 0f && state != State.Sweep)
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
            case State.Calibrate: UpdateCalibrate(dt); break;
            case State.Sweep: UpdateSweep(dt); break;
            case State.Overheat: UpdateOverheat(dt); break;
        }
    }

    // ------------------------------------------------------------------ states

    private void UpdateChase(float dt)
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;
        Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.zero;

        Vector2 desired = dist > keepDistance ? dir * moveSpeed : Vector2.zero;
        desired += GetSeparation(rb.position);

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * dt);

        if (Time.time >= nextAttackTime && dist >= attackMinRange && dist <= attackMaxRange)
        {
            StartCalibrate();
        }
    }

    private void StartCalibrate()
    {
        state = State.Calibrate;
        timer = 0f;
        aimAngle = AngleToPlayer();

        EnsureLaser();
        laser.ShowFan(Origin, aimAngle, sweepAngle, GetBeamLength);

        if (feedback != null)
        {
            feedback.Shake(0.05f, calibrateTime, false);
        }
    }

    private void UpdateCalibrate(float dt)
    {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
        timer += dt;

        float lockMoment = Mathf.Min(lockTime, calibrateTime * 0.5f);
        bool locked = timer >= calibrateTime - lockMoment;

        if (!locked)
        {
            // the aim turns toward the player at a limited speed, like a servo joint
            aimAngle = Mathf.MoveTowardsAngle(aimAngle, AngleToPlayer(), turnSpeed * dt);
            laser.UpdateFan(Origin, aimAngle, sweepAngle, GetBeamLength);
        }

        laser.SetLocked(locked);

        if (hop != null)
        {
            hop.charge = Mathf.Clamp01(timer / calibrateTime) * 0.6f;
        }

        if (timer >= calibrateTime)
        {
            StartSweep();
        }
    }

    private void StartSweep()
    {
        state = State.Sweep;
        timer = 0f;
        beamHit = false;

        float half = sweepAngle * 0.5f;
        float sign = Random.value < 0.5f ? -1f : 1f;
        sweepStart = aimAngle - sign * half;
        sweepEnd = aimAngle + sign * half;

        if (feedback != null)
        {
            feedback.StopShake();
        }
        if (hop != null)
        {
            hop.charge = 0f;
        }

        CameraShake.Shake(0.08f, sweepTime);
        GameFX.Burst(Origin, new Color[] { CyanSpark, OrangeSpark }, 8, 4f, 0.15f, 0.4f, Vector2.zero);
    }

    private void UpdateSweep(float dt)
    {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
        timer += dt;

        float t = Mathf.Clamp01(timer / sweepTime);
        float angle = Mathf.Lerp(sweepStart, sweepEnd, t);
        float length = GetBeamLength(angle);

        laser.ShowBeam(Origin, angle, length, beamWidth);
        CheckBeamHit(angle, length);

        if (hop != null)
        {
            hop.Face(Mathf.Cos(angle * Mathf.Deg2Rad));
        }

        if (t >= 1f)
        {
            StartOverheat();
        }
    }

    private void StartOverheat()
    {
        state = State.Overheat;
        timer = 0f;
        sparkTimer = 0f;

        laser.HideAll();
        nextAttackTime = Time.time + attackCooldown;
        health.damageMultiplier = overheatDamageMultiplier;

        if (feedback != null)
        {
            feedback.Shake(0.06f, overheatTime, false);
            GameFX.Popup(transform.position + Vector3.up * (feedback.TopOffset + 0.5f), "ERROR", OrangeSpark, 4f);
        }
    }

    private void UpdateOverheat(float dt)
    {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
        timer += dt;
        sparkTimer -= dt;

        if (sparkTimer <= 0f)
        {
            sparkTimer = 0.3f;

            Vector2 spot = Origin + Random.insideUnitCircle * 0.5f;
            GameFX.Burst(spot, new Color[] { CyanSpark, OrangeSpark }, 4, 3f, 0.12f, 0.35f, Vector2.zero);

            // a hit shortens the shake, so start it again
            if (feedback != null)
            {
                feedback.Shake(0.06f, Mathf.Max(0.1f, overheatTime - timer), false);
            }
        }

        if (timer >= overheatTime)
        {
            ResetToChase();
        }
    }

    private void ResetToChase()
    {
        state = State.Chase;
        timer = 0f;
        health.damageMultiplier = 1f;

        if (hop != null)
        {
            hop.charge = 0f;
        }
        if (laser != null)
        {
            laser.HideAll();
        }
        if (feedback != null)
        {
            feedback.StopShake();
        }
    }

    // ------------------------------------------------------------------ helpers

    private void EnsureLaser()
    {
        if (laser != null) return;

        GameObject go = new GameObject("RobotLaser");
        laser = go.AddComponent<RobotLaser>();
    }

    private float AngleToPlayer()
    {
        Vector2 d = (Vector2)player.position - Origin;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    // How far the laser reaches in this direction (it stops at classroom walls).
    private float GetBeamLength(float angleDegrees)
    {
        float r = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));

        float best = beamLength;
        RaycastHit2D[] hits = Physics2D.RaycastAll(Origin, dir, beamLength);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].collider.GetComponent<ClassroomWall>() == null) continue;

            if (hits[i].distance < best)
            {
                best = hits[i].distance;
            }
        }

        return Mathf.Max(0.3f, best);
    }

    // Simple math instead of a collider: is the player inside the beam rectangle?
    private void CheckBeamHit(float angleDegrees, float length)
    {
        if (beamHit || playerHealth == null || playerHealth.IsDead) return;

        float r = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        Vector2 rel = (Vector2)player.position - Origin;

        float along = Vector2.Dot(rel, dir);
        float sideways = rel.x * dir.y - rel.y * dir.x;

        bool inLength = along > 0f && along <= length + playerRadius;
        bool inWidth = Mathf.Abs(sideways) <= beamWidth * 0.5f + playerRadius;

        if (inLength && inWidth)
        {
            beamHit = true;

            // push the player out of the beam, to the side they are standing on
            Vector2 push = new Vector2(-dir.y, dir.x);
            if (Vector2.Dot(rel, push) < 0f)
            {
                push = -push;
            }

            playerHealth.TakeDamage(beamDamage, push, beamKnockback);
        }
    }

    private void UpdateCore()
    {
        if (core == null) return;

        Color c = coreBase;

        if (state == State.Calibrate)
        {
            float progress = Mathf.Clamp01(timer / calibrateTime);
            float pulse = Mathf.Sin(Time.time * (10f + 30f * progress)) * 0.5f + 0.5f;
            c = Color.Lerp(coreBase, Color.white, pulse * progress);
        }
        else if (state == State.Sweep)
        {
            c = OrangeSpark;
        }
        else if (state == State.Overheat)
        {
            bool on = Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
            c = on ? OrangeSpark : DeadCore;
        }

        core.color = c;
    }

    private Vector2 GetSeparation(Vector2 pos)
    {
        Vector2 push = Vector2.zero;

        for (int i = 0; i < all.Count; i++)
        {
            RobotEnemy other = all[i];
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
        // shooting the robot while it calibrates cancels the attack
        if (state == State.Calibrate)
        {
            ResetToChase();
            nextAttackTime = Time.time + 1.5f;
        }

        if (state == State.Sweep) return;

        if (knockback > 0f && hitDir.sqrMagnitude > 0.0001f)
        {
            knockVelocity = hitDir.normalized * knockback;
            knockDuration = 0.18f;
            knockTimer = knockDuration;
        }
    }

    // ------------------------------------------------------------------ touching the player

    private void OnTriggerStay2D(Collider2D other)
    {
        if (state == State.Sweep || state == State.Overheat) return;
        if (Time.time < nextContactTime) return;

        Health target = other.GetComponentInParent<Health>();
        if (target == null || target.IsDead || !target.CompareTag("Player")) return;

        nextContactTime = Time.time + contactCooldown;

        Vector2 dir = (Vector2)(target.transform.position - transform.position);
        target.TakeDamage(contactDamage, dir, contactKnockback);
    }
}